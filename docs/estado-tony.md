# Estado de Tony (modo automático)

Sesión de aprobación del 2026-10-15: Tony aprobó tony/fase-3 y tony/fase-4 sin revisión extra y se integraron en main.
Las decisiones D-tony-1 a D-tony-39 pasaron a docs/decisiones.md como D-033 a D-071 (más D-072). T-F1 a T-F4 del lado de Tony
están aprobadas y en main. Rama de integración: tony/fase-5 (creada desde main; T-F5 planificada el 2026-10-15).

## T-F5: daño persistente, reparaciones y guardado

Planeada el 2026-10-15 (planificador Opus 5.5). Decisiones propuestas: D-tony-40 a D-tony-55 en docs/decisiones-pendientes-tony.md.
IDs T-100 a T-112 porque Alan ya usa T-074 a T-085 (D-tony-40). Todo con dobles (TallerDePrueba, RelojDePrueba): nada espera a Alan.
La integración con el taller de la casa de Alan (H5 completo) se escribe cuando A-F3 esté en main (D-tony-52).

Orden recomendado (D-tony-54): T-100, T-101, T-102, T-103, T-104, T-105, T-106, T-107, T-108, T-109, T-110, T-111, T-112.

| Tarea | Depende de | Estado |
| --- | --- | --- |
| T-100 Contratos de guardado, interacción y renta (Core; a main en cuanto se verifique) | nada | hecha (sin revisar), en main |
| T-101 Deudas de T-F4 y pedido de Alan (posproceso en cámaras) | nada | hecha (sin revisar) |
| T-102 Cartera del jugador y datos de la economía | T-100 | hecha (sin revisar) |
| T-103 Daño persistente del mecha del jugador | T-100 | hecha (sin revisar) |
| T-104 Mecha dañado: más lento, peor manejo y alarma roja | T-103 | hecha (sin revisar) |
| T-105 Cristal cuarteado | T-104 | pendiente |
| T-106 Premios de pelea | T-102 | hecha (sin revisar) |
| T-107 Renta cada 7 días y bloqueo del taller (con RelojDePrueba) | T-100, T-102 | pendiente |
| T-108 Usar interactuables con E | T-100 | pendiente |
| T-109 Banco del taller: reparar y pagar la renta | T-102, T-103, T-107, T-108 | pendiente |
| T-110 Dinero y renta en el HUD a pie | T-102, T-107 | pendiente |
| T-111 Guardar y cargar la partida | T-100, T-102, T-103, T-107 | pendiente |
| T-112 Recorrido de daño, taller y guardado (cierre de T-F5 de Tony) | T-101 a T-111 | hecha (sin revisar) |

Pedidos a Alan de T-F5 (D-tony-52): su `RelojMundo` implementa `IGuardable` con clave `reloj`; el `PuntoInteraccion` del taller sin
otro interactuable cerca; la cama y el espejo de A-F3 pueden ser `IInteractuable`.

## T-F4: mecha en la calle y emboscada

Orden recomendado (D-071). Decisiones de la planeación: D-059 a D-071 en docs/decisiones.md.

| Tarea | Depende de | Estado |
| --- | --- | --- |
| T-062 Contratos de llamado del mecha, taller y perfil (Core) | nada | aprobada, en main |
| T-063 Deudas menores de T-F3 | nada | aprobada, en main |
| T-064 El mecha como montable | nada nuevo | aprobada, en main |
| T-065 Encendido del mecha en 3 pasos | T-064 | aprobada, en main |
| T-066 Llamar al mecha, zonas y regreso | T-062, T-064 | aprobada, en main |
| T-067 Calor por usar el mecha en la calle | T-062, T-066 | aprobada, en main |
| T-068 Rival que no te deja perder y rival del tutorial | nada nuevo | aprobada, en main |
| T-069 Pandilla con turnos de ataque | T-068 | aprobada, en main |
| T-070 Presentador y perfil del jugador | T-062, T-068 | aprobada, en main |
| T-071 Director de la emboscada | T-064, T-065, T-066, T-069 | aprobada, en main |
| T-072 Recorrido de la emboscada (cierre de T-F4 de Tony) | T-063 a T-071 | aprobada, en main |
| T-073 Emboscada en el callejón de Alan | T-072, T-060 | bloqueada (espera A-F3) |

## T-F3 y anteriores

| Tarea | Estado |
| --- | --- |
| T-030 Carpetas y asmdefs | aprobada, en main |
| T-031 Contratos de arena y jugador | aprobada, en main |
| T-032 Contratos de reloj, lugares y puertas | aprobada, en main |
| T-021 Rival básico | aprobada, en main |
| T-022 Flujo de pelea | aprobada, en main |
| T-033 Contratos de cartera y ofertas | aprobada, en main |
| T-034 Contratos de calor y objetivos | aprobada, en main |
| T-060 Jugador en el barrio de Alan | bloqueada (espera A-F1) |
| T-061 Subir al carro de Alan | bloqueada (espera A-F2) |
| T-050 Contrato de subir y bajar | aprobada, en main |
| T-051 Jugador a pie | aprobada, en main |
| T-055 Servicio de objetivos | aprobada, en main |
| T-054 Puertas desde el lado del jugador | aprobada, en main |
| T-052 Vida y golpes a pie | aprobada, en main |
| T-053 Pistola | aprobada, en main |
| T-056 HUD a pie | aprobada, en main |
| T-057 HUD del objetivo y avisos de interacción | aprobada, en main |
| T-058 Subir y bajar del carro | aprobada, en main |
| T-059 Prefab completo del jugador a pie y recorrido | aprobada, en main |


Qué sigue: turno de día con las tareas de T-F5 en el orden de arriba (T-100 primero y a main en cuanto se verifique). T-060, T-061
y T-073 siguen bloqueadas porque alan/fase-1 todavía no está en main (A-F1, A-F2, A-F3).
Pendientes menores heredados de T-F4 (dos AudioListener, orientación del mecha estacionado, pruebas posiblemente frágiles de la
pandilla y la emboscada, capturas T-072-1/2): van en T-101.
