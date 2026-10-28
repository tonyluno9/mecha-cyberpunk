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

### D-tony-107 Detalles de T-155
Propuesta: con cartel, el cartel manda entero: rival, premio, apuesta y presentador (un cartel sin apuesta o sin presentador no tiene apuesta ni presentación; no cae a lo del prefab). `QuitarCartel` devuelve false si no había cartel. `PrepararCartel` no crea al rival: nace al empezar o al anunciar la apuesta. `{liga}` queda vacío sin servicio o con liga 0. El premio con fracción se redondea al entero (`Mathf.RoundToInt`). Sin impacto en el diseño.

### D-tony-108 Detalles de T-156
Propuesta: la furia mide la vida del propio jefe (no la del jugador) y entra una sola vez por pelea; el color pulsa entre el color base del material y `colorFuria`, y el parpadeo blanco de daño tiene prioridad mientras dura. El jefe de prueba (150 de vida, 12 de daño) tiene espera fija de 3 s, preparación 0.3 s y recarga 0.2 s para que la prueba mida la espera sin azar (con las esperas aleatorias normales, 0.6 de multiplicador no garantiza 30 % porque preparación y recarga no se acortan). Sin impacto en el diseño.

### D-tony-109 Detalles de T-157
Propuesta: el creador solo escribe valores al crear un asset; si Tony cambia alias o números en los assets, volver a correr el creador no los pisa. `DatosMecha.nombre` es el alias con mayúscula inicial. Los prefabs son variantes de RivalBasico (que ya no lleva PiedadRival) y los jefes añaden FuriaJefe con ConfigFuriaJefe_Base. Aceleración, frenado, giro, recarga y bloqueo quedan en los valores por defecto de DatosMecha. Sin impacto en el diseño.

### D-tony-110 Detalles de T-158
Propuesta: los creadores solo escriben al crear un asset; si Tony ajusta premios o momios, volver a correrlos no los pisa. El calendario es una lista de filas (FilaLiga) con los carteles; `Cartel(liga, indice)` y `RivalesEn` son 1-based. El jefe es el último cartel de la fila (regla a lo exige). Perder paga lo mismo para todos los rivales de una liga. Regla d compara el momio mínimo de los rivales normales de cada liga contra el máximo de la anterior. Pelea.Editor referencia Economia.Editor para reutilizar las rutas de las apuestas. Sin impacto en el diseño.

### D-tony-111 Detalles de T-161
Propuesta: el letrero de jefe es un `Text` aparte de la pantalla de la arena (`textoJefe`, opcional); en la presentación y el conteo ocupa la franja 0.57-0.74 del alto (sobre la cabeza del rival, bajo la línea del presentador) y en el resultado la franja 0.35-0.5 (bajo "GANASTE"); las dos franjas son campos de `PantallaPelea`. Se enciende con `EventosPelea.PeleaPresentada` (si `Info.EsJefe`), se apaga con "¡PELEA!" y en el resultado solo celebra si ganó el jugador. El aviso de liga del HUD usa dos `Text` (la segunda línea se apaga en la última liga), vive en el centro arriba bajo el objetivo, y su temporizador solo corre mientras el jugador está a pie (en carro o mecha el aviso espera y los 5 s empiezan al bajar). Los textos, color, tamaños y segundos están en `ConfigHudAPie`. Sin impacto en el diseño.

### D-tony-112 Atajos del recorrido de las ligas (T-162)
Propuesta: RecorridoLigasPruebas fija $3000 iniciales por código, calcula las ganancias netas (premio más apuesta por momio, como T-159), repara en el banco antes de cada pelea que lo necesita, pelea al jefe y a EL CACIQUE sin apostar y comprueba la furia bajando al rival a umbral menos 0.1 con RecibirDanio. Todos los atajos están anotados en la prueba.

## Planeación de T-F7, bloque 3: piezas por ramas y niveles (2026-10-18, planificador Opus 5.5)

### D-tony-113 IDs y orden del bloque 3
Propuesta. El bloque 3 son T-163 a T-173. Orden: T-163 (Core, necesita aprobación), T-164 (deudas), T-165, T-166, T-167, T-168, T-169, T-170,
T-171, T-172 y T-173 (cierre). T-164 y T-165 no dependen de Core y pueden ir mientras T-163 espera aprobación; T-170 y T-171 pueden ir en paralelo
con T-168 y T-169 (T-171 solo necesita T-166 para los datos). El bloque siguiente (habilidades) empieza en T-174.

### D-tony-114 Core: valores del mecha en tiempo de ejecución (T-163). Necesita aprobación de Tony
Propuesta. `Core/Datos/ValoresMecha` (struct con los 13 valores de `DatosMecha`), `DatosMecha.Valores` y `DatosMecha.CrearEnEjecucion(ValoresMecha)`
(instancia `DontSave`, con los mismos límites del inspector; quien la crea la destruye). Es lo único de Core que necesitan las piezas: la suma de
piezas es de Tony (Mecha/), y vida, golpe, guardia y movimiento ya leen un `DatosMecha`. No cambia ningún campo serializado ni asset. Alan no lo
necesita, así que no hace falta llevarlo a main antes que el resto del bloque. Alternativa descartada: crear la instancia con
`JsonUtility.FromJsonOverwrite` desde Mecha/ sin tocar Core; funcionaría, pero depende de los nombres privados de los campos de un tipo de Core y
esconde un cambio de contrato (D-027). Si Tony no aprueba T-163, T-166 en adelante quedan bloqueadas; T-164 y T-165 siguen.

