$env:GREENRETAIL_DEV_SEED = "true"
Write-Host "GreenRetail test recovery mode enabled." -ForegroundColor Yellow
Write-Host "Known test owner credentials: owner / Owner!123" -ForegroundColor Cyan
Write-Host "Start the application from this PowerShell window so the recovery button is available." -ForegroundColor Gray
Write-Host "Command: dotnet run -f net10.0-windows10.0.19041.0" -ForegroundColor Gray
