$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
Set-Location $repoRoot
New-Item -ItemType Directory -Force '.tools' | Out-Null
$installer = Join-Path $repoRoot '.tools\dotnet-install.ps1'
if (!(Test-Path '.tools\dotnet\dotnet.exe')) {
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    Invoke-WebRequest 'https://dot.net/v1/dotnet-install.ps1' -OutFile $installer -UseBasicParsing
    & $installer -Channel 8.0 -InstallDir (Join-Path $repoRoot '.tools\dotnet') -NoPath
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo descargar el SDK de .NET 8.' }
}
. "$PSScriptRoot\Environment.ps1"
& $Dotnet restore GolBet.sln
if ($LASTEXITCODE -ne 0) { throw 'No se pudieron restaurar las dependencias.' }
Write-Host 'Entorno listo. Ejecuta Iniciar-GolBet.cmd.' -ForegroundColor Green
Write-Host 'Se requiere SQL Server LocalDB; viene con la carga de trabajo ASP.NET y desarrollo web de Visual Studio.'
