# T-157 Los 20 rivales de las 4 ligas
Dueño: Tony | Rama: tony/T-157-rivales-ligas | Modelo: Sonnet 5.5, medium
Depende de: T-156

## Objetivo
Existen los 20 rivales del clandestino, 5 por liga, cada uno con su nombre, su mecha y su ritmo, y cada liga más dura que la anterior. El
quinto de cada liga es su campeón: más vida, más daño y furia de jefe (T-156). Son prefabs que el calendario de las ligas (T-158) pone en
cada cartel.

## Contexto
- Diseño: personaje-progresion.md ("4 ligas de 5 rivales, 20 peleas en total"; "Cada una paga más y tiene rivales más duros"; pregunta
  abierta "Nombres y comportamientos de los 20 rivales"), concepto-historia.md (sin copiar nombres, marcas ni personajes de otros juegos).
- Decisiones: D-tony-66 (la casera sin `PiedadRival`). Propuestas nuevas: D-tony-96 (pelea de jefe) y D-tony-97 (los 20 rivales; valores
  provisionales que se reajustan con el bloque de piezas).
- Ejemplos: `IA/Editor/CreadorRivalCasero.cs` (variante de `RivalBasico` con sus `DatosMecha`, `DatosRival` y material; idempotente),
  `IA/Rival/FuriaJefe.cs` y `ConfigFuriaJefe_Base.asset` (T-156). El jugador: `Mecha/Datos/DatosMecha_Jugador.asset` (vida 100, daño 10).

## Qué hacer
1. `IA/Editor/CreadorRivalesLiga.cs` (menú y `-executeMethod`), idempotente y sin pisar valores de assets que ya existen. Crea por rival
   `IA/Datos/Ligas/DatosMecha_Liga<L>_<N>.asset` y `DatosRival_Liga<L>_<N>.asset`, y `IA/Prefabs/Ligas/RivalLiga<L>_<N>.prefab` (variante de
   `RivalBasico.prefab`, sin `PiedadRival`). Un material de cuerpo por liga y otro por jefe (8 en `IA/Materiales/`): sótano óxido, calle
   magenta, distrito turquesa, campeonato naranja; los jefes, la versión más brillante de su color. Los jefes (N = 5) llevan `FuriaJefe` con
   `ConfigFuriaJefe_Base`.
2. Valores (D-tony-97, provisionales; `distanciaAtaque` 2.6 y `distanciaMinima` 2 en todos):

   | Liga | N | Alias | Vida | Daño | Velocidad (m/s) | Preparación (s) | Espera (s) |
   | --- | --- | --- | --- | --- | --- | --- | --- |
   | 1 Sótano | 1 | TORNILLO | 80 | 8 | 3.8 | 0.6 | 1.4-2.6 |
   | 1 | 2 | LA RATA | 90 | 8 | 4.6 | 0.6 | 1.0-2.0 |
   | 1 | 3 | EL BACHE | 110 | 11 | 3.2 | 0.7 | 1.6-2.8 |
   | 1 | 4 | MOFLE | 100 | 10 | 4.0 | 0.55 | 1.2-2.4 |
   | 1 | 5 | DON FIERRO (jefe) | 150 | 12 | 4.0 | 0.55 | 1.1-2.2 |
   | 2 Calle | 1 | NEÓN | 120 | 11 | 4.2 | 0.55 | 1.2-2.4 |
   | 2 | 2 | CUMBIA | 125 | 11 | 4.8 | 0.5 | 0.9-1.9 |
   | 2 | 3 | EL TOPE | 160 | 14 | 3.3 | 0.65 | 1.5-2.7 |
   | 2 | 4 | LA PATRULLA | 140 | 12 | 4.2 | 0.5 | 1.1-2.2 |
   | 2 | 5 | LA MATRACA (jefe) | 190 | 14 | 4.3 | 0.5 | 1.0-2.0 |
   | 3 Distrito | 1 | VOLTIO | 165 | 14 | 4.4 | 0.5 | 1.1-2.2 |
   | 3 | 2 | LA DOÑA | 175 | 14 | 4.6 | 0.45 | 1.0-2.0 |
   | 3 | 3 | GRÚA | 210 | 17 | 3.4 | 0.6 | 1.4-2.6 |
   | 3 | 4 | EL PERITO | 190 | 15 | 4.4 | 0.45 | 1.0-2.0 |
   | 3 | 5 | EL ALCALDE (jefe) | 250 | 17 | 4.5 | 0.45 | 0.9-1.8 |
   | 4 Campeonato | 1 | OBSIDIANA | 220 | 17 | 4.5 | 0.45 | 1.0-2.0 |
   | 4 | 2 | XOLO | 225 | 17 | 5.0 | 0.4 | 0.8-1.6 |
   | 4 | 3 | TLÁLOC | 270 | 20 | 3.6 | 0.55 | 1.3-2.4 |
   | 4 | 4 | LA LLORONA | 240 | 18 | 4.6 | 0.4 | 0.9-1.8 |
   | 4 | 5 | EL CACIQUE (jefe) | 320 | 22 | 4.6 | 0.4 | 0.8-1.6 |

   El nombre del mecha (`DatosMecha.nombre`) es el alias en minúsculas con mayúscula inicial. Aceleración, frenado, giro, recarga y bloqueo,
   los de `RivalBasico` (no se tocan).
