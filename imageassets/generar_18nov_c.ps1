# Pedido 18-nov mediodia (CYBERCITY): 32 assets x 2 variantes (_a y _b) = 64 imagenes.
# Fases: 1 zonas nuevas, 2 texturas, 3 NPCs, 4 enemigos y robots, 5 drones y naves.
# Uso: sin parametros corre todo. Con -Fase N corre solo esa fase.
# Requiere la variable de entorno WAVESPEED_API_KEY (no la escribas aqui).
param([int]$Fase = 0)
$ErrorActionPreference = "Stop"
Set-Location "C:\Users\Angel Perez\Desktop\videojuego\mecha-cyberpunk"
if (-not $env:WAVESPEED_API_KEY) { Write-Error "Falta WAVESPEED_API_KEY"; exit 1 }
$lote = "imageassets/lote_18nov_c.json"
$nombres = @{ 1 = "Zonas nuevas (ML05-ML07, SK01, PU07, PU08, CC06-CC08, AU02, AU03)"; 2 = "Texturas (T07-T09)"; 3 = "NPCs (NP07-NP14)"; 4 = "Enemigos y robots (E15-E20)"; 5 = "Drones y naves (DR02-DR04, NV03)" }
$fases = if ($Fase -gt 0) { @($Fase) } else { 1..5 }
foreach ($n in $fases) {
  Write-Host "=== Fase ${n}: $($nombres[$n]) ===" -ForegroundColor Cyan
  python imageassets/generar_imagenes.py --lote $lote --fase $n
  if ($LASTEXITCODE -ne 0) { Write-Error "La fase $n fallo. Vuelve a correr (las imagenes que ya existen se omiten)."; exit 1 }
}
Write-Host "Listo. Revisa imageassets/modelos-nuevos/nuevos-18nov e imageassets/calle." -ForegroundColor Green
