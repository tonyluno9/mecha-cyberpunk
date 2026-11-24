# T-musica-24nov: música del juego por fases

## Paso 0 (obligatorio)
Cerrar Unity y Unity Hub, confirmar que no queda ningún Unity.exe, borrar `Temp/UnityLockfile` y repetir si el candado vuelve a aparecer.

## Archivos
Están en `3d sonidos/20_Musica/`. Copiarlos a `Assets/Arte/Audio/20_Musica/`, cada uno con su .meta. Importar en **Streaming** y en loop.

| Archivo | Cuándo suena |
|---|---|
| (ya existe) música del menú de inicio | Menú principal: no se cambia |
| MUS_PIANO_INTRO.wav (42 s, loop) | Intro y cinemáticas C01–C09, debajo de la narración (bajar la música ~-10 dB mientras habla la narración) y en toda la parte guiada antes de la venganza (calle, Doc, Tino) |
| MUS_CAMPO_TIRO.wav (7 s, loop) | Mientras el jugador está en el campo de tiro holográfico |
| MUS_FASE_SKULLS.mp3 | Desde "ERES LIBRE": fase de pelea contra los SKULLS |
| MUS_FASE_POLICIA.mp3 | Cuando entra la fase de policía (sube el calor y llegan las patrullas) |
| MUS_FASE_NOVA.mp3 | Cuando entra la fase NOVA (caen las cápsulas) |
| MUS_PIANO_INTRO.wav (otra vez) | Pantalla "HAS MUERTO": entra con fade |

## Reglas
- Cambio de fase con crossfade de 2–3 s; la música nunca vuelve a una fase anterior mientras el jugador siga vivo.
- "ERES LIBRE" no lleva sonido propio: solo arranca MUS_FASE_SKULLS.
- Si el jugador está en un interior (Doc, Tino), la música de fase baja de volumen o se apaga y vuelve al salir.
- Al REINTENTAR, la música vuelve a la fase SKULLS.
- No hay radio en los carros ni estaciones: se quitó.

## Cómo comprobarlo
Que el usuario juegue: cinemáticas con piano y narración encima → campo de tiro con su música → ERES LIBRE → SKULLS → policía → NOVA → morir (piano). Anotar en la revisión los pasos para oírlo.
