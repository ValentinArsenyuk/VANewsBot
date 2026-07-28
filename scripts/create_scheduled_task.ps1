<#
Create a scheduled task to run VANewsBot at system startup.

Usage examples:
  # Register task for current user (no password required)
  .\create_scheduled_task.ps1 -PublishPath "C:\VANewsBot\publish" -DelaySeconds 30

  # Register task to run as SYSTEM (no credentials required)
  .\create_scheduled_task.ps1 -PublishPath "C:\VANewsBot\publish" -RunAsSystem

Note: running as SYSTEM uses schtasks.exe and will not require a password, but
the service will run under the SYSTEM account.
#>

param(
	[string]$PublishPath = "C:\VANewsBot\publish",
	[string]$TaskName = "VANewsBot",
	[int]$DelaySeconds = 30,
	[switch]$RunAsSystem
)

function Ensure-PathQuoted([string]$p) {
	if ($p -match ' ' -and $p -notmatch '^".*"$') { return '"' + $p + '"' }
	return $p
}

if (-not (Test-Path $PublishPath)) {
	Write-Host "Publish path '$PublishPath' does not exist. Please publish the app first (dotnet publish -o $PublishPath)" -ForegroundColor Yellow
}

$bat = Join-Path -Path $PublishPath -ChildPath "run_bot.bat"
if (-not (Test-Path $bat)) {
	Write-Host "Warning: run_bot.bat not found in $PublishPath. Ensure run_bot.bat exists or adjust the action" -ForegroundColor Yellow
}

if ($RunAsSystem) {
	$tr = Ensure-PathQuoted "$PublishPath\run_bot.bat"
	$cmd = "schtasks /Create /SC ONSTART /TN `"$TaskName`" /TR $tr /RL HIGHEST /F /RU SYSTEM"
	Write-Host "Creating scheduled task as SYSTEM using schtasks..."
	Write-Host $cmd
	Invoke-Expression $cmd
	if ($LASTEXITCODE -eq 0) { Write-Host "Task '$TaskName' created as SYSTEM." -ForegroundColor Green }
	else { Write-Host "Failed to create task (exit $LASTEXITCODE)." -ForegroundColor Red }
}
else {
	Write-Host "Registering scheduled task for current user (requires to be run with this user)."
	$action = New-ScheduledTaskAction -Execute 'cmd.exe' -Argument "/c `"$PublishPath\run_bot.bat`""
	$trigger = New-ScheduledTaskTrigger -AtStartup -Delay (New-TimeSpan -Seconds $DelaySeconds)
	try {
		Register-ScheduledTask -TaskName $TaskName -Action $action -Trigger $trigger -Description 'Start VANewsBot at startup' -RunLevel Highest -Force
		Write-Host "Task '$TaskName' registered for current user." -ForegroundColor Green
	}
	catch {
		Write-Host "Failed to register scheduled task: $_" -ForegroundColor Red
	}
}
