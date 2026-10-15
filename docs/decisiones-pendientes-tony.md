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
