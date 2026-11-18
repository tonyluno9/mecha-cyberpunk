# T-1344: puertas y cofre de los carros que se abren

Rama de la tarea sobre T-1342 (los prefabs Carro_<cod> arman las bisagras). Sin cambios de escenas ni prefabs aquí.

## Hecho
- `Vehiculos/Carro/PuertasCarro`: bisagras `bisagraPuertaIzquierda`, `bisagraPuertaDerecha`, `bisagraCofre` (70°, 70°, 55°, 260 °/s). Izquierda +yaw, derecha -yaw, cofre -X. API: `AbrirPuerta/CerrarPuerta(bool izquierda)`, `AbrirCofre/CerrarCofre`, `PuertaAbierta`, `AperturaPuerta` (0..1), `LadoIzquierdoMasCercano`, estático `RotacionAbierta`. Animación en Update (sin corutinas); todo tolera bisagras nulas o sin hijo (tras `DanioCarro.LanzarPiezas`).
- Subir/bajar: el componente se suscribe a `IMontable.OcupadoCambio` del propio carro (sin tocar Personaje, que no referencia Vehiculos). Abre la puerta del lado de `PuntoSubida`; la cierra sola a los 1.6 s al subir y 1.2 s al bajar. Robar con conductor sube por el mismo evento, así que abre la puerta del conductor igual.
- Cofre: se abre al pasar `DanioCarro` a estado Fuego (`EstadoCambio`).

## Pruebas
- EditMode `PuertasCarroT1344Pruebas` (signos de la rotación, sin bisagras no falla).
- PlayMode `PuertasCarroT1344Pruebas` (70° y 55° tras 0.5 s, subir/bajar abre y cierra, piezas soltadas sin excepciones). Carro armado en código con un montable falso.

## Pendiente
- No se corrió Unity en esta pasada: falta ejecutar las pruebas.
- Los prefabs deben tener `PuertasCarro` en la raíz con las bisagras asignadas (lo hace T-1342).
