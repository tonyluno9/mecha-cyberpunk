# Hoja de ruta hasta la demo y después

Basada en docs/diseno/ (sobre todo demo.md y su orden de construcción) y en la división D-026: Tony hace el jugador y
los sistemas y datos; Alan hace el mundo. Las dos personas avanzan en paralelo y se encuentran en hitos de integración.
Lo que entra en la demo va primero; lo que no entra va al final (D-031).

La demo (demo.md, unos 15 minutos): creación de personaje, pelea tutorial imposible de perder, calle y emboscada,
llamar al mecha y aprender el encendido, volver a casa en carro y dormir, flashback de los papás y primera pelea casera
que sí se puede perder. Casino y mercado negro se ven por fuera y están cerrados.

## Hitos de integración

| Hito | Qué se puede hacer cuando se cumple | Prueba de cierre | Tony | Alan |
| --- | --- | --- | --- | --- |
| H1 | Una pelea completa en el clandestino | 3 peleas seguidas con revancha en Clandestino.unity, sin errores | T-F2 | A-F1 (arena) |
| H2 | Caminar por el barrio y entrar y salir de locales sin carga | De casa al clandestino a pie, entrar y salir del bar | T-F3 | A-F1 |
| H3 | Carro conducible | Ir de casa al clandestino dando al menos una vuelta en carro | T-F3 | A-F2 |
| H4 | Llamar al mecha y la emboscada | Emboscada jugable de principio a fin, sin poder perder | T-F4 | A-F3 |
| H5 | Daño persistente, reparaciones y taller | Salir dañado de una pelea y repararlo en el taller con dinero | T-F5 | A-F3 |
| H6 | La demo completa | Una persona nueva termina la demo en unos 15 minutos sin ayuda | T-F6 | A-F4 |
| H7 | Contenido posterior a la demo | Ligas, casino, mercado negro, persecuciones | T-F7 | A-F5 |

## Tony

### T-F1. Pelea greybox del jugador (hecha, aprobada en D-025)
- Meta: mecha pesado, cabina, vida y golpe y bloqueo contra un saco y un atacante de prueba.
- Tareas: T-001, T-002, T-010, T-011, T-012, T-013.
- Prueba de cierre: pasada (EditMode 51/51, PlayMode 64/64 y la prueba de Tony en Unity).

### T-F2. Contratos de Core y pelea completa (siguiente)
- Meta: todo lo que Alan necesita para enchufarse está en Core/, y hay una pelea completa con rival, conteo y revancha.
- Tareas: T-030 (carpetas y asmdefs), T-031 (contratos de arena y jugador), T-032 (reloj, lugares y puertas),
  T-033 (cartera y ofertas), T-034 (calor y objetivos), T-021 (rival básico), T-022 (flujo de pelea).
  Orden: T-030, T-031, T-032, T-021, T-022, T-033, T-034 (la pelea antes que cartera y calor: H1 la necesita y Alan no usa cartera ni calor hasta A-F5).
- Prueba de cierre: en Pruebas_Tony.unity, 3 peleas seguidas con revancha ganando y perdiendo sin errores; en
  Clandestino.unity (con la arena de Alan) lo mismo (H1).
- Depende de Alan: la jaula de A-F1 (T-020) para cerrar H1 en Clandestino.unity. Hasta entonces prueba en Pruebas_Tony.unity.
- Alan espera de Tony: T-030 y T-031 primero, T-032 después, T-022 para H1, T-033 y T-034 al final. Las tareas de Core se integran a main apenas se verifican.

### T-F3. Jugador a pie y HUD
- Meta: caminar por el barrio de Alan con un HUD mínimo, y pasar a pie, a carro y a mecha por un solo contrato.
- Contenido: controlador a pie (primera persona), HUD a pie (vida, objetivo, canción), combate a pie básico (golpes simples y
  pistola), prefab del jugador a pie, lógica de entrar y salir de lugares y de subir al carro (lado jugador), sistema de
  objetivos que guía de vuelta durante el prólogo.
- Prueba de cierre: PlayMode con entrada simulada que recorre un tramo de Pruebas_Alan.unity a pie, entra por una puerta
  y sigue un objetivo; Tony en Ciudad.unity camina y ve el HUD (H2).
- Depende de Alan: puertas y calle de A-F1.

### T-F4. Mecha en la calle y emboscada
- Meta: llamar al mecha desde el taller, encenderlo en 5 segundos y pelear la emboscada sin poder perder.
- Contenido: llamado del mecha y zonas permitidas, encendido de 3 pasos (~5 s), calor por usar el mecha en la calle,
  rivales de la emboscada (pandilla), rival del tutorial que se debilita para que siempre ganes, presentador que dice el alias.
- Prueba de cierre: emboscada jugable de principio a fin; con vida baja los rivales no te matan (H4).
- Depende de Alan: callejón de la emboscada y punto de llamado (A-F3), carro (A-F2) para llegar.

### T-F5. Daño persistente, reparaciones y guardado
- Meta: el daño se queda entre peleas y se repara con dinero en el taller; hay renta mínima y partida guardada.
- Contenido: daño persistente, efectos de vida baja (cabina roja, cristal cuarteado, más lento), cartera, costo de
  reparación, premios de pelea, renta cada 7 días y bloqueo de taller, guardado y carga.
