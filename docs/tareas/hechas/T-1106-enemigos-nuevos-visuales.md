# T-1106: enemigos nuevos (visuales y ganchos de IA)

Rama `tony/T-1106-enemigos-nuevos-visuales`. Diseño: `docs/diseno/enemigos-nuevos-16nov.md`. No coloca enemigos en la ciudad.

## Qué se hizo
- `imageassets/modelos3d/enemigos/` en el repo con E10_PandilleroSnakes, E11_JefeSnakes, E12_GuardiaPesadoNova, E14_JefeFinalNova (zips copiados de la carpeta principal, que sigue con SIN_NOMBRE_A/B/C).
- Importados como Humanoid (rig mixamorig, avatar válido solo) en `Assets/Arte/Personajes/<Nombre>/` con `Texturas/<Nombre>_Base|metallic|roughness|normal.jpg` (la `_0_3` es normal). Material URP propio con la textura base, sin retinte.
- Alturas 1.8 / 1.95 / 2.05 / 2.3 m. Prefabs `Visual_E10...E14` en `Personaje/Visual/Prefabs/`. Papeles: E10 SNAKES, E11 Jefe, E12 Guardias, E14 Jefe.
- Editor: `CreadorVisualTripo.CrearNuevosT1106` y `ConectarControladorBase.ConectarNuevosT1106` (solo los 4); `PostprocesadorAssetsT250.PersonajesDeTripo` incluye los 4.
- Ganchos en IA: `IConductaAPie`, `IFiltroDanioAPie` y API pública mínima en `PandilleroAPie` (Jugador, JugadorAtacable, Pandilla, VidaFraccion, SetCastigable, MoverPlano, MirarHacia, DaniarJugador, DaniarA, MostrarAviso, AnimarGolpe).

## Evidencia
- [x] EditMode completo: 2515/2515 pasan (incluye `VisualesEnemigosT1106Pruebas`, 4).
- [x] PlayMode `GanchosConductaT1106Pruebas` 4/4; `EnemigosNuevosAnimT1106Pruebas` 1 pasa + 1 de capturas (ignorada sin T1106_CAPTURAS); `EnemigosAnim2Pruebas` 6/6; `IA.PandillaPruebas` 5/5; `IA.GrupoAcosoPruebas` 8/8. Cada nueva corrió dos veces seguidas.
- [x] `JefeNovaT693Pruebas`: 3/4. La que falla (`CadaTresGolpes_CargaEnLinea...`) falla igual en la base sin mis cambios.
- [x] Capturas en `docs/capturas/t-1106/` (16 PNG, frente y perfil, parado y caminando).

## Notas
- En batchmode los cuadros salen a miles por segundo y el paso queda bajo el minMoveDistance del CharacterController; la prueba de caminar fija `Application.targetFrameRate = 60`.
- Pendiente (otras tareas): conductas, reglas, lugares y datos de los enemigos.
