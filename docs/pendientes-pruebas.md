# Pendientes de las pruebas de Tony (2 y 3 de noviembre)

# Prueba de Tony — 2-nov-2026 (jugada con teclado y ratón)
Todo lo que Tony encontró jugando Builds/Demo, ordenado. [C] = lo arregla Claude Code sin arte nuevo. [A] = necesita arte o imágenes primero. [D] = después de la demo.

## Lo que SÍ gustó (no romper)
- Casas apiladas y encimadas: dan efecto de ciudad real.
- El Tianguis: cómo se ve y los NPC que interactúan.
- Cambio a tonos verdes en Neón Alto.
- El callejón cerrado al final con texturas.
- NPC quietos platicando; que se detengan y te miren cuando te paras enfrente.
- Los carros chocan y se incendian al dañarse; las barricadas; cómo se ve el día.
- Ver alrededor del personaje girando la cámara cuando está quieto.

## Pantallas
- [A] Logo nuevo: el juego probablemente se llamará CYBER STREET (por confirmar).
- [C] Pantalla de título con fondo animado (vistas de calles, cámara lenta por la ciudad) en vez de imagen fija.
- [x] Bug: al dar CONTINUAR desaparece todo. (T-537: CARGANDO… al continuar)
- [A] Creación de personaje: rediseñar al 100% (primero mockup).
- [C] Menú de armas del HUD: iconos, fondo y colores como el mockup.
- [C] HUD: mostrar la hora del juego.
- [D] Teléfono con animación al sacarlo (se descartó para la demo).

## Escala y cámara (general)
- [C] El personaje se ve muy pequeño frente al mundo (muros, puertas, mesas, objetos). Revisar la escala de TODO contra una persona de 1.8 m (puerta 2.1 m, mesa 0.75 m, banca 0.45 m, muro de piso 3 m) y acercar un poco la cámara. NPC y profes a la misma escala que el jugador. Robot de Nova más grande que una persona (2.1–2.3 m).

## Tu habitación (inicio de la demo)
- [C] Quitar la estación del mecha (T02) y dejar solo un banco de trabajo; no debe parecer taller.
- [C] Quitar a la mujer NPC que está dentro de la habitación.
- [C] Arreglar la puerta y la entrada de la habitación.
- [A] Imagen de referencia de la habitación + assets nuevos para amueblarla bien.

## Calle de inicio y personajes
- [C] El carro de enfrente dice "CARRO VIEJO" pero es moderno: nombre según el modelo.
- [C] Los enemigos SKULLS deben usar el modelo SkullHombre (no el genérico).
- [A] Modelo de la pistola que suelta el SKULL (falta el archivo).
- [C] Tino el Inventor no va enfrente de tu casa: va en la calle de al lado (donde están la peluquería, elotes, etc.), con su local junto al taller mecánico.
- [A] Interior propio para el taller de Tino y el taller mecánico (imagen de referencia + assets).
- [C] El Doc no va junto a El Mercado: necesita su propia área (I+D+I aparte).
- [A] Interior del consultorio del Doc.

## Carros y tráfico
- [x] Los carros del tráfico se estrellan mucho: carriles, distancia de seguimiento, frenar ante obstáculos y otros carros, acomodarse. (T-532 carriles, frenado y cruces; T-588 destrabe de atorones y sin salir encimados: TraficoFluidoT588Pruebas)
- [x] Que la ciudad tenga más calle más allá de lo jugable: los carros aparecen y desaparecen lejos y fuera de la vista; sin muros visibles al final; límites lejanos con niebla. (T-513 fondo lejano, T-530 afueras sin muro, T-532 aparecer y desaparecer fuera de la vista; T-588 lo prueba)
- [C] Manejo: que frene y se detenga con inercia (no que se resbale); freno firme.
- [C] Aviso cuando el carro se está incendiando ("¡Bájate, se va a quemar!").
- [x] Motos: poder subirse y manejarlas. (T-532 subir; T-592 manejo de moto con inclinación y freno con inercia: ManejoMotoPruebas)
- [x] Quitar la patrulla vieja fea; solo las patrullas nuevas (V05/V06/V08). (T-532: Patrulla.prefab usa V05 y el tráfico no lleva patrullas)
- [D] Ruedas separadas.

