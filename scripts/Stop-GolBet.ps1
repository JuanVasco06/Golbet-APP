. "$PSScriptRoot\Environment.ps1"
$pidFile = Join-Path $repoRoot 'artifacts\run\app.pid'
if (!(Test-Path $pidFile)) { Write-Host 'No hay un proceso iniciado por el lanzador.'; exit 0 }
$appProcessId = [int](Get-Content $pidFile)
$info = Get-CimInstance Win32_Process -Filter "ProcessId = $appProcessId"
$expectedDll = Join-Path $repoRoot 'GolBet.Web\bin\Debug\net8.0\GolBet.Web.dll'
if ($info -and $info.ExecutablePath -eq $Dotnet -and $info.CommandLine.Contains($expectedDll)) {
    Stop-Process -Id $appProcessId
    Write-Host 'GolBet detenido.'
} elseif ($info) { throw 'El PID pertenece a otro proceso. No se detuvo.' }
Remove-Item -LiteralPath $pidFile
