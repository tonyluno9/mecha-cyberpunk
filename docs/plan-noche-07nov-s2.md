# Noche 7-nov, sesión 2: el look de K01 en la ciudad (docs/plan-look-ciudad.md)

Fuente: el /goal de Tony del 7-nov (calle de prueba idéntica a K01, kit de manzanas, ciudad por cuadras). IDs T-870 en adelante para no chocar
con la sesión 1 (T-69x).

## Reglas
- Carpeta principal; la sesión 1 trabaja en `../mecha-s1` y tiene `main` abierto, así que la integración es en HEAD separado sobre
  `origin/main` con `git push origin <commit>:main` (merge `--no-ff` hecho con commit-tree), como en la noche del 6-nov.
- No se tocan scripts del jugador, HUD, vehículos, policía ni misiones. Archivos nuevos que solo leen sí.
- Lo nuevo va en código de editor idempotente (`Assets/Arte/Editor/ColocarS2c*.cs`, `KitS2c.cs`, menú Mecha Cyberpunk/S2c).
- Nunca CreadorTodo; Barrio.prefab no se regenera (lo que se quita del barrio se apaga en la escena).

## Decisiones (tomadas sin preguntar, por la regla "decide lo más razonable")
- **La §8 del plan no existe**: el plan llega a la §6. La distribución de la ciudad es la de la §4 ("Mapa nuevo: ciudad por cuadras")
  más el mapa corregido M11 (imageassets/referencias/look/M11_MapaCiudad.png).
- **La red de calles del tráfico se queda.** El tráfico, las patrullas, la ruta del mapa y los peatones de la sesión 1 están escritos para
  la rejilla de 3 × 2 manzanas de 80 m con paso de 94 m (`RutaCallesBarrio`, `GrafoCalles`, `CreadorPeatones`). Cambiar esas calles las
  dejaría pasando por dentro de las casas. Las cuadras cortas se hacen partiendo las manzanas con calles nuevas sin tráfico (como la calle
  de prueba) y cambiando lo que hay a los lados de las calles de siempre.
- **La calle de prueba va dentro de la manzana de la casa** (fila de en medio de Manzana_4, de x = -57 a -131, z = -43), junto al callejón
  de los SKULLS: es la calle chica de K01 (11.5 m de fachada a fachada, arroyo de 7.5 m) y se ve desde la calle de la casa.

## Tareas
| ID | Qué | Estado |
| --- | --- | --- |
| T-870 | Calle de prueba K01: posproceso (ACES, bloom, sombras azules y luces naranjas), niebla azul, contornos de pantalla completa, piso mojado con charcos que reflejan (reflejo planar), halo bajo cada lámpara, neones con su luz, ventanas cálidas al azar, cables colgantes; capturas s2c-calle-prueba-*; pedido docs/pedido-arte-08nov.md | hecha |
| T-871 | Kit de manzanas: frentes de cuadra A–F (prefabs), esquina como K05 (semáforo, cruces, negocios, FARMACIA) y variante Nova como K06 | |
| T-872 | Suburbios con el kit (ruta de la demo): frentes de las calles de la casa, cuadras cortas, sin calles infinitas | |
| T-873 | El Mercado: tianguis sin focos colgando, plaza | |
| T-874 | Neón Alto | |
| T-875 | Nova | |
