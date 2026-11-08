# T-892: meseras R01 en el casino (parte de B6)

Rama `tony/T-892-meseras-casino`. Colocación: `ColocarS2pMeseras.Todo`. Código: `Assets/_Proyecto/Casino/Meseras/`.

## Qué quedó
- R01 importada como Humanoid (el esqueleto es `mixamorig`) con el flujo de los demás personajes; toma Idle y Caminar de woman2.
- 3 meseras con rondas de ida y vuelta por los pasillos, sin atravesar mesas ni columnas (cada tramo revisado con una cápsula), con pausa en cada punto.
- Altura animada 1.65 m, medida con BakeMesh, con los pies en el piso del casino.

## Pendiente de arte
- La charola viene en la misma malla de la mano: se balancea con el brazo al caminar. Haría falta separarla o una animación de cargar charola.

## Evidencia
- EditMode: 2121 de 2121.
- PlayMode: `MeserasCasinoS2pPruebas`, `CasinoPuertaS2oPruebas` y las de Casino: 23 de 23.
- Capturas revisadas: docs/capturas/s2p-b6-casino.png (editor, por eso en pose T) y s2p-b6-casino-play.png (caminando en Play).
