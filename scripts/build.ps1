param([switch]$Publish)
$ErrorActionPreference='Stop'
$taskRoot=Split-Path $PSScriptRoot -Parent
Push-Location $taskRoot
try {
    dotnet restore AltF4.Cinema.sln
    if($LASTEXITCODE -ne 0){throw 'Restore failed'}
    dotnet build AltF4.Cinema.sln -c Release --no-restore
    if($LASTEXITCODE -ne 0){throw 'Build failed'}
    dotnet test tests/AltF4.Cinema.Tests -c Release --no-build --logger 'trx;LogFileName=business-tests.trx' --results-directory docs/test-results
    if($LASTEXITCODE -ne 0){throw 'Tests failed'}
    if($Publish){
        dotnet publish src/AltF4.Cinema.WinForms -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o deliverables/portable
        if($LASTEXITCODE -ne 0){throw 'Publish failed'}
    }
}finally{Pop-Location}
