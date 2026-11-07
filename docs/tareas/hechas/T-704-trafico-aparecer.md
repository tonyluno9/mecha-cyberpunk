# T-704 Tráfico: aparecer y borrarse fuera de la vista, y pegarse al suelo
Dueño: Tony | Rama: tony/T-704-trafico-aparecer | Modelo: Sonnet 5.5

## Objetivo
Los carros del tráfico nacen y se borran solo fuera de la vista de la cámara y lejos del jugador, con un ciclo normal (pool), y no flotan ni se hunden.

## Decisiones
- Fuera de la vista = fuera del frustum real de Camera.main (GeometryUtility, con la caja del modelo y 3 m de margen), no un punto en la pantalla con alcance de 160 m como antes. Lejos del jugador sigue siendo 45 m.
- Pool: TraficoCalle ya arma todos los carros al inicio y los prende y apaga según día o noche. Ahora un carro solo se prende si Reaparecer encuentra un nodo oculto y lejos (si no hay, espera al siguiente cuadro) y solo se apaga si no se ve; si se ve, sigue su camino y se apaga cuando salga de la vista. No se crea ni se destruye nada al correr.
- Reaparecer (atorones y final de calle en las afueras) ahora también exige que el carro no se vea desde donde está. Efecto: un carro atorado dentro de la vista ya no se reubica a la fuerza a los 30 s (antes sí, y se veía aparecer en otra parte); y al final de una calle que se ve, da la vuelta en lugar de desaparecer.
- Suelo: cada carro mide cuánto quedan sus ruedas abajo de su raíz (la malla más baja) y la raíz va a esa altura sobre el suelo que ve un raycast hacia abajo (ignora el propio carro, cuerpos con Rigidbody y gente). El rayo se tira al nacer y cada 0.3 s, y la altura sigue al suelo suavemente (ya no es un 0.3 fijo para todos los modelos).
- CarroEstacionado (Vehiculos): si sus ruedas flotan más de 0.15 m sobre el suelo, se baja con un raycast, al nacer en la escena y 1.5 s después de que el jugador lo deja (estacionar), si sigue quieto. Un carro que ya toca no se mueve. (La lógica de medir el suelo está repetida en Trafico y Vehiculos porque las dos áreas no se referencian.)

## Criterios
- [x] Con la cámara fija y el ciclo día/noche, ningún carro nace, se borra ni se teletransporta dentro del frustum (PlayMode, ve 9 nacimientos, 9 borrados y 9 saltos)
- [x] El pool no crea ni destruye carros
- [x] Todos los modelos del tráfico quedan con las ruedas sobre el suelo, aunque el suelo esté a 0.35 m (PlayMode)
- [x] Al subir a una plataforma, el carro sigue el suelo nuevo (PlayMode)
- [x] Carros estacionados que flotan se bajan al nacer y al estacionar (PlayMode, 6 modelos)
- [x] Captura: docs/capturas/s1c-T-704-carro-sobre-el-suelo.png

## Pruebas
- PlayMode Trafico: AparecerTraficoT704Pruebas (4). PlayMode Vehiculos: CarroSueloT704Pruebas (3).
