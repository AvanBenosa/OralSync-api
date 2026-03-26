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
            using var scope = app.Services.CreateScope();
            var services = scope.ServiceProvider;

            var logger = services.GetRequiredService<ILogger<Program>>();

            try
            {
                logger.LogInformation("🔹 Resolving UserManager...");
                var userManager = services.GetRequiredService<UserManager<UserProfile>>();

                logger.LogInformation("🔹 Resolving Configuration...");
                var configuration = services.GetRequiredService<IConfiguration>();

                logger.LogInformation("🔹 Resolving DbContext...");
                var context = services.GetRequiredService<DmdDbContext>();

                logger.LogInformation("🔹 Applying migrations...");
                await context.Database.MigrateAsync(); // ✅ better than EnsureCreated

                logger.LogInformation("🔹 Starting seed process...");
                await SeedDatabaseInternal(userManager, configuration, logger);

                logger.LogInformation("✅ Seed completed successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ Error during seeding: {Message}", ex.Message);

                if (ex.InnerException != null)
                {
                    logger.LogError("❌ Inner Exception: {InnerMessage}", ex.InnerException.Message);
                }

                throw; // 🔥 VERY IMPORTANT (so you see actual error in dev)
            }
        }

        private static async Task SeedDatabaseInternal(
            UserManager<UserProfile> userManager,
            IConfiguration configuration,
            ILogger logger)
        {
            // ✅ safer fallback email
            var adminEmail = configuration["Seed:Admin:Email"] ?? "admin@oralsync.local";
            var adminPassword = configuration["Seed:Admin:Password"] ?? "Xdrx3991*?";

            logger.LogInformation("🔹 Checking admin user: {Email}", adminEmail);

            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            adminUser ??= await userManager.FindByNameAsync(adminEmail);

            if (adminUser == null)
            {
                logger.LogInformation("🔹 Admin not found. Creating new admin...");

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
                    logger.LogInformation("✅ Admin user created successfully: {Email}", adminEmail);
                }
                else
                {
                    logger.LogError(
                        "❌ Failed to create admin user {Email}. Errors: {Errors}",
                        adminEmail,
                        string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                logger.LogInformation("🔹 Admin already exists. Checking for updates...");

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
                    logger.LogInformation("🔹 Updating existing admin user...");

                    var updateResult = await userManager.UpdateAsync(adminUser);

                    if (!updateResult.Succeeded)
                    {
                        logger.LogError(
                            "❌ Failed to update admin user {Email}. Errors: {Errors}",
                            adminEmail,
                            string.Join(", ", updateResult.Errors.Select(e => e.Description)));
                    }
                    else
                    {
                        logger.LogInformation("✅ Admin user updated successfully.");
                    }
                }
                else
                {
                    logger.LogInformation("✅ Admin user is already up-to-date.");
                }
            }
        }

internal static async Task ConfigureDatabase(IServiceProvider services)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("🔄 ConfigureDatabase started.");

            try
            {
                await EnsureHangfireDatabaseAsync(services);

                var context = services.GetRequiredService<DmdDbContext>();
                logger.LogInformation("✅ DbContext resolved.");

                await context.Database.MigrateAsync();
                logger.LogInformation("✅ Migrations applied.");

                await EnsureClinicIdIsNullableAsync(context);
                logger.LogInformation("✅ ClinicId nullable fix applied.");
            }
            catch (SqlException sqlEx)
            {
                logger.LogError(sqlEx, "💥 SqlException in ConfigureDatabase: Code={Number}, Message={Message}", sqlEx.Number, sqlEx.Message);
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "💥 Unexpected error in ConfigureDatabase");
                throw;
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
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("🔄 EnsureHangfireDatabaseAsync started.");

            try
            {
                var configuration = services.GetRequiredService<IConfiguration>();
                var hangfireConnectionString = configuration.GetConnectionString("Hangfire");

                if (string.IsNullOrWhiteSpace(hangfireConnectionString))
                {
                    logger.LogInformation("Hangfire connection string is not configured. Skipping.");
                    return;
                }

                var hangfireBuilder = new SqlConnectionStringBuilder(hangfireConnectionString);
                var hangfireDatabaseName = hangfireBuilder.InitialCatalog;

                if (string.IsNullOrWhiteSpace(hangfireDatabaseName))
                {
                    logger.LogWarning("Hangfire Initial Catalog missing. Skipping.");
                    return;
                }

                logger.LogWarning("⚠️ Attempting CREATE DATABASE on master (may fail in Azure SQL - normal in prod).");

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
                logger.LogInformation("✅ Hangfire DB ensured: {DatabaseName}", hangfireDatabaseName);
            }
            catch (SqlException sqlEx)
            {
                logger.LogWarning(sqlEx, "⚠️ SqlException in EnsureHangfireDatabaseAsync (likely permissions): Code={Number}, Message={Message}", 
                    sqlEx.Number, sqlEx.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "💥 Unexpected error in EnsureHangfireDatabaseAsync");
            }
        }
    }
}
