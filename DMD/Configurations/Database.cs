using DMD.DOMAIN.Entities.UserProfile;
using DMD.DOMAIN.Enums;
using DMD.PERSISTENCE.Context;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace DMD.API.Configurations
{
    public class Database
    {
        internal static void RegisterDatabase(WebApplicationBuilder builder)
        {
            builder.Services.AddDbContext<DmdDbContext>(options =>
            {
                var connStr = builder.Configuration.GetConnectionString("Default"); // must match appsettings.json
                options.UseSqlServer(connStr);
            });
        }

        internal static void ApplyPendingMigrations(WebApplication app)
        {
            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<DmdDbContext>();
                dbContext.Database.Migrate();
            }
        }


        internal static async Task SeedDatabase(WebApplication app)
        {
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var userManager = services.GetRequiredService<UserManager<UserProfile>>();
                    var configuration = services.GetRequiredService<IConfiguration>();
                    var logger = services.GetRequiredService<ILogger<Program>>();

                    var context = services.GetRequiredService<DmdDbContext>();
                    await context.Database.EnsureCreatedAsync();
                    await SeedDatabaseInternal(userManager, configuration, logger);
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "An error occurred while seeding the database.");
                }
            }
        }

        private static async Task SeedDatabaseInternal(
            UserManager<UserProfile> userManager,
            IConfiguration configuration,
            ILogger logger)
        {
            // Seed admin user
            var adminEmail = configuration["Seed:Admin:Email"] ?? "admin@email.com";
            var adminPassword = configuration["Seed:Admin:Password"] ?? "abcdE@123";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            adminUser ??= await userManager.FindByNameAsync(adminEmail);

            if (adminUser == null)  
            {
                adminUser = new UserProfile
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailAddress = adminEmail,
                    FirstName = "Admin",
                    LastName = "User",
                    MiddleName = string.Empty,
                    ContactNumber = string.Empty,
                    Address = string.Empty,
                    Religion = string.Empty,
                    Bio = string.Empty,
                    Role = UserRole.SuperAdmin,
                    IsActive = true
                };

                var result = await userManager.CreateAsync(adminUser, adminPassword);
                if (result.Succeeded)
                {
                    logger.LogInformation("Admin user created successfully with email {Email}", adminEmail);
                }
                else
                {
                    logger.LogError(
                        "Failed to create admin user {Email}. Errors: {Errors}",
                        adminEmail,
                        string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                var needsUpdate = false;

                if (string.IsNullOrWhiteSpace(adminUser.Email))
                {
                    adminUser.Email = adminEmail;
                    needsUpdate = true;
                }

                if (string.IsNullOrWhiteSpace(adminUser.EmailAddress))
                {
                    adminUser.EmailAddress = adminEmail;
                    needsUpdate = true;
                }

                if (!adminUser.IsActive)
                {
                    adminUser.IsActive = true;
                    needsUpdate = true;
                }

                if (needsUpdate)
                {
                    var updateResult = await userManager.UpdateAsync(adminUser);
                    if (!updateResult.Succeeded)
                    {
                        logger.LogError(
                            "Failed to update existing admin user {Email}. Errors: {Errors}",
                            adminEmail,
                            string.Join(", ", updateResult.Errors.Select(e => e.Description)));
                    }
                }
            }
        }

        internal static async Task ConfigureDatabase(IServiceProvider services)
        {
            try
            {
                await EnsureHangfireDatabaseAsync(services);

                var context = services.GetRequiredService<DmdDbContext>();

                await context.Database.MigrateAsync();
                await EnsureClinicIdIsNullableAsync(context);
            }
            catch (Exception ex)
            {
                var logger = services.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "An error occurred during migration.");
            }
        }

        internal static async Task EnsureClinicIdIsNullableAsync(DmdDbContext context)
        {
            const string sql = @"
IF COL_LENGTH('AspNetUsers', 'ClinicId') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_AspNetUsers_ClinicProfiles_ClinicId')
        ALTER TABLE [AspNetUsers] DROP CONSTRAINT [FK_AspNetUsers_ClinicProfiles_ClinicId];

    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AspNetUsers_ClinicId' AND object_id = OBJECT_ID('AspNetUsers'))
        DROP INDEX [IX_AspNetUsers_ClinicId] ON [AspNetUsers];

    IF EXISTS (
        SELECT 1
        FROM sys.columns
        WHERE object_id = OBJECT_ID('AspNetUsers')
          AND name = 'ClinicId'
          AND is_nullable = 0
    )
        ALTER TABLE [AspNetUsers] ALTER COLUMN [ClinicId] int NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AspNetUsers_ClinicId' AND object_id = OBJECT_ID('AspNetUsers'))
        CREATE INDEX [IX_AspNetUsers_ClinicId] ON [AspNetUsers]([ClinicId]) WHERE [ClinicId] IS NOT NULL;

    IF EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ClinicProfiles]') AND type = 'U')
       AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_AspNetUsers_ClinicProfiles_ClinicId')
        ALTER TABLE [AspNetUsers] ADD CONSTRAINT [FK_AspNetUsers_ClinicProfiles_ClinicId]
            FOREIGN KEY ([ClinicId]) REFERENCES [ClinicProfiles]([Id]);
END";

            await context.Database.ExecuteSqlRawAsync(sql);
        }

        internal static async Task EnsureHangfireDatabaseAsync(IServiceProvider services)
        {
            try
            {
                var configuration = services.GetRequiredService<IConfiguration>();
                var logger = services.GetRequiredService<ILogger<Program>>();
                var hangfireConnectionString = configuration.GetConnectionString("Hangfire");

                if (string.IsNullOrWhiteSpace(hangfireConnectionString))
                {
                    logger.LogInformation("Hangfire connection string is not configured. Skipping Hangfire database bootstrap.");
                    return;
                }

                var hangfireBuilder = new SqlConnectionStringBuilder(hangfireConnectionString);
                var hangfireDatabaseName = hangfireBuilder.InitialCatalog;

                if (string.IsNullOrWhiteSpace(hangfireDatabaseName))
                {
                    logger.LogWarning("Hangfire Initial Catalog is missing. Skipping Hangfire database bootstrap.");
                    return;
                }

                var masterBuilder = new SqlConnectionStringBuilder(hangfireConnectionString)
                {
                    InitialCatalog = "master"
                };

                await using var connection = new SqlConnection(masterBuilder.ConnectionString);
                await connection.OpenAsync();

                await using var command = connection.CreateCommand();
                command.CommandText =
                    $"IF DB_ID(@databaseName) IS NULL BEGIN CREATE DATABASE [{hangfireDatabaseName}] END";
                command.Parameters.AddWithValue("@databaseName", hangfireDatabaseName);

                await command.ExecuteNonQueryAsync();
                logger.LogInformation("Ensured Hangfire database {DatabaseName} exists.", hangfireDatabaseName);
            }
            catch (Exception ex)
            {
                var logger = services.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "An error occurred while ensuring the Hangfire database exists.");
            }
        }
    }
}
