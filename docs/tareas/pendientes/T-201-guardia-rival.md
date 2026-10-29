# T-201 GUARDIA: el rival que te estudió bloquea tus puños
Dueño: Tony | Rama: tony/T-201-guardia-rival | Modelo: Sonnet 5.5, medium
Depende de: T-200

## Objetivo
Un rival que aprendió GUARDIA sube los brazos cuando ve venir tu golpe y lo bloquea (bloqueo normal, nunca perfecto) con la probabilidad
de su nivel. Mientras se cubre no ataca, no se cubre mientras prepara o recupera su propio golpe, y un golpe más rápido que su reacción
(SINAPSIS II) le gana. Así machacar el clic deja de bastar y pegarle justo después de su golpe sí funciona: la adaptación se contesta
cambiando de estilo, no con más daño.

## Contexto
- Diseño: combate.md ("los brazos son de precisión: golpes, bloqueos y contraataques"; "Puedo bloquear un golpe y contraatacar con buen timing").
- Decisiones: D-012 (golpe en tres tiempos), D-tony-122 (sin cambiar materiales en ejecución), D-tony-165 (SINAPSIS). Propuestas: D-tony-192,
  D-tony-196 (GUARDIA: 0.4 / 0.65, reacción 0.06 s, sostener 0.3 s), D-tony-197 d, D-tony-199.
- Código: `Combate/Bloqueo/GuardiaMecha.cs` (hoy pide una `InputActionReference`), `Combate/Bloqueo/CalculoBloqueo.cs`, `Combate/Vida/VidaMecha.cs`
  (busca `GuardiaMecha` y aplica el bloqueo; `RecibirDanioSinGuardia` del MARTILLO), `Combate/Golpe/GolpeMecha.cs` (`Fase`), `Combate/Peleador/PeleadorMecha.cs`
  (`controlados`, `Aturdir`), `IA/Rival/RivalBasico.cs` (`Detenido`, brazo y colores de aviso con `MaterialPropertyBlock`), `IA/Rival/HabilidadRival.cs`
  (`EnAviso`), `IA/Datos/ConfigPresentacionRival.cs` y su creador (`IA/Editor/CreadorRivalBasico.cs`), `IA/Adaptacion/AdaptacionRival.cs` (T-200).
- Pruebas modelo: `Tests/PlayMode/IA/AdaptacionRivalPruebas.cs` (T-200), `Tests/PlayMode/IA/PiedadRivalPruebas.cs`.

## Qué hacer
1. Combate (el jugador no cambia):
   - `GuardiaMecha`: modo serializado `mandoPorCodigo` (por defecto falso): sin `InputActionReference` (no da error), sube y baja con
     `Sostener(bool)` usando el mismo `TiempoSubirGuardia` de su `DatosMecha`; sin el modo, todo igual que hoy. `permitePerfecto` (por defecto
     verdadero): en falso, `VidaMecha` calcula el bloqueo con ventana 0, así que siempre es Normal. Expone `PermitePerfecto` y `Progreso`.
   - `VidaMecha`: usa `PermitePerfecto` al llamar a `CalculoBloqueo`. Nada más cambia.
2. `IA/Adaptacion/ReglaGuardiaRival.cs` (pura, con `dt` y azar inyectado): estados Abajo → EsperandoReaccion → Sosteniendo. Cuando el golpe
   del jugador pasa de Listo a Preparando, tira el azar contra `probabilidadGuardia[nivel − 1]`; si sale, tras `reaccionGuardia` s pide
   sostener, pero solo si en ese momento el rival está libre (su golpe en Listo, sin `Detenido`, sin aviso de habilidad, con control; si no,
   no se cubre esta vez); sostiene hasta `sostenerTrasImpacto` s después de que el golpe del jugador sale de Preparando y luego suelta. Nivel 0:
   nunca. Un golpe nuevo del jugador mientras sostiene alarga el sostener.
3. `IA/Adaptacion/GuardiaRival.cs` (en el rival, dentro de `controlados`): lee el `GolpeMecha` del objetivo y su `Nivel(Guardia)` de
   `AdaptacionRival`, aplica la regla a la `GuardiaMecha` del rival y llama a `AvisarUso(Guardia)` cada vez que un golpe del jugador le entra con
   Bloqueo Normal estando la guardia arriba por la adaptación. `FijarAzar(Func<float>)` solo para pruebas (como el azar de `CerebroRival`).
4. `RivalBasico` (lo mínimo): con la guardia del rival levantada (`GuardiaMecha.Levantada`) no empieza golpes, pero sigue girando y caminando;
   el brazo va a una pose de guardia (`anguloBrazoGuardia`) y se pinta `colorGuardia`, en proporción a `GuardiaMecha.Progreso`, con el mismo
   `MaterialPropertyBlock`. `ConfigPresentacionRival` gana los dos campos (azul acero: ni el naranja del aviso, ni rojo puro, ni el turquesa de
   OJOS y del escudo); el creador que ya hace `ConfigPresentacionRival_Base` les pone valor sin pisar los demás (anotar cuál). `ILecturaRival` no
   cambia.
5. `CreadorAdaptacionRival` agrega a los prefabs que tienen `AdaptacionRival`: `GuardiaMecha` (mando por código, sin perfecto) y `GuardiaRival`,
   las dos en `controlados` de su `PeleadorMecha` (así el aturdimiento y el KO bajan la guardia). Idempotente.
