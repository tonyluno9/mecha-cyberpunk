# Progreso de la sesión B (22-nov)

## B TERMINÓ (22-nov-2026, 03:45) — la sesión A ya puede hacer el build

Sesión B: Opus dirigió en `.sesiones/s2` (rama de integración `integ/B22`), con subagentes Sonnet. Numeración de B: T-1619 y T-1620 a T-1628.
Todo está en main con merge --no-ff y push. Lo que hay que colocar en el mapa está en docs/para-sesion-A.md.

| Tarea | Rama | Estado | Commit | Nota |
| --- | --- | --- | --- | --- |
| Contratos Core (MisionProtegida, CinematicaEnCurso, Subtitulos, EventosRuido con radio y muertes) | integ/B22 | HECHA | 7ee345e | |
| T8 escalada de enemigos | tony/T-1620-escalada | HECHA | e1f849a | registro en docs/capturas/_tareas/T8 |
| T7 cápsulas de Nova | tony/T-1621-capsulas | HECHA | 9008cd1 | capturas en docs/capturas/_tareas/T7; drones de persecución sin enganchar a la cápsula |
| T11 NPCs de frente y reacciones (+ llamar a la policía de T3) | tony/T-1622-npcs-reaccion | HECHA | d9bc6f5 | |
| T5 voces SKULLS | tony/T-1623-voces-skulls | HECHA | f3a02f3 | |
| T15 escenas del barrio y subtítulos | tony/T-1624-escenas-barrio | HECHA | 975ff84 | |
| T12 bullying de los SKULLS | tony/T-1624-escenas-barrio | NO QUEDÓ del todo (PARCIAL) | 975ff84 | código listo; en Play no se vio un bullying dinámico completo (los SKULLS ya están en GrupoAcoso; los SNAKES atacaron al jugador) |
| T10 historia y cinemáticas | tony/T-1625-historia | HECHA | 910f567 | sin probar en Play: VideoClip, pasos 15-19 y CONTINUARÁ |
| T1 brazos del E18 | tony/T-1626-brazos-e18 | HECHA | f5ecbbf | |
| T2 conductor robado | tony/T-1627-conductor | HECHA | 4d9df2b | |
| T3 sentarse, camilla, colgarse | tony/T-1628-estados | HECHA | f9fea2e | bancas y camilla por colocar (docs/para-sesion-A.md) |
| T6 (animaciones de Blender) | — | HECHA (lo de B) | — | E15_Muerte, E16_Muerte, NPC_SacadoDelCarro_Corto y los 4 *_Capsula_Salida importados (Humanoid/Generic, sin compresión) y conectados; modelos y carros son de A |

## Para el build
- No se corrió CreadorTodo. Los Aplicar de B ya están corridos y sus assets subidos (`AplicarSesionB22.Aplicar` los repite si hace falta).
- En s2 quedaron sin subir, a propósito, .fbx.meta de Assets/Arte que solo cambiaron por el remapeo de materiales de la reimportación y el
  ControladorBase.controller regenerado (mismo contenido, otros IDs).

## Cómo seguir si se corta
1. `cd .sesiones/s2`, `git status`, rama `integ/B22`.
2. Compilar con Unity cerrado (batch), probar por clase, commit con add explícito, merge a main con commit-tree (memoria merge-a-main-sin-checkout).

---
# Ronda C de B (22-nov noche): T25, T26, T27, T21.3, T19 (1-3), T28
B trabaja en `.sesiones/s2`, rama `integ/B22` (al día con main 5b68ef3). Ramas: T-1650 policía (T25), T-1651 confrontación (T26+T27),
T-1652 empujones (T21.3 + T19.1), T-1653 detalles T19 (T19.2 + T19.3), T-1654 pasos (T28). Cuatro subagentes Sonnet escribiendo en s2.
- T25: código en curso
- T26+T27: código en curso
- T21.3 + T19.1: código en curso
- T19.2 + T19.3 + T28: código en curso

## B TERMINÓ la ronda C (22-nov noche) — merge 46cae5f en main
| Tarea | Rama | Estado | Nota |
| --- | --- | --- | --- |
| T25 policía | tony/T-1650-policia | HECHA | 2 rondas, daño y estrellas en toda patrulla, atropello con impulso, puntería real, sin arresto |
| T26 SKULLS confrontación | tony/T-1651-confrontacion | HECHA | |
| T27 reglas por facción | tony/T-1651-confrontacion | HECHA | docs/confrontacion.md |
| T21 punto 3 | tony/T-1652-empujones | HECHA | falta colocar Historia_CallejonSkulls (A) |
| T19 punto 1 | tony/T-1652-empujones | HECHA | pruebas de bullying en verde |
| T19 punto 2 | tony/T-1653-detalles-t19 | HECHA | falta una alarma corporativa propia en el catálogo |
| T19 punto 3 | tony/T-1653-detalles-t19 | HECHA con reserva | no existe clip de levantarse de sentado |
| T28 pasos | tony/T-1654-pasos | HECHA | |
Unity de B cerrado. La sesión A ya puede hacer el build.
