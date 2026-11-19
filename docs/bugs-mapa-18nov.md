# Bugs del mapa nuevo (18-nov, sesión C)

Se encontraron con dos barridos (detalle en `docs/plan-bugs-18nov.md`):
- **Barrido automático** `BarridoMapaT1360.Barrer`. Escribe `Logs/barrido-mapa.json` y `.txt`.
- **Recorridos en Play** `RecorridoZonasT1361Pruebas`. Hay seis partes, a pie y en carro. Cada parte escribe `Logs/recorrido-zonas-<parte>.json`.

Capturas en `docs/capturas/bugs-18nov/`:
- `recorrido-*`: los incidentes de los recorridos.
- `union-*-antes` y `union-*-despues`: las uniones antes y después del arreglo.
- `barrido-*`: los hallazgos del barrido automático.

Dueños:
- **A (esta sesión):** escena, Malecón, Autopista y uniones.
- **B:** prefabs del Cerro y del Puerto. Sus bugs se le pasaron directo y los está arreglando ella.

Gravedad: 4 = te caes, te atoras o no puedes salir; 3 = choque, escalón, estorbo o se ve muy mal; 2 = detalle.

## Arreglados (A)

| # | Grav. | Dónde | Bug | Causa | Arreglo | Evidencia |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | 4 | Uniones norte, sur y este, y junto al Puerto | Huecos sin piso: al salirte de la calle caías al vacío. El barrido dio 129 huecos y 419 vacíos. El carro caía 2.1 m en U4. | Al apagar los edificios de fondo de T530 (T-1358) no quedó piso debajo. | Pisos de respaldo a 5 cm bajo el nivel de calle (raíz de escena `T1365_Uniones`) en 5 franjas: norte, sur, este, puerto norte y oeste. | `recorrido-uniones-carro-2.png` (antes); segunda corrida de `Uniones_EnCarro` sin caídas. |
| 2 | 4 | Autopista, boca del túnel (-328, 10, 250) | El carro se atoraba contra una pared al entrar al túnel. | La cara del acantilado no tenía hueco en la boca. | Acantilado recortado al contorno del tubo. | `recorrido-autopista-carro-4.png` (antes); en la corrida siguiente el carro entra al túnel. |
| 3 | 4 | Malecón, escaleras a la playa | Quien bajaba a la playa no podía volver: la cima de la escalera quedaba 1.3 m bajo el paseo. El muelle tampoco se unía al paseo y al faro no se llegaba. | La escalera se anclaba por su caja visible. | Escaleras colocadas por su geometría (cima al ras del paseo, pie en la arena), escalera al muelle y camino de muelle sobre la escollera hasta el faro. | `Malecon_APie`: antes 7 "sin-camino"; ahora el NavMesh une paseo, playa, muelle y faro. |
| 4 | 4 | Calle baja de la autopista | Calle sin salida (cul-de-sac). | — | Calle oeste nueva por x = -242 que la une con la cruzada oeste de las afueras. | `AutopistaT1356Pruebas.Extremos_..._NingunoQuedaSuelto`. |
| 5 | 3 | Calle oeste (-242, 64) | Un edificio de las afueras viejas cerraba la calle nueva. | Remate de la calle cruzada oeste de T530. | `T1365.LiberarCalles` apaga lo viejo que tape una calle de zona (1 objeto apagado). | `recorrido-autopista-carro-1.png` (antes). |
| 6 | 3 | Dentro del túnel | La roca del acantilado se metía en el tubo y el carro chocaba. | Vértices de la malla del acantilado dentro del corredor del túnel. | Esos vértices subidos sobre el techo del tubo. Queda un choque: ver la tabla de B, punto 1. | `recorrido-autopista-carro-6.png`. |
| 7 | 3 | Rampa norte y calle oeste | Escalón de ~0.4 m al pasar de la calle vieja a la nueva, y canto al arrancar la rampa. | La losa generada sobresalía de y = 0. | Asfalto con la cara de arriba en y = 0 y rampa al ras los primeros 12 m. | `recorrido-uniones-carro-8.png` (antes). |
| 8 | 3 | Unión de la rampa norte con el tablero | Canto atravesado entre losas. | La rampa tocaba el tablero antes de llegar a 10 m. | La unión se movió a x = -300 y los parapetos se cortan ahí. | `AutopistaT1356Pruebas`. |
| 9 | 3 | Conector U9 (300, 0, -6) | Choque contra la banqueta del conector. | La banqueta invadía medio carril de la calle este. | Banquetas recortadas en el cruce. | `recorrido-autopista-carro` (choque en x 300). |
| 10 | 3 | Calle baja y U7 | Carros estacionados en el carril, y un carro chatarra en el enlace de la rampa. | — | Estacionados a 6.3 m del eje y nada en el enlace U7. | `OrillasT1358Pruebas` (sin pendientes en U7). |
| 11 | 3 | Plazas | La plaza de banqueta 3 estorbaba la boca de U2. | — | Movida a x ≈ 89. | `Logs/t1358-orillas.txt` sin pendientes. |
| 12 | 3 | Malecón | Bloques altos lisos de las afueras viejas a la vista. | Edificios de relleno de T530 (corredores sur). | 18 apagados (lista en `Logs/t1354-malecon.txt`); fachadas del kit de 3 a 5 pisos con neón. | Capturas `docs/capturas/zonas/malecon-*.png`. |
| 13 | 3 | Malecón | Luz azul pareja. | — | Volumen local de URP (solo cubre el Malecón; no llega a ningún punto con z > -190) con sodio cálido, y faroles más fuertes. | `MaleconT1354Pruebas` (el volumen no toca los barrios viejos). |
| 14 | 3 | Malecón | Sin ciudad al otro lado del agua. | — | Otra orilla a 330-470 m con torres T20-T23 de ventanas encendidas, pantallas, luces de azotea y franja de luces para las estelas en el agua. | `Logs/rev/sur2.png`. |
| 15 | 2 | Fotos de zona | La cámara de las fotos salía a 60 m de altura. | `DejarLibre` chocaba con los triggers del volumen y de las zonas de tráfico. | Ignora triggers. | — |

