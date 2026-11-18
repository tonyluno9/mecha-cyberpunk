# T-1329 Controles: rueda de armas, rueda EXO con Tab e interacciones al centro

Origen: docs/playtest-17nov-b.md, sección 2 (Jugador y controles). Rama tony/T-1329-controles. Sin verificar en Unity (lo corre el orquestador).

## Hecho
- Rueda de armas: la acción `Rueda` pasó de Tab al botón medio del ratón (`<Mouse>/middleButton`), mantener abre y soltar elige.
- Rueda de habilidades del EXO (nuevo `UI/Rueda/RuedaHabilidadesExo.cs`): mantener Tab (acción `RuedaExo`), mover el ratón hacia la habilidad, clic izquierdo
  (acción `RuedaExoElegir`) la activa con `HabilidadesProtesis.Pulsar`; soltar Tab cierra. Tiempo a 0.25, igual que la rueda de armas; no abren a la vez.
- Tab ya hacía la rueda de armas: esa pasó al botón medio. En la clínica Tab sigue siendo su propia pestaña (usa PilaPantallas, la rueda no abre ahí).
- Quitadas las teclas sueltas Q, C, X, 4 y 5 de las cinco habilidades (las acciones siguen, sin tecla, para que `HabilidadesProtesis` las encuentre).
  Barra de habilidades, tutorial (`ReglaTutorial.TextoHabilidades`) y Opciones > Controles (`ReglaControlesInicio`) ya no mencionan esas teclas.
- Interacciones (HudH10): el aviso sale al centro, 170 px abajo del centro, con la tecla real en una caja; a la derecha quedan solo recargar y golpe.

## Archivos
- Personaje/Entrada/ControlesAPie.inputactions (editado a mano en JSON), Personaje/Editor/CreadorJugadorAPie.cs (valores por omisión).
- UI/Rueda, UI/HudAPie (HudAPie, ReglaTutorial), UI/HudH10 (HudH10, BarraHabilidades, TeclasHud), UI/Inicio, UI/Mapa/Minimapa, UI/Pausa/PantallaPausa.
- Pruebas: Tests/PlayMode/UI/ControlesT1329Pruebas.cs (nueva) y ajustes en las pruebas viejas que usaban Tab para la rueda o las teclas de habilidades.
