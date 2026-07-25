VANewsBot
=========

Lightweight Telegram bot that aggregates news from multiple RSS feeds and selected Telegram channels, scores risk indicators, and sends alerts to subscribers.

Prerequisites
-------------
- .NET 10 SDK
- Telegram bot token (create via BotFather)

Quick start
-----------
1. Copy configuration template:
   - cp appsettings.example.json appsettings.json
   - Fill in Telegram:Token and Telegram:ChatId (admin chat) and add RSS/Telegram sources under RiskMonitoring.

2. Build and run:
   - dotnet build
   - dotnet run
   - Or open VANewsBot.slnx in Visual Studio and run.

Bot commands
------------
- /start — returns your ChatId.
- /risk — request current risk; also subscribes the caller for alerts.
- /unsubscribe — remove yourself from subscribers.
- /subscribers — show subscriber display names (for admins / for debugging).
- /setphone <number> — save your phone number to the subscriber record.

Subscriber storage
------------------
- Subscriber data (username, first/last name, phone, language, chat type, subscribedAt) are stored in Data/subscribers.txt under the application's runtime folder.
- The Data/ folder is ignored by git (.gitignore) to avoid committing personal data.

Privacy & notes
---------------
- Phone numbers are captured only if the user provides them via /setphone or by sharing contact explicitly.
- If you need persistence in the repository (not recommended for PII), change storage path in Services/SubscriberStore.cs.

Development notes
-----------------
- News and Telegram sources are configured in appsettings.json under RiskMonitoring.
- NewsRiskService scores text by keywords (English/Hebrew/Russian) and aggregates results.
- Consider using a persistent data store for subscribers if you want durability across deployments.

Contributing
------------
PRs welcome. Run tests (if any) and follow existing code style.

License
-------
See repository license (if any).