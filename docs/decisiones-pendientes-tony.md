# Decisiones pendientes de Tony (se pasan a docs/decisiones.md al aprobar)

Las D-tony-1 a D-tony-39 pasaron a docs/decisiones.md como D-033 a D-071 (más D-072 nueva) (2026-10-15).
La numeración sigue en D-tony-40 para no confundirla con las viejas.

## Planeación de T-F5 (2026-10-15, planificador Opus 5.5)

### D-tony-40 IDs de las tareas de T-F5
Propuesta. Las tareas de T-F5 de Tony son T-100 a T-112. No siguen en T-074 porque Alan ya usa T-074 a T-085 en alan/fase-2
(carro, arte, retenes, noche, calle, fachadas, letreros, bar, tráfico, modelo del carro). Se deja T-086 a T-099 libre para las
fases siguientes de Alan; las de Tony siguen desde T-113.

### D-tony-41 Contratos de T-F5 en Core (T-100)
Propuesta. Tres contratos nuevos, escritos antes de usarse (D-027) e integrados a main en cuanto se verifiquen:
- `Core/Guardado/IGuardable` (clave, `Guardar`, `Cargar`): la partida guardada se arma por piezas y cada pieza la implementa
  quien posee el dato. Tony: `cartera`, `renta`, `mecha`, `perfil`. Alan: `reloj` (y lo que el mundo necesite conservar después).
  `Cargar` no simula el tiempo (no avisa `DiaCambio` ni cobra) y nunca lanza excepciones.
- `Core/Jugador/IInteractuable` (`Posicion`, `Texto`, `PuedeUsar`, `Usar`): lo que el jugador a pie usa con E además de puertas y
  montables. Lo usa el banco del taller (T-109) y le sirve a Alan para la cama y el espejo de A-F3 sin otro contrato.
- `Core/Economia/IRenta`: la renta y el bloqueo del taller; Tony la implementa, Alan la puede leer para mostrar el bloqueo.
`MotivoDinero` gana `Carga` al final (cambio de dinero por cargar una partida; el HUD no lo anuncia). Solo cambian comentarios en
`ICartera` (ya se registra), `IPeleador.PrepararPelea` (daño persistente), `ITallerMecha.PuntoInteraccion` (el banco) e
`IRelojMundo` (el reloj de Alan se guarda con clave `reloj`). No se cambia `ModeloVida`.

