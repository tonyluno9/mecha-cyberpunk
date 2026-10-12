# T-042 Locales con puertas, horarios y letreros
Dueño: Alan | Rama: alan/T-042-locales-puertas-horarios | Modelo: Sonnet 5.5, medium
Depende de: T-040 (barrio y cámara libre), T-041 (reloj). Pasos 4 a 8: T-032 en main (`IdLugar`, `HorarioLugar`, `ILugar`,
`IPuerta`, `EventosLugares`) y la parte B de T-041.

## Objetivo
Los cinco locales del barrio (casa, bar, clandestino, casino y mercado negro) tienen fachada, puerta que se abre y un letrero con
su nombre, su horario y si está abierto. De noche el clandestino abre y de día está cerrado; el casino y el mercado negro están
cerrados en la demo. Las puertas cumplen `IPuerta` para que el jugador de Tony las use en T-F3 sin cambiar nada.

## Contexto
- Diseño: docs/diseno/ciudad.md (de día el clandestino cerrado y de noche abierto; un lugar cerrado tiene la puerta cerrada y un
  letrero con el horario; de día abre el mercado negro, de noche el bar, el clandestino y el casino), docs/diseno/demo.md
  (casino y mercado negro se ven por fuera y están cerrados), docs/diseno/estetica-hud.md (neón azul, magenta, naranja, turquesa;
  los nombres de los locales están sin confirmar: usa nombres de trabajo).
- Decisiones: D-010 (texto con uGUI `Text` en Canvas World Space). Propuestas: D-alan-5 (horarios), D-alan-6 (la puerta deja
  salir a quien está dentro), D-alan-8 (interacción de prueba con E) en docs/decisiones-pendientes-alan.md.
- Contrato: lee los comentarios de `ILugar`, `IPuerta`, `HorarioLugar` y `EventosLugares` en `Core/Mundo/` de main.
  `HorarioLugar.EstaAbierto` ya resuelve los horarios que cruzan la medianoche: úsalo, no lo reescribas.
- Interiores: solo el bar tiene interior en esta fase (T-043). Detrás de las puertas de la casa y del clandestino hay un
  vestíbulo oscuro y cerrado de unos 3 m (sus interiores son de A-F3).
- Cómo saber si llegó T-032: `git show main:Assets/_Proyecto/Core/Mundo/ILugar.cs`.

## Qué hacer

### Sin esperar a Tony
1. Fachada de cada local en el barrio (con el creador de T-040): marco de puerta, hoja de puerta con collider, vestíbulo
   (casa y clandestino) y un letrero sobre la puerta con Canvas World Space y dos `Text` (nombre y horario, y estado).
2. `Mundo/Lugares/HojaPuerta.cs`: abre (gira o se desliza en la duración del config) y cierra; abierta, su collider deja pasar;
   se cierra sola después de unos segundos si nadie está en el umbral (trigger del umbral). Valores en
   `Mundo/Datos/ConfigPuertas.cs` con su asset.
3. `CamaraLibre`: acción Interactuar (E) que lanza un rayo de 3 m al frente y, si da con una puerta, le pide abrir y escribe el
   resultado en la consola ("[Mundo] Puerta Bar: Abierta").

### Cuando T-032 esté en main
4. `Mundo/Datos/DatosLugar.cs` (ScriptableObject): `IdLugar`, nombre del letrero, `HorarioLugar`, `cerradoEnDemo`. Assets en
   `Mundo/Datos/`: casa (siempre), bar (19:00 a 04:00), clandestino (20:00 a 06:00), casino (20:00 a 06:00, cerrado en demo) y
   mercado negro (09:00 a 18:00, cerrado en demo).
5. Lógica pura `Mundo/Lugares/ReglaPuerta.cs`: con si el lugar está abierto por horario, si está cerrado en la demo, si el jugador
   está dentro y si la puerta ya está abierta, decide el `ResultadoPuerta`: ya abierta da `YaAbierta`; jugador dentro abre
   siempre (para que pueda salir); cerrado en demo da `Bloqueada`; fuera de horario da `CerradaPorHorario`; si no, `Abierta`.
