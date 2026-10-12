# Revisión de Tony (turno de día, detenido)

## Detenido: faltan permisos
- El merge de tony/T-001-contratos-core a main (paso 1.e, D-020) fue denegado por el clasificador del modo automático. main no se tocó.
  tony/fase-1 ya contiene T-001 y se usa como base.
- La verificación de T-002 quedó incompleta: el clasificador denegó `herramientas/verificar.sh todo` y matar 3 procesos
  AssetImportWorker huérfanos (PIDs 32504, 44408, 44440, iniciados 19:41, padre 44312 ya no existe). Vienen del editor vivo
  de la verificación de T-001, que se cerró con taskkill sin sus hijos.

## Tareas
| Tarea | Estado | Evidencia |
| --- | --- | --- |
| T-001 | verificada, 5/5 criterios, EditMode 18/18 | En tony/fase-1 desde antes; sin merge a main |
| T-002 | implementada, NO verificada | Rama tony/T-002-verificacion-automatica (pusheada). El implementador reporta verificar.sh todo OK (editmode 18/18, playmode 3/3) y captura real, sin verificación independiente |
| T-010, T-011, T-013, T-012 | pendientes | Dependen de T-002 (herramientas de verificación) |

## Para Tony
1. Cierra o mata los 3 AssetImportWorker huérfanos, o autoriza matarlos.
2. Autoriza correr `herramientas/verificar.sh` y `unity-vivo.sh`, y los merges/push a main que pide el skill.
3. T-002 toca `.gitattributes` (`*.sh text eol=lf`), fuera de las carpetas permitidas; decide si se acepta.
