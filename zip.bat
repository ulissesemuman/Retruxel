@echo off
set "TEMP_DIR=%TEMP%\backup_cs_temp"
set "ZIP_NAME=ArquivosCSharp.zip"

echo Criando estrutura temporaria...
if exist "%TEMP_DIR%" rd /s /q "%TEMP_DIR%"
mkdir "%TEMP_DIR%"

powershell -Command "Get-ChildItem -Filter '*.cs' -Recurse | ForEach-Object { $dest = Join-Path '%TEMP_DIR%' $_.RelativePath; $null = New-Item -Path (Split-Path $dest) -ItemType Directory -Force; Copy-Item $_.FullName -Destination $dest }"

echo Compactando...
powershell -Command "if (Test-Path '%ZIP_NAME%') { Remove-Item '%ZIP_NAME%' }; Compress-Archive -Path '%TEMP_DIR%\*' -DestinationPath '%ZIP_NAME%'"

echo Limpando arquivos temporarios...
rd /s /q "%TEMP_DIR%"

echo Concluido! O arquivo %ZIP_NAME% foi criado com a estrutura de pastas.