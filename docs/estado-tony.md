# Estado de Tony (modo automático)

Fase T-F4 (docs/hoja-de-ruta.md): tareas escritas, sin empezar. Rama de integración: tony/fase-4, que se crea desde tony/fase-3 al
empezar a ejecutar (todavía no existe). T-F3 está completa, revisada y corregida en tony/fase-3 (la corrección se integró con el
merge fc6d8af); las tareas de Core de T-F2 y T-F3 están en main.
Todo lo de la fase 1 (T-001, T-002, T-010 a T-013) está aprobado y en main (D-025).

## T-F4: mecha en la calle y emboscada

Orden recomendado (D-tony-39). Decisiones de la planeación: D-tony-27 a D-tony-39 en docs/decisiones-pendientes-tony.md.

| Tarea | Depende de | Estado |
| --- | --- | --- |
| T-062 Contratos de llamado del mecha, taller y perfil (Core) | nada | hecha (sin revisar), integrada a main |
| T-063 Deudas menores de T-F3 | nada | hecha (sin revisar) |
| T-064 El mecha como montable | nada nuevo | hecha (sin revisar) |
| T-065 Encendido del mecha en 3 pasos | T-064 | hecha (sin revisar) |
| T-066 Llamar al mecha, zonas y regreso | T-062, T-064 | hecha (sin revisar) |
| T-067 Calor por usar el mecha en la calle | T-062, T-066 | hecha (sin revisar) |
| T-068 Rival que no te deja perder y rival del tutorial | nada nuevo | hecha (sin revisar) |
| T-069 Pandilla con turnos de ataque | T-068 | hecha (sin revisar) |
| T-070 Presentador y perfil del jugador | T-062, T-068 | hecha (sin revisar) |
| T-071 Director de la emboscada | T-064, T-065, T-066, T-069 | hecha (sin revisar) |
| T-072 Recorrido de la emboscada (cierre de T-F4 de Tony) | T-063 a T-071 | hecha (sin revisar) |
| T-073 Emboscada en el callejón de Alan | T-072, T-060 | bloqueada (espera A-F3) |

## T-F3 y anteriores

| Tarea | Estado |
| --- | --- |
| T-030 Carpetas y asmdefs | hecha (sin revisar), integrada a main |
| T-031 Contratos de arena y jugador | hecha (sin revisar), integrada a main |
| T-032 Contratos de reloj, lugares y puertas | hecha (sin revisar), integrada a main |
| T-021 Rival básico | hecha (sin revisar), en tony/fase-2 (y en main por la corrección) |
| T-022 Flujo de pelea | hecha (sin revisar) |
| T-033 Contratos de cartera y ofertas | hecha (sin revisar), integrada a main |
| T-034 Contratos de calor y objetivos | hecha (sin revisar), integrada a main |
| T-060 Jugador en el barrio de Alan | bloqueada (espera A-F1) |
| T-061 Subir al carro de Alan | bloqueada (espera A-F2) |
| T-050 Contrato de subir y bajar | hecha (sin revisar), integrada a main |
| T-051 Jugador a pie | hecha (sin revisar) |
| T-055 Servicio de objetivos | hecha (sin revisar) |
| T-054 Puertas desde el lado del jugador | hecha (sin revisar) |
| T-052 Vida y golpes a pie | hecha (sin revisar) |
| T-053 Pistola | hecha (sin revisar) |
| T-056 HUD a pie | hecha (sin revisar) |
| T-057 HUD del objetivo y avisos de interacción | hecha (sin revisar) |
| T-058 Subir y bajar del carro | hecha (sin revisar) |
| T-059 Prefab completo del jugador a pie y recorrido | hecha (sin revisar) |

Qué sigue (parada a petición de Tony, 12-oct-2026): T-F4 en tony/fase-4. Hechas e integradas (sin revisar a mano): T-062 (también en main por cherry-pick), T-063, T-064, T-065, T-066, T-068.
T-067 hecha e integrada (EditMode 335/335, PlayMode 279/279; la barra de la pantalla de calor se corrigió en una ronda).
T-F4 del lado de Tony completa (T-062 a T-072 hechas). Sigue: revisión de conjunto de T-F4 por el revisor contra main, y planificar T-F5. T-060, T-061 y T-073 siguen bloqueadas esperando a Alan (A-F1, A-F2, A-F3).
Pendientes menores: aviso "2 audio listeners" visto al montar por código en T-065; mecha estacionado mira al inicio de la calle, no al barrio (T-064).

Parada a petición de Tony (14-oct-2026): T-F4 del lado de Tony completa (T-062 a T-072 hechas, integradas en tony/fase-4 y subidas; EditMode 375/375, PlayMode 304/304). Falta: (1) la revisión de conjunto de tony/fase-4 contra main por el revisor (Opus): se lanzó y se detuvo sin resultado, hay que repetirla; (2) corregir lo importante que salga; (3) planificar T-F5 con el planificador. T-060, T-061 y T-073 siguen bloqueadas esperando a Alan. Pendiente para el revisor: PandillaPruebas caso d y RecorridoEmboscadaPruebas (~50 s) por posible fragilidad; las capturas T-072-1/2 son poco legibles. Registrar como D-tony que Pelea referencia Progresion (ServicioObjetivos) en DirectorEmboscada.
