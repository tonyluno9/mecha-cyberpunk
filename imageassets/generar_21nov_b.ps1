# Pedido 21-nov (CYBERCITY): referencias de todo el mapa (21 imagenes) en imageassets/referencias/look/mapa-final/
# Fases: 1 calles (K20-K31), 2 muro (K32-K34), 3 aereas (A20-A24), 4 mapa (M13).
# Uso: sin parametros corre todo. Con -Fase N corre solo esa fase.
# Requiere la variable de entorno WAVESPEED_API_KEY (no la escribas aqui).
param([int]$Fase = 0)
$ErrorActionPreference = "Stop"
Set-Location "C:\Users\Angel Perez\Desktop\videojuego\mecha-cyberpunk"
if (-not $env:WAVESPEED_API_KEY) { Write-Error "Falta WAVESPEED_API_KEY"; exit 1 }
$lote = "imageassets/lote_21nov_b.json"
$nombres = @{ 1 = "Vistas de calle (K20-K31)"; 2 = "Muro (K32-K34)"; 3 = "Vistas aereas (A20-A24)"; 4 = "Mapa final (M13)" }
$fases = if ($Fase -gt 0) { @($Fase) } else { 1..4 }
foreach ($n in $fases) {
  Write-Host "=== Fase ${n}: $($nombres[$n]) ===" -ForegroundColor Cyan
  python imageassets/generar_imagenes.py --lote $lote --fase $n
  if ($LASTEXITCODE -ne 0) { Write-Error "La fase $n fallo. Vuelve a correr (las imagenes que ya existen se omiten)."; exit 1 }
}
Write-Host "Listo. Revisa imageassets/referencias/look/mapa-final." -ForegroundColor Green
