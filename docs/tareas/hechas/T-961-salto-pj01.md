# T-961: salto con los clips de Blender (Sesión A, A2)

**Hecho.** El jugador salta con `PJ01_Loco_SaltoInicio`, `PJ01_Loco_SaltoAire` (SIN bucle, 12 cuadros: se queda en su última pose hasta tocar el piso), `PJ01_Loco_SaltoAterrizaje` y `PJ01_Loco_Caida` (con bucle, solo en caídas largas). El Animator no cambió: SaltoAire ya solo sale al aterrizar o al caer rápido.

## CharacterController
- `DatosPersonaje_Base.alturaSalto` 1.1 → **0.45 m** y `ConfigControlesAPie_Base.gravedad` 20 → **16 m/s²** (con `AjustarSaltoT961`). 2·√(2·0.45/16) = 0.47 s en el aire, como la muestra aprobada.
- Medido en juego (`SaltoMedidaT944Pruebas`): sube 0.45 m y pasa 0.47 ± 0.08 s en el aire; con PIERNAS I 0.585 m y con PIERNAS II 0.72 m.
- Las cajas de salto de `Pruebas_Implantes` se rehicieron con la nueva altura (0.40 / 0.867 / 1.003 m) con `CreadorEscenaImplantes.AjustarCajasSalto`, sin regenerar la escena.
- Ojo: con 0.45 m ya no se suben obstáculos que antes sí (1.1 m). Es lo pedido; si algún sitio de la ciudad lo necesitaba, se notará en el playtest.

## Capturas (revisadas contra 03_vista_frente, 03_vista_lado y 03_vista_aterrizaje)
`docs/capturas/pj01-salto-frente.png` y `-lado.png`: SaltoAire con las rodillas arriba (una más que la otra) y los brazos abiertos; aterrizaje estirando las piernas. `ui-salto-1-inicio.png`, `ui-salto-3-aterrizaje.png` en juego.

## Pruebas
EditMode 2295/2295 (`ClipsBlenderPJ01Pruebas`: overrides, datos y SaltoAire sin bucle). PlayMode: `SaltoMedidaT944Pruebas` (altura y tiempo en el aire), `ImplantesAPiePruebas`, `EscenaImplantesPruebas`, `AnimatorPJ01T733Pruebas`, `VelocidadesAnimacionT652Pruebas`: 24/24.
