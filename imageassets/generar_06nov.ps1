# Pedido de arte 06-nov (CYBERFIX). Corre las 5 fases en orden.
# Los hologramas (fase 2) usan como entrada las armas de la fase 1, por eso van despues.
# Requiere la variable de entorno WAVESPEED_API_KEY (no la escribas aqui).
$ErrorActionPreference = "Stop"
$repo = "C:\Users\Angel Perez\Desktop\videojuego\mecha-cyberpunk"
Set-Location $repo
if (-not $env:WAVESPEED_API_KEY) { Write-Error "Falta WAVESPEED_API_KEY"; exit 1 }

$lote = "imageassets/lote_06nov.json"
$fases = @(
  @{ n = 1; nombre = "Armas (W02-W04)" },
  @{ n = 2; nombre = "Hologramas (H01-H04)" },
  @{ n = 3; nombre = "Efectos (F01, D01, D02)" },
  @{ n = 4; nombre = "Calle (M01, M02)" },
  @{ n = 5; nombre = "Policia y carros para Tripo (C01, V09-V11)" }
)

foreach ($f in $fases) {
  Write-Host "=== Fase $($f.n): $($f.nombre) ===" -ForegroundColor Cyan
  if ($f.n -eq 2) {
    foreach ($a in "W01_Pistola","W02_Escopeta","W03_Fusil","W04_Francotirador") {
      if (-not (Test-Path "imageassets/armas/$a.png")) {
        Write-Error "Falta imageassets/armas/$a.png: la fase 1 no termino bien. Revisa y vuelve a correr."
        exit 1
      }
    }
  }
  python imageassets/generar_imagenes.py --lote $lote --fase $($f.n)
  if ($LASTEXITCODE -ne 0) { Write-Error "La fase $($f.n) fallo. Corrige y vuelve a correr (las imagenes que ya existen se omiten)."; exit 1 }
}
Write-Host "Listo. Revisa imageassets/armas, ui/hologramas, vfx, calle, personajes y carros." -ForegroundColor Green
