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
| T-059 Prefab completo del jugador a pie y recorrido | en curso (a medias, sin verificar) |

T-050 a T-059 de la fase T-F3: pendientes, en el orden T-050, T-051, T-055, T-054, T-052, T-053, T-056, T-057, T-058, T-059.
Qué sigue: terminar T-059 en su rama tony/T-059-prefab-jugador-recorrido. Hecho a medias (commit WIP, NO verificado, sin correr verificar.sh): AparicionJugador, PuntoAparicionDePrueba, materiales de calle y banqueta, JugadorAPie.prefab con HUD y servicios anidados, Pruebas_APie regenerada como barrio en L, pruebas AparicionJugadorPruebas, PrefabJugadorAPiePruebas y RecorridoAPiePruebas, capturas T-059-1 y T-059-2. Falta: correr verificar.sh todo y arreglar lo que falle; confirmar que RecorridoAPiePruebas pasa completo (casos a-f y ningún frame de más de 100 ms); captura T-059-3 (dentro del carro); quitar de la escena las instancias sueltas del HUD y de los servicios si quedaron; pasar por el verificador. Después: cerrar T-059 (bitácora y revisión; en la revisión anotar el pedido para Alan: colocar JugadorAPie.prefab en Ciudad.unity y Pruebas_Alan.unity con idAparicion "Casa_Puerta" y apagar su cámara libre, D-tony-25), revisión de conjunto de T-F3 con el revisor (Opus) contra main y corregir lo importante, y luego T-F4. T-060 y T-061 siguen bloqueadas esperando a Alan. Me detuve a petición tuya con el turno largo sin terminar; Unity quedó cerrado.