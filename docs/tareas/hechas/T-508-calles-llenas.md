# T-508 Pulido visual, punto 3: calles más llenas sin moverlas
Dueño: Tony | Rama: tony/T-508-calles-llenas | Modelo: Sonnet 5.5 | Logs/reglas-plan-ciudad.md, "Pasada de pulido visual"
Hecho: colocador idempotente `ColocarT508.Colocar` (menú Mecha Cyberpunk/T-508/Calles llenas, Assets/Arte/Editor/) que rehace la raíz `T508_CallesLlenas` de Ciudad_Prologo. Recorre con rayos las fachadas de Suburbios, calle principal con El Tianguis, calles cruzadas, callejón, entrada de El Mercado y Neón Alto, y cuelga: 70 toldos de lona (6 colores sucios con faldón) o lámina (con prioridad sobre las puertas), letreros salientes perpendiculares con brazo y cadenas (neón o pintados) con texto en español en las dos caras (FARMACIA, TACOS, CIBER, ABARROTES, TORTILLERIA, PULQUERIA...), balcones con losa, barandal, macetas y a veces ropa tendida en los pisos altos, macetas y tambos C16 y huacales C17 y basura N06 en las banquetas, y gente parada con los visuales de T-520 (parejas platicando, personas recargadas, clientes de los puestos del Tianguis; woman2 con 9 materiales y woman). Nada lleva collider ni física: no tapa caminos, puertas, personajes de la historia, puntos de aparición ni coberturas (se comprueba con las puertas, personajes y puntos de ColocarNoche). El texto de los letreros es una imagen generada (TextMesh dibujado con cámara a Texturas/T508_Txt_*.png) porque el shader de TextMesh ignora la profundidad y se veía a través de todo. Charcos: los 92 charcos de T-501 a T-507 (discos de color sólido de hasta 7 m) pasan a T508_Charco_* (sin brillo, alfa 0.22 de color y 0.4 el mojado, forma irregular) y a la mitad de tamaño (máximo 3.2 m).
Gente: 24 a 48 personas; sin sombra, LODGroup que las corta a 4.5% de pantalla (unos 35 m) y Animator CullCompletely. No hay animación de sentarse (ver docs/animaciones-faltan.md): nadie va sentado. Los peatones de Trafico (Alan) no se tocaron.
Pruebas: EditMode CallesLlenasT508Pruebas 5 (toldos, letreros con texto, balcones, macetas, gente, sin colliders, lejos de puertas, sin sombra, charcos), PlayMode CallesLlenasT508PlayPruebas 2 (gente parada con animador vivo, sin física); SinGreyboxT506 sigue verde (EditMode completo 1365/1365). Capturas: docs/capturas/pulido3-<zona>-comparacion.png (referencia | antes | después), -antes y -despues, 5 zonas.

## PlayMode de Mundo: los 10 fallos que T-507 no comparó
Mundo PlayMode 178/188 (10 fallos, los mismos de T-507). Contra 00292ce (escena, ciclo, perfil y DirectorPandillas de antes de T-507), las pruebas CamaraLibreCarro, CamaraLibre, EntradasNeon, Lugares y Puertas dieron 33/37: fallan igual 4 (EntradasNeon, Lugares x2, Puertas: el horario del mercado negro, "El Mercado 24 horas" contra 09:00 a 18:00), es decir ya fallaban antes de T-507 y no son de T-507 ni de T-508. Los otros 6 (CamaraLibre y CamaraLibreCarro) pasan 14/14 en HEAD cuando se corren solos: fallan solo dentro de la corrida completa de Mundo, por estado que dejan otras pruebas que cargan Ciudad_Prologo (dependen del orden); no se tocaron.

## FPS (build de medición, 1920x1080, Medio, hora 23)
| Punto | FPS | 1% bajo |
| --- | --- | --- |
| suburbios | 285.4 | 211.2 |
| callejon | 263.5 | 127.7 |
| tianguis | 157.1 | 97.7 |
| mercado_puerta | 234.2 | 121.4 |
| neon_calle | 227.1 | 122.6 |
| calle | 152.1 | 97.7 |
| clandestino | 197.0 | 105.6 |
Todos muy por encima de 60 (T-507: tianguis 157, calle 161: sin costo medible).

## Qué se ajustó (3 vueltas)
1. Primer pase: casi nada se veía de noche (lonas oscuras) y el texto salía al revés.
2. Lonas 1.45x más claras, letreros más grandes (1.4 x 0.66), más toldos y letreros; el texto seguía al revés y visible a través de las cosas (ZTest del TextMesh).
3. Texto como imagen generada con quad por cara: legible y con profundidad.

## Bitácora
- 2026-11-01: hecho; capturas revisadas con Read. Queda: callejón y mercado muy oscuros, los toldos casi no se ven desde el centro de la calle (se ven al caminar por la banqueta).
- 2026-11-01: orquestador corrió RecorridoDemoPruebas (1/2: tramos 01-09 pasan, KO intermitente del bot en El Tianguis, ya conocido) y regeneró Builds/Demo (Build Finished, Result: Success).
