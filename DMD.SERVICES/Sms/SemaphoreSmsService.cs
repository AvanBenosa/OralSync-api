using System.Text.Json;
using DMD.SERVICES.Sms.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DMD.SERVICES.Sms
{
    public class SemaphoreSmsService : ISmsService
    {
        private static readonly JsonSerializerOptions serializerOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly HttpClient httpClient;
        private readonly SemaphoreSmsSettings smsSettings;
        private readonly ILogger<SemaphoreSmsService> logger;

        public SemaphoreSmsService(
            HttpClient httpClient,
            IOptions<SemaphoreSmsSettings> smsSettings,
            ILogger<SemaphoreSmsService> logger)
        {
            this.httpClient = httpClient;
            this.smsSettings = smsSettings.Value;
            this.logger = logger;
        }

        public async Task<IReadOnlyList<SemaphoreSmsMessageResponse>> SendAsync(
            PatientSmsJobRequest request,
            CancellationToken cancellationToken = default)
        {
            ValidateSettings();

            if (string.IsNullOrWhiteSpace(request.RecipientNumber))
            {
                throw new InvalidOperationException("RecipientNumber is required.");
            }

            if (string.IsNullOrWhiteSpace(request.Message))
            {
                throw new InvalidOperationException("Message is required.");
            }

            var endpoint = request.UsePriority
                ? smsSettings.PriorityEndpoint
                : smsSettings.MessagesEndpoint;

            var senderName = request.SenderName?.Trim() ?? string.Empty;

            var payload = new Dictionary<string, string>
            {
                ["apikey"] = smsSettings.ApiKey,
                ["number"] = NormalizeNumbers(request.RecipientNumber),
                ["message"] = request.Message.Trim()
            };

            if (!string.IsNullOrWhiteSpace(senderName))
            {
                payload["sendername"] = senderName;
            }

            using var content = new FormUrlEncodedContent(payload);
            using var response = await httpClient.PostAsync(endpoint, content, cancellationToken);

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogError(
                    "Semaphore SMS request failed. StatusCode: {StatusCode}, Response: {ResponseBody}",
                    (int)response.StatusCode,
                    responseBody);

                throw new InvalidOperationException(
                    $"Semaphore SMS request failed with status {(int)response.StatusCode}: {responseBody}");
            }

            var result = JsonSerializer.Deserialize<List<SemaphoreSmsMessageResponse>>(
                             responseBody,
                             serializerOptions)
                         ?? new List<SemaphoreSmsMessageResponse>();

            logger.LogInformation(
                "Semaphore SMS request accepted for {RecipientNumber}. MessageCount: {MessageCount}, Priority: {UsePriority}",
                request.RecipientNumber,
                result.Count,
                request.UsePriority);

            return result;
        }

        private void ValidateSettings()
        {
            if (!smsSettings.IsEnabled)
            {
                throw new InvalidOperationException("SemaphoreSmsSettings:IsEnabled is false.");
            }

            if (string.IsNullOrWhiteSpace(smsSettings.ApiKey))
            {
                throw new InvalidOperationException("SemaphoreSmsSettings:ApiKey is not configured.");
            }

            if (string.IsNullOrWhiteSpace(smsSettings.BaseUrl))
            {
                throw new InvalidOperationException("SemaphoreSmsSettings:BaseUrl is not configured.");
            }

            if (smsSettings.TimeoutMilliseconds <= 0)
            {
                throw new InvalidOperationException("SemaphoreSmsSettings:TimeoutMilliseconds must be greater than zero.");
            }

            if (string.IsNullOrWhiteSpace(smsSettings.MessagesEndpoint))
            {
                throw new InvalidOperationException("SemaphoreSmsSettings:MessagesEndpoint is not configured.");
            }

            if (string.IsNullOrWhiteSpace(smsSettings.PriorityEndpoint))
            {
                throw new InvalidOperationException("SemaphoreSmsSettings:PriorityEndpoint is not configured.");
            }
        }

        private static string NormalizeNumbers(string recipientNumbers)
        {
            var normalizedNumbers = recipientNumbers
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(NormalizeSingleNumber)
                .ToArray();

            if (normalizedNumbers.Length == 0)
            {
                throw new InvalidOperationException("At least one valid recipient number is required.");
            }

            return string.Join(",", normalizedNumbers);
        }

        private static string NormalizeSingleNumber(string value)
        {
            var digits = new string(value.Where(char.IsDigit).ToArray());
            if (string.IsNullOrWhiteSpace(digits))
            {
                throw new InvalidOperationException("Recipient number contains no digits.");
            }

            if (digits.StartsWith("09") && digits.Length == 11)
            {
                return $"63{digits[1..]}";
            }

            if (digits.StartsWith("9") && digits.Length == 10)
            {
                return $"63{digits}";
            }

            if (digits.StartsWith("639") && digits.Length == 12)
            {
                return digits;
            }

            throw new InvalidOperationException(
                $"Recipient number '{value}' is not a supported Philippine mobile number.");
        }
    }
}
