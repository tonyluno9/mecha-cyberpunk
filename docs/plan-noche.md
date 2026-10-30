# Plan de la noche (30-oct-2026) — Tony
Léelo completo antes de empezar. Hazlo en este orden y no te detengas: decide lo razonable y anótalo en una línea en
docs/decisiones-pendientes-tony.md. Rama de integración: tony/fase-g (una rama por tarea, merge --no-ff).

## Reglas
- Modo rápido pero bien hecho: sin revisor ni verificador; el implementador compila y corre solo las pruebas nuevas o de lo que tocó.
  Opus para diseño de UI, guardado y planes; Sonnet para lo mecánico. Opus también si algo falla 2 veces.
- Al terminar CADA paso: merge a main con push (yo, Tony, lo autorizo), regenera Builds/Juego y Builds/Demo, y 3 líneas de
  qué probar en docs/guia-para-jugar.md.
- Autorizo tocar todas las áreas (también Mundo/, Arena/, Vehiculos/, Casino/, Trafico/, Arte/). Nunca borres el arte de T-250
  ni de este plan: si regeneras Barrio.prefab, vuelve a correr ColocarT250.Colocar y la colocación nueva.
- No borres código del mecha. Nunca force push. git restore a lo que Unity cambia solo; no subas Assets/InitTestScene* ni Temp/.
- 60 FPS de noche en la calle principal es obligatorio: mídelo después de cada paso que agregue cosas al mundo.
- Si se acaba el límite, espera y continúa. Bitácoras de una línea.

## Paso 1 — Terminar la ronda 2 (T-407, quedó en WIP sin empezar)
- Nombre: el juego se llama CYBERFIX por ahora, pero el nombre aún no está decidido: ponlo en UNA sola constante/asset
  (productName, título, ventana, .exe) para cambiarlo en un minuto. Quita "Mecha" de todo lo que ve el jugador.
- Silencio: apaga lluvia sonora, música y el audio de ambiente actual (no borres archivos).
- Bugs: no se llegaba a la escopeta; no se encontraba El Tianguis; el mercado negro estaba vacío. Recórrelo con entrada
  simulada como jugador nuevo: vendedor y mostrador visibles, letrero, "E para comprar", tienda funcionando; zona marcada;
  precios y dinero que cuadren.
- GestorSonidos por ID según la Lista 1 de docs/listas-equipo-cyberfix.md (archivo que falta no suena ni da error;
  volumen por sección; AMB y MUS apagados).

## Paso 2 — Nombres y decisiones del equipo (documento del juego, 29-oct)
Cambia en todo lo que ve el jugador (textos, HUD, mapa, letreros de Unity, datos):
| Antes | Ahora |
| --- | --- |
| La Fosa (tu zona) | Suburbios |
| Los Calaveras | SKULLS (magenta) |
| Las Víboras | SNAKES (verde ácido) |
| Neotek | Nova |
| La ciudad | Megalopolis |
| Mercado negro | El Mercado |
| Clínica | I+D+I |
| Bar / Cantina El Chip | El Fox |
| Casino | VivaCoin |
| Taller del Inventor | CIC |
| Moneda ($) | Biits (símbolo propio en HUD y tiendas) |
- Tu pandilla: el jugador escribe su nombre y elige color en la creación de personaje; se usa en HUD, mapa y textos.
- Armas: puños, pistola, escopeta, fusil (el subfusil pasa a fusil de asalto) y sniper nuevo (zoom con clic derecho,
  pocas balas, mucho daño). Las 5 se compran y mejoran en 3 niveles en El Mercado.
- Peatones: los menos posibles (bájalos a un mínimo que no se note vacío).
- Personajes de apoyo (profes): El Inventor (TINO) en el CIC mejora armas; El Doc (JOEL) en I+D+I cura y pone implantes;
  El Hacker (SERGIO) da información; El Mecánico (CANEPA) arregla carros; El Director (ESTRADA) neutral. Por ahora como
  NPC de prueba con su nombre y función; el modelo llega después.
