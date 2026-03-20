namespace DMD.HANGFIRE.AppointmentReminders
{
    internal static class AppointmentReminderTimeZoneResolver
    {
        private static readonly Dictionary<string, string[]> timeZoneAliases =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["Asia/Manila"] = new[] { "Asia/Manila", "Singapore Standard Time" },
                ["Singapore Standard Time"] = new[] { "Singapore Standard Time", "Asia/Manila" }
            };

        public static TimeZoneInfo Resolve(string configuredTimeZoneId)
        {
            var candidates = timeZoneAliases.TryGetValue(configuredTimeZoneId ?? string.Empty, out var aliases)
                ? aliases
                : new[] { configuredTimeZoneId ?? string.Empty };

            foreach (var candidate in candidates.Where(value => !string.IsNullOrWhiteSpace(value)))
            {
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById(candidate);
                }
                catch (TimeZoneNotFoundException)
                {
                }
                catch (InvalidTimeZoneException)
                {
                }
            }

            throw new InvalidOperationException(
                $"AppointmentReminderSettings:TimeZoneId '{configuredTimeZoneId}' could not be resolved.");
        }
    }
}
