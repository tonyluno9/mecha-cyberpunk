# Estado de Tony (modo automático)

Sesión de aprobación del 2026-10-15: Tony aprobó tony/fase-3 y tony/fase-4 sin revisión extra y se integraron en main.
Las decisiones D-tony-1 a D-tony-39 pasaron a docs/decisiones.md como D-033 a D-071 (más D-072). T-F1 a T-F4 del lado de Tony
están aprobadas y en main. Rama de integración: tony/fase-5 (creada desde main; T-F5 planificada el 2026-10-15).

## T-F7, bloque 1: economía completa

Planeado el 2026-10-17 (planificador Opus 5.5) en tony/planificacion-fase-7, que sale de main (ya con T-F6). Decisiones propuestas: D-tony-75 a
D-tony-88 en docs/decisiones-pendientes-tony.md. T-F7 se planifica por bloques (D-tony-75); este es el primero. Todo con dobles y la escena
nueva de Tony Pruebas_Economia (T-143): nada espera a Alan. Pedido a Alan para A-F5: colocar `MostradorMercadoNegro.prefab` en su mercado negro;
su casino puede tomar apuestas con `IApuestasPelea` (T-141).

Orden recomendado (D-tony-88): T-140, T-141, T-142, T-143, T-144, T-145, T-146, T-147, T-148, T-149, T-150, T-151.
T-141 puede ir primero si T-140 se atora; T-149 solo necesita T-143; T-148 no necesita T-146.

| Tarea | Depende de | Estado |
| --- | --- | --- |
| T-140 Deudas: cuelgue de PlayMode de Economia y helper del estado de los mapas de acciones | nada | hecha (sin revisar) |
| T-141 Contrato de apuestas de pelea y PlayMode por área en verificar.sh (Core; a main en cuanto se verifique) | nada (mejor tras T-140) | hecha (sin revisar) |
| T-142 Inventario del jugador y objetos vendibles | T-140 | hecha (sin revisar) |
| T-143 Escena Pruebas_Economia | T-140, T-142 | hecha (sin revisar) |
| T-144 Apuestas: lógica, datos y resolución en el flujo de pelea | T-141, T-142 | hecha (sin revisar) |
| T-145 Apostar en el mostrador de peleas | T-143, T-144 | hecha (sin revisar) |
| T-146 Mostrador de tienda: comprar en el mercado negro | T-142, T-143 | hecha (sin revisar) |
| T-147 Vender piezas y chatarra en el mercado negro | T-146 | hecha (sin revisar) |
| T-148 Kits de reparación en plena pelea | T-142, T-143 | hecha (sin revisar) |
| T-149 Reparar a medias en el banco del taller | T-143 | hecha (sin revisar) |
| T-150 Precios de la economía y pruebas de balance | T-144, T-146 a T-149 | hecha (sin revisar) |
| T-151 Recorrido de la economía completa (cierre del bloque 1) | T-140 a T-150 | hecha (sin revisar) |

Bloques siguientes de T-F7, en orden (se escriben al terminar el anterior, desde T-152; D-tony-76):
1. Ligas: 4 ligas de 5 rivales, campeón con pelea de jefe, subir de liga, premios y momios por liga.
2. Piezas por ramas y niveles: 5 piezas, 4 ramas mezclables, 4 niveles desbloqueados por liga, compra en el taller y piezas viejas al inventario.
3. Habilidades: especiales por pieza con cooldown y munición limitada que se compra.
4. Implantes: 4 espacios (ojos, brazos, piernas, sistema nervioso) en la clínica, con el doctor y su deuda al morir en la calle.
5. Misiones y reputación: barrio, corporativas, ilegales y robos, con reputación de barrio y corporaciones y favores usables.
6. Radio: 3 estaciones en el carro y el mecha.
7. Adaptación del rival: el rival aprende de tu forma de pelear en peleas repetidas.
8. Hackeo, si sobra tiempo.

## T-F6: creación de personaje, prólogo y cierre de la demo

Planeada el 2026-10-16 (planificador Opus 5.5) en tony/planificacion-fase-6, integrada en tony/fase-5 (se sigue trabajando ahí hasta que
Tony apruebe T-F5 y se abra tony/fase-6). Decisiones propuestas: D-tony-56 a D-tony-74 en docs/decisiones-pendientes-tony.md.
IDs T-120 a T-137; T-113 a T-119 reservadas para deudas (D-tony-56). Todo con dobles y escenas de Tony (Pruebas_Prologo,
Pruebas_Flashback); lo que espera a Alan está bloqueado (T-136, T-137), como T-073.

Orden recomendado (D-tony-73): T-120, T-121, T-122, T-123, T-124, T-125, T-126, T-127, T-128, T-129, T-130, T-131, T-132, T-133, T-134, T-135.
T-121, T-122 y T-125 no dependen de Core (pueden ir primero si T-120 se atora); T-134 solo depende de T-121.

