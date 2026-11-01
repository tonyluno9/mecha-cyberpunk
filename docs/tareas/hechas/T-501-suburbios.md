# T-501 Paso A1: Suburbios como las referencias
Dueño: Tony | Rama: tony/T-501-suburbios | Modelo: Sonnet 5.5 | docs/plan-ciudad-ui-personajes.md, paso A1
Hecho: colocador idempotente `ColocarT501.Colocar` (menú Mecha Cyberpunk/T-501/Colocar Suburbios, Assets/Arte/Editor/) que rehace la raíz `T501_Suburbios` de Ciudad_Prologo: 30 cables cruzados en catenaria (8 por cuadra, calle principal y cruzada) y 54 entre postes (LineRenderer), 26 lámparas N01 (9 con luz cálida sin sombra), 23 letreros de neón magenta/turquesa/verde (textura generada, emisivo), 19 toldos, 18 orillas (N06 basura, tambos, huacales), 17 charcos (reflejo falso con emisivo de color), 6 capas de niebla azul, 21 tinacos C09 (uno por azotea de las manzanas 1 y 4), 48 torres de horizonte (shader propio TorreHorizonte: ventanas por código, pantallas verde/magenta/E03/CY02, antenas con luz roja) y un posproceso local (VolumenSigueNoche, Mundo/Ambiente) que oscurece y enfría Suburbios solo de noche. Obstáculos, puertas y personajes salen de ColocarNoche (ahora internal).
Pruebas: EditMode SuburbiosT501Pruebas 9/9, PlayMode SuburbiosT501PlayPruebas 3/3, RecorridoDemoPruebas (ver bitácora). Capturas: docs/capturas/ref-suburbios-noche-*, ref-cruce-dia-lluvia-*, ref-azotea-* (antes, despues, comparacion).

## FPS (build de medición, 1920x1080, hora 23, punto "suburbios")
| Calidad | Antes | Después | 1% bajo después |
| --- | --- | --- | --- |
| Medio | 564 | 463 | 315 |
| Ultra | 409 | 329 | 249 |
Calle principal (punto "calle", Medio): 240 antes, 260 después. Muy por encima de 60.

## Qué se ajustó (3 vueltas, calle de Suburbios)
1. Primer pase: demasiado oscuro, letreros y charcos invisibles (el emisivo no entraba: faltaba globalIlluminationFlags) -> exposición -0.55 a -0.25, luces más fuertes.
2. Charcos como cuadros sólidos (el emisivo de URP Lit no se multiplica por alfa) -> mapa de emisión propio.
3. Torres demasiado cerca y brillantes -> radio 340-820 m, más niebla propia, brillo menor, apagadas de día.

## Bitácora
- 2026-10-31: hecho y verificado; el cruce de día no llueve ni es gris (la lluvia de día está apagada en ConfigLluvia y el cielo diurno es azul; no se tocó).
- 2026-10-31: RecorridoDemoPruebas 1/2 con y sin T-501 (el HEAD sin mis cambios falla igual: el bot cae KO en la pelea con pistola de los SKULLS del Tianguis y no llega al jefe); es un fallo anterior de la pelea del Tianguis, no de Suburbios. Antes y después del cambio el recorrido pasa Suburbios, CIC, DOC, INVENTOR y puños.
