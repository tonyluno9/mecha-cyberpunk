# Pedido de arte 09-nov (CYBERCITY): menus, logo y iconos con el look de las REF (solo estilo).
# Requiere la variable de entorno WAVESPEED_API_KEY (no la escribas aqui).
$ErrorActionPreference = "Stop"
Set-Location "C:\Users\Angel Perez\Desktop\videojuego\mecha-cyberpunk"
if (-not $env:WAVESPEED_API_KEY) { Write-Error "Falta WAVESPEED_API_KEY"; exit 1 }
$req = @("imageassets/referencias/menus/REF_MenuInicio.png","imageassets/referencias/menus/REF_MenuArmas1.png","imageassets/referencias/menus/REF_MenuArmas2.png","imageassets/referencias/menus/REF_MenuImplantes.png","imageassets/modelos-nuevos/PJ01_Protagonista_v2.png","imageassets/ui/hologramas/H01_Pistola.png")
foreach ($r in $req) { if (-not (Test-Path $r)) { Write-Error "Falta $r"; exit 1 } }
$lote = "imageassets/lote_09nov.json"
$fases = @(
  @{ n = 1; nombre = "Mockups de menus (M20, M21, M22)" },
  @{ n = 2; nombre = "Logo agresivo (LG20-LG23)" },
  @{ n = 3; nombre = "Hojas de iconos (H30, H31, H32)" }
)
foreach ($f in $fases) {
  Write-Host "=== Fase $($f.n): $($f.nombre) ===" -ForegroundColor Cyan
  python imageassets/generar_imagenes.py --lote $lote --fase $($f.n)
  if ($LASTEXITCODE -ne 0) { Write-Error "La fase $($f.n) fallo. Vuelve a correr (las imagenes que ya existen se omiten)."; exit 1 }
}
Write-Host "Listo. Revisa imageassets/referencias/menus, imageassets/logo e imageassets/ui/menus." -ForegroundColor Green