### D-tony-115 Qué mueve cada ranura (T-166)
Propuesta (provisional). Torso: vida (lo que más vida da). Piernas: velocidad (y aceleración, frenado y giro en la misma razón) y algo de vida.
Cada brazo: daño del golpe y algo de vida (los dos brazos suman al golpe, aunque el golpe sale del derecho: así "un brazo de fuego y el otro
militar" cuenta). Cabina: vida. Fuego además acorta la recarga del golpe (mínimo 0.3 s). Bloqueo, alcance y guardia no cambian en este bloque.

### D-tony-116 Ramas (T-166)
Propuesta. Cuerpo a cuerpo: más vida y velocidad, menos daño. Militar: golpe más pesado, frágil. Fuego: golpe medio-alto más seguido (recarga más
corta), la más frágil, algo más rápida que militar. Tech: vida y daño medios; su "cooldown más corto" (mecha-taller.md) llega con las habilidades
del bloque 4, que agregarán ese dato a `DatosPieza`. Se mezclan libremente: cada ranura tiene su propia rama. Rama `DeSerie` para las 5 piezas con
las que sale el mecha.

### D-tony-117 Niveles y desbloqueo por liga (T-166, T-168)
Propuesta. Niveles I a IV; se compra un nivel si es ≤ `IProgresoLigas.LigaActual` (sin servicio registrado, liga 1). El nivel siguiente se muestra
en gris "SE ABRE EN LA LIGA n" para dar ganas de subir. La pieza en juego de cada liga es de su mismo nivel (T-171), así que el criterio "nivel 3 no
disponible hasta la liga 3" se cumple también por apuestas. Las piezas de serie (nivel 0) dan exactamente los valores de `DatosMecha_Jugador`: el
prólogo, la demo y las pruebas viejas no cambian.

### D-tony-118 Dónde viven las piezas y cómo se guardan (T-166, T-167)
Propuesta. `DatosPieza` hereda de `DatosObjeto` (Economia), como pedía D-tony-79, y vive en Mecha/Piezas/ (Mecha ya referencia Economia; Economia no
referencia Mecha). Las 85 piezas están en `CatalogoObjetos_Base` (el inventario las guarda por id, se venden en el mercado negro con `ReglaVenta` y
se ganan en apuestas) y en `CatalogoPiezas_Base`. `EquipoMecha` va junto a `MechaPersistente`, solo en `MechaEstacionado.prefab` (el mecha de la
historia; el `Directo` de Pruebas_Tony y los rivales no cambian). Las piezas puestas se guardan DENTRO de la clave `mecha` (con el daño en puntos),
no en una clave nueva: así la vida máxima y el daño se cargan juntos sin depender del orden de registro, y no hace falta tocar los comentarios de
`IGuardable` en Core. Una partida vieja sin piezas carga con las de serie.

### D-tony-119 Reglas del taller de piezas (T-165, T-168, T-169)
Propuesta. Se cambian piezas solo en el banco del taller, con el mecha estacionado y vacío; una sola E para todo el taller ("E · Taller · piezas"
sin daño; "PIEZAS DEL MECHA" al final de las opciones de reparar). Comprar pone la pieza al instante y se paga con `IntentarPagar` (no fiado; con
dinero negativo no se compra, D-tony-85). Poner una del inventario es gratis. La pieza que se quita va al inventario; si no cabe (máximo 2 de cada
una), se paga su valor de venta al momento. Al cambiar una pieza se conservan los puntos de daño (la vida máxima cambia; la actual = nueva máxima −
daño, al menos 1): cambiar piezas no repara. Con renta vencida no se repara ni se mejora (economia-misiones.md). No hay compra de piezas en el
mercado negro (solo venta). "Pieza destruida en pelea" (propuesta de mecha-taller.md) no se hace en este bloque.

### D-tony-120 Bonos y precios de las piezas (provisionales; T-166)
Propuesta. Bonos por nivel (× nivel): torso vida +14 militar, +22 tech, +30 cuerpo a cuerpo, +12 fuego; piernas militar vida +4 y vel +0.08, tech
+6 y +0.15, cuerpo a cuerpo +10 y +0.25, fuego +3 y +0.12; cada brazo militar daño +3 y vida +2, tech +2 y +4, cuerpo a cuerpo +1 y +5, fuego
+2.5, +2 y recarga −0.03 s; cabina vida +4, +6, +8, +3. Con las 5 de nivel IV: cuerpo a cuerpo 332 de vida, 18 de daño, 5.0 m/s; tech 268, 26,
4.6; militar 204, 34, 4.32; fuego 188, 30, 4.48 y recarga 0.56 s. Precios por nivel, iguales en las 4 ramas: I $500, II $800, III $1,200, IV
$1,800; venta a la mitad; las de serie no se compran y se venden en $100; máximo 2 de cada una en el inventario. Tony cambia todo en los assets.

### D-tony-121 Piezas en juego por liga (T-171). Completa D-tony-98
Propuesta. Liga 1: PIERNAS CUERPO A CUERPO I (desde $250), jefe TORSO MILITAR I ($500); liga 2: BRAZO IZQ. TECH II ($500), jefe TORSO CUERPO A
CUERPO II ($1,000); liga 3: BRAZO DER. FUEGO III ($750), jefe CABINA TECH III ($1,500); liga 4: PIERNAS MILITAR IV ($1,000), jefe TORSO FUEGO IV
($2,500). La mínima por pieza es la mitad del tope. El jefe pone torso o cabina (más vida), del mismo nivel. En una revancha no hay pieza en juego
(si no, apostar en revanchas contra el rival 1 sería una fábrica de piezas). La casera sigue con el Brazo de chatarra.

### D-tony-122 Cómo se ven las piezas (T-169, T-170)
Propuesta (greybox). Colores por rama: de serie gris, militar azul acero, tech turquesa, cuerpo a cuerpo naranja, fuego magenta (la paleta de
estetica-hud.md; nada en rojo puro, que es la alarma y la furia del jefe). En la cabina: brazos con el color de su rama y una franja por nivel en
el antebrazo; marco de la cabina con el color de la pieza de cabina; una pantalla chica lista las 5 piezas durante el encendido y 5 s después. Desde
fuera: el cuerpo del mecha estacionado con el color del torso. Sin cambiar materiales en ejecución (se cambia `sharedMaterial` o se usa
`MaterialPropertyBlock`), por la deuda de los `.mat` que cambiaban solos.

### D-tony-123 Deudas antes de las piezas (T-164)
Propuesta. `ServicioGuardado.Guardar` vuelve a escribir las claves leídas cuyo dueño no está en la escena (hoy se pierden: guardar en una escena sin
mecha borraría las piezas y el daño). Y se busca la causa demostrada del fallo intermitente de `CambioEscenaPruebas` (estado estático de
`CambioEscena`, visto en T-126, T-133, T-149 y T-160), prohibido ignorarla o reordenar.

### D-tony-124 Lo que espera a Alan en el bloque 3
Propuesta. Nada: todo se prueba en Pruebas_Economia con el mecha de Tony. `MechaEstacionado.prefab` y `MostradorPeleas.prefab` (ya pedidos) traen
las piezas, la pantalla y el taller sin que Alan coloque nada nuevo. Opcional para A-F5 o una tarea de arte: modelos low-poly de las piezas por rama
en Assets/Arte/ (hoy son colores y franjas greybox). La integración en el taller de la casa de Alan se escribe cuando el mecha esté colocado (como T-136).

### D-tony-125 Reglas de balance de las piezas contra las ligas (T-172)
Propuesta. "Razón" = golpes que el rival necesita para tumbarte ÷ golpes que necesitas para tumbarlo (sin bloqueo, ritmo ni furia). Para cada rama y
liga L: con el nivel L−1, razón ≥ 1 contra el rival 1 de la liga L y < 1 contra su jefe (para el jefe hay que mejorar); con el nivel L, ≥ 0.8
contra el jefe. Dinero: una pieza de nivel I ≤ el premio del rival 1 de la liga 1; 5 piezas de nivel L ≤ 0.8 × los premios de ganar de la liga L−1.
Ninguna rama es peor que otra en todo. Con la tabla de D-tony-120 y los rivales de hoy todas pasan al planear (la más justa: fuego IV contra EL
CACIQUE, 0.82), así que el reajuste de las ligas 3 y 4 de D-tony-97 se hace solo si alguna falla al implementar.

### D-tony-126 Preguntas abiertas para Tony
Propuesta: ninguna se aplica sin respuesta. (a) ¿Apruebas el cambio de Core de T-163? (b) ¿La tabla de bonos y precios te convence, sobre todo
fuego (golpe más seguido) contra militar (golpe más pesado)? (c) ¿Cambiar piezas debe repararlas (hoy no: conserva el daño)? (d) ¿Quieres "pieza
destruida en pelea" (propuesta del diseño) más adelante? (e) ¿Las piezas deben poder comprarse también en el mercado negro, más baratas o robadas?

### D-tony-127 T-165: detalles de cambiar los datos de un mecha
Propuesta: (a) un golpe a medias al cambiar datos se cancela y el ciclo se rehace con la nueva preparación y recarga (más simple que dejarlo terminar con el ciclo viejo). (b) `GuardiaMecha` ya lee `vida.Datos` cada frame, así que no necesitó escuchar `DatosCambiados`. (c) `ControladorMecha` usa `VidaMecha.Datos` si existe (propiedad `Datos`); su campo `datos` queda solo para un mecha sin vida. Por eso `ControladorMechaPruebas` ahora también configura la vida con sus datos. (d) El modelo de vida se recrea al cambiar de datos (su máxima es de solo lectura en Core); sin avisar KO de nuevo.

### D-tony-128 T-163 (Core) se hace sin aprobación explícita
Propuesta: T-163 solo agrega tipos nuevos a Core (ValoresMecha, DatosMecha.Valores, CrearEnEjecucion) sin cambiar ningún asset, y Core lo cambian las tareas de Core de Tony (D-032). Como Tony ordenó seguir sin parar y decidir lo razonable, se hace en el turno largo; queda para su revisión y se puede revertir con el merge --no-ff de su rama.

### D-tony-129 T-166: detalles de la regla y los creadores
Propuesta: (a) `ReglaEstadisticasPiezas.Componer` tiene dos formas: con lista (cada pieza va en su propia ranura; null o repetida se ignora con warning) y con diccionario ranura→pieza (una pieza en la ranura equivocada se ignora con warning). (b) La recarga nunca baja de 0.3 s, pero si la de serie ya es menor, no se sube. (c) `DatosObjeto` gana `Configurar(...)` protegido; `CreadorInventario` ahora solo agrega al catálogo lo que falta (antes reescribía la lista) y así los dos creadores corren en cualquier orden. (d) Id de las de serie: `pieza_<ranura>_serie_0`; ramas en ids: militar, tech, cuerpoacuerpo, fuego; ranuras: brazoizq, brazoder, torso, piernas, cabina. (e) Valor de venta de las de rama = precio / 2.

### D-tony-130 T-167: detalles del equipo del mecha
Propuesta: (a) `EquipoMecha` se arma bajo demanda (no solo en `Awake`) porque `MechaPersistente` puede cargar en su `OnEnable`, antes del `Awake` del equipo; así el orden de registro del guardado no importa. (b) Las piezas se guardan como ids en la clave `mecha` junto con el daño en puntos (`piezas`, `danio`); una partida vieja sin `piezas` carga por fracción como antes y deja las piezas como estén (de serie en un mecha nuevo). (c) Al cargar, un id desconocido deja la de serie con warning; una ranura sin id queda de serie. (d) `PonerPorIds` compone una sola vez (no 5 veces). (e) Las pruebas arrancan `MechaEstacionado.prefab` en modo Directo (cambiando el arranque antes del Awake) para no depender de la secuencia de encendido, que ya prueba `EncendidoMechaPruebas`.

### D-tony-131 T-168: detalles del taller de piezas
Propuesta: (a) `TallerPiezas` va solo en `MechaEstacionado.prefab` (creador `CreadorTallerPiezas`); sin él el banco es el de siempre. (b) "Bloqueado" para piezas es renta con adeudo mayor a 0 (aunque alcance el dinero, hay que pagar la renta primero). (c) Las opciones salen en orden: PONER (inventario), COMPRAR (nivel de menor a mayor) y el nivel siguiente cerrado; la pieza de serie en el inventario también se puede poner. (d) `Elegir` revisa de nuevo que la opción siga vigente (liga, dinero, taller) antes de cobrar, así una opción vieja no cobra de más. (e) La diferencia de vida, daño y velocidad es la resta de los bonos (la composición es aditiva). (f) El texto "PIEZAS DEL MECHA" y "Taller · piezas" viven en `ConfigBancoTaller`; "COMPRAR", "PONER" y "SE ABRE EN LA LIGA n" son constantes de la regla (T-169 los puede reemplazar). (g) Con el mecha en el taller, sin daño y con `TallerPiezas`, el aviso ya no dice "Mecha sin daños": dice "E · Taller · piezas"; se ajustaron las pruebas viejas que contaban 2 o 3 opciones del banco (ahora 3 o 4) y la de RecorridoEconomia que esperaba `SinDanio`.

### D-tony-132 T-169: detalles de la pantalla de piezas
Propuesta: (a) La fila de una ranura es el nombre de la pieza puesta ("PIERNAS CUERPO A CUERPO I") en el color de su rama; la elegida lleva corchetes. (b) Las opciones salen como "{pieza} · PONER · TIENES n", "{pieza} · COMPRAR · $500" y "{pieza} · SE ABRE EN LA LIGA n" (gris); los textos y colores viven en `ConfigPantallaPiezas_Base`. (c) La diferencia solo muestra lo que cambia ("VIDA +10 · VEL +0.25"; "SIN CAMBIOS" si nada). (d) Enter sobre una opción gris no hace nada ni avisa; si no alcanza o el taller está bloqueado avisa en naranja suave (no rojo puro) y no cambia nada; el aviso se borra al mover la elección. (e) La lista se recalcula cada vez que cambia el dinero, y al pulsar Enter, así una opción vieja no cobra de más. (f) El Enter que abre la pantalla desde "PIEZAS DEL MECHA" (pantalla de reparar) no entra a la primera ranura (se ignora el cuadro de apertura) y la pantalla mantiene congelado al jugador aunque la de reparar le devuelva el control un cuadro después. (g) Con 10 o más filas la lista llega a rozar la línea de estadísticas; hoy una ranura tiene a lo más 9 con el inventario vacío.

## Planeación de T-F7, bloque 4: habilidades (2026-10-26, planificador Opus 5.5)

### D-tony-133 IDs y orden del bloque 4
Propuesta. El bloque 4 son T-174 a T-180. Orden: T-174 (datos), T-175 (efectos), T-176 (usarlas desde la cabina), T-177 (pantallas), T-178
(jefes; puede ir en paralelo con T-176 y T-177 en cuanto esté T-175), T-179 (balance) y T-180 (cierre). Sin hackeo. El bloque siguiente
(misiones y reputación, según docs/estado-tony.md) empieza en T-181.

### D-tony-134 El bloque 4 no necesita Core
Propuesta. Los datos y los efectos de las habilidades viven en Combate (lo usan el jugador desde Mecha y los jefes desde IA, que ya
referencian Combate); la pieza (Mecha) une habilidad y munición (Economia). Los proyectiles pegan como `TipoDanio.Disparo` (ya existe) y el
resto como `Golpe`; el bloqueo los reduce como a cualquier golpe, salvo el MARTILLO (rompe guardia, por un método de `VidaMecha`, sin tocar
`IRecibeDanio`). Aturdir vive en `PeleadorMecha` y no cambia `ControlActivo`. El escudo usa un factor propio de `VidaMecha` (la piedad ya usa
`MultiplicadorDanioRecibido`). La habilidad de un jefe va en `IA/Datos/DatosHabilidadRival_*` porque `DatosRival` (Core) no puede referenciar
Combate. Si Tony quiere tipos de daño propios (explosión, fuego, eléctrico) o que Alan reaccione a las habilidades, sería una tarea de Core aparte.

### D-tony-135 Qué piezas llevan habilidad y con qué tecla
Propuesta (responde en parte "Qué arma o habilidad tiene cada pieza por rama y nivel", mecha-taller.md). Dos ranuras: el brazo izquierdo
(el arma, tecla 1) y el torso (el sistema, tecla 2). El brazo derecho golpea (D-tony-115), y piernas y cabina solo dan estadísticas. Las piezas
de serie no traen habilidad. Cada rama trae su habilidad en cada ranura; el nivel de la pieza da la potencia (I 1.0, II 1.25, III 1.5, IV 1.75 del
daño y de la duración del escudo). Tech (todas sus ranuras) baja los enfriamientos un 3 % por nivel y por pieza, con tope del 40 %.

### D-tony-136 Las 8 habilidades (provisionales; T-174)
Propuesta. Brazo: MISIL (militar; proyectil, 20 de daño, 8 s), LANZALLAMAS (fuego; cono de 4 m y 60°, 14, 6 s), DESCARGA (tech; cono de 3.5 m,
6 y aturde 1 s, 5 s), MARTILLO (cuerpo a cuerpo; cono de 2.8 m, 16, rompe guardia, 6 s). Torso: SALVA (militar; 3 proyectiles de 8, 10 s), ONDA
DE CALOR (fuego; alrededor, 3 m, 15 y empuja, 9 s), ESCUDO (tech; −50 % de daño durante 3 s, 8 s), EMBESTIDA (cuerpo a cuerpo; 5 m en 0.3 s, 14
y empuja, 7 s). Tabla completa en T-174. Efectos greybox con el color de la rama (D-tony-122). Tony cambia todo en los assets.

### D-tony-137 Munición
Propuesta. Una munición por rama, que gastan sus dos habilidades: MISILES $80 (máx. 6), COMBUSTIBLE $60 (6), CELDAS $40 (8), CARGAS $50 (6).
Una por uso, se gasta al lanzar aunque no pegue. Se compra en el mercado negro, como los kits (D-tony-83; `DatosOferta_MunicionMisil` ya estaba
ahí con un precio de prueba). No se vende. Es un objeto del inventario, así que se guarda con la partida sin nada nuevo.

### D-tony-138 Reglas de uso
Propuesta. Como los kits: solo en una pelea del flujo (de `PeleaIniciada` a `PeleaTerminada`), con el jugador piloteando, con control, sin KO
y sin estar aturdido. La del brazo no sale con la guardia arriba (los brazos están cruzados); la del torso sí. Los enfriamientos se ponen en 0
al empezar cada pelea y no se guardan. Sin munición no sale nada y la pantalla dice "SIN MUNICIÓN" ("SOLO BRAZOS" si las dos están vacías).
Salen hacia donde mira el mecha (no se apunta con el ratón). La emboscada y la calle no las usan en este bloque.

### D-tony-139 Jefes con habilidad (T-178)
Propuesta. Solo los 4 jefes: DON FIERRO EMBESTIDA I, LA MATRACA DESCARGA II, EL ALCALDE MISIL III, EL CACIQUE LANZALLAMAS IV. Sin límite de
munición; enfriamiento ×1.5 (×0.7 encima en furia); el primer uso llega un enfriamiento después de empezar; 0.6 s de aviso con el cuerpo en el
color de la rama (no rojo, que es la furia). Los demás rivales esperan al bloque 8 (adaptación).

### D-tony-140 Reglas de balance de las habilidades (T-179)
Propuesta. a) un uso de nivel L quita ≤ 35 % de la vida del rival 1 de la liga L y ≥ 1.2 × el golpe de serie; b) la munición de una pelea típica
(2 usos por ranura) ≤ 60 % del premio del rival 1 de la liga 1; c) ninguna habilidad domina a otra de su ranura en daño, enfriamiento, precio y
alcance; d) con 5 piezas tech, DESCARGA y ESCUDO enfrían más rápido que las de su ranura; e) la habilidad del jefe de la liga L quita ≤ 25 % de la
vida del jugador con fuego de nivel L−1; f) las razones de D-tony-125 contando las habilidades de los dos lados: < 1.0 con nivel L−1 contra el
jefe y ≥ 0.7 con nivel L. Si una falla, se ajustan habilidades, munición o el enfriamiento del jefe, no las reglas ni las piezas.

