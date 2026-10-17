# T-134 Ayudas de controles para una persona nueva
Dueño: Tony | Rama: tony/T-134-ayudas-controles | Modelo: Sonnet 5.5, medium
Depende de: T-121 (las dos regeneran `MechaJugador.prefab`; ningún contrato nuevo)

## Objetivo
Una persona que nunca jugó termina la demo sin que nadie le diga las teclas: la primera vez que camina, maneja, pelea en el mecha o va
en el mecha por la calle, ve una línea corta con los controles que necesita, que se va sola cuando ya los usó. A pie va en el HUD; en el
mecha va en una pantalla de la cabina (nada flota en el mecha).

## Contexto
- Hito H6: "una persona nueva termina la demo en unos 15 minutos sin ayuda". El encendido ya se explica en su pantalla (T-065); la pelea
  tutorial es la primera vez que la persona usa los brazos.
- Decisiones: D-011 (en el mecha todo es diegético), D-050 (controles a pie), D-052 (HUD a pie por modo), D-061 (encendido), D-012 (golpe,
  guardia y bloqueo perfecto). Propuesta nueva: D-tony-71.
- Piezas: `HudAPie` (UI/HudAPie/), pantallas de cabina (UI/Cabina/, por ejemplo `PantallaCalorCabina` y su creador en Mecha/Editor/),
  `EventosPelea` (Core), `IJugador.ModoCambio`, `GolpeMecha`/`GuardiaMecha` (Combate) y `ControladorAPie` (Personaje).
- El carro de Alan puede usar otras teclas que `CarroDePrueba` (flechas): el texto del carro es dato y T-136 lo ajusta.

## Qué hacer
1. `UI/Datos/ConfigAyudas.cs` + `ConfigAyudas_Base.asset`: una línea por contexto y cuánto dura como máximo (12 s):
   - A pie: "WASD caminar · SHIFT correr · ESPACIO saltar · E usar".
   - Carro: "FLECHAS manejar · E bajar (detenido)".
   - Mecha en pelea: "CLIC IZQ golpe · CLIC DER bloquear · justo antes del golpe = bloqueo perfecto · WASD mover".
   - Mecha en la calle (ya encendido): "WASD mover · RATÓN mirar · E bajar (quieto)".
   - Llamar al mecha (la primera vez que el objetivo pide llamarlo o el jugador está en un punto de llamado): "M · llamar a tu mecha".
2. `UI/HudAPie/AyudaControles.cs` (en `HudAPie.prefab`, con su creador): muestra la línea del contexto la primera vez en la sesión que el
   jugador entra a ese modo, abajo a la izquierda sin tapar vida, objetivo ni avisos; se oculta a los 12 s o cuando la persona ya hizo lo que
   dice (a pie: 3 s caminando; carro: 3 s manejando; llamar: pulsó M). Lo ya visto se recuerda en la sesión de juego aunque cambie la escena
   (estático, limpio al entrar a Play), así no reaparece después del flashback.
3. `UI/Cabina/PantallaAyudaCabina.cs` (en `MechaJugador.prefab`, con su creador; una pantalla diegética de la cabina que no tape la vista
   central ni las otras pantallas): la primera pelea de la sesión (`EventosPelea.PeleaIniciada`) muestra la línea de pelea hasta que la
   persona dio 3 golpes y subió la guardia 1 vez, o 20 s; la primera vez que el mecha queda encendido en la calle muestra la línea de calle
   12 s.
4. Pruebas PlayMode con entrada simulada: `Tests/PlayMode/UI/AyudasControlesPruebas.cs` (Pruebas_APie: la ayuda a pie aparece al arrancar;
   tras 3 s caminando con W se oculta; con otro jugador instanciado en la misma sesión no reaparece; subir al carro de prueba muestra la del
   carro) y `Tests/PlayMode/Mecha/AyudaCabinaPruebas.cs` (Pruebas_Tony: P arranca la pelea y la ayuda de pelea se ve en la cabina; tras 3
   clics izquierdos y 1 clic derecho mantenido se oculta; la segunda pelea de la sesión no la muestra).

## Carpetas permitidas
Assets/_Proyecto/UI/ (Datos/, HudAPie/, Cabina/, Editor/, Prefabs/HudAPie.prefab con su creador), Assets/_Proyecto/Mecha/Editor/ (creador
de la pantalla en la cabina) y Mecha/Prefabs/MechaJugador.prefab (con su creador), Assets/_Proyecto/Tests/PlayMode/UI/,
Tests/PlayMode/Mecha/, docs/capturas/
No tocar: Core/, todo lo de Alan, escenas (las pruebas las cargan sin guardarlas), los .inputactions.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa (HudAPiePruebas y CabinaPruebas siguen en verde)
- [ ] AyudasControlesPruebas y AyudaCabinaPruebas pasan
- [ ] La ayuda a pie no se superpone con vida, objetivo, dinero ni avisos de interacción a 1920×1080 y a 1280×720 (rectángulos medidos en la
      prueba)
- [ ] La pantalla de ayuda de la cabina queda fuera del 40 % × 40 % central de la vista (medido en la prueba, como las grietas de T-105)
- [ ] Capturas docs/capturas/T-134-1.png (HUD a pie con la ayuda) y T-134-2.png (cabina en la pelea con la ayuda)
- [ ] Consola sin errores

## Criterios de sensación (para un humano)
- Pide a alguien que no conozca el juego que abra Pruebas_Prologo y juegue sin decirle nada: ¿encuentra cómo caminar, usar, pelear y
  bloquear solo con las ayudas? ¿Las ayudas se van antes de estorbar?

## Pruebas automáticas
- PlayMode: AyudasControlesPruebas, AyudaCabinaPruebas.

## Fuera de alcance
- Remapear teclas, un menú de controles, ayudas para T-F7; las teclas del carro de Alan (se ajustan en T-136)
