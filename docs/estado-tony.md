# Estado de Tony (modo automático)

Fase 1, rama de integración: tony/fase-1. El merge a main fue denegado por el clasificador; tony/fase-1 hace de base.

| Tarea | Estado |
| --- | --- |
| T-001 | hecha (verificada; contenida en tony/fase-1; sin merge a main) |
| T-002 | bloqueada (implementada en tony/T-002-verificacion-automatica, sin verificar) |
| T-010 | bloqueada (depende de T-002) |
| T-011 | bloqueada (depende de T-002) |
| T-013 | bloqueada (depende de T-002) |
| T-012 | bloqueada (depende de T-002) |

Qué sigue: Tony desbloquea permisos (ver docs/revision-tony.md); después se verifica T-002 y siguen T-010, T-011, T-013, T-012.

## Tarea a medias
T-002 (verificación automática): implementada y pusheada en tony/T-002-verificacion-automatica, sin verificar.
Qué sigue: verificarla (herramientas/verificar.sh todo y unity-vivo.sh), integrarla a tony/fase-1 y seguir con T-010, T-011, T-013, T-012.
Pendiente de permisos: 3 procesos Unity.exe AssetImportWorker huérfanos (PIDs 32504, 44408, 44440) siguen corriendo; el clasificador negó matarlos.
