<#
Install VANewsBot as a Windows service using nssm (Non-Sucking Service Manager).

Usage:
  .\install_nssm_service.ps1 -NssmPath "C:\tools\nssm\win64\nssm.exe" -DotnetPath "C:\Program Files\dotnet\dotnet.exe" -DllPath "C:\VANewsBot\publish\VANewsBot.dll"

Requirements:
 - Download nssm from https://nssm.cc and set NssmPath to the nssm.exe location.
#>

param(
	[string]$NssmPath = "C:\nssm\nssm.exe",
	[string]$ServiceName = "VANewsBot",
	[string]$DotnetPath = "C:\Program Files\dotnet\dotnet.exe",
	[string]$DllPath = "C:\VANewsBot\publish\VANewsBot.dll",
	[string]$LogFolder = "C:\VANewsBot\logs"
)

if (-not (Test-Path $NssmPath)) {
	Write-Host "nssm not found at $NssmPath. Download nssm from https://nssm.cc and set -NssmPath." -ForegroundColor Red
	exit 1
}

if (-not (Test-Path $DotnetPath)) {
	Write-Host "dotnet not found at $DotnetPath" -ForegroundColor Red
	exit 1
}

if (-not (Test-Path $DllPath)) {
	Write-Host "DLL not found at $DllPath" -ForegroundColor Red
	exit 1
}

if (-not (Test-Path $LogFolder)) { New-Item -ItemType Directory -Path $LogFolder -Force | Out-Null }

$stdout = Join-Path $LogFolder "out.log"
$stderr = Join-Path $LogFolder "err.log"

Write-Host "Installing service '$ServiceName' using nssm at $NssmPath"
& "$NssmPath" install $ServiceName "$DotnetPath" "$DllPath"

# configure stdout/stderr
& "$NssmPath" set $ServiceName AppStdout $stdout
& "$NssmPath" set $ServiceName AppStderr $stderr
& "$NssmPath" set $ServiceName AppRotateFiles 1

Write-Host "Starting service $ServiceName"
& "$NssmPath" start $ServiceName

Write-Host "Service $ServiceName installed and started. Logs: $LogFolder" -ForegroundColor Green
