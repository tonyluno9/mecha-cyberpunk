# T-713 Bug de los SKULLS del Tianguis
Dueño: Tony (S1) | Rama: tony/T-713-bug-skulls | Modelo: Sonnet 5.5
Depende de: T-402, T-406, T-447

## Objetivo
En la misión de los SKULLS del Tianguis ("VENCE A LOS SKULLS n/7") solo salían unos cuantos, reaparecían y la misión se trababa. Salen todos a la vez, no reaparecen y al matarlos a todos la misión avanza.

## Causa (medida en Ciudad_Prologo)
- La misión pide 7 vencidos (respeto 70, 10 por SKULL) y hay 7 SKULLS en 4 grupos (3 + 2 + 1 + 1), pero cada grupo era un grupo normal de T-447: de día solo sale una fracción (34 %, mínimo uno por grupo), y se retiran al amanecer.
- Al quedar un grupo vacío reaparecía a los 40 s si te alejabas 30 m: los que mataste volvían y el conteo de la misión no cerraba.
- Un grupo que cae fuera de la zona no suma respeto, y si faltaba uno el contador no llegaba a 70.

## Qué se hizo
- PandillaAPie.ModoMision (nuevo): salen todos los del grupo a la vez de día y de noche, no se retiran al amanecer, no reaparecen al quedar vacío, y el que cayó no vuelve (ni al irte y regresar). Vencidos y TodosVencidos.
- DirectorPandillas pone ModoMision en los grupos del Tianguis y, si cayeron todos y el respeto no llegó al umbral del jefe, lo completa (la misión no se traba por faltar uno).
- No se tocó la escena: todo se decide al correr.

## Criterios
- [x] Salen los 7 a la vez, de día y de noche
- [x] No reaparecen: tras matar 3, irte 140 m y esperar 150 s de juego (a 20x), siguen 4; ni al regresar
- [x] Al matar a todos el objetivo pasa a VENCE AL JEFE; al vencer al jefe el Tianguis es tuyo (NeonAlto)

## Pruebas
- EditMode IA: PandillaModoMisionT713Pruebas (2)
- PlayMode Progresion: SkullsTianguisT713Pruebas (2: de noche y de día; F6 con teclado simulado, daño directo)

## Fuera de alcance
- Que los SKULLS lleguen en carro (T-714) o se cubran agachados (T-712)
