# T-021 Rival básico
Dueño: Tony (D-026) | Rama: tony/T-021-rival-basico | Modelo: Sonnet 5.5, medium
Depende de: T-030 (ensamblados de pruebas de IA). Usa lo de la fase 1 que ya está en main (T-012, T-013).

## Objetivo
Existe un rival greybox que busca al jugador, se acerca, avisa cada golpe antes de darlo, pega por el contrato de Core,
recibe daño con las mismas reglas que el jugador y cae al llegar a 0. Es la otra mitad de la pelea completa de T-F2.

## Contexto
- Diseño: docs/diseno/combate.md y docs/diseno/personaje-progresion.md (el rival de la liga sótano es el más fácil).
- Decisiones: D-005 (datos), D-006 (ModeloVida), D-009 (ignorar el propio `transform.root`), D-011 (la vida del rival no se
  muestra), D-012 (la preparación del golpe es el aviso visible que permite bloquear), D-015 (sin NavMesh).
  Propuestas: D-tony-9 (IA referencia Combate y el rival reusa VidaMecha, PeleadorMecha y CicloGolpe) y D-tony-13 (el rival
  busca solo a su objetivo) en docs/decisiones-pendientes-tony.md.
- Esta tarea reemplaza la T-021 vieja de Alan (D-026): ya no hay `PeleadorDePrueba` ni Pruebas_Compa; el rival se prueba
  contra el MechaJugador real en Pruebas_Tony.unity.
- Código que se reusa (Combate/, de Tony): `VidaMecha` (vida, bloqueo y `ImpactoRecibido`), `PeleadorMecha` (lado, control y
  `PrepararPelea`), `CicloGolpe` (preparación, impacto una vez, recarga) y el barrido de `GolpeMecha.Impactar`.
- Cuidado: `PeleadorMecha` apaga los componentes de su lista `controlados` en el KO. Lo que tenga que seguir vivo en el KO
  (la caída, el parpadeo) va en un componente aparte que no esté en esa lista.
- Cuidado: `GolpeBloqueoPruebas` y `VidaJugadorPruebas` cargan Pruebas_Tony.unity. El rival de la escena no debe moverse
  ni atacar si nadie le da control, o esas pruebas se rompen.

## Qué hacer
1. Agrega `MechaCyberpunk.Combate` a las referencias de `IA/MechaCyberpunk.IA.asmdef` (mismo dueño, lo pide esta tarea).
2. Datos en `IA/Datos/`: `DatosMecha_RivalPrueba.asset` (como los valores por defecto, pero `preparacionGolpe` 0.6 s para que el
   aviso se lea) y `DatosRival_Prueba.asset` que lo use. `IA/Datos/ConfigPresentacionRival.cs` con su asset
   `ConfigPresentacionRival_Base.asset`: color del aviso (naranja emisivo), color y duración del parpadeo, duración de la
   caída, ángulo del brazo echado atrás.
3. Lógica pura `IA/Rival/CerebroRival.cs`: con la distancia al objetivo, si el objetivo está en KO, si el rival tiene control,
   `dt` y los `DatosRival`, decide Esperar, Acercarse o EmpezarGolpe. Se acerca si está más lejos que `DistanciaAtaque`;
   empieza un golpe en distancia cuando ya pasó la espera; después de cada golpe elige una espera nueva entre la mínima y la
   máxima con azar inyectado; sin control o con el objetivo en KO, espera.
4. Si conviene, saca el barrido de `GolpeMecha.Impactar` a un método estático en `Combate/Golpe/BarridoGolpe.cs` y úsalo desde
   `GolpeMecha` y desde el rival, sin cambiar lo que hace `GolpeMecha`. Agrega a `PeleadorMecha` un `event Action PeleaPreparada`
   que avise al final de `PrepararPelea`.
5. `IA/Rival/RivalBasico.cs` (en la lista `controlados` del `PeleadorMecha` del rival):
   - Objetivo: campo `GameObject` opcional; si está vacío, en `Start` busca el `IPeleador` del lado contrario en la escena
     (una sola vez, nunca en `Update`). `public void AsignarObjetivo(GameObject objetivo)` para cambiarlo después.
     Sin objetivo: `Debug.LogWarning` una vez y se queda quieto.
   - Camina con `CharacterController` hacia el objetivo con `VelocidadMaxima` y `Aceleracion`, gira sin pasar de
     `VelocidadGiroMaxima` y nunca se acerca más que `DistanciaMinima`.
   - Golpe con `CicloGolpe`: durante la preparación el brazo se echa atrás y se pinta del color de aviso; en el impacto usa el
     barrido con su `AlcanceGolpe` y su `DanioGolpe` contra el primer `IRecibeDanio` que no sea él mismo.
   - Campo `controlAlArrancar` (por defecto false): si es false, en `Start` llama a `ActivarControl(false)` de su peleador.
