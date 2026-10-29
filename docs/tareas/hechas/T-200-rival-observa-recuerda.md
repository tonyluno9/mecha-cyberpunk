# T-200 El rival observa y recuerda: estilo por pelea, memoria y guardado
Dueño: Tony | Rama: tony/T-200-rival-observa-recuerda | Modelo: Sonnet 5.5, medium
Depende de: T-199 (y T-198 para el comentario de la clave; no bloquea)

## Objetivo
Los 20 rivales de las ligas y EL CHATARRAS (la casera) miran cómo peleas: cuántos puños tiras, cuánto te cubres y cuánto los castigas
justo después de su golpe. Al terminar cada pelea lo apuntan en su memoria, suben o bajan cada adaptación de nivel y lo avisan; la memoria
se guarda en la partida. Todavía no cambian su forma de pelear (eso es T-201 y T-202): esta tarea deja medido, recordado y guardado lo que
aprendieron, y la interfaz con la que la cabina lo leerá.

## Contexto
- Diseño: combate.md ("El rival se adapta a tu forma de pelear en peleas repetidas").
- Decisiones: D-013 (la revancha restaura la vida), D-tony-168 (lectura del rival en Combate). Propuestas: D-tony-193, D-tony-194, D-tony-195,
  D-tony-199.
- Contratos: `IGuardable` (clave `adaptacion`, T-198), `EventosPelea` (`PeleaIniciada`, `PeleaTerminada`, `ImpactoRecibido`), `ResultadoPelea`
  (`Ganador`, `DuracionSegundos`), `Registro<T>` (D-034).
- Código: `IA/Rival/RivalBasico.cs` (`Datos`, `Objetivo`, `Fase`; se registra como `ILecturaRival` en `Awake`), `IA/Rival/PiedadRival.cs` (cómo un
  componente acompaña a `RivalBasico` y escucha `PeleadorMecha.PeleaPreparada`), `Combate/Golpe/GolpeMecha.cs` (`Impacto`), `Combate/Peleador/PeleadorMecha.cs`,
  `Progresion/Guardado/ServicioGuardado.cs`, `Pelea/Editor/CreadorPresentador.cs` (otra área que agrega un componente a
  `ServiciosProgresion.prefab`), `IA/Editor/CreadorRivalesLiga.cs` y `CreadorHabilidadesJefes.cs` (los prefabs de liga son variantes de
  `RivalBasico.prefab`).
- Pruebas modelo: `Tests/PlayMode/IA/PiedadRivalPruebas.cs` (ArenaDePrueba + FlujoPelea + MechaJugador en código con entrada simulada).

## Qué hacer
1. Combate (lo mínimo, el jugador no cambia):
   - `GolpeMecha`: evento `Conecto` (`Action<ResultadoDanio>`), después del `RecibirDanio` de un golpe que sí dio a alguien. `Impacto` sigue igual.
   - `Combate/Lectura/ILecturaAdaptacion.cs`: `Nombre`, `Raiz`, `int Nivel(TipoAdaptacion tipo)`, `int Activas`, `event Action<TipoAdaptacion> Usada`
     (cuando el rival usa una adaptación; lo dispararán T-201 y T-202), `event Action<IReadOnlyList<CambioAdaptacion>> Aprendio` (una vez al
     cerrar cada pelea, aunque la lista venga vacía), `event Action NivelesCambiados`. `///` en cada miembro. Quien la implementa se agrega a
     `Registro<ILecturaAdaptacion>` en `Awake` y se quita en `OnDestroy` (como `ILecturaRival`: no se pierde al apagarse sin control).
2. `IA/Adaptacion/MemoriaRivales.cs` (MonoBehaviour, `IGuardable` con clave `adaptacion`): se agrega a `Registro<MemoriaRivales>` y a
   `Registro<IGuardable>` en `OnEnable` (se quita en `OnDisable`); `MemoriaRival Obtener(string idRival)` (crea una vacía si no hay),
   `void Olvidar(string idRival)`, `void OlvidarTodo()`, `event Action Cambio` (también al cargar). `Guardar`/`Cargar` con `ListaMemoriasRival`;
   texto que no se entiende: warning, no cambia nada, nunca lanza.
