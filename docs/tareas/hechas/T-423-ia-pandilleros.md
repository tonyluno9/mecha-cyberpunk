# T-423 IA de pandilleros más lista
Dueño: Tony | Rama: tony/T-423-ia-pandilleros | Modelo: Sonnet 5.5 | MODO RÁPIDO (docs/plan-noche.md, paso 5; mejora 3 de docs/mejoras-pendientes.md)
Depende de: T-421. Tony autorizó tocar todas las áreas y Core.

## Objetivo
Los pandilleros armados rodean al jugador, se cubren entre ráfagas y se asoman a disparar, huyen con poca vida y piden refuerzos. Siguen avisando con el láser rojo.

## Qué hacer
1. Regla pura `ReglaTacticaPandillero` (EditMode) y parámetros en `DatosPandilleroAPie` (Táctica T-423).
2. `PandilleroAPie`: rodeo en arco, búsqueda de cubierta, huida, grito. `PandillaAPie.Avisar`/`Repartir`.
3. Pruebas: EditMode ReglaTacticaPandilleroPruebas, PlayMode TacticaPandillerosPruebas.

## Carpetas permitidas
IA/, Tests/, docs de Tony.

## Bitácora
- Hecha: EditMode 18/18 (con ReglaPandilleroAPiePruebas), PlayMode TacticaPandillerosPruebas 2/2 + PandillaArmadaPruebas + AliadosPruebas + HistoriaDemoPruebas pasan; captura docs/capturas/T-423-1.png. Sin costo notable por cuadro (búsqueda cada 1.5 s), FPS no medido.