| Tarea | Depende de | Estado |
| --- | --- | --- |
| T-120 Contratos de historia, apariencia y puntos de historia (Core; a main en cuanto se verifique, detrás de T-100) | nada | hecha (sin revisar), solo en tony/fase-5 |
| T-121 Deudas de T-F5: daño persistente solo en el mecha de la historia y creador de Pruebas_Calle | nada | hecha (sin revisar) |
| T-122 Pruebas frágiles: tope de frame, ritmo del rival y materiales que cambian solos | nada (mejor tras T-121) | hecha (sin revisar) |
| T-123 Apariencia del jugador: catálogo, cuerpo y vista previa | T-120 | hecha (sin revisar) |
| T-124 Etapas de la historia y partida entre escenas | T-120 | hecha (sin revisar) |
| T-125 Fundido a negro y pantalla de "continuará" | nada | hecha (sin revisar) |
| T-126 Pantalla de título y creación de personaje | T-123, T-124, T-125 | hecha (sin revisar) |
| T-127 Ensamblado del prólogo y escena Pruebas_Prologo | T-120, T-121 | hecha (sin revisar) |
| T-128 El mecha de la historia entra a la arena | T-121, T-125, T-127 | hecha (sin revisar) |
| T-129 Primera pelea casera y mostrador del clandestino | T-128 | hecha (sin revisar) |
| T-130 Cama (dormir y guardar) y espejo de la casa | T-123, T-124, T-125, T-127 | hecha (sin revisar) |
| T-131 Flashback de los papás: lógica, datos y escena de pruebas | T-120, T-124, T-125, T-127 | hecha (sin revisar) |
| T-132 Director del prólogo, parte 1: la noche del campeonato | T-124, T-127, T-128, T-130 | hecha (sin revisar) |
| T-133 Director del prólogo, parte 2: despertar, pelea casera y "continuará" | T-129, T-131, T-132 | hecha (sin revisar) |
| T-134 Ayudas de controles para una persona nueva | T-121 | hecha (sin revisar) |
| T-135 Recorrido del prólogo completo (cierre de T-F6 de Tony) | T-121 a T-134 | hecha (sin revisar) |
| T-136 El prólogo en Ciudad.unity y Flashback.unity de Alan (H6) | T-135, T-073, T-061 | bloqueada (A-F3 mezclada, pero Ciudad.unity no trae DirectorPrologo ni IPuntoHistoria de Tony; falta T-061 y A-F4) |
| T-137 Escenas de la demo en Build Settings y build de Windows (Core) | T-136 | bloqueada (espera A-F4) |

Pedidos a Alan de T-F6 (D-tony-72): colocar `DirectorPrologo.prefab`, `CamaJugador.prefab`, `EspejoJugador.prefab` y `MostradorPeleas.prefab`
en Ciudad.unity y `Flashback.prefab` en Flashback.unity; apariciones "Casa_Cama", "Casa_Puerta", "Clandestino_Salida"; `IPuntoHistoria`
"Prologo_Carro", "Prologo_Esquina_N" y "Flashback_Plano_1" a "_6"; leer `IHistoria` (carro cerca del clandestino en la noche del prólogo).
Pregunta para Tony (D-tony-58): ¿al despertar empieza la historia desde cero ($500, mecha al 45 %) o sigue la mañana siguiente con lo que quedó?

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
| T-109 Banco del taller: reparar y pagar la renta | T-102, T-103, T-107, T-108 | hecha (sin revisar) |
| T-110 Dinero y renta en el HUD a pie | T-102, T-107 | hecha (sin revisar) |
| T-111 Guardar y cargar la partida | T-100, T-102, T-103, T-107 | hecha (sin revisar) |
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
| T-073 Emboscada en el callejón de Alan | T-072, T-060 | hecha (sin revisar) |

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
| T-060 Jugador en el barrio de Alan | hecha (sin revisar) |
| T-061 Subir al carro de Alan | hecha (sin revisar) |
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


Qué sigue: T-112 (rehacer al final) y T-F6. T-061 y T-073 siguen bloqueadas: alan/fase-1 (A-F1) ya está en main, pero faltan A-F2 (IMontable en Vehiculos/) y A-F3 (IPuntoLlamadoMecha e ITallerMecha en Mundo/).
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

## Después de T-130 (2026-10-17)
- T-130 hecha (sin revisar), integrada en tony/fase-5. Sigue la siguiente de T-F6 en el orden de arriba.

## Después de T-131 (2026-10-17)
- T-131 hecha (sin revisar), integrada en tony/fase-5. Sigue la siguiente de T-F6 en el orden de arriba.

## Después de T-132 (2026-10-17)
- T-132 hecha (sin revisar), integrada en tony/fase-5. Sigue la siguiente de T-F6 en el orden de arriba.