3. `IA/Adaptacion/AdaptacionRival.cs` (va junto a `RivalBasico`, fuera de la lista `controlados` para seguir oyendo en el KO; implementa
   `ILecturaAdaptacion`; `[SerializeField] DatosAdaptacionRival datos`, validado en `Awake`):
   - Id: `RivalBasico.Datos.name` (D-tony-191). Memoria: la de `Registro<MemoriaRivales>.Actual`; sin ella, una propia que vive con el rival
     (sirve para la revancha con R en escenas sin servicios; una instancia nueva empieza vacía).
   - Niveles: los calcula con `ReglaAdaptacion.Nivel` en `Start`, en cada `PeleaPreparada` de su `PeleadorMecha` y con `MemoriaRivales.Cambio`
     fuera de pelea; nunca a media pelea. Avisa `NivelesCambiados` si cambian.
   - Pelea: `PeleaPreparada` → se arma con un `PerfilEstilo` nuevo; `EventosPelea.PeleaIniciada` → observa (solo si está armado, así un rival
     que no está en esta pelea no aprende); `EventosPelea.PeleaTerminada` → si observaba: `ReglaEstilo.Vio` con `DuracionSegundos`,
     `ReglaAdaptacion.Aprender` (`ganoElJugador` = `Ganador == LadoPelea.Jugador`), avisa `Aprendio`, deja de observar; un `Debug.Log`
     `[IA] TORNILLO aprendió: GUARDIA I` u `olvidó: ...` por cambio.
   - Qué cuenta, solo mientras observa: con el `GolpeMecha` de su objetivo (`RivalBasico.Objetivo`), `Impacto` → golpe lanzado y `Conecto`
     contra este rival → conectado, "castigando" si `RivalBasico.Fase == FaseGolpe.Recuperando`; con `EventosPelea.ImpactoRecibido` (objetivo =
     el peleador de su objetivo, atacante = este rival) → golpe del rival que llegó, "cubierto" si `Bloqueo != Ninguno`. Suscripciones en
     `OnEnable`/`OnDisable` y al cambiar el objetivo; nada de `GetComponent` en `Update`.
   - Para lo que sigue: `IdRival`, `PerfilActual` (solo lectura; null fuera de pelea), `AvisarUso(TipoAdaptacion)` (dispara `Usada`), `Releer()`
     (recalcula niveles; para la herramienta de T-204).
4. `CreadorAdaptacionRival` (de T-199) agrega `AdaptacionRival` a los 20 prefabs de `IA/Prefabs/Ligas/` (ligas 1 y 2 con _Base, 3 y 4 con
   _Avanzado) y a `RivalCasero.prefab` (_Base), y `MemoriaRivales` a `Progresion/Prefabs/ServiciosProgresion.prefab` si no lo tiene. No toca
   `RivalBasico.prefab`, `RivalTutorial.prefab`, `RivalPandilla.prefab` ni los pandilleros a pie (D-tony-195). Idempotente; su `<summary>` dice
   que se corre después de CreadorRivalesLiga y CreadorHabilidadesJefes.
