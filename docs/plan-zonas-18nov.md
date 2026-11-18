# Plano de las zonas nuevas (18-nov, sesión A)

Fuente: imageassets/referencias/look/M12_MapaExpandido.png (distribución), K11, K12, K13, K15, K18 y A11, A13, A15, A18 (look).
Los 4 barrios actuales NO se mueven. Lo de "Le gustó" de docs/playtest-17nov-b.md no se toca.
Escena: Assets/_Proyecto/Escenas/Ciudad_Prologo.unity. Nunca se regenera (no correr CreadorTodo ni CreadorPrologoCiudad):
cada tarea agrega su propia raíz con un script de editor idempotente (`Aplicar()` borra y rehace solo su raíz).

## Ejes y lo que ya existe
x = este, z = norte, y = arriba. 1 unidad = 1 m. Calles de 14 m con banquetas de 3 m (0.2 m de alto).
- Centro (4 barrios): x -135..135, z -88..88. Calles N-S en x = ±47, E-O en z = 0.
- Afueras (T530_Afueras, ColocarT530): 6 corredores de 100 m que salen por (±135, 0) y (±47, ±87).
  Al final de cada uno hay una calle cruzada de 14 x 120 m (sur: z = -194; norte: z = 194; oeste: x = -242; este: x = 242),
  un edificio de fondo que tapa la vista y muros "LimiteInvisible". Los cruzados del sur (y del norte) se juntan: x -107..107.
- Autopista decorativa vieja en z = 103, y = 10 (VestirFinales): se queda como está.
- Suburbios (oeste), Mercado (centro), Neón Alto (noreste), Nova (sureste).

## Distribución nueva (adaptada de M12 a la ciudad real)
| Zona | Rectángulo aproximado (x, z) | Se conecta por |
| --- | --- | --- |
| Malecón | x -330..175, z -205..-262 (agua desde z -262) | corredores sur x = ±47 (se alargan de z -201 a -226); oeste sube a la base del cerro; este entra al Puerto |
| Colonia del Cerro | x -295..-480, z -170..215 | corredor oeste (z = 0) hasta la calle base x = -305; abajo con el Malecón; arriba con el túnel de la autopista |
| Autopista elevada | z = 250, x -330..240, y = 10; curva y rampa en x = 300 | rampa norte desde la calle cruzada norte (x = -107); rampa este que baja en x = 300 hasta y = 0 en z = 60; túnel al cerro en x = -330 |
| Puerto | x 115..330, z -45..-250 (muelle al sur en z -250 y al este en x 330) | puerta oeste desde el fin del Malecón (160, -215); puerta norte (300, -45) desde la calle de la rampa este |

### Puntos de unión (los dos lados deben pasar EXACTO por aquí, y = piso)
- U1 Malecón ← corredor sur oeste: (-47, 0, -201) → (-47, 0, -226).
- U2 Malecón ← corredor sur este: (47, 0, -201) → (47, 0, -226).
- U3 Malecón → Cerro: (-300, 0, -205) → (-330, 0, -150) → calle base del cerro (-305, 0, -120).
- U4 Cerro ← corredor oeste: (-249, 0, 0) → (-305, 0, 0).
- U5 Malecón → Puerto, puerta oeste con caseta: (160, 0, -215).
- U6 Autopista → túnel: boca en (-330, 10, 250); salida del túnel en la ladera del cerro (-360, 10, 185), mirando al sur.
  El cerro debe tener calle en (-360, 10, 175) que siga el zigzag.
- U7 Rampa norte (T-1362, reubicada): calle cruzada norte (-107, 0, 201) → (-120, 0, 215), de ahí curva y sube hacia el OESTE por z = 232 hasta y = 10 y se une a la autopista en (-270, 10, 250). Antes subía al este y se unía en (30, 10, 250). La calle baja (z = 215, x -290 a -120) llega al enlace.
- U8 Rampa este: autopista (240, 10, 250) → curva → (300, 10, 190) → baja por x = 300 hasta (300, 0, 60) → (300, 0, 0) → (300, 0, -45) puerta norte del Puerto.
- U9 Conector este: (300, 0, 0) → (249, 0, 0) (calle cruzada este).
Pendiente máxima de calles y rampas: 9 %. Radio mínimo de curva: 40 m (Malecón 60 m, para manejar rápido).

### Nivel del mar
Agua en y = -3. Playa de y -2.2 (pie del muro del Malecón) a y -3.3 (orilla). Malecón (banqueta) en y 0.2. Muelle en y -1.

