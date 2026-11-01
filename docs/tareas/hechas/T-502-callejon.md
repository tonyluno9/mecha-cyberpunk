# T-502 Paso A2: Callejón como la referencia
Dueño: Tony | Rama: tony/T-502-callejon | Modelo: Sonnet 5.5 | docs/plan-ciudad-ui-personajes.md, paso A2
Hecho: colocador idempotente `ColocarT502.Colocar` (menú Mecha Cyberpunk/T-502/Colocar Callejón, Assets/Arte/Editor/) que rehace la raíz `T502_Callejon` en el callejón de la emboscada de Ciudad_Prologo (x -87 a -59, muros z -54.1 y -63.9, ruta z -59): 4 tuberías N04, 5 ventilas N05 con vapor (ParticleSystem de 20 partículas máx. cada uno), contenedor L01, 2 llantas L04, tambos C16, huacales C17, bolsas N06, escalera L03, neón magenta, 2 lámparas N01 cálidas, 3 luces de color sin sombra (magenta x2, turquesa), 6 charcos magenta/turquesa, 5 capas de niebla rosa y un volumen local oscuro y magenta solo de noche (PerfilCallejon). Reutiliza las texturas de T-501 (Assets/Arte/Suburbios/Texturas) y CapturaT501. Un corredor de 4 m de ancho sobre la ruta queda vacío y no se tapan Emboscada_Anclaje ni Llamado_Callejon.
Pruebas: EditMode CallejonT502Pruebas 7/7, PlayMode CallejonT502PlayPruebas 2/2, RecorridoDemoPruebas (ver bitácora). Capturas: docs/capturas/ref-callejon-noche-antes, -despues y -comparacion.

## FPS (build de medición, 1920x1080, Medio, hora 23)
| Punto | Antes | Después | 1% bajo después |
| --- | --- | --- | --- |
| callejon | 436 | 443 | 312 |
| calle | 218 | 204 | 113 |
Muy por encima de 60.

## Qué se ajustó (3 vueltas)
1. Primer pase: vapor enorme, charcos como cuadros con brillo, todo demasiado magenta -> menos luz, vapor chico, exposición -0.6.
2. Charcos con material Unlit (sin cuadro visible), vapor con textura de disco suave propia, escalera sin la prueba de espacio libre (el muro tiene paneles).
3. Letrero de neón sacado del muro (los paneles sobresalen 0.7 m).

## Bitácora
- 2026-11-01: hecho; 17 de 20 piezas caben (las que no, chocaban con paneles y cajas existentes); el callejón sigue más ancho (9.8 m) que la referencia: no se angosta para no tocar la ruta de la emboscada.
- 2026-11-01: RecorridoDemoPruebas 1/2: el bot cae KO en la pelea con pistola de los SKULLS del Tianguis (el mismo fallo de T-501); los tramos anteriores (incluida la casa y el callejón) pasan igual.
