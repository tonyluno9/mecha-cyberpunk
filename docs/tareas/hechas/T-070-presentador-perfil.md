# T-070 Presentador que grita tu alias y perfil del jugador
Dueño: Tony | Rama: tony/T-070-presentador-perfil | Modelo: Sonnet 5.5, medium
Depende de: T-062, T-068

## Objetivo
Antes del conteo, la pantalla de la arena muestra al presentador anunciando la pelea: el rival por su alias y el jugador por el
suyo, gritado. El alias sale del perfil del jugador (`IPerfilJugador`), que ya existe con un valor de respaldo hasta que la
creación de personaje (T-F6) lo llene. Queda un prefab de flujo para la pelea de campeonato del tutorial.

## Contexto
- Diseño: demo.md ("Pelea de campeonato tutorial ... donde el presentador grita tu alias"); personaje-progresion.md ("El
  presentador del clandestino dice mi alias"); D-011 (conteo y resultado en la pantalla grande de la arena).
- Contrato: Core/Jugador/IPerfilJugador.cs (T-062). Propuesta: D-tony-34 en docs/decisiones-pendientes-tony.md.
- Lo que ya hay: `MaquinaFlujoPelea` (lógica pura: Preparando, Conteo, Peleando, KO, Resultado), `DirectorPelea`,
  `PantallaPelea`, `ConfigFlujoPelea`, `FlujoPelea.prefab` (que Alan coloca en Clandestino.unity en T-044: no le cambies el
  comportamiento por defecto más allá de agregar la presentación), `ServiciosProgresion.prefab` anidado en `JugadorAPie.prefab`,
  `RivalTutorial.prefab` y el alias del rival en `IPeleador.Nombre` (T-068).
- Pelea no referencia Progresion ni IA: el alias se lee de `Registro<IPerfilJugador>` y el del rival de `IPeleador.Nombre`.

## Qué hacer
1. `MaquinaFlujoPelea`: etapa `Presentacion` antes del `Conteo`, con una lista de duraciones de líneas. Con 0 líneas pasa directo al
   conteo como hoy. Evento `LineaCambio(indice)`. La revancha no presenta salvo que se pida (`presentarEnRevancha`). Agrega el
   valor nuevo de `EstadoFlujoPelea` sin cambiar el de los existentes si alguna prueba o asset los guarda como número.
2. `ConfigPresentador` (Pelea/Datos/): líneas con `{alias}` y `{rival}`, segundos por línea (1.6), cuál línea es "el grito" (más
   grande y en otro color), alias de respaldo "NOVATO". Líneas de la demo, provisionales: "¡BIENVENIDOS AL CLANDESTINO!",
   "POR EL CAMPEONATO DEL BARRIO...", "EN ESTA ESQUINA... ¡{rival}!", "Y EN ESTA OTRA... ¡¡{alias}!!".
3. `DirectorPelea` y `PantallaPelea`: si el director tiene `ConfigPresentador`, muestra cada línea en la pantalla de la arena
   (mayúsculas, el grito más grande) y avisa `LineaDicha(string)` (para el audio que llegará después). Sin config, todo igual que hoy.
4. Perfil: `Progresion/Perfil/ServicioPerfil.cs : MonoBehaviour, IPerfilJugador` con `DatosPerfil` (Progresion/Datos/: nombre y
   alias de respaldo). `Fijar(nombre, alias)` pasa por una regla pura `ReglaAlias` (quita espacios de los lados, máximo 16
   caracteres, vacío o null = respaldo) y avisa `Cambio`. Se registra en `Registro<IPerfilJugador>`. Agrégalo a
   `ServiciosProgresion.prefab` con el creador de Progresion.
5. `Pelea/Prefabs/FlujoPeleaTutorial.prefab` con su creador: como `FlujoPelea.prefab` pero con `RivalTutorial.prefab` y
   `ConfigPresentador_Tutorial`. En Pruebas_Tony, una forma de probarlo con tecla (sin romper la P de la pelea normal ni las pruebas
   que cargan esa escena; si no cabe, una escena `Escenas/Tony/Pruebas_Tutorial.unity` generada por el creador de Pelea).
6. Pruebas:
   - EditMode `Tests/EditMode/Pelea/MaquinaFlujoPeleaPruebas.cs` (agregar casos): sin líneas pasa directo al conteo; con 4 líneas
     de 1.6 s el conteo empieza a los 6.4 s; la revancha no presenta por defecto y sí con `presentarEnRevancha`.
   - EditMode `Tests/EditMode/Progresion/ReglaAliasPruebas.cs`: espacios, largo, vacío, null.
   - PlayMode `Tests/PlayMode/Pelea/PresentadorPruebas.cs`: con un `ServicioPerfil` con alias "LA CHISPA" la pantalla muestra en
     orden las 4 líneas y la última contiene "LA CHISPA" y "EL CAMPEÓN" sale en la tercera; sin perfil registrado sale "NOVATO";
     durante la presentación ningún peleador tiene control; después viene el conteo y la pelea normal.
   - PlayMode `Tests/PlayMode/Progresion/ServicioPerfilPruebas.cs`: registro, `Fijar` y `Cambio`.

## Carpetas permitidas
Assets/_Proyecto/Pelea/, Assets/_Proyecto/Progresion/, Assets/_Proyecto/Escenas/Tony/ (solo Pruebas_Tutorial.unity si hace falta),
Assets/_Proyecto/Tests/EditMode/Pelea/, Assets/_Proyecto/Tests/EditMode/Progresion/, Assets/_Proyecto/Tests/PlayMode/Pelea/,
Assets/_Proyecto/Tests/PlayMode/Progresion/
No tocar: Core/, IA/ (solo se usa RivalTutorial.prefab), Personaje/ (su prefab se actualiza solo por el anidado de
ServiciosProgresion), Pruebas_Tony.unity salvo lo del punto 5 con su creador, las carpetas y escenas de Alan.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa, con FlujoPeleaPruebas (3 peleas con revancha) sin cambios
- [ ] Las pruebas nuevas de MaquinaFlujoPelea, ReglaAlias, Presentador y ServicioPerfil pasan
- [ ] Con `JugadorAPie.prefab` solo en una escena vacía hay exactamente un `IPerfilJugador` con alias "NOVATO"
- [ ] Captura de la pantalla de la arena con la línea del grito y el alias: docs/capturas/T-070-1.png (se lee desde la cabina)
- [ ] Consola sin errores en las pruebas

## Criterios de sensación (para un humano)
- ¿La presentación se siente como el arranque de una pelea de campeonato (emoción, el alias gritado) y no como un texto de carga?
  ¿6 segundos de presentación se sienten bien la primera vez?

## Pruebas automáticas
- EditMode: MaquinaFlujoPeleaPruebas (casos nuevos), ReglaAliasPruebas. PlayMode: PresentadorPruebas, ServicioPerfilPruebas.

## Fuera de alcance
- Voz y sonido del presentador y del público; luces de la arena (Alan)
- Pantalla de creación de personaje y guardar el perfil (T-F6 y T-F5); indicaciones de tutorial (T-F6)
