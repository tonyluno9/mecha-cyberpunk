# Meter assets nuevos (noche 6-nov)

## Sesión 2 (mundo)

El importador es `Assets/Arte/Editor/ImportarS2n.cs`. Tiene una tabla `Entradas`, con una fila por asset: el archivo de `imageassets/`,
dónde va en `Assets/` y qué hacer con él.

**Cómo correrlo**
- En Unity: menú **Mecha Cyberpunk > S2n > Meter assets nuevos**.
- Con Unity cerrado (Bash, desde la raíz del repo):
  `"/c/Program Files/Unity/Hub/Editor/6000.3.25f1/Editor/Unity.exe" -batchmode -quit -projectPath . -executeMethod MechaCyberpunk.Arte.Editor.ImportarS2n.MeterTodo -logFile Logs/importar-s2n.log`
  (lo que hizo queda en `Logs/s2n-importar.txt`).
- Para que las líneas vuelvan a ponerse en la ciudad, corre después el menú **S2n > 6 Lineas de la calle**
  (`ColocarS2nLineas.Colocar`). Ese paso ya llama al importador. Las capturas `docs/capturas/s2n-lineas-*.png` salen con
  `ColocarS2nLineas.Fotos`.

**Qué hace con cada fila**
1. Si el archivo está en `imageassets/`, lo copia a su destino, pero solo si es distinto del que ya hay.
2. Si el archivo no está y el destino tampoco, hace un respaldo en Unity: una textura procedural guardada como PNG en el mismo destino.
   Cuando llegue el archivo de verdad, la siguiente pasada lo copia encima del respaldo.
3. Pone los ajustes de importación según la acción. `MascaraCalle`: el alfa sale del gris (el fondo negro queda transparente), la textura
   se repite, lleva mipmaps y aniso 8.
4. Arma los materiales: URP/Lit transparente, sin escribir profundidad y sin sombra, pintado encima del asfalto. Van sin
   "conservar especular" (Preserve Specular): con esa opción URP suma el brillo del quad entero y el fondo negro se ve como una banda clara.

Correrlo dos veces no cambia nada. Lo comprueba la prueba `LineasS2nPruebas.Importador_DosVeces_NoCambiaNada`.

| Archivo en imageassets/ | Destino | Uso | Respaldo hecho en Unity |
| --- | --- | --- | --- |
| `calle/M01_LineasAmarillas.png` (doble raya amarilla vertical, se repite hacia arriba, fondo negro) | `Assets/Arte/Calle/Lineas/M01_LineasAmarillas.png` + `.mat` | Doble amarilla al centro de cada calle | Dos franjas amarillas gastadas (ruido Perlin) |
| `calle/M02_CrucePeatonal.png` (franjas blancas de cebra, fondo negro) | `Assets/Arte/Calle/Lineas/M02_CrucePeatonal.png` + `.mat` (se repite 2 veces a lo ancho) | Cebras en cada boca de los cruces | 8 franjas blancas gastadas |
| (no hay; no hace falta) | `Assets/Arte/Calle/Lineas/M03_LineaBlanca_Unity.png` + `M03_LineaBlanca.mat` | Rayas blancas discontinuas de carril | Siempre es la hecha en Unity |

Hoy M01 y M02 ya están en `imageassets/calle/` y son los que se usan.

**Cómo agregar más assets** (D01/D02 huecos de bala, H01-H04 hologramas, V09-V11 carros)
- Agrega una fila a `Entradas` con `new Entrada(origen, destino, Accion.X, respaldo)`.
- Si hace falta otro tipo de importación (por ejemplo un modelo .zip o .fbx), agrega un valor a `Accion` y su caso en `Ajustes`. No cambies
  las filas de otros.

**Carros V09-V11 (T-810)**
- Filas `Accion.CarroTripo` en `Entradas`: `imageassets/modelos3d/V09_Taxi.zip`, `V10_Pickup.zip` y `V11_Van.zip` van a
  `Assets/Arte/Vehiculos/V09/V09.fbx` (igual con V10 y V11). El trabajo lo hace `Assets/Arte/Editor/ImportarS2nCarros.cs`.
- Si el zip no está, no hace nada: anota en `Logs/s2n-importar.txt` "falta V09_Taxi.zip ..." y no abre la escena. Lo comprueba
  `CarrosS2nPruebas.Importador_SinZip_NoHaceNadaYAnotaQueFalta`.
- Cuando llega el zip, la siguiente pasada del importador hace esto:
  1. Desempaca el `.fbx` como `V09.fbx`. La textura de color (`*basecolor*` o la `tripo_image_*_0_0.jpg`) queda como `V09_basecolor.jpg`
     y la normal (`*normal*` o `*_0_3.jpg`) como `V09_normal.jpg`. Es la misma convención de V02-V08.
  2. Arma el prefab del arte con el flujo de T-250, como `ImportarT511`: frente a +Z, el largo real y los pies en y = 0. Los largos
     son taxi 4.5 m (como el sedán V02), pickup 5 m (como V03) y van 4.7 m (como la combi V04).
  3. Arma el carro manejable con `CreadorCarrosT511.CrearUno`. Las ruedas de greybox quedan invisibles, como en los otros carros de Tripo.
  4. Arma `Trafico/Prefabs/CarroTrafico_V09.prefab`, igual que `CreadorTraficoT511`.
  5. Lo agrega a `TraficoCalle.prefabsCarro` de Ciudad_Prologo.