### Límites del mundo nuevos
Norte z = 290, oeste x = -480, este x = 345, sur: en el agua (z = -268 para nadar no; muro invisible a 6 m de la orilla y alrededor de muelle y escollera).
Nada sin salida: toda calle termina en otra calle o da vuelta. Ningún hoyo del que no se pueda salir.

## Contratos
- Core: `MechaCyberpunk.Core.Mundo.CalleZona` (Assets/_Proyecto/Core/Mundo/CalleZona.cs). Cada calle nueva de una zona lleva una
  CalleZona con su línea del centro (`Definir(puntos, ancho, banquetas, autopista, zona)`). Zona: "Malecon", "Cerro", "Autopista", "Puerto".
  Tráfico, policía, peatones y minimapa la leen (`CalleZona.Activas`).
- Piezas modulares de Blender (imageassets/blender/zonas/*.fbx, Scale Factor 1, lado de afuera a +Z, rejilla de 4 m):
  se importan en `Assets/Arte/ZonasModulos/Modelos/` y sus prefabs quedan en `Assets/Arte/ZonasModulos/Prefabs/<NombreFbx>.prefab`
  (ej. `ML01_BarandalMalecon.prefab`), pivote en la base, al centro, con collider. Si cambia el FBX, el prefab se actualiza solo.
- Modelos de Tripo (sesión B, `Assets/Arte/Zonas/`, NO se tocan): se buscan por nombre con
  `AssetDatabase.FindAssets("<CODIGO>_<Nombre> t:Prefab", new[] { "Assets/Arte/Zonas" })` (ej. "AR03_Palmera", "ML01_Farol", "ML02_Lancha",
  "ML03_PuestoMariscos", "ML04_Faro", "PZ01_Banca", "CC01_CasaCerro", "CC02_CasaCerro", "CC03_CasaCerro", "CC04_Tendedero", "CC05_Lavadero",
  "PU01_Grua", "PU02_Barco", "PU03_Bodega", "PU04_Bodega", "PU05_Montacargas", "PU06_Pallets", "AU01_Luminaria"). Si no está: se deja el
  hueco (un GameObject vacío "Hueco_<CODIGO>" en su lugar, sin malla) y se anota en docs/assets-faltan.md. Nada de primitivos en su lugar.
- Se puede reusar lo que ya hay en la ciudad: postes, cables, tinacos, antenas, casas y fachadas del kit, letreros, puestos, NPCs.
- Rendimiento: sin luces de más (pocas Light reales, el resto emisivo), LOD en lo grande, objetos estáticos (Occluder/Occludee) para la oclusión.

## Tareas
| Tarea | Qué | Script (`-executeMethod`) |
| --- | --- | --- |
| T-1350 | Red de calles de las zonas: tráfico, policía, peatones con CalleZona | código de Trafico |
| T-1351 | Mar: shader y material de agua | `MechaCyberpunk.Mundo.Editor.CreadorMarT1351.Crear` |
| T-1352 | Mapa grande y minimapa con las zonas nuevas y sus nombres | `MechaCyberpunk.UI.Editor.CapturadorMapaCiudad.Generar` |
| T-1353 | Importar piezas modulares de Blender a prefabs | `MechaCyberpunk.Arte.Editor.ImportarT1353Zonas.Importar` |
| T-1354 | Malecón | `MechaCyberpunk.Arte.Editor.ColocarT1354Malecon.Aplicar` / `.Fotos` |
| T-1355 | Colonia del Cerro | `MechaCyberpunk.Arte.Editor.ColocarT1355Cerro.Aplicar` / `.Fotos` |
| T-1356 | Autopista elevada | `MechaCyberpunk.Arte.Editor.ColocarT1356Autopista.Aplicar` / `.Fotos` |
| T-1357 | Puerto | `MechaCyberpunk.Arte.Editor.ColocarT1357Puerto.Aplicar` / `.Fotos` |
| T-1358 | Abrir las orillas y límites nuevos | `MechaCyberpunk.Arte.Editor.ColocarT1358Orillas.Aplicar` |
Orden de corrida: T-1353, T-1351, T-1358, T-1354, T-1355, T-1356, T-1357, luego mapa (T-1352) y pruebas.
Capturas lado a lado en docs/capturas/zonas/ (helper `ColocarS2.LadoALado(referencia, juego, salida)`).
