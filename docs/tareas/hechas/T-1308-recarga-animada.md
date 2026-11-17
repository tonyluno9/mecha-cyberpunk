# T-1308: Recarga siempre con su animación

Origen: docs/playtest-17nov.md punto 8. Rama `tony/T-1308-recarga-animada`.

## Qué estaba mal
- `AnimacionJugador` disparaba el trigger `Recarga` directo al Animator. Con el arma guardada (sin apuntar) el Animator ve `TipoArma = Ninguna`
  y ninguna transición de recarga se cumple: se reponían las balas sin animación (y con la recarga automática al vaciarse tampoco sonaba).
- Al cambiar de arma a media recarga, el cargador de la otra arma seguía contando y reponía balas igual.
- Rodar, entrar al carro o caer KO tampoco interrumpían la recarga.

## Qué se hizo
- `VisualPersonaje.Recargar(float segundos)`: saca el arma (pose levantada, `ArmaEnMano`) y la mantiene levantada toda la recarga
  más 1.5 s; después se guarda sola (decisión: queda como tras un disparo, se guarda sola, es lo más natural).
- `AnimacionJugador` usa ese método cuando hay visual (manual, R) y también en la recarga automática al vaciarse.
- `CargadorPistola.CancelarRecarga()` y evento `RecargaCancelada`: no repone balas.
- `PistolaAPie.CancelarRecargasInterrumpidas`: cancela al cambiar de arma (la que se dejó), rodar, entrar o salir del carro y KO.
- La recarga automática (clic vacío) ahora también reproduce su sonido de recarga.
- Pruebas EditMode nuevas en `Tests/EditMode/Personaje/CargadorPistolaPruebas.cs` (3 de cancelación).

## Sin verificar (sin Unity en este worktree)
No se compiló ni se corrieron pruebas. Falta confirmar en Unity que la duración del clip coincide con `TiempoRecarga` del arma y
correr las pruebas de Personaje (EditMode y PlayMode).
