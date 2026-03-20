# Semaphore SMS Setup

The API and the Hangfire worker both load SMS settings from the `SemaphoreSmsSettings` section.

Example configuration:

```json
"SemaphoreSmsSettings": {
  "IsEnabled": true,
  "ApiKey": "your-semaphore-api-key",
  "BaseUrl": "https://api.semaphore.co/api/v4/",
  "MessagesEndpoint": "messages",
  "PriorityEndpoint": "priority",
  "TimeoutMilliseconds": 30000
}
```

Environment variable example:

```powershell
$env:SemaphoreSmsSettings__IsEnabled = "true"
$env:SemaphoreSmsSettings__ApiKey = "your-semaphore-api-key"
$env:SemaphoreSmsSettings__BaseUrl = "https://api.semaphore.co/api/v4/"
$env:SemaphoreSmsSettings__MessagesEndpoint = "messages"
$env:SemaphoreSmsSettings__PriorityEndpoint = "priority"
$env:SemaphoreSmsSettings__TimeoutMilliseconds = "30000"
```

Notes:

- `IsEnabled` must be `true` before the service can send messages.
- Recipient numbers are normalized to Semaphore's Philippine format (`639XXXXXXXXX`).
- Priority SMS uses the `/priority` route and regular SMS uses `/messages`.
- Sender name is optional. If omitted per request, the API uses the patient's clinic name as the default sender in the Semaphore payload.
