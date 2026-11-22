# Pedido 21-nov mediodia (CYBERCITY): fachadas con entrada, mapa real desde capturas del juego, refineria y detalles.
# Orden del pedido: fase 1 fachadas (FC01-FC03), fase 2 mapa real (K20-K34 _final y M14), fase 3 refineria y detalles.
# Uso: sin parametros corre todo. Con -Fase N corre solo esa fase.
# Requiere la variable de entorno WAVESPEED_API_KEY (no la escribas aqui).
param([int]$Fase = 0)
$ErrorActionPreference = "Stop"
Set-Location "C:\Users\Angel Perez\Desktop\videojuego\mecha-cyberpunk"
if (-not $env:WAVESPEED_API_KEY) { Write-Error "Falta WAVESPEED_API_KEY"; exit 1 }
$lote = "imageassets/lote_21nov_c.json"
$nombres = @{ 1 = "Fachadas con entrada (FC01-FC03)"; 2 = "Mapa real (K20-K34 _final, M14)"; 3 = "Refineria y detalles (RF02-RF05, MQ01, TL02, TL03, GR01)" }
$fases = if ($Fase -gt 0) { @($Fase) } else { 1..3 }
foreach ($n in $fases) {
  Write-Host "=== Fase ${n}: $($nombres[$n]) ===" -ForegroundColor Cyan
  python imageassets/generar_imagenes.py --lote $lote --fase $n
  if ($LASTEXITCODE -ne 0) { Write-Error "La fase $n fallo. Vuelve a correr (las imagenes que ya existen se omiten)."; exit 1 }
}
Write-Host "Listo. Revisa modelos-nuevos/nuevos-21nov-b y referencias/look/mapa-real." -ForegroundColor Green
