# Pedido de arte 06-nov tarde (CYBERFIX). Corre las 3 fases en orden.
# Requiere la variable de entorno WAVESPEED_API_KEY (no la escribas aqui).
$ErrorActionPreference = "Stop"
Set-Location "C:\Users\Angel Perez\Desktop\videojuego\mecha-cyberpunk"
if (-not $env:WAVESPEED_API_KEY) { Write-Error "Falta WAVESPEED_API_KEY"; exit 1 }
$lote = "imageassets/lote_06nov_b.json"
$fases = @(
  @{ n = 1; nombre = "Mockups de HUD (HUD_A-HUD_D)" },
  @{ n = 2; nombre = "Modelos para Tripo (P10-P12, L15, R01, IMP01-03, O01, O02, DR01)" },
  @{ n = 3; nombre = "Textura de banqueta (T01)" }
)
foreach ($f in $fases) {
  Write-Host "=== Fase $($f.n): $($f.nombre) ===" -ForegroundColor Cyan
  python imageassets/generar_imagenes.py --lote $lote --fase $($f.n)
  if ($LASTEXITCODE -ne 0) { Write-Error "La fase $($f.n) fallo. Corrige y vuelve a correr (las imagenes que ya existen se omiten)."; exit 1 }
}
Write-Host "Listo. Revisa imageassets/ui/hud-opciones, imageassets/modelos-nuevos e imageassets/calle." -ForegroundColor Green
