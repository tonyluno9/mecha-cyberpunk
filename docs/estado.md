# Estado del proyecto

Actualizado: 2026-10-11, sesión de aprobación de la fase 1 de Tony y nueva división del trabajo (D-025 a D-032).
Fases, hitos y dependencias: docs/hoja-de-ruta.md.

## Dónde está cada uno

| Persona | Fase actual | Meta | Estado |
| --- | --- | --- | --- |
| Tony | T-F2: contratos de Core y pelea completa | Todo lo que Alan necesita está en Core/ y hay una pelea completa con rival, conteo y revancha | Tareas escritas en docs/tareas/pendientes (T-030 a T-034, T-021, T-022). T-F1 aprobada |
| Alan | A-F1: arena y barrio greybox | La jaula del clandestino y un barrio donde caminar, con día y noche y locales con horario | Tareas escritas en docs/tareas/pendientes (T-020, T-040 a T-044). Todavía no arranca |

## Hecho y aprobado (en main)
- T-001 Contratos de Core, T-002 Verificación automática (herramientas/verificar.sh y unity-vivo.sh).
- T-010 Movimiento del mecha, T-011 Cabina greybox, T-013 Vida y daño, T-012 Golpe y bloqueo (Tony, fase 1).
- Pruebas en main: compilar OK, EditMode 51/51, PlayMode 64/64. Prueba de Tony en Pruebas_Tony.unity: aprobada.

## Qué sigue, en orden
1. Tony: T-030 y T-031 primero (Alan los necesita), luego T-032, T-021, T-022, T-033 y T-034.
2. Alan: T-020 y T-040 pueden empezar de inmediato en greybox; T-041 a T-043 esperan T-032 (T-044 espera T-022 de Tony) para los contratos de reloj y puertas.
3. Hito H1 (pelea completa en el clandestino) cuando T-022 y T-020 estén en main.

## Pendiente de las personas
- Tony: dar acceso al repo a Alan si falta. Alan clona, corre `git lfs install` y abre el proyecto con Unity 6000.3.25f1.
- Cuando algo de Tony o Alan se apruebe, el estado de cada tarea pasa de docs/estado-<persona>.md a este archivo.