6. `Mundo/Lugares/LugarMundo.cs : MonoBehaviour, ILugar` en cada local: lee el reloj de `Registro<IRelojMundo>`; `EstaAbierto` =
   no cerrado en demo y horario abierto; `PosicionEntrada` es su hijo `Entrada`; avisa `EstadoCambio` y
   `EventosLugares.PublicarEstadoLugarCambio` solo cuando cambia; se agrega a `Registro<ILugar>`.
   `JugadorDentro` lo cambia la zona del interior (T-043); por ahora queda en false.
7. `Mundo/Lugares/PuertaLocal.cs : MonoBehaviour, IPuerta`: usa `ReglaPuerta` y `HojaPuerta`; se agrega a `Registro<IPuerta>`.
8. `Mundo/Lugares/LetreroLugar.cs`: nombre y `HorarioLugar.Texto()`, y "ABIERTO" en turquesa o "CERRADO" en magenta;
   los cerrados en demo dicen "CERRADO · PRÓXIMAMENTE". Se actualiza con `EstadoCambio`, no en cada frame.
   La cámara libre interactúa ahora por `IPuerta`.

## Carpetas permitidas
Assets/_Proyecto/Mundo/, Assets/_Proyecto/Tests/EditMode/Mundo/, Assets/_Proyecto/Tests/PlayMode/Mundo/,
Assets/_Proyecto/Escenas/Ciudad.unity y Assets/_Proyecto/Escenas/Pruebas_Alan.unity
No tocar: Core/, Arena/, las carpetas y escenas de Tony, ProjectSettings/.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa
- [ ] PlayMode con entrada simulada (sin T-032): frente a una puerta, E la abre y caminando 4 m con W la cámara pasa el umbral;
      unos segundos después, sin nadie en el umbral, la puerta se cierra sola y ya no deja pasar
Con T-032:
- [ ] EditMode ReglaPuerta: todos los casos del paso 5, incluido "fuera de horario pero el jugador dentro: abre"
- [ ] PlayMode con entrada simulada a las 22:00: frente al bar, E da `Abierta` y la cámara entra; frente al clandestino, E da
      `Abierta`
- [ ] PlayMode con entrada simulada a las 12:00: frente al clandestino, E da `CerradaPorHorario` y caminando 4 s con W la cámara
      no pasa la puerta
- [ ] PlayMode a las 22:00: casino y mercado negro dan `Bloqueada`; la casa abre a las 12:00 y a las 22:00
- [ ] PlayMode: avanzando el reloj de las 05:30 a las 06:30, el clandestino avisa `EstadoCambio(false)` y
      `EventosLugares.EstadoLugarCambio(Clandestino, false)` una sola vez, y su letrero cambia a "CERRADO"
- [ ] PlayMode: los cinco lugares están en `Registro<ILugar>` con su `IdLugar` y su `PosicionEntrada` frente a la puerta (a menos
      de 3 m de ella, del lado de la calle); las cinco puertas en `Registro<IPuerta>`
- [ ] Capturas de la fachada del clandestino a las 12:00 (CERRADO, con su horario) y a las 22:00 (ABIERTO), y del casino:
      docs/capturas/T-042-1.png a -3.png; el letrero se lee desde la banqueta de enfrente
- [ ] Consola sin errores en 3 minutos con el reloj a ×30
- [ ] Si T-032 no estaba en main, la tarea queda en bloqueadas/ después de los pasos 1 a 3, con lo que falta anotado

## Criterios de sensación (para un humano)
- Abrir una puerta con E se siente inmediato y la puerta no estorba al pasar.
- Los letreros se leen de un vistazo y se ve a distancia qué está abierto; el clandestino cerrado de día se entiende.

## Pruebas automáticas
- EditMode (Tests/EditMode/Mundo/): ReglaPuertaPruebas.
- PlayMode (Tests/PlayMode/Mundo/): PuertasPruebas y LugaresPruebas, con entrada simulada y el reloj movido con `AvanzarHasta`.

## Fuera de alcance
- Interior del bar y la zona que marca `JugadorDentro` (T-043)
- Interiores de la casa y del clandestino (A-F3); casino y mercado negro abiertos (A-F5)
- Interacción del jugador de Tony con las puertas (T-F3)
