# T-1383 Look de noche de las zonas nuevas

Estado: escrita (código de editor sin compilar ni correr; la corre el orquestador en la carpeta principal).
Rama: tony/T-1383-look-noche. Carpetas: Assets/Arte/Editor/, Assets/Arte/Mundo/ (Zonas/ y Look/), docs/.

## Causa de la escarcha
Material Assets/Arte/Mundo/S2c/S2c_Asfalto.mat (el arroyo de las calles de las 4 zonas; en el Cerro son 3 usos, en el Puerto 9, en la Autopista 8, en el Malecón 4):
- `_BaseColor` = (2.4, 2.3, 2.4): albedo mayor que 1 (T-880, hecho para los barrios con neones), las luces de sodio lo queman a blanco.
- `_BumpMap` = Assets/Arte/Exteriores/Texturas/T535_Asfalto_Normal.png está importada como Default con sRGB (no NormalMap): el plano (0.5, 0.5, 1) se lee como una normal torcida y el grano brilla por píxel.
- La banqueta (S2c_Banqueta) usa como `_BumpMap` T506_Losa.png, un gris que tampoco es normal.
Los .mat de S2c_ y de los barrios viejos no se tocan: dentro de los prefabs de zona se cambian por materiales propios.

## Qué hace
- `LookNocheT1383.Aplicar`: piso mojado (M_T07_AsfaltoMojado de T-1382 si existe; si no, ZM_AsfaltoMojado_T1383 con la normal copiada como NormalMap), banqueta sin normal, revisión de los materiales de zona (albedo > 1, normal mal importada),
  Charco.prefab y 20 a 40 charcos con semilla por zona, sondas de reflejo de caja (una cada 40 m, máx. 14 por zona), faroles de sodio (1, 0.55, 0.2), una Point Light de 5 m por neón magenta o turquesa (máx. 60 por zona, sin sombras, con LuzNocturna),
  niebla #1a1630 (RenderSettings y claves de noche de ConfigCicloDia_Mundo), PerfilNoche (ACES, bloom 1/1.2, contraste, balance cálido, viñeta 0.24). Informe en Logs/t1383-look.txt.
- `LookNocheT1383.Fotos`: docs/capturas/t-1383/<zona>.png y <zona>-lado.png contra la referencia.

## Pendiente de verificar con Unity
Compilar, correr Aplicar y Fotos, mirar las capturas, revisar el conteo de luces y el tamaño de la escena (< 100 MB).
