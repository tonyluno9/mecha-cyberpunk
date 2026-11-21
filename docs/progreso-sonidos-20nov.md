# Progreso sonidos 20-nov (integrar "3d sonidos" completo)

Pedido: el /goal de sonidos del 20-nov (Tony). Base: integración del 19-nov (docs/reporte-sonidos-19nov.md, 138 WAV).
Fuente: `3d sonidos/` en la carpeta principal (ignorada por git; 229 sonidos útiles + 00_*). Leer LEEME_SONIDOS.md (decisiones del 20-nov).

## Dónde está todo
- Carpeta principal: en main; **el chat de Blender trabaja ahí** (cambia FBX de PJ01 en Assets): no hacer checkout/restore ahí.
- Integración y Unity: `.sesiones/s2`, rama `integ/sonidos-20nov` (desde main 8f97866). Una sola instancia de Unity.
- Subagentes: worktrees dispersos en `Desktop/videojuego/wt21/` (sin imageassets ni binarios LFS); leen `3d sonidos/` de la carpeta principal.
- Al final: merge --no-ff a main, push, builds Demo y Juego en la carpeta principal, reporte docs/reporte-sonidos-20nov.md.

## Plan
| Tarea | Qué | Estado |
| --- | --- | --- |
| T-1530 | Comparar carpeta vs Assets/Arte/Audio, copiar/recortar/convertir, carpetas nuevas 11..17 y 00_*, importador, ids-20nov.csv, sonidos-faltan.md | pendiente |
| T-1531 | Catálogo + MapaSonidos + ReglaMezclaSonidos para todos los IDs nuevos (con pruebas) | después de T-1530 |
| T-1532 | Armas, impactos, explosiones, plasma W07, rueda con cámara lenta real | después de T-1531 |
| T-1533 | Nova, robots, drones, NPCs, policía | después de T-1531 |
| T-1534 | Vehículos y pasos por evento de animación según BITACORA | después de T-1531 |
| T-1535 | Ambiente por barrio con transición, clima, interiores, objetos, casino, misiones/HUD, EXO/implantes, radio | después de T-1531 |
| Final | Pruebas por clase, merge, builds, reporte | — |

## Bitácora
- 20-nov tarde: paso 0 (sin Unity abierto, sin lockfile; main 8f97866). integ/sonidos-20nov creada en s2.
- T-1530 integrada (b6a9569): 122 WAV nuevos, 5 renombrados, 0 reemplazos. Importador viejo ImportadorAudio pisaba TODAS las carpetas numeradas (todo CompressedInMemory y mono, también lo del 19-nov): ya no; reimportado 01..18. T-1531 (catálogo y mezcla) corriendo.
- T-1531 integrada (a85697f): catálogo 01..18 creado en Unity (5954b9f), alias de renombrados, TablaMezcla20nov (96 IDs). EditMode de sonido 251/251 (IdsSonidoPruebas al día).
- T-1532..T-1535 lanzadas en paralelo (wt21/). Reglas: no tocan tablas de mezcla/catálogo/mapa (me piden lo que falte) ni los .meta de clips de PJ01 (el chat de Blender los re-exporta en la carpeta principal).
- T-1532..T-1535 integradas (cad696e, bbb804f, a968667, d494460). EditMode de sonido/armas/vehículos/pasos 384/384 antes de T-1533/35.
- Hecho por mí (622f165): T-1536 Core ClimaMundo (la lluvia de Mundo llega al sonido y al carro; hoy la lluvia está apagada en el juego), alias ROB_PES_PASO/ROB_APAGADO/ROB_CAIDA con tono bajo, ROB_VOZ_ALERTA en 5 frases, fuera UI_CAMARA_LENTA sintetizado, catálogo regenerado. EditMode completo corriendo.
- Pruebas: EditMode de sonido 837/839 (2 de main); PlayMode Sonido 19/19 y juego 33/33 por clase. En Play: 241 fuentes de interiores/objetos, ambiente por barrio correcto (AMB_CALLE_DIA y AMB_TIANGUIS apagados en el catálogo desde main). Reporte escrito. Siguiente: merge a main, push, builds.
- Builds en la carpeta principal desde main 6073d8f, uno por uno: Builds/Demo/CyberCity.exe (Success, 21-nov 01:01) y Builds/Juego/CyberCity.exe (Success, 01:02). Incluyen los FBX que el chat de Blender tenía modificados sin guardar en la carpeta principal (PJ01, E16, E18, drones, PA01/PA02). Unity cerrado. TERMINADO.
