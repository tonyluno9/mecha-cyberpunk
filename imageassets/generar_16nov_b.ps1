# Pedido de arte 16-nov (CYBERCITY). Fases 1-5 obligatorias; la 6 (letreros de neon) es opcional.
# Uso: powershell -ExecutionPolicy Bypass -File imageassets\generar_16nov_b.ps1            (fases 1-5)
#      powershell -ExecutionPolicy Bypass -File imageassets\generar_16nov_b.ps1 -ConNeones  (fases 1-6)
# Requiere la variable de entorno WAVESPEED_API_KEY (no la escribas aqui).
param([switch]$ConNeones)
$ErrorActionPreference = "Stop"
Set-Location "C:\Users\Angel Perez\Desktop\videojuego\mecha-cyberpunk"
if (-not $env:WAVESPEED_API_KEY) { Write-Error "Falta WAVESPEED_API_KEY"; exit 1 }
$req = @("imageassets/referencias/look/K01_Barrio_HUDnuevo.png","imageassets/referencias/look/H10_HUD.png","imageassets/modelos-nuevos/PJ01_Protagonista_v2.png","imageassets/ui/menus/H41_IconosProtesis.png")
foreach ($r in $req) { if (-not (Test-Path $r)) { Write-Error "Falta $r"; exit 1 } }
$lote = "imageassets/lote_16nov_b.json"
$fases = @(
  @{ n = 1; nombre = "Modelos para Tripo (MA01, TR01, TR02)" },
  @{ n = 2; nombre = "Pantallas de carga (L01-L06)" },
  @{ n = 3; nombre = "Mockups ARRESTADO y HAS MUERTO (M33, M34)" },
  @{ n = 4; nombre = "Iconos de protesis de ataque (H42)" },
  @{ n = 5; nombre = "Referencias de calles (K08, K09, K10)" }
)
if ($ConNeones) { $fases += @{ n = 6; nombre = "Letreros de neon (LT01-LT04)" } }
foreach ($f in $fases) {
  Write-Host "=== Fase $($f.n): $($f.nombre) ===" -ForegroundColor Cyan
  python imageassets/generar_imagenes.py --lote $lote --fase $($f.n)
  if ($LASTEXITCODE -ne 0) { Write-Error "La fase $($f.n) fallo. Vuelve a correr (las imagenes que ya existen se omiten)."; exit 1 }
}
Write-Host "Listo. Revisa modelos-nuevos, ui/carga, referencias/menus, ui/menus y referencias/look." -ForegroundColor Green
