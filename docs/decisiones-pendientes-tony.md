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

## Planeación de T-F7, bloque 1: economía completa (2026-10-17, planificador Opus 5.5)

### D-tony-75 IDs y bloques de T-F7
Propuesta. T-F7 de Tony se planifica por bloques, en el orden de docs/hoja-de-ruta.md, y cada bloque se escribe cuando el anterior está
hecho. El bloque 1 (economía completa) son T-140 a T-151; los siguientes bloques siguen desde T-152. T-136 y T-137 siguen bloqueadas (esperan a
Alan) y T-138 y T-139 quedan libres para correcciones de T-F6.

### D-tony-76 Orden de los bloques de T-F7
Propuesta. 1) economía completa (premios, apuestas, venta de piezas, kits, reparar a medias, precios); 2) ligas (4 ligas de 5 rivales, campeón
con pelea de jefe, subir de liga, premios y momios por liga); 3) piezas por ramas y niveles (5 piezas, 4 ramas, 4 niveles, compra en el taller,
piezas viejas al inventario); 4) habilidades con cooldown y munición comprada; 5) implantes (4 espacios, clínica; con ella el doctor que te
levanta al morir en la calle y su deuda); 6) misiones y reputación (barrio, corporativas, ilegales y robos; favores; chatarra de robos);
7) radio con 3 estaciones; 8) adaptación del rival en peleas repetidas; 9) hackeo si sobra tiempo.

### D-tony-77 Estabilización primero y un helper para el estado de entrada (T-140)
Propuesta. Antes de la economía se arregla de raíz el cuelgue de PlayMode de Economia (diagnóstico por clase con el log; prohibido ignorar o
marcar pruebas) y la limpieza del estado de los `InputActionAsset` entre fixtures (hoy copiada en 8 pruebas) pasa a un helper estático
`Progresion/Pruebas/MapasDeAccionesDePrueba` dentro de `#if UNITY_EDITOR`, como `MedidorFrames`: todas las pruebas de PlayMode de Tony ya
referencian Progresion, así que no hace falta un ensamblado de pruebas compartido. También se quita el `Press` tras `Release` en el mismo cuadro.

### D-tony-78 Contrato de apuestas en Core y PlayMode por área (T-141)
Propuesta. `Core/Economia/IApuestasPelea`: la apuesta del jugador sobre SU próxima pelea (abierta, rival, momio, tope, apostado, apostar,
cancelar, avisos de cambio y de resolución). Dentro de Tony no hace falta (Pelea y Progresion ya referencian Economia); va en Core para que el
casino de Alan (A-F5, "apuestas sobre las peleas") tome apuestas sin implementar nada (D-027: el contrato antes de usarse). Anunciar y resolver
la pelea no están en el contrato: solo los hace Tony. `herramientas/verificar.sh` gana `playmode-por-area` (un ensamblado por llamada, con
límite de tiempo por área y sin tumbar la corrida si un área se cuelga). Se integra a main en cuanto se verifique.

### D-tony-79 Inventario del jugador en Economia, sin contrato de Core (T-142)
Propuesta. `ServicioInventario` (Economia/, en `ServiciosEconomia.prefab`, clave de guardado `inventario`) con objetos `DatosObjeto` (id,
nombre, tipo Pieza/Chatarra/KitReparacion, valor de venta, máximo y la oferta de tienda que lo entrega) y un `CatalogoObjetos`. No va en Core:
Alan no lo necesita, porque los mostradores que compran y venden son de Tony (D-tony-81). El bloque de piezas extenderá `DatosObjeto`. Si Alan lo
llega a necesitar (por ejemplo, premios del casino en objetos), será una tarea de Core.

