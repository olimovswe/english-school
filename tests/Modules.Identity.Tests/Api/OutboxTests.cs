using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using EnglishSchool.BuildingBlocks.Abstractions.Events;
using EnglishSchool.BuildingBlocks.Outbox;
using EnglishSchool.Modules.Identity.Contracts.Events;
using EnglishSchool.Modules.Identity.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishSchool.Modules.Identity.Tests.Api;

/// <summary>
/// Outbox oxirigacha: API eventni roʻyxatdan oʻtish bilan bitta tranzaksiyada yozadi, Worker'dagi
/// dispatcher esa uni obunachiga yetkazadi. Dispatcher bu yerda Worker'dagidek yigʻilgan DI'da ishlaydi.
/// </summary>
[Collection(IdentityApiFactory.CollectionName)]
public sealed class OutboxTests(IdentityApiFactory factory)
{
    [Fact]
    public async Task Registration_writes_UserRegistered_to_the_identity_outbox()
    {
        using var client = factory.CreateClient();
        var registration = await client.RegisterNewUserAsync();

        var (message, registered) = Assert.Single(await FindUserRegisteredAsync(registration.UserId));

        Assert.Equal(registered.EventId, message.Id);
        Assert.Null(message.ProcessedAt);
        Assert.Equal("Ali Valiyev", registered.DisplayName);
        Assert.Equal("Asia/Tashkent", registered.TimeZone);
    }

    [Fact]
    public async Task Rejected_registration_writes_no_event()
    {
        using var client = factory.CreateClient();
        var email = IdentityApi.UniqueEmail();
        var displayName = $"Outbox {Guid.NewGuid():N}";

        using var accepted = await client.RegisterAsync(email, displayName: displayName);
        using var rejected = await client.RegisterAsync(email, displayName: displayName);

        Assert.Equal(HttpStatusCode.Created, accepted.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, rejected.StatusCode);
        Assert.Single(await ReadAllUserRegisteredAsync(), x => x.Event.DisplayName == displayName);
    }

    [Fact]
    public async Task Dispatcher_delivers_the_event_to_consumers_and_marks_it_processed()
    {
        using var client = factory.CreateClient();
        var registration = await client.RegisterNewUserAsync();
        await using var worker = BuildWorker(services => services
            .AddSingleton<ReceivedEvents>()
            .AddIntegrationEventConsumer<UserRegistered, RecordingConsumer>());
        var received = worker.GetRequiredService<ReceivedEvents>();

        await DrainAsync(worker, () => received.Events.Any(e => e.UserId == registration.UserId));

        Assert.Contains(received.Events, e => e.UserId == registration.UserId);
        var (message, _) = Assert.Single(await FindUserRegisteredAsync(registration.UserId));
        Assert.NotNull(message.ProcessedAt);
        Assert.Equal(0, message.FailedAttempts);
    }

