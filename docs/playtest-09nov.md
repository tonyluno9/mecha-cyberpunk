# Prueba de Tony 9-nov 01:00 (build de main con interfaces, T-749 y B7) — tareas

Corte 12-nov. Lo pendiente de reportes anteriores sigue vigente. Cada sesión, además, juega su parte y arregla por su cuenta lo roto.
Le gustó (NO tocar): menú de inicio (salvo lo de abajo), abrir puertas al salir de lugares, la animación de la silla del Doc, la interfaz
de armas de Tino ("cool"), la clínica e implantes (le gusta cómo se ven), la luz de los postes (perfecta), los neones, que los barriles
exploten, que dibuje la ruta en el mapa, que las estrellas se quiten al entrar a casa, las motos (ruedas bien).

## SESIÓN A — Jugador, combate, policía, vehículos, UI y mapa
- Menú de inicio: fondo nítido Full HD (quitar el desenfoque); quitar el logo de abajo y el texto "sobrevive a la noche".
- **Quitar por completo los rayos/trazos de las balas** (enemigos, policía, dron y jugador). Las balas a veces dejan marcas en el aire:
  los huecos solo van sobre superficies reales.
- Muerte: la caída y el paso a la pantalla de muerte, más rápidos.
- Al morir reapareces con el Doc (I+D+I): hoy a veces no aparece el Doc/I+D+I. Revisar el respawn.
- **Quitar los aliados por completo, para siempre** (que te siguen, que suben al carro, la pandilla). Sin dejar código muerto que falle.
- Todos los carros (incluida la policía) dejan un rastro de fuego al avanzar: quitarlo (probablemente un efecto del Particle Pack mal puesto).
- Ruedas de los carros por piezas más chicas que el hueco: escalar RU01 por carro para que llene el paso de rueda.
- Robar un carro: el conductor sale volando por los cielos. Debe salir, caer al piso y quedarse, o levantarse y huir corriendo.
- Policía: **solo te busca si una patrulla o un policía te ve cometer el delito**. Quitar las cámaras y los civiles que llaman como disparadores.
  La policía también va contra los rateros que disparan o atacan sin que tú hicieras nada.
- Drones: solo aparecen con cierto nivel de búsqueda y llegan volando (no de la nada); que no se atraviesen entre sí.
- Los asaltos de rateros son demasiado frecuentes: bajarlos mucho.
- Disparar agachado: el personaje se para solo y dispara (y hace daño). Hoy dispara agachado y no hace daño.
- Si disparas hacia atrás o de lado, el personaje gira hacia donde dispara.
- La vida se recupera demasiado rápido: más lenta y con espera tras recibir daño.
- Salto: la animación se ve rara; usar bien los clips de salto de Tony (SaltoInicio/Aire/Aterrizaje). Si el clip es el problema, anotarlo para Blender.
- **Mapa grande (M), prioridad:** rediseño tecnológico y transparente: solo contornos finos de calles y edificios (líneas turquesa), sin
  rellenos de colores; íconos de lugares; la ruta marcada. El I+D+I del Doc debe verse siempre y el destino marcado no debe desaparecer al cerrar el mapa.
- Banco de Tino: mejoras para TODAS las armas (escopeta, fusil, francotirador), no solo la pistola, con sus piezas.

## SESIÓN B — Mundo, interiores, casino, mercado
- **Quitar casi toda la niebla**: que se vea el fondo de la ciudad.
- I+D+I del Doc: una pared con la textura vieja y se ve la pared del fondo. Arreglar.
- Cuarto: quitar los pósters (texturas rotas). Poner la puerta del cuarto con el modelo P10 (`imageassets/modelos3d/P10.zip`).
- Entrada de la casa vista desde dentro: textura vieja rota. Arreglar.
- Canepa está diminuto otra vez: 1.80 m y una prueba que lo vigile.
- Barrio junto a la casa: casas sin terminar, huecos. Completarlo.
- Charcos: más realistas.
- **Quitar los letreros de nombres de calles** (EJE CENTRAL, AV. COYOTE...) de TODOS los postes de la ciudad.
- Puertas con neones raros: revisar que se vean bien.
- Tienda del armero (Mercado de armas): rearmarla según `docs/referencias-interiores/entrada-armas-mercado.png` (y lo pendiente en docs/interiores-faltan.md).
- **Casino VivaCoin**: entrada mejor armada; interior armado de verdad según `docs/referencias-interiores/casino-interior.png`; quitar o
  arreglar el "croupier" raro; meseras robot R01 rotas (revisar import, avatar y animación); la mesa del Hacker rara. Que todo se vea pensado.
- Realismo general: terminar texturas pendientes y lo que se vea a medias.

## PARA TONY (Blender / Tripo)
- Puertas de los carros que no se ajustan bien al hueco (V01a, V01b, V05): ajustarlas en Blender.
- Pose del personaje al ir en moto: ajustar en Blender (clip de manejar moto sobre PJ01).
- Miras de las armas (modelos de mira).
- Carrocerías y puertas de V02, V03, V04, V06, V09, V10; cuerpos de V07 y V08.
- Clips que faltan sobre PJ01: docs/pedido-blender-10nov.md.

## Después
- Más implantes en la clínica.