### D-tony-80 Reglas de las apuestas (T-144, T-145)
Propuesta. Se apuesta dinero a que tú ganas, una vez por pelea, con montos fijos de la pelea (casera: $100, $250, $500) y solo si alcanza
(con dinero negativo no se apuesta). Se cobra al apostar. Ganas: te devuelven lo apostado más lo apostado × momio, además del premio; si apostaste
al menos el mínimo de la pieza, te llevas la pieza que el rival puso en juego (si no cabe en el inventario, te pagan su valor de venta). Pierdes:
pierdes lo apostado y el premio (economia-misiones.md). Se apuesta en una pantalla al pulsar E en el mostrador de peleas, justo antes de pelear;
Esc sale sin cobrar. Si la pelea no termina (el flujo se destruye o se reinicia), la apuesta se devuelve. Las apuestas no se guardan: en este
bloque solo hay apuesta abierta durante la pelea, que nunca se guarda a media pelea; si el casino de Alan permite apostar antes, el guardado de
una apuesta abierta se resuelve en esa tarea.

### D-tony-81 Los mostradores de tienda son prefabs de Tony (T-146, T-147). Cambia la idea de T-033
Propuesta. En T-033 se pensó que Alan haría el mostrador del mercado negro con `ITienda`. Como con la cama, el espejo y el mostrador de peleas
(D-tony-66, D-tony-67), Tony entrega `UI/Prefabs/MostradorMercadoNegro.prefab` (un `IInteractuable` con su pantalla de comprar y vender) y Alan
solo lo coloca dentro de su mercado negro. El mostrador está abierto si el `ILugar` registrado con su mismo `IdLugar` está abierto (el horario y
el "cerrado en la demo" siguen siendo de Alan); sin ese `ILugar` (escenas de Tony) está abierto. Registra su `ITienda` mientras está activo.
Solo lista ofertas que tienen un objeto de inventario (en este bloque, el kit); las demás llegan con su bloque. El casino (tragamonedas, fichas,
cajera) sigue siendo de Alan.

### D-tony-82 Venta (T-147)
Propuesta. Cada objeto tiene un valor de venta fijo (sin regateo ni precios que cambian); el mercado negro compra piezas y chatarra, no kits.
Vender paga con `MotivoDinero.Venta` y el HUD a pie lo anuncia ("+$300 · VENTA"). Las piezas viejas del mecha se venderán igual cuando exista el
bloque de piezas (al cambiar una pieza, la vieja va al inventario).

### D-tony-83 Kits de reparación (T-148)
Propuesta. Tecla G dentro de una pelea (no en el conteo, no en KO, no con vida completa): gasta un kit al instante y cura el 20 % de la vida
máxima de forma pareja en 1.5 s; máximo 2 por pelea; llevas máximo 3. Una pantalla de cabina fuera del centro dice "KITS n · G". Fuera de las
peleas del flujo (calle, emboscada) no se usan en este bloque.

### D-tony-84 Reparar a medias (T-149). Completa D-tony-46
Propuesta. E en el banco abre opciones: "REPARAR TODO" (fiado, como hoy), "REPARAR LA MITAD" (la mitad del daño, fiado) y "CON LO QUE TIENES"
(solo con dinero positivo menor al costo total, sin deuda). Sin pantalla en la escena, E repara todo como antes. No hay límite de crédito: un
límite podría atorar al jugador (sin dinero, mecha en KO y sin poder reparar para ganar).

### D-tony-85 Deudas
Propuesta. La deuda es el dinero negativo (D-039) más el adeudo de renta (D-tony-47). Con dinero negativo no se compra ni se apuesta (falla
`IntentarPagar`), pero sí se repara fiado y se pelea. Sin intereses ni cobradores en este bloque. La deuda con el doctor (personaje-progresion.md,
propuesta) llega con la clínica en el bloque de implantes.

