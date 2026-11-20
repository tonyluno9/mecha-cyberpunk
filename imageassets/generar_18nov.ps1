# Pedido extra 18-nov (CYBERCITY): carros de carreras.
# Uso: sin parametros genera V20-V22 (fase 1). Con -ConV23 genera tambien V23 (fase 2).
# Requiere la variable de entorno WAVESPEED_API_KEY (no la escribas aqui).
param([switch]$ConV23)
$ErrorActionPreference = "Stop"
Set-Location "C:\Users\Angel Perez\Desktop\videojuego\mecha-cyberpunk"
if (-not $env:WAVESPEED_API_KEY) { Write-Error "Falta WAVESPEED_API_KEY"; exit 1 }
if (-not (Test-Path "imageassets/referencias/look/K16_Deshuesadero.png")) { Write-Error "Falta K16_Deshuesadero.png"; exit 1 }
$lote = "imageassets/lote_18nov.json"
$fases = @(1)
if ($ConV23) { $fases += 2 }
foreach ($n in $fases) {
  Write-Host "=== Fase $n ===" -ForegroundColor Cyan
  python imageassets/generar_imagenes.py --lote $lote --fase $n
  if ($LASTEXITCODE -ne 0) { Write-Error "La fase $n fallo. Vuelve a correr (las imagenes que ya existen se omiten)."; exit 1 }
}
Write-Host "Listo. Revisa imageassets/modelos-nuevos/nuevos-17nov." -ForegroundColor Green