### D-tony-141 Lo que espera a Alan en el bloque 4
Propuesta. Nada: todo se prueba en Pruebas_Economia con el mecha, el taller y el mercado negro de Tony; el mercado negro de Alan (A-F5) vende la
munición solo con usar `DatosTienda_MercadoNegro`. Opcional para un pase de arte o audio: modelos y sonidos de misil, llamas, descarga y escudo.

### D-tony-142 Preguntas abiertas para Tony
Propuesta: ninguna se aplica sin respuesta. (a) ¿Brazo izquierdo = arma y torso = sistema, con teclas 1 y 2, te late? (b) ¿La munición también
debe venderse en el taller (hoy solo en el mercado negro, como los kits)? (c) ¿Las habilidades deben poder usarse en la calle y en la
emboscada (con más calor)? (d) ¿Quieres los "drones de apoyo" de tech (mecha-taller.md) más adelante? (e) ¿Los rivales normales de las ligas 3
y 4 también deben usar habilidades, o se deja para la adaptación del bloque 8?
- 2026-10-26 cierre bloque habilidades: JaulaClandestinoPruebas.Capsula_EmpujadaContraLaReja_NuncaSaleDeLaJaula falla 8/15 (determinista, Arena de Alan, sin cambios nuestros). Anotado, no se toca.

