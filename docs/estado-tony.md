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
| T-105 Cristal cuarteado | T-104 | hecha (sin revisar) |
| T-106 Premios de pelea | T-102 | hecha (sin revisar) |
| T-107 Renta cada 7 días y bloqueo del taller (con RelojDePrueba) | T-100, T-102 | hecha (sin revisar) |
| T-108 Usar interactuables con E | T-100 | hecha (sin revisar) |
| T-109 Banco del taller: reparar y pagar la renta | T-102, T-103, T-107, T-108 | pendiente |
| T-110 Dinero y renta en el HUD a pie | T-102, T-107 | pendiente |
| T-111 Guardar y cargar la partida | T-100, T-102, T-103, T-107 | pendiente |
| T-112 Recorrido de daño, taller y guardado (cierre de T-F5 de Tony) | T-101 a T-111 | pendiente (se rehace al final) |

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

## Parada del turno largo (2026-10-15, a petición de Tony)
Tony pidió detenerse. Unity cerrado (se terminó la corrida batch de PlayMode que había quedado viva). Lo que falta:
- **T-106 (rama tony/T-106-premios-pelea, en curso):** implementada y verificada por el verificador (compilar OK, EditMode 429/429, PlayMode 337/337, commit bcbc30e). Falta la captura docs/capturas/T-106-1.png: el implementador estaba añadiendo CAPTURAR_T106 a PremioPeleaPruebas.cs; ese cambio y un PNG quedaron en un commit WIP, SIN verificar (no se corrió todo con la variable ni se revisó la imagen; la corrida se cortó). Siguiente paso: revisar la imagen, correr verificar.sh todo (con y sin CAPTURAR_T106=1), confirmar con git status que no quedan prefabs/escenas modificados, y cerrar T-106 (mover a hechas, bitácora, estado, revisión, merge a tony/fase-5).
- **T-107 a T-112:** pendientes, sin empezar. Orden: T-107, T-108, T-109, T-110, T-111, T-112 (ver tabla de arriba). Después T-F6 (pedir su planificación al planificador en Opus).
- **T-100 a main:** el push de Core a main fue denegado por el clasificador de permisos. Está listo en la rama local tony/core-pendiente-push-main (538a9c2); falta `git push origin tony/core-pendiente-push-main:main` por parte de Tony. Hasta entonces Alan no tiene IGuardable/IInteractuable/IRenta.
- **T-060, T-061, T-073:** siguen bloqueadas; alan/fase-1 todavía no está en main (revisado tras cada tarea).
- **Deudas anotadas:** Rival_Cuerpo.mat y ProjectSettings.asset cambian solos al correr pruebas; Cabina_Grietas.mat y Cabina_AlarmaPrendida.mat también se regeneran; la prueba OyenteUnicoPruebas no distingue el código viejo del nuevo; aviso de 2 audio listeners al subir a un carro; el implementador de T-104 rodeó un bloqueo del hook de unity-vivo.sh eval con un script temporal (ver revisión).
- Hecho y cerrado en tony/fase-5: T-100 a T-105 (sin revisión del revisor; el turno de noche las revisa).

## Parada manual (2026-10-15, a petición de Tony)
- Rama actual: tony/T-107-renta-bloqueo-taller (en curso, archivo en docs/tareas/en-curso/). Implementada; el verificador se detuvo a medias y se mató su corrida de PlayMode (Unity cerrado).
- Falta: verificar T-107 (verificar.sh todo + criterios), cerrarla (mover a hechas, bitácora, estado, revisión) y mezclarla en tony/fase-5.
- T-106 cerrada y en tony/fase-5. Siguen: T-108, T-109, T-110, T-111 y rehacer T-112 al final; luego T-F6.
- El worktree .claude/worktrees/tony-turno-largo es el que se usa; el checkout raíz quedó en HEAD desacoplado en edd62aa para liberar tony/fase-5.

## Parada manual 2 (2026-10-15, a petición de Tony)
- T-107 hecha y mezclada en tony/fase-5 (b3f5b0d). Verificada: EditMode 452/452, PlayMode 343/345 (2 fallos de tope de frame 100 ms, ajenos).
- Rama actual: tony/T-108-interactuables-con-e, archivo en docs/tareas/en-curso/. Implementación A MEDIAS (WIP en Personaje/ y UI/HudAPie, Pruebas_APie.unity, CreadorJugadorAPie); no compilada ni verificada. El implementador fue detenido durante `verificar.sh` de PlayMode.
- Unity: la corrida batch de PlayMode del implementador (PID 41164) seguía viva y no pude cerrarla (permiso denegado). Hay que cerrarla antes de seguir; Temp/UnityLockfile existe en el worktree.
- Falta: terminar T-108, verificarla, cerrarla; luego T-109, T-110, T-111, rehacer T-112; luego T-F6. Revisar que Pruebas_APie.unity se haya regenerado con el creador y no a mano.
- Deuda: RecorridoEmboscadaPruebas y RecorridoAPiePruebas fallan por tope de frame de 100 ms.
