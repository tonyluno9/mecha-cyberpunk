# T-689 Pantalla HAS MUERTO
Dueño: Tony | Rama: tony/T-689-has-muerto | Modelo: Sonnet 5.5, medium
Depende de: T-196, T-586

## Objetivo
Al morir a pie, después de la caída (que no se cambia), sale una pantalla HAS MUERTO con el estilo del menú de inicio (Teko, neón, glitch) y luego se reaparece en la clínica con un fundido.

## Qué hacer
1. UI/Historia/PantallaAviso: pantalla por código (canvas propio bajo el fundido) con título en Teko, copias de color, cortes de glitch (RelojGlitch), halo y subtítulo.
2. DoctorClinica: tras la espera de la caída muestra la pantalla (entra desde negro, se queda), luego el fundido de siempre y el resto de la secuencia.
3. DatosDoctor: segundos de la pantalla (2.6 s), de entrada (0.9 s) y textos.

## Carpetas permitidas
UI/Historia, Progresion/Prologo/Clinica, Tests/PlayMode/Progresion/Implantes

## Criterios de aceptación
- [x] La caída se ve primero; no hay pantalla hasta que termina la espera
- [x] Sale HAS MUERTO en la fuente de títulos del juego, con el jugador aún donde cayó
- [x] Luego fundido y reaparece en la clínica con vida llena, control devuelto y la pantalla quitada

## Pruebas automáticas
- PlayMode HasMuertoT689Pruebas (ciudad de verdad); captura docs/capturas/s1t-T-689-has-muerto.png
