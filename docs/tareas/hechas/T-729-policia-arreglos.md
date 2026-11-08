# T-729 Arreglos de la policía: tamaño, a quién ataca, estrellas, silbato y patrullas
Dueño: Tony (S1, noche 08-nov) | Rama: tony/T-729-policia-arreglos | Modelo: Sonnet 5.5
Depende de: T-709, T-665, T-707

## Objetivo
Arreglar lo que Tony vio del lado de la policía en el playtest del 7-nov: policías diminutos, atacan al jugador en vez de al criminal, 5 estrellas sin hacer nada, estrellas que parpadean siempre, silbato y patrullas que giran sobre su eje.

## Qué se hizo
- **1.80 m**: los policías a pie medían 1.23 m animados (BakeMesh, PoliciaAlturaT729Pruebas). `EscalarPoliciaT729` (IA/Editor) escala Giro/Modelo de Visual_Policia por 1.465 y la prueba mide 1.80 m (±0.06) animado.
- **Atacan primero al criminal**: `PandilleroAPie.CriminalMasCercano()`: un pandillero que no es policía, está vivo y apunta ahora mismo a alguien (el asaltante, un SKULL que dispara) a menos de 1.5 veces el alcance de tiro. Si hay, el policía lo toma como blanco (rodea, se cubre y dispara contra él) y no le apunta al jugador; sin criminales sigue por el jugador. Lo que cae por balas de la policía (`CayoPorLaPolicia`) no suma búsqueda (`PandillaAPie.AlVencerUno`).
- **Dejas de ser buscado**: `ReglaBusquedaPolicial` ya no depende del círculo: si nadie (patrulla, dron o cámara) te ve durante `ConfigPersecucion.SegundosParaEscapar` (45 s, antes 10 s dentro de un círculo que casi nunca se salía), la policía se retira. El círculo solo se dibuja en el mapa. Antes, con el calor sostenido por `CalorConTestigos.Mantener`, la búsqueda no terminaba si seguías dentro del círculo de 90 m.
- **Estrellas**: solo parpadean cuando te están por perder, los últimos 8 s antes de los 45 (`ReglaEstrellasH10.DebeParpadear`, `HudH10` cuenta cuánto llevas sin que te vea la policía con `IZonaBusqueda.Visto`). El HUD (UI) no ve Trafico: la constante 37 s se cuida con una prueba contra `ReglaBusquedaPolicial.SegundosAviso`.
- **Silbato**: el único silbido que suena con la policía es ARM_BALA_ROZA (barrido agudo, la bala del dron que roza al jugador; el policía a pie nunca silba: nace en alerta y PAN_RIVAL_ALERTA solo suena al cambiar a alerta). Se apagó en el catálogo (`SilenciarSilbatoT729`). **No pude oír el audio**: si el silbato que oyó Tony era otro sonido, hay que decirme cuál.
- **Patrullas (NO HECHO)**: se probó un modelo de giro igual al del carro del jugador en PatrullaMundo, pero rompió 10 pruebas de patrullas de Trafico y por el cambio de prioridad no se terminó. Se revirtió; falta una tarea propia.

## Tabla de causas: de dónde sube la búsqueda
Medido en Ciudad_Prologo con CausasDeCalorT729Pruebas (cada delito pasa por TestigosDelito.DelitoEvaluado con quién lo llamó):
- 30 s de asalto completo con el jugador parado (cinemática, carro que huye, asaltante): **0 delitos, calor 0**.
- Robar un carro del tráfico a plena calle con un civil mirando: **+30 (CarroEstacionado.AlCambiarOcupado, Pendiente)** que sube cuando termina la llamada del civil (2 estrellas).

| Fuente | Cuánto | Cuándo es válida | Qué se hizo |
| --- | --- | --- | --- |
| Disparar la pistola en la calle (`PistolaAPie`) | +2 cada 0.5 s | Es del jugador | Nada |
| Dañar o matar a un policía (`PandilleroAPie`, datos Policia) | +8 por bala, +30 al matar | Solo si lo hace el jugador (los policías no se disparan entre sí) | Nada; el daño no dice quién lo hizo (InfoDanio.Atacante es null) |
| Pandillero vencido (`PandillaAPie.AlVencerUno`, +8) | +8 | Solo si lo vence el jugador | **Falsa si lo vence la policía** (ahora que dispara a los SKULLS): se quitó con `CayoPorLaPolicia` |
| Robar un carro (`CarroEstacionado`, 30 o 55) | +30/+55 | Solo con testigos (CalorConTestigos) | Nada; medido: 0 sin testigos, 30 con uno |
| Explosión de un carro cerca (`DanioCarro`) | CalorExplosion | Si el jugador la causó | Pendiente: no distingue quién la causó (necesita atacante en InfoDanio, Core) |
| Guardias de Nova (`GuardiaNova`) al oír disparos | calorAlReaccionar por alerta | Si dispara el jugador | Pendiente: oyen disparos de cualquiera (EventosRuido no dice quién) |
| Mecha en la calle (`CalorPorMecha`) | por tiempo | Es del jugador | Nada |
| Pasos de misión con calor (`ServicioMisiones`) | del paso | Es de la misión | Nada |
| Calor sostenido (`CalorConTestigos.Mantener`) | 0, solo sostiene el piso | Mientras te buscan | **Causa probable de "5 estrellas pegadas"**: el calor no bajaba y la búsqueda no terminaba dentro del círculo. Resuelto con la regla de 45 s sin que te vean |
| Con nivel mayor que 0, todo delito pasa sin testigos | cada delito suma directo | Es el diseño de T-661 | Nada, pero explica que una pelea con la policía suba rápido (2 por disparo, 8 por SKULL) |

No pude reproducir en la ciudad 5 estrellas sin hacer nada: la cinemática, el carro del asalto y el asaltante no suman. La explicación más probable es la combinación de las dos últimas filas más disparos propios (cada 30 balas son 60 puntos, 3 estrellas) con un calor que no bajaba nunca. Si vuelve a pasar con las estrellas altas sin disparar, el log `[T-729]` de CausasDeCalorT729Pruebas se puede copiar a una partida para ver quién suma.

## Criterios
- [x] Los policías animados miden 1.80 m (BakeMesh)
- [x] Con un SKULL disparando al jugador, la policía le apunta y dispara al SKULL, no al jugador
- [x] Lo que la policía derriba no suma búsqueda al jugador
- [x] Sin que te vea nadie 45 s dejas de ser buscado (también dentro del círculo)
- [x] Las estrellas solo parpadean cuando faltan 8 s para perderte
- [x] Sin silbato (ARM_BALA_ROZA apagado; falta confirmar con el oído)
- [ ] Las patrullas giran con el modelo del carro del jugador (pendiente)

## Pruebas
- EditMode Trafico: ReglaBusquedaPolicialPruebas (7, reescritas). EditMode UI: HudH10ReglasPruebas (1 nueva). EditMode Sonido: SonidoMinimoT620Pruebas (1 nueva).
- PlayMode IA: PoliciaAlturaT729Pruebas (1), PoliciaCriminalT729Pruebas (2), EstrellasBusquedaT729Pruebas (1), CausasDeCalorT729Pruebas (1, la medición de arriba). PlayMode Trafico: BusquedaT665Pruebas actualizada a la regla nueva.
