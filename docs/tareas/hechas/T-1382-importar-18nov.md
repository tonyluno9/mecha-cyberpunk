# T-1382: importar la tanda del 18-nov (props de zona, texturas de calle y skyline)

Estado: escrito, SIN correr en Unity (el worktree no tiene Library). Falta correr `Aplicar` y `Fotos` en la carpeta principal y revisar las fotos.

## Hecho
- Zips de Tripo descomprimidos (FBX + color, normal, metallic, roughness) en `Assets/Arte/Zonas/<grupo>/Fuentes18nov~/<Codigo>_<a|b>/`.
  La carpeta lleva `~` al final para que Unity no la importe (ahi solo viven las fuentes; el importador copia lo necesario).
- Importador `Assets/Arte/Editor/ImportarZonasT1382.cs` (basado en ImportarZonasT1340): FBX sin materiales, texturas con normal y mapa metal/suavidad,
  material URP Lit, Mesh LOD si pasa de 5000 triangulos, pivote en la base, collider y prefab `Assets/Arte/Zonas/<grupo>/<Codigo>_<a|b>.prefab`.
- Escalas: AU02 3 m de largo, AU03 4.5 m de largo, CC06/CC07 7.5 m de alto, CC08 6 m de largo, ML05 17 m de alto, ML07 0.8 m de alto, PU07 3 m de alto,
  PU08 contenedor 12.2 x 2.59 x 2.44 m (escala por eje, BoxCollider exacto). Casas, edificio y pluma con MeshCollider; barrera, chatarra, muro, hielera y contenedor con BoxCollider.
- Texturas T07, T08, T09 (_a y _b) en `Assets/Arte/Materiales/Calle18nov/` con mapa normal generado de la luminancia (`*_normal.png`) y materiales
  `M_T07_AsfaltoMojado` (suavidad 0.8, metalico 0), `M_T08_LaminaOxidada` (metalico 0.5, suavidad 0.35), `M_T09_ConcretoManchado` (suavidad 0.25); variantes `_b` con sufijo.
  Tiling 0.25 (una repeticion cada 4 m si la malla tiene UV de 1 por metro).
- SK01_Skyline: PNG _a y _b en `Assets/Arte/Fondos/`, material `M_SK01_Skyline` (Unlit aditivo, la imagen no trae alfa y el cielo es negro, brillo HDR 1.6),
  malla de arco `SK01_Skyline_Arco.asset` (120 grados, radio 900 m, alto 180 m, base en y=0, centro del cilindro en el origen, arco hacia +Z) y prefab `SK01_Skyline.prefab`
  sin collider y sin sombras. La imagen (2.36:1) se estira ~4.4x a lo ancho en ese arco; si se ve mal, bajar el arco o subir el alto.
- Informe de `Aplicar`: `Logs/t1382-importar.txt`. Fotos: `docs/capturas/t-1382/<Codigo>_<a|b>.png` con un cubo de 1.8 m.

## Falta / no se hizo
- ML06_EdificioMalecon: solo hay .png (_a y _b), no hay zips.
- Drones DR02-DR04, NV03, E15-E20 y NP07-NP14: no se tocan (los rigea Blender en otra tarea). Sin zip: DR03_b, DR04_b, E17_b, E18_b, E20? (tiene), NP10_b, NP12_b, ML06; E19_a tiene un segundo zip `_a_2`.
- W09 -> W10 del arma de Nova: no hay nada versionado con ese nombre (ver abajo); solo existen archivos sin versionar en la carpeta principal.
- Validar: tamanos reales en el informe, que el normal generado no se vea exagerado (`FuerzaNormalCalle`), el brillo y el estiramiento del skyline.
