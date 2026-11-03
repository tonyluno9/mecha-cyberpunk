# Plan de la noche — 3-nov-2026
Modo demo. Reglas de siempre: Sonnet (Opus solo si algo falla 2 veces o para comparar capturas), una rama por tarea desde tony/fase-g (IDs T-530 a T-559), pruebas de lo que tocas + RecorridoDemoPruebas al final de cada paso, merge a main con push al terminar cada paso (yo, Tony, lo autorizo), regenera Builds/Demo y Builds/Juego, 3 líneas en docs/guia-para-jugar.md, 60 FPS de noche, no CreadorTodo, nunca force push. Si se acaba el límite, espera y continúa. Haz commit de este archivo primero.

## Paso 1 — Arreglos de la prueba de Tony
Todos los puntos [C] de docs/prueba-02nov.md, en el orden de esa lista de prioridades: escala y colisiones; barrido de assets rotos/flotantes/encimados; habitación y lugares de Tino y el Doc; tráfico, ciudad que sigue más allá, motos, manejo, patrullas; policía y guardias; NPC; luces, lluvia, asfalto; puertas; HUD, bug de CONTINUAR, título animado. No rompas "Lo que SÍ gustó". Capturas antes/después en docs/capturas/prueba2-*.png.

## Paso 2 — Assets nuevos de imageassets/modelos3d/ (flujo de T-250/T-419; ignora "- copia")
- Kit de torres T20_BaseTorre, T21_PisoOficinas, T22_PisoPantalla, T23_RemateTorre (hay dos zips de T23: usa el que se vea mejor y anótalo). Arma torres en Neón Alto apilando: 1 base + 6 a 20 pisos alternando T21/T22 + 1 remate; alturas distintas; reemplazan a las casas de barrio que hoy forman Neón Alto (las casas se quedan solo en Suburbios, Callejón y Tianguis). Las torres 3D de código que sobren en Neón Alto se quitan. FPS 60 (LOD, combinar mallas).
- N09_PosteRico: reemplaza los postes de Neón Alto; luz fría blanca que salga del foco.
- Letreros 3D: L10 VivaCoin sobre la entrada del casino (reemplaza la mezcla actual de asset y formas), L11 Hotel en el hotel, L12 El Mercado en la entrada de armas de El Mercado, L13 I+D+I en el local del Doc, L14 CIC en el local de Tino. Quita los letreros planos que reemplazan. Que su texto brille (emisión) de noche.
- V07_MotoReparto: moto civil manejable y robable, estacionada en Suburbios y El Tianguis.
- Pantallas LED: imageassets/pantallas/*.png como contenido de E03, CY02, N08 y T22 (horizontales en pantallas anchas, verticales en altas), con emisión, un leve parpadeo y cambio de anuncio cada 8–12 s. Acomódalas con sentido (en fachadas y torres, no flotando).

## Paso 3 — Pistola
Según imageassets/modelos3d/armas/W01_Pistola/LEEME.md.

## Paso 4 — Interiores con lo que hay (referencias en docs/referencias-interiores/)
Arma cada interior acomodando assets existentes para que se parezca a su referencia en distribución, luz y color, a escala humana (1.8 m), con la puerta abierta y visible desde afuera (sin pared negra). Lo que falte, déjalo como hueco limpio (sin cubos) y anótalo en docs/interiores-faltan.md.
- habitacion: cuarto de joven, NO taller (cama H02, buró H11, tele H06 o compu, sofá fuera si no cabe), foco colgante cálido, ventana con luz neón de la calle magenta y turquesa, pósters (planos con textura generada).
- consultorio-doc (I+D+I, ya en su nueva área): luz turquesa, camilla, monitores, estantes, El Doc parado junto a la camilla.
- taller-tino (CIC): bancos T03, tablero T01, repisa T08, drones CY07b colgando, series de focos, pizarrón; Tino junto al banco.
- casino-interior (VivaCoin): tragamonedas en hileras, mesas, barra, alfombra estampada (textura generada), papel picado, luces doradas y magenta; El Hacker en una mesa al fondo.
- entrada-armas-mercado: mostrador del vendedor, cajas, tambos, reja, papel picado, series de focos.
Capturas lado a lado con cada referencia: docs/capturas/interior-<nombre>-comparacion.png.

Al terminar: reporte corto en docs/estado-tony.md.