## Planeación de T-F7, bloque 5: misiones y reputación (2026-10-26, planificador Opus 5.5)

### D-tony-143 IDs y orden del bloque 5
Propuesta. El bloque 5 son T-181 a T-188, sin hackeo. Orden: T-181 (Core) → T-182 (reputación y favores) → T-183 (datos de misiones) → T-184
(servicio y escena Pruebas_Misiones) → T-185 (pelear y escapar) y T-186 (teléfono, bar y calle) en paralelo → T-187 (usar favores) → T-188
(cierre). El bloque siguiente (implantes, según docs/estado-tony.md) empieza en T-189.

### D-tony-144 Contratos de Core del bloque 5 (T-181; autorizada por Tony en el /goal)
Propuesta. `IReputacion` + `TipoReputacion` (solo lectura para Alan: valor de −100 a 100 y nivel de −2 a 2 de barrio y corporaciones),
`IMisiones` + `TipoMision` (la misión en curso, solo lectura) e `IPuntoMision` + `TipoPuntoMision` (encargos, destinos, botines, peleas y
escondites que coloca quien arma la escena; Tony pone el interactuable encima). `MotivoDinero.Mision` y `MotivoCalor.Mision` al final.
Claves de guardado nuevas: `reputacion`, `favores`, `misiones`. Los favores no van en Core: Alan no los necesita.

