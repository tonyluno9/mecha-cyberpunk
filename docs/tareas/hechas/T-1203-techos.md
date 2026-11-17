# T-1203: caminar por los techos ya no te regresa a la escalera

Rama `tony/T-1203-techos`. Pedido del 17-nov, punto 4.

## Causa
- `TreparAPie` recuerda la escalera cercana (`puntoCercano`) cuando el jugador entra a su trigger y la olvida al salir (`OnTriggerExit`).
- Para subir, apaga el CharacterController. Con el collider apagado Unity ya no avisa la salida del trigger, así que arriba seguía recordando la escalera.
- Al caminar con W en el techo hacia el mismo lado que la escalera (justo hacia donde quedas mirando al subir), volvía a engancharse. La subida "llegaba a la cima" y el agarre lo regresaba a la orilla, una y otra vez.
- No era por los colliders, el step offset, la pendiente ni la navegación.

## Arreglo
- Al terminar una subida se olvida la escalera.
- Antes de engancharse se comprueba que el jugador de verdad esté junto al trigger (punto más cercano a menos de 0.35 m del centro del cuerpo). Si no, se olvida.
- Aplica a todas las escaleras y tubos, porque está en el jugador.

## Evidencia
- Prueba nueva `EscalerasTechosT1101Pruebas.Techos_CaminarDespuesDeSubir_NoRegresaNiEmpuja`: en las 30 escaleras de la ciudad sube con W simulada; luego camina 1.2 s con W, con A y con D, y se queda quieto 1 s. Falla si vuelve a engancharse, si salta más de 0.4 m en un cuadro o si quieto se mueve más de 0.1 m.
  - Sin el arreglo: fallan los 30 techos ("con w se vuelve a enganchar a la escalera").
  - Con el arreglo: 30 de 30.
- `EscalerasTechosT1101` (las demás), `TreparAPie` y `TechosS2`: 14 pasan, 1 de capturas ignorada. EditMode 2577/2577.
