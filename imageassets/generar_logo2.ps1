# Logo CYBERCITY ronda 2: 6 variantes de LG05_Minimal (imageassets/logo/).
# Requiere la variable de entorno WAVESPEED_API_KEY (no la escribas aqui).
$ErrorActionPreference = "Stop"
Set-Location "C:\Users\Angel Perez\Desktop\videojuego\mecha-cyberpunk"
if (-not $env:WAVESPEED_API_KEY) { Write-Error "Falta WAVESPEED_API_KEY"; exit 1 }
if (-not (Test-Path "imageassets/logo/LG05_Minimal.png")) { Write-Error "Falta imageassets/logo/LG05_Minimal.png"; exit 1 }
python imageassets/generar_imagenes.py --lote imageassets/lote_logo2.json
if ($LASTEXITCODE -ne 0) { Write-Error "Fallo el lote. Vuelve a correr (las imagenes que ya existen se omiten)."; exit 1 }
Write-Host "Listo. Revisa imageassets/logo (LG07-LG12) y que todas digan CYBERCITY bien escrito." -ForegroundColor Green