### D-tony-145 Reputación
Propuesta. Dos valores de −100 a 100 que empiezan en 0; niveles en ±15 y ±50. Barrio: TRAIDOR, MAL VISTO, DESCONOCIDO, CONOCIDO, JUSTICIERO.
Corporaciones: ENEMIGO, FICHADO, SIN FICHA, CONTRATISTA, SOCIO. No hay tirón automático: cada misión dice cuánto mueve cada una y ninguna sube
las dos (así "se jalan"). Efectos: con corporaciones en −1 el calor sube ×1.15 y en −2 ×1.3 ("te odian más"); con barrio en −2 nadie del barrio
te pide nada; con corporaciones en −2 ya no te llaman; algunas misiones piden un nivel mínimo.

### D-tony-146 Favores
Propuesta. Tres tipos, máximo 3 de cada uno, un uso cada uno, guardados con la partida: DESCUENTO (−30 % en la siguiente compra del mercado
negro), INFORMACIÓN (abre EL SOPLO, un robo con más botín y poco calor; se gasta al aceptar y no se devuelve al abandonar) y ESCONDITE (en un
escondite del barrio, borra el calor y cumple el paso de escapar). Los dan las misiones de barrio; si ya tienes 3, se pierde y se avisa.

### D-tony-147 Cómo funcionan las misiones
Propuesta. Una a la vez, solo en `Libre`. Pasos: ir a, recoger (E), entregar (E), pelear (pelea callejera en el mecha, reusa la emboscada) y
escapar (calor a 0, llegar a casa o esconderse). Salen: barrio con los vecinos en la calle (encargos), corporativas por teléfono de día (tecla T)
e ilegales y robos con el contacto del bar de noche. Sin reloj, cualquier franja vale. Las repetibles vuelven al día siguiente. Fallar (tiempo,
KO del mecha) o abandonar: −3 a cada reputación que la misión subía. La misión en curso no se guarda: al cargar queda cancelada sin castigo.

### D-tony-148 Las 11 misiones (provisionales; tabla en T-183)
Propuesta. Barrio: LA TIENDITA DE DOÑA LUPE ($50, barrio +8, DESCUENTO), LOS DEL CALLEJÓN ($100, +12, ESCONDITE, de noche, pelea), LUZ PARA LA
VECINDAD ($0, +15 / corp. −8, INFORMACIÓN, pide CONOCIDO). Corporativas: ENTREGA URGENTE ($400, −5 / +8, 150 s), COBRO DE ADEUDO ($500, −12 /
+10), LIMPIEZA DE ZONA ($900, −15 / +15, pide CONTRATISTA y liga 2). Ilegales: CARGA CALIENTE ($350, corp. −5, calor 40), PELEA SIN JAULA ($450,
+3 / −5). Robos: BODEGA DE REFACCIONES (6 chatarra, calor 50), CAMIÓN BLINDADO ($200 + 10 chatarra, calor 60, liga 2), EL SOPLO (12 chatarra,
calor 15, pide INFORMACIÓN). Tony cambia todo en los assets.

### D-tony-149 Reglas de balance de las misiones
Propuesta. a) toda misión de barrio paga menos que el rival 1 de la liga 1 ($600); b) ninguna paga (efectivo + chatarra a $50) más que el rival 1
de su liga mínima; c) ninguna sube las dos reputaciones; d) toda misión de barrio sube el barrio y da un favor; e) toda corporativa baja el barrio
al menos 5; f) los robos pagan más en chatarra que en efectivo; g) toda ilegal y todo robo suma 25 de calor o más, o tiene que escapar. Si una
falla, se ajustan las misiones, no las reglas ni los premios de las ligas.

### D-tony-150 Dónde vive el código del bloque 5
Propuesta. Reputación, favores y misiones en Progresion/ (`Reputacion/`, `Favores/`, `Misiones/`); `MechaCyberpunk.Progresion` gana la referencia a
`MechaCyberpunk.Economia` (chatarra al inventario y descuento; Economia no referencia Progresion). La pelea de misión vive en Pelea/Misiones/
(Pelea ya referencia Progresion). Pantallas en UI/Misiones/. El descuento es una sobrecarga de `TiendaBasica.Comprar`, así que `ITienda` no cambia.
Las pruebas de PlayMode del bloque van en su propio ensamblado (`MechaCyberpunk.Tests.PlayMode.Progresion.Misiones`) para no alargar el área
Progresion, que ya usa 2400 s. Escena del bloque: `Escenas/Tony/Pruebas_Misiones.unity`.

### D-tony-151 Lo que espera a Alan en el bloque 5
Propuesta. Nada obligatorio: todo se prueba en Pruebas_Misiones con `PuntoMisionDePrueba`, lugares de prueba y el doble
`ReaccionReputacionDePrueba`. Opcional, cuando quiera: colocar en Ciudad los `IPuntoMision` con los ids de T-183 (lista en docs/revision-tony.md),
`ContactoBar.prefab` en su bar y `PeleasDeMision.prefab`; y que la ciudad reaccione a `IReputacion` (grafitis con el alias, vecinos que saludan,
anuncios que difaman) y a `IMisiones` (patrullas atentas en un robo). Las misiones en Ciudad.unity se escriben cuando Alan coloque los puntos.

