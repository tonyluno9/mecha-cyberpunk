# Pedido 22-nov noche (CYBERCITY): texturas, torre Nova y letreros.
# Fase 1: T02_Banqueta, T11, TN01 (referencia) y LT05-LT09. Fase 2: TN02 (usa la TN01 de su misma letra).
# Requiere la variable de entorno WAVESPEED_API_KEY (no la escribas aqui).
$ErrorActionPreference = "Stop"
Set-Location "C:\Users\Angel Perez\Desktop\videojuego\mecha-cyberpunk"
if (-not $env:WAVESPEED_API_KEY) { Write-Error "Falta WAVESPEED_API_KEY"; exit 1 }
$n22 = "imageassets/modelos-nuevos/nuevos-22nov"
$lote = "imageassets/lote_22nov_b.json"
foreach ($n in 1,2) {
  Write-Host "=== Fase $n ===" -ForegroundColor Cyan
  if ($n -eq 2) { foreach ($v in "a","b") { if (-not (Test-Path "$n22/TN01_TorreNova_$v.png")) { Write-Error "Falta TN01_TorreNova_$v.png: revisa la fase 1."; exit 1 } } }
  python imageassets/generar_imagenes.py --lote $lote --fase $n
  if ($LASTEXITCODE -ne 0) { Write-Error "La fase $n fallo. Vuelve a correr (las imagenes que ya existen se omiten)."; exit 1 }
}
Write-Host "Listo. Revisa imageassets/calle e imageassets/modelos-nuevos/nuevos-22nov." -ForegroundColor Green
