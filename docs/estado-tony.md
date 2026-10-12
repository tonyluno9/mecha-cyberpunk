# Estado de Tony (modo automático)

Fase T-F3 (docs/hoja-de-ruta.md), rama de integración: tony/fase-3 (sale de main). T-F2 terminó con revisión de conjunto y corrección, todo en tony/fase-2 y las tareas de Core en main.
Todo lo de la fase 1 (T-001, T-002, T-010 a T-013) está aprobado y en main (D-025).

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

T-050 a T-059 de la fase T-F3: hechas (sin revisar a mano) e integradas en tony/fase-3.
Qué sigue: T-F3 está completa y revisada en conjunto (revisor: aprobar con cambios). La corrección está A MEDIAS en la rama tony/T-F3-correccion (sale de tony/fase-3), sin verificar y sin correr verificar.sh; me detuve a petición tuya.
Falta de la corrección: (1) quitar del índice AGENTS.md, .agents/ y .codex/ con `git rm --cached -r` (el commit 4248361 los metió; siguen en disco sin versionar); (2) [UnityTearDown] en RecorridoAPiePruebas que cargue escena vacía y limpie Registros (hecho en parte en RecorridoAPiePruebas y BaseJugadorAPie, sin probar); (3) SensibilidadRaton leída del ConfigControlesAPie_Base en RecorridoAPiePruebas y BaseHudObjetivo (hecho en parte); (4) docs/revision-tony.md con las posiciones actuales de Pruebas_APie (editado, sin revisar) y estado. Luego: verificar.sh todo, merge a tony/fase-3 y planificar T-F4 con el planificador (Opus). Observaciones menores del revisor sin tratar (HUD repetido en pruebas, textos de FormatoHud como datos, captura T-059-2 sin el aviso de cerrado, flaky GolpeBloqueoPruebas.Golpe_ClicsSeguidos). T-060 y T-061 bloqueadas esperando a Alan. Unity quedó cerrado (maté una corrida de PlayMode a medias del agente).

## Parada (12-oct-2026, a petición de Tony)
Rama: tony/T-F3-correccion. Sin cambios nuevos desde el WIP anterior. Ya hecho: AGENTS.md, .agents/ y .codex/ fuera del índice (commit c1d34d7); siguen sin versionar en disco.
Me detuve al lanzar `verificar.sh todo`: llegó a abrir Unity en PlayMode y lo cerré a la fuerza (sin resultados; limpié InitTestScene*, SceneTemplateSettings.json y ProjectSettings.asset).
Falta: (1) correr `bash herramientas/verificar.sh todo` y confirmar la corrección (UnityTearDown de RecorridoAPiePruebas, sensibilidad desde ConfigControlesAPie_Base, docs/revision-tony.md); (2) merge --no-ff a tony/fase-3 y push; (3) planificar T-F4 con el planificador (Opus); (4) observaciones menores del revisor sin tratar (HUD repetido en pruebas, textos de FormatoHud como datos, captura T-059-2, flaky GolpeBloqueoPruebas.Golpe_ClicsSeguidos).
Sigue: verificar la corrección. T-060 y T-061 siguen bloqueadas esperando a Alan.
