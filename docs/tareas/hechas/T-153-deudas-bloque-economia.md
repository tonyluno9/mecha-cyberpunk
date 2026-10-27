# T-153 Deudas del bloque de economía: creador del kit idempotente y orden frágil en Progresion
Dueño: Tony | Rama: tony/T-153-deudas-bloque-economia | Modelo: Sonnet 5.5, medium
Depende de: nada

## Objetivo
Antes de sumar las peleas de liga (que agregan pruebas largas a Progresion y vuelven a correr creadores sobre `MechaJugador.prefab`), se quitan
las dos deudas que dejó el bloque 1: correr `CreadorKitReparacion.Crear` ya no ensucia `MechaJugador.prefab`, y `CambioEscenaPruebas` y
`PrologoPresentePruebas` dejan de fallar a veces en la primera corrida del área Progresion.

## Contexto
- docs/estado-tony.md (parada manual del 2026-10-17, "Deudas") y docs/revision-tony.md (T-149, T-150): "CreadorKitReparacion.Crear modifica
  MechaJugador.prefab cada vez que corre"; "en la primera corrida fallaron CambioEscenaPruebas y PrologoPresentePruebas; pasaron al repetir
  sin cambiar su código: fragilidad de orden por vigilar".
- T-140 (D-tony-77) encontró la causa de un cuelgue en un teardown que destruía todas las raíces de la escena, y sugirió aplicar el patrón de
  anotar raíces a otras pruebas (PeleaCasera, CamaEspejo) como defensa. Helper: `Progresion/Pruebas/MapasDeAccionesDePrueba`.
- Patrón de creadores idempotentes: los creadores de T-150 ya no pisan valores existentes; `CreadorFlujoPeleaCasera` (si la propiedad ya
  está puesta, no la toca).
- Prohibido ignorar, marcar `[Explicit]` o reordenar pruebas a mano para "arreglar" el orden: hay que encontrar el estado que se filtra.

## Qué hacer
1. `Mecha/Editor/CreadorKitReparacion.cs`: que una segunda corrida no cambie nada. Compara antes de asignar y guarda el prefab solo si algo
   cambió (o usa el patrón de `CreadorFlujoPeleaCasera`). Comprueba también que `MechaEstacionado.prefab` no cambia.
2. Diagnostica la fragilidad de `CambioEscenaPruebas` y `PrologoPresentePruebas`: corre el área Progresion completa (con el límite de 2400 s si
   T-152 ya está; si no, `LIMITE_AREA=2400`) y, si hace falta, solo esas dos clases detrás de las que corren antes en el orden por defecto.
   Busca estado que sobrevive entre fixtures (estáticos sin limpiar, `ServicioGuardado.RutaForzada`, `CambioEscena` con transición pendiente,
   escenas cargadas que no se descargan, objetos `DontDestroyOnLoad`, `Registro<T>` con restos). Arregla la causa en la prueba o en el código
   (si es del código del juego, que sea dentro de las carpetas de abajo) y anota la evidencia en la bitácora.
3. Aplica el patrón de "anotar las raíces al empezar y destruir solo las nuevas" a los `[UnityTearDown]` de Tests/PlayMode/Progresion/ que
   todavía destruyan todas las raíces de la escena activa.

## Carpetas permitidas
Assets/_Proyecto/Mecha/Editor/, Assets/_Proyecto/Tests/PlayMode/Progresion/, Assets/_Proyecto/Progresion/Escenas/,
Assets/_Proyecto/Progresion/Guardado/, Assets/_Proyecto/Progresion/Pruebas/
No tocar: Core/, herramientas/, prefabs (salvo para comprobar que no cambian), escenas, todo lo de Alan.

## Criterios de aceptación (los comprueba el verificador)
- [ ] Correr `CreadorKitReparacion.Crear` dos veces seguidas (batch con `-executeMethod`) deja `git status` limpio en Mecha/Prefabs/ (md5 de
      `MechaJugador.prefab` y `MechaEstacionado.prefab` igual antes y después de la segunda corrida)
- [ ] El área Progresion de PlayMode pasa completa 2 corridas seguidas en una máquina recién abierta (sin correr antes otra cosa), con
      `CambioEscenaPruebas` y `PrologoPresentePruebas` en verde las dos veces; la bitácora explica la causa encontrada
- [ ] Grep: ningún `[UnityTearDown]` de Tests/PlayMode/Progresion/ destruye todas las raíces de la escena activa sin anotarlas antes
- [ ] EditMode completo y PlayMode de Mecha siguen en verde

## Criterios de sensación (para un humano)
- Ninguno: no cambia nada jugable.

## Pruebas automáticas
- PlayMode: el área Progresion completa dos veces. EditMode: la suite completa.

## Fuera de alcance
- Las pruebas de captura que se saltan sin su variable (son a propósito). Los 9 fallos de Alan en `JaulaClandestinoPruebas`.
