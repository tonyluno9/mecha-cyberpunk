# Pedido 18-nov (CYBERCITY): vistas aereas (fase 1) y modelos V14, CH01, V23, V24 (fase 2).
# Requiere la variable de entorno WAVESPEED_API_KEY (no la escribas aqui).
$ErrorActionPreference = "Stop"
Set-Location "C:\Users\Angel Perez\Desktop\videojuego\mecha-cyberpunk"
if (-not $env:WAVESPEED_API_KEY) { Write-Error "Falta WAVESPEED_API_KEY"; exit 1 }
$look = "imageassets/referencias/look"
foreach ($r in "K11_Malecon","K12_Malecon_Muelle","K13_ColoniaCerro","K14_CentroHistorico","K15_Puerto","K16_Deshuesadero","K17_LasLomas","K18_AutopistaElevada") {
  if (-not (Test-Path "$look/$r.png")) { Write-Error "Falta $look/$r.png"; exit 1 }
}
$lote = "imageassets/lote_18nov_b.json"
foreach ($n in 1,2) {
  Write-Host "=== Fase $n ===" -ForegroundColor Cyan
  python imageassets/generar_imagenes.py --lote $lote --fase $n
  if ($LASTEXITCODE -ne 0) { Write-Error "La fase $n fallo. Vuelve a correr (las imagenes que ya existen se omiten)."; exit 1 }
}
Write-Host "Listo. Revisa imageassets/referencias/look (A11-A18) e imageassets/modelos-nuevos/nuevos-17nov." -ForegroundColor Green
