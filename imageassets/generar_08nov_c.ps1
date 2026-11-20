# Pedido de arte 08-nov c (CYBERCITY): pasarela de Nova (PS01) y puerta de cristal (PC01).
# Requiere la variable de entorno WAVESPEED_API_KEY (no la escribas aqui).
$ErrorActionPreference = "Stop"
Set-Location "C:\Users\Angel Perez\Desktop\videojuego\mecha-cyberpunk"
if (-not $env:WAVESPEED_API_KEY) { Write-Error "Falta WAVESPEED_API_KEY"; exit 1 }
foreach ($r in "K01_Barrio_HUDnuevo.png","K06_CalleNova.png","K07_PuenteNova.png") {
  if (-not (Test-Path "imageassets/referencias/look/$r")) { Write-Error "Falta imageassets/referencias/look/$r"; exit 1 }
}
python imageassets/generar_imagenes.py --lote imageassets/lote_08nov_c.json --fase 1
if ($LASTEXITCODE -ne 0) { Write-Error "Fallo el lote. Vuelve a correr (las imagenes que ya existen se omiten)."; exit 1 }
Write-Host "Listo. Revisa imageassets/modelos-nuevos." -ForegroundColor Green
