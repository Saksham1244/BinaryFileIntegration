$serviceName = "ToshibaMachine2DB"
$logDir = "d:\latestBinary\BinaryFileIntegration\ToshibaMacine2Local2DbWinService\bin\Debug\logs"

Write-Host "Checking status of service: $serviceName" -ForegroundColor Cyan
try {
    $service = Get-Service -Name $serviceName -ErrorAction Stop
    Write-Host "Status: $($service.Status)" -ForegroundColor Green
}
catch {
    Write-Host "Service '$serviceName' not found or error retrieving status." -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
}

Write-Host "`nChecking for latest log file in: $logDir" -ForegroundColor Cyan
if (Test-Path $logDir) {
    $latestLog = Get-ChildItem -Path $logDir -Filter "*.log" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    
    if ($latestLog) {
        Write-Host "Latest log file: $($latestLog.FullName)" -ForegroundColor Yellow
        Write-Host "Last Write Time: $($latestLog.LastWriteTime)" -ForegroundColor Yellow
        Write-Host "`n--- Last 20 lines of log ---" -ForegroundColor Gray
        Get-Content $latestLog.FullName -Tail 20
        Write-Host "----------------------------" -ForegroundColor Gray
    }
    else {
        Write-Host "No log files found in directory." -ForegroundColor Red
    }
}
else {
    Write-Host "Log directory not found." -ForegroundColor Red
}

$alarmFile = "D:\Project\download\AlarmText.txt"
Write-Host "`nChecking for AlarmText.txt..." -ForegroundColor Cyan
if (Test-Path $alarmFile) {
    Write-Host "AlarmText.txt exists." -ForegroundColor Green
}
else {
    Write-Host "AlarmText.txt is MISSING." -ForegroundColor Red
}

Write-Host "`nDone." -ForegroundColor Cyan
