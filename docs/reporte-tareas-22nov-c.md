# Reporte de la ronda C del 22-nov (noche)

Sesión A: T29, T21 (1 y 2), T22, T23, T24, T19 punto 4 y T20, más el marcador que pidió B. Sesión B: T19 (1 a 3), T21 punto 3, T25, T26,
T27 y T28 (ver docs/progreso-tareas-B.md). Todo está en main con merge --no-ff y push. Las capturas y los registros están en docs/capturas/_tareas/<tarea>/.

Nota: a media ronda se acabó el límite semanal de Sonnet (vuelve el 26-nov a las 2 a. m.). T22, la integración de T23/T19.4/T20 y T24 se hicieron con subagentes Opus.

| Tarea | Estado | Merge | Qué quedó / qué falta |
| --- | --- | --- | --- |
| T29 Quitar civiles sentados | HECHA | a2a1a1e | Se quitaron 20 NPCs sentados y quedan 0. El código y las bancas sentables se quedan. |
| T21 puntos 1 y 2 | HECHA | 96afae0 | La chica era Doña Lupe, en medio del asfalto; ahora está en la banqueta de enfrente. El callejón de la vuelta de la casa (x -86..-54, z≈-59) ya tiene asfalto, raya amarilla y bordillos. El punto 3 lo hizo B; A colocó su marcador `Historia_CallejonSkulls` en (-64, 0.2, -59), fuera de la vista de donde despierta el jugador. |
| T22 Charcos y texturas | HECHA | 88a04e9 | Había 92 charcos casi invisibles que sí sonaban a agua; ahora son charcos visibles, y se agregaron 5 nuevos (callejón y calle de la casa). Los 400 materiales sin textura ya tienen textura, y hay 25 bordillos de concreto. Pendiente: unos 5 charcos quedan debajo de carros o puestos. |
| T23 NPCs que atraviesan modelos | HECHA | 9b27d39 y 36e6964 | Los peatones revisan con una cápsula el paso de enfrente, rodean o dan la vuelta, y si aparecen dentro de algo se reubican. Se movieron 2 toldos y un corro que estaban sobre la ruta. En 2 min las muestras de peatones dentro de algo bajaron de 31 a 20, antes de mover los toldos; no se volvió a medir después. Quedan sin mover un corro (lo tapa un carro estacionado) y unos costales (los tapan pandilleros). Pruebas: 12/12. |
| T24 Carros originales de Tripo | HECHA | 36e6964 | La patrulla buena es PA01 y no se tocó. 21 carros y 5 motos usan ahora la malla de Tripo a escala real, con faros y calaveras de noche y la física de PA01. Se siguen manejando, robando y explotando, y los 57 estacionados quedaron bien. Pruebas: 132/132. Pendiente: 6 pruebas de robo esperan calor de 25 o más y ahora da 15 (es por el cambio de calor de T-1528, no por los modelos); unas 30 pruebas viejas de piezas de Blender quedaron en [Ignore]. |
| T19 punto 4 Carro fuera del muro | HECHA | 9b27d39 | Era CarroRobable_17, guardado en la escena en (-20, -127.8); se apagó. Además, un vigilante en el juego apaga o reubica cualquier vehículo que quede fuera del muro. En 3 min: 0 vehículos fuera. |
| T20 Intro con el video del logo | HECHA (falta que Tony la vea) | 9b27d39 | `videologo.mp4` se movió a Assets/Arte/Video/ con Git LFS; ya no está en la raíz, y se agregó *.mp4 a .gitattributes. El video va a pantalla completa con su proporción y se salta con Enter, clic o Esc; al terminar arranca el menú (sonido del logo y música con fade). En el build el video se decodifica sin errores, pero no se pudo ver la pantalla. |

## Builds
Desde main 588a6ed, que ya incluye todo lo de B, uno por uno, los dos con "Build Finished, Result: Success":
- `Builds/Demo/CyberCity.exe`: datos de las 17:07 (Logs/build-demo.log).
- `Builds/Juego/CyberCity.exe`: datos de las 17:09 (Logs/build-juego.log).

Prueba del Demo: con `-pruebaInicio`, Nueva partida carga la ciudad en 4.0 s con el jugador en la calle de la casa, dentro del muro; guarda, y Continuar lo deja en el mismo punto. Sin la prueba, el log muestra que el video se decodifica con Windows Media Foundation sin errores.

## Qué debe probar Tony
1. **T20:** abrir el Demo y ver el video del logo después del logo de Unity: que no se deforme, que se salte con Enter o clic, y que después suenen el sonido tecnológico y la música del menú.
2. **T21:** con Nueva partida, al levantarse, Doña Lupe ya no estorba. Los SKULLS deben salir del callejón de la vuelta (el de x≈-64), que ahora tiene asfalto.
3. **T22:** caminar por la calle de la casa y el callejón: que se vean charcos donde suena agua, y revisar las banquetas y sus bordillos.
4. **T24:** robar y manejar varios carros: que se vean como los de Tripo, al derecho, con faros de noche; que choquen y exploten. La patrulla PA01 debe seguir igual.
5. **T23:** mirar a los peatones de la banqueta de Suburbios (z≈9): que no atraviesen toldos ni carros.
6. **T29:** confirmar que nadie aparece sentado en el parque.
