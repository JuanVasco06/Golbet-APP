. "$PSScriptRoot\Environment.ps1"
$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
if (!(Test-Path $vswhere)) { throw 'No se encontro Visual Studio. Instala Visual Studio con ASP.NET y desarrollo web.' }
$installation = & $vswhere -latest -prerelease -products Microsoft.VisualStudio.Product.Community Microsoft.VisualStudio.Product.Professional Microsoft.VisualStudio.Product.Enterprise -property installationPath
if (!$installation) { throw 'No se encontro Visual Studio Community, Professional o Enterprise.' }
$devenv = Join-Path $installation 'Common7\IDE\devenv.exe'
if (!(Test-Path $devenv)) { throw 'No se encontro el IDE de Visual Studio.' }
# Release the web project's assemblies before Visual Studio builds them.
# Stop-GolBet checks the saved PID and command line before stopping anything.
if (Test-Path (Join-Path $repoRoot 'artifacts\run\app.pid')) {
    & powershell -NoProfile -ExecutionPolicy Bypass -File "$PSScriptRoot\Stop-GolBet.ps1"
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo detener el servidor del lanzador. Revisa el mensaje anterior.' }
}
# A visible IDE is intentional: the user opens this launcher to work in Visual Studio.
Start-Process -FilePath $devenv -ArgumentList ('"' + (Join-Path $repoRoot 'GolBet.sln') + '"')