## Después de T-133 (2026-10-17)
- T-133 hecha (sin revisar), integrada en tony/fase-5. Sigue la siguiente de T-F6 en el orden de arriba.

## Después de T-134 (2026-10-17)
- T-134 hecha (sin revisar), integrada en tony/fase-5. Sigue la siguiente de T-F6 en el orden de arriba.

## Después de T-135 (2026-10-17)
- T-135 hecha (sin revisar), integrada en tony/fase-5. Sigue la siguiente de T-F6 en el orden de arriba.

## T-F6 cerrada (2026-10-17)
- T-129 a T-135 hechas (sin revisar) en tony/fase-5. Suite completa en docs/revision-tony.md. Deuda: PlayMode de Economia se cuelga (anterior a T-129). Siguiente: merge a main, mezclar alan/fase-3, T-073, T-136, T-137 y planificar T-F7.

## Después de T-140 (2026-10-17)
- T-140 hecha (sin revisar), integrada en tony/fase-5. Sigue la siguiente de T-F6 en el orden de arriba.

## Después de T-141 (2026-10-17)
- T-141 hecha (sin revisar), integrada en tony/fase-5. Sigue la siguiente de T-F6 en el orden de arriba.

## Después de T-142 (2026-10-17)
- T-142 hecha (sin revisar), integrada en tony/fase-5. Sigue la siguiente de T-F6 en el orden de arriba.

## Después de T-143 (2026-10-17)
- T-143 hecha (sin revisar), integrada en tony/fase-5. Sigue la siguiente de T-F6 en el orden de arriba.

## Después de T-144 (2026-10-17)
- T-144 hecha (sin revisar), integrada en tony/fase-5. Sigue la siguiente de T-F6 en el orden de arriba.

## Después de T-145 (2026-10-17)
- T-145 hecha (sin revisar), integrada en tony/fase-5. Sigue la siguiente de T-F6 en el orden de arriba.

## Después de T-146 (2026-10-17)
- T-146 hecha (sin revisar), integrada en tony/fase-5. Sigue la siguiente de T-F6 en el orden de arriba.

## Después de T-147 (2026-10-17)
- T-147 hecha (sin revisar), integrada en tony/fase-5. Sigue la siguiente de T-F6 en el orden de arriba.

## Después de T-148 (2026-10-17)
- T-148 hecha (sin revisar), integrada en tony/fase-5. Sigue la siguiente de T-F6 en el orden de arriba.

## Después de T-149 (2026-10-17)
- T-149 hecha (sin revisar), integrada en tony/fase-5. Sigue la siguiente de T-F6 en el orden de arriba.

## Después de T-150 (2026-10-17)
- T-150 hecha (sin revisar). Sigue T-151 (recorrido de cierre del bloque de economía). Deuda: CreadorKitReparacion.Crear modifica MechaJugador.prefab cada vez que corre (no es idempotente).

## Después de T-151 (2026-10-17)
- T-151 hecha (sin revisar), integrada en tony/fase-5. Sigue la siguiente de T-F6 en el orden de arriba.

## Parada manual (2026-10-17, a petición de Tony)
- Rama actual: tony/T-151-recorrido-economia (commit f89c2c3, con push). T-151 está TERMINADA y verificada (RecorridoEconomiaPruebas 3/3, Progresion 87/87) pero SIN cerrar: falta mover su archivo a docs/tareas/hechas/, la bitácora, la entrada en docs/revision-tony.md y el merge a tony/fase-7.
- tony/fase-7 llega hasta T-150 (T-140 a T-150 mezcladas y con push). T-140 y T-141 (Core) ya están en main; el resto del bloque de economía NO está en main.
- Falta: cerrar T-151; correr la suite completa del bloque (compilar, EditMode y `LIMITE_AREA=2400 bash herramientas/verificar.sh playmode-por-area`; Progresion roza los 900 s por defecto); dejar en docs/revision-tony.md qué probar jugando; mezclar tony/fase-7 en main con push (autorizado por Tony).
- Sigue: planificar y construir los bloques siguientes de T-F7 (ligas, piezas por ramas y niveles, habilidades, implantes, misiones y reputación, radio, adaptación del rival, hackeo) desde T-152.
- Deudas: CreadorKitReparacion.Crear ensucia MechaJugador.prefab cada vez que corre (no es idempotente); Mecha y UI traen pruebas omitidas por mirar; CambioEscenaPruebas y PrologoPresentePruebas fallan a veces en la primera corrida de Progresion (orden); T-136 y T-137 siguen bloqueadas (Alan aún no coloca IPuntoHistoria ni mis prefabs en Ciudad.unity); tony/fase-6b-alan (main + origin/alan/fase-3, con T-073) no se ha mezclado a main.
- Unity cerrado, sin lockfile y sin procesos míos.

## Después de T-073 (2026-10-17)
- T-073 hecha (sin revisar), integrada en tony/fase-5. Sigue la siguiente de T-F6 en el orden de arriba.
