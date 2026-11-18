# T-1333: animación de caminar del jugador, otra vez

Rama: tony/T-1333-caminar. Dueño: Tony. Estado: escrita sin Unity (worktree sin Library); falta compilar, correr Aplicar y las pruebas.

## Causa (hipótesis principal, sin medir)
- T-724 dejó al jugador con Animator.speed = 1 fijo (`AnimacionJugador.ClipsDeTony` ponía `ConfigurarClips(0,0)`). T-739/T-749/T-1310 cambiaron los clips a los de Blender (~1.29 m/s caminar, ~3.2 m/s correr) pero el jugador camina a 4.5 y corre a 7.5 m/s: los pies patinan.
- Visual_woman y Visual_PJ01 siguen con velocidadClipCaminar 0.997 / velocidadClipCorrer 2.51 (clips viejos de Tony).
- Las constantes 1.293 / 3.19 de ReglasAnimacion se midieron antes de T-1310 (clips de caminar reemplazados) y no se volvieron a medir.
- Descartado en código: loop, root y espejo ya los fija `ImportadorClipsPJ01.ConfigurarClip`; ningún creador pone un clip viejo (el controlador mapea Loco_Caminar a PJ01_Caminar_SinImplante).
- Otras hipótesis si sigue mal: velocidad de andar 4.5 demasiado alta para un clip de caminar (el tope RapidezMaximaCaminar 2.2 deja patinar); medir con MedicionClips.

## Cambios
- `AnimacionJugador.ClipsDeTony` usa las velocidades de los clips y RapidezMaximaLocomocion (sin IK de pies, como antes).
- `Personaje/Editor/CaminarT1333.cs`: `MechaCyberpunk.Personaje.Editor.CaminarT1333.Aplicar` reaplica el import de los 4 clips, mide y escribe las velocidades en Visual_woman y Visual_PJ01.
- Pruebas PlayMode de T-724 y JugadorVisualPruebas ajustadas (speed > 1 y <= tope).
- Prueba EditMode `MechaCyberpunk.Tests.CaminarT1333Pruebas`.

## Criterios
- [ ] Aplicar corrido, constantes de ReglasAnimacion revisadas contra lo medido.
- [ ] CaminarT1333Pruebas, ClipsTonyT724Pruebas y JugadorVisualPruebas pasan.
- [ ] Captura caminando: pies sin deslizarse.
