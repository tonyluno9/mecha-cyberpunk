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

## Qué sigue
- Al volver cada subagente: merge de su rama en `integ/zonas-19nov-B` (en s2), correr Aplicar en orden T1440 → T1441 → T1442 → T1443,
  luego Validar y, en otra corrida, Fotos. Revisar las fotos y devolver errores.
