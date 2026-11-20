# Pedido grande 17-nov noche (CYBERCITY): vehiculos y assets de la expansion del mapa.
# Orden: 1 vehiculos -> 2 malecon -> 3 cerro -> 4 puerto -> 5 autopista -> 6 industrial -> 7 centro -> 8 lomas -> 9 texturas.
# Requiere la variable de entorno WAVESPEED_API_KEY (no la escribas aqui).
$ErrorActionPreference = "Stop"
Set-Location "C:\Users\Angel Perez\Desktop\videojuego\mecha-cyberpunk"
if (-not $env:WAVESPEED_API_KEY) { Write-Error "Falta WAVESPEED_API_KEY"; exit 1 }
$look = "imageassets/referencias/look"
foreach ($r in "K11_Malecon","K12_Malecon_Muelle","K13_ColoniaCerro","K14_CentroHistorico","K15_Puerto","K16_Deshuesadero","K17_LasLomas","K18_AutopistaElevada") {
  if (-not (Test-Path "$look/$r.png")) { Write-Error "Falta $look/$r.png"; exit 1 }
}
$lote = "imageassets/lote_17nov_c.json"
$fases = @(
  @{ n = 1; nombre = "Vehiculos (V12-V19, MT01, MT02)" },
  @{ n = 2; nombre = "Malecon (ML01-ML04)" },
  @{ n = 3; nombre = "Colonia del Cerro (CC01-CC05)" },
  @{ n = 4; nombre = "Puerto (PU01-PU06)" },
  @{ n = 5; nombre = "Autopista (AU01)" },
  @{ n = 6; nombre = "Zona Industrial (IN01-IN03)" },
  @{ n = 7; nombre = "Centro Historico (CH01-CH04)" },
  @{ n = 8; nombre = "Las Lomas (LO01-LO06)" },
  @{ n = 9; nombre = "Texturas (T04-T06)" }
)
foreach ($f in $fases) {
  Write-Host "=== Fase $($f.n): $($f.nombre) ===" -ForegroundColor Cyan
  python imageassets/generar_imagenes.py --lote $lote --fase $($f.n)
  if ($LASTEXITCODE -ne 0) { Write-Error "La fase $($f.n) fallo. Vuelve a correr (las imagenes que ya existen se omiten)."; exit 1 }
}
Write-Host "Listo. Revisa imageassets/modelos-nuevos e imageassets/calle." -ForegroundColor Green
