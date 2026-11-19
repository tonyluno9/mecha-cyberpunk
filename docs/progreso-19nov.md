# Progreso 19-nov (noche): ciudad idéntica a las referencias

Dirige Opus. Las tareas las hacen subagentes Sonnet, una rama `tony/T-xxxx-…` por tarea. Se integran en `integ/ciudad-19nov`
(carpeta principal, que tiene la única Library) y de ahí van a main con merge --no-ff y push.
**Si se corta la sesión:** lee este archivo, `git log --oneline integ/ciudad-19nov -15` y `git branch | grep T-13[89]` y sigue con lo
que diga "Qué sigue".

## Reglas de esta noche
- Un solo Unity, en modo batch y en la carpeta principal. Nada de PlayMode completo: pruebas por clase. Blender sigue abierto.
- No correr CreadorTodo. Cada zona es un prefab en `Assets/Arte/Mundo/Zonas/`; la escena debe quedar en menos de 100 MB.
- Mandan las referencias de `imageassets/referencias/look/`: K11–K18, A11–A18 y M12.

## Paso 0 (02:00–02:20) ✔
- A las 02:00 Unity y Unity Hub ya estaban cerrados y no había lockfile. Blender está abierto (con blender-mcp).
- `git fetch`: origin/main (cbd8ad5) ya está contenida en integ/bugs-mapa.
- Lo de la sesión s2 (T-1370 a T-1377, paso 1 del Cerro y el Puerto) estaba sin commit desde las 14:29 del 18-nov:
  se hizo commit en su rama (a1796f7).
- Rama nueva `integ/ciudad-19nov` = integ/bugs-mapa + tony/T-1370-cerro-puerto-paso1 (merge bd4fc97; se conservaron
  las dos versiones de docs/assets-faltan.md).

## Tareas
| Tarea | Qué | Estado |
| --- | --- | --- |
| T-1380 | Blender: ruedas de carros, puertas, cofre, cristal; rotores de DR01–DR04, E13, NV01, NV03 | en curso (subagente) |
| T-1381 | Blender: rig humanoide E15, E16, E18, E19, E20 y NP07–NP14; cuadrúpedo E17 | en curso (subagente) |
| T-1382 | Importar nuevos-18nov (variantes _a y _b), texturas T07–T09, SK01 como skyline, W10 de Nova | en curso (subagente) |
| T-1383 | Look: sodio, neones con luz, niebla de color, piso mojado con charcos, quitar el piso de escarcha | en curso (subagente) |
| T-1384 | Malecón como K11, K12 y A11 | en curso (subagente) |
| T-1385 | Cerro como K13 y A13 | en curso (subagente) |
| T-1386 | Autopista como K18 y A18 | en curso (subagente) |
| T-1387 | Puerto como K15 y A15 | en curso (subagente) |
| T-1388 | Uniones con los 4 barrios y detalle (cables, letreros, carros, NPCs, basura) | en curso (subagente) |
| T-1389 | Tráfico, peatones, enemigos y drones nuevos | parte a (carros) en curso; parte b (NPCs, enemigos y drones) espera a T-1380 y T-1381 |
| T-1390 | Rendimiento: 50 fps o más por zona | pendiente |
| T-1391 | Builds (Demo y Juego/CyberCity.exe) y reporte-19nov | pendiente |

## Bitácora
- 02:20 Plan escrito. Primera tanda: T-1380, T-1381, T-1382 y T-1383, en paralelo.
- 02:35 integ/ciudad-19nov compila (0 errores). Lanzadas también las zonas T-1384 a T-1387, T-1388 (uniones, prefab nuevo T1388_Uniones) y T-1389a (tráfico por zona y Carro_V11). Brief común de zonas: los subagentes solo entregan scripts (Aplicar, Validar, Fotos); los prefabs los genera Unity en la carpeta principal.
- Orden para correr en Unity: T-1382.Aplicar → zonas (T-1384..T-1388 Aplicar) → T-1383.Aplicar (look al final) → en otra corrida, Fotos.

