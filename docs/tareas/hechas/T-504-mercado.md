# T-504 Paso A4: Entrada de El Mercado como la referencia
Dueño: Tony | Rama: tony/T-504-mercado | Modelo: Sonnet 5.5 | docs/plan-ciudad-ui-personajes.md, paso A4
Hecho: colocador idempotente `ColocarT504.Colocar` (menú Mecha Cyberpunk/T-504/Colocar Entrada de El Mercado, Assets/Arte/Editor/) que rehace la raíz `T504_Mercado` en el callejón de El Mercado de Ciudad_Prologo (x -25 a -19, z 11 a 37, portón E02 de T-419 en el muro oeste a z 26): 2 luces naranjas del portón (adentro y derrame al piso), 7 hilos de papel picado de muro a muro a más de 6 m con 66 banderas (planos con textura generada de 64x64 con agujeros, 6 colores), 4 barreras C18 con franja turquesa emisiva en las dos caras y foco naranja, 3 cámaras CY08, servidor CY05 con luz turquesa, puesto de implantes CY09 con luz magenta (el de T-419 sigue en la boca del callejón), plataforma de dron CY07a con luz turquesa, un tambo C16 y 5 charcos de color. Reutiliza `ColocarNoche` (obstáculos), la textura de charco de T-501 y `CapturaT501`. Quedan libres el punto Entrada de El Mercado (2.2 m), el mostrador y el carril de 2.4 m desde la boca del callejón. Todos los assets (E02, C18, CY05, CY07a, CY08, CY09) ya estaban importados (T-250/T-419): no hubo importación nueva.
Extras: `ColocarNoche.bloqueo` pasa a internal (para explicar por qué no cabe una pieza) y `MedicionNoche` trae el punto `mercado_puerta` (-21.5, 0.2, 19).
Pruebas: EditMode MercadoT504Pruebas 8/8 (más T-501/502/503: 31/31), PlayMode MercadoT504PlayPruebas 3/3 (más T-501/502/503), RecorridoDemoPruebas (ver bitácora). Capturas: docs/capturas/ref-entrada-mercado-noche-antes, -despues y -comparacion.

## FPS (build de medición, 1920x1080, Medio, hora 23)
| Punto | Antes | Después | 1% bajo después |
| --- | --- | --- | --- |
| mercado_puerta | 301.0 | 301.3 | 150.8 |
| calle | 219.2 | 214.5 | 123.6 |
Muy por encima de 60.

## Qué se ajustó (2 vueltas)
1. Primer pase: papel picado y charcos demasiado brillantes (verde limón, turquesa plano), una barrera de más.
2. Segunda: papel con color al 70 %, charcos al 30 % de brillo y 0.7 de alfa. Queda por debajo de la referencia en densidad (el muro rojo de la fachada de T-419 sigue liso, no hay toldo con rayas ni cables gruesos) y la cámara frontal no cabe: el portón mira al este en un callejón de 6 m.

## Bitácora
- 2026-11-01: hecho; servidor y puesto de implantes no cupieron pegados al muro (paneles de 0.35 m y tacos de T-407): van sueltos en el callejón buscando sitio libre. Los huacales C17 no cupieron.
- 2026-11-01: RecorridoDemoPruebas 1/2 en dos corridas: el bot cae KO en la pelea con pistola de los SKULLS del Tianguis (x -37, z 76 a 80, lejos del callejón x -22, z 11 a 37); los tramos de El Mercado, El Doc y las compras pasan.
- 2026-11-01: ArmasMercadoPruebas.RecorridoFusilYSniper pasa sola (abre la tienda con E) y falla por orden cuando corre después de las pruebas de T-501/502/503 con o sin T504 en el grupo.
