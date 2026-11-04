param(
    [Parameter(Mandatory = $true)][string]$InnoCompiler,
    [string]$Dotnet = 'dotnet',
    [string]$OutputDirectory = (Join-Path $PSScriptRoot '..\dist')
)
$ErrorActionPreference = 'Stop'
$sourceRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$outputRoot = [IO.Path]::GetFullPath($OutputDirectory)
$payload = Join-Path $outputRoot 'enestv-win-x64'
New-Item -ItemType Directory -Path $outputRoot -Force | Out-Null
# Fresh staging avoids stale payload files; never deletes a user-selected directory.
$payload = Join-Path $outputRoot ('enestv-win-x64-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $payload -ErrorAction Stop | Out-Null
& $Dotnet publish (Join-Path $sourceRoot 'EnesTV.App') -c Release -r win-x64 --self-contained true -o $payload -p:DebugType=None -p:DebugSymbols=false
if ($LASTEXITCODE -ne 0) { throw 'Application publish failed.' }
Copy-Item (Join-Path $sourceRoot 'README.md'), (Join-Path $sourceRoot 'README.tr.md'), (Join-Path $sourceRoot 'LICENSE'), (Join-Path $sourceRoot 'THIRD-PARTY-NOTICES.md'), (Join-Path $sourceRoot 'SOURCE-ACCESS.md') $payload -Force
Copy-Item (Join-Path $sourceRoot 'licenses') (Join-Path $payload 'licenses') -Recurse -Force
& $InnoCompiler "/DPayloadDir=$payload" "/DOutputPath=$outputRoot" (Join-Path $PSScriptRoot 'enestv.iss')
if ($LASTEXITCODE -ne 0) { throw 'Setup compilation failed.' }
