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
