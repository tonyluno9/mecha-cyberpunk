# Pedidos de arte 08-nov y 08-nov b (CYBERCITY). Corre las 5 fases en orden.
# Requiere la variable de entorno WAVESPEED_API_KEY (no la escribas aqui).
$ErrorActionPreference = "Stop"
Set-Location "C:\Users\Angel Perez\Desktop\videojuego\mecha-cyberpunk"
if (-not $env:WAVESPEED_API_KEY) { Write-Error "Falta WAVESPEED_API_KEY"; exit 1 }
foreach ($r in "K01_Barrio_HUDnuevo.png","H10_HUD.png","K06_CalleNova.png") {
  if (-not (Test-Path "imageassets/referencias/look/$r")) { Write-Error "Falta imageassets/referencias/look/$r"; exit 1 }
}
$lote = "imageassets/lote_08nov.json"
$fases = @(
  @{ n = 1; nombre = "Graficos del HUD (H20-H26)" },
  @{ n = 2; nombre = "Lugares (R10 cuarto, K07 pasarela Nova)" },
  @{ n = 3; nombre = "Personajes para Tripo (PJ01, AL10-AL12)" },
  @{ n = 4; nombre = "Piezas de calle (TK01, NE01 x2, NE02, AL01, BS01, MT01, TL01, ES01, FA01)" },
  @{ n = 5; nombre = "Piezas de Nova (NV01, NV02, BL01, BT01)" }
)
foreach ($f in $fases) {
  Write-Host "=== Fase $($f.n): $($f.nombre) ===" -ForegroundColor Cyan
  python imageassets/generar_imagenes.py --lote $lote --fase $($f.n)
  if ($LASTEXITCODE -ne 0) { Write-Error "La fase $($f.n) fallo. Corrige y vuelve a correr (las imagenes que ya existen se omiten)."; exit 1 }
}
Write-Host "Listo. Revisa imageassets/ui/hud, imageassets/referencias/look e imageassets/modelos-nuevos." -ForegroundColor Green
