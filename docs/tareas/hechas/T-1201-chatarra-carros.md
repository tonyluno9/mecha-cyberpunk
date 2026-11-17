# T-1201: el carro que explota queda como chatarra

Rama `tony/T-1201-chatarra-carros`. Pedido del 17-nov, punto 2.

## Causa
- El carro del jugador y la patrulla apagaban su script de manejo al explotar: sin suspensión ni agarre, el cuerpo seguía rodando y se deslizaba con la inercia.
- Los carros de tráfico (cinemáticos, los mueve `CarroTrafico`) no se enteraban de la explosión y seguían su ruta.
- El tráfico apaga y vuelve a prender sus carros; al apagarse, el humo y el fuego se detenían y no volvían a arrancar: los carros de tráfico explotaban sin fuego ni humo.
- Solo se ennegrecía la malla "Carroceria", que los carros de tráfico no tienen.

## Qué se hizo
- `DanioCarro`: al explotar se frena en seco (solo el salto del estallido) y, en cuanto casi no se mueve medio segundo, queda fijo (cuerpo cinemático, `ChatarraFija`).
- Arden las llamas medianas del pack sobre el motor. El fuego se apaga en el primer 70% de los 25 s que arde y el humo baja poco a poco hasta el final.
- El sistema de partículas se reanuda si estaba detenido; la chatarra se ennegrece entera.
- `CarroTrafico.AlExplotarVehiculo`: deja de avanzar (`Destruido`).

## Evidencia
- `ChatarraCarroT1201Pruebas` 2/2: a 18 m/s avanza menos de 2.5 m tras el estallido y luego no se mueve; el de tráfico no se mueve en 6 s y a los 6 s tiene fuego (36 partículas), humo (174) y llamas.
- Regresión: DanioCarro, ExplosionCarro, ExplosionAreaT692, FuegoCarroT932, LucesCarro, EfectosCombateT745, MotoHumoT695, ChoqueTraficoT703, TraficoCalle, RobarConConductorT705, PatrullaLlegaT708: todas pasan. EditMode 2577/2577.
- Capturas en `docs/capturas/t1201/`: `chatarra-ardiendo-6s` (negra, con llamas y humo), `chatarra-ardiendo-20s` (sin fuego, con humo) y `chatarra-trafico-*` (la de tráfico, negra y quieta en la calle).

## Notas
- En las capturas de la ciudad con la cámara del juego no salen las partículas (en batchmode); las cuentas de partículas sí las confirman. Revisarlo jugando.
