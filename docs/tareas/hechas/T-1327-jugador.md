# T-1327: jugador, atropello, golpe dirigido, daño de caída y escaleras

Rama tony/T-1327-jugador. Escrita sin Unity (sin Library): pendiente de compilar y correr por el orquestador.

## Qué cambió (Personaje/)
- Atropello: `Movimiento/AtropelloJugador.cs` + `ReglaAtropello.cs`. Un carro (IMontable modo Carro, Rigidbody o transform en movimiento) a más de 4 m/s hacia el jugador lo tira: usa la caída existente de VisualPersonaje (Caido; no hay ragdoll), lo empuja ~1.2 m, 1.4 s en el suelo y se levanta con la animación de levantarse (1.1 s). `ControladorAPie.Derribado` bloquea caminar, saltar y golpear. Lo agrega TreparAPie (sin cambios de prefab, no hay `Aplicar()`).
- Golpe dirigido: ya giraba al más cercano (T-1003); ahora además se acerca hasta 0.8 m (`DatosPersonaje.AvanceAutoGiro`, parada a 1 m, `SelectorAutoGiro.Acercamiento`).
- Daño de caída: `VidaAPie.RecibirDanioSoloVida` (no pasa por el escudo ni lo recarga); `CaidaJugador` lo usa.
- Escaleras: (a) el clip de bajar de la escalera de mano se veía al revés (el progreso baja al bajar): `AnimacionEscalera.TiempoClip(..., alReves)`; el ritmo ya depende de los metros reales. (b) Bajando escalones/rampas el jugador perdía el piso un cuadro y "flotaba": `ControladorAPie.PegarAlPiso` (hasta EscalonMaximo).

## Pruebas
EditMode `JugadorT1327EditPruebas`; PlayMode `JugadorT1327Pruebas` (atropello, carro lento, golpe se acerca, caída vs escudo, escalones sin flotar).

## Pendiente / notas
- No existe ragdoll ni clip propio de atropello: se usa la pose de caída. El atropello no quita vida.
- metrosPorCiclo (0.9) no se midió contra el root motion del clip.
- playtest-17nov-b.md no existía en el worktree; se siguió el encargo.
