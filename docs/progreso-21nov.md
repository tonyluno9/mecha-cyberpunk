# Progreso 21-nov (noche): mapa final, animaciones, modelos y NPCs

Pedido: el /goal del 21-nov (Tony). Regla: nada hecho sin verlo en el juego desde el jugador, con captura en `docs/capturas/_21nov/`,
lado a lado con su referencia cuando la haya (`imageassets/referencias/look/mapa-final/`: K20–K34, A20–A24, M13; y K01–K10).
Si algo no avanza en 15 min: se mata, se anota y se sigue.

## Dónde está todo
- Carpeta principal: en main (eb42576). El chat de Blender puede volver a trabajar ahí: no hacer checkout/restore ahí sin revisar.
- Integración y Unity: `.sesiones/s2`, rama `integ/21nov`. Una sola instancia de Unity.
- Subagentes: worktrees dispersos en `Desktop/videojuego/wt22/` (sin imageassets ni binarios LFS); leen imageassets solo-lectura en la carpeta principal.
- Al final: merge --no-ff a main, push, builds Demo y Juego en la carpeta principal, `docs/reporte-21nov.md`, cerrar Unity.

## Paso 0 (hecho)
- Cambios del chat de Blender en `tony/blender-21nov` → main eb42576 (subido). Fuera (en disco): zip de audio, .blend1, __pycache__ y 4 .blend
  de `_REVISION_TONY` de más de 100 MB (revision_PJ01_animaciones, revision_personajes, revision_vehiculos, revision_vehiculos_arreglados).
- Unity cerrado; sin lockfile.

## Hallazgos de inicio
- FC01–FC03 (fachadas de Clandestino, Bar, Mercado Negro) NO existen en ningún lado: no se pueden poner; pedir al chat de arte.
- nuevos-21nov: W11 granada, EM01 portón del mercado, EM02 puesto de entrada (zips de Tripo) + K19 (referencia). H11 holograma de la granada (PNG).
- Animaciones: ANIMACIONES_FALTAN.md separa aprobadas / 6 borradores nuevos / pendientes de Tony (esos no se tocan).

## Tareas
| Tarea | Qué | Estado |
| --- | --- | --- |
| T-1540 | Animaciones a los controladores | hecha (1 prueba PlayMode pendiente) |
| T-1541 | Modelos nuevos (W11+H11, EM01/EM02 entrada del mercado, assets-faltan) | hecha |
| T-1542 | NPCs por barrio | hecha |
| T-1543 | Muro K32–K34 + nada detrás del muro + sin huecos | hecha en parte |
| T-1544 | 143 casas caja → modelos reales | hecha (134) |
| T-1545 | Luz (sodio, neón, piso mojado) y 50 fps | hecha (13 puntos >= 50 fps) |
| Final | Capturas lado a lado, builds, reporte | pendiente |

## Bitácora
- 21-nov: paso 0 hecho; integ/21nov en s2; compilando lo nuevo de Blender.
- Aérea actual (docs/capturas/_21nov/00-actual-aerea.png, render en modo edición sin niebla): la ciudad es una cruz (centro 3x2 dentro del muro + 4 alas de afueras fuera del muro + piso gris enorme). M13 es un rectángulo denso de 4 barrios con glorieta. En Play hay niebla y algo que oculta lo lejano (para capturas aéreas usar `scratchpad/aerea.sh`, render en modo edición).
- Lanzadas: T-1540 animaciones, T-1541 modelos, T-1542 NPCs, T-1543 muro/nada detrás/huecos, T-1544 casas caja. T-1545 (luz y fps) espera su worktree.
- Barrios reales medidos en Play (ITerritorio.ZonaEn, rejilla 20 m): Suburbios x<-40; Mercado -40..40; Neón Alto x>40 z>-10; Nova x>40 z<-10. M13 los quiere distinto (Nova arriba-izq, Neón arriba-der, Suburbios abajo-izq, Mercado abajo-der, glorieta al centro): mover barrios enteros = rehacer el mapa (misiones, apariciones, lugares, tráfico). NO se hace hoy: queda en el reporte como lo principal que no quedó, con propuesta.
- T-1541 aplicada (2c5a0ea). T-1542..T-1545 integradas; T-1543 corregida a los barrios reales. Lote "mapa1" corriendo (afuera del muro, casas, luz, NPCs).
- T-1540 (animaciones) sigue corriendo.
- Lote mapa1 aplicado y T-1540 aplicada. Fondos T513 apagados; 5 lotes de relleno que tapaban cruces, callejón y recinto apagados; 6 líneas de afuera apagadas.
- Capturas K20–K34 lado a lado (de noche); fps 13/13 >= 50. Reporte en docs/reporte-21nov.md. Sigue: merge a main y builds.
