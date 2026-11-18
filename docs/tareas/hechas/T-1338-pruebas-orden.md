# T-1338 Pruebas que fallan según el orden

Origen: pendiente de docs/playtest-17nov-b.md (no está en el repo; se trabajó con el encargo). Hecho sin abrir Unity (sin Library):
nada de esto está compilado ni corrido. El orquestador debe compilar y correr las dos tandas de abajo antes y después.

## Síntoma
Si en una corrida de PlayMode van antes `PoliciasT709Pruebas` o `AsaltoDineroT728Pruebas`, las pruebas de Personaje con entrada simulada fallan en masa
("no salió ningún disparo", "E no recogió"). Solas pasan.

## Causa más probable (sin confirmar en Unity)
- `PoliciasT709Pruebas` (IA/PoliciasT709Pruebas.cs, SetUp ~línea 65 y TearDown ~108) no es `InputTestFixture`, instancia el jugador real y un `GestorPersecucion`
  con patrullas, drones y policías. Su TearDown solo destruye `barrio`, `extras` y los `PandilleroAPie`: patrullas, drones y policías que el gestor crea fuera de `barrio`
  siguen vivos en la clase siguiente y atacan o estorban al jugador nuevo.
- `ControladorAPie.OnEnable` (Personaje/Movimiento/ControladorAPie.cs:152-164) bloquea el cursor y habilita acciones compartidas; el cursor bloqueado y el estado de entrada
  quedan así para quien siga.
- `RespuestaNova` (IA/APie/RespuestaNova.cs:73, `DontDestroyOnLoad`) es un singleton que vive toda la corrida; con la búsqueda que dejan las pruebas de policía suelta guardias que persiguen al jugador.
- `AsaltoDineroT728Pruebas` carga Ciudad_Prologo (escena completa con tráfico, policías y Nova) y solo destruye las raíces de la escena activa en su `[UnityTearDown]`.
- Con Enter Play Mode Options el estado estático sobrevive (unity-csharp.md), así que nada de esto se reinicia solo entre clases.

## Qué se hizo (solo Tests/)
- Nuevo `Tests/PlayMode/Personaje/LimpiezaEntreClasesT1338.cs`: tiempo a 1, pausa apagada, cursor libre, destruye `PandilleroAPie`, `PatrullaMundo`, `DronMundo`,
  `PeatonMundo` y `GestorPersecucion` sueltos, y limpia registros y suscriptores de eventos.
- `BaseJugadorAPie.Setup` llama a esa limpieza y apaga `RespuestaNova` (`Apagada = true`) durante la prueba; el TearDown la deja como estaba.
- `PoliciasT709Pruebas.Limpiar` llama a la limpieza al terminar.

## Pendiente
- `PandilleroAPie.DanioDeLaPolicia` (IA/APie/PandilleroAPie.cs:591, `private set`) no tiene forma de reiniciarse desde pruebas; se reinicia solo en el mismo método que lo pone. Si resulta ser la causa, hace falta una tarea que le agregue un reinicio estático.
- `AsaltoDineroT728Pruebas` no llama a la limpieza (su asmdef no referencia el de Personaje); la base de Personaje ya limpia al empezar.

## Cómo comprobarlo
Tanda A y B (antes y después): `-testFilter "MechaCyberpunk.Tests.PoliciasT709Pruebas;MechaCyberpunk.Tests.Personaje.ArmasPruebas"` y
`-testFilter "MechaCyberpunk.Tests.AsaltoDineroT728Pruebas;MechaCyberpunk.Tests.Personaje.ArmasPruebas"` (ajustar el nombre de la clase de armas si su namespace difiere).
