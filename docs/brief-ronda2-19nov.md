# Ronda 2 (19-nov): correcciones tras las fotos de la ronda 1

Antes de empezar: en tu worktree corre `git merge --no-edit integ/ciudad-19nov` (rama local). Esa rama ya tiene aplicado en los
prefabs todo lo de la ronda 1 (T-1382..T-1389b). Lee también docs/brief-zonas-19nov.md (reglas: solo scripts, sin Unity, sin
primitivos, cero bugs, idempotente, no rehacer las zonas con ColocarT135x).
Las fotos de la ronda 1 (referencia a la izquierda, juego a la derecha) están en la CARPETA PRINCIPAL, sin versionar todavía:
C:\Users\Angel Perez\Desktop\videojuego\mecha-cyberpunk\docs\capturas\t-1384\malecon-k11.png, t-1385\cerro-k13.png,
t-1386\autopista-k18.png, t-1387\puerto-k15.png, t-1388\U*.png. Míralas con Read. Los logs de la ronda 1 están en
<principal>/Logs/t138*.txt.

## Hallazgos generales (todas las zonas)
1. Todo sale MUY oscuro y azul marino; en las referencias la noche es azul grisácea con niebla clara a lo lejos, las fachadas se
   leen bien y las luces cálidas hacen charcos de luz grandes.
2. Charcos: los de T-1383 (Assets/Arte/Mundo/Look/Charco.prefab, Lit negro con smoothness .97) salen como CUADROS NEGROS. Los
   barrios ya tienen un sistema de reflejo plano: `ReflejoCharcos` (Grep) y un material de charco S2c que usa esa textura. Los
   charcos de zona deben usar ese mismo material/sistema, con forma irregular (no cuadros).
3. Neones: casi no hay. LookNocheT1383 puso 0 luces de neón porque no encontró renderers emisivos magenta/turquesa en las zonas.
   Las zonas necesitan letreros de neón (Assets/Arte/Calle o Exteriores: NE01/NE02_NeonPared, letreros en bandera, pantallas) en las
   fachadas, con su Point Light chica (LuzNocturna, sin sombras), como en las referencias.
4. La cámara de fotos (ColocarS2cLook.Foto) tiene farClipPlane = 600 m: lo que esté más lejos (el skyline SK01 a 900 m) no sale.
