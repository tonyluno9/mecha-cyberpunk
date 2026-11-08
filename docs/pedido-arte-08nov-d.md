# Pedido al chat de arte — 7-nov noche (CYBERCITY): piezas separadas para Tripo

Tripo no deja separar en partes las casas, fachadas y carros que ya hicimos (vienen en quads). Por eso se generan **por piezas
desde la imagen**: cada pieza es una imagen y un modelo de Tripo. Para que combinen, en cada una usa como imagen de entrada la
imagen ORIGINAL de ese asset (está en imageassets/, o el render del modelo que ya existe) y pide el mismo diseño, colores y desgaste.
Para Tripo: fondo blanco liso, objeto completo, iluminación pareja, sin texto. Mismo flujo: prompts + script de PowerShell para
WaveSpeed (Nano Banana 2.1, 1K, una imagen por prompt), guardando en `imageassets\modelos-nuevos\piezas\`.

## 1. Llantas (una para todos)
| Archivo | Qué es |
| --- | --- |
| RU01_LlantaCarro.png | Llanta de carro con rin sencillo, de lado (vista de frente al rin) y en 3/4. Una sola, se usa en TODOS los carros. |
| RU02_LlantaMoto.png | Llanta de moto con rin de rayos, de lado y en 3/4. Una sola, se usa en TODAS las motos. |

## 2. Carros: carrocería + puerta delantera
Para cada carro, dos imágenes, vista 3/4 frontal:
- `<código>_Carroceria.png`: el carro **sin ruedas** (pasos de rueda vacíos), **sin las dos puertas delanteras** (huecos de puerta
  abiertos, se ve el asiento y el volante) y **sin cristales** (ventanas abiertas, parabrisas sin vidrio o muy transparente).
- `<código>_Puerta.png`: solo la puerta delantera izquierda, sin cristal (ventana abierta), vista de lado por fuera.
  (La derecha se hace en Unity como espejo.)
Carros: V01a_CarroViejito, V01b_CarroPro, V02_SedanViejo, V03_PickupTrabajo, V04_CombiVieja, V05_PatrullaSedan,
V06_PatrullaCamioneta, V09_Taxi, V10_Pickup.

## 3. Motos: cuerpo sin ruedas
`<código>_Cuerpo.png`, de lado y 3/4, sin ruedas (horquilla y basculante vacíos): V07_MotoReparto, V08_MotoPatrulla, C19_MotoCyber.

## 4. Fachadas sin puerta + puertas aparte
- Fachada **con el hueco de la puerta vacío** (marco sí, puerta no), de frente:
  C01_Fachada, C02_Fachada, C03_Fachada (planta baja del kit), P11_EntradaCasa (entrada del edificio de la casa, sin reja ni puerta),
  P12_FachadaConsultorio (consultorio del Doc, sin puerta metálica).
- Puertas sueltas, de frente, del tamaño del hueco: PU01_PuertaVivienda (madera/lámina pintada, para C01-C03), PU02_CortinaMetalica
  (cortina enrollable de local, cerrada), PU03_RejaEntrada (puerta de reja del edificio de la casa, para P11), PU04_PuertaMetalica
  (puerta metálica del consultorio, para P12).
- La puerta del cuarto del jugador NO se hace.

## Orden
1 → 2 (empezar por V01a, V05 y V09, los que más se ven) → 4 → 3.
