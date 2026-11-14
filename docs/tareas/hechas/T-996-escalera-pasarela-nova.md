# T-996 La escalera de la pasarela de Nova

Pendiente del reporte 11-nov-B: "sin probar a mano: subir la escalera de la pasarela PS01 en su nueva posición" (T-930).

## Qué estaba mal
- El primer peldaño del modelo PS01 queda a 0.92 m de la banqueta (el resto sube 0.28 m por peldaño). El jugador a pie solo sube 0.35 m: **no se podía subir** ninguna de las 4 escaleras (2 pasarelas x 2 lados); el jugador se quedaba parado al pie.
- Props tapaban el pie de las escaleras: neones N07 (3), cargadores CY10 (3) y huacales C17 (1) de Neón Alto y Barrio.
- Lo demás estaba bien: el barandal del tablero no se atraviesa (empujando hacia los 4 lados el jugador no sale) y el tablero cruza de una escalera a la otra.

## Qué se hizo
- `Assets/Arte/Editor/ColocarT996.cs` (`ColocarT996.Escaleras`, idempotente, guarda la escena): una rampa invisible (BoxCollider, hija `T996_RampasEscalera` de cada PS01) de la banqueta al primer peldaño en cada escalera, y apaga los props chicos que caen en la entrada.
- `Assets/_Proyecto/Tests/PlayMode/Mundo/PasarelaNovaT996Pruebas.cs`: sube con W por cada escalera de cada pasarela, cruza el tablero y baja por la otra; empuja de lado en el tablero; pie de cada escalera libre (OverlapCapsule); capturas con `T996_CAPTURAS=1`. El asmdef de pruebas de Mundo ahora referencia Personaje y Progresion.
- Ciudad_Prologo.unity: solo las rampas y los 7 props apagados.

## Criterios
- [x] Se sube por las 4 escaleras con entrada simulada (W) hasta el tablero (+-0.3 m): PasarelaNovaT996Pruebas.
- [x] Se cruza el tablero y se baja por la otra hasta la banqueta.
- [x] El barandal no se atraviesa empujando de lado.
- [x] Nada tapa el pie de las escaleras.
- [x] Capturas revisadas en docs/capturas/t996/ (pasarela-1 a 4, pasarela-pie-1 y 2): escalera libre y completa, tablero con barandal a los dos lados.
- [x] PuentesS2tPruebas 1/1, PiezasCalleT910Pruebas EditMode 6/6 y PlayMode 3/3, FlotantesS2tPruebas 1/1, SinGreyboxT506Pruebas 9/9, PasarelaNovaT996Pruebas 3/3 (+1 de capturas ignorada sin la variable). `verificar.sh compilar` OK.
