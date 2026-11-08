# T-728 El asalto a un tercero: róbate un carro y persíguelos
Dueño: Tony (S1, noche 08-nov) | Rama: tony/T-728-asalto-tercero | Modelo: Sonnet 5.5
Depende de: T-717, T-723, T-726

## Objetivo
Rehacer el asalto de T-717 según el playtest del 7-nov: a quien asaltan es una persona de afuera, NO el jugador. El jugador mira una cinemática corta, luego el objetivo es "RÓBATE UN CARRO" y, ya en uno, "PERSÍGUELOS". El carro de los asaltantes va a velocidad real de persecución. Nunca se hace aparecer un vehículo encima de otro.

## Qué había
- AsaltoDinero te quitaba todo el dinero y el asaltante te caminaba a ti. Durante la cinemática el muñeco del jugador seguía "caminando" en su sitio.
- El carro huía a 10 m/s y llegaba junto al jugador; los spawners solo miraban que no hubiera otro CarroTrafico cerca (no motos, carros robados o estacionados, patrullas ni el jugador).

## Qué se hizo
- Progresion/Prologo/Pandillas: `ReglaAsalto` rehecha (la víctima pierde 300, no sale nada de tu cartera; al vencer al asaltante la víctima te da la mitad, 150; el objetivo depende de si vas en un vehículo). `AsaltoDinero`: pone una víctima (Resources/VictimaAsalto, modelo Visual_woman2) a 8 m y al asaltante 5 m detrás; el asaltante camina a la víctima, se encaran 1.6 s (sin clips nuevos), corre al carro y se va. Cámara de la cinemática centrada en la escena. Objetivos: RÓBATE UN CARRO (marcador al carro por robar más cercano, sin contar el de los asaltantes) y PERSÍGUELOS (marcador sigue al carro). Registra el punto de historia `Historia_Asalto_Escena` para que Trafico mande el carro adonde asaltan a la víctima. `DirectorPandillas` ya no guarda dineroRobado (los guardados viejos con ese campo siguen leyéndose y se les repone el dinero).
- Personaje/Movimiento/ControladorAPie: `ActivarControl(false)` hace un Move en cero. Era la causa del muñeco caminando quieto: VisualPersonaje lee CharacterController.velocity, que se quedaba con la del último Move.
- Trafico: `AsaltoMundo` velocidad de huida 14 m/s (antes 10; el tráfico va a 8-12 y el carro del jugador a 16), llegada 16, rutas de 200 a 500 m (ideal 320; si la red no da, el nodo más lejano), tope 90 s, llega a la escena de la víctima. `ConfigTraficoCalle.Crear(v, aceleracion)` y `CarroTrafico.IniciarHuida` arrancan con 7 m/s² (el tráfico 3).
- Trafico/Calle/`EspacioLibre` (OverlapBox, caja de 6.4 m sobre el piso ignorando triggers y al propio vehículo). Se usa en todos los lugares donde sale un vehículo: `CarroTrafico.Reaparecer` (pool del tráfico), `TraficoCalle.EnviarRefuerzo` (refuerzos SKULL y carro del asalto), y la salida de las patrullas (`GestorPersecucion.NodoDeSalida`). Los refuerzos a pie, la víctima y el asaltante son personas, no vehículos.
- Decisión de recompensa: la víctima te da la mitad de lo robado (150 de 300). Constantes en ReglaAsalto.

## Criterios
- [x] Al salir de casa asaltan a una persona de afuera; tu dinero no se toca en ningún momento
- [x] El jugador queda quieto (sin velocidad que mueva su animación) y sin control durante la cinemática; Enter la salta
- [x] Objetivos: RÓBATE UN CARRO a pie; PERSÍGUELOS al subir a un carro; ATRAPA AL ASALTANTE cuando el carro se detiene
- [x] El carro de los asaltantes llega a más de 12.5 m/s medidos (14 de crucero)
- [x] Al vencer al asaltante la víctima te da 150
- [x] Ningún spawner de vehículos saca uno encima de otro objeto sólido (moto, carro, jugador)

## Pruebas
- EditMode Progresion: ReglaAsaltoT728Pruebas (8). EditMode Trafico: AsaltoMundoT728Pruebas (2).
- PlayMode Progresion: AsaltoDineroT728Pruebas (4: control apagado deja velocidad cero, flujo completo con robo de carro, llanta, cancelar). PlayMode Trafico: EspacioLibreT728Pruebas (2: la caja y todos los nodos tapados con motos).
- Capturas docs/capturas/s1d-T-728-*.png (cinemática, persecución a pie, persecución en carro, asaltante bajo del carro).

## Notas y pendientes
- No hay clips para "robar" ni "manos arriba": se encaran parados con Idle. Faltan en docs/animaciones-faltan.md (T-728).
- El spawner de TraficoAutopista (Mundo, de la sesión 2) no se tocó; si hace aparecer carros, debería usar EspacioLibre.Libre.
- Al robarte un carro del tráfico cerca de gente sale búsqueda (ver T-729).