6. Pruebas:
   - EditMode `Tests/EditMode/IA/ReglaGuardiaRivalPruebas.cs`: nivel 0 nunca; azar debajo de la probabilidad sí y encima no; la reacción exacta;
     no se cubre si el rival no está libre al reaccionar; suelta tras el impacto + sostener; dos golpes seguidos alargan.
   - `Tests/EditMode/Combate/` (o PlayMode de Combate si `VidaMecha` lo pide): con `permitePerfecto` falso, una guardia recién subida da Normal;
     con verdadero, todo igual que antes.
   - PlayMode `Tests/PlayMode/IA/GuardiaRivalPruebas.cs` (arena + flujo + MechaJugador + `RivalLiga1_1`, entrada simulada con el ratón;
     atajos anotados: niveles fijados en `MemoriaRivales` antes de `PeleaPreparada`, `FijarAzar`, `Time.timeScale`, `DatosMecha` del jugador
     clonado con la preparación de SINAPSIS II calculada con `ReglaReaccionImplantes`, `Aturdir` llamado como lo hace la DESCARGA):
     a) nivel 0: de 20 golpes del jugador que conectan, ninguno se bloquea (igual que antes de esta tarea);
     b) GUARDIA I con azar 0 (siempre se cubre): el golpe de serie entra con Bloqueo Normal y daño × (1 − `ReduccionBloqueo` del rival) en 10 de
        10; la guardia estaba arriba en el impacto; nunca Perfecto;
     c) lo mismo con la preparación de SINAPSIS II: 0 de 10 bloqueados;
     d) azar 0.99: 0 de 10; azar natural en 40 golpes: la fracción bloqueada queda en `probabilidadGuardia` ± 0.2 (se imprime);
     e) un golpe que el jugador suelta mientras el rival prepara o recupera su golpe nunca se bloquea, y el rival nunca entra en aviso con la
        guardia levantada;
     f) `Aturdir(1)` con la guardia arriba la baja; el MARTILLO (`RecibirDanioSinGuardia`) entra completo;
     g) `Usada(Guardia)` se avisa una vez por golpe bloqueado; consola sin errores.
   - Captura docs/capturas/T-201-1.png: TORNILLO con la guardia arriba, vista desde la cabina, revisada a ojo (se lee la pose y el color).

## Carpetas permitidas
Assets/_Proyecto/Combate/Bloqueo/GuardiaMecha.cs, Assets/_Proyecto/Combate/Vida/VidaMecha.cs, Assets/_Proyecto/IA/Adaptacion/,
Assets/_Proyecto/IA/Rival/RivalBasico.cs, Assets/_Proyecto/IA/Datos/ConfigPresentacionRival.cs y su asset _Base, Assets/_Proyecto/IA/Editor/,
Assets/_Proyecto/IA/Prefabs/ (solo con creadores), Assets/_Proyecto/Tests/EditMode/IA/, Assets/_Proyecto/Tests/EditMode/Combate/,
Assets/_Proyecto/Tests/PlayMode/IA/, Assets/_Proyecto/Tests/PlayMode/Combate/ (solo si hace falta para VidaMecha), docs/capturas/, docs/bitacora/
No tocar: Core/, los `DatosMecha` y `DatosRival` (los tiempos de los rivales no cambian), `DatosAdaptacionRival_*` (salvo que una regla de
balance falle: se anota), `MechaJugador.prefab`, UI/, Pelea/, escenas, todo lo de Alan.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa (salvo los fallos conocidos de Alan en Arena) y `playmode-por-area` no marca ningún área "LIMITE"
- [ ] ReglaGuardiaRivalPruebas y GuardiaRivalPruebas pasan 3 corridas seguidas; la bitácora imprime, por golpe del jugador, el momento de la
      guardia arriba contra el impacto (golpe de serie y con SINAPSIS II) y la fracción bloqueada con azar natural
- [ ] Las pruebas del golpe y la guardia del jugador siguen en verde sin cambios (GolpeBloqueoPruebas y las de Combate), igual que
      AdaptacionRivalPruebas, RivalBasicoPruebas, HabilidadRivalPruebas, FuriaJefePruebas, OjosSinapsisPruebas y RecorridoHabilidadesPruebas
- [ ] Correr los creadores dos veces no cambia los prefabs (md5 igual); `MechaJugador.prefab` y `RivalBasico.prefab` sin cambios
- [ ] Captura T-201-1 revisada a ojo

## Criterios de sensación (para un humano)
- Pruebas_Economia: pelea contra TORNILLO a puro clic y luego la revancha (o Pruebas_Adaptacion cuando exista, T-204): ¿se nota que ya te
  leyó? ¿La pose de guardia se ve de lejos y a tiempo? ¿Pegarle justo después de su golpe se siente como la respuesta natural, o te sientes
  castigado por atacar?

## Pruebas automáticas
- EditMode: ReglaGuardiaRivalPruebas y la del bloqueo sin perfecto. PlayMode: GuardiaRivalPruebas.

## Fuera de alcance
- Fintas y retirada (T-202), la pantalla de la cabina (T-203). Que el rival bloquee las habilidades a propósito. Bloqueo perfecto del rival.
