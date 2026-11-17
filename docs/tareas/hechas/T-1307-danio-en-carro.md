# T-1307: daño dentro del carro y la moto

Origen: docs/playtest-17nov.md, punto 7.

- `PandilleroAPie.EsAtacable`: el jugador en modo Carro (carro o moto) también es blanco; el mecha no.
- `PistolaPandillero.HayLinea`: una bala que pega en el vehículo ocupado del jugador cuenta como que le llega (antes el carro tapaba el tiro).
- `VidaAPie`: en carro recibe `FraccionDanioDisparoEnCarro` (0,55) del daño de disparo; en moto, el daño completo.
- `JugadorAPie`: si cae KO dentro del vehículo, intenta bajar cada 0,25 s (hasta 15 s) y no recupera el control a pie estando KO.
- Prueba: `DanioEnCarroT1307Pruebas.EnCarro_UnDisparoAlVehiculo_BajaVidaDelJugador` (PlayMode, Personaje). No corrida (sin Unity en este worktree).
- Límite: los golpes cuerpo a cuerpo al jugador en carro hacen daño completo.
