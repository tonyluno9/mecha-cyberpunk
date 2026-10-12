# T-057 HUD a pie: objetivo que guía y avisos de interacción
Dueño: Tony | Rama: tony/T-057-hud-objetivo-interaccion | Modelo: Sonnet 5.5, medium
Depende de: T-054 (interacción con puertas), T-055 (servicio de objetivos), T-056 (HUD base)

## Objetivo
El HUD dibuja el objetivo de la historia con una flecha que apunta hacia él y la distancia, y avisa "REGRESA" cuando el jugador
se aleja; además dice qué hace la E ("E · Abrir Bar") y por qué no abrió una puerta ("CERRADO · abre 20:00"). Con esto el
objetivo guía de vuelta durante el prólogo (demo.md) y el HUD a pie queda completo.

## Contexto
- Diseño: docs/diseno/estetica-hud.md (a pie veo vida, objetivo y canción), docs/diseno/demo.md (caso límite del objetivo que
  guía de vuelta), docs/diseno/ciudad.md (lugar cerrado: puerta cerrada y horario).
- Contratos: Core/Objetivos/IObjetivos.cs y UbicacionObjetivo.cs; Core/Mundo/ILugar.cs (`Nombre`, `Horario`),
  HorarioLugar.cs (`Texto()`, `TextoHora`, `HorasHastaAbrir`), ResultadoPuerta.cs.
- Lo de Tony que se lee: `ServicioObjetivos` (Progresion/, T-055: seguimiento con distancia y `SeAleja`) e `InteraccionAPie`
  (Personaje/, T-054: puerta candidata y `PuertaIntentada`). `MechaCyberpunk.UI` pasa a referenciar también
  `MechaCyberpunk.Progresion` (mismo dueño, esta tarea lo pide).
- El HUD toma `Registro<IObjetivos>.Actual`; si es un `ServicioObjetivos` usa su seguimiento; si es otra implementación, muestra
  solo el texto (sin flecha ni "REGRESA"). Propuesta D-tony-21 en docs/decisiones-pendientes-tony.md.

## Qué hacer
1. `ConfigHudAPie`: agrega color del aviso "REGRESA" (magenta), duración del aviso de puerta (2 s), velocidad de pulso de la flecha
   al alejarse, y textos fijos ("E · Abrir", "REGRESA", "CERRADO · PRÓXIMAMENTE", "CERRADO · abre {0}").
2. Lógica pura en `UI/HudAPie/`:
   - `CalculoFlechaObjetivo`: con la posición y el forward de la cámara y la posición del objetivo, el ángulo en grados (con signo,
     en el plano horizontal) para girar la flecha: 0 si está enfrente, positivo a la derecha. Y el texto de distancia ("45 m",
     "1.2 km" desde 1000 m).
   - `TextoInteraccion`: con el tipo de candidato y el nombre del lugar arma "E · Abrir Bar"; con un `ResultadoPuerta` y el
     `HorarioLugar` arma el aviso: `CerradaPorHorario` da "CERRADO · abre 20:00" (`TextoHora`), `Bloqueada` da
     "CERRADO · PRÓXIMAMENTE", `Abierta` y `YaAbierta` no avisan nada. Diseñado para que T-058 sume "E · Subir <nombre>".
3. `HudAPie`: arriba al centro, el texto del objetivo, una flecha (Image que gira) y la distancia; con `SeAleja` el texto se
   antepone con "REGRESA ·" en magenta y la flecha pulsa. Se actualiza con `IObjetivos.Cambio` y el seguimiento (la flecha sí
   cada frame). Sin objetivo, oculto; si la posición no se resuelve, solo el texto.
   Abajo al centro, el aviso de interacción: el de la E mientras haya candidato; tras `PuertaIntentada` con un resultado que avisa,
   ese texto durante la duración del config. El nombre del lugar sale de `Registro<ILugar>` por el `IdLugar` de la puerta.
   Por modo: el objetivo se ve a pie y en carro; la interacción solo a pie; en Mecha nada.
4. Creador de la escena (Personaje/Editor): en Pruebas_APie agrega `ServiciosProgresion.prefab` y un `FijadorObjetivoDePrueba`
   con el Bar como primer objetivo. Regenera.

## Carpetas permitidas
Assets/_Proyecto/UI/ (incluido su asmdef), Assets/_Proyecto/Personaje/Editor/ (solo el creador de la escena),
Assets/_Proyecto/Escenas/Tony/Pruebas_APie.unity, Assets/_Proyecto/Tests/EditMode/UI/ y Assets/_Proyecto/Tests/PlayMode/UI/
No tocar: Core/, el resto de Personaje/ y Progresion/ (solo usarlos), las carpetas y escenas de Alan.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa
- [ ] EditMode CalculoFlechaObjetivo: enfrente 0°, a la derecha +90°, a la izquierda -90°, atrás ±180°; ignora la altura;
      distancias "45 m" y "1.2 km"
- [ ] EditMode TextoInteraccion: "E · Abrir Bar"; `CerradaPorHorario` con horario 20:00 a 06:00 da "CERRADO · abre 20:00";
      `Bloqueada` da "CERRADO · PRÓXIMAMENTE"; `Abierta` y `YaAbierta` no avisan
- [ ] PlayMode: con jugador, HUD, servicio y un `LugarDePrueba` del Bar, al fijar el objetivo "Ve al bar" el texto aparece en el
      mismo frame o el siguiente; la flecha marca el ángulo calculado (±5°) y al girar 90° a la derecha con el ratón simulado la
      flecha gira unos -90° (±10°); la distancia coincide con la del seguimiento (±1 m)
- [ ] PlayMode con entrada simulada: caminando 40 m en dirección contraria al objetivo aparece "REGRESA" en menos de 4 s; al dar
      media vuelta y caminar hacia el objetivo desaparece
- [ ] PlayMode con entrada simulada: frente a la puerta del Bar dice "E · Abrir Bar"; frente al Clandestino, tras E, dice
      "CERRADO · abre 20:00" unos 2 s y luego vuelve a "E · Abrir Clandestino"; frente al Casino, "CERRADO · PRÓXIMAMENTE"
- [ ] PlayMode: con `Quitar()` el objetivo se oculta; con un `JugadorFalso` en modo Carro el objetivo se ve y la interacción no;
      en Mecha nada se ve
- [ ] Capturas en Pruebas_APie: caminando con el objetivo y la flecha (docs/capturas/T-057-1.png), con "REGRESA"
      (T-057-2.png) y frente al Clandestino cerrado (T-057-3.png)
- [ ] Consola sin errores en Pruebas_APie yendo al Bar siguiendo la flecha

## Criterios de sensación (para un humano)
- Siguiendo solo la flecha se llega sin pensar; el "REGRESA" se nota sin ser molesto.
- Los avisos de la puerta se entienden a la primera y no se quedan pegados en pantalla.

## Pruebas automáticas
- EditMode (Tests/EditMode/UI/): CalculoFlechaObjetivoPruebas, TextoInteraccionPruebas.
- PlayMode (Tests/PlayMode/UI/): HudObjetivoPruebas y HudInteraccionPruebas con InputTestFixture; `Registros.LimpiarTodos()` y
  `EventosLugares.LimpiarSuscriptores()` en SetUp y TearDown.

## Fuera de alcance
- Marcadores del objetivo en el mundo, minimapa, ruta por las calles (la flecha apunta en línea recta; el director del prólogo pone
  objetivos intermedios en las esquinas)
- Aviso de subir al carro (T-058); secuencia de objetivos del prólogo (T-F6)
