# Pedido de arte 07-nov b (CYBERFIX): HUD final, esquina, calle Nova y mapa corregido.
# Fase 1: H10, K05, M11. Fase 2: K06 (usa H10_HUD.png como referencia del HUD).
# Requiere la variable de entorno WAVESPEED_API_KEY (no la escribas aqui).
$ErrorActionPreference = "Stop"
Set-Location "C:\Users\Angel Perez\Desktop\videojuego\mecha-cyberpunk"
if (-not $env:WAVESPEED_API_KEY) { Write-Error "Falta WAVESPEED_API_KEY"; exit 1 }
$look = "imageassets/referencias/look"
foreach ($r in "K01_Barrio_HUDnuevo.png","M10_MapaCiudad.png") {
  if (-not (Test-Path "$look/$r")) { Write-Error "Falta $look/$r"; exit 1 }
}
$lote = "imageassets/lote_07nov_b.json"
Write-Host "=== Fase 1: H10, K05, M11 ===" -ForegroundColor Cyan
python imageassets/generar_imagenes.py --lote $lote --fase 1
if ($LASTEXITCODE -ne 0) { Write-Error "Fallo la fase 1. Vuelve a correr (las imagenes que ya existen se omiten)."; exit 1 }
if (-not (Test-Path "$look/H10_HUD.png")) { Write-Error "No se genero H10_HUD.png; K06 lo necesita."; exit 1 }
Write-Host "=== Fase 2: K06 (con el HUD de H10) ===" -ForegroundColor Cyan
python imageassets/generar_imagenes.py --lote $lote --fase 2
if ($LASTEXITCODE -ne 0) { Write-Error "Fallo la fase 2. Vuelve a correr."; exit 1 }
Write-Host "Listo. Revisa imageassets/referencias/look." -ForegroundColor Green