- 03:10 Las ramas de los subagentes salieron de origin/main y no de integ: se les pidió hacer merge de integ. T-1382 integrada y corrida: 18 prefabs (_a y _b) de AU02, AU03, CC06, CC07, CC08, ML05, ML07, PU07 y PU08, materiales T07–T09 y el skyline SK01 (assets en 77676b2). ML06 solo tiene .png.
- 03:15 W10: el arma de Nova solo existía sin versionar (imageassets/modelos-nuevos/W09_ArmaNova.png, nuevos-17nov/, modelos3d/*.zip, lote_17nov.json, pedido-arte-17nov.md, PROMPTS_ASSETS.md); renombrada a W10_ArmaNova. La granada sigue como W09.
- 03:20 Integradas en integ: T-1383 (look), T-1384 (Malecón), T-1386 (Autopista), T-1387 (Puerto) y T-1389a (tráfico). Causa de la escarcha: S2c_Asfalto usa T535_Asfalto_Normal.png importada como Default en sRGB (y _BaseColor 2.4); se corrige la importación (Correr19nov.ArreglarNormalesAsfalto) y las zonas usan materiales propios.
- 03:25 Corrida 1 de Unity con Correr19nov.Correr (todas las Aplicar y Validar); el resultado queda en Logs/correr19nov.txt.

- 04:10 Corridas 2 y 3: T-1385 (Cerro) y T-1388 (Uniones) no compilaban (corregido); con eso se aplicaron todas las zonas, las uniones (prefab nuevo T1388_Uniones de 6.8 MB), el tráfico (8 zonas, Carro_V11, PatrullaCamioneta, MT01/MT02) y el look (faroles de sodio, charcos, sondas, niebla, perfil de noche). Escena de 55.8 MB y Cerro de 32 MB. Commit cbd1302 con push.
- T-1381 (rigs) integrada: 24 humanoides y el perro E17 en imageassets/blender/personajes/. E16_b quedó regular. Lanzada T-1389b, que mete esos personajes al juego (peatones y enemigos).
- Pendiente de revisar: el look puso 0 luces de neón nuevas (no encontró neones por color); el Cerro solo tiene 5 casas nuevas porque rechazó 119 lotes.
- 04:15 Fotos de todas las zonas (corrida aparte).

- Ronda 1, fotos (docs/capturas/t-1384..t-1388): ya no hay escarcha, pero todo sale muy oscuro, los charcos son cuadros negros, no hay neones, el skyline no se ve y los encuadres de K13 y K18 están mal. Se lanzó la ronda 2 (docs/brief-ronda2-19nov.md): T-1392 look, T-1393 Malecón, T-1394 Cerro, T-1395 Autopista, T-1396 Puerto.
- El disco C: se llenó (0 MB): se quitaron los worktrees de los subagentes que ya estaban integrados (sus ramas quedan). Ahora hay unos 13 GB libres.
- T-1380 integrada: arregló 11 carros (puertas, cofre, cristal, ruedas) y separó los rotores de 9 drones y naves. Los FBX viejos quedaron en imageassets/blender/carros/_antes_T1380/.
- T-1389b corrida: 23 personajes Humanoid válidos y el perro E17 (Generic). E16_b no tiene avatar válido. Peatones NP07–NP14 mezclados en la lista general (el sistema no reparte por zona). Nova suma E15, E16, E18 y el perro; los 30 SKULLS de la escena usan E19/E20.
- T-1397 corrida: los 11 carros rearmados y los drones importados. Dron.prefab de la policía con DR01. No había drones de Nova ni de SKULLS en el código, así que se lanzó T-1398 (drones con ruta aérea para Nova, SKULLS, policía y tráfico aéreo).
- T-1392 (look, ronda 2) lista: se corre al final, después de las zonas de la ronda 2.

- Ronda 2 aplicada completa: T-1393 Malecón (skyline en 2 arcos a 495 m, 27 neones, 76 cables; solo 1 ML05 cupo porque mide 14.7 m y las casas 5–7 m), T-1394 Cerro (70 enlaces entre azoteas, 45 escaleras de servicio, variedad CC, 26 neones, cámara K13 en la calle base), T-1395 Autopista (cámara K18 en la calle baja, postes C07, túnel iluminado, 25 faroles AU01 con placa de sodio), T-1396 Puerto (PU08 orientado, bodegas con alero, neón y lámparas, balizas en las grúas, barco a la vista), T-1398 (18 drones con ruta: Nova, SKULLS, policía y tráfico aéreo) y T-1392 look (charcos S2c con reflejo, ambiente y niebla más claros, volumen por zona, emisión de NE01, NE01B y NE02 encendida). Hay 1341 luces en la escena (el tope seguro es 1400).
- Pruebas EditMode de las tareas nuevas: 218/222. Las 4 fallas se corrigieron: 3 eran de E16_b (se quitó) y 1 de carros en U1.

- 03:10 Fotos de la ronda 2: hay neones, fachadas de color, charcos con reflejo y carros. Faltan la niebla y el cielo, el skyline del Malecón salía como panel liso (el material copiado tenía niebla; corregido con Correr19nov.ArreglarSkylineMalecon), los cables del Cerro estaban rotos, no había grafiti a la vista en el Puerto y la composición de K18 no coincidía. Se lanzó la ronda 3 (docs/brief-ronda3-19nov.md): T-1399 niebla y cielo, T-1400 Malecón (ML05 que ocupan 2 a 4 casas), T-1401 Cerro (cables) y T-1402 Autopista y Puerto.
- El primer build de Juego falló por disco lleno ("Failed to write file"). Se quitaron 32 worktrees viejos del 17 y 18 de noviembre, todos limpios y ya integrados (sus ramas quedan): ahora hay 27 GB libres.
- 03:45 Corrida 9: se aplica la ronda 3 y el look (T-1392 y luego T-1399).

- Ronda 3 (T-1399 a T-1402) y ronda 4 (T-1403 a T-1405) aplicadas. El Malecón tiene niebla, skyline, 9 ML05 y faro; el Cerro, variedad, azoteas unidas y una cámara K13 sin cables pegados (las "cintas negras" eran cables a menos de 4 m del lente); la Autopista tiene la calle baja con el tablero y chatarra a escala, y el Puerto grafiti PU08, bodegas con neón y grúas con balizas. T-1403 había apagado 250 azoteas y 50 rieles por error: se reencendieron (Correr19nov.ReencenderT1403).
- FPS en el build de Juego: el peor es Cerro/plaza_arriba con 137 fps (el 1 % más lento, 103). Todas las vistas pasan de 50 (docs/fps-19nov/).
- Pruebas EditMode: clases nuevas y de zonas 160/161; la falla de las bocas de U2, U4 y U7 se arregló con Correr19nov.DespejarBocasUniones, y OrillasT1358 da 4/4. Dos corridas largas en segundo plano las paró Claude Code por falta de memoria; se repitieron en primer plano.
- 04:10 Siguen los builds (Demo con ConstruirDemoPandillas y luego Juego), el reporte y el merge a main.

## Qué sigue
- Esperar la primera tanda; integrar T-1382 y T-1383 en Unity (importar y aplicar el look); después las zonas (T-1384 a T-1388).
