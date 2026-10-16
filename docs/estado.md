# Estado del proyecto

Actualizado: 2026-10-15, sesión de aprobación de Tony: tony/fase-3 y tony/fase-4 integradas en main (Tony las aprobó sin revisión extra).
Fases, hitos y dependencias: docs/hoja-de-ruta.md.

## Dónde está cada uno

| Persona | Fase actual | Meta | Estado |
| --- | --- | --- | --- |
| Tony | T-F5 (por planificar) | Ver docs/hoja-de-ruta.md | T-F2, T-F3 y T-F4 aprobadas y en main. T-060, T-061 y T-073 bloqueadas esperando a Alan (A-F1, A-F2, A-F3) |
| Alan | A-F1: arena y barrio greybox | La jaula del clandestino y un barrio donde caminar, con día y noche y locales con horario | Su trabajo vive en alan/fase-1 y alan/fase-2; todavía no está en main |

## Hecho y aprobado (en main)
- T-001 Contratos de Core, T-002 Verificación automática (herramientas/verificar.sh y unity-vivo.sh).
- T-F1: T-010 Movimiento del mecha, T-011 Cabina greybox, T-013 Vida y daño, T-012 Golpe y bloqueo.
- T-F2: T-030 a T-034 (Core), T-021 Rival básico, T-022 Flujo de pelea.
- T-F3: T-050 (Core), T-051 Jugador a pie, T-052 Vida y golpes a pie, T-053 Pistola, T-054 Puertas, T-055 Servicio de objetivos,
  T-056 y T-057 HUD a pie, T-058 Subir y bajar del carro, T-059 Prefab completo del jugador a pie.
- T-F4: T-062 (Core), T-063 Deudas de T-F3, T-064 Mecha montable, T-065 Encendido, T-066 Llamado del mecha, T-067 Calor,
  T-068 Piedad del rival y rival del tutorial, T-069 Pandilla con turnos, T-070 Presentador y perfil, T-071 Director de la
  emboscada, T-072 Recorrido de la emboscada.
- Decisiones de Tony de T-F2 a T-F4: D-033 a D-072 en docs/decisiones.md.
- Pruebas en main tras el merge: compilar OK, EditMode 375/375, PlayMode 304/304.

## Bloqueadas (esperan a Alan)
- T-060 Jugador en el barrio de Alan (espera A-F1), T-061 Subir al carro de Alan (espera A-F2), T-073 Emboscada en el callejón (espera A-F3).
  alan/fase-1 todavía no está en main; cuando lo esté pasan a pendientes.

## Qué sigue, en orden
1. Tony: planificar T-F5 (planificador, turno de noche si se pide) y seguir con sus tareas.
2. Alan: terminar y aprobar A-F1 a A-F3 para desbloquear T-060, T-061 y T-073.

## Pendiente de las personas
- Cuando algo de Tony o Alan se apruebe, el estado de cada tarea pasa de docs/estado-<persona>.md a este archivo.
