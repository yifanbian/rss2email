# RSS to Email with Azure Durable Functions

A open-source Serverless tool for getting news from RSS feeds in email.

## Usage

1.  Deploy `rss2email` to Azure Functions
2.  In Azure Portal, navigate to your Azure Functions, go to **Settings** - **Configuration**, and add your configuration there.
    You may create a fork and maintain your `appsettings.json` yourself.
3.  Time Trigger should automatically start a RSS to Email workflow on Azure Functions start-up and periodically after start-up,
    but you may use HTTP Trigger to start a RSS to Email workflow manually.

## Configuration

Here is a example of `appsettings.json`.

```json
{
  "RssToEmail": {
    "Subscriptions": [
      {
        "Name": "Test",
        "Recipient": "test@example.com",
        "Feeds": [
          {
            "Name": "Test Feed",
            "FeedUrl": "https://rss.example.com/feed.xml"
          }
        ]
      }
    ]
  },
  "Email": {
    "Type": "Smtp",
    "Config": {
      "Host": "smtp.example.com",
      "Port": 465,
      "EnableSsl": true,
      "From": "rss2email@example.com",
      "Username": "rss2email@example.com",
      "Password": "?"
    }
  }
}
```

`Email:Type` can be `Smtp` or `MicrosoftGraph`.
If `Email:Type` is `MicrosoftGraph`, `Email:Config` should be like this:

```json
{
  "TenantId": "00000000-0000-0000-0000-000000000000",
  "ClientId": "00000000-0000-0000-0000-000000000000",
  "ClientSecret": "?",
  "From": "rss2email@example.com"
}
```

To provide configuration with environment variables (such as on Azure Portal),
check out the **Environment variables** section in
[Configuration in ASP.NET Core](https://docs.microsoft.com/aspnet/core/fundamentals/configuration/).

## License

Licensed under [MIT License](LICENSE).
