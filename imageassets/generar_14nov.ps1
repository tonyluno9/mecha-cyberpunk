# Pedido de arte 14-nov (CYBERCITY). Fases 1-5; los hologramas (fase 5) usan las armas de la fase 2.
# Requiere la variable de entorno WAVESPEED_API_KEY (no la escribas aqui).
$ErrorActionPreference = "Stop"
Set-Location "C:\Users\Angel Perez\Desktop\videojuego\mecha-cyberpunk"
if (-not $env:WAVESPEED_API_KEY) { Write-Error "Falta WAVESPEED_API_KEY"; exit 1 }
$req = @("imageassets/referencias/look/H10_HUD.png","imageassets/referencias/look/K01_Barrio_HUDnuevo.png","imageassets/ui/menus/H33_Implantes_Nivel2_v2.png","imageassets/modelos-nuevos/PJ01_Protagonista_v2.png","imageassets/modelos-nuevos/DR01_Dron.png")
foreach ($r in $req) { if (-not (Test-Path $r)) { Write-Error "Falta $r"; exit 1 } }
$lote = "imageassets/lote_14nov.json"
$fases = @(
  @{ n = 1; nombre = "Mockups (M30 rueda, M31 habilidades, M32 tienda)" },
  @{ n = 2; nombre = "Armas nuevas (W05-W08)" },
  @{ n = 3; nombre = "Protesis de ataque (IMP04, IMP05)" },
  @{ n = 4; nombre = "Enemigos (E10-E14)" },
  @{ n = 5; nombre = "Hologramas de las armas nuevas (H05-H08)" }
)
foreach ($f in $fases) {
  Write-Host "=== Fase $($f.n): $($f.nombre) ===" -ForegroundColor Cyan
  if ($f.n -eq 5) {
    foreach ($a in "W05_Subfusil","W06_Revolver","W07_RiflePlasma","W08_Lanzagranadas") {
      if (-not (Test-Path "imageassets/armas/$a.png")) { Write-Error "Falta imageassets/armas/$a.png: revisa la fase 2."; exit 1 }
    }
  }
  python imageassets/generar_imagenes.py --lote $lote --fase $($f.n)
  if ($LASTEXITCODE -ne 0) { Write-Error "La fase $($f.n) fallo. Vuelve a correr (las imagenes que ya existen se omiten)."; exit 1 }
}
Write-Host "Listo. Revisa referencias/menus, armas, modelos-nuevos y ui/hologramas." -ForegroundColor Green
