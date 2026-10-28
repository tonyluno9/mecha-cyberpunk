# Estado de Alan (modo automático)

Actualizado: 2026-10-27. Ramas: alan/fase-5 (integración actual, sale de main) y alan/T-093-recorrido-prologo.

**En main (5c6e986):** A-F1 a A-F4 completas, es decir T-020 a T-099 salvo T-084 y T-085 (A-F2, denegadas por el modo Auto: no se intentan por otra vía),
y T-092. Lo de Tony que Alan esperaba (T-F4, T-F5, T-F6, prefabs del prólogo) también está en main.

**T-093, T-300, T-330 y T-340 hechas** (2026-10-27, en alan/T-093-recorrido-prologo y alan/fase-5, todavía no en main): `Ciudad_Prologo.unity` con el prólogo de Tony colocado,
`Prologo_Esquina_2`, `Flashback_Plano_1` a `_6` y `Flashback.prefab` en `Flashback.unity`, y `RecorridoPrologoCiudadPruebas` (3 pruebas con entrada simulada).
Verificado: compilar OK, EditMode 914/914, PlayMode 889/898 (0 fallos; las 9 omitidas son capturas de Tony que se activan con variable de entorno).
Qué falta para el hito H6 del lado de Tony: T-136 (que apunte `DatosEscenas_Demo` a `Ciudad_Prologo.unity`). Ver docs/revision-alan.md, sección T-093.

| Tarea | Estado |
| --- | --- |
| T-074 a T-083 (A-F2) | en main |
| T-084 Tráfico mínimo de fachada, T-085 Modelo del carro viejo | pendientes: el modo Auto las denegó; no se hacen |
| T-086 a T-092, T-094 a T-099 (A-F3 y A-F4) | en main |
| T-093 Recorrido del prólogo | hecha, falta mezclar a main (aprobación de Alan) |
| T-300, T-330, T-340 (lógica pura de casino, peatones y persecución) | hechas, en alan/fase-5 |
| T-301 Tragamonedas jugable | hecha (2026-10-27), en alan/fase-5; Casino PlayMode 9/9, suite completa 898/907 sin fallos antes del ajuste visual final |
| T-302 Fichas y cajera | hecha (2026-10-27), en alan/T-302-fichas-cajera; compilar OK, EditMode 952/952, PlayMode 909/918 (9 omitidas, 0 fallos). Fichas son un servicio aparte (ServicioFichas.prefab): colócalo en una escena siempre cargada para que se guarden (clave `fichas`) |
| T-341 | hecha (2026-10-28), en alan/T-341-drones-patrullas, sin integrar |
| T-303, T-320, T-321, T-331 | pendientes (T-321 espera el mostrador de la clínica de Tony) |
| T-136 (de Tony) | bloqueada solo por el cambio de datos de escena de Tony |
| T-310, T-311 (visuales) | en otras sesiones/worktrees (mecha-cyberpunk-alan-visual y -fachadas); no tocar desde aquí |

Fecha de corte del proyecto: desde el 2026-11-12 no se empiezan tareas nuevas.

## T-341 (2026-10-28): drones y patrullas con calor, lista para revisión
- Rama alan/T-341-drones-patrullas, con origin/main integrado (incluye T-250). `GrafoCalles`, `DronMundo`, `PatrullaMundo`, `GestorPersecucion` (escucha `ICalor.NivelCambio`; al entrar a la Casa llama a `ICalor.Borrar()`) y `CreadorPersecucion`. El aviso de la casa vive en el gestor, no en Mundo.
- `CreadorPersecucion.Crear` colgó el gestor "Persecucion" en Barrio.prefab editando el prefab en sitio (+48 líneas, sin regenerar el barrio; el arte T-250 no se tocó).
- Evidencia: compilar OK; EditMode 1135/1135; PlayMode completo 1012 pasaron, 0 fallos, 9 omitidas. Pruebas de Trafico con doble de ICalor sobre Barrio.prefab: nivel 0 sin perseguidores, subir crea, bajar a 0 retira, entrar a Casa borra el calor, el dron se acerca sin colliders, la patrulla recorre 60 s de juego sin salir de la calle. Capturas revisadas: docs/capturas/T-341-1.png (dron) y T-341-2.png (patrulla con sirena en la calle); las genera la prueba `Capturas_DronYPatrulla`.
- No se probó con el calor real de Tony (solo dobles) ni a mano en Unity. El PlayMode completo regeneró capturas viejas y ProjectSettings.asset: no se incluyeron en el commit.
