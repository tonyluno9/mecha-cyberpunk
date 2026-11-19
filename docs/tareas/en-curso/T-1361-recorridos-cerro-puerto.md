# T-1361: recorridos en Play por el Cerro y el Puerto

Rama tony/T-1361-recorridos. Pruebas de PlayMode con entrada simulada (teclado del Input System) que instancian SOLO el prefab de cada zona (`T1355_Cerro`, `T1357_Puerto`), sin abrir la ciudad. Sirven para encontrar atascos, caídas y saltos que no salen.

## Hecho
- Carpeta `Assets/_Proyecto/Tests/PlayMode/Personaje/` (asmdef `MechaCyberpunk.Tests.PlayMode.Personaje`: es la única que ve Personaje, Vehiculos y Progresion a la vez). Namespace `MechaCyberpunk.Tests.Personaje`.
- `RutaRecorridoT1361`: línea de puntos con largo, proyección, partir en tramos e invertir. `BaseRecorridoZonaT1361`: arma la zona, el jugador (JugadorAPie) o el carro (Carro_V09), camina, maneja, salta, trepa, junta hallazgos y capturas.
- `RecorridoCerroT1361Pruebas` (14 pruebas con escaleras y cadenas): a pie por la calle base y el zigzag en 3 partes; subir y bajar las 3 escaleras CE01; en carro la base y el zigzag en 3 partes; 6 cadenas de azoteas; PuntoTrepar de acceso.
- `RecorridoPuertoT1361Pruebas`: a pie por las 4 calles y 4 pasillos entre bloques; carro en circuito (2 mitades); trepar por Escalera_Suelo y Escalera_Fila en 4 bloques y bajar; saltar entre contenedores de 3 bloques.
- Las rutas salen de los `CalleZona` (puntos), de las escaleras `CE01_EscaleraCerro` agrupadas por z, de `Casas/Casa_n/Cuerpo` (azoteas), de `Bloque_n`, `PT01_Contenedor*` y de los `PuntoTrepar`. Nada está escrito a mano.

## Salidas
- `Logs/t1361-recorrido-<prueba>.txt`: resultado, contadores y cada hallazgo con posición.
- `docs/capturas/bugs-18nov-B/recorrido-<prueba>-<n>.png`: hasta 2 por prueba, solo de puntos malos.

## Decisiones
- El zigzag se parte en 3 por largo igual (no por vueltas) para que siga sirviendo si cambia el trazado.
- El giro del jugador usa `ControladorAPie.Empujar` (sin ratón); el avance y el salto son teclas simuladas.
- Las pruebas de carro no meten al jugador. La cámara del jugador se apaga (las zonas pesan millones de triángulos); las capturas usan una cámara aparte.
- Partes largas con `Time.timeScale = 2`. Fallan si no llegan, si hay caída al vacío, o más de 2 atascos por parte.

## Pendiente
Sin correr: las corre el orquestador por clase (`RecorridoCerroT1361Pruebas`, `RecorridoPuertoT1361Pruebas`). Unity generará los .meta.
