# T-1008: vuelo natural de los drones de policía

Rama: tony/T-1008-drones-naturales. Área: Trafico.

## Qué cambió
- El dron se movía a velocidad fija en línea recta y giraba a 180 grados/s sin inclinarse. Ahora vuela con inercia.
- `CalculoVueloDron` (lógica pura): aceleración máxima, velocidad máxima, frenado suave al llegar, rumbo con velocidad angular limitada, cabeceo y alabeo según la aceleración, flote y balanceo leves, altura con su propia aceleración.
- `DronMundo` lo usa para ir a su punto, para retirarse y para orientarse; la separación entre drones (T-938), los disparos y la caída (T-730) no cambian.
- Valores en `ConfigPersecucion` (encabezado Vuelo del dron T-1008); la velocidad máxima sigue siendo la velocidad del dron.

## Pruebas
- EditMode: `CalculoVueloDronT1008Pruebas` (12).
- PlayMode: `DronVueloT1008Pruebas` (de A a B: sin pasar de largo y sin saltos de rotación).
