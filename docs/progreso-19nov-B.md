# Progreso 19-nov (sesión B): Cerro y Puerto iguales a K13, A13, K15 y A15

Dirige Opus en el worktree `.sesiones/s2` (rama main y ramas `tony/T-14xx-…`). Las tareas las hacen subagentes Sonnet, que
solo escriben scripts de editor; Unity corre aquí, en s2, una sola instancia en modo batch.
**Si se corta la sesión:** lee este archivo, `git log --oneline main -10` y `git branch | grep T-144` y sigue con "Qué sigue".

## Reglas
- Solo los prefabs `Assets/Arte/Mundo/Zonas/T1355_Cerro.prefab` y `T1357_Puerto.prefab` (y lo que está dentro de ellos).
  Luz global, postproceso, materiales compartidos, escena, Malecón y Autopista son de la sesión A: lo que haga falta
  de ahí va en docs/para-sesion-A-19nov.md.
- Nada de PlayMode completo ni builds; pruebas por clase. No correr CreadorTodo. Nunca force push. Merge --no-ff a main y push (autorizado por Tony).
- Capturas lado a lado en docs/capturas/_vista19b/ con el mismo ángulo que la referencia.
- Brief común de los subagentes: docs/brief-19nov-B.md.

## Tareas
| Tarea | Qué | Estado |
| --- | --- | --- |
| T-1440 | Cerro A13: ladera en terrazas (alturas, muros CC08, escaleras largas iluminadas, calle en zigzag con postes y luz naranja, ciudad brillante abajo) | en curso (subagente) |
| T-1441 | Cerro K13: detalle de calle y azoteas (casas variadas, tinacos, antenas, tendederos, lavaderos, aires, varillas, cables, neones chicos, carros, NPCs, basura) | en curso (subagente) |
| T-1442 | Puerto K15: calle de bodegas (charcos, contenedores con grafiti, montacargas, pallets, barriles, caseta con pluma, estibadores, neón, barco iluminado) | en curso (subagente) |
| T-1443 | Puerto A15: patio desde arriba (pilas de contenedores de colores, carriles marcados, postes de luz, grúas con balizas, barco atracado, bodegas) | en curso (subagente) |
| T-1444 | Bugs, parkour en azoteas y contenedores, rendimiento (50 fps) y fotos finales | pendiente |

## Bitácora
- 08:30 Paso 0: s2 estaba en tony/T-1370-cerro-puerto-paso1 (ya en main); se pasó a main y se hizo pull (ce7c4bd). No había
  Unity abierto (lockfile viejo en s2). La carpeta principal la usa la sesión A (rama integ/look-19nov-A): no se toca.
  Referencias copiadas a s2 (no están en git). Comparaciones de partida: docs/capturas/_vista19/.
- Diagnóstico de partida: el Cerro desde arriba es rejilla plana (el zigzag sube solo 10 m y las azoteas varían 0.6–2.2 m),
  la niebla tapa la ciudad; el Puerto desde la calle está vacío (sin charcos, pallets, montacargas, barco ni grúas visibles)
  y desde arriba son filas iguales de contenedores azules con postes.

- 08:40 Lanzadas T-1440 a T-1443 en paralelo (brief docs/brief-19nov-B.md). Nota a la sesión A en docs/para-sesion-A-19nov.md.
  s2 en main compila con 0 errores (3 min 44 s).

- Integración en `integ/zonas-19nov-B` (en s2). Faltaban K15 y A15 en las referencias de s2: copiadas.
- T-1442 ronda 1 corrida: compila, 0 fallas en Validar, pero la foto no se parece (avenida de tránsito vacía, barco y grúas a 140 m,
  NPCs blancos y en pose T, charcos casi invisibles). Devuelta para ronda 2: patio junto al muelle con cámara nueva.
- T-1443 ronda 1 corrida: 142 contenedores con color, carriles pintados, 70 charcos, balizas, ventanas del barco; 0 bodegas, grúas 1 m
  enterradas, charcos de luz cuadrados y cámara mirando al agua. Devuelta para ronda 2 (búsqueda automática de cámara).

- Ronda 1 completa aplicada (commit adb9820 en integ/zonas-19nov-B, Cerro 46 MB, Puerto 7 MB; fotos en docs/capturas/_vista19b/).
  - Cerro K13: ya hay cables, poste, carro, fachadas con detalle; falta escalera, gente, carros; un panel de "ciudad" sale como pared cerca de la cámara.
  - Cerro A13: sigue leyéndose como cajas blancas (azoteas claras), calles poco visibles, 0 escaleras de ladera, paneles de ciudad como torres colgando.
  - Puerto K15: patio nuevo junto al muelle con barriles, montacargas, estibadores con textura y pose; falta encuadre (bodegas, barco, grúa).
  - Puerto A15: encuadre ya parecido (grúas, carriles pintados, colores), pero pilas ralas de 1 alto, 0 bodegas, grúas 0.94 m enterradas.
  - Validar: T-1440 19 fallos, T-1441 96 (casi todo enterrado 5–8 cm) y 3700 objetos (tope 3000), T-1442 1, T-1443 3.
  - Los .fbx.meta que Unity reescribe al importar (ruido de flotantes) se descartan con git restore antes de cada commit.
- Ronda 2/3 lanzada a los 4 subagentes con las correcciones.

- 09:05 Ronda 2/3 aplicada: Cerro con techos oscuros, 29 postes con charco de luz, 2 escaleras de ladera, 7 carros, 14 personas,
  tendederos; Puerto K15 con patio de 3 bodegas, Puerto A15 con grúas asentadas. Validar: T1440 1, T1441 2, T1442 0, T1443 0 fallos.
  Fotos: A13 sigue en "torres de cajas" con peldaños blancos; K13 sin casas a la izquierda (espejo); K15 pasillo angosto y pata de
  grúa; A15 pilas bajas y sin bodegas. La sesión A hace niebla por altura en T-1462 (falta que la integre).
- Ronda siguiente mandada: casas reales (CC06/CC07) en los pisos extra, peldaños oscuros, cámara A13 con la calle en diagonal;
  K13 con detalle a la derecha; K15 patio de ≥ 14 m; A15 bodegas primero y pilas de 3–4.

- 09:40 Ronda 4 del Puerto empeoró (K15 con cajas pegadas al lente; A15 sin cámara, sin bodegas ni relleno). Las búsquedas automáticas
  de cámara a ciegas no funcionan: se hizo Assets/Arte/Editor/CamarasB19nov.cs (Probar: lee Logs/camaras-b.txt y renderiza
  candidatas) y se eligieron a ojo cámaras FIJAS: K15 (160, 1.7, -150)→(150, 12, -260) campo 55; A15 (320, 50, -150)→(230, 0, -230)
  campo 60; A13 (-450, 55, 100)→(-330, 5, 20) campo 60 (docs/capturas/_vista19b/prueba/k15e, a15c, a13b).
- Bug: los 150 paneles de "ciudad lejana" de T-1440 caían dentro de la ciudad y del Puerto; se quitan en su ronda 4.
- No cabe otro Unity (12 GB de disco y 2.5 GB de RAM libres; la sesión A tiene los suyos).

## Qué sigue
- Al volver cada subagente: merge de su rama en `integ/zonas-19nov-B` (en s2), correr Aplicar en orden T1440 → T1441 → T1442 → T1443,
  luego Validar y, en otra corrida, Fotos. Revisar las fotos y devolver errores.
