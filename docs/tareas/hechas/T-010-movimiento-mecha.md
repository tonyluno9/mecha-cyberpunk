# T-010 Movimiento del mecha en primera persona
Dueño: Tony | Rama: tony/T-010-movimiento-mecha | Modelo: Sonnet 5.5, medium
Depende de: T-001

## Objetivo
El jugador maneja un mecha greybox con WASD y el ratón, y se siente pesado: arranca y frena con inercia y su cuerpo
gira con una velocidad máxima. Es la base de la cabina, del golpe y de la pelea.

## Contexto
- Diseño: docs/diseno/combate.md (con vida baja el mecha va más lento; eso es de otra fase).
- Decisiones: D-005 (datos), D-008 (entrada), D-014 (escala), D-015 (CharacterController).
- Contratos de Core que usa: `DatosMecha` (VelocidadMaxima, Aceleracion, Desaceleracion, VelocidadGiroMaxima).
- Esta tarea crea la escena Pruebas_Tony.unity y el prefab MechaJugador.prefab, que usan T-011, T-012, T-013 y,
  al final, el compañero en Clandestino.unity (T-022).

## Qué hacer
1. Crea `Escenas/Pruebas_Tony.unity`: un piso de 40 × 40 m, una luz direccional y unos 8 cubos de 2 a 4 m repartidos
   como referencia para sentir la velocidad y chocar.
2. Crea `Mecha/Entrada/ControlesMecha.inputactions` con un mapa "Mecha" y cuatro acciones:
   Mover (WASD, Vector2 compuesto), Mirar (delta del ratón), Golpear (clic izquierdo) y Bloquear (clic derecho).
   Golpear y Bloquear las usa T-012; aquí solo se crean. No generes clase C# del asset; los componentes reciben
   `InputActionReference` en el inspector.
3. Lógica pura en `Mecha/Movimiento/`:
   - `MovimientoPesado`: recibe la entrada (Vector2), la velocidad horizontal actual, `dt` y los valores de `DatosMecha`,
     y devuelve la nueva velocidad. Acelera hacia la velocidad objetivo con `Aceleracion`, frena con `Desaceleracion`
     cuando no hay entrada, nunca pasa de `VelocidadMaxima` (tampoco en diagonal) y nunca se pasa de 0 al frenar.
   - `GiroPesado`: recibe el giro pedido por el ratón acumulado, `dt` y `VelocidadGiroMaxima`, y devuelve cuánto gira
     el cuerpo este frame sin pasar del máximo. El giro que no alcanzó a hacerse se queda pendiente para los siguientes
     frames, así un movimiento rápido del ratón se reparte en el tiempo.
4. `Mecha/Datos/ConfigControlesMecha.cs` (ScriptableObject) con la sensibilidad del ratón, el límite de mirar arriba y
   abajo (±35°) y el cabeceo de la cámara al caminar (amplitud y pasos por segundo). Crea su asset
   `Mecha/Datos/ConfigControlesMecha_Base.asset`.
5. Crea `Mecha/Datos/DatosMecha_Jugador.asset` con los valores iniciales de `DatosMecha`.
6. `Mecha/Movimiento/ControladorMecha.cs` (MonoBehaviour, requiere CharacterController): lee Mover y Mirar,
   usa `MovimientoPesado` y `GiroPesado`, aplica gravedad para quedarse en el piso, el ratón horizontal gira el cuerpo
   y el vertical inclina solo la cámara dentro del límite. Aplica el cabeceo de cámara proporcional a la velocidad.
   Al dar Play bloquea el cursor; con Escape lo libera y con un clic lo vuelve a bloquear.
   La tecla Escape va como acción en el mismo asset de entrada (agrega "LiberarCursor").
7. Prefab `Mecha/Prefabs/MechaJugador.prefab`: una cápsula de unos 4 m de alto y 1 m de radio, con CharacterController
   a la medida, `ControladorMecha` y una cámara hija a unos 3.5 m del piso (D-014). Ponlo en Pruebas_Tony.unity y
   quita la cámara que trae la escena.

## Carpetas permitidas
Assets/_Proyecto/Mecha/, Assets/_Proyecto/Tests/EditMode/Mecha/ y Assets/_Proyecto/Escenas/Pruebas_Tony.unity
No tocar: Core/, Combate/, UI/, Arena/, IA/, otras escenas, ProjectSettings/ ni el InputSystem_Actions.inputactions de la plantilla.

## Criterios de aceptación (se comprueban jugando)
- [ ] Con WASD el mecha camina en las cuatro direcciones y en diagonal no va más rápido
- [ ] Al soltar W el mecha tarda un momento en detenerse en vez de frenar en seco
- [ ] Si muevo el ratón muy rápido a un lado, el mecha gira con una velocidad máxima y termina el giro poco después
- [ ] Mirar arriba y abajo se detiene a unos 35°
- [ ] Al caminar la vista se mece un poco al ritmo de los pasos, y quieto deja de mecerse
- [ ] No atravieso los cubos ni caigo a través del piso
- [ ] Si cambio `velocidadMaxima` en DatosMecha_Jugador.asset durante Play, el mecha cambia de velocidad
- [ ] Escape libera el cursor y un clic lo vuelve a bloquear
- [ ] No aparecen errores en la consola en 2 minutos de caminar y girar

## Pruebas automáticas
EditMode, en Tests/EditMode/Mecha/:
- MovimientoPesado: acelera sin pasar de la velocidad máxima; en diagonal no pasa de la máxima; sin entrada frena hasta 0
  sin quedar en negativo ni invertir la dirección; con `dt` de 0 no cambia la velocidad.
- GiroPesado: nunca gira más que `VelocidadGiroMaxima * dt` en un frame; el giro pendiente se completa en los siguientes
  frames; un giro pequeño se completa en un solo frame.

## Fuera de alcance
- Cabina y brazos (T-011), golpe y bloqueo (T-012), vida (T-013)
- Correr, saltar, agacharse, sonido de pasos
- Mecha más lento con vida baja
