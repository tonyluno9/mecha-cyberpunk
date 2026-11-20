# Pedido 19-nov noche (CYBERCITY): 23 assets x 2 variantes (_a y _b) = 46 imagenes.
# Fases: 1 vehiculos, 2 edificios, 3 muro, 4 cobertura, 5 charcos.
# Uso: sin parametros corre todo. Con -Fase N corre solo esa fase.
# Requiere la variable de entorno WAVESPEED_API_KEY (no la escribas aqui).
param([int]$Fase = 0)
$ErrorActionPreference = "Stop"
Set-Location "C:\Users\Angel Perez\Desktop\videojuego\mecha-cyberpunk"
if (-not $env:WAVESPEED_API_KEY) { Write-Error "Falta WAVESPEED_API_KEY"; exit 1 }
$lote = "imageassets/lote_19nov.json"
$nombres = @{ 1 = "Vehiculos (PA01, PA02, V22)"; 2 = "Edificios (ED01-ED12)"; 3 = "Muro (MU01-MU03)"; 4 = "Cobertura (CB01-CB04)"; 5 = "Charcos (T10)" }
$fases = if ($Fase -gt 0) { @($Fase) } else { 1..5 }
foreach ($n in $fases) {
  Write-Host "=== Fase ${n}: $($nombres[$n]) ===" -ForegroundColor Cyan
  python imageassets/generar_imagenes.py --lote $lote --fase $n
  if ($LASTEXITCODE -ne 0) { Write-Error "La fase $n fallo. Vuelve a correr (las imagenes que ya existen se omiten)."; exit 1 }
}
Write-Host "Listo. Revisa imageassets/modelos-nuevos/nuevos-19nov e imageassets/calle." -ForegroundColor Green
