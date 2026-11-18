# T-1360: barrido automático de bugs del mapa nuevo

Herramienta de editor `MechaCyberpunk.Arte.Editor.BarridoMapaT1360` (Assets/Arte/Editor/BarridoMapaT1360.cs). No guarda la escena ni modifica nada.

## Cómo correrlo (Unity cerrado, desde la raíz del repo)

```bash
U="/c/Program Files/Unity/Hub/Editor/6000.3.25f1/Editor/Unity.exe"
"$U" -batchmode -projectPath . -executeMethod MechaCyberpunk.Arte.Editor.BarridoMapaT1360.Barrer -logFile Logs/barrido.log -quit
"$U" -projectPath . -executeMethod MechaCyberpunk.Arte.Editor.BarridoMapaT1360.Fotos -logFile Logs/barrido-fotos.log -quit
```
`Fotos` necesita render: córrelo sin `-batchmode` (o con el editor vivo). Lee Logs/barrido-mapa.json, así que va después de `Barrer`.

## Salidas
- Logs/barrido-mapa.json y Logs/barrido-mapa.txt (tipo, gravedad 1-4, zona, posición, objeto, detalle, cuenta).
- docs/capturas/bugs-18nov/barrido-<n>.png (gravedad 3 o 4, máx 60, grupos a menos de 8 m, cámara a 6 m) y barrido-indice.txt.

## Revisiones
1 flotantes y enterrados, 2 encimados y z-fighting, 3 huecos y vacíos, 4 escalones, 5 muros invisibles / collider que no corresponde / falta collider, 6 texturas rotas, 7 calles sin conexión, 8 islas sin salida (NavMesh en memoria), 9 luces.
Alcance: zonas nuevas y franjas de 30 m alrededor de las uniones (zona Union-Uk); los barrios viejos solo en las franjas.

## Aproximaciones
- z-fighting con cajas (bounds), no con las caras reales de la malla.
- Calles viejas y uniones: coordenadas del plan, aproximadas.
- Escrita sin poder correr Unity: no está probada en el editor. Pruebas EditMode en Tests/EditMode/Mundo/BarridoMapaT1360Pruebas.cs.