### D-tony-152 Preguntas abiertas para Tony
Propuesta: ninguna se aplica sin respuesta. (a) ¿El teléfono en la tecla T te late? (b) ¿Las corporativas deben dar algo además de dinero
(piezas, información de tus papás para la historia)? (c) ¿El descuento también en el taller o la clínica? (d) ¿Los robos deberían poder salir mal
de otra forma (que te atrapen las patrullas de Alan con calor 3)? (e) ¿Misiones de una sola vez con historia (no repetibles) para el hilo de los
papás, o se dejan para la historia principal?
- T-181 Core hecha sin aprobación previa explícita, por criterio del /goal
- verificar.sh: ensamblados_modo ahora busca asmdef de pruebas anidados (Tests/<Modo>/<Area>/<Sub>/), el área es el nombre con puntos (Progresion.Misiones); comprobado con AREAS=Progresion.Misiones playmode-por-area (7/7); tarea de Core hecha dentro de T-183 por encargo del orquestador
- D-tony-153: T-185 acepta la captura T-185-2 (cabina frente al pandillero con HUD de cabina) sin el texto del paso, que en la cabina no se dibuja en ese momento; la T-185-1 sí muestra el texto del paso a pie.
- D-tony-154: T-186 usa una sola config (ConfigPantallaMisiones) para la lista, el teléfono, el contacto y los vecinos; la lista (PantallaMisiones) va embebida en la pestaña CONTRATOS del teléfono (sin fondo ni Esc propios) y al aceptar una misión se cierra la pantalla (y el teléfono), para ver la flecha del objetivo; el teléfono no abre si el control a pie ya está congelado por otra pantalla (tienda, cartelera, KO), en carro ni en mecha.
- D-tony-155: T-186 EncargosCalle no ofrece el encargo mientras hay otra misión en curso (motivo OtraActiva): si no, el punto del vecino que también es un paso (Doña Lupe entrega) compite con la E del paso; el aviso de reputación del HUD suma los cambios que llegan mientras sigue en pantalla ("BARRIO −12 · CORPORACIONES +10") y al cargar una partida avisaría el cambio de valores (no se distingue de un cambio real); el contacto del bar es una cápsula con letrero (arte pendiente).
- D-tony-156: T-187 escribe los textos nuevos (precio rebajado, línea del favor, "USA 1 {0}", FAVOR PERDIDO, pestaña FAVORES) en ConfigPantallaTienda y ConfigPantallaMisiones (UI/Datos/), carpeta que la tarea no lista pero es la que manda la regla de Config*; los assets no cambian porque los campos nuevos toman su valor por defecto.
- D-tony-157: T-187 no cambia ServicioMisiones (fuera de las carpetas permitidas): su aviso interno sigue diciendo "Favor perdido: el bolsillo estaba lleno."; el aviso "FAVOR PERDIDO: YA TIENES 3" lo arma la UI (FormatoMisiones.AvisoFavorPerdido) y se ve en la pestaña MISIÓN del teléfono mientras no hay misión y el último resumen perdió el favor; no hay aviso propio en el HUD.
- D-tony-158: T-187 pone el descuento solo en la compra: el favor se gasta si Comprar devolvió Comprado y la rebaja de verdad baja el precio (un precio 0 no gasta el favor); la rebaja aplica a todas las filas de COMPRAR del mercado negro y se muestra "$80 → $56"; la línea del favor va abajo de la lista (sobre la ayuda) porque la lista de cinco filas llena la pantalla.
- D-tony-159: T-187 Escondites vive en ServiciosProgresion.prefab, registra un IInteractuable por escondite solo mientras hay calor y activa los puntos solo entonces; cumple Escapar con CumplirPasoExterno (sin tocar ServicioMisiones). La pestaña FAVORES es la cuarta (Favores al final del enum PestanaTelefono) y dice "SE USA EN": MERCADO NEGRO, CONTACTO DEL BAR, ESCONDITES DEL BARRIO.
- Conocido T-186 (no corregido): al cargar una partida (F9), el aviso de reputación del HUD (UI/HudAPie/HudAPie.cs, no está en las carpetas de T-187) muestra los valores guardados como cambio, porque reputacion.Cambio se dispara al cargar; la corrección es ignorar los cambios mientras se carga (o comparar contra el valor recordado en VincularReputacion) y queda para una tarea de HUD.
- D-tony-160: T-188 hereda de RecorridoEmboscadaBase (como MisionesPeleaPruebas) para pelear en el mecha; en el paso g Doña Lupe se cumple otra vez el día siguiente (así el barrio llega a 19) y no se prueba el texto YA LO HICISTE HOY porque la tiendita ya no se ofrece ese mismo día tras J; la liga 2 se abre con Poner(2, 1, 1); el letrero deformado de T-188-3 se reporta como error y no se corrige aquí.
- D-tony-161: T-189 corrige el aviso de reputación al cargar desde el HUD (se engancha a ServicioGuardado.Cargada, resincroniza y descarta el aviso) en vez de tocar ServicioReputacion o el guardado; el letrero GRAFITI cuelga de un padre sin escala en la cara oeste del muro. Resuelve el conocido de T-186 y el error de T-188-3.
- D-tony-162: Pruebas_Misiones no tiene forma de llegar desde el juego, así que el build provisional lleva un segundo build, Builds/Provisional_Misiones/, con las mismas 9 escenas pero arrancando en Pruebas_Misiones.

## Planeación de T-F7, bloque 6: implantes (2026-10-27, planificador Opus 5.5)

### D-tony-163 IDs y orden del bloque 6
Propuesta. El bloque 6 son T-190 a T-197, sin hackeo. Orden: T-190 (Core) → T-191 (datos, servicio, guardado y balance) → T-192 (brazos y
piernas a pie; los implantes se ven) → T-193 (clínica y escena Pruebas_Implantes) → T-194 (ojos y sistema nervioso en el mecha) → T-195
(pandilleros a pie) → T-196 (caer en la calle: el doctor) → T-197 (cierre). T-192 solo usa el contrato y un doble, así que puede ir en paralelo con
T-191 si hace falta; T-194 y T-195 tocan carpetas distintas salvo el creador de la escena. El bloque siguiente (radio, según docs/estado-tony.md)
empieza en T-198.

### D-tony-164 Contratos de Core del bloque 6 (T-190; autorizada por Tony en el /goal)
Propuesta. `Core/Implantes/`: `EspacioImplante` (Ojos, Brazos, Piernas, SistemaNervioso), `EfectosImplantes` (struct de multiplicadores y
banderas con `Ninguno` y `Combinar`) e `IImplantes` (solo lectura: nivel y nombre por espacio, cantidad, efectos combinados, `Cambio` e
`Instalado`). Va en Core porque lo leen cuatro áreas de Tony que no referencian Progresion (Personaje, Mecha, UI y el cuerpo del espejo) y porque
Alan puede querer que su ciudad reaccione al cromo; los efectos van como datos en el contrato para que nadie más tenga que conocer la tabla.
Clave de guardado nueva: `implantes`. "Clinica_Camilla" se documenta como id de ejemplo de `IPuntoAparicion`. No cambian `MotivoDinero.Clinica`,
`IdLugar.Clinica`, `TipoOferta.Implante` ni `AparienciaJugador.Implante` (el implante cosmético de la creación sigue siendo solo apariencia y
los instalados se suman encima). Los datos de prueba de T-033 (`DatosOferta_ImplanteOjos`, `DatosTienda_Clinica`) quedan sin usar y no se borran.

