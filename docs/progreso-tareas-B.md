# Progreso de la sesión B (22-nov)

Sesión B: Opus dirige en `.sesiones/s2`, rama de integración `integ/B22`. Subagentes Sonnet trabajan en s2 (sin worktrees: el disco se llenó al
crearlos). Numeración de B: T-1620 en adelante (A usa T-1601…1605).

| Tarea | Rama | Estado | Commit en main | Nota |
| --- | --- | --- | --- | --- |
| Contratos Core (MisionProtegida, CinematicaEnCurso, Subtitulos, EventosRuido.DisparoConRadio/Muerte) | tony/T-1619-contratos | en curso | | |
| T8 escalada | tony/T-1620-escalada | en curso | | |
| T7 cápsulas | tony/T-1621-capsulas | en curso | | |
| T11 NPCs | tony/T-1622-npcs-reaccion | pendiente | | |
| T5 voces SKULLS | tony/T-1623-voces-skulls | pendiente | | |
| T12+T15 bullying, escenas, subtítulos | tony/T-1624-escenas-barrio | pendiente | | |
| T10 historia | tony/T-1625-historia | pendiente | | |
| T1 brazos E18 | tony/T-1626-brazos-e18 | pendiente | | |
| T2 conductor | tony/T-1627-conductor | pendiente | | |
| T3 estados | tony/T-1628-estados | pendiente | | |
| T6 (animaciones de Blender) | tony/T-1629-anim-blender | pendiente | | |

## Cómo seguir si se corta
1. `cd .sesiones/s2`, `git status`, rama `integ/B22`. Ver la tabla: lo "en curso" puede tener archivos sin commit en s2.
2. Compilar con Unity cerrado (batch), probar por clase, commit con add explícito, merge a main con commit-tree (ver memoria merge-a-main-sin-checkout).
