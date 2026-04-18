$zipFile = "$pwd\ArquivosCSharp.zip"
$tempDir = Join-Path $env:TEMP "export_cs_$(Get-Random)"

# 1. Busca os arquivos ignorando bin e obj
# O -Exclude não funciona bem com -Recurse para pastas, por isso usamos o Where-Object
$arquivos = Get-ChildItem -Path ".\" -Include "*.cs", "*.xaml", "*.json", "*.c.rtrx" -Recurse | Where-Object { 
    $_.FullName -notmatch '\\bin\\' -and $_.FullName -notmatch '\\obj\\' 
}

if ($arquivos.Count -eq 0) {
    Write-Host "Nenhum arquivo .cs encontrado (fora das pastas bin/obj)." -ForegroundColor Yellow
    exit
}

# 2. Copia mantendo a estrutura
foreach ($arq in $arquivos) {
    $relPath = Resolve-Path $arq.FullName -Relative
    $destFile = Join-Path $tempDir $relPath
    $destFolder = Split-Path $destFile
    
    if (!(Test-Path $destFolder)) { New-Item -ItemType Directory -Path $destFolder -Force | Out-Null }
    Copy-Item $arq.FullName -Destination $destFile -Force
}

# 3. Compacta usando .NET
if (Test-Path $zipFile) { Remove-Item $zipFile -Force }
Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::CreateFromDirectory($tempDir, $zipFile)

# 4. Limpa a sujeira
Remove-Item $tempDir -Recurse -Force

Write-Host "Sucesso! O ZIP foi criado sem as pastas bin e obj." -ForegroundColor Green