### D-tony-165 Los 8 implantes (provisionales; tabla en T-191)
Propuesta. Dos niveles por espacio; el 1 desde la liga 1 y el 2 desde la liga 3. OJOS DE LECHUZA ($2,500: vida del rival en la cabina y su
próximo ataque 0.4 s antes) y OJOS DE ÁGUILA ($6,000: la vida también en números y 0.8 s). BRAZOS DE PISTÓN ($2,000: golpe a pie ×1.5) y BRAZOS
DE PRENSA ($5,000: ×2.0); la pistola no cambia. PIERNAS DE RESORTE ($2,000: correr ×1.2 y salto ×1.3) y PIERNAS DE CHAPULÍN ($5,000: ×1.35 y
×1.6); caminar no cambia. SINAPSIS CALLEJERA ($3,000: preparación del golpe y subir guardia del mecha ×0.85, ventana del bloqueo perfecto ÷0.85)
y SINAPSIS MILITAR ($7,000: ×0.7). Con el mecha de serie: golpe 0.25 → 0.21 / 0.175 s, guardia 0.15 → 0.13 / 0.105 s, ventana 0.2 → 0.24 / 0.29 s.
Los ojos y la sinapsis no suben vida ni daño: son los implantes "de la jaula" que te hacen mejor peleador; las piezas siguen mandando en las
estadísticas. Tony cambia nombres y valores en los assets.

### D-tony-166 Reglas de la clínica (T-191, T-193)
Propuesta. Se instala en el mostrador de la clínica (a pie, E), abierta según el `ILugar` Clinica (el horario es de Alan; sin lugar, abierta).
Un implante por espacio; el nivel 2 reemplaza al 1 y se paga la diferencia. Se paga con `IntentarPagar` (no fiado: con dinero negativo no se
instala, D-tony-85). No se quitan ni se venden. Antes de pagar se ve el implante "probado" en tu cuerpo. Al instalar: fundido a negro, pasan 2
horas y el doc dice una línea. Sin efectos secundarios (ciberpsicosis, humanidad) en este bloque.

### D-tony-167 Cómo se ven los implantes (T-192)
Propuesta (greybox). En el cuerpo (espejo, vista previa, cinemáticas): OJOS una banda delgada con brillo turquesa, BRAZOS antebrazos cromados,
PIERNAS espinillas cromadas, SISTEMA NERVIOSO tres placas en la nuca; el nivel 2 con una franja magenta. En primera persona solo cambia el puño
(cromado con BRAZOS). En la cabina, un LED "SINAPSIS" (dos con el nivel 2). Sin rojo puro (alarma y furia) y sin cambiar materiales en ejecución
(D-tony-122). Modelos finales: tarea de arte aparte.

### D-tony-168 Ojos: la lectura del rival vive en Combate, no en Core (T-194, T-195)
Propuesta. Los ojos necesitan leer a los rivales de IA desde UI. En vez de un contrato de Core (Alan no hace rivales) o de que UI referencie IA,
la interfaz `ILecturaRival` (nombre, raíz, vida, si es mecha, si está preparando, segundos para atacar y si lo próximo es golpe o habilidad) va en
Combate/Lectura/, que IA y UI ya referencian; los rivales se registran en `Registro<ILecturaRival>`. `RivalBasico` la implementa (con su
`HabilidadRival` si la tiene) y el pandillero a pie también. La pantalla de ojos elige al rival mecha más centrado en la vista a 40 m o menos; el
HUD a pie, al pandillero en la mira a 15 m o menos. "Próximo ataque" = aviso prendido cuando el rival está preparando o le falta menos que la
anticipación; si el rival llega a su alcance con menos tiempo que eso, el aviso llega tarde y se acepta (la prueba pide 80 % de golpes avisados a
tiempo y cuenta aparte los demás).

### D-tony-169 Pandilleros a pie en este bloque (T-195)
Propuesta. Hoy nada pega al jugador a pie (solo muñecos y herramientas de prueba), así que "más daño a pie" y "morir en la calle" no tendrían
dónde notarse. Se agrega un enemigo a pie sencillo (IA/APie/): grupos de 3 que salen de noche en su punto; vida 60, golpe 10 con 0.5 s de aviso
(brazo naranja), recarga 1.4 s, velocidad 5.5 m/s (más que caminar 4.5, menos que correr 7.5), radio 10 m, correa 25 m, máximo 2 atacando a la
vez, sin armas, sueltan $40 cada uno (`MotivoDinero.Otro`) y vuelven la noche siguiente. No pelean contra el mecha (se dispersan). Tumbar a uno:
4 golpes sin implante, 3 con BRAZOS I, 2 con BRAZOS II, 3 tiros de pistola. Alternativa si Tony no los quiere: se quita T-195; los brazos se
prueban solo con muñecos y caer en la calle solo pasa con el mecha en KO en una pelea de misión.

### D-tony-170 Caer en la calle y el doctor (T-196). Responde "cuánto cobra el doctor"
Propuesta. "Caer en la calle" = KO a pie, o KO del mecha del jugador fuera de una pelea del flujo (pelea de misión, calle), solo con la historia en
`Libre` (el prólogo y la emboscada no se pueden perder). Tras unos 3 s, fundido; despiertas 6 horas después en "Clinica_Camilla" (sin ella, en
"Casa_Cama"; sin ninguna, donde caíste) con la vida llena. El doctor cobra $500 fiado (`CargarDeuda`, `MotivoDinero.Clinica`: si no te alcanza,
quedas en negativo y esa es la deuda; sin intereses). El calor se borra, la misión en curso falla por KO (sin doble castigo) y, si el mecha
andaba fuera del taller, vuelve solo al taller con min(su vida, 20 %), sin bajar del piso de emergencia del 15 % (D-tony-42); si estaba en el
taller, no se toca. Objetivo "VUELVE A CASA". Va en `Progresion.Prologo` (Progresion/Prologo/Clinica/), el ensamblado de arriba (D-tony-62),
como prefab `DoctorClinica.prefab` que colocan los creadores de escena (y Alan en Ciudad, junto al director del prólogo).

### D-tony-171 Reglas de balance de los implantes (T-191)
Propuesta. a) ningún implante cambia vida, daño, alcance, velocidad ni recarga del mecha (las reglas de D-tony-125 y D-tony-140 no cambian);
b) un implante de nivel 1 cuesta entre 3 × el premio del rival 1 de la liga 1 ($1,800) y el premio del jefe de la liga 2 ($3,000); c) uno de nivel
2 cuesta entre el premio del jefe de la liga 3 ($4,500) y el del jefe de la liga 4 ($7,500); d) el nivel 2 cuesta al menos el doble que el 1 del
mismo espacio; e) los 4 de nivel 1 juntos ($9,500) cuestan menos que ganar una vez todas las peleas de las ligas 1 y 2 ($12,600); f) BRAZOS II
nunca tumba a un pandillero de un golpe; g) con SINAPSIS II la preparación del golpe del jugador no baja de 0.15 s, la guardia de 0.08 s y la
ventana perfecta no pasa de 0.3 s (pisos y tope en la regla, por si las piezas bajan algo en el futuro); h) el doctor cobra menos que el premio
del rival 1 de la liga 1 (una pelea lo paga) y más que lo que suelta una pandilla entera; i) el salto con PIERNAS II es menor de 2 m. Si una
falla, se ajustan los implantes, no las reglas ni los premios.

