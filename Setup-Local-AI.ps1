$ErrorActionPreference = 'Stop'
$taskRoot = $PSScriptRoot
$taskLocal = Join-Path $taskRoot '.local-ai'
$taskRuntime = Join-Path $taskLocal 'runtime'
New-Item -ItemType Directory -Path $taskRuntime -Force | Out-Null
$taskOllama = Join-Path $taskRuntime 'ollama.exe'
if (!(Test-Path -LiteralPath $taskOllama)) {
    $taskZip = Join-Path $taskLocal 'ollama-windows-amd64.zip'
    Write-Host 'Downloading Ollama from the official GitHub release...'
    & curl.exe --fail --location --retry 3 --output $taskZip 'https://github.com/ollama/ollama/releases/latest/download/ollama-windows-amd64.zip'
    if ($LASTEXITCODE -ne 0) { throw 'Ollama download failed. Run this script again to retry.' }
    Expand-Archive -LiteralPath $taskZip -DestinationPath $taskRuntime -Force
}
$env:OLLAMA_MODELS = Join-Path $taskLocal 'models'
$env:OLLAMA_HOST = '127.0.0.1:11434'
try { $null = Invoke-RestMethod 'http://127.0.0.1:11434/api/tags' -TimeoutSec 3 }
catch {
    Start-Process -FilePath $taskOllama -ArgumentList 'serve' -WindowStyle Hidden -RedirectStandardOutput (Join-Path $taskLocal 'server.log') -RedirectStandardError (Join-Path $taskLocal 'server-error.log')
    $taskReady = $false
    for ($taskAttempt = 0; $taskAttempt -lt 30; $taskAttempt++) {
        Start-Sleep -Seconds 1
        try { $null = Invoke-RestMethod 'http://127.0.0.1:11434/api/tags' -TimeoutSec 2; $taskReady = $true; break } catch { }
    }
    if (!$taskReady) { throw 'Ollama did not start. See .local-ai/server-error.log.' }
}
Write-Host 'Downloading the local model (qwen3:1.7b, about 1.4 GB)...'
& $taskOllama pull 'qwen3:1.7b'
if ($LASTEXITCODE -ne 0) { throw 'Model download failed. Run this script again to retry.' }
# Disable reasoning to keep responses short and suitable for the existing adapter.
$taskModelFile = Join-Path $taskLocal 'Modelfile'
@'
FROM qwen3:1.7b
PARAMETER num_ctx 4096
PARAMETER temperature 0.4
SYSTEM "Tra loi bang tieng Viet. /no_think"
'@ | Set-Content -LiteralPath $taskModelFile -Encoding utf8
& $taskOllama create 'rental-local' -f $taskModelFile
if ($LASTEXITCODE -ne 0) { throw 'Failed to create the rental model.' }
Write-Host 'Testing the Chat Completions endpoint...'
$taskBody = @{model='rental-local';messages=@(@{role='user';content='Chi tra loi: OK /no_think'});stream=$false} | ConvertTo-Json -Depth 5
$taskTest = Invoke-RestMethod 'http://127.0.0.1:11434/v1/chat/completions' -Method Post -ContentType 'application/json' -Body $taskBody -TimeoutSec 180
if ([string]::IsNullOrWhiteSpace($taskTest.choices[0].message.content)) { throw 'The local model returned an empty response.' }
$taskConfig = Join-Path $taskRoot 'API/Hinet.Api/appsettings.json'
$taskText = [IO.File]::ReadAllText($taskConfig)
$taskText = [regex]::Replace($taskText, '"AiMode"\s*:\s*"[^"]*"', '"AiMode": "local"')
$taskText = [regex]::Replace($taskText, '"AiBaseUrl"\s*:\s*"[^"]*"', '"AiBaseUrl": "http://127.0.0.1:11434/v1/"')
$taskText = [regex]::Replace($taskText, '"AiModel"\s*:\s*"[^"]*"', '"AiModel": "rental-local"')
$taskText = [regex]::Replace($taskText, '"AiApiKey"\s*:\s*"[^"]*"', '"AiApiKey": ""')
[IO.File]::WriteAllText($taskConfig, $taskText)
Write-Host 'Local AI is ready. Restart the API to apply the configuration.' -ForegroundColor Green
Write-Host 'Run this script again after restarting Windows to start Ollama.'
