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

Deployment / Run as background service
------------------------------------
Two recommended options to run VANewsBot on Windows so it starts automatically after reboot:

1) Task Scheduler (simple)
 - Publish the app: dotnet publish -c Release -o C:\VANewsBot\publish
 - Create run_bot.bat that starts the app (included in this repo)
 - Open Task Scheduler -> Create Task
   * General: Name = VANewsBot, Run whether user is logged on or not
   * Triggers: At startup, Delay task for 30 seconds
   * Actions: Start a program -> Program/script: cmd.exe
     Arguments: /c "C:\VANewsBot\run_bot.bat"
   * Settings: Allow task to be run on demand; restart on failure

2) Install as Windows service (recommended for production) using nssm
 - Download nssm (https://nssm.cc) and extract
 - Run: nssm install VANewsBot
   * Path: C:\Program Files\dotnet\dotnet.exe
   * Arguments: C:\VANewsBot\publish\VANewsBot.dll
   * Startup type: Automatic
 - Start service: nssm start VANewsBot (or use Services.msc)
 - nssm provides options to capture stdout/stderr into files and automatic restarts.

Notes
-----
- Do not commit publish/ folder into source control; it is ignored by .gitignore.
- For Task Scheduler running 'whether user is logged on or not' you will need to provide the account password.
- Keep PollIntervalSeconds in appsettings.json to configure how often the monitor runs (minimum 5 seconds).