- Si un modelo sale de lado o de reversa, cambia su giro en `ImportarS2nCarros.Medidas` (hoy 180°, como V05 y V06) y vuelve a
  correr el importador. Revisa la hoja `Logs/s2n-carros.png`.
- Para correrlo todo, carros y variantes: menú **Mecha Cyberpunk > S2n > 8 Variedad de carros del trafico**
  (`ColocarS2nCarros.Colocar`), que llama al importador.

**Variantes de color (T-810, hoy)**
- `ColocarS2nCarros` hace dos variantes de color de cada carro civil: V02 rojo y azul, V03 verde y gris, V04 naranja y turquesa.
  Cada una es una copia del material con otro tinte y una variante del prefab CarroTrafico (mismo comportamiento). Quedan en
  `Assets/Arte/Vehiculos/Variantes/` y están en el tráfico.
- El tráfico queda con 11 modelos: V01a, V01b, V02, V03, V04 y las 6 variantes.
- La moto de reparto V07 no se agregó: el tráfico no tiene motos y su prueba no las cubre.

## Sesión 1 (jugador, armas, policía)

El importador es `Assets/Arte/Editor/ImportarS1n.cs`. No toca las filas de ImportarS2n. Mete cada archivo solo cuando aparece y correrlo otra vez no cambia nada
(lo comprueba `ImportarS1nPruebas.Importador_DosVeces_LaSegundaNoCopiaNada`).

**Cómo correrlo**
- En Unity: menú **Mecha Cyberpunk > S1n > Meter assets nuevos**.
- Con Unity cerrado (Bash, desde la raíz del repo):
  `"/c/Program Files/Unity/Hub/Editor/6000.3.25f1/Editor/Unity.exe" -batchmode -quit -projectPath . -executeMethod MechaCyberpunk.Arte.Editor.ImportarS1n.MeterTodo -logFile Logs/s1n-importar.log`
  (lo que hizo queda en `Logs/s1n-importar.txt`).
- Busca los archivos en `imageassets/` del proyecto y, si no, en `../mecha-cyberpunk/imageassets/` (el worktree mecha-s1 no tiene la suya).

**Qué hace con cada archivo**

| Archivo en imageassets/ | Destino | Qué se arma después |
| --- | --- | --- |
| `modelos3d/W02.zip` escopeta, `W03.zip` fusil, `W04.zip` francotirador | `Assets/Arte/Armas/Wxx_Nombre/` (`.fbx` y `Texturas/`, como W01_Pistola) | `ColocarS1nArmas` (T-679): material, prefab de mano (`Wxx_Nombre.prefab`, cañón a +Z, empuñadura en el origen, hijo Boca), prefab de piso (`..._Piso.prefab` con ArmaEnPiso) y el modelo en la mano del jugador (JugadorAPie.prefab) |
| `modelos3d/C01.zip` policía | `Assets/Arte/Personajes/Policia/` (`Policia.fbx`, `Policia.fbm/`) | `ColocarS1nPolicia` (T-680): visual humanoide `Visual_Policia` y prefab `IA/Prefabs/PoliciaAPie.prefab` |
| `vfx/F01_Destello.png` | `Assets/Arte/VFX/F01_Destello.png` | Se asigna a `DatosSensacion.texturaDestello` |
| `ui/hologramas/H01-H04` | Ya los metió la sesión 2 (T-816) en `Assets/Arte/UI/Hologramas/` | El importador solo lo comprueba; si faltan llama a `ImportarS2nHologramas` |

Si un archivo no ha llegado, lo anota en el informe y sigue con los demás.

**C01 policía: sí trae esqueleto.** El .fbx trae 78 huesos y un Avatar Humanoid válido (igual que man). Tripo lo entrega sin importarlo como Humanoid hasta que
`PostprocesadorAssetsT250` lo reconoce ("Policia" está en `PersonajesDeTripo` y en `CreadorVisualTripo.Nombres`, mide 1.85 m). `PoliciaAPie` usa sus animaciones
humanoides. Si algún día llega un C01 sin esqueleto, el importador detecta que no hay huesos y arma `PoliciaAPie` con el modelo man y un material azul
marino (`man_PoliciaMarino.mat`); cuando vuelva a llegar uno con esqueleto lo cambia solo. En ese caso hay que pasar el modelo por Mixamo.

**Qué falta**
- Los modelos nuevos de las armas son una sola malla: en el banco de Tino se muestra la imagen holográfica (H02-H04), no el modelo 3D con piezas que se separan.
- La lógica de que el policía se baje de la patrulla y dispare va en un lote posterior. `PoliciaAPie` ya tiene vida, colisión, pistola W01 e IA a pie, pero no hay
  ninguno colocado en la ciudad.
- Si alguien corre `CreadorArmas.Crear` o `CreadorJugadorAPie.RegenerarPrefabTerceraPersona`, el prefab del jugador vuelve a las cajas: correr después el importador.
