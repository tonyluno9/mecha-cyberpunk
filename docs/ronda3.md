# Ronda 3 — detalle (lo lee el /goal)
Va después del paso 2 de docs/plan-noche.md (T-414 a T-418): usa sus nombres (Suburbios, SKULLS, SNAKES, El Mercado, I+D+I, CIC, Biits Ƀ).
Si la ronda 2 (nombre CYBERFIX, silencio, bugs de mercado negro/Tianguis/escopeta, GestorSonidos) no terminó, termínala primero.
1) Marcador de objetivo nuevo: icono en pantalla sobre el objetivo con distancia en metros; fuera de pantalla, flecha en el borde apuntando; texto corto arriba ("Ve al Mercado", "Vence a los SKULLS 2/7"). Mismo objetivo en minimapa y mapa.
2) Minimapa redondo abajo a la izquierda: calles, tu posición y dirección, objetivo, zonas pintadas de su dueño, enemigos cercanos en rojo, aliados del color de tu pandilla, iconos de El Mercado, I+D+I, CIC y casa. Gira con el jugador. Mapa grande (M): zoom con rueda, leyenda, marcar punto con clic (aparece en minimapa con distancia).
3) Pausa con Esc: continuar, guardar, cargar, opciones, salir al título. Pausa real (tiempo, IA, disparos).
4) Opciones: sensibilidad, invertir Y, volumen general y por sección, calidad, resolución, pantalla completa. Se guardan y aplican al instante.
5) Guardar/cargar: 3 ranuras con fecha, zona y dinero; autoguardado al dormir y al conquistar zona; "Continuar" carga la última.
6) Título CYBERFIX (fondo de ciudad de noche, cámara lenta): nueva partida, continuar, opciones, salir. Creación de personaje sin mechas: 2 personajes (muñecos de prueba), nombre y color de tu pandilla. La demo arranca ahí.
Calidad: misma fuente y colores (naranja, turquesa, magenta sobre negro translúcido), legible en 1080p y 1440p, ratón y teclado. Cada pantalla: captura revisada y recorrido con entrada simulada como jugador nuevo; si se ve mal, arréglalo antes de seguir.
Reglas: sin revisor ni verificador, pruebas solo de lo nuevo; Opus para diseño de UI y guardado, Sonnet para lo mecánico. Merge a main con push al terminar cada punto (autorizado por Tony). Tras cada punto regenera Builds/Juego/Cyberfix.exe y Builds/Demo/Cyberfix.exe y 3 líneas de qué probar en docs/guia-para-jugar.md. Ciudad grande NO. Nunca force push; git restore a lo que Unity cambia solo.
