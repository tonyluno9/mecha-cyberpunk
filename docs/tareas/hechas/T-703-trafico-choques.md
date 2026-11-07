# T-703 Tráfico: choques y velocidades
Dueño: Tony | Rama: tony/T-703-trafico-choques | Modelo: Sonnet 5.5

## Objetivo
Los carros del tráfico que chocan se quedan detenidos unos segundos y el conductor reacciona (claxon, quieto y luego sigue, o se baja enojado si fue fuerte). Los carros no van demasiado lento: cada uno tiene su velocidad dentro de un rango normal.

## Decisiones
- Un golpe se detecta con OnCollisionEnter del carro de tráfico (el carro del jugador o una patrulla son cuerpos dinámicos; el tráfico es cinemático). Rapidez relativa < 3 m/s es un roce y no hace nada; 3 a 9 m/s es leve; 9 o más es fuerte (ReglaChoqueTrafico).
- Leve: se para en seco, pita de inmediato, 4 s quieto y sigue. Fuerte: igual, pero 9 s quieto, y el conductor (prefab Peaton) se baja, mira a quien chocó y le reclama con el gesto Burla cada ~2 s; a los 6 s vuelve al carro (desaparece) y el carro arranca poco después. Elegí "se baja enojado" (no "huye") porque huir es del robo.
- Un choque no cuenta como atorón (no dispara Destrabar/Reaparecer) y el mismo golpe no reacciona dos veces (1.5 s de descanso).
- Velocidades: antes todos iban a 7 m/s (25 km/h) y frenaban al pie de cualquier cosa. Ahora ConfigTraficoCalle trae velocidadMinima 8 y velocidadMaxima 12 m/s (29 a 43 km/h) y cada carro toma una al azar (CarroTrafico.VelocidadPropia). La aceleración, el frenado y la distancia de parada no cambian.
- Trafico.asmdef ahora referencia MechaCyberpunk.Personaje.Visual (mismo dueño) para el gesto del conductor.

## Criterios
- [x] Un choque leve deja al carro quieto unos segundos, pita y luego sigue (PlayMode)
- [x] Un choque fuerte baja al conductor, se queda junto al carro reclamando y vuelve (PlayMode)
- [x] Un golpe real de un cuerpo pesado hace reaccionar al carro (PlayMode)
- [x] Ningún carro del tráfico va por debajo de la velocidad mínima y no todos van igual (PlayMode y EditMode)
- [x] Captura: docs/capturas/s1c-T-703-conductor-enojado.png

## Pruebas
- EditMode: ChoqueTraficoT703Pruebas (7). PlayMode: ChoqueTraficoT703Pruebas (5).
