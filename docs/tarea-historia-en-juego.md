# T10 · El juego sigue la historia nueva (con huecos para las cinemáticas de IA)

Fuente de verdad: la historia (historia-cybercity-v1.md, versión 2) y el guion del tráiler (guion-trailer-cybercity.md, versión 2) del proyecto
VIDEOJUEGOS. La ciudad se llama CYBER CITY en pantalla (antes MEGALOPOLIS). Ya NO hay aliados: EL CHATO, LA GÜERA y EL TOPO se apagan de la
historia (su código se queda, desconectado, igual que se hizo con el mecha). No se borra contenido viejo: se desconecta.
Animaciones: solo clips que ya existen o de Mixamo; nada animado a mano.

## 10.1 Sistema de "cinemática pendiente" (lo primero)
Un componente reutilizable `CinematicaPendiente` (Progresion/Historia) que la historia llama en cada momento donde irá un video de IA:
1. Fundido a negro (0.5 s). Pantalla negra con, al centro, en el estilo de la UI del juego: "CINEMÁTICA · <NOMBRE>", debajo, más chico,
   una línea con lo que pasa (texto de la tabla) y una barra de progreso. Abajo a la derecha, pequeño: "ENTER: SALTAR".
2. Dura exactamente la duración prevista del video (tabla); al terminar o al saltar, fundido de vuelta al juego.
3. Listo para cambiarse por el video real sin tocar código: cada cinemática es un ScriptableObject `DatosCinematica` (id, nombre, texto, duración
   y un `VideoClip` opcional). Si el VideoClip está puesto, se reproduce el video a pantalla completa con su audio (VideoPlayer) en lugar de la
   pantalla negra. Carpeta: Assets/_Proyecto/Progresion/Historia/Cinematicas/.
4. Durante la cinemática: jugador sin control, IA y tráfico cercano en pausa, HUD oculto, música bajada.
5. La cinemática es el momento para acomodar el mundo sin que el jugador lo vea: mover al jugador al punto siguiente, poner o quitar NPCs y
   limpiar la policía (10.3).

## 10.2 El recorrido de la historia, en orden
| # | Momento | Tipo | Qué hace el juego |
| --- | --- | --- | --- |
| 1 | C01 EL RECUERDO (9 s) | Cinemática | "Un niño corre en el campo con su hermano. Su mamá los llama. Despierta." Al iniciar NUEVA PARTIDA, antes de todo |
| 2 | Despierta en su casa | Juego | Aparece en su casa de Suburbios, de noche. Objetivo: "SAL A LA CALLE" |
| 3 | Lo molestan | Juego guionado | Al caminar por Suburbios, dos SKULLS normales le salen al paso, lo insultan, lo empujan y lo tiran al suelo. El jugador NO puede atacar (puños y armas desactivados, aviso discreto "NO PUEDES CON ELLOS... TODAVÍA"). Se ríen y se van; él se levanta (clip de levantarse). Usar las voces de T5 si ya están |
| 4 | C02 EL MALETÍN (23 s) | Cinemática | "Los SKULLS dejan un maletín de NOVA para asaltar a un vendedor. Él lo roba sin que lo vean. En su cuarto lo abre: dinero, una llave y un suero. Por la ventana oye que esta noche nadie cuidará la ranura del muro." |
| 5 | Ir a la ranura | Juego | Noche, lluvia. Objetivo: "VE A LA RANURA DEL MURO": un tramo del muro cerca de su casa con una grieta (asset si existe; si no, un marcador). Los SKULLS de su zona no están (fueron a buscar el maletín) |
| 6 | C03 LA DECISIÓN (22 s) | Cinemática | "Está a punto de escapar. Oye gritos: los SKULLS golpean a la gente del barrio por el maletín. Se da la vuelta." Después, la ranura queda cerrada para el resto del juego |
| 7 | El dinero | Juego | Recibe Ƀ20,000 (aviso "MALETÍN: Ƀ20,000"). Objetivo: "VE CON EL DOC" |
| 8 | C04 EL SUERO (5 s) | Cinemática | "El Doc le inyecta el suero: ahora aguanta cualquier mejora." Luego se abre la clínica de implantes y el jugador instala su primera mejora con la pantalla de mejoras |
| 9 | Armas (mercado y TINO) | Juego | T44: objetivo "COMPRA UN ARMA EN EL MERCADO". Al comprar, el vendedor dice (subtítulo) "¿Por qué tan baratas? Porque son de aquí. Conozco a un viejo que te las deja de otro planeta, pero cobra caro. ¿Traes dinero?" y sale el aviso de la rueda de armas. Objetivo "VE CON TINO AL CIC"; al hablarle TINO dice "Acércate, te mejoramos el arma." |
| 10 | C05 EL INVENTOR (4 s) | Cinemática | Entra 3 s después de la frase de TINO: "El Inventor le mejora la pistola." Al terminar, pistola mejorada |
| 11 | C06 LA CHICA (8 s) | Cinemática | Al SALIR de la tienda de armas o del CIC la primera vez: "Choca con una chica que lleva a su abuela; la ciudad ya no le da medicinas." |
| 12 | Guerra con los SKULLS | Juego | T44: antes, paso del hambre: "Tengo hambre..." y objetivo "COMPRA ALGO EN LA MÁQUINA" (la máquina más cercana; al comprar, sigue). Objetivo: "LIBERA EL TIANGUIS": vencer a los SKULLS del Tianguis y a su jefe (la conquista que ya existe). PELEA PROTEGIDA |
| 13 | Guerra con los SNAKES | Juego | Objetivo: "VE A NEÓN ALTO": vencer a los SNAKES y a su jefe. PELEA PROTEGIDA |
| 14 | C07 EL HACKER (6 s) | Cinemática | "El Hacker conecta la llave: las pandillas son los carceleros de NOVA, y NOVA graba a todos desde el principio." |
| 15 | Llegan los robots | Juego | Robots de NOVA bajan del cielo (cápsulas de T7) y patrullan. Objetivo: "VE A LA TORRE DE NOVA". Protegida contra la policía normal (NOVA sí ataca) |
| 16 | C08 EL HERMANO (6 s) | Cinemática | "En lo alto de la torre lo espera el jefe de NOVA: su hermano." |
| 17 | Jefe final | Juego | Pelea contra E14 (el hermano). PELEA PROTEGIDA |
| 18 | C09 LAS PUERTAS (4 s) | Cinemática | "Abre las puertas de la torre con la llave. Todo se vuelve blanco." |
| 19 | Fin | Pantalla | Negro, "CYBER CITY" y "¿YA SALISTE?", créditos y vuelta al título. CONTINUAR = juego libre |

