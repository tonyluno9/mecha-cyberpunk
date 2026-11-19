# T-1464 Autopista elevada como K18 y A18, con vida

Rama: tony/T-1464-autopista (sale de integ/look-19nov-A). Script: Assets/Arte/Editor/ZonaAutopistaT1464.cs. Raiz en el prefab T1356_Autopista: T1464_Autopista (idempotente).
Estado: escrito sin poder compilar ni correr (worktree sin Library); falta la primera corrida de Opus.

## Que hace
- Abre el ramal curvo de T-1415 (quita reja y letreros de OBRA) y ilumina su segunda boca (tiras emisivas, panel de fondo, 1 spot).
- Faroles AU01 de sodio nuevos cada 20 m en el borde sur del tablero (con luz y halo), omitiendo donde ya hay uno.
- Estelas de luz: cintas emisivas (rojas y blancas, sin Light) sobre los 4 carriles del tablero y los 2 del ramal.
- Vida: chatarra AU03, carros deshechos y estacionados, tambos C16, huacales C17, basura BS01/BT01/C11 y charcos, sin encimarse con lo ya puesto.
- Marcas Vista19b_K18 y Vista19b_A18 (Mira y Campo_<grados>), tomadas de Logs/t1464-camara.txt.
- PoblarFoto(Camera) o PoblarFoto(desde, hacia, campo) pone carros y peatones NP visibles; QuitarFoto() los quita.
- No se curva el tablero principal (recto por z = 250): K18 usa la rampa norte, una columna del tablero y la boca principal; A18 suma el ramal curvo abierto.

## Orden de corrida
Aplicar, (corrida nueva con la escena) BuscarCamara, PonerMarcas, Validar, Vista19b.Fotos -tomas K18,A18.

## Criterios (Validar, Logs/t1464-validar.txt)
Faroles, estelas >= 30, vida minima, luces <= 40, ramal abierto, K18 (boca x .65-.95 y .5-.85, columna x .5-.72, rampa centro-izquierda, torre arriba-izquierda), A18 (boca derecha, tablero cruza el cuadro, ramal visible), PoblarFoto con carros y peatones.

## Assets que faltan
- Visuales NP01 a NP06 (solo existen NP07 a NP14).
- Tablero curvo hacia el tunel principal (hoy es recto).
- Material aditivo transparente para estelas (las actuales son opacas con degradado oscuro en las puntas).

## Ronda 2 (tras la primera corrida)
- BuscarCamara K18 ahora exige columna (de la rampa norte con tablero a 3 m o mas, o del principal) a 10-25 m con rayos al fuste, ancho en pantalla 7-14 %, x 0.55-0.65, mirada 8-15 grados hacia arriba, torre visible por rayo arriba a la izquierda. Aplicar engrosa esas columnas a 3.9 m.
- A18 se busca desde el norte-este (x -140..0, z 290..380, y 50-70) mirando 20-40 grados hacia abajo, boca a la derecha, tablero cruzando, torre lejos a la izquierda.
- Validar usa rayos de visibilidad y falla si no se cumple.