## Calor y policía
- [x] Al robar una patrulla o subir el calor: sonido de sirena y aviso de radio ("Unidades, sospechoso en…"), y que las patrullas vengan por ti y te persigan de verdad, como en GTA. (T-533 sirena y radio; T-587 persecución por calles, corte de paso y pérdida de vista: PersecucionRealistaT587Pruebas)
- [x] Seguridad de Nova sin inteligencia: que patrullen, te vigilen y reaccionen si sacas el arma o disparas. (T-533: GuardiaNova)

## NPC
- [C] Los NPC quietos: después de ~1 s de que estés cerca y quieto, te voltean a ver extrañados.
- [C] Los NPC que caminan chocan con paredes y se quedan atorados: NavMesh / evitar obstáculos / destrabarse.
- [D] Diálogos ("¿Qué haces aquí?").

## Ciudad y props
- [C] Quitar el pedazo de poste cutre en medio de la calle antes de El Tianguis.
- [C] Luces de los postes: la luz sale de otro lado; debe salir del foco.
- [C] Barrido de todo el mapa: assets rotos, cámaras en el suelo, cosas y focos flotando, proyectores repetidos de relleno, objetos encimados. Quitar o acomodar uno por uno (lo revisa Claude, no Tony).
- [C] Quitar los letreros planos de lugares (HOTEL, etc.) que se ven mal.
- [A] Letreros 3D reales para los lugares.
- [C] Quitar el letrero/barrera "ALTO CONTROL CORPORATIVO".
- [C] Lluvia: que las gotas lleguen al suelo con salpicaduras (ahora parecen quedarse en el cielo).
- [C] Asfalto más realista: textura de calle mojada con detalle (grietas, parches, líneas gastadas).
- [C] Día y noche: las luces no deben apagarse de golpe; que sigan prendidas un rato y se apaguen poco a poco.
- [x] Colisiones: no poder meterse en las paredes (callejón y en general); la cámara no debe atravesar edificios. (T-583/T-584: ParedesSolidasT583Pruebas, cámara con esfera)

## El Tianguis
- [C] Puestos encimados unos con otros: acomodar sin traslapes.
- [C] Cajas y assets flotando: bajarlos al piso.
- [C] Quitar los carteles 3D raros.
- [C] Cada puesto con su propia luz pequeña según lo que vende.
- [C] Quitar las dos barricadas en la zona de los licuados y el pastor.

## El Mercado
- [C] Quitar el letrero "EL MERCADO 24 HORAS ABIERTO".
- [A] Entrada especial de tienda de armas.
- [D] El sótano/clandestino vacío: para después de la demo (quizá en una animación).

## Neón Alto
- [A] Edificios reales (kit de torres T20–T23): las casas siguen siendo las del barrio.
- [A] Postes distintos para la zona rica.
- [A] Contenido para las pantallas LED (anuncios animados) y acomodo con estilo.
- [C] Los puentes atraviesan los edificios: moverlos.
- [x] Límite de la ciudad: no debe haber muro; más carretera y el límite lejos. (T-530 y T-513)

## Casino VivaCoin
- [A] Letrero y entrada del casino generados (hoy es mezcla de asset y formas).
- [C] Entradas y salidas de todos los edificios: se ven rotas y hay una pared negra al cruzar; que la puerta abra y se vea el interior.
- [A] Interior del casino: imagen de referencia + mesas y assets; hoy se ve vacío.
- [C] El Hacker (SERGIO) sentado en una mesa programando (necesita animación de sentado de Mixamo) y a escala.
- Comprar fichas primero: gustó, se queda.

---

# Prueba de Tony — 3-nov-2026 (segunda ronda)
Mismas marcas: [C] lo arregla Claude Code, [A] necesita arte/animaciones primero (Tony), [?] falta confirmar qué se quiere. Los que repiten algo de la lista del 2-nov dicen "(otra vez)".

## Sonido
- [x] (T-593) Sonidos raros al disparar y recargar: revisar que cada arma use su ID correcto (ARM_PIS/ESC/FUS/SNI), volumen y que no se encimen.
- [x] (T-594) Quitar el sonido de piano repetitivo (no va con el contexto).
- [x] (T-594, tic provisional) Cambiar el sonido de texto (avisos/diálogos) por uno mejor (UI más sutil).
- [x] (T-595) Quitar los sonidos repetitivos de los carros (motor/claxon en bucle sin variar): más variación, distancia y menos repetición.
- [x] (T-596, por zancada) Pasos más precisos: que suenen sincronizados con la pisada y según el piso (asfalto, mojado, interior, metal).
- [x] (T-597, faltan voces _F/_M) Sonido de agitación (jadeo al correr, esfuerzo, daño) según el personaje y su género. [A] si faltan voces femeninas/masculinas en Audio/JUG.
- [x] (T-597, faltan voces _F/_M) Mejorar los sonidos de muerte (jugador, pandilleros, aliados; variantes por género).

