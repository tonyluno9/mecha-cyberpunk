# Pedido de arte 07-nov (CYBERFIX): look objetivo con HUD_A_minimalista como referencia.
# Requiere la variable de entorno WAVESPEED_API_KEY (no la escribas aqui).
$ErrorActionPreference = "Stop"
Set-Location "C:\Users\Angel Perez\Desktop\videojuego\mecha-cyberpunk"
if (-not $env:WAVESPEED_API_KEY) { Write-Error "Falta WAVESPEED_API_KEY"; exit 1 }
if (-not (Test-Path "imageassets/ui/hud-opciones/HUD_A_minimalista.png")) { Write-Error "Falta HUD_A_minimalista.png"; exit 1 }
python imageassets/generar_imagenes.py --lote imageassets/lote_07nov.json
if ($LASTEXITCODE -ne 0) { Write-Error "Fallo el lote. Vuelve a correr (las imagenes que ya existen se omiten)."; exit 1 }
Write-Host "Listo. Revisa imageassets/referencias/look e imageassets/ui/barras." -ForegroundColor Green