6. `IA/Rival/PresentacionRival.cs` (fuera de la lista `controlados`): parpadea con el color de parpadeo cuando
   `EventosPelea.ImpactoRecibido` trae a su propio peleador con daño aplicado > 0; en el KO se inclina hasta quedar tirado en
   la duración de la caída; con `PeleaPreparada` vuelve a quedar de pie al instante.
7. Prefab `IA/Prefabs/RivalBasico.prefab` (con un creador de editor en `IA/Editor/`, asmdef solo para Editor): cápsula de unos
   4 m de otro color que el piso, brazo de cubos al frente, `CharacterController`, `VidaMecha` con DatosMecha_RivalPrueba,
   `PeleadorMecha` lado Rival con `RivalBasico` en `controlados`, `RivalBasico` y `PresentacionRival`. Materiales en
   `IA/Materiales/`.
8. `IA/Pruebas/AlternadorControlRival.cs`: con una `InputAction` del inspector (tecla C) alterna `ActivarControl` del rival.
9. En Pruebas_Tony.unity: el rival a unos 15 m del jugador, mirándolo, lejos del saco y del atacante, sin control al arrancar,
   y el alternador en la escena.

## Carpetas permitidas
Assets/_Proyecto/IA/, Assets/_Proyecto/Combate/Golpe/ y Combate/Peleador/ (solo los pasos 4), Assets/_Proyecto/Tests/EditMode/IA/,
Assets/_Proyecto/Tests/PlayMode/IA/ y Assets/_Proyecto/Escenas/Pruebas_Tony.unity
No tocar: Core/, Mecha/ (ni el prefab del jugador), UI/, Pelea/, carpetas y escenas de Alan, ProjectSettings/.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa, incluidas GolpeBloqueoPruebas y VidaJugadorPruebas sin cambios
- [ ] PlayMode: con control, el rival llega hasta el MechaJugador y se detiene a una distancia entre `DistanciaMinima` y
      `DistanciaAtaque`, sin atravesarlo
- [ ] PlayMode: antes de cada golpe el brazo tiene el color de aviso durante al menos 0.55 s y solo después baja la vida del
      jugador; sin guardia la vida baja exactamente el `DanioGolpe` del rival (10)
- [ ] PlayMode con entrada simulada: manteniendo clic derecho desde antes del aviso, el golpe del rival entra reducido según
      `ReduccionBloqueo` del jugador y el resultado es `TipoBloqueo.Normal`
- [ ] PlayMode con entrada simulada: un clic izquierdo con el rival a menos de 3 m le baja la vida el `DanioGolpe` del jugador
      y lo hace parpadear
- [ ] PlayMode: si el jugador se mueve 90° alrededor del rival, el rival gira sin pasar de su `VelocidadGiroMaxima` en ningún frame
- [ ] PlayMode: en cuatro golpes seguidos las esperas entre golpes no son todas iguales y todas quedan entre la mínima y la
      máxima (más el tiempo del golpe)
- [ ] PlayMode: en KO el rival no se mueve ni ataca por 3 s, se inclina más de 60° en menos de 1.5 s, y más daño devuelve
      `Ignorado`; `PrepararPelea` lo deja de pie, con vida completa, en la pose pedida y sin control
- [ ] PlayMode: con `ActivarControl(false)` el rival no se mueve ni ataca por 3 s; en Pruebas_Tony arranca sin control
- [ ] Unity en vivo, Pruebas_Tony: con C el rival se activa y camina hacia el jugador; consola sin errores en 2 minutos
- [ ] Captura del rival en aviso (brazo atrás y naranja) desde la cabina: docs/capturas/T-021-1.png

## Criterios de sensación (para un humano)
- En Pruebas_Tony, con C: el aviso naranja se ve a tiempo para subir la guardia; bloquear "en la raya" se siente justo.
- El rival se siente pesado como un mecha, no como un muñeco que patina; la caída en KO se entiende.

## Pruebas automáticas
- EditMode (Tests/EditMode/IA/): CerebroRival: lejos se acerca; en distancia y con la espera cumplida empieza un golpe; en
  distancia pero esperando, espera; con el objetivo en KO o sin control, espera; la espera nueva siempre queda entre la mínima
  y la máxima (azar fijo).
- PlayMode (Tests/PlayMode/IA/): los criterios de arriba, con el prefab real del rival, MechaJugador.prefab, un piso creado en
  la prueba y `InputTestFixture` para el clic (ejemplo: Tests/PlayMode/Mecha/ControladorMechaPruebas.cs).

## Fuera de alcance
- Bloqueo, esquivas y adaptación del rival; varios rivales; ligas y nombres finales
- Flujo de pelea, conteo y revancha (T-022)
- Mostrar la vida del rival (D-011)
