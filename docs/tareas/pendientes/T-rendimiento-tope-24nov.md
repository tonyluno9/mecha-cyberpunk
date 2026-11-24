# T-rendimiento-tope-24nov · Tope de enemigos para que nunca baje el fps
**Estado:** PENDIENTE
**Lo que pidió Tony:** al jugar y probar el modo libre, evitar que el juego se sature y dé un bajón de fps. El número de enemigos con el que empieza a saturarse es el límite.
**Qué hacer:**
1. Jugar el modo libre (PC y simulador VR, en el build, no solo en el editor) llevando cada fase al máximo, midiendo fps (promedio y 1% más bajo) contra número de enemigos vivos, vehículos, drones, ragdolls, efectos y luces activas.
2. El tope = el número de enemigos simultáneos donde el fps se mantiene: PC ≥ 60 fps; VR ≥ 72 fps estables (con RTX 5070 Laptop). Si VR necesita un tope menor, usar uno por modo.
3. Poner ese tope en ConfigRespuesta (enemigos simultáneos, patrullas, drones, cápsulas, ragdolls vivos, efectos) y que el goteo de refuerzos lo respete: llegan nuevos solo al morir otros.
4. Además: los cuerpos muertos desaparecen tras ~20 s (fundido), las balas/casquillos/efectos tienen tope, y lo que no se ve se apaga.
5. Anotar en el reporte la tabla de mediciones y los topes elegidos.
**Cómo comprobar (Tony):** jugar hasta NOVA pesada sin bajones.