    [Fact]
    public async Task Failing_consumer_is_retried_after_backoff_and_parked_at_max_attempts()
    {
        using var client = factory.CreateClient();
        var registration = await client.RegisterNewUserAsync();
        var clock = new TestClock(DateTimeOffset.UtcNow);
        await using var worker = BuildWorker(
            services => services
                .AddSingleton(new FailFor(registration.UserId))
                .AddIntegrationEventConsumer<UserRegistered, FailingConsumer>(),
            maxAttempts: 3,
            clock);
        var processor = worker.GetRequiredService<OutboxProcessor>();

        await processor.ProcessAsync(CancellationToken.None);
        await processor.ProcessAsync(CancellationToken.None);
        Assert.Equal(1, await FailedAttemptsAsync(registration.UserId));

        for (var pass = 0; pass < 4; pass++)
        {
            clock.Advance(TimeSpan.FromHours(1));
            await processor.ProcessAsync(CancellationToken.None);
        }

        var (message, _) = Assert.Single(await FindUserRegisteredAsync(registration.UserId));
        Assert.Equal(3, message.FailedAttempts);
        Assert.Null(message.ProcessedAt);
        Assert.Contains("consumer is down", message.LastError, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_consumer_timeout_counts_as_a_failure_and_does_not_stop_the_dispatcher()
    {
        using var client = factory.CreateClient();
        var registration = await client.RegisterNewUserAsync();
        await using var worker = BuildWorker(services => services
            .AddSingleton(new FailFor(registration.UserId))
            .AddIntegrationEventConsumer<UserRegistered, TimingOutConsumer>());

        // HttpClient timeout'i ham OperationCanceledException: u partiyani bekor qilib, dispatcher'ni toʻxtatmasligi kerak.
        await worker.GetRequiredService<OutboxProcessor>().ProcessAsync(CancellationToken.None);

        var (message, _) = Assert.Single(await FindUserRegisteredAsync(registration.UserId));
        Assert.Equal(((DateTimeOffset?)null, 1), (message.ProcessedAt, message.FailedAttempts));
        Assert.StartsWith(nameof(TaskCanceledException), message.LastError, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Stopping_mid_batch_keeps_finished_deliveries_and_leaves_the_rest_queued()
    {
        using var client = factory.CreateClient();
        var first = await client.RegisterNewUserAsync();
        var second = await client.RegisterNewUserAsync();
        using var stopping = new CancellationTokenSource();
        await using var worker = BuildWorker(services => services
            .AddSingleton(new StopAfter(first.UserId, stopping))
            .AddIntegrationEventConsumer<UserRegistered, StopRequestingConsumer>());
        var processor = worker.GetRequiredService<OutboxProcessor>();

        // Birinchi event yetkazilgach toʻxtash signali keladi (SIGTERM kabi): ikkinchisi shu partiyada yetkazilmaydi.
        await processor.ProcessAsync(stopping.Token);

        Assert.NotNull(Assert.Single(await FindUserRegisteredAsync(first.UserId)).Message.ProcessedAt);
        var (queued, _) = Assert.Single(await FindUserRegisteredAsync(second.UserId));
        Assert.Equal(((DateTimeOffset?)null, 0), (queued.ProcessedAt, queued.FailedAttempts));

        await DrainAsync(worker, () => false);
        Assert.NotNull(Assert.Single(await FindUserRegisteredAsync(second.UserId)).Message.ProcessedAt);
    }

    private ServiceProvider BuildWorker(Action<IServiceCollection> configure, int maxAttempts = 10, TimeProvider? clock = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = factory.ConnectionString,
                ["Outbox:MaxAttempts"] = maxAttempts.ToString(CultureInfo.InvariantCulture),
                // Boshqa testlarning eventlari ham navbatda: bitta partiya hammasini qamrasin.
                ["Outbox:BatchSize"] = "1000",
            })
            .Build();

        var services = new ServiceCollection().AddLogging();
        if (clock is not null)
        {
            services.AddSingleton(clock);
        }

        new IdentityModule().AddServices(services, configuration);
        services.AddOutboxDispatcher(configuration);
        configure(services);
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
    }

    /// <summary>Baza boshqa testlarning eventlarini ham saqlaydi: kerakli event yetguncha partiyalar yetkaziladi.</summary>
    private static async Task DrainAsync(IServiceProvider worker, Func<bool> done)
    {
        var processor = worker.GetRequiredService<OutboxProcessor>();
        while (!done())
        {
            if (await processor.ProcessAsync(CancellationToken.None) == 0)
            {
                return;
            }
        }
    }

    private async Task<int> FailedAttemptsAsync(Guid userId) =>
        Assert.Single(await FindUserRegisteredAsync(userId)).Message.FailedAttempts;

    private async Task<List<(OutboxMessage Message, UserRegistered Event)>> FindUserRegisteredAsync(Guid userId) =>
        [.. (await ReadAllUserRegisteredAsync()).Where(x => x.Event.UserId == userId)];

    private async Task<List<(OutboxMessage Message, UserRegistered Event)>> ReadAllUserRegisteredAsync()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var typeName = OutboxSerializer.TypeName(typeof(UserRegistered));

        var messages = await db.Set<OutboxMessage>().AsNoTracking().Where(m => m.Type == typeName).ToListAsync();
        return [.. messages.Select(m => (m, (UserRegistered)OutboxSerializer.Deserialize(m.Content, typeof(UserRegistered))))];
    }

    private sealed class ReceivedEvents
    {
        public ConcurrentBag<UserRegistered> Events { get; } = [];
    }

    private sealed class RecordingConsumer(ReceivedEvents received) : IIntegrationEventConsumer<UserRegistered>
    {
        public Task HandleAsync(UserRegistered integrationEvent, CancellationToken cancellationToken)
        {
            received.Events.Add(integrationEvent);
            return Task.CompletedTask;
        }
    }

    private sealed record FailFor(Guid UserId);

    private sealed class FailingConsumer(FailFor target) : IIntegrationEventConsumer<UserRegistered>
    {
        public Task HandleAsync(UserRegistered integrationEvent, CancellationToken cancellationToken) =>
            integrationEvent.UserId == target.UserId
                ? throw new InvalidOperationException("Analytics consumer is down.")
                : Task.CompletedTask;
    }

    private sealed class TimingOutConsumer(FailFor target) : IIntegrationEventConsumer<UserRegistered>
    {
        public Task HandleAsync(UserRegistered integrationEvent, CancellationToken cancellationToken) =>
            integrationEvent.UserId == target.UserId
                ? throw new TaskCanceledException("The request was canceled due to the configured HttpClient.Timeout of 30 seconds elapsing.")
                : Task.CompletedTask;
    }

    private sealed record StopAfter(Guid UserId, CancellationTokenSource Stopping);

    private sealed class StopRequestingConsumer(StopAfter stop) : IIntegrationEventConsumer<UserRegistered>
    {
        public async Task HandleAsync(UserRegistered integrationEvent, CancellationToken cancellationToken)
        {
            if (integrationEvent.UserId == stop.UserId)
            {
                await stop.Stopping.CancelAsync();
            }
        }
    }

    private sealed class TestClock(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset _now = now;

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan by) => _now += by;
    }
}
