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
- T22: HECHA, merge 88a04e9. El subagente Sonnet se cortó por el límite semanal (vuelve el 26-nov 2 a. m.): se siguió con subagentes Opus.
- T23, T19.4, T20 y marcador del callejón (-64,0.2,-59): HECHOS, merge 9b27d39 (T20 falta verlo en el build)
- T24: HECHA, merge 36e6964 (+ T-1647 toldos y corro fuera de la ruta de peatones)
- Builds: HECHOS (Demo 17:07, Juego 17:09, Result: Success; Demo probado). Reporte: docs/reporte-tareas-22nov-c.md. RONDA C TERMINADA.

---
# Ronda D (22-nov noche, 2): T37, T35, T38+E3, T45, T47, T36 y para-sesion-A
Sesión B en .sesiones/s2: T30-T34, T39-T44, T46, T48. A es la única que edita escenas; revisar docs/para-sesion-A.md tras cada tarea.
Sonnet sin límite hasta el 26-nov: subagentes Opus. Ramas de A (B usa T-165x): T-1680 limpieza (T37), T-1681 muro torres (T35),
T-1682 banquetas y charco (T38+E3), T-1683 menos carros (T45), T-1684 torre Nova y letreros (T47), T-1685 siempre noche (T36, wt23A/T1605-realismo).
Escenas: los subagentes trabajan de uno en uno en la carpeta principal (Unity batch); T36 es solo código, en paralelo.
- Paso 0: HECHO (merge 9032f76, rama tony/blender-22nov-d).
- T37: HECHA, merge c62636d (marcada 96e271a). Pendiente: panel blanco de la captura 05 no encontrado en la escena; cascarones de cubo con textura (Bar, Clandestino, Mercado Negro, casa, CIC, taller) anotados en assets-faltan.
- T36: código en tony/T-1685-siempre-noche (4338dc2), falta compilar e integrar (lo hace el subagente de T35 al empezar).
- T36: HECHA, merge ce13bcf (prueba Play NocheFijaT1685Play 2/2).
- T35: HECHA, merge 8340fdf (44 tramos MU01 alargados, rendijas 52 → 0).
- T45: código en tony/T-1683-menos-carros (9f74562), falta correr MenosCarrosT1683.Aplicar en la carpeta principal.
- T38+E3: en curso (T-1682).
- Sesión B: TERMINÓ (2579abf). Pide en para-sesion-A ronda D: Historia_EsperaSkulls, expendedora entre CIC y Tianguis, PuntoTrepar a azoteas, (opcional) vendedores y drones.
