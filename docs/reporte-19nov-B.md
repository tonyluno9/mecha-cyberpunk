# Reporte 19-nov, sesión B: Cerro y Puerto contra K13, A13, K15 y A15

Rama `integ/zonas-19nov-B` (worktree `.sesiones/s2`). **Todavía no está en main**: la verificación final con las rondas 9–10 se cortó por falta
de memoria (ver "Qué falta"). Solo se tocaron `T1355_Cerro.prefab`, `T1357_Puerto.prefab` y lo que está dentro de ellos.

## Tareas
| Tarea | Script | Rondas | Qué hace |
| --- | --- | --- | --- |
| T-1440 | Assets/Arte/Editor/ZonaCerroT1440.cs (raíz T1440_Ladera) | 10 | Pisos extra (casas CC06/CC07 reales en el cuadro de A13, kit fuera), 3 escaleras largas iluminadas entre terrazas, 11–36 postes con luz naranja y charcos de luz, carros viejos, 40 neones y 63 ventanas cálidas hacia A13, techos de concreto oscuro, cámara A13 fija, mallas combinadas |
| T-1441 | Assets/Arte/Editor/ZonaCerroT1441.cs (raíz T1441_Detalle) | 10 | Fachadas (aires, medidores, lámparas, rejas, toldos, 53 neones), ~800 objetos en ~460 azoteas (tinacos de 4 colores, antenas, parabólicas, tendederos, lavaderos, varillas, chatarra), 180+ cables, tendederos que cruzan la calle, 5 carros, 14 NPC (NP07–NP14), basura y charcos |
| T-1442 | Assets/Arte/Editor/ZonaPuertoT1442.cs (raíz T1442_Calle) | 7 | Cámara K15 fija en la avenida; montacargas, pallets, tarima, tambos, huacales, 7 estibadores (NP11, NP13, NP07), neones de bandera, toldos, postes con racimos de cables, 50 charcos en el cuadro, balizas de grúa y ventanas del barco |
| T-1443 | Assets/Arte/Editor/ZonaPuertoT1443.cs (raíz T1443_Patio) | 9 | Cámara A15 fija; contenedores de 9 colores, pilas rellenas a 3–4 alto con escaleras nuevas, carriles pintados con flechas, 70 charcos, postes con charco de luz, 3 bodegas de lámina con ventiladores y neón, caseta PU07 con pluma y reja, grúas asentadas en el muelle |
| — | Assets/Arte/Editor/CamarasB19nov.cs | — | `Probar`: renderiza cámaras candidatas de Logs/camaras-b.txt (así se eligieron K15, A15 y A13); `DiagnosticoCerro`: lista lo que rompe las pruebas del Cerro |

Las cámaras se eligieron a ojo con `CamarasB19nov.Probar` (docs/capturas/_vista19b/prueba/), después de que las búsquedas
automáticas a ciegas dieran malos encuadres:
K15 (160, 1.7, -150)→(150, 12, -260) campo 55; A15 (320, 50, -150)→(240, 0, -215) campo 60; A13 (-450, 55, 100)→(-330, 5, 20) campo 60;
K13 es la de T-1416 (-361.5, 12.85, 152)→(-355, 14, 60).

## Capturas (izquierda referencia, derecha juego), docs/capturas/_vista19b/
- `puerto-k15-t1442.png`: **la más cercana**. Contenedores con grafiti a la izquierda, bodegas con cortina y neón de bandera a la derecha,
  grúa con balizas al fondo, montacargas, pallets, tambos, estibadores y racimos de cables. Falta: el barco no cae en el cuadro y los charcos
  no brillan (hay 50 en el cuadro: el reflejo es de la sesión A).
- `cerro-k13-t1441.png`: calle que sube con postes de sodio, cables, carros estacionados (uno con faros), fachadas con medidores, rejas, neón
  y tendederos cruzando. Falta: la escalera y las casas a la izquierda (el lado está espejado contra la referencia: la ciudad queda a la izquierda
  y la tapa la niebla), y las personas se leen poco.