La demo de 15 minutos se corta después del paso 14 (C07 EL HACKER) con "CONTINUARÁ".

## 10.3 Peleas protegidas (sin policía en las misiones de historia)
En los pasos 3, 12, 13, 15 (solo la policía) y 17, la policía, los drones de calor y los refuerzos de NOVA por estrellas NO entran:
1. Al empezar la misión (de preferencia durante la cinemática anterior, donde el jugador no ve) el calor se congela en 0 y no sube mientras dure.
2. Lo que ya estaba en el mapa NO desaparece de golpe: las patrullas cercanas apagan la torreta y se alejan manejando fuera de la zona, los
   policías a pie se suben a su patrulla o se van caminando, los drones suben y se alejan; se borran solo cuando ya están fuera de la vista
   (o se limpian durante la cinemática). Usar el director de respuesta de T8 con un modo "MisionHistoria".
3. Al terminar la misión, el calor vuelve a funcionar normal (desde 0).
4. En los pasos 12 y 13, la otra pandilla tampoco se mete en la pelea.

## 10.4 Comprobar
En Play, partida nueva de principio a fin (o con un menú de depuración para saltar a cada paso): cada cinemática pendiente sale con su nombre
y duración exacta y se puede saltar; el paso 3 no deja atacar y termina con el jugador en el suelo; con 3 estrellas encima, al entrar a la misión
del Tianguis las patrullas se van manejando (ni aparecen ni desaparecen a la vista) y el calor no sube en toda la pelea; un DatosCinematica con
un VideoClip de prueba reproduce el video en vez de la pantalla negra. Capturas de cada paso.

## 10.5 T44 (T-1662): guía, orden y máquinas
- Orden: maletín (C02) → ranura → C03 LA DECISIÓN → recién entonces el dinero (Ƀ20,000, `EntrarDinero`) → Doc (suero, implantes) → mercado de armas (compras; frase del vendedor) → TINO en el CIC ("Acércate, te mejoramos el arma", luego C05) → al salir, C06 LA CHICA → hambre (máquina) → guerras. El dinero ya llega después de la decisión: no hizo falta cambiarlo.
- Guía: `GuiaHistoria` dibuja una línea turquesa en el piso hasta el objetivo (ruta de calles como el minimapa; si no hay, NavMesh; si no, una recta corta de 25 m). Se recalcula cada 0.5 s y se oculta sin objetivo, en cinemáticas, en pausa o a menos de 4 m. El minimapa ya trazaba su línea hacia el objetivo (T-632).
- Rueda de armas: se abre con el botón central del ratón (la RUEDA, acción "Rueda"), no con otra tecla. El aviso sale al comprar el arma (o al llegar a la guerra, si no se vio) y se quita al abrir la rueda.
- Máquinas expendedoras: vuelven a vender consumibles (`ActivarMaquinasT1662.Aplicar`; antes eran de decorado por T-1529). La historia usa `Expendedora.Compra` para saber que compraste.
- Depuración: `DirectorRecorrido.DepurarHistoria()` y `DirectorRecorrido.DepurarExpendedora()`.
