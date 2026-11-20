# Componentes para Unity (CYBERCITY): H40 iconos de armas y H41 iconos de protesis, linea fina turquesa.
# Requiere la variable de entorno WAVESPEED_API_KEY (no la escribas aqui).
$ErrorActionPreference = "Stop"
Set-Location "C:\Users\Angel Perez\Desktop\videojuego\mecha-cyberpunk"
if (-not $env:WAVESPEED_API_KEY) { Write-Error "Falta WAVESPEED_API_KEY"; exit 1 }
$req = @("imageassets/referencias/menus/M30_RuedaArmas.png","imageassets/referencias/menus/M31_HabilidadesProtesis.png","imageassets/referencias/menus/M32_TiendaArmas.png","imageassets/ui/menus/H30_IconosImplantes.png","imageassets/ui/menus/H33_Implantes_Nivel2_v2.png")
foreach ($r in $req) { if (-not (Test-Path $r)) { Write-Error "Falta $r"; exit 1 } }
python imageassets/generar_imagenes.py --lote imageassets/lote_16nov.json
if ($LASTEXITCODE -ne 0) { Write-Error "Fallo el lote. Vuelve a correr (las imagenes que ya existen se omiten)."; exit 1 }
Write-Host "Listo. Revisa imageassets/ui/menus (H40 y H41)." -ForegroundColor Green
