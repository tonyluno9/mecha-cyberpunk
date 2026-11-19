# Bugs del mapa nuevo (18-nov, sesión C)

Encargo: barrer y arreglar los bugs del mapa expandido (docs/reporte-18nov-A.md, parte 2; coordenadas en docs/plan-zonas-18nov.md).

## Dueños (no se cruzan)
- **Sesión A (esta)**: la escena Assets/_Proyecto/Escenas/Ciudad_Prologo.unity, el Malecón (T1354, prefab Assets/Arte/Mundo/Zonas/T1354_Malecon.prefab),
  la Autopista (T1356, prefab T1356_Autopista.prefab), las orillas (T1358) y las uniones entre zonas y con los barrios viejos.
- **Sesión B**: SOLO los prefabs del Cerro (T1355_Cerro.prefab) y del Puerto (T1357_Puerto.prefab). Aquí NO se corren
  ColocarT1355Cerro.Aplicar ni ColocarT1357Puerto.Aplicar, ni nada que modifique esos prefabs o sus instancias en la escena.
  Sus bugs se anotan en docs/bugs-mapa-18nov.md (sección "Para la sesión B") y no se arreglan aquí.
- La luz de los barrios viejos no se toca. Lo de "Le gustó" de docs/playtest-17nov-b.md no se toca.

## Reglas
- Escena < 100 MB: lo de zonas va en sus prefabs (CorrerZonasT1350.GuardarComoPrefabs, que solo guarda Malecón, Autopista y Orillas).
- FlotantesS2t.Arreglar y RevisarCiudadT995.Arreglar son de toda la escena: NO se corren tal cual (tocarían el Cerro y el Puerto).
  Para arreglar flotantes/encimados se filtra por raíz (solo T1354, T1356, T1358 y objetos sueltos de la escena en las uniones).
- Nada con primitivos que sustituya modelos; lo que falte, a docs/assets-faltan.md.
- Cada arreglo con captura de antes y después en docs/capturas/bugs-18nov/.

## Tareas
| Tarea | Qué |
| --- | --- |
| T-1360 | Barrido automático (editor): flotantes, enterrados, encimados, z-fighting, huecos en piso, muros invisibles, colliders, texturas, calles con escalón, vacíos, islas sin salida, luces |
| T-1361 | Recorrido en Play (por partes) a pie y en carro por Malecón, Autopista, túnel y uniones, con capturas |
| T-1362 | Autopista como K18 (elevada, rampa, curva, túnel, sin casas encima, tráfico arriba) |
| T-1363 | Malecón como K11/K12 (fachadas del kit, skyline al otro lado del agua, luz sodio + neón local) |
| T-1364 | ZonaTrafico_* y plazas (PZ/Plazas con adoquín) |
| T-1365 | Uniones y límites (los arregla Opus con lo que salga de T-1360/T-1361) |
| T-1366 | Medición de FPS por zona (en el build) y optimización |