3. EditMode `Tests/EditMode/IA/RivalesLigaPruebas.cs` sobre los assets reales (`AssetDatabase`), con las reglas de D-tony-97:
   a) hay 20 prefabs, cada uno con `RivalBasico` cuyo `DatosRival` tiene alias único y no vacío, y `VidaMecha` con el mismo `DatosMecha` que
   su `DatosRival`; b) ninguno tiene `PiedadRival`; solo los N = 5 tienen `FuriaJefe` con config; c) el rival 1 de la liga 1 no supera al
   jugador en vida ni en daño; d) el promedio de vida y el de daño de cada liga son mayores que los de la anterior; e) en cada liga el jefe
   tiene la vida más alta y un daño mayor o igual que el de cualquier otro de su liga; f) `distanciaAtaque` no pasa del alcance del golpe.
4. PlayMode `Tests/PlayMode/IA/RivalesLigaPruebas.cs`: instancia cada uno de los 20 en una arena de prueba, espera un cuadro y comprueba que
   su `IPeleador.Nombre` es su alias, que su vida es la de su asset y que la consola no tiene errores.

## Carpetas permitidas
Assets/_Proyecto/IA/ (Editor/, Datos/Ligas/, Prefabs/Ligas/, Materiales/), Assets/_Proyecto/Tests/EditMode/IA/, Assets/_Proyecto/Tests/PlayMode/IA/
No tocar: Core/, los prefabs de rival que ya existen, Pelea/, escenas, todo lo de Alan.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa (salvo los 9 fallos conocidos de Alan en Arena)
- [ ] RivalesLigaPruebas (EditMode) pasa las reglas a a f; RivalesLigaPruebas (PlayMode) pasa con los 20
- [ ] El creador corrido dos veces no cambia ningún asset (md5 igual de los 20 prefabs y sus datos); cada asset tiene su .meta en el commit
- [ ] Captura docs/capturas/T-157-1.png con los 5 rivales de una liga y su jefe lado a lado (se distinguen por color y el jefe por brillo)

## Criterios de sensación (para un humano)
- Más adelante, con la cartelera (T-160): ¿cada rival de la liga 1 se siente distinto (la rata rápida, el bache lento y pesado)? ¿Los nombres
  suenan a barrio mexicano cyberpunk sin parecer copiados? Tony puede cambiar alias y valores en los assets.
- Aviso: las ligas 3 y 4 están pensadas para un mecha con piezas mejores (bloque 3); con el mecha base son casi imposibles. Es esperado.

## Pruebas automáticas
- EditMode: RivalesLigaPruebas. PlayMode: RivalesLigaPruebas.

## Fuera de alcance
- Premios, momios y carteles (T-158). Comportamientos nuevos (bloquear, esquivar, combos) y adaptación del rival (bloque 7). Modelos finales.