## Para la sesión B (prefabs del Cerro y del Puerto; avisados a la sesión B, que los tomó)

1. **Grave, túnel:** el terreno del Cerro (`T1355_Cerro/Terreno/Ladera`) se mete en el tubo cerca de la salida, en (-359, 10.8, 214.7). El carro choca a 15 m/s. La salida del túnel (x -368..-352, z 185..193) debe quedar libre.
2. **U4:** escalones de 0.41 m en `Calle_U4/Arroyo` (-293, 0, 0) y en `Calle_Base/Banquetas` (-312, 0.5, 3), y la escalera CE03 estorba el carril.
3. **Puerto:** la reja o caseta del norte tapa la puerta U8 (300, 0, -45), un carro se atora en el patio en (295, 0, -85) y hay un golpe al entrar.
4. **Barrido:** ~1100 piezas sin collider y ~265 casos de z-fighting en el Cerro. La sesión B además encontró cimientos sin collider, postes con collider de 7 m, carros en el carril del zigzag y escaleras que terminan 0.57 m bajo la calle.

## Abiertos (A)
- **Autopista, atorones en la rampa norte (-204, 5.6, 233) y en el tablero (-157, 10, 252):** el carro del recorrido y un carro del tráfico se atoran. El informe dice "causa físico, bloqueado False". En las capturas hay tráfico y patrullas en la rampa, así que puede ser tráfico detenido y no geometría. No está confirmado.
- **Autopista, escalón de 0.4 m dentro del túnel:** viene del punto 1 de B.
- **Plazas:** `Plaza_Kiosco` y `Plaza_Esquina` no caben sin quitar fachadas, palmeras o faroles. Solo se pusieron 3 `Plaza_Banqueta` con su piso de adoquín.
- **Falsos positivos del barrido:** la mayoría de los "enterrados" son falsos: cimientos que bajan a propósito, el espesor del tablero o columnas medidas contra el parapeto. Los "muros invisibles" del parapeto los cubre el guardarraíl visible.
