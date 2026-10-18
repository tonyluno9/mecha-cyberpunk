# T-164 Deudas antes de las piezas: el guardado no pierde lo que la escena no tiene y CambioEscenaPruebas por orden
Dueño: Tony | Rama: tony/T-164-deudas-guardado | Modelo: Sonnet 5.5, medium
Depende de: nada

## Objetivo
Antes de guardar las piezas del mecha (T-167), el guardado deja de borrar lo que la escena actual no tiene: si se guarda en una escena
sin mecha (o sin ligas, o sin inventario), lo que se había leído de esas claves se vuelve a escribir igual. Y se quita de raíz el fallo
intermitente de `CambioEscenaPruebas.Ir_ConAliasYEtapaFijados_LaEscenaNuevaCargaLaPartida`, que lleva cinco bloques fallando en la
primera corrida de Progresion.

## Contexto
- Decisiones: D-tony-50 (guardado por piezas, una ranura), D-tony-61 (partida entre escenas por el guardado), D-tony-101 y D-tony-105
  (T-153 cerró la fragilidad "por defensa", sin causa demostrada). Propuesta nueva: D-tony-123.
- Código: `Progresion/Guardado/ServicioGuardado.cs`: `Cargar` llena `pendientes` y entrega a quien se registre después; `Guardar` solo
  escribe los `IGuardable` registrados, así que una clave leída cuyo dueño no está en la escena se pierde al guardar.
  `Progresion/Escenas/CambioEscena.cs` tiene estado estático (`transicionPendiente`, `interceptor`).
- Pistas de las bitácoras: T-126 ("la transición ya se consumió": depende del orden, estado estático de `CambioEscena`), T-133, T-149 y
  T-160 (cargó Pruebas_Prologo en vez de Pruebas_APie; pasa sola).
- Prohibido ignorar, reordenar a mano o marcar pruebas como `[Ignore]`/`[Explicit]`; si la causa está en otra prueba que deja estado
  sucio, se arregla esa prueba.

## Qué hacer
1. `ServicioGuardado.Guardar`: además de los registrados, escribe tal cual las piezas de `pendientes` cuya clave ningún registrado tiene
   (se leyeron y su dueño todavía no aparece). Un registrado siempre gana sobre una pendiente de la misma clave. `Borrar` sigue vaciando
   `pendientes`. Actualiza el `///` de la clase.
2. PlayMode `Tests/PlayMode/Progresion/GuardadoClavesPendientesPruebas.cs` (ruta forzada, nunca la partida real): guardar con
   `mecha` e `inventario` registrados; quitar esos dos (como al cambiar a una escena sin ellos); cargar y guardar otra vez: el archivo sigue
   teniendo `mecha` e `inventario` con el mismo texto; si después se registra un `mecha` nuevo, recibe lo guardado.
3. Encuentra la causa del fallo de `CambioEscenaPruebas` (corre Progresion completa varias veces con el log; busca qué prueba anterior
   deja `CambioEscena` con una transición o un interceptor puestos, o una escena cargada de más) y arréglala en la prueba o en el código que
   deja el estado. Anota la causa demostrada en la bitácora (la prueba anterior culpable y la línea).

## Carpetas permitidas
Assets/_Proyecto/Progresion/Guardado/, Assets/_Proyecto/Progresion/Escenas/, Assets/_Proyecto/Progresion/Pruebas/,
Assets/_Proyecto/Tests/PlayMode/Progresion/, Assets/_Proyecto/Tests/EditMode/Progresion/
No tocar: Core/, otras áreas, escenas, todo lo de Alan.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa (salvo los 9 fallos conocidos de Alan en Arena)
- [ ] GuardadoClavesPendientesPruebas pasa; las pruebas de guardado que ya existían (ServicioGuardado, Cama, Ligas, Inventario) siguen en verde
- [ ] `AREAS=Progresion bash herramientas/verificar.sh playmode-por-area` pasa 3 veces seguidas sin ningún fallo (también en la primera
      corrida, con Unity recién abierto)
- [ ] La bitácora nombra la causa demostrada del fallo de CambioEscenaPruebas (no "por defensa"); ninguna prueba quedó ignorada ni reordenada

## Criterios de sensación (para un humano)
- Ninguno (sin cambios visibles).

## Pruebas automáticas
- PlayMode: GuardadoClavesPendientesPruebas y la corrida repetida de Progresion.

## Fuera de alcance
- Varias ranuras de guardado, menú "continuar / nueva partida". Fallos de Alan en Arena.
