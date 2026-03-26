# OralSync API Azure Startup Fix - Enhanced Logging Plan

Status: [IN PROGRESS] 0/7

## Steps:
1. [ ] Create appsettings.Production.json with Azure SQL connection strings.
2. [ ] Update DMD/Program.cs: Add IHostEnvironment, early logging, conn string validation, try-catch per phase, conditional skips, /startup-log endpoint.
3. [ ] Update DMD/Configurations/Database.cs: Granular try-catch + logging in DB functions, prod skips.
4. [ ] Test locally: Run `dotnet run --environment Production`, check console logs.
5. [ ] Publish to Azure via VS or FTP.
6. [ ] Check Azure logs: App Service > Log Stream / Deployment Center > Logs.
7. [ ] Manual DB setup if needed (create Hangfire DB, run migrations via Azure Query Editor).

## Notes:
- After each major step, check for errors.
- Goal: Pinpoint exact startup failure via logs.

Last updated: Now

