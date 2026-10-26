param([string]$OutputName = 'NewYearTree.exe')

$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'

if (-not (Test-Path -LiteralPath $compiler)) {
    throw '.NET Framework 4.x C# compiler was not found.'
}

$outputDirectory = Join-Path $PSScriptRoot 'dist'
New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
$outputPath = Join-Path $outputDirectory $OutputName

& $compiler /nologo /target:winexe /optimize+ "/out:$outputPath" /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.Xml.dll "/resource:$PSScriptRoot\assets\tree.png,tree.png" "/resource:$PSScriptRoot\assets\ornaments.png,ornaments.png" "/resource:$PSScriptRoot\assets\winter-base.png,winter-base.png" "$PSScriptRoot\src\NewYearTree.cs"

if ($LASTEXITCODE -ne 0) {
    throw 'Build failed.'
}

Write-Output $outputPath
