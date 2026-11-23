param([int]$Port = 5112, [switch]$Rebuild, [switch]$NoBrowser)
$ErrorActionPreference = 'Stop'
$taskRoot = $PSScriptRoot
$taskOutput = Join-Path $taskRoot 'dist\AI_TOOL'
$taskApp = Join-Path $taskOutput 'Hinet.Api.dll'
$taskData = Join-Path $taskRoot 'App_Data\rental-desktop'
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw 'Can cai .NET 8 SDK: https://dotnet.microsoft.com/download/dotnet/8.0' }
New-Item -ItemType Directory -Path $taskData -Force | Out-Null
if ($Rebuild -or -not (Test-Path -LiteralPath $taskApp)) {
    Write-Host 'Dang bien dich AI TOOL. Khong can npm.'
    $taskAppData = $env:APPDATA
    $taskCliHome = $env:DOTNET_CLI_HOME
    try {
        # Keep CLI/NuGet configuration inside this project; do not require changes to user settings.
        $env:APPDATA = Join-Path $taskRoot 'App_Data\cli\roaming'
        $env:DOTNET_CLI_HOME = Join-Path $taskRoot 'App_Data\cli'
        dotnet restore (Join-Path $taskRoot 'API\Hinet.Api\Hinet.Api.csproj') --configfile (Join-Path $taskRoot 'NuGet.Rental.Config') --ignore-failed-sources -p:NuGetAudit=false -v quiet
        if ($LASTEXITCODE -ne 0) { throw 'Khong khoi phuc duoc goi NuGet.' }
        dotnet publish (Join-Path $taskRoot 'API\Hinet.Api\Hinet.Api.csproj') -c Release -o $taskOutput --no-restore -v quiet
        if ($LASTEXITCODE -ne 0) { throw 'Bien dich that bai. Kiem tra .NET SDK va ket noi tai goi NuGet.' }
        # The desktop bundle does not need the original API's database credentials or JWT secrets.
        Set-Content -LiteralPath (Join-Path $taskOutput 'appsettings.json') -Value '{"Logging":{"LogLevel":{"Default":"Information","Microsoft.AspNetCore":"Warning"}}}' -Encoding UTF8
    } finally { $env:APPDATA = $taskAppData; $env:DOTNET_CLI_HOME = $taskCliHome }
}
$taskBrowserScript = Join-Path $taskOutput 'playwright.ps1'
$taskBrowserRoot = if ($env:PLAYWRIGHT_BROWSERS_PATH) { $env:PLAYWRIGHT_BROWSERS_PATH } else { Join-Path $env:LOCALAPPDATA 'ms-playwright' }
$taskChromium = Join-Path $taskBrowserRoot 'chromium-1208'
if (-not (Test-Path -LiteralPath $taskChromium)) {
    Write-Host 'Dang cai Chromium cho Facebook (chi lan dau).'
    & $taskBrowserScript install chromium
    if ($LASTEXITCODE -ne 0) { Write-Warning 'Chua cai duoc Chromium. Bang quan ly van chay; cai lai de dung Facebook.' }
}
Write-Host "AI TOOL: http://127.0.0.1:$Port/rental-desktop/"
Write-Host "Du lieu: $taskData"
Write-Host 'Nhan Ctrl+C de tat ung dung. Khong dong cua so nay khi muon bot hoat dong.'
$taskArguments = @($taskApp, '--rental-desktop', '--rental-port', "$Port", '--rental-data', $taskData)
if (-not $NoBrowser) { $taskArguments += '--open' }
dotnet @taskArguments
if ($LASTEXITCODE -ne 0) { throw 'Ung dung da dung. Neu da mo truoc do, dung cua so dang chay hoac chon cong khac.' }
