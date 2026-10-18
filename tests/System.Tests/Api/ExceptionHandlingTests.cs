using System.Net;
using System.Text;
using System.Text.Json;
using EnglishSchool.Modules.Curriculum.Contracts;
using EnglishSchool.SystemTests.TestSupport;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishSchool.SystemTests.Api;

[Collection(SystemFixture.CollectionName)]
public sealed class ExceptionHandlingTests(SystemFixture system)
{
    [Fact]
    public async Task An_unhandled_exception_becomes_problem_details_without_internal_details()
    {
        await using var api = system.Api.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.AddScoped<ICurriculumQueries, ExplodingCurriculumQueries>()));
        using var client = api.CreateClient();

        using var response = await client.GetAsync(new Uri("/api/v1/curriculum/days", UriKind.Relative));
        var body = await response.Content.ReadAsStringAsync();
        var problem = JsonDocument.Parse(body).RootElement;

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(
            (500, "Server xatosi", "Kutilmagan xatolik yuz berdi. Birozdan keyin qayta urinib koʻring."),
            (problem.GetProperty("status").GetInt32(), problem.GetProperty("title").GetString(), problem.GetProperty("detail").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(problem.GetProperty("traceId").GetString()));

        // Istisno matni (unda server nomlari va parollar boʻlishi mumkin) va stack trace mijozga chiqmaydi.
        Assert.DoesNotContain(ExplodingCurriculumQueries.Secret, body, StringComparison.Ordinal);
        Assert.DoesNotContain(nameof(ExplodingCurriculumQueries), body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_malformed_request_body_is_a_400_problem_not_a_server_error()
    {
        using var client = system.Api.CreateClient();
        using var content = new StringContent("{ \"email\": ", Encoding.UTF8, "application/json");

        using var response = await client.PostAsync(new Uri("/api/v1/identity/login", UriKind.Relative), content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    private sealed class ExplodingCurriculumQueries : ICurriculumQueries
    {
        public const string Secret = "Host=db.internal;Password=hunter2";

        public Task<IReadOnlyList<DaySummaryDto>> GetDaysAsync(CancellationToken cancellationToken) =>
            throw new InvalidOperationException(Secret);

        public Task<DayDto?> GetDayAsync(int dayNumber, CancellationToken cancellationToken) =>
            throw new InvalidOperationException(Secret);

        public Task<IReadOnlyList<VocabularyEntryDto>> GetVocabularyAsync(int dayNumber, CancellationToken cancellationToken) =>
            throw new InvalidOperationException(Secret);
    }
}
