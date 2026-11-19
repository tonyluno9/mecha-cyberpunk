# T-1367: respuesta al reporte de la sesión A (18-nov C)

Número provisional. Solo cambia `T1355_Cerro.prefab` y `T1357_Puerto.prefab` (y mallas nuevas `T1367_*` en `Assets/_Proyecto/Mundo/Datos/Zonas/`).

## Hecho (sin correr: lo corre el orquestador, después de T1363, T1364, T1365 y T1366)
- `Assets/Arte/Editor/ArreglarReporteAT1367.cs`: `Aplicar()` (idempotente, informe `Logs/t1367-arreglos.txt`), `FotosAntes()` y `FotosDespues()` (`docs/capturas/bugs-18nov-B/t1367-<n>-antes|despues.png`; `FotosAntes` ANTES de `Aplicar`).
- `Assets/_Proyecto/Tests/EditMode/Mundo/ReporteAT1367Pruebas.cs`: pasan solo después de `Aplicar`.

## Criterios
1. Ladera sin vértices en el tubo ni en la boca de salida.
2. Arroyos de U4 y base a y en [-0.02, 0.02] en la unión; banquetas fuera del arroyo vecino; CE03 fuera del arroyo.
3. Puerta norte del Puerto: paso libre de 10 m; sin colliders en (304.3, -56.2) y (295, -85.2).
4. Sin arroyos coplanares encimados; pares coplanares de casas separados; FocoPared al ras; CC01/CC02 al ras.
