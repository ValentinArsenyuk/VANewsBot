@echo off
REM run from repository root, this script will start the bot from publish folder
cd /d "%~dp0publish"
if exist VANewsBot.exe (
	VANewsBot.exe
) else (
	dotnet VANewsBot.dll
)
pause
