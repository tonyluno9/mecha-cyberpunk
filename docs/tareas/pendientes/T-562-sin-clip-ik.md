# T-562 Lo que no tiene clip: moto, rodar, variaciones de NPC y muerte del jugador (paso 3 de docs/noche-B-tony.md)
Dueño: Tony | Rama: tony/T-562-sin-clip-ik (sale de tony/anim-noche, después de T-561) | Modelo: Sonnet 5.5
Carpetas permitidas: las de T-561. No tocar la lógica de manejo de motos ni carros (Sesión A): solo la pose y el IK del conductor, enganchando al componente de moto que ya exista.

## Qué (con IK o poses en Unity, sin keyframes de cuerpo completo)
- Moto: pose sentado inclinado con IK de manos al manubrio y pies a los estribos (puntos hijos en la moto si no hay, creados por código de editor en Personaje/ o Arte/Personajes/), subir/bajar con fundido corto, esconder el caballete al manejar si es un objeto de la moto. Si no existe el componente de moto, dejar el componente de pose listo y anotarlo.
- Rodar con CTRL: dejar el giro actual hasta que llegue el clip (no tocar).
- NPC: variaciones con lo que hay (idle con desfase de tiempo y velocidad distinta por NPC, cabeza que voltea al jugador por IK tras 1 s cerca).
- Muerte del jugador: cinemática con los clips de muerte (caer, fundido, despertar en I+D+I levantándose con Dano_Levantarse_BocaArriba), enganchando a la muerte/clínica que ya existe.
- Pruebas: EditMode de la lógica pura (desfase/velocidad de idle, temporizador de mirar al jugador); PlayMode de la muerte del jugador (llega a I+D+I y pasa por el estado de levantarse).

## Hecho cuando
Compila y las pruebas nuevas y tocadas pasan.
