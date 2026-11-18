# T-1360: barrido automático de bugs del Cerro y el Puerto

Rama tony/T-1360-barrido-cerro-puerto. Solo detecta y fotografía; los arreglos son otra tarea.

## Hecho
- `Assets/Arte/Editor/BarridoZonasT1360.cs`: `Barrer()` (batch, `-sufijo antes|despues`), `FotosDespues()` y `BarrerZona(GameObject)` público. Instancia cada zona (T1355_Cerro, T1357_Puerto) en una escena nueva y corre las 12 revisiones.
- Salida: `Logs/t1360-barrido-<zona>-<sufijo>.json`, `Logs/t1360-barrido-<sufijo>.md`, fotos de los 6 peores por tipo en `docs/capturas/bugs-18nov-B/`; la cámara de cada foto queda en el JSON para repetirla.
- Tipos: flota, enterrado, encimado, parpadea, sin collider, collider que no corresponde, muro invisible, rampa rota, textura rota, escalón, hueco, pendiente, escalera con salto, hueco al vacío, atrapado, no cabe el jugador, no cabe un carro, azotea aislada, pretil que bloquea, contenedor no escalable, carro en el carril, ruta de peatón atorada.

## Pruebas
- EditMode `MechaCyberpunk.Tests.Mundo.BarridoZonasT1360Pruebas` (cubos de prueba para cada tipo, caso limpio, muestreo y regla de salto).
