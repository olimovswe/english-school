using EnglishSchool.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;

namespace EnglishSchool.Modules.Identity.Infrastructure;

/// <summary>
/// Modul faqat oʻz <c>identity</c> schema'si bilan ishlaydi. Parol xeshlari va tokenlar
/// boshqa modullarga hech qachon chiqarilmaydi.
/// </summary>
internal sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options) : DbContext(options)
{
    public const string Schema = "identity";

    public DbSet<User> Users => Set<User>();

    public DbSet<UserCredentials> Credentials => Set<UserCredentials>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    /// <summary>
    /// Qatorni tranzaksiya oxirigacha bloklaydi: bir akkauntga parallel login urinishlari navbat bilan
    /// tekshiriladi va xato hisoblagichini chetlab oʻta olmaydi.
    /// </summary>
    public async Task<UserCredentials?> LockCredentialsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var rows = await Credentials
            .FromSql($"""SELECT * FROM "identity".user_credentials WHERE user_id = {userId} FOR UPDATE""")
            .ToListAsync(cancellationToken);

        return rows.SingleOrDefault();
    }

    /// <summary>Bitta refresh token faqat bir marta almashtiriladi, hatto ikki soʻrov bir vaqtda kelsa ham.</summary>
    public async Task<RefreshToken?> LockRefreshTokenAsync(string tokenHash, CancellationToken cancellationToken)
    {
        var rows = await RefreshTokens
            .FromSql($"""SELECT * FROM "identity".refresh_tokens WHERE token_hash = {tokenHash} FOR UPDATE""")
            .ToListAsync(cancellationToken);

        return rows.SingleOrDefault();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<User>(user =>
        {
            user.ToTable("users");
            user.HasKey(x => x.Id);
            user.Property(x => x.Email).HasMaxLength(254);
            user.Property(x => x.NormalizedEmail).HasMaxLength(254);
            user.HasIndex(x => x.NormalizedEmail).IsUnique();
            user.HasIndex(x => x.TelegramId).IsUnique();
            user.Property(x => x.DisplayName).HasMaxLength(100);
            user.Property(x => x.TimeZone).HasMaxLength(64);

            user.HasOne(x => x.Credentials)
                .WithOne()
                .HasForeignKey<UserCredentials>(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserCredentials>(credentials =>
        {
            credentials.ToTable("user_credentials");
            credentials.HasKey(x => x.UserId);
        });

        modelBuilder.Entity<RefreshToken>(token =>
        {
            token.ToTable("refresh_tokens");
            token.HasKey(x => x.Id);
            token.Property(x => x.TokenHash).HasMaxLength(64);
            token.HasIndex(x => x.TokenHash).IsUnique();
            token.HasIndex(x => x.FamilyId);

            token.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
