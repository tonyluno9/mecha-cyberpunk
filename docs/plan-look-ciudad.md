# Plan: que CYBERFIX se vea como HUD_A_minimalista.png (6-nov noche)

Imagen objetivo: `imageassets/ui/hud-opciones/HUD_A_minimalista.png` (y las 3 imágenes de zona que vienen del chat de arte:
K02 tianguis, K03 Neón Alto, K04 barrio de día). **Esta es la imagen de cómo debe verse el juego**, no solo el HUD.
Corte 12-nov: quedan 5 días. Se trabaja en este orden para que lo de la demo quede perfecto primero.

## 1. Qué tiene la imagen (lista para comparar capturas)
**Calle y cuadras**
- Calle angosta (~7–8 m de arroyo) con banquetas de ~2 m, una línea blanca discontinua al centro. Cuadras cortas: se ve el final
  de la calle o la siguiente esquina, nunca un pasillo infinito.
- Fachadas continuas, sin huecos entre casas, de 2 pisos (a veces 3), techo plano. Cada casa de un color: terracota, turquesa
  apagado, crema, verde agua, con marcos de puerta amarillo/naranja.
- En planta baja: cortinas metálicas, puertas de reja, ventanas con barrotes, toldos de lámina, medidores en la pared, focos de
  pared, macetas en las ventanas, un letrero de neón chico sin texto cada 2–3 casas (magenta, turquesa).
- Arriba: tinacos negros sobre bases de ladrillo, aires acondicionados, tubos, antenas. Postes de concreto con transformador y
  muchos cables colgando entre postes y casas (catenaria, cruzando la calle).
- Carros estacionados junto a la banqueta, no en medio.
**Luz**
- Noche azul con niebla que da profundidad: lo lejano se vuelve azul y se pierde.
- La luz principal es naranja de sodio y sale de la cabeza de la lámpara del poste, cono hacia abajo, con un halo en la niebla.
- Toques de neón magenta y turquesa (letreros, la línea del carro) que se reflejan en el piso.
- Ventanas y focos de pared cálidos.
**Piso**
- Asfalto mojado con charcos que reflejan las luces (naranja, magenta, turquesa). Banquetas de concreto con bordes, alcantarillas,
  basura pequeña.
**Estilo**
- Ilustrado: contornos oscuros finos, texturas pintadas, poco detalle fino, colores saturados en la luz y apagados en las paredes.
**Cámara**
- Tercera persona como GTA: el personaje centrado y abajo, ~1/4 de la altura de la pantalla, cámara a la altura del hombro un poco
  alta, mirando a lo largo de la calle.
**HUD (decidido 6-nov 23:45, ver imageassets/referencias/look/H10_HUD.png cuando llegue)**
- Arriba izquierda: barra de ESCUDO delgada y corta arriba, debajo barra de VIDA larga con su número (143 / 204); debajo las
  estrellas de búsqueda turquesa y el dinero.
- Arriba derecha: minimapa CUADRADO con la ruta punteada, y debajo el objetivo de misión con su distancia.
- Abajo izquierda: artilugios / objetos (bebida, papitas, venda...) en casillas con su tecla y cantidad, para usarlos rápido.
- Abajo derecha: arma con su holograma de lado, balas grandes, muñequito del cuerpo (implantes/daño) al lado, y arriba 1–2 ayudas
  de botón ("[R] Recargar", "[F] Golpe").
- Estilo: inspirado en el acomodo de esa captura, pero diseño propio (no copiar gráficos de otro juego): líneas finas, sin paneles,
  colores rojo-rosa (vida) y turquesa (escudo, estrellas, arma).
- Mapa de la ciudad: sin barrios cuadrados marcados. Los enemigos y la policía pueden salir en cualquier parte; los barrios se
  distinguen solo por el color de su luz y su tipo de casa, mezclándose en los bordes. El Doc (I+D+I), Tino (CIC) y El Canepa
  van cerca de la casa, en Suburbios. Tianguis sin focos colgando.

## 2. Cómo se logra en Unity (URP)
1. **Postproceso** (un Volume global): tonemapping ACES, bloom suave (que los neones y lámparas den halo), gradación azul en
   sombras y naranja en luces, viñeta leve, niebla exponencial azul más densa a lo lejos.
