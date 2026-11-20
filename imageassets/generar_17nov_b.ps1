# Pedido de arte 17-nov noche (CYBERCITY): zonas nuevas K11-K18 y mapa expandido M12.
# Fase 1: K11, K13-K18. Fase 2: K12 (sigue a K11) y M12.
# Requiere la variable de entorno WAVESPEED_API_KEY (no la escribas aqui).
$ErrorActionPreference = "Stop"
Set-Location "C:\Users\Angel Perez\Desktop\videojuego\mecha-cyberpunk"
if (-not $env:WAVESPEED_API_KEY) { Write-Error "Falta WAVESPEED_API_KEY"; exit 1 }
$look = "imageassets/referencias/look"
foreach ($r in "K01_Barrio_HUDnuevo.png","K06_CalleNova.png","K08_CalleSuburbios_Cuadra.png","K09_Plaza_Barrio.png","M10_MapaCiudad.png","M11_MapaCiudad.png") {
  if (-not (Test-Path "$look/$r")) { Write-Error "Falta $look/$r"; exit 1 }
}
$lote = "imageassets/lote_17nov_b.json"
foreach ($n in 1,2) {
  Write-Host "=== Fase $n ===" -ForegroundColor Cyan
  if ($n -eq 2 -and -not (Test-Path "$look/K11_Malecon.png")) { Write-Error "Falta K11_Malecon.png; K12 lo usa. Revisa la fase 1."; exit 1 }
  python imageassets/generar_imagenes.py --lote $lote --fase $n
  if ($LASTEXITCODE -ne 0) { Write-Error "La fase $n fallo. Vuelve a correr (las imagenes que ya existen se omiten)."; exit 1 }
}
Write-Host "Listo. Revisa imageassets/referencias/look (K11-K18 y M12)." -ForegroundColor Green