- Historia de la demo: despiertas en Suburbios, dos SKULLS molestan a un vecino, pelea a golpes, recoges la pistola, reclutas al
  primer aliado, El Doc te cura y El Inventor te mejora la pistola, conquistas El Tianguis venciendo al jefe de los SKULLS;
  después vas a Neón Alto, El Hacker descubre que Nova les paga y les da armas a los SNAKES, y sale "CONTINUARÁ".
  Neón Alto se puede conquistar en el juego libre.

## Paso 3 — Ronda 3 (docs/ronda3.md)
Marcador de objetivo nuevo, minimapa, mapa grande, pausa, opciones, guardar/cargar con 3 ranuras, pantalla de título y
creación de personaje (2 personajes, nombre y color de tu pandilla). Todo con los nombres del paso 2.

## Paso 4 — Assets 3D nuevos (39 modelos)
Origen: C:\Users\Angel Perez\Desktop\videojuego\mecha-cyberpunk\imageassets\modelos3d (5 zips; Paquete4, Parte1, Parte2 y Parte3
traen un zip por modelo adentro; CY04_AntenaAzotea.zip trae el FBX directo). Descomprime en Temp/assets_noche (fuera de git).
Importa con el mismo flujo de T-250 (PostprocesadorAssetsT250, ArteT250, Assets/Arte/<Zona>/<Código>/, prefab con hijo Visual,
material URP, frente en -Z) y colócalos:
- Kit de pisos (C20a, C20b, C20d macetas, C21 liso, C22 cornisa, C23 franja): apílalos sobre cada fachada del Barrio hasta la
  altura de su edificio greybox, alternando variantes, cornisa arriba y franja entre planta baja y pisos; apaga solo las caras
  cubiertas del bloque viejo. Colliders intactos. Los edificios ya no deben verse cortados.
- C01/C02/C03 _cy: reemplazan a las C01/C02/C03 de T-250 (misma medida y posición).
- Props cyberpunk CY01–CY10 (proyector holo, pantalla vertical, mazo de cables, antena de azotea, servidor callejero,
  expendedora, plataforma de dron, cámara corporativa, puesto de implantes, cargador): repártelos por las calles, más
  densos en El Tianguis y Neón Alto; CY09 junto a I+D+I; CY08 en Neón Alto; CY04 en azoteas.
- CY07b Dron: visual del dron de vigilancia de la policía.
- E01 entrada del casino VivaCoin, E02 portón de El Mercado, E03 pantalla gigante en Neón Alto, A08 marcador del clandestino,
  A11a/b portal y escalera del sótano en la entrada del clandestino.
- Taller y casa: T06 soldadora, T07 grúa, T08 repisa en el taller; H06 tele, H07 sofá, H08 estufa y tarja, H09 repisa con
  veladoras, H11 buró en la casa (quita el cubo greybox junto a la cama).
- Calle: C13 puesto ciber y C16 tambos, C17 huacales en El Tianguis; C18 barreras en las orillas de zona; C19 moto estacionada.
- Si un modelo pasa de 30 mil triángulos o baja los FPS, ponle LOD o usa menos copias y anótalo en docs/assets-importados.md.
- Capturas antes/después de calle, callejón, Tianguis, Neón Alto, casa y taller en docs/capturas/noche-*.png, revisadas por ti.
- Agrega las filas a docs/assets-importados.md.

## Paso 5 — Mejoras por prioridad (docs/mejoras-pendientes.md)
Haz en orden los puntos 1 a 5 (sensación de disparo, cubrirse, IA más lista, apuntado asistido, rendimiento), luego 6 a 11,
luego 12 a 18, hasta que se acabe la noche. Excepción: el punto 13 (asaltar peatones) queda fuera porque habrá pocos peatones.
Ciudad grande (19) y cooperativo (24) NO.

## Al terminar o al parar
docs/estado-tony.md: qué quedó en main, qué falta y qué probar primero mañana (5 líneas).
