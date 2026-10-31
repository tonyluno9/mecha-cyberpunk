# T-350 Ciclo completo de horarios del barrio (A-F5)
Dueño: Alan | Rama: alan/T-350-ciclo-horarios (base alan/fase-6) | Modelo: Sonnet 5.5, medium
Depende de: T-042, T-320, T-321, T-303 (todas hechas). Solo Mundo y pruebas de Mundo; no toca Core ni sistemas de Tony.

## Por qué esta tarea
La hoja de ruta (A-F5) pide "ciclo completo con todos los horarios". Hoy las pruebas solo muestrean 12:00 y 22:00 (`LugaresPruebas`, `PuertasPruebas`) y un tramo corto con el reloj rápido que cuenta cambios sin comprobarlos. No hay barrido de 24 h con Casino, Mercado negro y Clínica, ni prueba de guardar/cargar el reloj a mitad del ciclo. No se duplica nada: se agregan pruebas, la lógica ya existe.

## Qué hacer
1. `CicloHorariosPruebas` (PlayMode, Tests/PlayMode/Mundo) sobre Ciudad.unity con el reloj pausado:
   - Barrido de las 24 h en pasos de media hora: por cada lugar con horario (Casa, Bar, Clandestino, Casino, Mercado negro, Clínica) `EstaAbierto` coincide con su `HorarioLugar`, el letrero dice ABIERTO/CERRADO, y la puerta (si tiene) responde `Abierta` o `CerradaPorHorario`.
   - Horarios definidos: Casa 24 h, Bar 19 a 04, Clandestino 20 a 06, Casino 20 a 06, Mercado negro 09 a 18, Clínica 09 a 18.
   - Cada lugar avisa exactamente 2 cambios en un día (Casa 0), también por `EventosLugares`.
   - Guardar el reloj a las 18:30, avanzar a 19:30 y cargar: los lugares, letreros y puertas vuelven al estado de las 18:30 y el siguiente cruce de las 19:00 se vuelve a avisar una vez.
2. Mostrador: la Clínica conserva el mostrador de Tony colocado mientras cambia de estado (no se modifica; solo se comprueba que sigue ahí).

## Criterios
- Compilar, EditMode y PlayMode con `bash herramientas/verificar.sh todo`, 0 fallos.
- Sin cambios en Core/, carpetas de Tony, ProjectSettings/, Packages/ ni herramientas/.
