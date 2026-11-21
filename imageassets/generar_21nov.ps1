# Pedido 21-nov (CYBERCITY): granada W11 + holograma H11, entrada del mercado K19, EM01, EM02. 2 variantes (_a y _b).
# Fase 1: W11, K19. Fase 2: H11 (usa W11). Fase 3: EM01, EM02 (usan K19).
# Requiere la variable de entorno WAVESPEED_API_KEY (no la escribas aqui).
$ErrorActionPreference = "Stop"
Set-Location "C:\Users\Angel Perez\Desktop\videojuego\mecha-cyberpunk"
if (-not $env:WAVESPEED_API_KEY) { Write-Error "Falta WAVESPEED_API_KEY"; exit 1 }
$n21 = "imageassets/modelos-nuevos/nuevos-21nov"
$lote = "imageassets/lote_21nov.json"
foreach ($n in 1,2,3) {
  Write-Host "=== Fase $n ===" -ForegroundColor Cyan
  if ($n -eq 2) { foreach ($v in "a","b") { if (-not (Test-Path "$n21/W11_Granada_$v.png")) { Write-Error "Falta W11_Granada_$v.png: revisa la fase 1."; exit 1 } } }
  if ($n -eq 3) { foreach ($v in "a","b") { if (-not (Test-Path "$n21/K19_EntradaMercado_$v.png")) { Write-Error "Falta K19_EntradaMercado_$v.png: revisa la fase 1."; exit 1 } } }
  python imageassets/generar_imagenes.py --lote $lote --fase $n
  if ($LASTEXITCODE -ne 0) { Write-Error "La fase $n fallo. Vuelve a correr (las imagenes que ya existen se omiten)."; exit 1 }
}
Write-Host "Listo. Revisa imageassets/modelos-nuevos/nuevos-21nov e imageassets/ui/hologramas." -ForegroundColor Green
