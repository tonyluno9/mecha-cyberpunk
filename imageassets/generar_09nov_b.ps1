# Pedido de arte 09-nov b (CYBERCITY): logo pintado (LG24-LG26) e iconos H33-H34.
# Requiere la variable de entorno WAVESPEED_API_KEY (no la escribas aqui).
$ErrorActionPreference = "Stop"
Set-Location "C:\Users\Angel Perez\Desktop\videojuego\mecha-cyberpunk"
if (-not $env:WAVESPEED_API_KEY) { Write-Error "Falta WAVESPEED_API_KEY"; exit 1 }
$req = @("imageassets/referencias-privadas/logo/LG20_Amarillo.png","imageassets/ui/menus/H30_IconosImplantes.png","imageassets/modelos-nuevos/IMP01_ExoPiernas.png","imageassets/modelos-nuevos/IMP02_ExoBrazos.png","imageassets/modelos-nuevos/IMP03_VisorCabeza.png","imageassets/armas/W01_Pistola.png")
foreach ($r in $req) { if (-not (Test-Path $r)) { Write-Error "Falta $r"; exit 1 } }
$lote = "imageassets/lote_09nov_b.json"
foreach ($n in 1,2,3) {
  Write-Host "=== Fase $n ===" -ForegroundColor Cyan
  python imageassets/generar_imagenes.py --lote $lote --fase $n
  if ($LASTEXITCODE -ne 0) { Write-Error "La fase $n fallo. Vuelve a correr (las imagenes que ya existen se omiten)."; exit 1 }
}
Write-Host "Listo. Revisa imageassets/referencias-privadas/logo e imageassets/ui/menus." -ForegroundColor Green
