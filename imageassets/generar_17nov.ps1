# Pedido de arte 17-nov (CYBERCITY). Orden del pedido: 3 (W09 antes de H09) -> 1 -> 4 -> 2 -> 5 -> 6.
# Requiere la variable de entorno WAVESPEED_API_KEY (no la escribas aqui).
$ErrorActionPreference = "Stop"
Set-Location "C:\Users\Angel Perez\Desktop\videojuego\mecha-cyberpunk"
if (-not $env:WAVESPEED_API_KEY) { Write-Error "Falta WAVESPEED_API_KEY"; exit 1 }
$req = @("imageassets/referencias/look/K01_Barrio_HUDnuevo.png","imageassets/referencias/look/K09_Plaza_Barrio.png","imageassets/referencias/look/H10_HUD.png","imageassets/modelos-nuevos/PJ01_Protagonista_v2.png")
foreach ($r in $req) { if (-not (Test-Path $r)) { Write-Error "Falta $r"; exit 1 } }
$lote = "imageassets/lote_17nov.json"
$fases = @(
  @{ n = 1; nombre = "Policia y Nova (V11 camioneta SWAT, NV01 nave, W09 arma Nova)" },
  @{ n = 2; nombre = "Holograma H09 (usa W09)" },
  @{ n = 3; nombre = "Plaza y vegetacion (AR01-AR03, PZ01-PZ03)" },
  @{ n = 4; nombre = "NPCs (NP01-NP06)" },
  @{ n = 5; nombre = "Texturas (T02 techo, T03 asfalto)" },
  @{ n = 6; nombre = "Efecto F02 caida Nova" },
  @{ n = 7; nombre = "UI (H43 rueda EXO, H44 iconos de tecla)" }
)
foreach ($f in $fases) {
  Write-Host "=== Fase $($f.n): $($f.nombre) ===" -ForegroundColor Cyan
  if ($f.n -eq 2 -and -not (Test-Path "imageassets/modelos-nuevos/nuevos-17nov/W09_ArmaNova.png")) { Write-Error "Falta imageassets/armas/W09_ArmaNova.png: revisa la fase 1."; exit 1 }
  python imageassets/generar_imagenes.py --lote $lote --fase $($f.n)
  if ($LASTEXITCODE -ne 0) { Write-Error "La fase $($f.n) fallo. Vuelve a correr (las imagenes que ya existen se omiten)."; exit 1 }
}
Write-Host "Listo. Revisa modelos-nuevos, armas, ui/hologramas, calle, vfx y ui/menus." -ForegroundColor Green