2. **Contornos**: pase de pantalla completa de detección de bordes (profundidad y normales) con línea oscura fina = look ilustrado.
   Probar primero en la calle de prueba; si ensucia, bajarlo.
3. **Piso mojado**: asfalto con rugosidad baja y máscara de charcos (decals); reflejos con sondas de reflexión por cuadra y una
   reflexión planar (o SSR) solo en la calle donde está el jugador. Sin lluvia.
4. **Luces**: spot naranja desde la cabeza de cada lámpara (ya hecho en T-855), con mesh de cono de luz translúcido para el halo en la
   niebla. Neones emisivos + luz puntual chica de su color. Ventanas con emisión cálida al azar (no todas).
5. **Cables**: generador de cables curvos entre postes y entre poste y casa (curva colgante, no recta), varios por poste.
6. **Cámara**: ajustar distancia/altura/FOV para que el encuadre sea el de la imagen (personaje ~1/4 de pantalla).
7. **HUD**: rehacer con el acomodo de arriba.

## 3. Método (para que sí quede igual)
1. **Calle de prueba ("calle dorada")**: construir UNA calle de ~60 m idéntica a HUD_A en la zona de la casa, con los assets del kit.
   Captura desde el mismo ángulo y comparar lado a lado con la imagen; repetir hasta que se parezca (luz, piso, color, cables, casas).
   Anotar qué assets faltan (ver §5) y pedirlos al chat de arte.
2. **Convertirla en kit de manzanas**: 6 prefabs de "frente de cuadra" (A–F) de ~60 m con variaciones de colores y negocios,
   + esquinas + cruce con postes, semáforo y nombres de calle. Así se arma la ciudad entera con piezas ya aprobadas.
3. **Rehacer la ciudad por barrios con el mapa nuevo** (§4), empezando por la ruta de la demo.
4. Lo de las zonas K02–K04 se hace igual: una calle de prueba por zona y luego su kit.

## 4. Mapa nuevo: ciudad por cuadras
- Cuadras de ~60 × 80 m, calles de 10–12 m (con banqueta), cruces cada cuadra. Ninguna calle recta de más de 3–4 cuadras sin un
  cambio (plaza, curva, glorieta, calle que dobla). Callejones cortos entre cuadras.
- 4 barrios, ~3 × 3 cuadras cada uno (≈ 500 × 500 m total). Cada barrio con su color de luz y su tipo de casa:
  1. **Suburbios / el barrio** (inicio, ruta de la demo): casa del jugador, callejón de los SKULLS, I+D+I del Doc, CIC de Tino, taller
     El Canepa. Casas de 2 pisos, luz naranja de sodio, neón chico. = HUD_A.
  2. **El Mercado**: tianguis en una calle cerrada con puestos, mercado de armas, plaza. Toldos, focos, mucho color. = K02.
  3. **Neón Alto**: casino VivaCoin, hotel, edificios de 4–6 pisos, pantallas, neón magenta y turquesa. = K03.
  4. **Megalópolis / Nova**: torres, la torre de Nova y su refinería; calles más anchas y limpias, luz blanca fría.
- Los bordes del mapa: autopista elevada o muro/barda con vista a las torres lejanas (que se vea que hay más ciudad).
- Mockup del mapa: M10 del chat de arte, para aprobarlo antes de construir.

## 5. Assets que probablemente faltan (confirmar en la calle de prueba)
Toldo de lámina, cortina metálica con marco amarillo, puerta de reja, ventana con barrotes y maceta, foco de pared, base de ladrillo
para tinaco, alcantarilla, basura pequeña, letreros de neón cuadrados sin texto (varios colores), poste de concreto con
transformador (comparar con los actuales), semáforo. Lo que falte va en un pedido nuevo al chat de arte.

## 6. Orden con el corte del 12-nov
- 7-nov: calle de prueba + postproceso + contornos + piso mojado + cámara + HUD nuevo. Tony aprueba la captura.
- 8–9-nov: kit de manzanas y barrio 1 completo (toda la ruta de la demo) con el mapa nuevo.
- 10–11-nov: barrios 2 y 3 (tianguis, mercado, casino); barrio 4 lo mínimo que se vea en la demo.
- 12-nov: corte. Solo arreglos.
Honesto: rehacer toda la ciudad en 5 días es mucho. Si no alcanza, los barrios 2–4 quedan con su calle principal al estilo nuevo y
el resto como está, limpio.