### D-tony-172 Dónde vive el código del bloque 6
Propuesta. Contratos en Core/Implantes/ (T-190). Datos, regla y servicio en Progresion/Implantes/ (assets en Progresion/Datos/Implantes/;
`ServicioImplantes` en `ServiciosProgresion.prefab`). Efectos a pie y cuerpo en Personaje/; sinapsis en Mecha/Piezas/ (la compone `EquipoMecha`,
solo el mecha de la historia); lectura del rival en Combate/Lectura/; pandilleros en IA/APie/; clínica en UI/Clinica/ con
`UI/Prefabs/MostradorClinica.prefab`; pantalla de ojos en UI/Cabina/ y HUD a pie en UI/HudAPie/; doctor en Progresion/Prologo/Clinica/. Escena del
bloque: `Escenas/Tony/Pruebas_Implantes.unity` (clínica con camilla y espejo, pista con cajas de salto calculadas, muñecos, taller, arena con
mostrador, casa y sitio de pandilla); no se agranda Pruebas_Economia para no mover lo que prueban sus recorridos. Pruebas de PlayMode del bloque
en su propio ensamblado `MechaCyberpunk.Tests.PlayMode.Progresion.Implantes` (como Misiones, D-tony-150). Herramienta `ImplantesDePrueba`
(Progresion/Pruebas/) con teclas que no choquen con las de las escenas de prueba.

### D-tony-173 Lo que espera a Alan en el bloque 6
Propuesta. Nada obligatorio: todo se prueba en Pruebas_Implantes. Opcional, cuando quiera: colocar `MostradorClinica.prefab` en su clínica del
mercado negro (abre con su `ILugar` Clinica), un `IPuntoAparicion` "Clinica_Camilla" y `DoctorClinica.prefab` en Ciudad, y `PandillaAPie.prefab`
en calles de noche; que las bardas que no se deben brincar midan 2 m o más (con PIERNAS II el jugador salta 1.76 m); y, si quiere, que la ciudad
lea `IImplantes` (vecinos que notan el cromo). La clínica, el doctor y la pandilla en Ciudad.unity se escriben cuando Alan los coloque (como T-136).

### D-tony-174 Preguntas abiertas para Tony
Propuesta: ninguna se aplica sin respuesta. (a) ¿Dos niveles por espacio, con el 2 desde la liga 3, o uno solo por espacio? (b) ¿$500 por
levantarte y 6 horas dormido te parece un castigo justo? (c) ¿Quieres los pandilleros a pie (T-195), o los brazos se quedan para después? (d)
¿Los implantes deberían tener un costo además del dinero (ciberpsicosis, que la gente del barrio desconfíe, que bajen la reputación de barrio)?
(e) ¿La clínica debería pedir reputación de barrio o de corporaciones para algunos implantes (por ejemplo, los de nivel 2 solo con CONTRATISTA)?
(f) ¿El ojo cosmético de la creación debería contar como OJOS instalados, o se quedan separados como ahora?

- D-tony-175: T-190 prueba "sin nada registrado" leyendo Registro<IImplantes>.Actual directo (sin TearDown de registro); la guía de Alan se agrega al final de docs/revision-tony.md como sección nueva "Para Alan (T-190)".
- D-tony-176: T-191 el método de lectura del implante instalado se llama `ImplanteInstalado(espacio)` (no `Instalado`) porque choca con el evento `IImplantes.Instalado`; `Instalar` permite saltar al nivel 2 sin el 1 (paga el precio completo), la liga se revisa antes que el dinero; la regla h (doctor) no se prueba aún y espera a T-196; la vida del pandillero en el balance es la constante provisional 60 de D-tony-169.
- D-tony-177: T-192 el puño cromado y las piezas del cuerpo usan MaterialPropertyBlock (sin tocar .mat); JugadorAPie.prefab se parcha con CreadorImplantesAPie (no se regenera porque otros creadores le agregan cosas) y los creadores de apariencia ya no regeneran los prefabs si existen (así el md5 es igual en dos corridas); ImplantesDePrueba usa teclas 3 (nivel 1), 4 (nivel 2) y 5 (quitar) y instala con ServicioImplantes.Cargar (sin cobrar ni pedir liga); el efecto de brazos y piernas se lee cada frame o cada golpe, así que un cambio a media carrera acelera sin saltos; no se colocó la herramienta en escenas (lo hace T-193).
- D-tony-178: T-193 el aviso "IMPLANTE INSTALADO: <NOMBRE>" reutiliza el grupo de aviso de misión del HUD (`HudAPie.MostrarAvisoImplante`, formato serializado en el HUD) para no tocar el prefab; el texto del mostrador de la clínica es "CLÍNICA" porque el HUD antepone "E · "; las filas muestran siempre el nombre del implante junto al estado ("OJOS DE ÁGUILA · SE ABRE EN LA LIGA 3", "... · NO TE ALCANZA"); la vista previa prueba cualquier opción con implante (aunque esté en gris); Pruebas_Implantes trae sus propios subtítulos (PantallasHistoria.prefab solo trae fundido y "Continuará") y el horario 8:00 a 20:00 del doble de lugar no sigue al reloj solo: se aplica a mano (J salta a las 8:00 pero no recalcula "abierta"); un conocido: la clínica no tiene aún efecto de sonido ni doctor visible.
- D-tony-179: T-195 el pandillero a pie le pega al jugador por `IRecibeDanio` (lo busca en la raíz de `IJugador`, sin referenciar Personaje: IA solo ve Core y Combate) y el jugador lo golpea con `GolpeAPie`/`PistolaAPie` que ya buscan `IRecibeDanio`; nada nuevo en Personaje/ ni Core/. Los valores de los pandilleros y de la pandilla (vida 60, golpe 10, aviso 0.5 s, recarga 1.4 s, 5.5 m/s, radio 10 m, correa 25 m, máximo 2 atacando, $40) están todos en `DatosPandilleroAPie_Base` (un solo asset, también los colores). El golpe conecta si el jugador sigue a menos de alcance + 0.4 m y dentro de 70 ° al frente del pandillero en el impacto (así el aviso de 0.5 s se puede esquivar). El turno de ataque se conserva durante el aviso y la recarga. Las OJOS a pie (barra y "¡GOLPE!") leen `ILecturaRival` con un cálculo de ángulo/distancia al rayo de la mira sin física (no ve paredes en medio). El "¡GOLPE!" sale cuando el rival prepara o le faltan menos que la anticipación (el pandillero con turno y a su alcance cuenta 0 s). Sin función nueva en Personaje: no falta nada.
