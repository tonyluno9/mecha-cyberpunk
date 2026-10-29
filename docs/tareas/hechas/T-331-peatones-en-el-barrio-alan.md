# T-331 Peatones en el barrio (A-F5)
Dueño: Alan | Rama: alan/T-331-peatones | Modelo: Sonnet 5.5, medium
Depende de: T-330, T-080 (banquetas), T-041 (franja del día).

## Objetivo
Entre 12 y 30 peatones low-poly (cuerpo de cubos, chamarra con neón) caminan por las banquetas: más de noche en el centro, menos de día, se detienen ante el jugador y no atraviesan nada.

## Qué hacer
`Trafico/Peatones/PeatonMundo.cs` y `Trafico/Editor/CreadorPeatones.cs`: rutas por las banquetas de Barrio.prefab (puntos por manzana), densidad por `FranjaDia`, se detienen a 2 m del jugador. Sin colliders pesados. Se agrega a `CreadorTodo`.

## Carpetas permitidas
Assets/_Proyecto/Trafico/, Assets/_Proyecto/Mundo/ (solo colgar el grupo del barrio), Tests de Trafico y Mundo, docs/capturas/. No tocar Core/ ni carpetas de Tony.

## Criterios de aceptación
- [x] En 60 s de juego ningún peatón sale de la banqueta ni atraviesa edificios (muestreo de posiciones)
- [x] La cuenta viva cambia con la franja (más a las 22:00 que a las 12:00)
- [x] Ningún cuadro de más de 100 ms con 30 peatones; capturas: docs/capturas/T-331-1..2.png
- [x] `bash herramientas/verificar.sh todo` pasa

## Fuera de alcance
Diálogos, pandillas, reacción al combate.
