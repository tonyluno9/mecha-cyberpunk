# Prueba de Tony 19-nov noche (build de game feel, .sesiones/s2/Builds/Demo)

## Le gustó (NO se toca)
Menú de inicio · cómo se siente correr y caminar · sonido de pasos sincronizado y el de pisar agua · salpicaduras (mejorables) ·
indicador de dirección del golpe · golpes 1 y 2 · robar el arma al enemigo golpeado · aceleración de los carros · cristales de los carros ·
naves pasando en Nova · clínica del Doc por dentro y que te cure al morir · sonido de vida baja · el hacker te voltea a ver ·
la persecución casi imposible de escapar · el muro que rodea la ciudad · la caída de los robots de Nova · el cañón de plasma
(lo mejor del juego) · que los enemigos traigan armas mejores con el tiempo · poder matar civiles · el teléfono.

## 1. Menú y casa
- Fondo del menú: quitar casi toda la niebla, que la ciudad se vea normal.
- Música de fondo del menú (pendiente, más adelante).
- ELIMINAR el cuarto: el juego empieza sin cuarto (no se arreglan las puertas de ahí).

## 2. Cámara y combate cuerpo a cuerpo
- La cámara se sacude demasiado al golpear, al disparar y en general: quitar casi todo el temblor.
- Combo: solo golpe 1 y golpe 2; quitar el tercero (la ráfaga).
- Enemigos con más vida contra golpes: con muchos enemigos debes necesitar un arma (hoy matas a todos a golpes).

## 3. Jugador y animaciones
- Sprint distinto al trote (no solo acelerar el clip) y agregar caminar de verdad. [Blender: clips]
- Escaleras: arreglar la animación de subir y bajar. [Blender]
- Salto: que se vea como saltar pataleando en el aire. [Blender]
- Bajar de la moto (y animaciones de ese tipo): no puedes moverte hasta que termine. Revisar solo las que deben bloquear, sin romper las demás.
- Cómo te ves sentado/montado en carros y motos: arreglar.
- Pasos: un sonido one-shot por cada pisada (evento de animación) para que se sincronicen solos.

## 4. Armas y disparos
- Francotirador y subfusil no hacen daño de lejos (solo la pistola sí): arreglar el alcance/daño de todas las armas.
- Cómo se ven los balazos (trazos) está raro: mejorar.
- Apuntar granadas: mira o arco de tiro; pulir sus sonidos.
- Cañón de plasma: pulir su animación.
- Rueda de armas: sonido de cámara lenta al abrirla.
- Indicador de daño también para drones (hoy no marca de dónde dispara el dron ni se ve dónde está).
- Aviso de vida baja un poco antes.

## 5. Enemigos y Nova
- Robots y guardias de Nova: no reaccionan cuando les disparas de lejos; que te detecten, te busquen y se defiendan.
- Que usen sus armas (el arma de Nova W09_ArmaNova ya existe: renombrar a W10 e integrarla).
- Robots de Nova mucho más grandes (gigantes) y el enemigo con escudo del tamaño de los otros agentes.
- Nova debe aguantar un poco más.
- Perros robot: hoy caen y no hacen nada; darles su comportamiento (patrullar, atacar).
- Cobertura real: agacharse, cubrirse, asomarse (no solo pararse detrás de una pared).
- Animaciones propias para cada tipo de robot y NPC (caminar, atacar, morir); animación especial de la caída de Nova. [Blender]
- Drones: no atraviesan muros (físicas reales), que se vean, y sonido de dron (Tony lo hace).
- Naves de Nova: verlas en acción (hoy solo pasan lejos).
- Demasiados pandilleros aparecen de golpe en un lugar: limitar la cantidad.
- Música épica con 5 estrellas (pendiente).

## 6. NPCs
- Caminar de NPCs no es natural: pulir (y Tony quiere esa misma animación pulida para él). [Blender]
- Civiles: que sufran al recibir disparos (reacción, caer, huir) y tengan animación de muerte. Todos los NPCs con animación de muerte.
- Meseras del casino caminan al revés: corregir.
- Hacker: que programe sentado en la silla, no lejos de ella.
- Voces para todos los personajes (Doc, Tino, hacker, NPCs) — parte importante, pendiente.
- Opcional después: pantalla gris al matar a alguien (como GTA).

## 7. Implantes, objetos y UI
- Prótesis: elegir con clic y aceptar con doble clic (sin Enter).
- Bebidas energéticas y consumibles: hoy no se entiende cómo usarlos. Dos ranuras equipables (una con E y otra con C) y uso claro.
- Curarse durante la persecución: revisar (hoy no deja).
- Mejorar las interfaces más adelante.

## 8. Ciudad
- Parquecito: poner los árboles AR01/AR02, bancas PZ01, kiosco y jardineras que ya están importados.
- Calles que pierden la textura: revisar todas.
- Revisar la ciudad parte por parte: nada flotando ni hundido (incluye carros hundidos bajo el piso).
- Edificios todavía hechos con primitivos: reemplazarlos con assets (pedir los que falten).
- Charcos: mejorar el agua, con físicas al pisarla (ondas y salpicadura mejor).
- Armería: arreglar su calle, voltear los gabinetes de armas (están al revés) y arreglar sus texturas.
- Casino: poner las tragamonedas nuevas TR01/TR02.
- Muro de la ciudad: si hace falta modelo exacto, pedirlo.

## 9. Vehículos [Blender]
- Revisar TODOS los carros en Blender: llantas, puertas, cristales, que ninguno esté roto.
- Patrullas: les faltan puertas y las llantas se ven feas.
- El superdeportivo tipo Lambo está arruinado: rehacerlo.
- Algunos carros no le gustan: revisar uno por uno con Tony.
- Carros hundidos en el piso (físicas).

## Para Blender
Clips: sprint, caminar, trote de lado y hacia atrás, subir/bajar escaleras, salto pataleando, caminar de NPC, animaciones propias de
robots (centinela, pesado, perro) y caída de Nova. Revisión de todos los carros y motos (patrulla, superdeportivo).

## Para el chat de arte (si hace falta)
Edificios para reemplazar los hechos con primitivos (pedir referencia de la calle a Tony), muro perimetral de la ciudad, y lo que las
sesiones anoten en docs/assets-faltan.md.
