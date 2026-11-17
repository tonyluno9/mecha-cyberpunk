# T-1302: Barra de prótesis vertical (playtest 17-nov, punto 2)

**Pedido:** barra de prótesis en columna en el lateral izquierdo (como M31 pero vertical) con Q PIERNAS, C BRAZOS, X CABEZA; láser y misiles en la misma columna.

**Cambió**
- `UI/HudH10/BarraHabilidades.cs`: casillas en columna anclada arriba-izquierda (margen 63 px, desde 388 px del borde de arriba, 236x72, hueco 6). Cada casilla: icono a la izquierda; a la derecha "[TECLA] NOMBRE" y el medidor debajo. Láser y misiles salen al final de la columna solo si hay prótesis. Con 5 casillas termina arriba de la ayuda de controles y debajo de vida/calor/Biits/renta. Nuevo `TeclaPorOmision` público.
- `Personaje/Entrada/ControlesAPie.inputactions` y `Personaje/Editor/CreadorJugadorAPie.cs`: bindings Q, C, X.
- Textos de ayuda: `UI/HudAPie/ReglaTutorial.cs`, `UI/Inicio/ReglaControlesInicio.cs` (TutorialLigero toma la tecla real del Input System, no cambió).

**Teclas finales:** Q piernas, C brazos, X cabeza, 4 láser, 5 misiles (sin choque).

**Reasignación:** Cubrirse a pie pasaba de Q a **V** (`Personaje/Movimiento/CubiertaYRodada.cs`), con textos: `UI/Datos/ConfigAyudas.cs`, `ConfigAyudas_Base.asset` (solo la cadena de texto), `Personaje/Editor/AjustarAgacharseT673.cs`. C y X no estaban en uso a pie (C es cámara del carro, otro modo).

**Pruebas:** nueva `Tests/EditMode/UI/BarraHabilidadesT1302Pruebas.cs`; actualizadas ReglaControlesInicio, ReglaTutorialHabilidades, ProgresoAyuda (EditMode); BarraProtesisT1011, HabilidadesProtesisT992, HabilidadesProtesisT992Capturas, CubiertaYRodada y RecorridoDemo (PlayMode). No se corrieron (sin Unity en este worktree).
