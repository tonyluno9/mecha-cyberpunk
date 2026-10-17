# T-133 Director del prólogo, parte 2: despertar, pelea casera y "continuará"
Dueño: Tony | Rama: tony/T-133-director-prologo-presente | Modelo: Sonnet 5.5, medium
Depende de: T-129, T-131, T-132

## Objetivo
Después del flashback el jugador despierta en el presente: en su cama, al anochecer, con poco dinero y el mecha fregado. El director
pone el estado inicial de la historia, lo guía al clandestino (con la opción de reparar antes en el taller) a su primera pelea casera,
que puede ganar o perder, y al terminar muestra "CONTINUARÁ" con la opción de seguir jugando o volver al título. Cierra la demo del lado
de Tony.

## Contexto
- Diseño: concepto-historia.md ("el prólogo es el futuro"; "empiezas con el mecha fregado"; "al llegar a la noche del prólogo el juego no
  termina: es una bisagra"), demo.md (paso 6 y preguntas abiertas), economia-misiones.md (vivir al día; quedar en negativo es intencional),
  ciudad.md (el clandestino abre de noche; un día son 20 minutos reales).
- Decisiones: D-tony-44 (dinero inicial 500, reparación 10 por punto), D-tony-46 (reparar en el banco aunque no alcance), D-tony-47 (renta
  cada 7 días). Propuestas nuevas: D-tony-58 (estado inicial al despertar) y D-tony-70 (continuará).
- Piezas: `DirectorPrologo`, `DatosPrologo`, `MaquinaPrologo` (T-132), `MostradorPeleas` con su evento `PeleaTerminada` (T-129),
  `PantallaContinuara` y `PantallaFundido` (T-125), `PantallaSubtitulos` (T-131, para un aviso de una línea), `ServicioCartera`
  (`Restablecer`), `ServicioRenta`, `MechaPersistente`, `ServicioCalor`, `CambioEscena`, `DatosEscenas`.

## Qué hacer
1. `DatosPrologo`: hora de despertar (19:30), fracción de vida del mecha al despertar (0.45: grietas leves, sin alarma), dinero inicial
   (el de `DatosEconomia`), aviso al despertar ("Tu mecha quedó fregado. Puedes repararlo en el banco del taller antes de pelear."),
   objetivos "Ve al clandestino: tu primera pelea casera" (lugar Clandestino) y "Habla con el encargado (E)" (punto de uso del
   `MostradorPeleas`). `MaquinaPrologo` suma estas etapas.
2. Lo que el director necesita de otras áreas, sin cambiar lo que ya hacen: `ServicioRenta.Reiniciar(int diaActual)` (próximo cobro =
   día actual + 7, adeudo 0, avisa `Cambio`), `MechaPersistente.FijarFraccionVida(float)` (fija la vida a esa fracción de la máxima, sin
   bajar del piso de emergencia), y lo que haga falta para borrar el calor (`ICalor`/`ServicioCalor`).
3. `DirectorPrologo`, etapas nuevas:
   - `Despertar` (una vez): jugador a pie en "Casa_Cama"; dinero = inicial (con `Restablecer`, sin aviso de premio), mecha a la fracción de
     despertar y en su taller, renta reiniciada, calor a 0, reloj a la hora de despertar; `Avanzar(PeleaCasera)`; guardar; fundido de entrada y
     el aviso de una línea durante 6 s.
   - `PeleaCasera`: objetivo al clandestino; al entrar al clandestino, objetivo al mostrador; al `PeleaTerminada` del mostrador →
     `Continuara`.
   - `Continuara`: quita el objetivo y muestra `PantallaContinuara` con el alias, el dinero y si ganó. "Seguir jugando" → `Libre` (sin
     objetivo, control normal); "Salir al título" → guardar y `CambioEscena.Ir(RutaPara(CreacionPersonaje))`.
   - Reanudar: `PeleaCasera` → jugador en "Casa_Cama" con su objetivo (sin repetir el estado inicial); `Continuara` → vuelve a mostrar la
     pantalla; `Libre` → nada.
4. PlayMode `Tests/PlayMode/Progresion/PrologoPresentePruebas.cs` en Pruebas_Prologo: prepara una partida (con `RutaForzada` temporal) en
   etapa `Despertar` con el mecha a vida completa y dinero 2000, y entra con una transición real (`CambioEscena.Ir` a Pruebas_Prologo). Entrada
   simulada para caminar, E y pelear; se permite rematar al rival tras daño real y acortar caminatas de más de 40 m con `IJugador.Colocar`
   (anotado). Si al llegar al mostrador todavía es de día para el reloj, la prueba lo adelanta con `AvanzarHasta(20.5)` (atajo anotado).
   Para "Salir al título" usa `CambioEscena.Interceptar`.

## Carpetas permitidas
Assets/_Proyecto/Progresion/Prologo/ (Datos/, Prefabs/, Editor/), Assets/_Proyecto/Economia/Renta/ServicioRenta.cs (y su modelo si hace
falta), Assets/_Proyecto/Mecha/Estado/MechaPersistente.cs, Assets/_Proyecto/Progresion/Calor/ (solo si falta borrar el calor),
Assets/_Proyecto/Escenas/Tony/Pruebas_Prologo.unity (con su creador), Assets/_Proyecto/Tests/EditMode/ y Tests/PlayMode/ de Progresion,
Economia y Mecha, docs/capturas/
No tocar: Core/, todo lo de Alan, otras escenas.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa
- [ ] EditMode: `ServicioRenta.Reiniciar` / su modelo (próximo cobro y adeudo), `MechaPersistente.FijarFraccionVida` (respeta el piso) y
      los casos nuevos de `MaquinaPrologoPruebas`
- [ ] PrologoPresentePruebas:
      a) al entrar: jugador a menos de 0.5 m de "Casa_Cama", dinero 500 (no 2000), vida del mecha 45 % (±0.5) y en el taller, renta con
         próximo cobro día actual + 7 y adeudo 0, calor 0, hora 19:30 (±0.1), etapa `PeleaCasera` guardada, aviso visible;
      b) reparar en el banco deja el dinero en 500 − costo (negativo si no alcanza) y la vida al máximo;
      c) al entrar al clandestino el objetivo pasa al mostrador; E → pelea casera; gana: "continuará" con el alias, el dinero y "Ganaste";
         Enter tras 4 s → etapa `Libre`, sin objetivo y el jugador se mueve con W;
      d) otra corrida en la que pierde (mecha al 20 % antes de entrar): "continuará" dice "Perdiste"; Esc pide la escena del título;
      e) reanudar con una partida en `PeleaCasera` no vuelve a poner el dinero en 500
- [ ] Consola sin errores; un solo `AudioListener`
- [ ] Captura docs/capturas/T-133-1.png: despertar en la casa con el aviso y el objetivo

## Criterios de sensación (para un humano)
- Pruebas_Prologo con una partida en `Despertar` (o el recorrido de T-135): ¿despertar sin el dinero del campeonato se entiende como "eso
  fue el futuro"? ¿El aviso del mecha fregado basta para que pienses en repararlo? ¿Ganar o perder la casera y ver "continuará" se siente
  como final de demo y no como corte?
- ¿19:30 deja tiempo justo para reparar y llegar cuando abre el clandestino (20:00)?

## Pruebas automáticas
- EditMode: casos nuevos de renta, mecha persistente y MaquinaPrologoPruebas. PlayMode: PrologoPresentePruebas.

## Fuera de alcance
- Contenido después de la demo (ligas, misiones, T-F7); morir en la calle; el recorrido completo desde la creación (T-135)
