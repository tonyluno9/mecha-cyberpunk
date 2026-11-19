# Reporte 19-nov: la ciudad contra las referencias

Trabajo de la madrugada del 19 de noviembre, de 02:00 a 04:30, hora de México. Opus dirigió e integró; las tareas las escribieron
subagentes Sonnet, una por rama `tony/T-13xx` o `tony/T-14xx`. Cada script se corrió en la carpeta principal con un solo Unity
en modo batch, mediante `Correr19nov.Correr`. Después de cada ronda se tomaron fotos lado a lado (la referencia a la izquierda y el juego a la derecha) y se
corrigió en la ronda siguiente. Hubo 4 rondas sobre las zonas y una quinta solo para el Cerro.
El paso a paso está en docs/progreso-19nov.md y lo que no quedó, en docs/pendientes-19nov.md.

## Fotos finales contra la referencia (mismo ángulo)

| Zona | Referencia | Foto lado a lado | Cómo quedó |
| --- | --- | --- | --- |
| Malecón | K11 | `docs/capturas/t-1404/malecon-k11.png` | Barandal y paseo con faroles de sodio, niebla sobre la bahía, skyline SK01 al otro lado del mar, fachadas con neón, aires y cables. Faltan la fila de palmeras de la referencia y la gente, que depende del tráfico de peatones en juego. |
| Malecón, muelle | K12 | `docs/capturas/t-1393/malecon-k12.png` | Paseo, faro encendido (T-1404) y skyline. Faltan el puesto de mariscos con lona y las lanchas. |
| Malecón, aérea | A11 | `docs/capturas/t-1393/malecon-a11.png` | |
| Colonia del Cerro | K13 | `docs/capturas/t-1405/cerro-k13.png` | Calle base con casas de colores, neones, cables y carros, sin cables pegados a la cámara. Lejos de K13: la calle no sube como en la referencia (ver pendientes). |
| Colonia del Cerro, aérea | A13 | `docs/capturas/t-1401/cerro-a13.png` | Azoteas con tinacos y antenas, unidas por escaleras y tablones. Lejos de A13: faltan las calles iluminadas que suben y la ciudad brillante abajo. |
| Autopista elevada | K18 | `docs/capturas/t-1404/autopista-k18.png` | Calle baja con casas, neones y carros estacionados; tablero con barreras AU02 y faroles de sodio, chatarra AU03 a escala bajo el tablero, túnel iluminado. Falta la composición con la columna al centro y la rampa curva. |
| Autopista, aérea | A18 | `docs/capturas/t-1402/autopista-a18.png` | |
| Puerto | K15 | `docs/capturas/t-1402/puerto-k15.png` | El más parecido: contenedores PU08 con grafiti a la izquierda, bodegas de lámina con alero, neón magenta y turquesa y lámparas de sodio a la derecha, grúas con balizas en la bruma y pluma PU07 en la puerta U8. |
| Puerto, aérea | A15 | `docs/capturas/t-1402/puerto-a15.png` | |
| Uniones con los barrios | — | `docs/capturas/t-1388/U*.png` (20 fotos, de la zona al barrio y del barrio a la zona) | Fachadas que cambian de estilo poco a poco, postes con cables, neones, banquetas y carros estacionados. |
| Drones | — | `docs/capturas/t-1398/*.png` | |

Fotos de las rondas anteriores, para ver el avance: ronda 1 en `t-1384` a `t-1388`, ronda 2 en `t-1393` a `t-1396`, ronda 3 en `t-1399` a `t-1402`.
Las del 18-nov, antes de esta noche, están en `docs/capturas/zonas/_v_*.jpg`.

## Qué se hizo, por punto del pedido

### 1. Blender (imageassets/blender/, bitácora en `_PENDIENTE_BLENDER_2/_REVISION/BITACORA.md`)
- **T-1380, carros:** se revisaron los 28. Se arreglaron 11: V02, V03, V04, V06, V09 y V10 ahora tienen puertas y cofre separados con su bisagra y cristal de puerta; V01A, V01B y V05 tienen puertas nuevas; en V22 y V23 se recortaron de nuevo las puertas. Las ruedas de los 28 tienen el pivote al centro.
- **T-1380, drones y naves:** DR01, DR02_a/_b, DR03_a, DR04_a, E13 (provisional), NV01 y NV03_a/_b tienen `Rotor_n` separados con el pivote en el centro de giro.
- **T-1381, personajes:** 24 humanoides con esqueleto Mixamo (E15–E20 y NP07–NP14, con sus variantes) y el perro E17 con un rig propio de 4 patas y dos clips. E16_b quedó regular.
- **T-1397:** los 11 carros se rearmaron en Unity con puertas y cofre que abren (PuertasCarro) y los drones se importaron con `GiroRotores`.

### 2. Importar
- **T-1382:** 18 prefabs (variantes _a y _b) de AU02, AU03, CC06, CC07, CC08, ML05, ML07, PU07 y PU08; materiales T07 (asfalto mojado), T08 (lámina oxidada) y T09 (concreto manchado); skyline SK01 como arco con el shader `SkylineSinNiebla` (T-1392).
- **Variantes elegidas en el mapa:** PU08 _a (naranja, combina con el patio), PU07 _a, AU02 _a (losa lisa con franjas), AU03 _a con _b cada tercer montón, CC06 _b, CC07 _a, CC08 _a, y ML05 _a y _b alternados.
- **W10:** el arma de Nova se renombró de W09 a W10 (imagen, zip, lote y pedido de arte). En el código no había nada con ese nombre.