### D-tony-86 Precios del bloque de economía (provisionales; T-142 a T-150)
Propuesta. Siguen los de D-tony-44 (inicial $500; reparación $10 por punto, mínimo $50; renta $500 cada 7 días; casera $600 al ganar y $0 al
perder; campeonato del tutorial $1,500). Nuevos: apuesta casera $100/$250/$500 con momio 1.0 (paga uno a uno) y tope $500; pieza en juego de la
casera: Brazo de chatarra desde $250; venta del Brazo de chatarra $300 (máximo 5), chatarra $50 (máximo 20); kit $250, cura 20 de 100 puntos
($12.5 por punto contra $10 del taller), máximo 3 en el inventario y 2 por pelea. Reglas de balance que vigila una prueba (T-150): premio de la
casera ≥ renta; reparar de KO cuesta más que el premio de la casera; el kit cuesta más por punto que el taller; nada se vende por su precio de
compra o más; la pieza en juego vale menos que el premio; la apuesta máxima no pasa del premio.

### D-tony-87 Escena Pruebas_Economia (T-143)
Propuesta. Una escena greybox de Tony, chica, para probar toda la economía después de la demo sin pasar por el prólogo: taller con el mecha de la
historia y su banco, mostrador de peleas con su arena de prueba, mercado negro de prueba siempre abierto, reloj de noche, historia en `Libre` y
teclas de prueba (7/8 dinero, 9 brazo, 0 kit, J reloj). Pruebas_Prologo no se usa para esto porque arranca el prólogo.

### D-tony-88 Orden del bloque 1 de T-F7
Propuesta. T-140 (deudas), T-141 (Core, a main en cuanto se verifique), T-142, T-143, T-144, T-145, T-146, T-147, T-148, T-149, T-150 y T-151
(cierre). T-141 no depende de T-140 y puede ir primero si T-140 se atora; T-149 solo necesita T-143 y puede adelantarse; T-148 no necesita T-146
(las pruebas dan los kits por código).

### D-tony-89 Pestaña VENDER (T-147)
Propuesta. `MostradorTienda.compra` es la lista de tipos que compra el mostrador (el mercado negro: Pieza y Chatarra); con la lista vacía la pestaña
VENDER se esconde y → no la abre. La regla pura `ReglaVenta` dice qué se vende (tipo comprado y `valorVenta` > 0) y cuánto paga (`valorVenta`, fijo).
Vender quita uno del inventario primero y después abona con `MotivoDinero.Venta`; si quitar falla no paga. Una fila por objeto distinto, con
"$300 c/u" y "TIENES n"; la fila elegida se acomoda sola cuando el objeto se acaba. Textos en `ConfigPantallaTienda`: "VENDISTE {0} POR {1}" y
"NO TIENES NADA QUE LE INTERESE".

### D-tony-90 Pantalla del banco dentro del mecha (T-149)
Propuesta. `PantallaBancoTaller.prefab` (UI/) va anidado en `MechaJugador.prefab` (lo hereda `MechaEstacionado`), no en el HUD ni en las escenas: el
banco vive en el mecha (D-tony-49), la pantalla le pide sus opciones por el evento `BancoTaller.OpcionesPedidas` (`GetComponentInParent`) y así
Pruebas_Calle, Pruebas_Economia, Pruebas_Prologo y las escenas de Alan que coloquen el mecha la traen sin colocar ni regenerar nada. Sin pantalla
(o con la pantalla apagada) E repara todo como antes. Valores: la mitad = `ceil(daño / 2)` y solo si el daño es al menos 2 × `PuntosMinimos` (el
mínimo cobrable en puntos: 5 con $10 por punto y mínimo $50); "con lo que tienes" = `dinero / costoPorPunto` puntos (0 si el dinero no llega al
mínimo) y cuesta exactamente esos puntos × $10, así que con $125 gasta $120. ↑ ↓ no dan la vuelta; Enter elige y cierra; Esc cierra sin cobrar. El control
del jugador vuelve un cuadro después de cerrar (D-tony-67).