### D-tony-42 Daño persistente del mecha del jugador (T-103). Reemplaza D-013
Propuesta. El mecha del jugador conserva su vida entre peleas, también en la revancha, y al volver al taller; el rival se restaura
siempre. Si al preparar una pelea (o al cargar) quedó por debajo del 15 % de su vida máxima o en KO, sube a ese piso de emergencia:
perder no deja el juego atorado, pero la revancha sin reparar es casi perdida (personaje-progresion.md: "reparas el mecha y puedes
volver a retar"). Va en `MechaPersistente` (Mecha/), que pone `PeleadorMecha.ConservarVidaAlPreparar` y se guarda con clave `mecha`.
Hay un solo mecha persistente por escena; si hay dos, el segundo avisa y no se guarda (en Ciudad, con el clandestino dentro, T-F6
decide con qué mecha se pelea en la arena). Morir en la calle, la clínica y el mecha que vuelve "muy dañado" son de T-F6/T-F7
(la demo no los necesita: la emboscada no se puede perder).

### D-tony-43 Efectos de vida baja (T-104, T-105)
Propuesta. Con la vida en 30 % o menos: velocidad ×0.7, aceleración y frenado ×0.6, giro ×0.75 y una alarma roja (LED y luz) que
parpadea a 2 Hz en la cabina (fija en KO). El cristal se cuartea en dos niveles: con 50 % o menos, grietas leves desde las orillas;
con 25 % o menos, grietas fuertes y una estrella de impacto. Las grietas nunca tocan el 40 % × 40 % central de la vista. Todo en
`ConfigMechaDaniado` (Mecha/Datos/). Los rivales no cambian.

### D-tony-44 Números de la economía de la demo (T-102, T-106, T-107)
Propuesta (provisionales, en assets de Economia/Datos/): dinero inicial 500; reparación 10 por punto de vida, mínimo 50 (de KO al
máximo con 100 de vida: 1000); premio de la pelea casera 600 al ganar y 0 al perder (sin apuestas todavía); premio del campeonato
del tutorial 1500; renta 500 cada 7 días de juego, primer cobro al empezar el día 8. Con esto una pelea dura (unos 65 de daño) cuesta
650 de reparar contra 600 de premio: se vive al día y quedar en negativo es posible, como pide economia-misiones.md.

### D-tony-45 Dónde vive la cartera (T-102)
Propuesta. `ServicioCartera` (ICartera + IGuardable) y `ServicioRenta` (IRenta + IGuardable) van en `Economia/Prefabs/ServiciosEconomia.prefab`,
anidado en `JugadorAPie.prefab` como `ServiciosProgresion.prefab` (D-055): un solo prefab del jugador. `MechaCyberpunk.Personaje.Editor`
referencia Economia para armarlo. En escenas sin jugador a pie (Pruebas_Tony) no hay cartera: las pruebas la instancian.
`ModeloCartera.Restablecer` fija el dinero al cargar y avisa con `MotivoDinero.Carga`. `FormatoDinero` ("$1,250", "-$150") vive en
Economia y lo usan Mecha y UI.

### D-tony-46 Reparación (T-109)
Propuesta. Solo reparación completa, en el banco del taller, con el mecha estacionado en su taller (estado `EnTaller` y vacío).
Cuesta `ReglaReparacion` (10 por punto, mínimo 50) y se cobra con `CargarDeuda` aunque no alcance (D-039: quedar en negativo es
intencional). Con el taller bloqueado por renta no se repara. Kits de reparación del mercado negro y reparar a medias: T-F7.

### D-tony-47 Renta y bloqueo del taller (T-107)
Propuesta. Al empezar el día de cobro (por `IRelojMundo.DiaCambio`), la renta se cobra sola si alcanza. Si no alcanza, o si ya había
adeudo, se suma al adeudo y el taller queda bloqueado hasta pagar todo el adeudo en el banco del taller. Bloqueado: no se repara ni se
mejora; sí se puede sacar el mecha y pelear dañado (propuesta de economia-misiones.md). Sin reloj en la escena no se cobra nunca.
Cargar una partida no cobra. El casero como personaje y avisos de "la renta vence mañana": después.

### D-tony-48 Premios de pelea (T-106)
Propuesta. `DirectorPelea` paga el premio al resultado con `DatosPremioPelea` (clase y assets en Economia/) a `Registro<ICartera>`,
una vez por pelea; cada revancha paga otra vez (el daño persistente y la reparación ya la encarecen; el límite llega con las ligas en
T-F7). La pantalla de la arena muestra "+$600". `MechaCyberpunk.Pelea` referencia `MechaCyberpunk.Economia` (mismo dueño, sin
ciclos). La emboscada no paga.

### D-tony-49 El banco del taller es un IInteractuable que vive en el mecha (T-108, T-109)
Propuesta. `InteraccionAPie` suma los `IInteractuable` a la misma competencia que puertas y montables (D-054) y el HUD dice
"E · Texto" o solo el texto si no se puede usar. `BancoTaller` (Mecha/Taller/) va en `MechaJugador.prefab`, pero solo se registra en
arranque Estacionado con un `ITallerMecha` registrado; su posición es el `PuntoInteraccion` del taller. Vive en el mecha porque
repara a ese mecha. Orden de lo que ofrece: pagar renta, bloqueado, mecha fuera, sin daño, reparar. `MechaCyberpunk.Mecha`
referencia `MechaCyberpunk.Economia`; Economia no referencia a nadie de Tony.

### D-tony-50 Guardado (T-111)
Propuesta. `ServicioGuardado` (Progresion/, en `ServiciosProgresion.prefab`) junta los `IGuardable` registrados en un JSON versión 1
en `Application.persistentDataPath/partida.json`, con escritura atómica (`.tmp` y reemplazo). Al cargar entrega cada pieza por clave,
también a las que se registren después; un archivo roto o de versión desconocida no cambia nada. Se guarda: dinero, renta y adeudo,
vida del mecha, nombre y alias, y el día y la hora del reloj. No se guarda: la posición del jugador (al cargar apareces donde la
escena te pone; en la demo, la casa), el calor, los objetivos ni el estado del llamado del mecha. Una sola ranura. Cuándo se guarda
solo (al dormir) y el menú "continuar / nueva partida": T-F6. Teclas de prueba: F5 guarda y F9 carga (solo en escenas de pruebas).

### D-tony-51 Reloj de prueba de Tony (T-107)
Propuesta. `Progresion/Pruebas/RelojDePrueba` cumple `IRelojMundo` e `IGuardable` (clave `reloj`) para probar renta y guardado sin
esperar el reloj de Alan, como los dobles de D-055 y D-068. Solo va en Pruebas_Calle y en pruebas; no va en Pruebas_APie (sus puertas
tienen horario propio). Tecla J: saltar a las 08:00 del día siguiente. Teclas de prueba nuevas: 7 y 8 (dinero ±500), J, F5 y F9.

### D-tony-52 Lo que espera a Alan en T-F5
Propuesta. Todo T-F5 se hace con dobles (TallerDePrueba, RelojDePrueba). La prueba de H5 en el taller de la casa de Alan será una
tarea de integración que se escribe cuando A-F3 esté en main (como T-073). Pedidos a Alan: su `RelojMundo` implementa `IGuardable`
con clave `reloj` (día y hora; al cargar no avisa `DiaCambio` ni `HoraEnteraCambio`); el `PuntoInteraccion` de su `ITallerMecha`
queda junto a un banco de trabajo y sin otro interactuable a menos de 2.5 m; la cama y el espejo de A-F3 pueden ser `IInteractuable`;
mostrar el taller bloqueado (con `IRenta`) es opcional.

### D-tony-53 Deudas de T-F4 primero (T-101)
Propuesta. Antes de tocar el mecha se quitan los pendientes de T-F4 (dos AudioListener, orientación del mecha estacionado, pruebas
posiblemente frágiles de la pandilla y la emboscada, capturas T-072 poco legibles) y se atiende el pedido de Alan (D-alan-19) de
prender `renderPostProcessing` en las cámaras de `MechaJugador.prefab` y `JugadorAPie.prefab`; en escenas sin Volume no cambia nada.

### D-tony-54 Orden de T-F5
Propuesta. T-100 (Core, a main en cuanto se verifique), T-101, T-102, T-103, T-104, T-105, T-106, T-107, T-108, T-109, T-110, T-111 y
T-112 (cierre). T-108 solo depende de T-100 y puede adelantarse si algo se bloquea; T-104 y T-105 pueden ir después de T-106 y T-107
si conviene llegar antes al banco del taller.

### D-tony-55 Dinero en el HUD a pie (T-110)
Propuesta. El dinero se ve solo a pie (D-052), con un aviso de 2.5 s por cada movimiento ("+$600 · PREMIO") salvo los de `Carga`, y
una línea fija "RENTA VENCIDA · $500" mientras el taller está bloqueado. En el mecha no hay dinero en pantalla: el premio se ve en la
pantalla de la arena. `MechaCyberpunk.UI` referencia Economia por `FormatoDinero`.

## Planeación de T-F6 (2026-10-16, planificador Opus 5.5)

### D-tony-56 IDs de las tareas de T-F6
Propuesta. Las tareas de T-F6 de Tony son T-120 a T-137. T-113 a T-119 quedan reservadas para deudas o correcciones de T-F5 que
aparezcan en la revisión. Alan sigue con T-074 a T-085 (A-F2) y T-086 a T-099 quedan libres para sus fases siguientes.

### D-tony-57 Contratos de T-F6 en Core (T-120)
Propuesta. Tres contratos nuevos y un cambio, escritos antes de usarse (D-027) e integrados a main en cuanto se verifiquen (detrás de
T-100, que sigue sin push a main):
- `Core/Historia/EtapaPrologo` (Ninguna, CreacionPersonaje, PeleaTutorial, SalidaClandestino, Emboscada, RegresoACasa, Dormir, Flashback,
  Despertar, PeleaCasera, Continuara, Libre; solo se agregan valores al final) e `IHistoria` (Etapa, EtapaCambio). La implementa Tony
  (`ServicioHistoria`, clave de guardado `historia`); Alan solo la lee para que su mundo acompañe. Sin `IHistoria`, el mundo es `Libre`.
- `Core/Jugador/AparienciaJugador` (Cara, Ropa, Implante: índices al catálogo de Tony) e `IPerfilJugador.Apariencia` (lo prometido en D-059).
  Alan no interpreta los índices: para mostrar al jugador coloca `CuerpoJugador.prefab`.
- `Core/Historia/IPuntoHistoria` (Id, Pose, Activar): puntos con nombre que pone Alan para la historia: planos de cámara del flashback
  (`Flashback_*`) y pasos de la ruta del prólogo (`Prologo_*`). Uno solo para los dos usos, así Alan aprende un contrato.
- ReglasEnsamblados acepta subensamblados `MechaCyberpunk.<Área>.<Sub>` con el dueño de `<Área>` (D-tony-62).

### D-tony-58 El prólogo es el futuro: al despertar empieza la historia (T-133)
Propuesta. concepto-historia.md dice que el prólogo es el futuro y que al llegar a esa noche "el juego no termina: es una bisagra". Por eso,
al despertar después del flashback, el director pone el estado inicial de la historia una sola vez: dinero inicial ($500), mecha al 45 %
de vida (grietas leves, sin alarma) en su taller, renta reiniciada (próximo cobro en 7 días, sin adeudo), calor 0 y reloj a las 19:30 (el
clandestino abre a las 20:00 y un día dura 20 minutos reales). El premio de $1,500 del campeonato se ve y se cobra en la noche del prólogo,
pero no sobrevive al despertar. Alternativa (si Tony prefiere): despertar a la mañana siguiente con el dinero y el daño que quedaron de la
emboscada. Pregunta para Tony en la revisión.

### D-tony-59 Daño persistente solo en el mecha de la historia (T-121). Modifica D-tony-42
Propuesta. `MechaPersistente` va en `MechaEstacionado.prefab` (el mecha de la historia, el que vive en el taller) y no en `MechaJugador.prefab`.
El arranque Directo (Pruebas_Tony, pruebas de pelea y el Clandestino.unity suelto de Alan) vuelve a restaurar la vida en cada pelea, como
D-013. Así la prueba de Alan `TresPeleasSeguidasConRevancha` pasa sin tocar Arena/, y en la demo las peleas del clandestino se juegan con el
mecha de la historia por medio de `EntradaArena` (D-tony-65), que sí conserva el daño.

### D-tony-60 Tope de frame en las pruebas de recorrido (T-122)
Propuesta. Los recorridos ya no exigen que ningún frame pase de 100 ms: `MedidorFrames` (Progresion/Pruebas/) ignora 2 s de calentamiento,
permite hasta 3 frames (o el 1 %) sobre 100 ms y ninguno sobre 500 ms, y da un resumen (máximo, percentil 99) para el mensaje del assert.
Sigue midiendo tirones reales sin fallar por el editor en batch.

### D-tony-61 Partida entre escenas por el guardado (T-124)
Propuesta. La creación de personaje, la ciudad y el flashback son escenas distintas. `CambioEscena.Ir(ruta)` guarda la partida, marca una
transición pendiente y carga la escena por ruta (en el editor con `EditorSceneManager.LoadSceneAsyncInPlayMode`, así no hace falta Build
Settings hasta T-137; en un build con `SceneManager`). En la escena nueva, `ServicioGuardado` carga la partida al arrancar solo si hubo
transición: dar Play directo en una escena de pruebas nunca lee la partida del humano. `ServicioGuardado.RutaForzada` (estática) aísla a las
pruebas que cruzan escenas. `DatosEscenas` dice qué escena va con cada etapa; `DatosEscenas_Pruebas` usa las escenas de Tony y
`DatosEscenas_Demo` las de la demo (hasta T-136, también las de Tony).

### D-tony-62 Ensamblado del prólogo (T-127)
Propuesta. El director del prólogo, la entrada a la arena, el mostrador, la cama, el espejo y el flashback encadenan Mecha, Pelea, Personaje,
Progresion, Economia, IA y UI, así que van en un subensamblado nuevo `MechaCyberpunk.Progresion.Prologo` (Progresion/Prologo/), encima de todos;
nadie lo referencia. CLAUDE.md ya dice que Progresion lleva "historia". Lo que es dato o servicio de bajo nivel (`ServicioHistoria`,
`CambioEscena`, `DatosEscenas`) se queda en Progresion. T-120 cambia la prueba de ensamblados para aceptarlo; si Tony quiere mencionarlo en
CLAUDE.md, lo hace él.

### D-tony-63 Apariencia (T-123)
Propuesta. 4 caras (tono de piel y peinado), 4 ropas (chamarra magenta, sudadera turquesa, chaleco naranja, overol azul) y 4 implantes
visibles con "Ninguno" (brazo cromado, ojo LED rojo, placa en la sien), en `CatalogoApariencia_Base` (Personaje/Datos/); nombres y colores
se cambian en el asset. El cuerpo es greybox de primitivas (`CuerpoJugador.prefab`) y una vista previa reutilizable
(`VistaPreviaApariencia.prefab`, cámara a RenderTexture lejos del mundo) sirve a la creación y al espejo. La apariencia se guarda con el
perfil; una partida vieja sin apariencia carga la predeterminada. No se ve el propio cuerpo en primera persona.

### D-tony-64 Pantalla de título y creación de personaje (T-126)
Propuesta. La demo arranca en `Escenas/Tony/CreacionPersonaje.unity` (D-029: escena de pantallas de Tony). Con partida guardada aparece un
título con CONTINUAR (va a la escena de la etapa guardada) y NUEVA PARTIDA (pide confirmar y borra). La creación se maneja con el teclado
(↑ ↓/Tab, ← →, Enter) y, si es barato, con el ratón; el texto se escribe con un campo propio que lee `Keyboard.onTextInput` (el `InputField`
de uGUI no recibe texto simulado). Nombre y alias son obligatorios (el presentador grita el alias). EMPEZAR deja la partida en
`PeleaTutorial` y pasa a la ciudad con un fundido.

### D-tony-65 El mecha de la historia entra a la arena (T-128)
Propuesta. `EntradaArena` (Prologo) lleva al mecha de la historia a la arena con un fundido (estado nuevo de llamado `EnArena`: sin calor ni
regreso automático), sube al jugador y arranca un `DirectorPelea` sin revancha (`permitirRevancha` en false); al terminar, el jugador queda a
pie en la aparición "Clandestino_Salida", el mecha vuelve al instante a su taller con su daño y el director se `Reinicia` para otra pelea.
Resuelve "aparecer ya dentro del mecha" (D-069). `FlujoPelea.prefab` (el que Alan usa con revancha) no cambia.

### D-tony-66 Primera pelea casera (T-129)
Propuesta (responde la pregunta abierta de demo.md "contra quién"). Rival "EL CHATARRAS" (vida 90, daño 10, un poco más lento, sin
`PiedadRival`: la casera sí se puede perder), presentador corto de dos líneas, premio $600 (0 al perder) y sin revancha. Se pelea desde
`MostradorPeleas.prefab` (un `IInteractuable` que Alan coloca dentro del clandestino): "Pelea casera · premio $600"; solo de noche y en las
etapas `PeleaCasera` o `Libre`; en la noche del prólogo dice "Hoy no hay peleas caseras".

### D-tony-67 Cama y espejo son prefabs de Tony (T-130). Cambia parte de D-tony-52
Propuesta. En vez de que Alan programe la cama y el espejo como `IInteractuable`, Tony entrega `CamaJugador.prefab` y `EspejoJugador.prefab`
(Progresion/Prologo/Prefabs/) y Alan los coloca en la casa (D-029). Dormir: fundido, reloj a las 08:00 y guardado automático (lo que D-tony-50
dejó para T-F6); en la etapa `Dormir` lleva al flashback; antes de llegar a casa en la noche del prólogo y en `PeleaCasera` no se duerme. El
espejo muestra a pantalla completa tu cuerpo con tu apariencia y tu alias (vista previa), sin reflejo real.

### D-tony-68 Flashback (T-131)
Propuesta. `Flashback.prefab` (Prologo) con `DatosFlashback`: 6 planos de unos 9 s (unos 55 s en total) con subtítulos, fundidos de 0.5 s,
cámara puesta en los `IPuntoHistoria` "Flashback_Plano_1" a "_6" de Alan (si falta uno, sigue con warning) y `Activar` para que Alan anime
cada plano. Se salta manteniendo Espacio 1 s. Al terminar pasa a `Despertar` y vuelve a la ciudad. Textos provisionales que cuentan que los
papás se fueron en una camioneta con un logo desconocido (el hilo de la corporación) y dejaron el mecha; Tony los cambia en el asset. Tony
prueba con `Escenas/Tony/Pruebas_Flashback.unity`.

### D-tony-69 Director del prólogo (T-127, T-132, T-133)
Propuesta. `DirectorPrologo.prefab` (Prologo; anida el flujo del tutorial, una `EntradaArena` y las pantallas de historia) se coloca una vez
en la ciudad. Avanza las etapas de `IHistoria` y fija un objetivo por paso: pelea de campeonato a las 22:00, "Ve a tu carro" (la emboscada
del callejón se activa en el camino; fuera de esa etapa queda deshabilitada con `DirectorEmboscada.Habilitada`), "Regresa a casa en tu carro"
con pasos intermedios por `IPuntoHistoria` "Prologo_Esquina_N" (D-053), "Duerme en tu cama", y en el presente "Ve al clandestino" y "Habla con el
encargado". Una partida cargada a media noche se reanuda desde la salida del clandestino (la emboscada se repite). Con el valor del prefab
(`etapaSinPartida` = `Libre`) no hace nada si Alan da Play en su Ciudad; Pruebas_Prologo (greybox de Tony con dobles) usa `PeleaTutorial`.

### D-tony-70 "Continuará" (T-125, T-133)
Propuesta (responde la pregunta abierta de demo.md). Al terminar la primera pelea casera, gane o pierda, se ve "CONTINUARÁ…" con el alias,
el dinero y el resultado; después de 4 s, Enter sigue jugando en la ciudad (etapa `Libre`: peleas caseras repetibles y taller) y Esc guarda y
vuelve al título.

### D-tony-71 Ayudas de controles (T-134)
Propuesta. Para que una persona nueva termine la demo sin ayuda (H6): la primera vez en la sesión que camina, maneja, llama al mecha, pelea
en el mecha o va en el mecha por la calle, ve una línea con las teclas que se va sola (12 s o cuando ya las usó). A pie va en el HUD; en el
mecha en una pantalla de la cabina fuera del centro (D-011). Lo visto se recuerda en la sesión aunque cambie la escena.

### D-tony-72 Lo que espera a Alan en T-F6 (T-136, T-137)
Propuesta. Todo T-F6 se hace con dobles y escenas de Tony (Pruebas_Prologo, Pruebas_Flashback). Quedan bloqueadas T-136 (el prólogo en
Ciudad.unity y Flashback.unity; espera A-F3 y A-F4, y también T-073 y T-061) y T-137 (Build Settings y build de Windows, de Core; espera
T-136). Pedidos a Alan, cuando T-120 y los prefabs estén en main: colocar en Ciudad.unity `DirectorPrologo.prefab`, `CamaJugador.prefab` y
`EspejoJugador.prefab` en la casa, `MostradorPeleas.prefab` dentro del clandestino (junto a la entrada a la arena), además de lo ya pedido
(`JugadorAPie`, `MechaEstacionado`, `Emboscada`); apariciones "Casa_Cama", "Casa_Puerta" y "Clandestino_Salida"; `IPuntoHistoria`
"Prologo_Carro" (donde queda su carro al salir del clandestino) y "Prologo_Esquina_N" en las vueltas de regreso a casa; en Flashback.unity el
escenario de la infancia, `Flashback.prefab` y los puntos "Flashback_Plano_1" a "_6"; leer `IHistoria` para dejar el carro cerca del
clandestino en la noche del prólogo y casino y mercado negro cerrados; su `RelojMundo` como `IGuardable` (D-tony-52).

### D-tony-73 Orden de T-F6
Propuesta. T-120 (Core, a main en cuanto se verifique), T-121, T-122, T-123, T-124, T-125, T-126, T-127, T-128, T-129, T-130, T-131, T-132,
T-133, T-134 y T-135 (cierre); T-136 y T-137 cuando Alan entregue. T-121, T-122 y T-125 no dependen de Core y pueden ir primero si T-120 se
atora; T-134 solo depende de T-121 y puede adelantarse; T-131 puede ir antes que T-128 a T-130 si conviene.

### D-tony-74 Build de la demo (T-137)
Propuesta. Build Settings con Escenas/Tony/CreacionPersonaje.unity (índice 0), Escenas/Ciudad.unity y Escenas/Flashback.unity (sale
SampleScene), nombre del producto "Mecha Cyberpunk (demo)" y build de Windows 64 bits en Builds/ (ignorado por git) con un método de editor.
Es la última tarea porque depende de las escenas finales de Alan; es de Core porque cambia ProjectSettings/.

## Aceptada por Tony (2026-10-16): D-tony-58
El prólogo es el futuro: al despertar se reinicia el estado (dinero $500, mecha al 45 %, renta reiniciada, calor 0, hora 19:30). T-132 y T-133 lo aplican; no se despierta "la mañana siguiente con lo que quedó".
