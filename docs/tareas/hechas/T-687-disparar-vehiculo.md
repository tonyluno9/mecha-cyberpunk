# T-687 Disparar desde el carro y la moto
Dueño: Tony | Rama: tony/T-687-disparar-vehiculo | Modelo: Sonnet 5.5, medium
Depende de: T-445 (disparo desde el carro), T-660 (conductor de moto)

## Objetivo
Manejando un carro o una moto, el clic derecho deja apuntar libre con el ratón (hacia los lados incluso) y el jugador saca la pistola; el clic izquierdo dispara hacia donde se apunta. Manejar, reversa y subir y bajar no cambian.

## Qué se hizo
- Personaje/Jugador/ApuntarDesdeVehiculo.cs (lo agrega JugadorAPie): con el clic derecho apretado en un vehículo, la cámara deja de seguir al vehículo y apunta libre (ratón, con la sensibilidad de las opciones); el conductor gira en el asiento hacia donde apunta (hasta 100° en carro, 90° en moto), la pistola sale (pose de apuntar + ApuntadoIK hacia el punto de mira) y se dibuja una mira en el centro. Corre después de la cámara del vehículo para tener la última palabra del cuadro; al soltar todo vuelve.
- Carro: cámara detrás del hombro sobre el techo (desde dentro la carrocería tapa todo); moto: cámara en la cabeza del conductor y la mano derecha suelta el manubrio (PoseMoto.ManoDerechaLibre), la izquierda sigue en él.
- El disparo es el de T-445 (PistolaAPie): sale del asiento hacia donde mira la cámara; no se tocó.

## Criterios
- [x] Clic derecho: apunta libre; el ratón gira la mira unos 90° a un lado
- [x] Clic izquierdo dispara hacia ese lado y le pega a lo que hay ahí
- [x] Al soltar vuelve la cámara del vehículo
- [x] Seguir manejando mientras se apunta, reversa y bajarse funcionan

## Pruebas
- PlayMode: Tests/PlayMode/Personaje/ApuntarVehiculoT687Pruebas.cs (carro viejo y moto V07, entrada simulada). Sin prueba EditMode: no hay reglas puras nuevas.

## Pendiente / para arte
- En el carro el vidrio del modelo es opaco: la pistola no se ve desde fuera; hace falta vidrio transparente o ventana abierta.
