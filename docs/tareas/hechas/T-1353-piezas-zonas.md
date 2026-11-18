# T-1353: piezas modulares de las zonas nuevas a prefabs

Rama: tony/T-1353-piezas-zonas. Script: `MechaCyberpunk.Arte.Editor.ImportarT1353Zonas.Importar` (Assets/Arte/Editor/ImportarT1353Zonas.cs).
Estado: escrito, SIN correr en Unity (el worktree no tiene Library). Falta correr el script y las pruebas
(Assets/_Proyecto/Tests/EditMode/Mundo/PiezasZonasT1353Pruebas.cs) en la carpeta principal.

## Carpetas
- Assets/Arte/ZonasModulos/Modelos/: los 18 FBX y T_Malla.png (copiados de imageassets/blender/zonas/; LFS por .gitattributes).
- Assets/Arte/ZonasModulos/Prefabs/<NombreFbx>.prefab: raíz (nombre = pieza) con hijo `Visual` (instancia del FBX, no copia de mallas) y LODGroup.
- Assets/Arte/ZonasModulos/Materiales/ZM_<Material>.mat: URP Lit por nombre de material de Blender.
- "ZonasModulos" está en `ArteT250.Zonas`: importa con ajustes T250 (1 unidad = 1 m, escala 1, sin materiales del archivo).
  Como el importador no trae los materiales, el script los asigna por el orden de ranuras del FBX (tabla `Piezas`).

## Medidas (m, ejes de Blender: X ancho, Y fondo = Z de Unity, Z alto = Y de Unity; medidas leídas con Blender en modo fondo)
Pivote = origen del FBX (no se mueve): base al centro, salvo lo anotado. El lado de afuera mira a +Z (según el chat de Blender; no verificado en Unity).

| Pieza | X (ancho) | Fondo | Alto | Rango Y base..tope | Caras | Collider | Pivote / notas |
| --- | --- | --- | --- | --- | --- | --- | --- |
| AU01_AutopistaRecta | 12 | 8 | 2.1 | 0..2.1 | 59 | MeshCollider | centro del tramo, base de las columnas; calzada arriba |
| AU02_AutopistaCurva45 | 17.27 | 21.21 | 2.1 | 0..2.1 | 290 | MeshCollider | ENGANCHE en el origen (x -6..11.27, fondo 0..21.21): entra por el origen hacia +fondo |
| AU03_AutopistaRampa | 12 | 48 | 8.4 | -0.3..8.1 | 463 | MeshCollider | ENGANCHE en el origen, sube a +fondo 48 m hasta 8.1 m (pendiente ~16 %) |
| AU04_ColumnaAutopista | 9.8 | 2.4 | 6 | 0..6 | 42 | BoxCollider | centro; el capitel de 9.8 m va a lo ancho |
| AU05_Guardarrail | 4 | 0.28 | 0.75 | 0..0.75 | 38 | BoxCollider (grosor min 0.2) | centro, tramo de 4 m |
| AU06_EntradaTunel | 16.4 | 14.2 | 11.5 | 0..11.5 | 68 | MeshCollider | boca en fondo -0.2..14 (entra hacia +fondo) |
| CE01_EscaleraCerro | 2.3 | 4 | 2.62 | -0.3..2.32 | 75 | Rampa invisible (caja) | centro; sube 2.32 m en 4 m (~30 grados) |
| CE02_BarandalTubo | 4.06 | 0.05 | 1.05 | 0..1.05 | 70 | BoxCollider (grosor min 0.2) | centro |
| CE03_BarandalTuboEscalera | 4.06 | 0.05 | 3.05 | 0..3.05 | 70 | BoxCollider (grosor min 0.2) | centro; barandal inclinado para la escalera |
| DS01_RejaPuas | 4.07 | 0.42 | 2.83 | 0..2.83 | 483 | BoxCollider | centro; material Malla con T_Malla (recorte alfa) |
| LM01_MuroLuz | 4 | 0.46 | 3.12 | 0..3.12 | 24 | BoxCollider | centro; material Luz emisivo |
| ML01_BarandalMalecon | 4 | 0.4 | 1.08 | 0..1.08 | 596 | BoxCollider | centro |
| ML02_EscaleraPlaya | 4 | 4 | 2.9 | 0..2.9 | 83 | Rampa invisible (caja) | centro; ~36 grados |
| ML03_MuelleRecto | 3.3 | 4 | 3.45 | -2..1.45 | 220 | MeshCollider | centro; cubierta a y -1 según el plan (pilotes bajan a -2) |
| ML04_MuelleEsquina | 3.65 | 3.65 | 3.45 | -2..1.45 | 282 | MeshCollider | codo; x -1.65..2, fondo -2..1.65 |
| ML05_Escollera | 4.96 | 4.14 | 2.53 | -0.45..2.08 | 460 | MeshCollider convexo | centro aprox (x -2.18..2.78) |
| PT01_Contenedor40 | 12.26 | 2.48 | 2.61 | 0..2.61 | 511 | BoxCollider | centro; largo a lo largo de X |
| PT02_BitaAmarre | 0.6 | 0.6 | 0.62 | 0..0.62 | 90 | BoxCollider | centro |

## Decisiones
- Pivote: no se desplaza nada; el origen del FBX ya es base-centro o enganche. Por eso el Y mínimo no es 0 en AU03, CE01, ML03, ML04 y ML05.
- Escaleras a pie (CE01, ML02): sin MeshCollider de peldaños (el CharacterController sube máx. 0.35 m); una caja "Rampa" invisible inclinada, como T-996.
  Eje y sentido de subida calculados de los vértices al crear el prefab.
- LOD: una sola malla por pieza (máx. 596 caras), así que el LODGroup es de un nivel con corte: 3 % barandales y bita, 2 % medianas, 1 % autopista, columna, túnel.
- Static: BatchingStatic + OccludeeStatic en todas; OccluderStatic en AU01-04, AU06, LM01, ML05, PT01.
- Materiales: si Blender retexturiza, hay que reordenar la tabla `Piezas` solo si cambia el orden de ranuras.
