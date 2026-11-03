# T-540 a T-544 Paso 2 del plan de la noche 3-nov: assets nuevos de imageassets/modelos3d
Dueño: Tony | Rama: tony/T-540-assets-nuevos | Modelo: Opus 5.5 | docs/plan-noche-03nov.md, paso 2

## T-540 Importar (ImportarT540, flujo de T-250)
T20-T23, N09, L10-L14 y V07 a Assets/Arte/<zona>/<código> con su color y normal (los zips de Tripo traen _0_0 color y _0_3 normal), URP Lit, pivote en la base, BoxCollider y Mesh LOD (todos pasan de 15 mil caras). Se ignoraron las carpetas "- copia". De los dos zips de T23 se usó "T23_RemateTorre" (la misma malla que "(1)", con textura más oscura que va con los pisos). Hoja: docs/capturas/prueba2-modelos-nuevos.png.
## T-541 Torres y postes de Neón Alto (ColocarT541)
13 torres (los edificios de barrio de la manzana del Casino y la columna de la manzana del Mercado que da a Neón Alto): base T20 (12 m) + 6 a 20 pisos T21 de 6 m con T22 (pantalla) de vez en cuando y nunca dos seguidos + remate T23; 179 pisos en total, alturas distintas (las de la calle de 8 a 14). Cada torre ocupa la planta del edificio y su cara de pantallas da a la calle. Fuera: las casas y sus pisos de arriba (ArteNoche/Pisos), lo que colgaba arriba de ellas (letreros de azotea, balcones, tinacos) y las 14 torres de bloques de T-505. Las casas siguen en Suburbios, Callejón y Tianguis. Postes de la calle de Neón Alto: N09 con luz blanca fría en el foco (12).
## T-542 Letreros 3D (ColocarT542)
L10 VivaCoin sobre la entrada del Casino (fuera la lámina VIVACOIN de T-414, el letrero de neón plano y los focos y tubos de formas), L11 Hotel de bandera en el hotel de la calle de tu casa (fuera su neón plano), L12 El Mercado sobre la puerta, L13 I+D+I en el local del Doc y L14 CIC en el de Tino. Pegados al muro con un rayo. El texto brilla solo de noche (mapa de emisión de Assets/Arte/Exteriores/Fuentes~/generar_emision.py + LuzNocturna); las torres también llevan emisión (tiras y ventanas).
## T-543 Moto de reparto (ColocarT543)
Carro_V07 "Moto de reparto", se maneja y se roba como moto; 4 estacionadas en lugares libres (2 en Suburbios, 2 en El Tianguis).
## T-544 Pantallas LED (ColocarT544, Mundo/Ambiente/PantallaLed)
52 pantallas con anuncio (3 horizontales en E03 y los pisos T22, 3 verticales en CY02 y N08): cambian cada 8 a 12 s y parpadean leve. El área de pantalla de cada modelo se midió en vistas ortográficas y se ajusta al frente con un rayo. Las 13 CY02 que estaban paradas en la banqueta se pegaron a su muro.

## Pruebas
EditMode AssetsNuevosT540Pruebas (5) + actualizadas NeonAltoT505 (torres y entrada del Casino), FondoCiudadT513 (torres de T-505 apagadas); PersonajesT510 verde (los guardias se recolocaron: el poste N09 traía una caja que tapaba la banqueta, se le quitó). PlayMode PantallaLedPruebas (1), CiudadCarros verde (una moto tapaba la salida de un carro estacionado: se movió). RecorridoDemoPruebas 2/2.
## FPS (build de medición, 1920x1080, Medio, hora 23, Logs/med-paso2/medicion.txt)
| Punto | FPS | 1% bajo |
| --- | --- | --- |
| calle | 161.5 | 88.0 |
| tianguis | 158.1 | 101.7 |
| carros | 165.4 | 120.0 |
| neon_calle | 240.9 | 115.7 |
| neon | 267.0 | 131.6 |
| suburbios | 277.8 | 188.4 |
| vivacoin | 332.0 | 226.0 |
## Pendiente / raro
De noche las torres se ven oscuras: brillan las tiras, las pantallas y pocas ventanas (la textura de Tripo trae las ventanas azul oscuro). Capturas: docs/capturas/prueba2-neon-alto-torres, -torres-pantallas, -casino-vivacoin, -letreros-idi-cic, -hotel-y-moto.
