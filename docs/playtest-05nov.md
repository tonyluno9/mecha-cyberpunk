# Prueba de Tony 5-nov (build MechaCyberpunk.exe de las 14:50) — tareas

Corte 12-nov. Todo lo de abajo es obligatorio. Además de esta lista, cada sesión **revisa su parte del juego por su cuenta**
(capturas propias comparadas con las referencias) y arregla lo que se vea fuera de escala, feo o roto, aunque no esté escrito aquí.
Regla de estética: si algo se ve hecho con primitivos (cubos, planos con texto) y rompe la ciudad, se quita o se reemplaza con assets.
Reparto en 3 sesiones para no pisarse. Cada una en su rama, merge a main seguido (git pull/merge de main antes de cada merge).

---
## SESIÓN 1 — Jugabilidad (PC de Tony, carpeta principal) — rama tony/s1-jugabilidad

### Pistola y animaciones con arma
- La pistola no está en la mano: va chueca, abajo. Arreglarlo en Unity (no Blender): el arma es hija del hueso de la mano derecha
  (Humanoid RightHand) con offset/rotación por arma para que el agarre coincida con la palma; mano izquierda por IK al arma en
  pistola a dos manos y rifle. Revisar en caminar, correr, apuntar, disparar y recargar (capturas de cerca).
- Al disparar el personaje se ve mal: que la pose de disparo use el arma bien agarrada y apuntando al frente.
- Con arma en mano (sin apuntar) debe usar Pistola_Caminar / Pistola_Correr / Rifle_Caminar / Rifle_Correr, no la caminata normal.
  Hoy solo cambia al apuntar.
- Animaciones lentas: revisar velocidad de todas contra el movimiento real (subir/bajar del carro, saltar, golpes, interacciones).
  Disparo y recarga ya están bien, no tocar.

### Carros y moto
- Subirse: si estoy cerca del carro (≤2.5 m de cualquier puerta, sin importar hacia dónde miro) y aprieto E, me subo al más cercano.
  Nada de buscar la posición exacta.
- Animación de subir/bajar: el personaje se pega a la puerta (se acomoda al punto de la puerta antes del clip, sin flotar ni
  despegarse del carro) y termina sentado en el asiento.
- Bajarse nunca debe trabarse: si la puerta del conductor está bloqueada (muro), salir por el otro lado, luego por atrás/arriba.
- Reversa: hoy se ve terrible. Que al dar reversa el carro retroceda suave, la dirección se sienta natural, la cámara no gire raro
  y no haya tirones al cambiar de adelante a atrás.
- Luces del carro: quitar los cuadrados que simulan los faros; usar emisión en la parte del faro del propio modelo o una lente plana
  pegada a la carrocería, más la luz spot. Las luces ya se ven bien, solo quitar los cuadros.
- Moto: quitar las luces que se le pusieron (se ven raras). El jugador queda muy abajo al subirse: ajustar asiento/pose para que
  quede sentado encima, manos en el manubrio, pies en los estribos.
- Después (si da tiempo, al final): puertas del carro que se abren al subir/bajar y ruedas que giran (si los modelos lo permiten).

### Policía (reglas nuevas)
- La policía solo viene por ti si te lo ganaste y alguien te vio:
  1. una patrulla o policía con línea de vista al delito,
  2. una cámara de seguridad que te ve (ponerlas visibles en postes/esquinas),
  3. un civil que te ve, se asusta y llama (con unos segundos de retraso; si lo matas o huye antes de llamar, no llega nadie).
- Nunca aparecer patrullas de la nada a la vista del jugador: nacen lejos y fuera de cámara y llegan manejando por las calles.
- No se regresan solas: te persiguen hasta arrestarte o hasta que escapes saliendo de su vista y de la zona de búsqueda un
  tiempo (como GTA: el círculo de búsqueda); sin eso no se van.
- IA de patrullas más natural: manejan por la calle, rodean, no chocan entre ellas tontamente.

### Mapa
- En el mapa grande y el minimapa: íconos de todos los lugares a los que se puede ir (casa, El Doc, Tino el inventor, mecánico,
  tianguis, casino, mercado, misiones) y poder marcar uno para que el GPS trace la ruta.

### Limpieza
- Borrar las ~61 Assets/InitTestScene*.unity sueltas (restos de pruebas caídas) y que no se vuelvan a quedar.

---
## SESIÓN 2 — Mundo y arte (PC de Alan) — rama tony/s2-mundo

- **Quitar la lluvia** por completo (partículas, sonido, charcos animados). El asfalto mojado con reflejos puede quedar.
- Quitar los letreros hechos con primitivos de los edificios (CAFÉ, INTERNET, CANTINA, ABARROTES, etc.). Dejar solo letreros que
  sean assets (L10–L14) o imágenes de imageassets/pantallas.
- Tianguis: quitar todas las series de focos colgadas en las sogas y el cartel/marco del tianguis. Lo demás se queda.
- Luz del poste: la luz debe salir de la lámpara (la cabeza del poste, apuntando hacia abajo), no del tubo del poste. Revisar todos.
- Quitar textos flotantes tipo "El Doc Joel te cura" y similares en todo el juego. La información va en el HUD o al interactuar.
- **Escala**: El Doc está enano; la mochila del cuarto es diminuta. Revisar TODO contra el jugador de 1.8 m (personajes, muebles,
  puertas, props, carros, puestos) y corregir lo que esté fuera de escala. Capturas lado a lado con el jugador.
- Talleres y consultorio armados según las referencias (docs/referencias-interiores/):
  - consultorio-doc.png (El Doc, I+D+I), taller-tino.png (Tino el inventor), **taller-mecanico.png (el mecánico de enfrente de
    la casa, que necesita su propio taller)**. Ver lo que falta en docs/interiores-faltan.md.
- Casa del jugador: el edificio por fuera está feo (rehacer fachada con el kit modular y props), la transición al entrar es fea
  (fundido corto y limpio o entrar caminando sin pantalla negra brusca), al cuarto le falta la puerta. Habitación según
  habitacion.png.
- Callejón junto a la casa: rehacerlo según docs/referencias-ciudad/callejon-noche.png.
- Las calles "simuladas" (bloques/fachadas de relleno al fondo y a los lados): construirlas de verdad con edificios y casas
  (kit modular de fachadas y pisos, azoteas, tinacos, cables), para que cualquier calle que se vea sea recorrible y se vea real.
  Sin tocar la ruta de la demo. Referencias en docs/referencias-ciudad/.

---
## SESIÓN 3 — Pantalla de inicio (PC de Tony, copia aparte) — rama tony/s3-menu

- Olvidar el menú actual y las imágenes de mockup. Hacer la pantalla de inicio más profesional e interactiva posible:
  - fondo vivo en 3D (la ciudad de noche desde una azotea o calle con cámara lenta que se mueve, neón, niebla) en lugar de imagen,
  - logo CYBERFIX animado (glitch/neón), botones con hover, sonido UI y transiciones,
  - menús: Jugar/Continuar, Opciones (gráficos, audio, controles, sensibilidad), Créditos, Salir; con teclado, mouse y control,
  - pantalla de carga con tips y fondo animado.
- Solo tocar la escena del menú, su UI y sus scripts. No tocar la ciudad, el jugador ni gameplay.
