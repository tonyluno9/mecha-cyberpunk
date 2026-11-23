# Tareas de la noche del 23-nov (prueba de Tony del 23-nov, madrugada)
Cada tarea tiene su documento en esta carpeta. Capturas en docs/capturas/_prueba-23nov/.
Reglas: las de arriba de docs/tareas-por-hacer.md. Además: comprobar cada tarea en Play con captura de la VENTANA del juego; si te trabas más de 20 min, NO QUEDÓ con la razón y sigue; al terminar cada una, márcala aquí como HECHA o NO QUEDÓ.
Le gustó (NO se toca): el logo, las opciones, levantarse del suelo al inicio, la ranura del muro, las patrullas respetando semáforos, el Doc sin camilla en la primera visita y su cinemática, ponerse el implante, el mercado y su textura, la torre de Nova, agacharse y cubrirse, que los enemigos se curen, que algunos peleen con escopeta, que no todos tengan armas, que las pandillas peleen entre ellas.

| Tarea | Título | Sesión | Estado |
|---|---|---|---|
| T50 | [Créditos y Cargar partida con el menú nuevo](T50.md) | B | HECHA |
| T51 | [Quitar la ruta / rastro de misiones](T51.md) | B | HECHA |
| T52 | [SKULLS del inicio: a media calle, de frente y se van corriendo](T52.md) | B (lo del mapa lo coloca A) | HECHA |
| T53 | [Todos los humanos usan EXACTAMENTE las animaciones del jugador](T53.md) | B | HECHA |
| T54 | [Reducir los carros estacionados a la mitad](T54.md) | A | HECHA (23-nov, T-1705 + escena ffefaee): estacionados activos 84 → 51 (de los 59 que se pueden tocar quedan 26; los 25 restantes son de guion, robables de encargos o motos/policía). Junto a la casa quedan 2 (V01a y V13) más los dos de guion. Tablas y captura en docs/capturas/_tareas/T54/. |
| T55 | [Verificar que ya no suena el roce de ropa (T49)](T55.md) | B | HECHA |
| T56 | [Prótesis: holograma más rápido y en su lugar; mismas animaciones con y sin prótesis](T56.md) | B | HECHA |
| T57 | [Menú de prótesis (Tab) con el cuerpo holográfico](T57.md) | B | HECHA |
| T58 | [Estela al caer con las piernas: efecto de humo](T58.md) | B | HECHA |
| T59 | [Tino vende las armas y las mejora; fuera la armería](T59.md) | B (quitar armería del mapa: A) | HECHA |
| T60 | [Guardar el arma: que desaparezca de la mano](T60.md) | B | HECHA |
| T62 | [Avisos del tutorial en una ventana central con 'Entendido'; máquina expendedora con ratón](T62.md) | B | HECHA |
| T63 | [Consumibles: usarlos mientras corres, con sonido, y que curen](T63.md) | B | HECHA |
| T64 | [Quitar el sonido al hacer una kill](T64.md) | B | HECHA |
| T65 | [Eliminar TODOS los sonidos viejos de la primera versión](T65.md) | B | HECHA |
| T66 | [Misión 'Libera el Tianguis': enemigo escondido que no deja terminar](T66.md) | A | pendiente |
| T67 | [Enemigos marcados en el minimapa y el mapa](T67.md) | A | pendiente |
| T68 | [Siempre de noche; día o noche solo desde Ajustes](T68.md) | B | HECHA |
| T69 | [Tinacos flotando y primitivos restantes: barrido automático](T69.md) | A | HECHA (23-nov, merge 23ce0f9): 27 flotantes → 0 tinacos/aires/antenas/cajas (19 bajados a su techo); quedan 6 justificados (elevador del taller y 5 candiles del casino). Primitivos 1615 → 1607: se apagaron 8 franjas de cubo sin textura; el resto son arquitectura con textura, neones, calcomanías de calle y cielo. Listas y captura en docs/capturas/_tareas/T69/. |
| T70 | [Charcos que se ven como manchas blancas](T70.md) | A | HECHA (23-nov, T-1704 28cd272): el shader del charco ya no muestra cielo claro cuando el reflejo plano cae fuera de cámara (usa fondo oscuro) y limita reflejo y fresnel. En Play de noche, 3 ángulos del Tianguis sin charcos blancos (docs/capturas/_tareas/T70/). |
| T71 | [Civil tirado en la calle del mercado](T71.md) | A | HECHA (23-nov, T-1703 f258def): el cuerpo era de un pandillero caído. Ahora todo cuerpo (pandilleros, policías a pie y civiles) se apaga a los 45 s si está fuera de vista (máx. 120 s a la vista), también colliders. En Play: dos cuerpos fuera de vista se apagaron entre 38 y 52 s. |
| T72 | [Refinería y calles detrás de Nova: enemigos que reaparecen sin fin](T72.md) | A | HECHA (23-nov, T-1702 5d6089d): Nova con tope de 4 vivos por grupo, reposición a 60 s y solo con el jugador a más de 80 m. En Play: 3 guardias muertos con el jugador a 16 m, a los 71 s seguían 0. Las cápsulas de Nova por nivel de búsqueda no se tocaron. |
| T73 | [Enemigos que mueren de un solo balazo en la cabeza](T73.md) | A | HECHA (23-nov, T-1707 5f98f85): cabeza ×3 → ×2; francotirador ×4. Pistola: pandillero (60) sobrevive 1 tiro a la cabeza y muere con 2; policía (140) con 3; Nova (420) con 9; francotirador mata de 1 a pandillero y policía. Comprobado en Play con la llamada de PistolaAPie.Impactar (60 → 10 → 0). Tabla en docs/capturas/_tareas/T73/tabla.md. Ojo: el revólver (55) sí mata de 1 tiro a la cabeza a un pandillero. |
| T74 | [Zonas de 'glitch': sonido de transformador donde haya cosas raras](T74.md) | A | pendiente |
| T75 | [Atropellar civiles y policías: que salgan volando](T75.md) | A | pendiente |
| T76 | [Puertas que se abren en los carros básicos (opcional)](T76.md) | A | NO QUEDÓ (23-nov): los carros básicos de Tripo (V01a, V02, V03, V09, V13, V14) son de una sola malla, sin puerta separada; PuertasCarro ya abre puertas en la patrulla y los carros de piezas. Hace falta separar la puerta en Blender. Nota en docs/capturas/_tareas/T76/nota.md (rama T-1711). |

# EN ESPERA (no se hacen esta noche)
- E1. Pantallas de carga con frames del tráiler (cuando termine el tráiler).
- E4. Cinemáticas reales: Tony las pasa mañana.
- E6. Mejorar la estética del mapa grande y el minimapa.
- E7. Animación del disparo de plasma (para el chat de Blender).
- E5 queda CANCELADO: el juego es siempre de noche (T68).
