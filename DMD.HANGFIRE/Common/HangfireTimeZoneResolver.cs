namespace DMD.HANGFIRE.Common
{
    internal static class HangfireTimeZoneResolver
    {
        private static readonly Dictionary<string, string[]> TimeZoneAliases =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["Asia/Manila"] = new[] { "Asia/Manila", "Singapore Standard Time" },
                ["Singapore Standard Time"] = new[] { "Singapore Standard Time", "Asia/Manila" }
            };

        public static TimeZoneInfo Resolve(string configuredTimeZoneId, string settingName)
        {
            var candidates = TimeZoneAliases.TryGetValue(
                configuredTimeZoneId ?? string.Empty,
                out var aliases)
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
                $"{settingName}:TimeZoneId '{configuredTimeZoneId}' could not be resolved.");
        }
    }
}
