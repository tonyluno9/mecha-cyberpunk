# T-1335: robar carro alineado a la puerta (pendiente de playtest 17-nov)

Rama `tony/T-1335-robar-carro`. Escrito sin abrir Unity: NADA está compilado ni probado todavía.

## Qué quedó
- `AnimacionCarro` (Personaje/Jugador): si al subir hay un `ConductorRobado` cerca del asiento y existe `ConfigAnimacionParkour.CarroSacarConductor`, el modelo del jugador se interpola 0.15 s al punto de la puerta del conductor (PuntoSubida arrimado al asiento, no la puerta opuesta), mira al asiento y juega PJ01_Carro_SacarConductor con `ReproductorClipUnico` acelerado a 1.25 s (lo que dura el del conductor, así empiezan y terminan juntos). Luego Carro_Entrar como antes.
- No hay estado de animator nuevo (el clip va por PlayableGraph): no hay `Aplicar()` que correr.
- Sin conductor (carro estacionado, moto, mecha) todo queda igual.

## Pruebas
PlayMode `RobarCarroSacarConductorT1335Pruebas` (Tests/PlayMode/Personaje).

## Pendiente
- Revisar en captura que jugador y conductor no se traslapen: el conductor cae en PuntoSubida y el jugador queda más cerca del asiento.