### D-tony-91 T-073 integrada desde tony/fase-6b-alan; T-136 y T-137 siguen bloqueadas
Propuesta. T-073 ya estaba hecha y verificada en tony/fase-6b-alan (Pelea 42/42 entonces); se mezcló en tony/fase-7 junto con alan/fase-3, conservando el `yield return null` de T-140 en `RecorridoEmboscadaBase` (Pelea 50/50 después). T-136 y T-137 quedan bloqueadas: Ciudad.unity de Alan (A-F3) no trae `DirectorPrologo.prefab` colocado, T-061 sigue en curso y falta A-F4. La suite de PlayMode del cierre del bloque 1 (EditMode 749/749, PlayMode 671/684) solo falla los 9 conocidos de Alan en `JaulaClandestinoPruebas`.

## Planeación de T-F7, bloque 2: ligas (2026-10-17, planificador Opus 5.5)

### D-tony-92 IDs y orden del bloque 2
Propuesta. El bloque 2 (ligas) son T-152 a T-162. Orden: T-152 (Core, a main en cuanto se verifique), T-153 (deudas), T-154, T-155, T-156,
T-157, T-158, T-159, T-160, T-161 y T-162 (cierre). T-153 y T-156 no dependen de Core y pueden ir primero si T-152 se atora; T-156 y T-157 (IA)
pueden ir en paralelo con T-154 y T-155. El bloque siguiente (piezas por ramas y niveles) empieza en T-163.

### D-tony-93 Contratos de ligas en Core (T-152)
Propuesta. `Core/Ligas/IProgresoLigas` (solo lectura: `CantidadLigas`, `LigaActual`, `CampeonesVencidos`, `NombreLiga`, `Cambio`, `CampeonVencido`)
y `Core/Pelea/InfoPelea` con `EventosPelea.PeleaPresentada` (rival, liga 0..4, si es jefe; una vez por pelea y por revancha, antes de la
presentación). Van en Core aunque dentro de Tony no hagan falta del todo porque: el bloque de piezas (Mecha/Economia, que no referencian
Progresion) toma `LigaActual` como el nivel máximo de pieza que se compra (mecha-taller.md: "4 niveles por rama, desbloqueados con las 4 ligas"),
y Alan puede leerlos para su clandestino (luces de la jaula en una pelea de jefe, carteles de la liga) sin implementar nada (D-027: el contrato
antes de usarse). Solo Tony registra victorias. Clave de guardado nueva: `ligas`. Además, `playmode-por-area` usa 2400 s para Progresion y 900 s
para las demás (T-151 vio que Progresion roza los 900 s).

### D-tony-94 Dónde vive cada parte de las ligas
Propuesta. Progreso puro y guardado: `ServicioLigas` en Progresion/Ligas/ (en `ServiciosProgresion.prefab`; Progresion solo referencia Core) con
`DatosLigas_Base` (nombres "SÓTANO", "CALLE", "DISTRITO", "CAMPEONATO", 5 rivales por liga, fracción de revancha 0.5). Lo que se pelea: `CartelPelea`
(clase serializable) y `CalendarioLigas_Base` en Pelea/Datos/ (Pelea ya referencia Economia y Progresion; carga los prefabs de IA por ruta sin
referenciar IA). Rivales: IA/Prefabs/Ligas/. Elegir y retar: `MostradorPeleas` (Progresion.Prologo, que referencia todo). Pantalla: UI/Peleas/.
Se usa el mismo `DirectorPelea` del mostrador (el casero) con `PrepararCartel`/`QuitarCartel`, en vez de un segundo flujo: un solo director
escucha el KO del jugador, y la casera sigue siendo lo que el prefab trae por defecto.

