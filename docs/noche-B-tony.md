# Noche 3→4 nov — Sesión B (computadora de Tony): ANIMACIONES
La Sesión A (computadora de Alan) hace a la par el resto de docs/pendientes-pruebas.md. Tú SOLO tocas: Assets/Arte/Personajes/ (incluye Animaciones/), Personaje/Visual/, los Animator Controllers y el código que dispara animaciones (estados de animación del jugador, aliados, NPC, pandilleros, policía y profes). No toques sonido, policía/tráfico (lógica), carros, mundo, UI, minimapa ni CIC: son de la Sesión A.
Reglas de siempre: Sonnet, rama tony/anim-noche y una rama por tarea (IDs T-560 a T-579), pruebas de lo que tocas + RecorridoDemoPruebas al final, merge a main con push al terminar cada paso (yo, Tony, lo autorizo), antes de cada merge git fetch y mezcla origin/main (conflicto en archivo de la Sesión A = quédate con origin/main), nunca force push, no CreadorTodo. Si se acaba el límite, espera y continúa.

## Paso 0
- Commit y push a main de docs/noche-A-alan.md, docs/noche-B-tony.md y docs/pendientes-pruebas.md PRIMERO (la Sesión A los espera).
- Si la rama tony/T-550-cuartos (interiores) está terminada y compila, mézclala a main; si está a medias, déjala y anótalo.

## Paso 1 — Importar las 46 animaciones de Mixamo
Origen: imageassets/animaciones/Movimiento_base.zip (13: Loco_Idle, Caminar, CaminarAtras, CaminarDer, CaminarIzq, Correr, Frenar, GirarDer, GirarIzq, SaltoInicio, SaltoAire, SaltoAterrizaje, Caida) y Combate_a_pie.zip (33: Pistola_*, Rifle_*, Punos_*, Cobertura_*, Dano_*, Muerte_*, Rifle Crouch Walk). Descomprime en Temp/, copia a Assets/Arte/Personajes/Animaciones/<Grupo>/, quita los puntos dobles de los nombres ("..fbx"). Humanoid, sin root motion salvo caídas/muertes si conviene, bucle en idle/caminar/correr/apuntar-caminar, In Place.

## Paso 2 — Controladores
Un controlador base humanoide para TODOS (jugador, aliados, NPC, SKULLS, SNAKES, guardias, robots, profes) con override por papel:
- Locomoción con blend tree 2D (adelante/atrás/lados según la velocidad local), giro en el sitio, frenar, salto en 3 fases y caída.
- Capa de arriba (máscara de torso) por arma: puños (guardia, golpe izq/der, fuerte), pistola (idle, apuntar, caminar apuntando a los lados, disparo, recarga y recarga caminando), arma larga para escopeta/fusil/sniper (idle, apuntar, apuntar caminando/corriendo, disparo caminando, recarga). Sincroniza disparo y recarga con el arma (la pistola W01 ya tiene sus propias animaciones de corredera y cargador).
- Cobertura (entrar y disparar desde cobertura) con la Q existente; agachado caminando con rifle.
- Daño de frente/espalda/fuerte, caer y levantarse boca arriba/abajo, muerte adelante/atrás/desmayo (al azar según de dónde viene el golpe).
- IK de pies en el suelo y mirada (la cabeza voltea al objetivo / al jugador).
- Quita el IK de manos de emergencia cuando ya haya clip de apuntar (ApuntadoIK.ManosPorIK).
- Velocidades de animación que coincidan con la velocidad real (sin patinar).

## Paso 3 — Lo que no tiene clip todavía (hazlo con IK o poses en Unity, no keyframes de cuerpo completo)
- Moto: pose sentado inclinado con IK de manos al manubrio y pies a los estribos, subir/bajar con fundido corto; esconde el caballete al manejar. Esto destraba subirse a las motos (la lógica de manejo de motos la tiene la Sesión A: coordina por el componente que ya exista, no lo dupliques).
- Rodar con CTRL: hasta que llegue el clip, deja el giro actual.
- NPC: variaciones con lo que hay (idle con desfase y velocidad distinta, voltear la cabeza por IK hacia el jugador tras 1 s).
- Muerte del jugador: usa los clips de muerte para la cinemática (caer, fundido, despertar en I+D+I levantándose con Dano_Levantarse_BocaArriba).

## Paso 4 — Lista de lo que falta para la compañera
Escribe docs/animaciones-faltan.md al día: solo lo que NO cubren estos 46 clips, con el nombre exacto de Mixamo y el nombre de archivo que debe llevar para que entre solo (mínimo: Forward Roll, Sitting Typing / Texting, Talking, Leaning on wall, Looking Around, Terrified / Running scared, Sitting idle, Phone (sacar y hablar), Victory/Cheer, Car enter/exit si existen, cualquier "Motorcycle" que exista).
Capturas de cada estado importante en docs/capturas/anim-*.png. Reporte corto al final en docs/estado-tony.md.
