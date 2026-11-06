# T-670 Escala del jugador a 1.80 m
Dueño: Tony | Rama: tony/T-670-escala-jugador | Modelo: Sonnet 5.5

## Objetivo
El jugador animado mide 1.80 m (como su collider) y los NPC quedan parejos con él.

## Qué se hizo
1. ColocarS2.AlturaJugador pasa de 1.40 a 1.80 y se corrigen las alturas medidas con BakeMesh de man, SkullHombre, woman2, GuardiaNova y woman; se corrió ColocarS2.Escala (escala del Giro/Modelo de cada Visual_*).
2. CubiertaYRodada reseteaba la escala del Modelo a 1 (al iniciar y al levantarse): ahora conserva la escala base del modelo.
3. Pruebas de las manos con el arma: los umbrales en metros crecen con la escala.

## Criterios de aceptación
- [x] Jugador (mujer y hombre) animado mide 1.80 m +- 0.06 (BakeMesh en Play)
- [x] NPC (Doctor, Inventor, Hacker, Robot, Skull, woman2, Guardia) parejos con el jugador
- [x] Manos en el arma, en el manubrio y los estribos de la moto, y postura de manejo del carro siguen bien (pruebas + capturas s1n-T-670-*)
- [x] Rodar/agacharse no deshace la escala

## Pruebas
- PlayMode Personaje/EscalaJugadorT670Pruebas (altura mujer, hombre, todos los personajes, escala tras rodar)
- EditMode Mundo/MundoS2Pruebas (alturas relativas)