### D-tony-95 Reglas del progreso de las ligas (T-154, T-159)
Propuesta. Los 5 rivales de una liga se pelean en orden y solo se reta al siguiente (no hay saltos ni ligas más altas, como propone
personaje-progresion.md). Perder no cambia el progreso y se puede volver a retar cuando quieras (con el daño y la reparación como costo).
Vencer al quinto (el campeón) abre la siguiente liga; tras el campeón de la 4 eres "campeón del clandestino" y todo queda como revancha.
Revanchas: solo contra los rivales ya vencidos de tu liga actual (y los 5 de la última al final), pagan la mitad del premio (el límite a repetir
peleas que D-tony-48 dejó para las ligas) y no cambian el progreso. Las ligas solo se ofrecen en la etapa `Libre`; en `PeleaCasera` (la demo) el
mostrador sigue ofreciendo solo la casera. La casera sigue repetible y paga $600.

### D-tony-96 Pelea de jefe (T-155, T-156, T-161)
Propuesta. El campeón de cada liga: más vida y daño que su liga, presentador propio ("¡NOCHE DE CAMPEONES EN LA LIGA {liga}!"), letrero "PELEA DE
JEFE" en la pantalla de la arena y "¡VENCISTE AL CAMPEÓN!" al ganarle, y furia a la mitad de su vida (`FuriaJefe`, IA/: daño ×1.25, espera entre
golpes ×0.6, velocidad ×1.2 y cuerpo pulsando en rojo a 3 Hz, una vez por pelea; valores en `ConfigFuriaJefe_Base`). Sin `PiedadRival`: el jefe se
puede perder. `InfoPelea.EsJefe` permite que Alan cambie las luces de la jaula. El campeón del prólogo (tutorial) se anuncia como jefe de la liga 4
("la noche del prólogo es tu primera pelea de campeonato") pero conserva su piedad y no tiene furia.

### D-tony-97 Los 20 rivales (T-157)
Propuesta (responde, provisional, "nombres y comportamientos de los 20 rivales" de personaje-progresion.md). Sótano: TORNILLO, LA RATA, EL BACHE,
MOFLE y DON FIERRO; Calle: NEÓN, CUMBIA, EL TOPE, LA PATRULLA y LA MATRACA; Distrito: VOLTIO, LA DOÑA, GRÚA, EL PERITO y EL ALCALDE; Campeonato:
OBSIDIANA, XOLO, TLÁLOC, LA LLORONA y EL CACIQUE. El "comportamiento" por ahora es su ritmo (rápido y débil, lento y pesado, equilibrado) con los
valores de `RivalBasico`; vida de 80 a 320 y daño de 8 a 22 (tabla en T-157). Reglas que vigila una prueba: el primer rival no supera al jugador,
cada liga promedia más vida y daño que la anterior, el jefe es el más duro de su liga. Las ligas 3 y 4 están pensadas para un mecha con piezas
(bloque 3) y se reajustan entonces; con el mecha base son casi imposibles, y es esperado. Tony cambia nombres y valores en los assets.

### D-tony-98 Premios y momios por liga (T-158). Completa D-tony-86
Propuesta (provisionales). Premio de ganar: Sótano 600/700/800/900 y jefe 2,000; Calle 1,000/1,100/1,200/1,300 y jefe 3,000; Distrito
1,500/1,650/1,800/1,950 y jefe 4,500; Campeonato 2,200/2,400/2,600/2,800 y jefe 7,500. Perder paga 0, 100, 150 y 200 según la liga. Momio de la liga
1.0, 1.2, 1.4 y 1.6; del jefe 1.5, 1.8, 2.0 y 2.5; tope de apuesta igual o menor que el premio de ganar (liga: 500, 1,000, 1,500, 2,000; jefe: 1,000,
2,000, 3,000, 5,000). Pieza en juego solo en la liga 1 (Brazo de chatarra desde $250; con el jefe desde $500) hasta que existan las piezas por
rama. Reglas de balance (prueba de T-158): premio sube con el rival; jefe ≥ 2 × rival 1; cada liga empieza pagando más que el rival 4 de la
anterior; momio sube con la liga y el del jefe es mayor; tope ≤ premio; perder ≤ 20 % de ganar; el rival 1 paga al menos lo de la casera; el jefe
paga más que reparar de KO; la pieza en juego vale menos que el premio.

