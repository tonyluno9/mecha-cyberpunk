# Prueba de Tony 6-nov 15:00 (build de las 14:xx) — tareas para mientras Tony no está

Corte 12-nov. Lo de docs/playtest-06nov.md que no esté hecho sigue vigente (sobre todo la sesión 1: vehículos, policía, SKULLS,
historia del dinero). Esta lista se suma. Cada sesión además juega su parte y arregla por su cuenta lo roto, encimado, flotante,
fuera de escala o hecho con primitivos. Assets nuevos YA están (meterlos primero): imageassets/modelos3d/ W02.zip, W03.zip,
W04.zip, C01.zip (policía), V09.zip (taxi), V10.zip (pickup); imágenes H01-H04, F01, D01-D02, M01-M02. V11 no hay: omitirla.
Si C01 no trae esqueleto, déjalo anotado para que Tony lo pase por Mixamo y usa el reemplazo.

Le gustó y NO se toca: pantalla de carga, salir por el callejón, puerta que se cierra sola, matar y caer de los enemigos, caminar sin
arma visible, correr y el cambio caminar/correr, el taller del mecánico por dentro con el carro, la ciudad y la calle, el tráfico en
general, la zona del Doc, el menú de mejora de armas (animación del cargador increíble), la moto (cómo te ves), escapar de la policía
cuando te roban una patrulla, el tianguis sin cartel y de día, Nova y sus letreros, los edificios lejanos, dormir y despertar.

---
## SESIÓN 1 — Jugador, armas, vehículos, policía, enemigos, misión (carpeta mecha-s1)

### Primero: meter assets
- W02 escopeta, W03 fusil, W04 francotirador con el importador (en la mano, en el piso, en el HUD con H02-H04, en el menú de Tino).
- C01 policía en los policías a pie.

### Jugador y armas
- Al apuntar la pistola se ve volteada (apunta hacia el jugador) y chueca: corregir la orientación en la pose de apuntar y de disparo
  (el cañón hacia la mira). Revisar con las 4 armas.
- Cámara al apuntar: el MISMO encuadre que al caminar (personaje centrado), solo con zoom. Que no suba la cámara (deshacer la
  subida de T-672, dejar el centrado).
- Agachado: usar animación real de agachado (Mixamo: los clips de Cobertura_* / Rifle Crouch Walk que ya están, o anótala en
  docs/animaciones-faltan.md) en vez de la pose procedural; sin arma no debe verse pose de arma. Con Shift agachado los pies deben
  moverse más rápido, no solo deslizarse. La rodada: más lenta, natural, sin hundirse en el suelo, termina reacomodándose agachado.
- Escudo de energía del jugador: una barra que absorbe daño antes que la vida y se recarga; mejorable (con el Doc).
- Máquinas expendedoras: comprar objetos. Bebida energética (cura y 5 s sin recibir daño), papitas (cura poco), y 2–3 más.
  Se guardan en el inventario y se usan con una tecla.
- Enemigos sueltan su arma y su dinero al morir.
- Disparar desde el carro y la moto (apuntar hacia los lados mientras manejas).

### Vehículos
- En el carro el jugador queda sentado muy abajo: subirlo al asiento (que se vea bien la cabeza por la ventana).
- El aliado no se sube al carro: que se suba por la puerta con animación y se baje contigo.
- Moto: el jugador da saltitos al avanzar; debe ir estático en el asiento. La aceleración de las motos está pasada: calibrar;
  la moto de reparto más lenta que las otras.
- Los carros del tráfico que chocan deben quedarse detenidos (y el conductor reacciona), no seguir como si nada.
- Carros que aparecen y desaparecen de la nada: que nazcan y se borren solo fuera de la vista, con un ciclo normal. Carros
  flotando: pegarlos al suelo.
- La explosión daña lo que está alrededor (una patrulla al lado no puede quedar intacta).
- Quemarse: el fuego del jugador se apaga solo en unos segundos o al rodar/salir del área; no debe dañarte sin fin.
- Quitar el efecto de luces de alerta en pantalla al robar un carro (feo) y el sonido de "carro robado".
- Las patrullas aún se mueven raro: física real de carro, sin girar sobre su eje, sólidas (no se atraviesan). Los policías se
  bajan y disparan. Una patrulla que te ve llega en un milisegundo: que llegue manejando con tiempo real.
- Dron de policía: dispara de verdad con trazos visibles y sonido de bala que pasa rozando.

### Enemigos
- Las balas de los enemigos atraviesan objetos: sin línea de vista no disparan y las balas chocan con todo.
- SKULLS en cobertura: hoy se cubren parados y raro. Cobertura agachados detrás de algo sólido, se asoman, disparan, se esconden.
- Jefe de Nova: no hace daño y se comporta raro. Que golpee y haga daño de verdad, con patrón claro.

