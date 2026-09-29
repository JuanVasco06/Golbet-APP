$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$localDotnet = Join-Path $repoRoot '.tools\dotnet\dotnet.exe'
if (Test-Path $localDotnet) {
    $script:Dotnet = $localDotnet
    $env:DOTNET_ROOT = Split-Path $localDotnet
    $env:DOTNET_MSBUILD_SDK_RESOLVER_CLI_DIR = $env:DOTNET_ROOT
    $env:PATH = "$env:DOTNET_ROOT;$env:PATH"
} else {
    $installedDotnet = Join-Path $env:ProgramFiles 'dotnet\dotnet.exe'
    if (!(Test-Path $installedDotnet)) { throw 'Falta .NET. Ejecuta Preparar-Entorno.cmd.' }
    $script:Dotnet = $installedDotnet
    if (!((& $script:Dotnet --list-runtimes) -match 'Microsoft.AspNetCore.App 8\.')) {
        throw 'Falta el runtime de ASP.NET Core 8. Ejecuta Preparar-Entorno.cmd.'
    }
}
$env:DOTNET_CLI_HOME = Join-Path $repoRoot '.tools\cli'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:MSBuildEnableWorkloadResolver = 'false'