- Prueba de cierre: pelea que deja el mecha dañado, siguiente pelea empieza dañada, reparación descuenta dinero,
  guardar y cargar conserva todo (H5).
- Depende de Alan: el taller de la casa con su punto de interacción (A-F3).

### T-F6. Creación de personaje, prólogo y cierre de la demo
- Meta: la demo de 15 minutos de principio a fin.
- Contenido: pantalla de creación (nombre, alias, apariencia), director del prólogo (secuencia y objetivos), flashback
  (lógica y datos), primera pelea casera que sí se puede perder, pantalla de "continuará".
- Prueba de cierre: PlayMode que recorre el prólogo completo con entrada simulada y una prueba humana de 15 minutos (H6).
- Depende de Alan: Ciudad.unity completa, casa, Flashback.unity (A-F3 y A-F4).

### T-F7. Después de la demo
- Meta: el juego completo según docs/diseno/.
- Contenido, en este orden: economía completa (premios, apuestas en peleas, venta de piezas), ligas (4 de 5 rivales, jefes),
  piezas por ramas y niveles, habilidades con cooldown y munición, implantes (4 espacios), misiones y reputación (barrio y
  corporaciones), radio con 3 estaciones, adaptación del rival, y hackeo si sobra tiempo.
- Se planifica por bloques, en ese orden, y cada bloque se escribe al terminar el anterior (D-tony-75). Bloque 1, economía completa
  (apuestas, venta de piezas, kits, reparar a medias, precios): T-140 a T-151, empezando por la estabilización de pruebas (T-140).
- Prueba de cierre: vencer a los campeones de las 4 ligas; reputación visible en la ciudad (H7).
- Depende de Alan: casino, mercado negro y clínica abiertos, persecuciones, ciclo completo (A-F5).

## Alan

### A-F1. Arena y barrio greybox (primera fase de Alan)
- Meta: la jaula del clandestino y un barrio greybox donde caminar, con día y noche y locales que abren y cierran.
- Tareas (ver docs/tareas/pendientes): T-020 (arena), T-040 (barrio greybox de unas 6 manzanas), T-041 (reloj y ciclo de
  día y noche), T-042 (locales con puertas y horarios), T-043 (bar interior sin carga) y T-044 (pelea en Clandestino.unity, cierra H1 cuando T-022 de Tony esté en main).
- Prueba de cierre: con una cámara libre en Pruebas_Alan.unity, de casa al clandestino hay al menos una vuelta, el
  clandestino está abierto de noche y cerrado de día, y entro y salgo del bar sin pantalla de carga (H2 del lado del mundo);
  la arena publica sus puntos por el contrato de Core (H1).
- Depende de Tony: T-030 y T-031 (arena, aparición), T-032 (reloj, lugares y puertas). Mientras no estén en main,
  hace greybox y lo que no los necesita.

### A-F2. Carro
- Meta: un carro conducible por todo el barrio.
- Contenido: carro viejo (manejo, cámara, entrar y salir), tráfico mínimo de fachada, retenes que cierran las calles.
- Prueba de cierre: de casa al clandestino en carro dando al menos una vuelta, sin atravesar nada (H3).
- Depende de Tony: contrato de modo del jugador (T-031) y el lado del jugador al subir (T-F3).

### A-F3. Interiores, casa y callejón de la emboscada
- Meta: lo que necesita el prólogo, en tiempo real y sin cargas.
- Contenido: casa con taller y cama (dormir), espejo, clandestino por dentro con la arena, bar completo, callejón de la
  emboscada y punto de llamado del mecha, Flashback.unity.
- Prueba de cierre: recorrido del prólogo de casa a la emboscada y de vuelta a dormir (H4 y H5).
- Depende de Tony: llamado del mecha y emboscada (T-F4), taller y reparaciones (T-F5).

### A-F4. Cierre de la demo
- Meta: el barrio se ve y suena como la imagen de referencia, y la demo no tiene huecos.
- Contenido: casino y mercado negro por fuera y cerrados, grafitis, neón y niebla, lluvia, audio de ciudad, pulido de
  calles y finales creíbles (retén, obra, autopista), iluminación final.
- Prueba de cierre: una persona nueva termina la demo y la ciudad se parece a la referencia (H6).
- Depende de Tony: director del prólogo (T-F6).

### A-F5. Después de la demo
- Meta: el resto de la ciudad.
- Contenido, en este orden: casino (tragamonedas, fichas, cajera), mercado negro y clínica, peatones, persecuciones de
  drones y patrullas, personalización de carros, ciclo completo con todos los horarios, arte con assets de Tripo.
- Prueba de cierre: con calor me persiguen drones o patrullas y al llegar a casa dejan de hacerlo; el casino y el mercado
  negro funcionan con la cartera de Tony (H7).
- Depende de Tony: cartera y ofertas (T-033), calor (T-034), economía, implantes y misiones (T-F7).

## Reglas de coordinación
- Los contratos de Core se escriben antes de usarse (D-027). Si Alan necesita uno nuevo, lo escribe en docs/revision-alan.md.
- Un hito se da por cumplido solo con la prueba de cierre corrida (D-023).
- Esta hoja se actualiza en cada aprobación; las tareas de una fase se escriben cuando esa fase empieza (por el planificador).
