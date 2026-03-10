using DMD.DOMAIN.Entities.UserProfile;
using DMD.DOMAIN.Enums;
using DMD.PERSISTENCE.Context;
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
                    var context = services.GetRequiredService<DmdDbContext>();
                    var userManager = services.GetRequiredService<UserManager<UserProfile>>();
                    var configuration = services.GetRequiredService<IConfiguration>();
                    var logger = services.GetRequiredService<ILogger<Program>>();

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
                var context = services.GetRequiredService<DmdDbContext>();

                await context.Database.MigrateAsync();
            }
            catch (Exception ex)
            {
                var logger = services.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "An error occurred during migration.");
            }
        }
    }
}
