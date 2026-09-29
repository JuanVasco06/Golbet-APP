param([switch]$NoBrowser)
. "$PSScriptRoot\Environment.ps1"
Set-Location $repoRoot
$url = 'http://localhost:5229'
$existing = $null
try { $existing = Invoke-WebRequest "$url/" -UseBasicParsing -TimeoutSec 2 } catch { }
if ($existing) {
    if ($existing.Content -notlike '*GolBet*') { throw 'El puerto 5229 esta ocupado por otra aplicacion.' }
    Write-Host "GolBet ya esta ejecutandose: $url"
    if (!$NoBrowser) { Start-Process $url }
    exit 0
}
& $Dotnet build GolBet.Web/GolBet.Web.csproj --nologo
if ($LASTEXITCODE -ne 0) { throw 'La compilacion fallo. Revisa los mensajes anteriores.' }
$logDir = Join-Path $repoRoot 'artifacts\run'
New-Item -ItemType Directory -Force $logDir | Out-Null
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:ASPNETCORE_URLS = $url
$appDll = Join-Path $repoRoot 'GolBet.Web\bin\Debug\net8.0\GolBet.Web.dll'
$process = Start-Process -FilePath $Dotnet -ArgumentList ('"' + $appDll + '"') -WorkingDirectory (Join-Path $repoRoot 'GolBet.Web') -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $logDir 'stdout.log') -RedirectStandardError (Join-Path $logDir 'stderr.log')
$process.Id | Set-Content (Join-Path $logDir 'app.pid')
for ($attempt = 0; $attempt -lt 45; $attempt++) {
    if ($process.HasExited) { Get-Content (Join-Path $logDir 'stderr.log'); throw 'No se pudo iniciar GolBet. Revisa SQL Server y artifacts/run.' }
    try {
        $response = Invoke-WebRequest "$url/" -UseBasicParsing -TimeoutSec 2
        if ($response.StatusCode -eq 200) {
            Write-Host "GolBet esta listo: $url" -ForegroundColor Green
            Write-Host 'Para detenerlo, ejecuta Detener-GolBet.cmd.'
            if (!$NoBrowser) { Start-Process $url }
            exit 0
        }
    } catch { Start-Sleep -Milliseconds 500 }
}
throw 'El servidor tarda en iniciar. Consulta artifacts/run/stdout.log y stderr.log.'
