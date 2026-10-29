# T-404 Tres zonas con dueño: respeto, jefe, conquista y mapa con M
Dueño: Tony | Rama: tony/T-404-zonas-respeto-mapa | Modelo: Sonnet 5.5, medium | MODO RÁPIDO
Depende de: T-402 (grupos de pandilleros con `IdPandilla`).

## Objetivo
El barrio se parte en LA FOSA (tuya), EL TIANGUIS (Calaveras) y NEÓN ALTO (Víboras). Vencer a su gente sube tu respeto en esa zona;
con 70 aparece su jefe y si lo vences la zona es tuya. El HUD dice en qué zona estás, de quién es y tu respeto. M abre un mapa.

## Contexto
- Medidas del barrio en `Mundo/Editor/CreadorBarrio.cs`: manzanas de 80 m, paso 94 m; Manzana_1 (−94, 47), _2 (0, 47), _3 (94, 47),
  _4 (−94, −47, casa), _5 (0, −47), _6 (94, −47). Zonas: oeste = 1 y 4, centro = 2 y 5, este = 3 y 6; el límite son las calles x = ±47.
- Servicios con guardado: `Progresion/Implantes/ServicioImplantes.cs` (modelo, servicio, `IGuardable`, `Registro`) y su lugar en
  `ServiciosProgresion.prefab` (`Progresion/Editor/CreadorImplantes.cs`). HUD: `UI/HudAPie/HudAPie.cs`.
- Decisiones: D-tony-217 (zonas), D-tony-218 (respeto y conquista), D-tony-219 (contrato), D-tony-220 (mapa).
- **Contrato nuevo de Core (Tony lo autorizó en esta tarea)**: `Core/Territorio/ITerritorio.cs`, que implementa Progresion y usan IA,
  UI y la historia. Lo mínimo:
  `IReadOnlyList<string> Zonas`, `string ZonaEn(Vector3 posicion)` ("" fuera), `string Nombre(string zona)`, `IdPandilla Dueno(string zona)`,
  `int Respeto(string zona)` (0 a 100), `bool JefeDisponible(string zona)`, `void RegistrarVencido(Vector3 posicion, IdPandilla pandilla, bool jefe)`,
  `void RegistrarCaida(Vector3 posicion)`, `event Action<string> Cambio`, `event Action<string, IdPandilla> Conquistada`.
  Se registra en `Registro<ITerritorio>`. Agrega la clave de guardado `territorio` a la lista "Claves en uso" de `Core/Guardado/IGuardable.cs`.

## Carpetas permitidas
Core/Territorio/ (ITerritorio.cs), Core/Guardado/IGuardable.cs (solo el comentario), Progresion/ (Territorio/ nuevo, Datos/, Editor/,
Prologo/Editor/CreadorCiudadPandillas.cs), IA/, UI/ (HudAPie/, Mapa/ nuevo, Editor/), Personaje/Entrada (acción Mapa, con su creador),
Escenas/Ciudad_Prologo.unity (solo con código), Tests/EditMode/Progresion/, Tests/PlayMode/Progresion/, docs de Tony.

## Qué hacer
1. `Progresion/Datos/DatosZona.cs` (id, nombre, dueño inicial, color, rectángulo XZ) y 3 assets: `fosa` LA FOSA (Jugador, naranja),
   `tianguis` EL TIANGUIS (Calaveras, magenta), `neon` NEÓN ALTO (Víboras, verde ácido). Respeto inicial: 0 en todas.
2. `Progresion/Territorio/ReglaTerritorio.cs` (pura, sin Unity) y `ServicioTerritorio.cs` (`ITerritorio`, `IGuardable` "territorio").
   Reglas de D-tony-218: +10 por pandillero vencido en su zona, +30 por el jefe; en LA FOSA +2 por cada vencido en cualquier lado y
   +20 por zona conquistada; −10 en la zona donde caes; tope 0 a 100. Con 70 en una zona rival, `JefeDisponible`. Vencer al jefe:
   la zona pasa a Jugador, respeto 100, avisa `Conquistada`.
3. IA: cada pandillero vencido llama `RegistrarVencido` con su posición y su pandilla. El grupo de una zona tuya no reaparece.
   `JefePandilla`: pandillero con 250 de vida, escopeta (8 perdigones de 6, cada 1.6 s, alcance 15 m) y un aviso más largo (0.5 s);
   aparece en el punto del jefe de su zona cuando `JefeDisponible` y no hay otro vivo. Los caídos en la calle llaman `RegistrarCaida`.
4. HUD: arriba a la izquierda "EL TIANGUIS · LOS CALAVERAS · RESPETO 40" con el color del dueño; "¡EL JEFE TE BUSCA!" al aparecer;
   "EL TIANGUIS ES TUYO" 3 s al conquistarla.
5. Mapa con M (`UI/Mapa/PantallaMapa.cs`, con su creador): ocupa el centro de la pantalla, vista desde arriba: las 6 manzanas pintadas del
   color del dueño de su zona, nombre y respeto de cada zona, un punto blanco para ti (y espacio para los aliados de T-405).
   M o Esc lo cierran; mientras está abierto no se dispara. Acción `Mapa` (M) en el creador de la entrada.
6. `CreadorCiudadPandillas.T404()`: `ServicioTerritorio` en ServiciosProgresion (o bajo `GTA_T404` si así lo hace el creador de servicios),
   un punto de jefe por zona rival (en la banqueta más amplia) y el `IdPandilla` correcto en cada grupo de T-402.

## Pruebas (solo estas 2 nuevas)
- EditMode `ReglaTerritorioPruebas.RespetoYConquista`: 7 vencidos en el tianguis → respeto 70 y jefe disponible; LA FOSA subió 14;
  vencer al jefe → dueño Jugador, respeto 100, LA FOSA +20 más; una caída en el tianguis antes del jefe baja a 60 y `JefeDisponible` vuelve a false.
- PlayMode `TerritorioPruebas.HudYMapa`: jugador en el centro → el HUD dice EL TIANGUIS; `RegistrarVencido` ×7 y vencer al jefe que
  aparece (con daño directo por `IRecibeDanio`) → HUD "ES TUYO" y el grupo vacío no reaparece en 45 s; M simulada abre y cierra el mapa.

## Al terminar
- Build: `-executeMethod MechaCyberpunk.Progresion.Prologo.Editor.ConstruirDemo.ConstruirJuego` (ver T-400) → Builds/Juego/MechaCyberpunk.exe.
- docs/guia-para-jugar.md, sección "Juego de pandillas": 3 líneas de qué probar.
- Bitácora de una línea: docs/bitacora/<fecha>-tony-T-404.md. Fila de docs/estado-tony.md en "hecha". Merge a tony/fase-g.

## Sensación (Tony)
- ¿Se siente que la calle es de alguien? ¿7 pandilleros antes del jefe es mucho o poco? ¿El jefe es un reto? ¿El mapa se entiende?

## Fuera de alcance
- Que las pandillas te reconquisten una zona, guerras entre ellas, grafitis que cambian de color.
