# Progreso de la sesión A — tareas por hacer del 22-nov

Sesión A (carpeta principal, única que edita escenas de la ciudad): T9, T4, T13, T6, T14 y lo de docs/para-sesion-A.md.
Sesión B (.sesiones/s2): T1, T2, T3, T5, T7, T8, T10, T11, T12, T15.
Ramas de A: tony/T-1601-muro (T9), tony/T-1602-hitboxes (T4), tony/T-1603-trafico (T13), tony/T-1604-blender-modelos (T6),
tony/T-1605-realismo (T14), tony/T-1606-para-sesion-A. (B: no usar T-1601..T-1619.)

## Si te quedaste sin tokens
Lee esta lista, `git log --oneline -15 main` y `git branch | grep T-160`; sigue con la primera tarea que no diga HECHA / NO QUEDÓ.

## Estado
- Paso 0: HECHO (22-nov). Cambios de Blender en tony/blender-22nov (1ff441a), merge 794b2c7 a main y push. Quedaron fuera: 4 .blend de
  revisión >95 MB (imageassets/blender/_REVISION_TONY/revision_*.blend), los .blend1, zips, __pycache__ y Logs_v121.txt (siguen en disco).
- Disco C: se llenó (0 MB) a las 02:00: vacié %TEMP%, moví Builds/ viejos a D:
espaldo-builds\Builds-hasta-22nov y quité 37 worktrees ya integrados (ramas intactas). Metas locales de cápsulas movidos a D:
espaldo-metas-22nov (B ya los subió).
- T9 muro: HECHA, merge 3c8ccff
- T4 hitboxes: HECHA, merge 96d66b1
- T13 tráfico + T6 carros: HECHAS, merge dd7fd55
- para-sesion-A: HECHA, merge 85a60e6
- Sesión B: TERMINÓ (docs/progreso-tareas-B.md). Siguen: builds Demo y Juego, reporte docs/reporte-tareas-22nov.md
- T6 Blender modelos: pendiente
- T14 realismo: HECHA, merge 4e47779 (+7b180a3 cielo menos naranja)
- para-sesion-A: pendiente

- Reporte: docs/reporte-tareas-22nov.md. Build Demo cortado por falta de memoria (Claude Code lo detuvo); builds pendientes.

---
# Ronda C (22-nov noche): T29, T21 (1-2), T22, T23, T24, T19 punto 4, T20
Sesión B en .sesiones/s2: T19 (1-3), T21 punto 3, T25, T26, T27, T28. A es la única que edita escenas; revisar docs/para-sesion-A.md tras cada tarea.
Ramas de A: T-1640 sin sentados (T29), T-1641 calle casa (T21), T-1642 charcos y texturas (T22), T-1643 NPCs colisión (T23, wt23A/T1602-hitboxes),
T-1644 carros Tripo (T24, wt23A/T1604-blender-modelos), T-1645 carro fuera del muro (T19.4, wt23A/T1603-trafico), T-1646 intro video (T20, wt23A/T1605-realismo).
Paso 0: HECHO (389475c). No hay video del logo en la carpeta (solo PNG en imageassets/logo).
- T29: HECHA, merge a2a1a1e
- T21 (1-2): HECHA, merge 96afae0
- T22: en curso en la carpeta principal (rama tony/T-1642-charcos)
- T23: código listo (0fcacd3), falta integrar
- T24: código listo (b711288), falta integrar
- T19.4: código listo (d5f0d67), falta integrar
- T20: código listo (07c88ed), falta integrar; Tony dejó videologo.mp4 en la raíz: moverlo a Assets/Arte/Video/ (LFS) al integrar
- Builds y reporte docs/reporte-tareas-22nov-c.md: pendiente
