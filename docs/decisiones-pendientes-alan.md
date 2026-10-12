# Decisiones pendientes de Alan (se pasan a docs/decisiones.md al aprobar)

## Planeación de A-F1 (2026-10-11, planificador Opus 5.5, a pedido de Tony)

### D-alan-1 Pruebas_Alan.unity (T-020, T-040)
Propuesta. Pruebas_Alan.unity reemplaza a Pruebas_Compa.unity. Tiene el barrio, la cámara libre y las herramientas de prueba de
Alan, con la jaula fuera del barrio. Ciudad.unity tiene el mismo barrio sin herramientas de prueba.

### D-alan-2 Cámara libre propia (T-040)
Propuesta. Alan prueba con `Mundo/Pruebas/CamaraLibre`: CharacterController con ojos a 1.75 m, modo vuelo con F y sus propias
teclas. Cuando T-031 llega a main implementa `IJugador` (modo a pie), así las puertas y zonas de Alan se prueban sin el jugador
de Tony y funcionan igual con él.

### D-alan-3 Barrio como prefab y medidas (T-040)
Propuesta. El barrio es `Mundo/Prefabs/Barrio.prefab`, hecho por un creador de editor y colocado en Ciudad.unity y
Pruebas_Alan.unity. 3 × 2 manzanas de unos 80 m, calles de 14 m y banquetas de 3 m (caben una persona y un mecha de 4 m). Se
ajusta en A-F2 midiendo con el carro el "minuto y medio cruzando" de ciudad.md.

### D-alan-4 Reloj (T-041)
Propuesta. Amanecer a las 07:00 y anochecer a las 19:00: las 12 horas de día pasan en 8 minutos reales y las 12 de noche en 12,
así el día dura 20 minutos como dice ciudad.md. El juego empieza a las 21:00. Valores en `ConfigRelojMundo`.

### D-alan-5 Horarios de los locales (T-042)
Propuesta. Casa siempre abierta; bar de 19:00 a 04:00; clandestino de 20:00 a 06:00; casino de 20:00 a 06:00 y mercado negro de
09:00 a 18:00, pero los dos "cerrados en la demo" (demo.md): su puerta da `Bloqueada` y el letrero dice "CERRADO · PRÓXIMAMENTE".

### D-alan-6 La puerta deja salir (T-042)
Propuesta. Si el jugador está dentro de un lugar que cierra, la puerta le abre para que pueda salir.

### D-alan-7 Integración de la pelea en una tarea aparte (T-044)
Propuesta. Colocar MechaJugador y FlujoPelea en Clandestino.unity es T-044, separada de T-020, porque depende de que T-022 de
Tony esté en main y así T-020 no queda a medias esperando.

### D-alan-8 Interacción de prueba con E (T-042)
Propuesta. La cámara libre abre puertas con E (rayo de 3 m). La interacción real del jugador con puertas es de Tony (T-F3) y usa
el mismo `IPuerta`.
