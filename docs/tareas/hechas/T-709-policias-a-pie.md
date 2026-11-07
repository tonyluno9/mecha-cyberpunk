# T-709 Policías a pie
Dueño: Tony (S1, autorizado en Trafico, Vehiculos e IA esta noche) | Rama: tony/T-709-policias-a-pie | Modelo: Sonnet 5.5
Depende de: T-707

## Objetivo
Al llegar, la patrulla baja a sus policías (IA/Prefabs/PoliciaAPie), que se cubren detrás de ella, asoman y disparan; dispararles tiene efecto.

## Qué hacer
1. PatrullaMundo.BajarPolicias (ConfigPersecucion.PoliciasPorPatrulla = 2) a la distancia de seguimiento.
2. DatosPandilleroAPie: esPolicia (siempre en alerta, empieza cubierto, calor al golpear 8 y al matar 30).
3. PandilleroAPie: un carro (cuerpo con varios colliders) cuenta como una cubierta.
4. Sus balas usan PistolaPandillero (línea de vista T-676: paredes y todo lo sólido las paran).

## Criterios de aceptación
- [x] Se bajan por las puertas de la patrulla
- [x] Se cubren detrás de ella, asoman y disparan
- [x] Con una pared en medio no disparan ni dañan; sin ella sí
- [x] Dispararles sube la búsqueda, reaccionan y pueden morir

## Pruebas automáticas
- PlayMode IA: PoliciasT709Pruebas

## Fuera de alcance
- Que disparen al jugador que va en carro (solo a pie, como los pandilleros)
- Animación de bajarse (aparecen por la puerta)
