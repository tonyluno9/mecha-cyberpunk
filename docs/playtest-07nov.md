# Prueba de Tony 7-nov 14:50 (build CyberCity.exe de mecha-s1, 14:20) — tareas

Corte 12-nov. Sigue vigente lo que falte de los reportes (T-721, T-722, Builds/Juego, recorrido completo).
Regla nueva: **NO inventar animaciones ni gráficos del HUD**. Si falta una animación, se anota en docs/animaciones-faltan.md y Tony
la baja de Mixamo. Si falta un gráfico, se pide al chat de arte (docs/pedido-arte-*.md) y mientras se deja el actual.

Le gustó: pantalla de carga, el espejo ("increíble"), el estilo nuevo de las calles y techos, cables, charcos (mejoraron), semáforos,
que los carros del tráfico interactúen y se frenen, la calle de Nova y su cambio de luz, las estrellas, el banco de Tino.

## SESIÓN 1 — Jugador, cámara, HUD, misión, policía, enemigos (mecha-s1)
- **Cámara**: más abajo y más centrada, como la imagen de referencia que manda Tony (imageassets/referencias/look/CAM01*.png cuando
  llegue; mientras, H10). En el cuarto la cámara está muy lejos y atraviesa paredes: colisión de cámara que se acerca en interiores.
- **Caminar y correr**: revisar qué clips usa el jugador. Deben ser EXACTAMENTE Loco_Caminar y Loco_Correr del zip Movimiento_base de
  Tony (no los de Tripo, no editados, sin capas procedurales encima). Hoy camina muy raro. Lo mismo con todos los clips: solo los de Tony.
- **Asalto al salir de casa**: la animación de golpe está rota y el jugador se queda caminando sin hacer nada. A quien asaltan es a
  una persona de afuera, NO al jugador (no te quitan tu dinero). Cinemática corta: ves que lo asaltan y se van en carro. Objetivo:
  "Róbate un carro" y luego "Persíguelos". El carro de los asaltantes va demasiado lento: velocidad de persecución real.
  Bug: al subirte a un carro salió volando porque apareció una moto encima: nunca hacer aparecer vehículos encima de otro.
- **Policía**: los policías a pie están diminutos → 1.80 m. Si alguien te ataca, la policía ataca primero a quien comete el delito
  (los asaltantes), no a ti. Te dan 5 estrellas sin haber hecho nada: revisar qué sube la búsqueda. Las estrellas solo parpadean
  cuando la policía está por perderte; si no te ven un tiempo, dejas de ser buscado. Quitar el sonido de silbato.
  Las patrullas aún giran raro sobre su eje al perseguir: misma física y dirección que el carro del jugador.
- **Dron**: se puede derribar a balazos (cae, explota chico) y al derribarlo baja la búsqueda.
- **Enemigos y policías muertos se quedan tirados** (animación de muerte y cuerpo en el piso un rato), no desaparecen. Policías y
  enemigos sueltan algo (dinero, balas, a veces el arma).
- Barriles: si les disparas, explotan.
- Balas: el trazo termina donde pega o desaparece a cierta distancia; nunca queda flotando.
- **Explosiones y humo**: el humo hecho en código se ve falso. Se hacen como en los juegos: partículas con texturas de secuencia
  (flipbook) de fuego y humo pre-renderizado, humo iluminado, destello de luz, chispas, escombros, onda y marca en el piso. Usar el
  paquete gratuito oficial "Unity Particle Pack" (Tony lo baja del Asset Store) convertido a URP. Si no está, dejar listo el sistema.
- **HUD**: los gráficos deben ser los del chat de arte (H20–H26 en imageassets/ui/hud/, pedido-arte-08nov-b): barras de vida y
  escudo SIN las palabras VIDA/ESCUDO, como la captura de referencia (escudo delgado arriba, vida debajo, número al lado), marco del
  minimapa, íconos de objetos y muñequito. Cuando lleguen, sustituir; hasta entonces, el actual.
- **Mapa grande (M)**: que muestre las calles reales nuevas. Hacerlo con una captura cenital de la ciudad real con estilo de M10
  (color por barrio, contornos), con íconos encima. El minimapa igual, con el marco de H20.
- Pendientes de antes: T-721 (nombres encimados del mapa), T-722 (HUD fuera de cinemáticas, teclas reales en Opciones, pruebas rojas),
  RecorridoDemoPruebas completo, Builds/Juego.

## SESIÓN 2 — Mundo, interiores, luz (carpeta principal)
- **Postes**: no debe verse un hueco: lo que brilla es solo el cristal de la lámpara del modelo (emisivo) y la luz sale de ahí. Si el
  cristal no es una pieza o material aparte del modelo, anotarlo para que Tony lo separe en Blender y seguir.
- Luz pareja: hay zonas demasiado iluminadas y otras demasiado oscuras. Ajustar a K01 en toda la ciudad.
- Charcos: mejoraron pero pueden verse mejor (reflejo más nítido en los charcos, borde mojado, menos efecto de espejo plano).
- Taller El Canepa muy pequeño: a escala real (cabe un carro con espacio). Canepa diminuto → 1.80 m.
- **Casino VivaCoin**: siempre abierto, con puertas automáticas de cristal como de tienda (se abren al acercarte). Poner adentro los
  assets que ya existen: I11 mesas de blackjack, I12 sillas, I13 candil, I14 barra, I15 bancos, I16 arco dorado (zips en
  imageassets/modelos3d/). Meseras R01 cuando llegue su modelo.
- **Puente de Nova**: el puente que atraviesa dos edificios sin sentido se quita, o se rehace según la referencia K07 que llega del
  chat de arte (pasarela con escaleras que conecta banquetas).
- Callejones detrás de la calle de Nova rotos: cerrarlos o terminarlos (prioridad baja).
- Menú de mejora de armas de Tino: reacomodar dónde aparecen las cosas (lista de piezas, estadísticas y costo), estilo H10.
- Cuarto del jugador: se rediseña más grande según la imagen R10 del chat de arte (cuando llegue). El espejo se queda igual.
- Assets de docs/pedido-arte-08nov.md cuando lleguen.

## Para Tony (Blender / Tripo / Mixamo)
- Apuntar con la pistola se ve roto: Tony lo arregla en Blender y lo devuelve.
- Personaje nuevo (imagen PJ01 del chat de arte → Tripo → Mixamo). Personajes por sexo: en Blender después.
- Aliados nuevos (AL10–AL12 → Tripo → Mixamo). Meseras R01 → Tripo → Mixamo.
- Hélices del dron que giren (separarlas en Blender o Tripo). Carros con ruedas, puertas delanteras y sin cristal (Tripo, partes).
- Modelos de P10–P12, L15, IMP01–03, O01, O02, DR01 → Tripo.
- Bajar "Unity Particle Pack" (gratis) del Asset Store.

## Después
- Menú de inicio (mejoras). Personalización de personaje.