- `cerro-a13-t1440.png`: ladera de casas de colores con ventanas cálidas, neones chicos y la calle del zigzag iluminada en curva a la derecha.
  Falta: la ladera sigue siendo poco empinada (el zigzag original sube solo ~10 m), la ciudad brillante abajo no se ve por la niebla y arriba
  salen torres de la escena que flotan (sesión A).
- `puerto-a15-t1443.png`: grúas con balizas arriba a la izquierda, carriles con líneas y flechas, pilas de colores, bodegas abajo a la derecha.
  Falta: todo se ve azul y con niebla (sesión A), las bodegas se leen poco y el barco con su carga no se distingue.

**Veredicto honesto:** K15 se parece de verdad; K13 y A13 se parecen en elementos pero no en composición; A15 se parece en el encuadre, pero
la niebla y la luz le quitan casi todo el parecido. Ninguna es "igual" a su referencia.

## Pruebas
- Estado final (T-1440 r11, T-1441 r10, T-1442 r7, T-1443 r9): Validar sin fallos en las 4 tareas; EditMode de las zonas **102/102**;
  T1440_Ladera.asset 2.9 MB, Cerro 41.5 MB, Puerto 7.8 MB.

Con la ronda 8:
- EditMode de las zonas, por clase (CerroT1355, PuertoT1357, ArreglosCerroT1363, ArreglosPuertoT1366, VariarCerroT1364, RendimientoCerroT1365,
  BarridoMapaT1360, PiezasZonasT1353): **102/102**. Tres regresiones de la ronda 7 se arreglaron en la 8: carros en el carril, luces con
  alcance de 20 m (tope 18) y 60 rejas fuera del LOD de su casa.
- Validar de cada tarea, ronda 10 del Cerro y ronda 9 del Puerto: T1440 1 fallo (Piso_2_t1 se encima con Tinaco_252), T1441 0, T1442 0, T1443 0.
- PlayMode RecorridoCerroT1361 (18 pruebas): main fallaba 6, la ronda 8 fallaba 7. La única regresión fue `Carro_Zigzag_Parte1` (un poste en la
  curva); la ronda 10 de T-1440 lo quita, pero falta correr la prueba. Las 3 escaleras CE01, 2 cadenas de parkour y los accesos ya fallaban en main.
- PlayMode RecorridoPuertoT1361 (11 pruebas): main fallaba 4 y la ronda 8 fallaba 4. Se arregló `APie_CuatroPasillos`, pero se rompió
  `Parkour_TreparSueloYFila_YBajar`, porque T-1443 apagaba las escaleras de fila. La ronda 9 lo corrige, pero falta correr la prueba.
- Rendimiento, PlayMode `RendimientoCerroT1362Pruebas` en el editor, 1920×1080 (docs/fps-19nov-B/), sin builds como se pidió:
  - Puerto: entre 121 y 200 fps.
  - Cerro, ronda 8: entre 43 y 70 fps. El peor punto es cerro-7, "azotea alta mirando al resto", con 43.4 fps en dos corridas; antes de la sesión daba 48.8.
  - El resto del Cerro pasa de 50, aunque el ruido entre corridas es de ±10 fps (cerro-2 dio 46.3 y luego 55.6).
  - Las rondas 9 y 10 bajaron los renderers de la zona de 8755 a 7163 (accesos, pisos, barandales y tinacos combinados). Falta medirlo.
  - El build de noviembre medía unas 2.5 veces más que esta prueba del editor, pero no se hizo build.

## Qué falta
1. **Verificación final:** se cortó por falta de memoria a las 10:57, a mitad de la corrida, y no se relanzó. Falta:
   - ZonaPuertoT1442.Aplicar, sobre T1443 r9 ya aplicada;
   - las pruebas EditMode de las zonas;
   - los recorridos de PlayMode del Cerro y del Puerto;
   - el rendimiento y las fotos finales.

   Después va el merge --no-ff a main. El orden para correrlo está en docs/progreso-19nov-B.md.
2. De la sesión A (docs/para-sesion-A-19nov.md):
   - la niebla por altura para A13 y A15;
   - el reflejo de los charcos;
   - la exposición y la luz cálida;
   - las torres que flotan en el cielo de A13.
3. Assets: en docs/assets-faltan.md, sección "19-nov, sesión B".
