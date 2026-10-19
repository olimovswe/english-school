using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EnglishSchool.SystemTests.TestSupport;

/// <summary>
/// Haqiqiy API host (<c>Host.Api</c>'ning <c>Program</c>'i) tizim konteynerlari ustida. Development'dagidek barcha modullar
/// ishga tushishda migratsiya qilinadi va kurs kontenti seed qilinadi.
/// </summary>
public sealed class ApiFactory(SystemFixture system) : WebApplicationFactory<Program>
{
    /// <summary>Bazasi bor modullar: <c>{Modul}:ApplyMigrationsOnStartup</c> sozlamasi.</summary>
    public static readonly string[] MigratedModules = ["Identity", "Curriculum", "Discipline", "Practice", "Retention", "Speech", "Analytics"];

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        foreach (var (key, value) in system.SharedSettings)
        {
            builder.UseSetting(key, value);
        }

        builder.UseSetting("Identity:Jwt:SigningKey", system.SigningKey);
        foreach (var module in MigratedModules)
        {
            builder.UseSetting($"{module}:ApplyMigrationsOnStartup", "true");
        }

        builder.UseSetting("Curriculum:SeedOnStartup", "true");
        builder.UseSetting("Practice:SeedOnStartup", "true");
        builder.UseDefaultServiceProvider(options =>
        {
            options.ValidateScopes = true;
            options.ValidateOnBuild = true;
        });
    }
}
