# T-1366: medir los FPS por zona en el build

Rama: tony/T-1366-fps-zonas. Carpetas: Mundo/Pruebas/, Mundo/Editor/HerramientasFpsT1366.cs, Tests/EditMode/Mundo/, docs/tareas/.

## Cómo medir

```
CyberCity.exe -medirFpsZonas -screen-width 1920 -screen-height 1080
```

Sin `-medirFpsZonas` el medidor no existe (no hay nada en el juego normal). Con el argumento: partida nueva en Ciudad_Prologo
(llama por reflexión a `FlujoPartida.IrANuevaPartida`, así salta el título sin que Mundo referencie Progresión), 6 s para asentar,
entrada del jugador apagada, hora 23:00 con RelojMundo + CicloDiaNoche, vsync apagado y sin tope de FPS (se restauran).
Una cámara nueva con los ajustes de la principal recorre las vistas: 3 s de espera y 5 s de medición cada una.
Sale solo con Application.Quit y escribe `fps-zonas.csv` y `fps-zonas.txt` junto al exe (o en persistentDataPath).

Columnas: zona, vista, frames, fps_promedio, fps_p1_bajo (FPS del 1 % de frames más lentos), ms_promedio, ms_peor1pct,
cpu_ms y gpu_ms (FrameTimingManager; -1 si no hay), renderers_visibles, triangulos (suma de índices/3 de los visibles),
luces_activas_60m, luces_en_vista, draw_calls y setpass_calls (ProfilerRecorder; suele valer -1 en un build que no es Development).

## Vistas

Están en `Assets/_Proyecto/Mundo/Pruebas/VistasFpsT1366.cs` (una sola lista, la usan el build y el editor). Las coordenadas son
aproximadas (sacadas de docs/plan-zonas-18nov.md); la altura sale de un rayo al suelo. Ajústalas ahí.

## Editor

- Menú Mecha Cyberpunk/Mundo/FPS T-1366/Diagnosticar: con la escena abierta, sin renderizar, escribe `Logs/fps-diagnostico.txt`
  (por vista, de peor a mejor por triángulos: renderers en el frustum, triángulos, materiales distintos, luces que lo tocan,
  15 objetos con más triángulos). No considera oclusión.
- Iniciar medición en Play: lo mismo que el build, en Play Mode (`MedidorFpsZonas.IniciarEnEditor`).

## Pruebas

EditMode: `MedidorFpsT1366Pruebas` (promedio, peor 1 %, CSV, lista de vistas). No se corrieron (sin Library en este worktree).
