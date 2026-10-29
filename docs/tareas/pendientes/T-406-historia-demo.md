# T-406 Historia corta y demo de 15 minutos, hasta "CONTINUARÁ"
Dueño: Tony | Rama: tony/T-406-historia-demo | Modelo: Sonnet 5.5, medium | MODO RÁPIDO
Depende de: T-400 a T-405 (es la última de la fase G).

## Objetivo
Una partida nueva cuenta una historia corta: despiertas solo en tu casa, defiendes a un vecino a puños, recoges la pistola,
reclutas al CHATO, conquistas EL TIANGUIS y sale "CONTINUARÁ". Unos 15 minutos. Build final en Builds/Demo.

## Contexto
- Modelo a copiar: `Progresion/Prologo/DirectorPrologo.cs` y `MaquinaPrologo.cs` (máquina pura + director en escena), objetivos con
  `Core/Objetivos/IObjetivos.cs` y la flecha de `Progresion/Objetivos/SeguimientoObjetivo.cs`, `UI/Historia/PantallaContinuara.cs`,
  `UI/Historia/PantallaSubtitulos.cs`. DirectorPrologo sigue apagado (T-400) y no se borra.
- Contratos que ya hay: `ITerritorio` (T-404), `IPandillaJugador` (T-405), `ICartera`, `ICalor`.
- Decisión: D-tony-223 (pasos, textos, guardado y build de la demo).

## Carpetas permitidas
Progresion/Prologo/ (Editor incluido), Personaje/Combate/ (arma en el piso), IA/ (los dos Calaveras de la primera pelea, el CHATO de la
historia), UI/Historia/, Core/Guardado/IGuardable.cs (solo el comentario de la clave), Escenas/Ciudad_Prologo.unity (solo con código),
Tests/PlayMode/Progresion/, docs de Tony.

## Qué hacer
1. `Progresion/Prologo/Pandillas/MaquinaHistoriaPandillas.cs` (pura) con los pasos y `DirectorPandillas.cs` en escena:
   a. DESPIERTA: apareces dentro de la casa a las 19:00, solo, con puños y $0. Objetivo "SAL A LA CALLE". La pistola no está al inicio
      (`pistolaAlInicio` false de T-403).
   b. PRIMERA PELEA: afuera, 2 Calaveras a puños empujan a DOÑA LUPE (muñeco quieto, color de vecina). Subtítulo: "—¡Ya déjenla!".
      Objetivo "DEFIENDE A DOÑA LUPE". No pueden matarte: si tu vida baja de 25, dejan de pegar 3 s.
   c. LA PISTOLA: el último en caer suelta su pistola (`Personaje/Combate/ArmaEnPiso.cs`, `IInteractuable`). "RECOGE LA PISTOLA (E)".
      Al tomarla: ayuda "CLIC DERECHO APUNTA · CLIC IZQUIERDO DISPARA".
   d. EL CHATO: "HABLA CON EL CHATO" → se une gratis (`Reclutable.soloHistoria`). Línea: "—Los Calaveras se creen dueños del Tianguis".
   e. EL TIANGUIS: "CONQUISTA EL TIANGUIS · RESPETO 0/70". Pistas de una vez: "PUEDES ROBAR UN CARRO CON E" y "EN EL MERCADO NEGRO
      VENDEN ESCOPETAS". Antes de este paso los grupos del tianguis no aparecen.
   f. EL JEFE: "VENCE AL JEFE DE LOS CALAVERAS" con la flecha hacia él.
   g. CONTINUARÁ: 4 s después de la conquista, la pantalla "CONTINUARÁ..."; al cerrarla sigues jugando libre (NEÓN ALTO queda).
   El director guarda su paso con la clave `historia_pandillas` (agrégala a "Claves en uso" de IGuardable). F6 salta un paso, solo
   fuera de la demo final (`#if !DEMO_FINAL`).
2. `CreadorCiudadPandillas.T406()`: `GTA_T406` con el director, el punto de inicio dentro de la casa, DOÑA LUPE y los 2 Calaveras de la
   primera pelea; el CHATO de T-405 queda en modo historia. `Todo()` corre T400 a T406 en orden.
3. Build final: `ConstruirDemo.ConstruirDemoPandillas()` → `Builds/Demo/MechaCyberpunk.exe`, solo Ciudad_Prologo,
   `BuildPlayerOptions.extraScriptingDefines = { "DEMO_FINAL" }`; con DEMO_FINAL se apagan las teclas de prueba (calor, guardado,
   saltar paso) y la partida siempre empieza nueva. No cambies `EditorBuildSettings` ni `PlayerSettings`. El viejo `Construir()` no se toca.

## Pruebas (solo estas 2 nuevas, PlayMode)
- `HistoriaPandillasPruebas.MaquinaDePasos` (puede ser EditMode): la máquina pasa de DESPIERTA a CONTINUARÁ solo con sus eventos, en
  orden, y al cargar un guardado en el paso e vuelve al paso e.
- `HistoriaPandillasPruebas.RecorridoHastaContinuara`: en Ciudad_Prologo, con entrada simulada para salir, golpear (clics), recoger (E) y
  reclutar (E); los 7 del tianguis y el jefe se pueden vencer con daño directo por `IRecibeDanio` para que la prueba dure menos de 3 min.
  Al final se ve "CONTINUARÁ", la zona es tuya y tienes 1 aliado y la pistola.

## Al terminar
- Build de siempre: `-executeMethod MechaCyberpunk.Progresion.Prologo.Editor.ConstruirDemo.ConstruirJuego` → Builds/Juego/MechaCyberpunk.exe.
- Build final: `-executeMethod MechaCyberpunk.Progresion.Prologo.Editor.ConstruirDemo.ConstruirDemoPandillas` → Builds/Demo/MechaCyberpunk.exe.
- docs/guia-para-jugar.md, sección "Juego de pandillas": 3 líneas de qué probar (con Builds/Demo).
- Bitácora de una línea: docs/bitacora/<fecha>-tony-T-406.md. Fila de docs/estado-tony.md en "hecha". Merge a tony/fase-g.

## Sensación (Tony)
- ¿Te dura unos 15 minutos? ¿Se entiende qué hacer sin leer la guía? ¿El final deja ganas de más? ¿La pelea con el jefe es buen cierre?

## Fuera de alcance
- Cinemáticas, voces, creación de personaje antes de la demo, la historia de NEÓN ALTO.
