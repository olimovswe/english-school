using EnglishSchool.BuildingBlocks.Abstractions.Events;
using EnglishSchool.BuildingBlocks.Outbox;
using EnglishSchool.Modules.Discipline.Contracts.Events;
using EnglishSchool.Modules.Practice.Contracts.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishSchool.Architecture.Tests;

/// <summary>
/// Integration eventlar modullar oʻrtasidagi ommaviy shartnoma va outbox'da JSON boʻlib yashaydi:
/// ular faqat Contracts'da, oʻzgarmas record sifatida yoziladi va serializatsiyadan buzilmasdan qaytishi kerak.
/// </summary>
public sealed class IntegrationEventTests
{
    public static TheoryData<Type> IntegrationEvents => new(
        ModuleComposition.ContractAssemblies
            .SelectMany(assembly => assembly.GetExportedTypes())
            .Where(type => typeof(IIntegrationEvent).IsAssignableFrom(type) && !type.IsAbstract));

    [Fact]
    public void Every_module_contract_assembly_is_scanned_for_events()
    {
        Assert.True(IntegrationEvents.Count >= 8, "Contracts'dagi eventlar topilmadi.");
    }

    [Theory]
    [MemberData(nameof(IntegrationEvents))]
    public void Integration_event_is_a_sealed_record_in_a_contracts_events_namespace(Type eventType)
    {
        Assert.True(eventType.IsSealed, $"{eventType.Name} sealed emas.");
        Assert.True(typeof(IntegrationEvent).IsAssignableFrom(eventType), $"{eventType.Name} IntegrationEvent'dan meros olmagan.");
        Assert.NotNull(eventType.GetMethod("<Clone>$"));
        Assert.EndsWith(".Contracts.Events", eventType.Namespace, StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(IntegrationEvents))]
    public void Integration_event_survives_the_outbox_round_trip(Type eventType)
    {
        var constructor = Assert.Single(eventType.GetConstructors());
        var original = (IIntegrationEvent)constructor.Invoke([.. constructor.GetParameters().Select(p => SampleValue(p.ParameterType))]);

        var (typeName, content) = OutboxSerializer.Serialize(original);

        Assert.Equal(eventType.FullName, typeName);
        Assert.Equal(original, OutboxSerializer.Deserialize(content, eventType));
    }

    [Fact]
    public void Module_implementations_define_no_integration_events()
    {
        var misplaced = ModuleComposition.ImplementationAssemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => typeof(IIntegrationEvent).IsAssignableFrom(type))
            .Select(type => type.FullName);

        Assert.Empty(misplaced);
    }

    [Fact]
    public void Modules_that_raise_events_write_them_through_the_outbox()
    {
        using var host = ModuleComposition.BuildNonWebHost(withOutboxDispatcher: false);
        using var scope = host.Services.CreateScope();
        var stores = host.Services.GetServices<IOutboxStore>().Select(store => store.Name).ToList();

        var publishing = ModuleComposition.ImplementationAssemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => typeof(DbContext).IsAssignableFrom(type) && !type.IsAbstract)
            .Select(type => (DbContext)scope.ServiceProvider.GetRequiredService(type))
            .Where(db => db.Model.GetEntityTypes().Any(entity => typeof(IIntegrationEventSource).IsAssignableFrom(entity.ClrType)))
            .ToList();

        Assert.NotEmpty(publishing);
        Assert.All(publishing, db =>
        {
            var name = db.GetType().Name;
            Assert.True(db.Model.FindEntityType(typeof(OutboxMessage)) is not null, $"{name}: AddOutboxMessages() chaqirilmagan.");
            Assert.True(
                db.GetService<IDbContextOptions>().FindExtension<CoreOptionsExtension>()?.Interceptors?.Contains(OutboxInterceptor.Instance) == true,
                $"{name}: UseOutbox() chaqirilmagan.");
            Assert.True(stores.Contains(name), $"{name}: AddOutboxStore<{name}>() chaqirilmagan.");
        });
    }

    [Fact]
    public void Every_consumer_in_module_code_is_internal_and_registered_for_the_worker()
    {
        using var host = ModuleComposition.BuildNonWebHost(withOutboxDispatcher: true);
        using var scope = host.Services.CreateScope();

        var consumers = ModuleComposition.ImplementationAssemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => !type.IsAbstract && ConsumerContracts(type).Any())
            .ToList();

        Assert.NotEmpty(consumers);
        Assert.All(consumers, consumer =>
        {
            Assert.False(consumer.IsPublic, $"{consumer.Name} modul ichida internal boʻlishi kerak.");
            Assert.All(ConsumerContracts(consumer), contract => Assert.True(
                scope.ServiceProvider.GetServices(contract).Any(service => service?.GetType() == consumer),
                $"{consumer.Name}: AddIntegrationEventConsumer chaqirilmagan, Worker unga eventni yetkazmaydi."));
        });
    }

    [Fact]
    public void Analytics_registers_each_progress_projection_consumer_for_the_worker()
    {
        using var host = ModuleComposition.BuildNonWebHost(withOutboxDispatcher: true);
        using var scope = host.Services.CreateScope();
        var analytics = ModuleComposition.ImplementationAssemblies
            .Single(assembly => assembly.GetName().Name == "EnglishSchool.Modules.Analytics");

        Type[] eventTypes = [typeof(AttemptEvaluated), typeof(ErrorDetected), typeof(DailyGatePassed), typeof(SessionEnded)];
        foreach (var eventType in eventTypes)
        {
            var consumerContract = typeof(IIntegrationEventConsumer<>).MakeGenericType(eventType);
            var consumerType = Assert.Single(analytics.GetTypes(),
                type => !type.IsAbstract && consumerContract.IsAssignableFrom(type));
            Assert.Contains(scope.ServiceProvider.GetServices(consumerContract),
                consumer => consumer?.GetType() == consumerType);
        }
    }

    private static IEnumerable<Type> ConsumerContracts(Type type) =>
        type.GetInterfaces().Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IIntegrationEventConsumer<>));

    private static object SampleValue(Type type) => type switch
    {
        _ when type == typeof(Guid) => Guid.NewGuid(),
        _ when type == typeof(string) => "namuna",
        _ when type == typeof(int) => 7,
        _ when type == typeof(long) => 7L,
        _ when type == typeof(decimal) => 0.75m,
        _ when type == typeof(bool) => true,
        _ => throw new NotSupportedException($"{type.Name} uchun namuna qiymat qoʻshing."),
    };
}