### Misión / tutorial
- Siempre un objetivo claro en el HUD y en el mapa. Hoy el jugador se pierde ("vuelve a casa" sin saber para qué).
- La historia del dinero empieza como cinemática obligatoria al salir de casa: ves que los SKULLS se llevan a alguien, tienes que
  ayudarlo; de ahí sale el dinero con el que te curas, mejoras el arma y compras implantes. Revisar todo el tutorial de punta a punta:
  hoy quedó trabado.
- Mercado de armas: tienda donde compras escopeta, fusil, francotirador y balas.

---
## SESIÓN 2 — Mundo, interiores, menús (carpeta principal)

### Primero: meter assets
- V09 taxi y V10 pickup en el tráfico.
- Huecos de bala: a veces salen sin textura y a veces con un círculo alrededor. Que el fondo blanco de D01/D02 se recorte bien
  (alfa) para que quede solo el hueco, siempre con textura.
- Ícono holográfico del arma en el HUD sin el cuadro de fondo (transparente).

### Casa
- En el cuarto hay una textura de cuadros encimada en el piso (parpadeo): quitarla.
- El medidor (cosa en la pared al salir del cuarto) se ve pequeño: escalar.
- Espejo: el jugador camina al espejo y se ve SU personaje reflejado (no un muñeco).
- Junto a la casa: quitar el banco de trabajo, los cables y el tanque de cargar carros que no tienen sentido.
- La entrada y el edificio de la casa se ven raros: mejorarlos con el kit (llega modelo de entrada; dejar el hueco listo).

### Ciudad
- **Luz de los postes (otra vez)**: abre el modelo del poste, encuentra la malla de la lámpara (la cabeza) por sus vértices y pon la
  fuente de luz exactamente debajo de la lámpara, apuntando al piso. Captura de cerca de un poste de noche mostrando que la luz sale
  de la lámpara. Revisar todos los tipos de poste.
- Quitar todo lo que inventaste y Tony no hizo: tejados, balcones, escaleras, muebles y la "mini ciudad" incoherente de la cuadra de
  al lado; las banquetas hechas como modelo (usar una textura o asset de banqueta: si no hay, anotarlo para el chat de arte); los
  puentes simulados. Solo se queda lo hecho con los assets de Tony (balcones C20c, etc.).
- Puentes: usar el modelo de puente que hizo Tony (búscalo en imageassets/ y Assets/Arte). Que conecte con calles reales: escalera
  que llega a algo, y si hay carros arriba, una rampa o curva para llegar manejando; si no, quitar los carros de arriba.
- Callejones y calles sin salida o donde te quedas atrapado: que conecten con otra calle. Ningún lugar del mapa del que no puedas salir.
- Cosas flotando: letreros, cámaras, adornos del tianguis. Texturas rotas en muros. NPC que atraviesan modelos (obstáculos de
  navegación). Barrido completo.
- La niebla tapa la ciudad al lado del callejón: bajar la niebla para que se vea que hay más ciudad.
- Todo se ve igual y no se reconocen las calles: dar a cada zona algo reconocible (color de luz, tipo de letrero, un punto de
  referencia) y nombres de calles.
- Taller del mecánico: quitar el letrero a media puerta y el que está atravesado. Un solo letrero bien puesto: "EL CANEPA" y abajo
  "TALLER MECÁNICO". Poner al mecánico (modelo Assets/Arte/Personajes/Mecanico) dentro de su taller con su Idle.
- Casino VivaCoin: siempre abierto, se puede entrar.

### Interiores y menús
- Doc (I+D+I): al hablar te dice que te sientes en la silla; la silla debe funcionar (hoy no deja sentarse). Al inicio del juego el
  Doc te regala un implante. El consultorio por fuera tiene paredes como un hilo (no sólidas): edificio sólido por fuera.
- Tino: un poco más grande. Te dice "el arma que quieras, la mejoramos; acércate al banco". El menú tarda en abrir: que abra al
  instante.
- Implantes: exo de piernas, exo de brazos (mejora recarga, golpes, todo) y visor de cabeza (ver enemigos a través de objetos),
  comprables con el Doc. Funcionan ya con reemplazo; el modelo llega del chat de arte.

---
## Después (con Tony)
- HUD nuevo (vida, calor, escudo): más moderno y cyberpunk. Primero mockups del chat de arte; Tony elige.
- Carros por partes (ruedas, dos puertas delanteras, sin cristal) y motos con ruedas aparte: los hace Tony en Tripo.
- Creación de personaje nueva.
- [?] "esto de cromo": preguntar a Tony.
