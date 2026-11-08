# Piezas separadas para Tripo (CYBERCITY). Orden del pedido: llantas -> carros (V01a, V05, V09 primero) -> fachadas y puertas -> motos.
# Requiere la variable de entorno WAVESPEED_API_KEY (no la escribas aqui).
$ErrorActionPreference = "Stop"
Set-Location "C:\Users\Angel Perez\Desktop\videojuego\mecha-cyberpunk"
if (-not $env:WAVESPEED_API_KEY) { Write-Error "Falta WAVESPEED_API_KEY"; exit 1 }
$lote = "imageassets/lote_piezas.json"
$fases = @(
  @{ n = 1; nombre = "Llantas (RU01, RU02)" },
  @{ n = 2; nombre = "Carros principales: carroceria + puerta (V01a, V05, V09)" },
  @{ n = 3; nombre = "Resto de carros: carroceria + puerta (V01b, V02, V03, V04, V06, V10)" },
  @{ n = 4; nombre = "Puertas sueltas (PU01-PU04)" },
  @{ n = 6; nombre = "Fachadas sin puerta con hueco al fondo blanco (C01-C03, P11, P12)" },
  @{ n = 5; nombre = "Motos sin ruedas (V07, V08, C19)" }
)
foreach ($f in $fases) {
  Write-Host "=== Fase $($f.n): $($f.nombre) ===" -ForegroundColor Cyan
  python imageassets/generar_imagenes.py --lote $lote --fase $($f.n)
  if ($LASTEXITCODE -ne 0) { Write-Error "La fase $($f.n) fallo. Corrige y vuelve a correr (las imagenes que ya existen se omiten)."; exit 1 }
}
Write-Host "Listo. Revisa imageassets/modelos-nuevos/piezas." -ForegroundColor Green
