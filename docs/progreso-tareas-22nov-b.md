# Progreso de las tareas del 22-nov (segunda ronda): T16, T17, T18 y terminar T12

Opus dirige; subagentes Sonnet escriben el código en los worktrees que ya existían (no crear más):
- T16 tráfico solo patrullas: ../wt23A/T1603-trafico, rama tony/T-1630-solo-patrullas
- T17 escalada final Nova: ../wt23A/T1602-hitboxes, rama tony/T-1631-escalada-nova
- T18 NPCs sentados: ../wt23A/T1604-blender-modelos, rama tony/T-1632-npcs-sentados
- T12 bullying (terminar): ../wt23A/T1605-realismo, rama tony/T-1633-bullying
Unity solo en la carpeta principal: cada rama se integra en integ/A2-<tarea>, se compila, se aplica, se prueba por clase y se comprueba en Play;
luego merge --no-ff a main y push. Al final: builds Demo y Juego uno por uno, prueba del Demo, docs/reporte-tareas-22nov-b.md.

## Si te quedaste sin tokens
Lee esta lista y `git log --oneline -15 main`; sigue con la primera tarea que no diga HECHA / NO QUEDÓ.

## Estado
- Paso 0: HECHO (merge cc913e9: tareas nuevas en tareas-por-hacer.md; no había cambios nuevos de Blender).
- T16: HECHA, merge 45695c3
- T17: HECHA, merge e6fa324
- T18: integrando en integ/A2-T18-T12
- T12: integrando en integ/A2-T18-T12
- Builds: pendiente
