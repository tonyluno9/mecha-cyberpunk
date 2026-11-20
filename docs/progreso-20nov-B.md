# Progreso 20-nov B (pendientes de docs/playtest-20nov.md sin Blender ni chat de arte)

Pedido completo: el /goal del 20-nov (Tony). Regla: nada se marca hecho sin verlo en el juego desde el jugador, con captura en
`docs/capturas/_20nov-B/`. Si no se pudo comprobar, se dice.

## Dónde está todo
- Carpeta principal: en `main`. **El chat de Blender trabaja ahí** (imageassets/blender): no hacer checkout ni restore ahí sin revisar.
- Integración y Unity: `.sesiones/s2` en la rama `integ/20nov-C` (tiene su Library). Una sola instancia de Unity.
- Subagentes Sonnet: worktrees dispersos (sin imageassets ni binarios LFS) en `Desktop/videojuego/wt20/T15xx-*`, sin Library: no corren Unity, dejan `Aplicar()` para -executeMethod.
- Al final: merge --no-ff de integ/20nov-C a main, push, builds Demo y Juego en la carpeta principal (uno por uno).

## Paso 0 (hecho)
- Cambios sin guardar de la carpeta principal en `tony/blender-20nov` (5 commits por tamaño: GitHub no acepta un push de más de 2 GB)
  y merge a main 601dd62 (subido). Fuera del commit: `Audio-20261031T070509Z-1-001.zip` (142 MB, GitHub no lo acepta),
  `Logs_v121.txt`, `__pycache__`, respaldos `.blend1` y `imageassets/blender/_REVISION_TONY/revision_personajes.blend`
  (140 MB y el chat de Blender lo sigue editando). Siguen en disco.
- Unity y Hub cerrados, sin lockfile.

## Tareas
| Tarea | Punto | Rama | Estado | Comprobado en juego |
| --- | --- | --- | --- | --- |
| T-1520 | 1 Inicio sin cuarto | tony/T-1520-sin-cuarto | pendiente | — |
| T-1521 | 2 Escalas de humanos, civil acostada | tony/T-1521-escalas | pendiente | — |
| T-1522 | 3 Armería mostradores y armero | tony/T-1522-armeria | pendiente | — |
| T-1523 | 4 Formas básicas → ED, nada flotando | tony/T-1523-sin-primitivas | pendiente | — |
| T-1524 | 4 Muro cerrado, puertas sólidas | tony/T-1524-muro | pendiente | — |
| T-1525 | 4 Costales CB02, agua sin salpicadura | tony/T-1525-costales-agua | pendiente | — |
| T-1526 | 5 Carros dentro del muro, patrulla, sonido | tony/T-1526-carros | pendiente | — |
| T-1527 | 6 Armas | tony/T-1527-armas | pendiente | — |
| T-1528 | 7 Enemigos | tony/T-1528-enemigos | pendiente | — |
| T-1529 | 8 Tienda e implantes | tony/T-1529-tienda | pendiente | — |
| Final | Builds, reporte-20nov-B, cerrar Unity | — | pendiente | — |

