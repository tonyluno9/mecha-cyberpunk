# T-069 Pandilla de la emboscada: varios rivales que se turnan
Dueño: Tony | Rama: tony/T-069-pandilla-turnos | Modelo: Sonnet 5.5, medium
Depende de: T-068

## Objetivo
Los mechas de la pandilla pelean en grupo contra el jugador sin encimarse: solo uno prepara un golpe a la vez, los demás esperan
un poco más lejos rodeándolo, no se pegan entre ellos y, si todavía no hay a quién perseguir, lo buscan solos. Llevan piedad
(T-068), así que la emboscada no se puede perder.

## Contexto
- Diseño: demo.md ("Sales a la calle y te emboscan ... también sin poder perder"); concepto-historia.md ("La pandilla del
  prólogo resulta trabajar para alguien más grande", propuesta; nombres de pandillas: pregunta abierta).
- Propuesta: D-tony-33 en docs/decisiones-pendientes-tony.md (turno de ataque, margen, búsqueda, datos de la pandilla).
- Lo que ya hay: `RivalBasico` busca a su objetivo una sola vez en `Start` (el `IPeleador` del lado contrario); `CerebroRival`
  decide esperar, acercarse o golpear; `BarridoGolpe.Buscar` devuelve el primer `IRecibeDanio` enfrente que no sea él mismo, y
  puede ser otro rival. `PiedadRival` y `ConfigPiedadRival` (T-068).
- En la emboscada el mecha del jugador puede no estar cerca al principio (llega del cielo): un rival sin objetivo debe seguir
  buscándolo.

## Qué hacer
1. Lógica pura `IA/Rival/TurnosAtaque.cs`: un turno de ataque por lado. `Pedir(rival)` da el turno si está libre o ya es suyo;
   `Soltar(rival)`; si quien lo tiene queda en KO, se desactiva o se destruye, el turno se libera. Una instancia compartida por
   escena (por ejemplo, estática con limpieza al entrar a Play, como `EventosPelea`).
2. `RivalBasico` con un campo `usarTurnos` (falso por defecto, así el rival del arena no cambia): con turnos, solo empieza un golpe
   si tiene el turno, lo suelta al terminar la recarga; sin turno se queda a `distanciaAtaque + margenEspera` (2 m) y rodea al
   objetivo despacio (`velocidadRodeo`) para no quedarse en fila detrás de otro.
3. No pegarse entre ellos: al impactar, si lo que encontró es un `IPeleador` del mismo lado, no le hace daño.
4. Buscar objetivo: si no tiene objetivo o su objetivo se destruyó, lo busca otra vez cada 0.5 s.
5. Datos y prefab de la pandilla con el creador de IA: `DatosMecha_Pandilla.asset` (vida 60, daño 8, un poco más rápido que el de
   prueba), `DatosRival_Pandilla.asset` (alias "PANDILLERO", provisional) y `IA/Prefabs/RivalPandilla.prefab` (otro color, con
   `PiedadRival` y `usarTurnos`).
6. Pruebas:
   - EditMode `Tests/EditMode/IA/TurnosAtaquePruebas.cs`: un solo dueño por lado; soltar; liberar al quedar en KO o destruido;
     lados distintos no se estorban.
   - PlayMode `Tests/PlayMode/IA/PandillaPruebas.cs` (escena armada en tiempo de juego con piso, `MechaJugador` y 3
     `RivalPandilla` con control): a) 60 s de juego con el jugador quieto: nunca hay dos rivales en preparación de golpe en el
     mismo frame, la vida del jugador nunca llega a 0 y ningún rival recibe daño de otro; b) los que no tienen turno se quedan a
     más de su distancia de ataque; c) rivales creados antes que el jugador lo encuentran a los 0.5 s de que aparece; d) un jugador
     simulado que mira al rival más cercano y hace clic seguido deja a los 3 en KO en menos de 90 s.

## Carpetas permitidas
Assets/_Proyecto/IA/, Assets/_Proyecto/Tests/EditMode/IA/, Assets/_Proyecto/Tests/PlayMode/IA/
No tocar: Core/, Combate/, Pelea/, Mecha/, Pruebas_Tony.unity, las carpetas y escenas de Alan.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa, con las pruebas del rival de arena sin cambios
- [ ] TurnosAtaquePruebas y PandillaPruebas pasan (casos a a d)
- [ ] Captura de 3 rivales de la pandilla rodeando al jugador, desde la cabina: docs/capturas/T-069-1.png
- [ ] Consola sin errores en las pruebas

## Criterios de sensación (para un humano)
- Contra 2 o 3 pandilleros, ¿se siente como pelear contra un grupo que se turna (de película) y no como una fila? ¿Se lee quién
  va a pegar?

## Pruebas automáticas
- EditMode: TurnosAtaquePruebas. PlayMode: PandillaPruebas.

## Fuera de alcance
- Cuándo y dónde aparece la pandilla y cuándo se activa (T-071, director de la emboscada)
- Pandilleros a pie, armas de fuego de la pandilla, nombres definitivos
