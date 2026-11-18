# T-1340: importar los props de zonas nuevos

Rama tony/T-1340-props-zonas. Carpetas permitidas: Assets/Arte/ (Editor/ y Zonas/), Assets/_Proyecto/Tests/EditMode/Mundo/, docs/.

## Qué hace
Importa los 33 props de `imageassets/modelos-nuevos/nuevos-17nov/<Nombre>.zip` (Tripo) a `Assets/Arte/Zonas/<ZZ>/<Nombre>/` y crea `Assets/Arte/Zonas/<ZZ>/<Nombre>.prefab`. La tabla (nombre, eje, medida, collider, alto del tronco) vive en `ImportarZonasT1340.Entradas` para que la use T-1341. `ImportarZonasT1340.RutaPrefab(nombre)` da la ruta del prefab.

## Criterios
1. Cada zip se desempaca (idempotente) y deja FBX, `_basecolor.jpg`, `_normal.jpg` y `_metalsuave.png` (R=G=B=metálico, A=1-rugosidad, 1024²). Un zip que falta se anota en `Logs/t1340-zonas.txt`.
2. Importadores: normal como NormalMap; metalsuave sin sRGB y con alfa de la entrada; textura 2048 si la medida es de 4 m o más, 1024 si no; FBX sin materiales, sin cámaras ni luces; Mesh LOD si pasa de 5000 triángulos.
3. Material URP Lit por modelo con _BaseMap, _BumpMap y _MetallicGlossMap, e instancing.
4. Prefab con raíz = Nombre y hijo Visual, escalado para que el alto (o el largo) dé la medida (±5%), con el pivote en el centro de la base (min.y = 0, centro xz = 0).
5. Colliders según la tabla: Caja (BoxCollider en la raíz), Malla (MeshCollider no convexo por MeshFilter) y Poste (hijo Colision con BoxCollider "base" y CapsuleCollider "tronco").
6. Capturas `docs/capturas/T-1340-<Nombre>.png` con el prop y el jugador a su lado, y el informe `Logs/t1340-zonas.txt`.

## Pruebas
EditMode `MechaCyberpunk.Tests.Mundo.ZonasT1340Pruebas`: por cada entrada, prefab existe (si no, falla), collider, medida, base y centro, material URP Lit con los tres mapas y tipo de collider.

## Cómo correr
`-executeMethod MechaCyberpunk.Arte.Editor.ImportarZonasT1340.ImportarYCapturar` con Unity cerrado, o el menú Mecha Cyberpunk/T-1340.
