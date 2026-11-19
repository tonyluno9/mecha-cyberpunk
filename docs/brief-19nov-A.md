# Brief común, sesión A del 19-nov (look de las zonas nuevas)

Lo dirige Opus en la carpeta principal (rama `integ/look-19nov-A`). Tú eres un subagente Sonnet con UNA tarea y UNA rama
`tony/T-14xx-…` que sale de `integ/look-19nov-A`.

## Reglas
- **No abras Unity** (no hay Library en tu worktree). Entregas solo scripts de editor C# que compilen a ojo con cuidado:
  métodos `public static void Aplicar()`, `Validar()` y lo que pida tu tarea. Opus los corre en la carpeta principal con
  `-executeMethod MechaCyberpunk.Arte.Editor.Correr19nov.Correr -pasos Tipo.Metodo,…` y te devuelve el log y las fotos.
- Tus scripts van en `Assets/Arte/Editor/` (asmdef `MechaCyberpunk.Arte.Editor`, namespace `MechaCyberpunk.Arte.Editor`).
  Si necesitas un componente de juego (runtime), va en `Assets/_Proyecto/Mundo/` (asmdef `MechaCyberpunk.Mundo`) con sus pruebas EditMode
  en `Assets/_Proyecto/Tests/EditMode/Mundo/`.
- Lee antes los scripts de rondas anteriores de tu zona (`ZonaMaleconT13xx/T14xx`, `ZonaAutopistaT13xx/T14xx`, `LookNocheT1383/T1392/T1399`,
  `AereasT1414`, `ColocarS2cLook`, `Correr19nov`) y reusa sus ayudas. No repitas lo que ya hay: muévelo, arréglalo o reemplázalo.
- Prefabs de zona: `Assets/Arte/Mundo/Zonas/T1354_Malecon.prefab` (Malecón), `T1356_Autopista.prefab` (Autopista). Edítalos con
  `PrefabUtility.LoadPrefabContents` / `SaveAsPrefabAsset` y todo lo nuevo bajo una raíz propia `T14xx_<Zona>` que `Aplicar` borra y rehace
  (idempotente). **No toques** `T1355_Cerro.prefab` ni `T1357_Puerto.prefab` (los trabaja la sesión B), ni los barrios viejos (S2c_*, S2*, T5xx).
- La escena `Assets/_Proyecto/Escenas/Ciudad_Prologo.unity` debe quedar en menos de 100 MB: lo grande va en prefab.
- **Nada inventado con primitivos** (cubos o cilindros como edificios, columnas o palmeras). Usa los modelos que ya hay
  (Assets/Arte/…: AR03_Palmera, ML01_Farol, ML02–ML07, AU01–AU04, C07, NE01/NE01B/NE02, NP01–NP14, carros Carro_V*, basura, cajas, bancas…).
  Busca con Grep/Glob qué hay. Lo que falte, anótalo en tu archivo de tarea en una sección "Assets que faltan" (Opus lo pasa a docs/assets-faltan.md).
  Planos con material (charcos, halos, haces de luz) sí se permiten.
- Luces: la escena tiene unas 1220 `Light`; el tope seguro es 1400 en total. Cada tarea tiene un cupo (abajo). Halos y haces van como
  quads o mallas emisivas sin `Light`. Al borrar una Light, quita antes `UniversalAdditionalLightData`.
- Materiales URP/Lit emisivos por código: `globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive` (si no, se pierde _EMISSION).
- Fotos: `Vista19b.Fotos` (Assets/Arte/Editor/Vista19b.cs) toma las 9 comparaciones. Para fijar tu cámara igual a la referencia, crea en tu
  prefab un GameObject vacío `Vista19b_<TOMA>` (K11, A11, K18, A18…) con un hijo `Mira` y un hijo `Campo_<grados>`; Vista19b lo usa solo.
- Referencias (no están en tu worktree; usa la ruta absoluta y ábrelas con Read para verlas):
  `C:\Users\Angel Perez\Desktop\videojuego\mecha-cyberpunk\imageassets\referencias\look\` (K11_Malecon.png, K12_Malecon_Muelle.png,
  A11_Malecon_Aerea.png, K18_AutopistaElevada.png, A18_Autopista_Aerea.png…).
  Comparaciones actuales (izquierda referencia, derecha juego): `C:\Users\Angel Perez\Desktop\videojuego\mecha-cyberpunk\docs\capturas\_vista19\*.jpg`.
  Las de la ronda en curso: `C:\Users\Angel Perez\Desktop\videojuego\mecha-cyberpunk\docs\capturas\_vista19b\*.png`.
- Escribe tu tarea en `docs/tareas/en-curso/T-14xx-<nombre>.md` (qué, criterios, qué se hizo, assets que faltan).
- Commit en tu rama con mensaje en español. No hagas merge a ninguna otra rama ni push. Al terminar devuelve: rama, commit, lista de métodos
  a correr en orden, y qué esperas ver en cada foto.
- Validar escribe `Logs/t14xx-validar.txt` con números medidos (cuántos objetos, distancias, luces añadidas, fallas). Debe fallar
  en voz alta (`Debug.LogError`) si algo no cumple.

## Cupos de luces
- T-1461 (look): hasta +80 Light nuevas, y puede quitar luces redundantes de las zonas nuevas.
- T-1463 (Malecón): hasta +40. T-1464 (Autopista): hasta +40. T-1462 (aéreas): 0.
