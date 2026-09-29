. "$PSScriptRoot\Environment.ps1"
Set-Location $repoRoot
& $Dotnet test GolBet.sln --logger 'trx;LogFileName=golbet.trx' --results-directory artifacts/test-results
exit $LASTEXITCODE
