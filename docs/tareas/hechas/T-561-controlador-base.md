# T-561 Controlador base humanoide para todos con override por papel (paso 2 de docs/noche-B-tony.md)
Dueño: Tony | Rama: tony/T-561-controlador-base (sale de tony/anim-noche, después de T-560) | Modelo: Sonnet 5.5
Carpetas permitidas: Assets/Arte/Personajes/, Assets/_Proyecto/Personaje/ (Visual/, Jugador/, Combate/ si es el código que dispara animaciones), Assets/_Proyecto/IA/ solo el código que dispara animaciones (AliadoAPie, PandilleroAPie, NPC, profes), Tests/*/Personaje/ y Tests/*/IA/.
No tocar: sonido, lógica de policía/tráfico, carros, mundo, UI, minimapa, CIC (Sesión A). No CreadorTodo.

## Contexto
Ya existe el sistema de T-473/T-520: VisualPersonaje (puente juego→Animator, ignora parámetros que faltan), ReglasVisual (nombres de parámetros: Velocidad, EnPiso, Apuntando, Arma, Disparo, Recarga, Golpe, Herido, KO, Cubierto, Rodar), ConstructorControladorVisual (editor), ApuntadoIK (IK de manos de emergencia, ManosPorIK) y un .controller por modelo en Assets/Arte/Personajes/<Modelo>/. Partir de ahí, sin romper los nombres que usan AnimacionJugador, PandilleroAPie y AliadoAPie.

## Qué
- Un controlador base humanoide (Assets/Arte/Personajes/Animaciones/ControladorBase.controller, hecho con código de editor) y un AnimatorOverrideController por papel (jugador, aliados, NPC, SKULLS, SNAKES, guardias, robots, profes); asignarlos a los prefabs visuales existentes en vez de los controladores sueltos.
- Locomoción: blend tree 2D con la velocidad local (VelX, VelZ) adelante/atrás/lados, idle, correr; giro en el sitio (GirarDer/Izq), frenar, salto en 3 fases (inicio, aire, aterrizaje) y caída.
- Capa de arriba con máscara de torso por arma (parámetro Arma): puños (guardia, golpe izq/der, fuerte), pistola (idle, apuntar, caminar apuntando a los lados, disparo, recarga y recarga caminando), arma larga para escopeta/fusil/sniper (idle, apuntar, apuntar caminando/corriendo, disparo caminando, recarga). Disparo y recarga sincronizados con el arma (la pistola W01 ya anima su corredera y cargador).
- Cobertura (entrar y disparar desde cobertura) con la Q existente; agachado caminando con rifle.
- Daño de frente/espalda/fuerte, caer y levantarse boca arriba/abajo, muerte adelante/atrás/desmayo según de dónde viene el golpe (al azar inyectado entre las válidas).
- IK de pies en el suelo y mirada (la cabeza voltea al objetivo o al jugador) en OnAnimatorIK.
- Apagar ApuntadoIK.ManosPorIK donde ya hay clip de apuntar.
- Velocidad de reproducción de caminar/correr ajustada a la velocidad real (sin patinar): lógica pura que se prueba en EditMode.
- Pruebas: EditMode de la lógica pura (elección de muerte/daño por dirección, escala de velocidad, estados y parámetros del controlador); PlayMode con entrada simulada que mueve al jugador y comprueba el estado de la capa base y la de arriba (caminar, correr, apuntar pistola, recargar).
- Capturas de los estados importantes en docs/capturas/anim-*.png (con código de editor en batchmode).

## Hecho cuando
Compila, pruebas nuevas y las de Personaje/IA tocadas pasan, capturas hechas.