### 3. Mapa
- **Escarcha:** la causa era `S2c_Asfalto`, con la normal T535 importada como Default en sRGB y un albedo de 2.4. Se corrigió la importación y las zonas usan materiales propios.
- **Luz (T-1383, T-1392, T-1399):**
  - faroles de sodio (1, 0.55, 0.2) más fuertes;
  - emisión encendida en los neones NE01, NE01B y NE02, con su luz;
  - charcos con el reflejo plano de los barrios;
  - niebla azul grisácea y nubes;
  - volúmenes por zona (exposición y bloom), bruma de altura y halos de sodio.
  Hay 1221 luces en la escena (el tope seguro es 1400).
- **Malecón (T-1384, T-1393, T-1400, T-1404):**
  - 9 ML05 frente al avenida;
  - skyline en dos arcos a 495 m;
  - mar más reflejante;
  - neones, aires, toldos y cables;
  - faroles y palmeras en el paseo;
  - faro encendido;
  - sin palmera frente a la cámara ni el panel pintado que flotaba sobre el mar.
- **Cerro (T-1385, T-1394, T-1401, T-1403, T-1405):**
  - 70 muros CC08;
  - casas CC01, CC02, CC06 y CC07 mezcladas;
  - 70 enlaces entre azoteas con escalones y tablones, y 45 escaleras de servicio;
  - 26 neones;
  - tinacos, antenas y ropa tendida;
  - salida del túnel libre;
  - 12 cables rotos quitados.
- **Autopista (T-1386, T-1395, T-1402, T-1404):**
  - unas 980 barreras AU02;
  - chatarra AU03 a escala;
  - calle baja con casas, neones, postes con cables y carros;
  - túnel iluminado;
  - 25 faroles AU01 con sodio;
  - luces bajo el tablero.
- **Puerto (T-1387, T-1396, T-1402):**
  - PU07 en U8;
  - 96 contenedores PU08 más dos pilas con el grafiti hacia el pasillo;
  - bodegas con alero, neón y lámparas;
  - balizas en las grúas;
  - barco a la vista;
  - postes con cables, tarimas, tambos y montacargas;
  - colliders recalculados, sin muros invisibles.
- **Uniones (T-1388):** prefab nuevo, `T1388_Uniones`, con 86 casas, 30 neones, 18 postes, 22 cables y banquetas.
- **Bocas de las uniones:** U1, U2, U4, U7 y U9 quedaron despejadas (`Correr19nov.DespejarBocasUniones`).
- **Tamaños:** la escena pesa 55.9 MB; el prefab más grande es el del Cerro, con 36 MB.

### 4. Tráfico, peatones, enemigos y drones
- **T-1389a:** 8 zonas de tráfico, con los carros nuevos repartidos por zona. Se armó Carro_V11 y la patrulla camioneta (sale con 5 estrellas), y MT01 y MT02 entraron al tráfico.
- **T-1389b:**
  - NP07 a NP14 están en la lista de peatones;
  - Nova suma E15, E16_a, E18 y el perro E17;
  - los 30 SKULLS de la escena ahora son E19 y E20.
- **T-1398:** 18 drones con ruta aérea:
  - Nova: DR02, DR04 y NV03;
  - SKULLS: E13;
  - policía: DR01, además del dron de la persecución, que ahora también es DR01;
  - ciudad: DR03 y NV01.
  Sus luces de navegación son halos.

### 5. Rendimiento
Se midió en el build de Juego con `CyberCity.exe -medirFpsZonas` a 1920x1080 (`docs/fps-19nov/fps-zonas.txt`). La vista más lenta es Cerro/plaza_arriba, con 137 fps (el 1 % más lento, 103). Todas pasan de 50.
No hizo falta combinar mallas ni poner oclusión. Lo que más pesa es la cantidad de triángulos del Cerro (unos 105 millones en vista), y eso es lo primero a bajar si una PC más débil no llega.

## Pruebas
- **EditMode de las clases nuevas y de las zonas:** 160/161 antes del último arreglo; la falla (OrillasT1358, bocas de las uniones) se corrigió y su clase dio 4/4.
- **EditMode de las tareas nuevas:** 218/222 en una corrida anterior. Las 4 fallas se corrigieron: 3 eran de E16_b (se quitó) y 1 de carros en U1.
- No se corrió PlayMode, por la memoria: Blender estaba abierto y Claude Code paró dos corridas largas por falta de memoria.

## Builds
- `Builds/Demo/CyberCity.exe` (`ConstruirDemoPandillas`): Succeeded, 0 errores, 5626 MB, 04:19.
- `Builds/Juego/CyberCity.exe` (`ConstruirJuego`): Succeeded, 0 errores, 5626 MB, 04:21.

## Avisos
- Los worktrees de los subagentes no traían Library: el código se compiló e integró en la carpeta principal. Hubo 6 errores de compilación de subagentes, ya corregidos.
- **Disco lleno:** el disco C: se llenó dos veces. Se quitaron 32 worktrees viejos del 17 y 18 de noviembre, todos limpios y ya integrados; sus ramas siguen.
- **Lo apagado:** `T-1400.QuitarColumna` había apagado la grúa del barrio y unas ventanas de T530; se volvieron a encender. `T-1403` había apagado 301 renderers legítimos (azoteas, rieles y un arroyo); también se volvieron a encender. Solo quedaron apagados los halos sospechosos y el panel T513 Tramo_3.