## Animaciones (Mixamo — las baja Tony, ver docs/animaciones-faltan.md)
- [A] Caminar, correr, saltar, caer y quieto con animación propia (no solo los clips de Tripo).
- [A] Subir y bajar de la moto y manejarla (por eso hoy no se puede subir).
- [A] Más animaciones para NPC: platicar, ver el celular, recargarse en la pared, voltear a ver, asustarse/huir, sentarse.
- [C] Conectarlas en cuanto estén en Assets/Arte/Personajes/Animaciones/.

## Jugador y cámara
- [x] Field of view y cámara raros al caminar: cámara más pegada al personaje (hombro), FOV más cerrado, sin flotar. (T-585)
- [x] Más vida al personaje (subir la vida base o la regeneración). [?] confirmar si es más vida/salud o más "vida" en movimiento. (T-585: vida 150, regen 8/s tras 4 s)
- [x] Al morir: cinemática corta (cae, fundido, despiertas en I+D+I con El Doc al lado) en vez de reaparecer de golpe. (T-586: MuerteReapareceEnClinicaPruebas)
- [C] Teléfono: interfaz con animación de sacarlo y guardarlo (antes estaba descartado para la demo; vuelve).

## Armas y CIC
- [C] En el CIC poder mejorar las armas cuerpo a cuerpo / los puños. [?] confirmar si "armas manuales" es eso o mejorar a mano.

## Carros y motos
- [x] Bug: se pueden atravesar los coches (colisiones jugador–carro y carro–carro). (T-580: ColisionCoches/CiudadT580Pruebas)
- [x] Prender y apagar las luces del carro (tecla), y faros encendidos de noche en el tráfico. (T-590: tecla L, LucesCarroPruebas)
- [x] Explosión con efectos (fuego, humo, chispas, onda, sonido) cuando el carro revienta. (T-591: ExplosionCarroPruebas; estruendo provisional, falta audio real)
- [x] Tráfico fluido (otra vez): sin choques tontos ni atorones. (T-588)
- [x] Motos: ocultar el reposapié/caballete expuesto al manejar. (T-592: malla sin caballete al manejar.) Subirse necesita la animación [A].

## Policía
- [x] Persecución realista: patrullas que te siguen por las calles, cortan el paso, sirena, radio, se pierden si te escondes. (T-587)
- [x] Las patrullas no deben atravesar paredes ni desaparecer de la nada. (T-581: PatrullaParedT581Pruebas)
- [x] Que aparezcan y se vayan con lógica: llegan desde fuera de la vista cuando hay peligro (calor, disparos) y se retiran fuera de la vista cuando baja el calor. (T-587: salen fuera de la vista y se retiran por la calle; más patrullas a más calor)

## NPC y aliados
- [C] Mejorar la inteligencia de los NPC (rutas, reacción a disparos y al jugador, no chocar).
- [C] Los aliados no se deben quedar quietos: que te sigan siempre (salvo que les digas "espera" con F) y peleen.

## Mundo y ciudad
- [C] Las gotas de lluvia deben caer al suelo (otra vez): salpicaduras en el piso.
- [C] Puestos ajustados al tamaño real (escala con la persona).
- [x] Letreros y carteles que se buguean en acción (parpadean, desaparecen o se mueven al pelear o manejar): revisar. (T-582: el culing no corta letreros)
- [C] Texturas en el cielo (nubes, estrellas, brillo de la ciudad en las nubes) de día y de noche.
- [C] Cambiar la iluminación de día (más natural, menos plana).
- [?] El mapa no debe ser cuadrado y debe tener muchas… (la frase quedó cortada: ¿muchas calles, curvas, niveles?). Por ahora: calles con curvas, diagonales y callejones para que no se vea como cuadrícula.

## Minimapa e interfaz
- [C] El minimapa marca solo el objetivo: debe mostrar la ruta (línea por las calles hasta el objetivo, como GPS).
- [C] Interfaces más atractivas (otra vez): HUD, menús, tienda, mapa y teléfono con el estilo del mockup.
