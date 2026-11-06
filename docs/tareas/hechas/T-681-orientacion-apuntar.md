# T-681 Orientación del arma al apuntar
Dueño: Tony | Rama: tony/T-681-orientacion-apuntar | Modelo: Sonnet 5.5

## Objetivo
Al apuntar y al disparar, el cañón de la pistola, la escopeta, el fusil y el francotirador apunta al punto de mira (centro de la cámara de juego), con la corredera arriba (ni volteada ni chueca).

## Qué se hizo
- Medido antes: con la pistola el cañón quedaba a 13° del punto de mira y ladeada (la muñeca del Humanoid no alcanza lo que pide el IK).
- ApuntadoIK recibe el punto de mira (PonerPuntoMira / QuitarPuntoMira). AnimacionJugador lo pone cada cuadro al apuntar con PistolaAPie.PuntoDeMira (el mismo rayo de la cámara que usa el disparo).
- El cañón se lleva de la boca al punto: primero con el IK de la mano derecha (AgarresArmas.CorreccionHaciaDireccion) y lo que no alcanza la muñeca se termina girando el torso entero en LateUpdate (hasta 30°), con brazos, manos y arma juntos.
- Sin punto de mira (enemigos, aliados) todo queda como antes.

## Criterios
- [x] Las 4 armas, apuntando con la cámara a 0°, -15° y 12°: ángulo del cañón al punto menor de 4° (medido 0.1 a 2.7°)
- [x] Disparando: igual (0.5 a 1.1°)
- [x] Corredera arriba (sin ladeo) y no volteada
- [x] Capturas docs/capturas/s1t-T-681-<arma>-<apuntando|disparo>-<lado|camara>.png

## Pruebas
PlayMode Personaje: CanonAlPuntoDeMiraT681Pruebas. EditMode PersonajeVisual: AgarresArmasT681Pruebas (6).
