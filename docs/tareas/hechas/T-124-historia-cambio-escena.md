# T-124 Etapas de la historia y partida entre escenas
Dueño: Tony | Rama: tony/T-124-historia-cambio-escena | Modelo: Sonnet 5.5, medium
Depende de: T-120

## Objetivo
La partida sabe en qué etapa del prólogo va (`ServicioHistoria`, que implementa `IHistoria` y se guarda) y puede pasar de una escena a
otra sin perder nada: antes de cambiar se guarda, y la escena nueva carga la partida al arrancar. Con esto la creación de personaje, la
ciudad y el flashback (escenas distintas) se encadenan, y "Continuar" vuelve a la escena de la etapa guardada.

## Contexto
- Contrato: `IHistoria` y `EtapaPrologo` (T-120). Guardado: `ServicioGuardado` (Progresion/Guardado/, T-111): junta los `IGuardable`
  registrados, una ranura en `Application.persistentDataPath/partida.json`, `FijarRuta` por instancia (las pruebas usan una carpeta
  temporal), entrega a los que se registran después de cargar.
- Decisiones: D-055 (servicios en `ServiciosProgresion.prefab`, anidado en `JugadorAPie.prefab`), D-tony-50 (qué se guarda; "cuándo se
  guarda solo y el menú continuar/nueva partida: T-F6"). Propuesta nueva: D-tony-61.
- Las escenas de Alan (Ciudad.unity, Flashback.unity) no están en Build Settings y Build Settings es de Core (T-137, al final). En el
  editor, una escena que no está en Build Settings se carga con `EditorSceneManager.LoadSceneAsyncInPlayMode` por ruta; en un build,
  con `SceneManager.LoadSceneAsync` por ruta.

## Qué hacer
1. `Progresion/Historia/ModeloHistoria.cs` (pura): etapa actual (empieza en `Ninguna`); `Avanzar(EtapaPrologo)` solo acepta una etapa
   posterior a la actual (si no, no cambia y devuelve false); `Reiniciar()` vuelve a `Ninguna` (nueva partida); avisa el cambio una vez.
2. `Progresion/Historia/ServicioHistoria.cs`: `IHistoria` + `IGuardable` (clave `historia`, guarda la etapa por nombre, no por número,
   para sobrevivir si el enum crece al final); se registra en `OnEnable`. Al cargar avisa `EtapaCambio` solo si cambió. Va en
   `ServiciosProgresion.prefab` (agrégalo con el creador que arma ese prefab).
3. `Progresion/Datos/DatosEscenas.cs` (ScriptableObject): rutas de la escena del título (creación de personaje), de la ciudad y del
   flashback, y `RutaPara(EtapaPrologo)`: `Ninguna` y `CreacionPersonaje` → título; `Flashback` → flashback; las demás → ciudad.
   Dos assets con su creador: `DatosEscenas_Pruebas` (Escenas/Tony/CreacionPersonaje.unity, Escenas/Tony/Pruebas_Prologo.unity,
   Escenas/Tony/Pruebas_Flashback.unity; esas escenas las crean T-126, T-127 y T-131) y `DatosEscenas_Demo` (por ahora las mismas
   rutas; T-136 la apunta a Ciudad.unity y Flashback.unity de Alan).
4. `Progresion/Escenas/CambioEscena.cs` (estática): `Ir(string ruta)` guarda la partida con el `ServicioGuardado` registrado (si hay),
   marca que hay una transición pendiente y carga la escena por ruta (editor: `EditorSceneManager.LoadSceneAsyncInPlayMode` dentro de
   `#if UNITY_EDITOR`; build: `SceneManager.LoadSceneAsync`); `bool ConsumirTransicion()` devuelve true una sola vez tras una
   transición; `Interceptar(Action<string>)` para pruebas que solo quieren saber qué escena se pidió, sin cargarla. Todo lo estático
   se limpia al entrar a Play (como `Registros`).
5. `ServicioGuardado`: en `Start`, si `CambioEscena.ConsumirTransicion()` es true, carga la partida (sin transición no carga: las
   escenas de pruebas y quien da Play directo no leen la partida del humano). `static string RutaForzada` (null por defecto, se limpia
   al entrar a Play) que, si está puesta, gana sobre la ruta por defecto en todas las instancias: así una prueba que cruza escenas no
   toca la partida real.
6. Que `ServicioGuardado` se pueda encontrar sin `Find` desde otras áreas de Tony (por ejemplo `Registro<ServicioGuardado>` o una
   propiedad estática `Actual`); úsalo en `CambioEscena` y en `GuardadoDePrueba`.

## Carpetas permitidas
Assets/_Proyecto/Progresion/ (Historia/ nueva, Escenas/ nueva, Datos/, Guardado/, Pruebas/GuardadoDePrueba.cs, Editor/, Prefabs/ con su
creador), Assets/_Proyecto/Tests/EditMode/Progresion/, Assets/_Proyecto/Tests/PlayMode/Progresion/
No tocar: Core/, todo lo de Alan, escenas existentes, ProjectSettings/ (nada de Build Settings).

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa
- [ ] EditMode `ModeloHistoriaPruebas`: avanza solo hacia adelante, no avisa si no cambia, `Reiniciar` vuelve a `Ninguna`
- [ ] EditMode `DatosEscenasPruebas`: `RutaPara` da título, ciudad o flashback según la tabla del punto 3 para cada valor del enum
- [ ] PlayMode `ServicioHistoriaPruebas`: se registra como `IHistoria` e `IGuardable` (clave `historia`); guardar y cargar conserva la
      etapa; un texto con una etapa desconocida no cambia nada y solo avisa con warning
- [ ] PlayMode `CambioEscenaPruebas` (con `RutaForzada` en una carpeta temporal): en una escena vacía con `ServiciosProgresion.prefab`
      se fija alias "EL RAYO" y etapa `PeleaTutorial`, `CambioEscena.Ir` a Pruebas_APie.unity; ya cargada, el perfil del jugador de esa
      escena dice "EL RAYO" y `Registro<IHistoria>.Actual.Etapa` es `PeleaTutorial`; dar Play directo a Pruebas_APie (sin transición) no
      carga nada; con `Interceptar` no se carga ninguna escena y se recibe la ruta pedida
- [ ] Ninguna prueba escribe en `Application.persistentDataPath/partida.json` (el verificador revisa la fecha del archivo o que no exista)
- [ ] ServicioGuardadoPruebas, PartidaCompletaPruebas y RecorridoTallerPruebas siguen pasando

## Criterios de sensación (para un humano)
- Ninguno (se juega en T-126 y T-132).

## Pruebas automáticas
- EditMode: ModeloHistoriaPruebas, DatosEscenasPruebas. PlayMode: ServicioHistoriaPruebas, CambioEscenaPruebas.

## Fuera de alcance
- Fundidos al cambiar de escena (T-125, los usa quien llama a `CambioEscena`); menú de título y nueva partida (T-126); quién avanza la
  etapa (T-126, T-130, T-131, T-132, T-133); Build Settings (T-137)