5. Pruebas:
   - EditMode `Tests/EditMode/IA/MemoriaRivalesPruebas.cs` (con `new GameObject().AddComponent<MemoriaRivales>()`): `Guardar` → `Cargar` deja
     todo igual; texto inválido no cambia nada y no lanza; `Olvidar` y `OlvidarTodo`; `Cambio` se avisa al cargar.
   - PlayMode `Tests/PlayMode/IA/AdaptacionRivalPruebas.cs` (`InputTestFixture`; ArenaDePrueba + FlujoPelea + MechaJugador + `RivalLiga1_1` en
     código y una `MemoriaRivales` en un objeto aparte; entrada simulada: clic izquierdo y derecho, tecla R; atajos permitidos y anotados:
     `Time.timeScale`, `RecibirDanio` al rival solo después de daño real del jugador):
     a) PUÑOS: clic izquierdo seguido con la guardia abajo hasta ganar: `PerfilActual.GolpesLanzados` es igual a los `Impacto` del `GolpeMecha`;
        al terminar, evidencia {1, 0, 0}, `Nivel(Guardia)` = 1 y `Aprendio` trae `[Guardia 0→1]`;
     b) R (revancha): `Nivel(Guardia)` = 1 al preparar; el rival pelea igual que antes (mismo daño por golpe recibido y dado; nada de esto
        cambia su forma de pelear todavía);
     c) CUBRIRSE: clic derecho sostenido casi toda la pelea, con golpes sueltos: FINTAS sube a I;
     d) CASTIGAR: la prueba espera el impacto del rival (lee `RivalBasico.Fase`) y suelta el clic justo después: RETIRADA sube a I y se
        cuentan bien los conectados castigando;
     e) la memoria vive en `MemoriaRivales`: `Guardar` trae el id `DatosRival_Liga1_1`; `OlvidarTodo` + `Cargar` devuelve los niveles; sin
        `MemoriaRivales` en la escena, la revancha recuerda y una instancia nueva del prefab empieza en 0;
     f) otro rival (`RivalLiga1_2`) no aprendió nada; con `RivalBasico.prefab` (sin `AdaptacionRival`) nada se registra en
        `Registro<ILecturaAdaptacion>`; con dos rivales que se adaptan en la escena, solo aprende el de la pelea;
     g) consola sin errores ni excepciones.

## Carpetas permitidas
Assets/_Proyecto/IA/Adaptacion/, Assets/_Proyecto/IA/Editor/CreadorAdaptacionRival.cs, Assets/_Proyecto/IA/Prefabs/Ligas/ y
Assets/_Proyecto/IA/Prefabs/RivalCasero.prefab (solo con el creador), Assets/_Proyecto/Progresion/Prefabs/ServiciosProgresion.prefab (solo con el
creador), Assets/_Proyecto/Combate/Golpe/GolpeMecha.cs (solo el evento), Assets/_Proyecto/Combate/Lectura/ILecturaAdaptacion.cs (nuevo),
Assets/_Proyecto/Tests/EditMode/IA/, Assets/_Proyecto/Tests/PlayMode/IA/, docs/bitacora/
No tocar: Core/, IA/Rival/ (el rival aún no cambia su forma de pelear), IA/Datos/ (solo se usan los assets de T-199), UI/, Pelea/, escenas,
`RivalBasico.prefab`, `FlujoPelea.prefab`, `MechaJugador.prefab`, todo lo de Alan.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa (salvo los fallos conocidos de Alan en Arena) y `playmode-por-area` no marca ningún área "LIMITE"
- [ ] AdaptacionRivalPruebas pasa 3 corridas seguidas y MemoriaRivalesPruebas pasa; la bitácora imprime, por pelea, el perfil (lanzados,
      golpes por minuto, % cubierto, % castigando) y los niveles antes y después
- [ ] Siguen en verde sin cambios: RivalBasicoPruebas, PiedadRivalPruebas, FuriaJefePruebas, HabilidadRivalPruebas, RivalesLigaPruebas
      (EditMode y PlayMode), FlujoPeleaPruebas, LigasMostradorPruebas, RecorridoHabilidadesPruebas, OjosSinapsisPruebas y RecorridoImplantesPruebas
- [ ] Correr los creadores dos veces no cambia los 21 prefabs ni `ServiciosProgresion.prefab` (md5 igual); `git diff main` no toca
      `RivalBasico.prefab`, `RivalTutorial.prefab`, `RivalPandilla.prefab` ni `FlujoPelea.prefab`
- [ ] Los asmdef no cambiaron (IA sigue sin ver Pelea, Progresion ni UI)

## Criterios de sensación (para un humano)
- En Pruebas_Economia, dos peleas contra TORNILLO peleando como peleas tú, y leer la consola: ¿el hábito que dice `[IA] TORNILLO aprendió`
  es el que de verdad usaste? ¿Alguna vez dice que te cubres o que castigas cuando no lo hiciste?

## Pruebas automáticas
- EditMode: MemoriaRivalesPruebas. PlayMode: AdaptacionRivalPruebas.

## Fuera de alcance
- Que el rival cambie su forma de pelear (T-201, T-202), la pantalla de la cabina (T-203), la escena y la herramienta (T-204). Contar las
  habilidades del jugador como hábito (D-tony-201 d).
