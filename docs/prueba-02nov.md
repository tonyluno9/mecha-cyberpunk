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
- [C] Bug: al dar CONTINUAR desaparece todo.
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
- [C] Los carros del tráfico se estrellan mucho: carriles, distancia de seguimiento, frenar ante obstáculos y otros carros, acomodarse.
- [C] Que la ciudad tenga más calle más allá de lo jugable: los carros aparecen y desaparecen lejos y fuera de la vista; sin muros visibles al final; límites lejanos con niebla.
- [C] Manejo: que frene y se detenga con inercia (no que se resbale); freno firme.
- [C] Aviso cuando el carro se está incendiando ("¡Bájate, se va a quemar!").
- [C] Motos: poder subirse y manejarlas.
- [C] Quitar la patrulla vieja fea; solo las patrullas nuevas (V05/V06/V08).
- [D] Ruedas separadas.

## Calor y policía
- [C] Al robar una patrulla o subir el calor: sonido de sirena y aviso de radio ("Unidades, sospechoso en…"), y que las patrullas vengan por ti y te persigan de verdad, como en GTA.
- [C] Seguridad de Nova sin inteligencia: que patrullen, te vigilen y reaccionen si sacas el arma o disparas.

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
- [C] Colisiones: no poder meterse en las paredes (callejón y en general); la cámara no debe atravesar edificios.

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
- [C] Límite de la ciudad: no debe haber muro; más carretera y el límite lejos.

## Casino VivaCoin
- [A] Letrero y entrada del casino generados (hoy es mezcla de asset y formas).
- [C] Entradas y salidas de todos los edificios: se ven rotas y hay una pared negra al cruzar; que la puerta abra y se vea el interior.
- [A] Interior del casino: imagen de referencia + mesas y assets; hoy se ve vacío.
- [C] El Hacker (SERGIO) sentado en una mesa programando (necesita animación de sentado de Mixamo) y a escala.
- Comprar fichas primero: gustó, se queda.
