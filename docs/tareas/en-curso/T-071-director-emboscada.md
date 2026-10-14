# T-071 Director de la emboscada
Dueño: Tony | Rama: tony/T-071-director-emboscada | Modelo: Sonnet 5.5, medium
Depende de: T-064, T-065, T-066, T-069 (y T-067 antes en el orden)

## Objetivo
`Emboscada.prefab` es lo que Alan coloca en el callejón: cuando el jugador llega, aparece la pandilla, el objetivo le dice que
llame a su mecha, y los pandilleros solo pelean mientras el jugador va dentro del mecha. Cuando todos caen, la emboscada termina y
avisa (lo usará el director del prólogo en T-F6). Con la piedad de la pandilla, no se puede perder ni a pie ni en el mecha.

## Contexto
- Diseño: demo.md (paso 3: "Sales a la calle y te emboscan. Llamas al mecha y aprendes el encendido, también sin poder
  perder"); mecha-taller.md (en plena emboscada el encendido dura lo mismo y eres vulnerable). Hoja de ruta: H4.
- D-029: Tony entrega en un prefab propio lo que va dentro de una escena de Alan; Alan lo coloca sin editarlo. Propuesta:
  D-tony-35 en docs/decisiones-pendientes-tony.md.
- Contratos que usa (todos de Core): `Registro<IJugador>` (posición y `Modo`), `IPeleador` (`ActivarControl`, `Vida.KO`),
  `Registro<IObjetivos>`, `Registro<IPuntoLlamadoMecha>` (para apuntar el objetivo al punto de llamado más cercano). Pelea no
  referencia IA ni Mecha: los rivales se crean desde un prefab (`RivalPandilla.prefab`, T-069) y se tratan como `IPeleador`.
- Por qué los rivales solo pelean con el jugador en el mecha: un mecha contra una persona a pie la mataría de un golpe y el
  barrido del rival también daña a quien esté enfrente (`VidaAPie` es `IRecibeDanio`). Si el jugador se baja a media pelea,
  los rivales se detienen hasta que vuelva a subir.

## Qué hacer
1. Lógica pura `Pelea/Emboscada/MaquinaEmboscada.cs`: estados `Esperando`, `Aparicion`, `EsperandoMecha`, `Peleando`, `Pausada`
   (jugador fuera del mecha a media pelea) y `Superada`. Entradas por `Avanzar(dt, jugadorCerca, modoJugador, rivalesEnPie)`.
   Eventos `Iniciada`, `EstadoCambio`, `Terminada` (una sola vez). Si el jugador ya llega en el mecha, salta `EsperandoMecha`.
2. `DatosEmboscada` (Pelea/Datos/): prefab del rival, poses de los rivales relativas al prefab (dos por defecto, a unos 14 m al
   fondo, +Z del prefab), `radioActivacion` 12 m, segundos de aparición 2, si fija objetivos y sus textos ("Llama a tu mecha (M)",
   "Sube a tu mecha", "Derrota a la pandilla").
3. `Pelea/Emboscada/DirectorEmboscada.cs` en `Pelea/Prefabs/Emboscada.prefab` (creado con el creador de Pelea): con
   `activarAlAcercarse` se activa sola cuando el jugador entra al radio; si no, con `Empezar()`. Solo una vez. En `Aparicion`
   instancia los rivales en sus poses, sin control. Con objetivos activados, fija el objetivo en la `Llegada` del punto de llamado
   más cercano (o sin posición si no hay ninguno) y lo cambia por fase. Da y quita el control de los rivales según el modo del
   jugador. `Superada` cuando todos los rivales están en KO; quita su objetivo. Gizmos del radio y de las poses de los rivales,
   para que Alan vea dónde aparecen al colocarlo.
4. En Pruebas_Calle, con `CreadorEscenaCalle`: `Emboscada.prefab` en el callejón del fondo, mirando hacia dentro del callejón, y un
   `PuntoLlamadoDePrueba` junto a la entrada del callejón cuya zona cubra la entrada.
5. Pruebas:
   - EditMode `Tests/EditMode/Pelea/MaquinaEmboscadaPruebas.cs`: todas las transiciones; `Terminada` una vez; llegar ya en el
     mecha; bajar a media pelea pausa y subir reanuda.
   - PlayMode `Tests/PlayMode/Pelea/DirectorEmboscadaPruebas.cs` (Pruebas_Calle; puede subir y bajar al jugador con
     `JugadorAPie.Subir/Bajar` y completar el encendido por API, porque el recorrido con entrada simulada es T-072):
     a) lejos no pasa nada; al entrar al radio aparecen 2 rivales a menos de 0.1 m de sus poses y sin control; b) 10 s a pie
     cerca de ellos: no se mueven ni le pegan; c) el objetivo apunta al punto de llamado; d) con el jugador en el mecha encendido
     los rivales tienen control y se acercan; e) al bajar, se detienen en menos de 0.2 s; f) dejarlos en KO (daño directo por
     `IRecibeDanio`) da `Terminada` una vez y quita el objetivo; g) con `activarAlAcercarse` falso solo empieza con `Empezar()`.

## Carpetas permitidas
Assets/_Proyecto/Pelea/, Assets/_Proyecto/Mecha/Editor/CreadorEscenaCalle.cs, Assets/_Proyecto/Escenas/Tony/Pruebas_Calle.unity
(con su creador), Assets/_Proyecto/Tests/EditMode/Pelea/, Assets/_Proyecto/Tests/PlayMode/Pelea/
No tocar: Core/, IA/ (solo se usa RivalPandilla.prefab), Mecha/ salvo CreadorEscenaCalle, Personaje/, Pruebas_Tony.unity,
las carpetas y escenas de Alan.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa
- [ ] MaquinaEmboscadaPruebas y DirectorEmboscadaPruebas pasan (casos a a g)
- [ ] ReglasEnsamblados pasa: Pelea sigue sin referenciar IA ni Mecha
- [ ] Captura de la pandilla recién aparecida en el callejón, a pie, con el objetivo "Llama a tu mecha (M)" en el HUD:
      docs/capturas/T-071-1.png
- [ ] Captura de la escena en el editor con los gizmos del prefab visibles: docs/capturas/T-071-2.png
- [ ] Consola sin errores en las pruebas

## Criterios de sensación (para un humano)
- Al entrar al callejón, ¿la aparición de la pandilla se siente como una emboscada (amenaza) y queda claro que hay que llamar al mecha?
- ¿Que los pandilleros esperen mientras el mecha llega se ve creíble, o parece que el juego se congeló?

## Pruebas automáticas
- EditMode: MaquinaEmboscadaPruebas. PlayMode: DirectorEmboscadaPruebas.

## Fuera de alcance
- El recorrido completo con entrada simulada y la prueba de cierre (T-072); el callejón real de Alan (T-073)
- Diálogos de la pandilla, cinemática de la emboscada, recompensas
