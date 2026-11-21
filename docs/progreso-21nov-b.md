# Progreso 21-nov B: animaciones y decisiones de la revisión de Blender

Pedido: /goal de Tony (21-nov B). Fuentes: docs/tareas-unity-{locomocion,alturas,robots-nova,vehiculos}-20nov.md (sección "Falta (Unity)"),
imageassets/blender/_REVISION_TONY/ANIMACIONES_FALTAN.md, REVISION.md y la BITÁCORA.
Regla: nada hecho sin verlo en Play, captura en docs/capturas/_21nov-b/. No editar clips ni FBX de Blender; no tocar PROMPTS_ASSETS.md ni registro.csv.

## Dónde está todo
- Integración y Unity: `.sesiones/s2`, rama `integ/21nov-b` (desde main 82f0bac). Una sola instancia de Unity.
- Subagentes: worktrees dispersos en `Desktop/videojuego/wt23/` (sin imageassets ni LFS); leen imageassets en la carpeta principal.
- Base: T-1540 (21-nov) ya conectó salto corriendo/rodada, multiplicadores RapidezSaltoInicio/Aterrizaje, 6 borradores PJ01, daños PJ01 en 7 papeles no jugador, Skull_Burla, E15/E16_Muerte.

## Tareas
| Tarea | Qué | Estado |
| --- | --- | --- |
| T-1550 | Locomoción y salto PJ01 (CorrerRapido, velocidades, mezcla, retraso 3 frames, aterrizajes, pisadas) | aplicada (falta Play) |
| T-1551 | Combate y eventos: clips aprobados sin conectar (daños del jugador, recargas, granada, interacciones, implantes, NPC, pandilleros, Nova caída) | aplicada (falta Play) |
| T-1552 | Alturas: tabla de factores, EscalasNova/EscalasHumanos, Tino, cápsulas y NavMesh E14/E16 | aplicada + Afinar (falta Play) |
| T-1553 | Robots Nova: avatar E18, clips E18, jetpack y puños, E16 escudo y cañón, E15 W10, mover clips a su carpeta | lanzada |
| T-1554 | Vehículos: bisagra cofre PA01/PA02/V22, V16–V19, hélices, carros viejos → Carro_Vxx, quitar V10/V11 | aplicada (falta Play) |
| Final | Capturas en Play, pruebas por clase, builds, reporte-21nov-b.md | pendiente |

## Bitácora
- Paso 0: Unity cerrado, sin lockfile, main 82f0bac (lo de Blender ya estaba en main; solo cambiaron docs).
- Lanzadas las 5 tareas (T-1550..T-1554) en wt23/.
- T-1550 aplicada (0 problemas). Escala de CorrerRapido 1.88 como Trote (el doc decía 1.8367). ImportadorClipsPJ01: CorrerRapido con bucle, Salto_Aire sin bucle.
- T-1551 aplicada: 16 clips en overrides, 2 estados del policía.
- T-1552: la columna "hoy" de Blender no coincide con Unity (NPCs ya medían ~1.84); el factor los dejó en 2.0–2.2 m. Se corrió Afinar: 40 a ±5 cm de la altura nueva.
- T-1554: arreglo de compilación (ComponentUtility). Rotores: el eje del disco era Z local y GiroRotores giraba en Y (el temblor); ahora giran sobre el eje más delgado de su malla.
- Sigue: T-1553 (robots Nova) corriendo; luego Play con capturas y pruebas por clase.
- EditMode de lo tocado: 697 pruebas, 48 fallos, casi todos esperados (pruebas que exigían lo viejo). Mandados a corregir a cada subagente: alturas (prueba del factor tras Afinar, woman2 por idle, Nova fuera de la prueba de idle de PJ01), locomoción (Salto_Aire sin bucle, umbral 1.62, 121 clips, claves nuevas sin clip en overrides, T739 3.155), vehículos (V10 aún en la escena, prueba del eje del rotor), combate (T1004 debe permitir Skull_Burla).
