# T-500 Paso 0: importar N01-N08, personajes de Tripo y piezas de interfaz
Dueño: Tony | Rama: tony/T-500-importar | Modelo: Sonnet 5.5 | docs/plan-ciudad-ui-personajes.md, paso 0
Hecho: N01-N08 con el flujo de T-250 (Assets/Arte/<Zona>/<Código>/, prefab con hijo Visual, URP Lit, pivote en la base; N03a y N03b con Mesh LOD por pasar de 15 mil caras); man, woman y woman2 en Assets/Arte/Personajes/<nombre>/ con su .fbm (FBX Generic con sus clips, sin rig Humanoid: es de B1); los 14 PNG de interfaz y fuentes.md en Assets/Arte/UI/Fuentes~/ (Unity no los importa). RF01 no se importó. Código de editor: Assets/Arte/Editor/ImportarT500.cs (menú Mecha Cyberpunk/T-500). Prueba EditMode ImportarT500Pruebas (2/2). Captura docs/capturas/T-500-1.png (1920x1080).

## Qué trae cada modelo
| Código | Zona | Medida final (m) | Triángulos |
| --- | --- | --- | --- |
| N01 lámpara de jaula | Calle | 0.5 alto | 9 728 |
| N02 autopista elevada | Exteriores | 24 largo | 8 823 |
| N03a puesto con lona | Calle | 2.6 alto | 18 667 (Mesh LOD) |
| N03b puesto de comida | Calle | 2.6 alto | 19 259 (Mesh LOD) |
| N04 tuberías | Callejon | 3 alto | 9 140 |
| N05 ventilas | Callejon | 1.2 alto | 9 963 |
| N06 basura | Calle | 0.9 alto | 10 135 |
| N07 valla | Exteriores | 5 alto | 6 014 |
| N08 anuncio vertical | Exteriores | 4 alto | 8 885 |

## Personajes (clips que trae cada FBX, todos con 81 transforms)
- man: 91 819 triángulos; agree.001 (4.00 s), angry_01.001 (3.50 s), run.001 (1.25 s), walk.001 (2.33 s).
- woman: 21 992 triángulos; afraid.001 (2.58 s), agree.001 (4.00 s), angry_01.001 (3.50 s), run.001 (1.25 s), walk.001 (2.33 s).
- woman2: 21 148 triángulos; agree.001 (4.00 s), run.001 (1.25 s), walk.001 (2.33 s).
Ninguno trae quieto (idle). man pesa 91 mil: para B1 conviene Mesh LOD o reducirlo.

## Bitácora
- 2026-10-31: importado y verificado; el postprocesador de T-250 ahora deja a man/woman/woman2 con animaciones (Generic); .gitattributes manda *.jpeg/*.JPEG a LFS.