## Bitácora
- 20-nov: paso 0 hecho. integ/20nov-C creada en s2 desde main 601dd62.
- 20-nov: 10 subagentes lanzados (T-1520..T-1529) en wt20/. Compilación base de main en s2 corriendo.
- T-1522 entregada (93aadb9): ArmeriaT1522.Diagnostico/Aplicar. Racks I18 de la pared a yaw -90 (como antes de T-1503), vitrinas I17 de enfrente 180°, armero GTA_T407/Mercado/Vendedor hacia la pared. Captura: jugador (-24,0.3,26.25) mirando -X.
- Entregadas por subagentes: T-1522 (93aadb9), T-1524 (1c48d4f), T-1525 (84c926a), T-1527 (53832ae), T-1528 (38d603f+1442a5f), T-1529 (bbc1f78+48c3fde). T-1523 en segunda vuelta (fondo del menú Inicio hecho con cajas, maletín/dinero/munición).
- Lote 1 aplicado y commiteado (3591e79): armería, muro (6 puertas sólidas, colliders de 10 m), costales, ondas. OJO muro: las calles S2_Calles llegan a x±277 z±227 y una Calzada elevada (cima 10.5 m) está junto al muro norte en z=99: revisar en captura si se sale por ahí.
- Lote 2 (T-1527/T-1528 métodos) corriendo.
- Todas las ramas integradas en integ/20nov-C (T-1520..T-1529). Lote 2 aplicado (8011045). Lote 3 (sin cuarto, primitivas, fondo del menú, pickups, tráfico, tiendita, vida de implantes/prótesis, escalas, colliders de postes) corriendo.
- Siguiente: compilar limpio, pruebas por clase de cada tarea, luego Unity vivo en s2 para capturas desde el jugador en docs/capturas/_20nov-B/.
- Lote 3 primera vez: T-1523 apagó 373 piezas "sin equivalente" incluyendo el asfalto de Barrio/Calles (272x178 m), losas y pilares de autopista, puentes, fachada del Bar y gradas (pisos invisibles con collider). Escenas regresadas a HEAD; PrimitivasT1523 ahora deja estructura (asfalto, calle, losa, autopista, pilar, puente, fachada, grada...) y nada de más de 5 m. Prefabs y datos de lote 3 (escalas, colliders, implantes, tiendita) se quedan. Lote 3b corriendo.
- Escalas medidas animadas (Logs/t1521-alturas.txt): jugador 1.80, Doc 1.80, civiles 1.66-1.80, pandilleros 1.78-1.82. GuardiaNova mide 2.41 (fuera de la tabla, como los de Nova).
- EditMode completo tras lote 3: 4127/4175 (29 fallas). 8 ya fallaban en main (V22 x3, Patrulla PA01 x3, clip PJ01_Moto_Manejar, costales CB02 en la calle de Nova S2c). Pruebas viejas actualizadas a lo que pidió Tony (93ea0a8). Regresiones devueltas: T-1523 (varillas de azotea cambiadas por tambos flotando, tianguis, fachada de Tino, encimados) y T-1521 (Hacker medido sentado: 2.43 m de pie; ya mide de pie, ff86e5c). Colliders de postes: cápsula en la base real de la malla.
- Siguiente: merge de T-1523 corregida, escenas a 8011045 y lote 3c (todos los métodos de escena otra vez), EditMode completo, PlayMode por clase, capturas.
- Lote 3c (3ed7351). EditMode completo: 4144/4175, solo las 8 que ya fallaban en main. PlayMode tanda A (jugador/UI/progresión) corriendo; luego tanda B (IA) y capturas con Capturas20nB (Progresion/Prologo/Editor).
- PlayMode tanda A: cada clase en verde corrida sola (Consumibles 7/7, InventarioRueda 4/4, Inicio/Calor/Escala 17/17 juntas). Las fallas juntas son de orden (CalorMecha deja al jugador como en vehículo), como dice la memoria. Arreglos reales: ranuras sin fijar todo (la ranura 2 no salta a la 1), pantalla negra si la escena se descarga a media levantada (e4cd914). Tanda B (IA) corriendo.
- Capturas en Unity vivo (docs/capturas/_20nov-B). Comprobado en juego: inicio en la calle levantándose (Animator Desmayo -> Levantarse -> control), Doc a la altura del jugador (cabeza 1.51 vs 1.53), armería, muro (6 puertas: el jugador se queda a 2 m de la línea), costales, ondas en piso mojado sin salpicadura, una arma por tipo, tiendita ED02 (lote baldío de la calle del Doc; las 20 máquinas ya no venden), prótesis +15 vida (150->165), calor x0.5 (+10 de 20; misión entera), maleantes provocan sin disparar.
- Encontrado en juego y arreglado: tráfico afuera del muro (recorte no corría; T-1526 2a y 3a vuelta), atasco en callejón sin salida, tiendita en medio de la calle, maleantes de la ciudad usaban datos E19E20/E10 sin provocar, 9 edificios de cubo rellenos con ED apilados.
- NO quedó: 143 casas generadas como caja (S2c_*, CalleDoc) y fachadas de cubo de Clandestino/Bar/MercadoNegro (tienen interior: hace falta modelo con entrada). Civil acostada: no reproducida (hay guarda T-1521).
- Pendiente: PlayMode tanda C (riñas, jefe, balas ciudad, provocación) corriendo; merge de T-1528 dec558b; EditMode final; main; builds; reporte.
- EditMode final: 4146/4178 (solo las 8 de main + 1 de T-1526 ya arreglada). PlayMode IA (riñas, jefe) en verde solas. Tráfico PlayMode: línea base con el código de main falla 6 (PersecucionBarrio x4, Cierres, atorón 126 s); el recorte de T-1526 agregaba 4-5 (aparición fuera de vista, carros prendidos). T-1526 cuarta vuelta: mover las calles de afuera adentro de la puerta en vez de desconectarlas.
- Reporte en borrador: docs/reporte-20nov-B.md.
- Tráfico T-1526 quinta vuelta integrada: en Play 0 carros/patrullas fuera y se mueven. PlayMode tráfico: 8 fallas (6 de la base de main + 4 de aparición; ver reporte). Siguiente: merge a main, builds en la carpeta principal.
