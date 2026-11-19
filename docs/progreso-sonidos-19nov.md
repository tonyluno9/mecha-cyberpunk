# Progreso sonidos 19-nov: integrar "3d sonidos"

Dirige Opus en `.sesiones/s2`, rama de integración `integ/sonidos-19nov`. Tareas hechas por subagentes Sonnet (brief: docs/brief-sonidos-19nov.md).
**Si se corta la sesión:** lee este archivo, `git branch -a | grep T-147` y sigue con "Qué sigue".

## Reglas
Una sola instancia de Unity (en s2, batch). Nada de PlayMode completo: pruebas por clase. No correr CreadorTodo. Nunca force push.
Merge --no-ff a main y push (autorizado por Tony). "3d sonidos" no va a git (.gitignore).

## Tareas
| Tarea | Qué | Estado |
| --- | --- | --- |
| T-1470 | Copiar y recortar WAV a Assets/Arte/Audio/<cat>/, importador, .gitignore | hecha, integrada |
| T-1471 | Catálogo: carpetas nuevas, IDs viejos -> archivos nuevos, encendidos, política de mezcla | hecha, integrada |
| T-1472 | Armas (disparo, recarga, sacar), impactos por material, explosiones | hecha, integrada |
| T-1473 | Pasos por piso, jugador (aterrizar, rodar, trepar, golpes) | hecha, integrada |
| T-1474 | UI, HUD, tienda, interacción, consumibles | hecha, integrada |
| T-1475 | Vehículos (motor con pitch, acelerón, frenos, choques, puertas, alarma, motos, camiones) | hecha, integrada |
| T-1476 | Policía (sirenas, radio, aviso de Nova) | hecha, integrada |
| Final | Integrar, compilar, pruebas por clase, builds Demo y Juego, reporte | pendiente |

## Bitácora
- Paso 0: no había Unity abierto; s2 pasada a main (d119ebf, ya con la sesión A) y lockfile borrado. Rama integ/sonidos-19nov.
  Se quitaron los 4 worktrees viejos de T-1440..T-1443 (sus ramas siguen). integ/zonas-19nov-B (zonas, sesión B) sigue sin merge, pendiente de su verificación.

- Las 7 ramas integradas en integ/sonidos-19nov. Compila (arreglo: preloadAudioData va en SampleSettings). Corridos ImportarSonidos19nov.Aplicar,
  CreadorCatalogoSonidos.Crear (230 IDs), AplicarSonidosPoliciaT1476.Aplicar y AplicarSonidoNovaT1476.Aplicar. Commit 82ee51e.
- Pruebas: EditMode de 27 clases de sonido 235/235 (tras 2 arreglos: prefijo "ARM_BALA_" partido y prueba de pasos con PAS_);
  PlayMode Tests.Sonido 19/19; PasosT596, DisparoLimpioT676 (acepta impacto de cabeza), ExplosionCarro, ChispeoPlasmaT1309 13/13.
  RecorridoRonda2Pruebas.MercadoTianguisEscopeta falla "El ambiente arranca apagado" (0.8 en vez de 0): código no tocado por estas tareas, falla vieja (piso de ambiente de T-977).

- Build de la Demo detenido por Claude Code por falta de memoria; no se relanzó. Reporte: docs/reporte-sonidos-19nov.md. Merge a main.

## Qué sigue
1. Build Builds/Demo (ConstruirDemo.Construir) y Builds/Juego (ConstruirDemo.ConstruirJuego), una a la vez.
2. (hecho) reporte, faltantes, merge a main.
