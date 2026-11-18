# T-1346: módulos de zonas hechos en Blender

Importar los 18 FBX de imageassets/blender/zonas/ (no se sube a git) con materiales URP Lit compartidos, prefabs con collider, informe y capturas. La colocación en el mapa no entra aquí.

## Hecho (escrito, pendiente de correr en Unity)
- `Assets/Arte/Editor/ImportarModulosT1346.cs`: menú "Mecha Cyberpunk/T-1346/Importar módulos de Blender" (`Importar`, `Capturar`, `ImportarYCapturar` para batch). Toma todos los .fbx de la carpeta de origen; idempotente.
- Salida en `Assets/Arte/Zonas/Modulos/`: `<Nombre>/<Nombre>.fbx`, `<Nombre>.prefab`, `Texturas/`, `Materiales/`. Una carpeta propia porque los códigos AU01, ML01... chocan con props Tripo de T-1340.
- Materiales: se leen los nombres de material de los renderers del FBX (Concreto, Asfalto, Metal, Hierro, Madera, MaderaOscura, Pilote, Roca, Malla, PinturaBlanca, Fondo, Luz, Contenedor, ConcretoCerro, ConcretoClaro). Cada uno es un `Materiales/<nombre>.mat` URP Lit con la textura de la tabla `Definiciones` y el FBX se remapea a ellos (`AddRemap`), así FBX y prefab usan los mismos. Un nombre nuevo queda gris liso y se anota en el informe.
- Malla (DS01): `_AlphaClip` 1, corte 0.5, `_ALPHATEST_ON`, doble cara. Luz (LM01, AU06): emisión cálida con `RealtimeEmissive`.
- Contenedor PT01: base gris más 4 prefabs `PT01_Contenedor40_Rojo/Azul/Verde/Naranja` (`Contenedor_<Color>.mat`).
- Escala y pivote: no se tocan. Si un FBX sale de más de 200 m se apaga `useFileScale` y se anota en el informe.
- Colliders: caja de límites en PT01 (y variantes), PT02 y AU04; MeshCollider no convexo en los demás. Marcas `BatchingStatic`.
- Texturas T03_Asfalto etc.: se reusa cualquier textura del mismo nombre que ya exista en Assets; si no, se copia a Modulos/Texturas.
- Salidas: `Logs/t1346-modulos.txt` (límites, UV máximo, materiales, triángulos, collider), `docs/capturas/T-1346-<Nombre>.png` y `T-1346-autopista-armada.png`.
- Prueba EditMode `MechaCyberpunk.Tests.Mundo.ModulosT1346Pruebas`.

## Notas
- UV máximo en el informe: si una pieza de varios metros trae UV 0..1, el tiling queda anotado ahí y no se cambió.
- La autopista armada es una aproximación: la salida de la curva se estima de sus límites; el punto real lo decide quien coloque el mapa.
