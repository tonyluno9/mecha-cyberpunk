# T-710 Dron dispara
Dueño: Tony (S1, autorizado en Trafico, Vehiculos e IA esta noche) | Rama: tony/T-710-dron-dispara | Modelo: Sonnet 5.5
Depende de: T-707

## Objetivo
El dron de la policía dispara de verdad: suena el disparo, deja un trazo finísimo y cortísimo (T-676), silba la bala que pasa cerca y hace daño real.

## Qué hacer
1. DronMundo: trazo, sonido ARM_PIS_DISPARO, ruido de disparo, desvío de la bala que falla, silbido ARM_BALA_ROZA a 2.5 m del jugador, cubierta del jugador (IProtegeDeBalas).
2. ARM_BALA_ROZA nuevo (WAV provisional, catálogo, encendido por defecto; T-620 pasa de 24 a 25 IDs).

## Criterios de aceptación
- [x] Acierta: daño real y trazo de 0.006 m y 6 m máximo
- [x] Falla: la bala pasa cerca y suena el silbido

## Pruebas automáticas
- EditMode: CalculoTiroDronT710Pruebas
- PlayMode: DronDisparaT710Pruebas

## Fuera de alcance
- Sonido final de la bala rozando (hoy provisional)
