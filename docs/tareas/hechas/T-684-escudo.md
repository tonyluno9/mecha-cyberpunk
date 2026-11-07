# T-684 Escudo de energía
Dueño: Tony | Rama: tony/T-684-escudo | Modelo: Sonnet 5.5, medium
Depende de: T-402 (vida a pie)

## Objetivo
El jugador a pie tiene una barra de escudo que absorbe el daño antes que la vida y se recarga sola tras unos segundos sin daño. Es mejorable por un contrato de Core (`IEscudo`) para que el Doc (sesión 2) suba su máximo o su recarga sin tocar su menú.

## Qué se hizo
- Core/Jugador/IEscudo.cs: contrato (Actual, Maximo, RecargaPorSegundo, MejorarMaximo, MejorarRecarga, Recuperar, eventos Cambio y Absorbio). Se registra en `Registro<IEscudo>`.
- Personaje/Vida: `ModeloEscudo` (C# puro), `EscudoJugador` (componente, IEscudo + IGuardable clave "escudo", solo guarda las mejoras), `VidaAPie` lo crea al nacer (no se toca el prefab), absorbe antes de la vida; `DarInvulnerabilidad(s)` y `Curar(p)` para los objetos de T-685.
- DatosPersonaje: escudoMaximo 30, recarga 12/s, espera 4 s.
- UI/HudAPie/HudEscudo.cs: barra azul delgada sobre la vida (se arma en código, sigue la visibilidad del grupo de la vida), destello y velo azul de 0.3 s al absorber.

## Criterios de aceptación
- [x] El daño gasta primero el escudo y lo que sobra baja la vida
- [x] Tras 4 s sin daño el escudo se recarga solo; un golpe nuevo reinicia la espera
- [x] `IEscudo.MejorarMaximo/MejorarRecarga` suben las estadísticas y se guardan
- [x] La barra aparece junto a la vida y destella al absorber
- [x] Capturas docs/capturas/s1t-T-684-*.png

## Pruebas
- EditMode: Tests/EditMode/Personaje/ModeloEscudoT684Pruebas.cs
- PlayMode: Tests/PlayMode/UI/EscudoT684Pruebas.cs
