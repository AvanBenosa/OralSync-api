# SMTP Setup

The API and the Hangfire worker both load SMTP settings from the `EmailSettings` section.

For Gmail, a normal account password will not work with `System.Net.Mail.SmtpClient`. You must use a Gmail app password.

## Required settings

```json
"EmailSettings": {
  "Host": "smtp.gmail.com",
  "Port": 587,
  "UserName": "your-email@gmail.com",
  "Password": "your-16-char-app-password",
  "FromEmail": "your-email@gmail.com",
  "FromName": "DMD Clinic",
  "EnableSsl": true,
  "UseDefaultCredentials": false,
  "TimeoutMilliseconds": 100000
}
```

## PowerShell environment variables

Use environment variables so credentials are not committed to `appsettings.json`.

```powershell
$env:EmailSettings__Host = "smtp.gmail.com"
$env:EmailSettings__Port = "587"
$env:EmailSettings__UserName = "your-email@gmail.com"
$env:EmailSettings__Password = "your-16-char-app-password"
$env:EmailSettings__FromEmail = "your-email@gmail.com"
$env:EmailSettings__FromName = "DMD Clinic"
$env:EmailSettings__EnableSsl = "true"
$env:EmailSettings__UseDefaultCredentials = "false"
$env:EmailSettings__TimeoutMilliseconds = "100000"
```

Set the same variables before starting both:

- `DMD`
- `DMD.HANGFIRE`

## Gmail account steps

1. Turn on 2-Step Verification for the Gmail account.
2. Create an App Password for `Mail`.
3. Use that app password as `EmailSettings__Password`.
4. Keep `Host = smtp.gmail.com`, `Port = 587`, and `EnableSsl = true`.

## Common failure

If you see `5.7.0 Authentication Required`, the mailbox rejected the credentials. For Gmail this usually means the password is not an app password, 2-Step Verification is not enabled, or the `FromEmail` does not match the authenticated mailbox.
