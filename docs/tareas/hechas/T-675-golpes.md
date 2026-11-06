# T-675 Golpes sin spam y reacción corta
Dueño: Tony | Rama: tony/T-675-golpes | Modelo: Sonnet 5.5

## Objetivo
No se puede dar el siguiente golpe hasta que termine la animación del anterior. La reacción al recibir un golpe es corta y se corta con un golpe o con un esquive.

## Qué se hizo
- VisualPersonaje: GolpeEnCurso (animación de puños sin terminar, con tope de 1.5 s para no trabarse), ReaccionEnCurso y CortarReaccion().
- GolpeAPie no arranca un golpe mientras GolpeEnCurso (con más nivel de puños la recarga era menor que la animación y se cortaba).
- Reacción al daño más corta: rapidez 2.6x y sale a 55 % del clip (ConstructorControladorBase, controlador reconstruido). Aplica a todos los personajes con el controlador base.
- AnimacionJugador: el esquive (rodar) corta la reacción; un golpe ya la cortaba (AnyState).
- Enemigos a pie: su melee y su daño no dependen de la animación (Herir es solo visual) y la reacción sale sola; prueba de 30 heridas seguidas.

## Criterios
- [x] Con clic repetido cada golpe empieza después de acabar la animación del anterior y no se traba
- [x] La reacción dura menos de 0.9 s
- [x] Un golpe o un esquive la cortan
- [x] Tras muchas heridas nadie queda trabado

## Pruebas
PlayMode Personaje: GolpesT675Pruebas (3), GolpeAPiePruebas. EditMode PersonajeVisual: ReaccionDanoT675Pruebas (3). EditMode total 1738/1738.
