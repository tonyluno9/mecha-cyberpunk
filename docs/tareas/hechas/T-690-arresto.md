# T-690 Consecuencia del arresto
Dueño: Tony | Rama: tony/T-690-arresto | Modelo: Sonnet 5.5, medium
Depende de: T-661 a T-665, T-689

## Objetivo
Ser arrestado cuesta: pierdes el 25 % del dinero, sale una pantalla ARRESTADO (mismo estilo que HAS MUERTO), apareces en casa y se limpia la búsqueda.

## Qué hacer
1. Trafico/GestorPersecucion: al arrestar manda el mensaje de Unity AlSerArrestado al jugador (Trafico no puede referenciar Personaje ni UI: ReglasEnsamblados).
2. Progresion/Prologo/Policia: ConsecuenciaArresto (recibe el mensaje; control off, pantalla, multa, baja del vehículo, Casa_Puerta o Casa_Cama, control on) y ReglaMultaArresto (25 %, hacia abajo, sin deuda). El doctor la agrega al jugador al enlazarlo.
3. UI/Historia/PantallaAviso: Desvanecer.

## Carpetas permitidas
Trafico/Persecucion (solo el mensaje), Progresion/Prologo, UI/Historia, Tests

## Criterios de aceptación
- [x] Una patrulla real arresta al jugador quieto y sale ARRESTADO con la multa escrita
- [x] Pierde 25 % (1000 -> 750); sin dinero no cobra ni deja deuda
- [x] Aparece en casa, calor 0, control de vuelta, pantalla quitada con fundido

## Pruebas automáticas
- EditMode ReglaMultaArrestoPruebas; PlayMode ArrestoT690Pruebas (Tests/PlayMode/IA); captura docs/capturas/s1t-T-690-arrestado.png