### D-tony-99 La cartelera del mostrador (T-159, T-160)
Propuesta. En `Libre`, E en el mostrador abre "PELEAS DEL CLANDESTINO · LIGA {liga} · CAMPEONES {n}/4" con filas en este orden: el siguiente rival
(o "JEFE DE LA LIGA …" en naranja), las revanchas de tu liga a mitad de premio, la pelea casera y la liga que sigue en gris ("BLOQUEADA · VENCE A
{jefe}"). ↑ ↓ eligen sin dar la vuelta, Enter pasa a la pantalla de apuesta de siempre (con el rival, el premio y el momio de ese cartel), Esc sale
sin cobrar y el control vuelve un cuadro después. Sin la pantalla, o con solo la casera, E hace lo de hoy (prólogo y escenas viejas sin cambios).

### D-tony-100 Subir de liga y campeón del clandestino (T-161)
Propuesta. Al vencer por primera vez al campeón de una liga, ya a pie, el HUD muestra 5 s "¡CAMPEÓN DE LA LIGA {liga}!" y "SE ABRE LA LIGA {siguiente}";
con el de la liga 4, "¡CAMPEÓN DEL CLANDESTINO!". No se avisa al cargar ni en revanchas (lo garantiza `IProgresoLigas.CampeonVencido`). Sin
cinemática ni música especial en este bloque.

### D-tony-101 Deudas del bloque 1 antes de las ligas (T-153)
Propuesta. Antes de sumar pruebas largas a Progresion: `CreadorKitReparacion.Crear` idempotente (hoy ensucia `MechaJugador.prefab`) y la causa de
que `CambioEscenaPruebas` y `PrologoPresentePruebas` fallen a veces en la primera corrida del área (prohibido ignorarlas o reordenarlas a mano). Las
pruebas "omitidas" de Mecha y UI son capturas que solo corren con su variable: no son deuda.

### D-tony-102 Lo que espera a Alan en el bloque 2
Propuesta. Nada del bloque espera a Alan: todo se prueba en Pruebas_Economia con dobles. `MostradorPeleas.prefab` (ya pedido en D-tony-72) trae la
cartelera y las ligas sin que Alan coloque nada nuevo. Pedidos opcionales para A-F5, cuando T-152 esté en main: cambiar las luces de la jaula si
`EventosPelea.PeleaPresentada` llega con `EsJefe`; un cartel en el clandestino con `IProgresoLigas.NombreLiga(LigaActual)`.

### D-tony-103 Preguntas abiertas para Tony
Propuesta: ninguna se aplica sin respuesta. (a) ¿Límite de peleas de liga por noche (por ejemplo, una) para que la renta y el reloj pesen más?
Hoy no hay límite. (b) ¿La casera sigue pagando $600 cada vez, o baja después de abrir las ligas? (c) Nombres de las ligas y de los 20 rivales:
¿te suenan bien o los cambias? (d) ¿El prólogo debe anunciar "PELEA DE JEFE" (adelanta su importancia) o solo las ligas?

### D-tony-104 Detalles de T-152
Propuesta: la línea de éxito de playmode-por-area imprime "(límite N s)"; el doble de IProgresoLigas ignora un campeón fuera de orden como si fuera revancha. Sin impacto en el diseño.

### D-tony-105 Detalles de T-153
Propuesta: el creador del kit ya no rehace la pantalla si existe (para cambiarla hay que borrarla del prefab antes); RaicesDePrueba es el helper para todo teardown. La fragilidad no se reprodujo, por eso el cierre es por defensa y no por causa demostrada. Sin impacto en el diseño.

### D-tony-106 Detalles de T-154
Propuesta: `rivalesPorLiga` en DatosLigas es un entero común a todas las ligas; `ModeloLigas.Poner` valida rangos pero no que campeones concuerde con la liga; sin asset de datos el servicio cae a 4x5 con nombres por defecto. Sin impacto en el diseño.
