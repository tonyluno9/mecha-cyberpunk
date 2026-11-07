# T-707 Patrulla física
Dueño: Tony (S1, autorizado en Trafico y Vehiculos esta noche) | Rama: tony/T-707-patrulla-fisica | Modelo: Sonnet 5.5
Depende de: T-341, T-532, T-703 a T-706

## Objetivo
Las patrullas son carros de verdad: la misma física de carro de Vehiculos que los carros del tráfico (cuerpo cinemático y colliders), con la IA de PatrullaMundo que maneja por GrafoCalles hacia el jugador. Son sólidas y robables.

## Contexto
Patrulla.prefab era cubos y un modelo V05 sin colliders. Ahora es el carro Vehiculos/Prefabs/Carro_V05.prefab con PatrullaMundo en la raíz (se conserva su referencia desde Barrio.prefab). Robarla la convierte en carro del jugador (CarroEstacionado suma calor 55, como antes). Escapar cuando te roban una patrulla y robar un carro sin patrulla cerca sin persecución se conservan: no se tocó CarroTrafico.

## Qué hacer
1. CreadorPatrullaT707 rehace Patrulla.prefab con el carro V05 (idempotente).
2. PatrullaMundo: se apoya en el suelo con un rayo, se deja robar (IMontable): al subir el jugador deja la IA, queda suelta en la escena y con física completa.
3. Mientras el jugador va en una patrulla robada, la policía lo ubica siempre (GPS): PatrullaMundo.JugadorEnPatrulla.
4. GestorPersecucion no cuenta la robada y manda otra en su lugar.

## Carpetas permitidas
Trafico/, Tests/EditMode/Trafico/ y Tests/PlayMode/Trafico/ (Tony autorizó a S1 en Trafico esta noche).
No tocar: Core/, Mundo/, ciudad.

## Criterios de aceptación
- [x] La patrulla tiene colliders sólidos, cuerpo cinemático, CarroViejo (IMontable) y CarroEstacionado
- [x] Maneja por las calles hacia el jugador y se puede subir a ella al llegar
- [x] Robada: física completa, 4 ruedas en el piso, sin salto, suelta en la escena; llega otra patrulla
- [x] Con el jugador en la patrulla robada todas las demás lo ven siempre (GPS) y no lo pierden; al bajar se apaga

## Pruebas automáticas
- EditMode: PatrullaFisicaT707Pruebas (prefab y referencia del barrio)
- PlayMode: PatrullaFisicaT707Pruebas (sólida, llega y se roba, GPS); PersecucionBarrioPruebas actualizada (ya no pide 0 colliders)

## Fuera de alcance
- Conductor visible dentro de la patrulla; policías que se bajan (T-709)
