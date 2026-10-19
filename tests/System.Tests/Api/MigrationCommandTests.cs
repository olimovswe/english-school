using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EnglishSchool.SystemTests.TestSupport;
using Microsoft.AspNetCore.Hosting;

namespace EnglishSchool.SystemTests.Api;

[Collection(SystemFixture.CollectionName)]
public sealed class MigrationCommandTests(SystemFixture system)
{
    [Fact]
    public async Task The_migrate_command_prepares_an_empty_database_for_an_api_that_never_migrates()
    {
        var database = await system.CreateDatabaseAsync();

        // Productiondagidek: avval bir martalik migrate (qayta ishga tushirish ham xavfsiz), keyin migratsiya qilmaydigan API.
        await SystemFixture.MigrateAsync(database);
        await SystemFixture.MigrateAsync(database);
        await using var api = system.Api.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:Postgres", database);
            foreach (var module in ApiFactory.MigratedModules)
            {
                builder.UseSetting($"{module}:ApplyMigrationsOnStartup", "false");
            }

            builder.UseSetting("Curriculum:SeedOnStartup", "false");
            builder.UseSetting("Practice:SeedOnStartup", "false");
        });
        using var client = api.CreateClient();

        using var ready = await client.GetAsync(new Uri("/health/ready", UriKind.Relative));
        var days = await client.GetFromJsonAsync<JsonElement>(new Uri("/api/v1/curriculum/days", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, ready.StatusCode);
        Assert.Equal(56, days.GetArrayLength());
        using var student = await Student.RegisterAsync(api, "Migrated Student");
        Assert.NotEmpty((await student.GetAsync("/api/v1/practice/days/1/exercises")).EnumerateArray());
    }
}
