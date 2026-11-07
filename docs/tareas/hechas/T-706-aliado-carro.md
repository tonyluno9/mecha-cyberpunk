# T-706 El aliado se sube a tu carro por la puerta del copiloto
Dueño: Tony | Rama: tony/T-706-aliado-carro | Modelo: Sonnet 5.5

## Objetivo
El aliado que te sigue se sube a tu carro caminando hasta la puerta del copiloto, con la misma animación de subir del jugador (Carro_Entrar), sin volar ni teletransportarse, y se baja contigo por la misma puerta (Carro_Salir). En la moto no sube; si el carro ya tiene aliado en ese asiento o no hay lugar, te sigue a pie.

## Decisiones
- Componente nuevo AliadoEnCarro (IA/Aliados), lo agrega AliadoAPie. Reglas puras en ReglaAbordarAliado (EditMode): sube si te sigue, no es moto, hay lugar, va a menos de 14 m del carro y el carro va a menos de 3 m/s; si mientras camina a la puerta el carro pasa de 5 m/s o tarda más de 7 s, se rinde y te sigue a pie.
- Puerta del copiloto = el espejo de la del conductor (igual que ya hace el jugador con PuertasMontable; IA no puede referenciar Personaje, así que la fórmula está repetida en ReglaAbordarAliado). Camina corriendo a esa puerta, juega Carro_Entrar y se desliza al asiento del copiloto; al terminar se oculta (queda "a bordo", como antes) y su posición sigue al asiento. Decisión: dentro del carro va oculto, no sentado a la vista (sentarlo con la altura correcta de cada carro es lo que hace AnimacionCarro con mucho cálculo; si se quiere, es otra tarea).
- Al bajar tú (Modo APie), el aliado reaparece en el asiento, juega Carro_Salir hasta la puerta y sale al piso 0.9 m afuera, y sigue su rutina normal.
- Una plaza: la moto (el asiento tiene PuntosMoto) no se sube. En un carro hay una sola plaza de copiloto: el segundo aliado te sigue a pie. No hay dato de plazas en el contrato (IMontable), así que "1 plaza" = moto; otros vehículos de una plaza habría que marcarlos.
- Lo de antes se conserva: con el mecha, o si no se encuentra el carro (pruebas con un jugador falso en modo Carro), el aliado va contigo oculto de inmediato (AliadosPruebas T-630 sigue pasando). Con el aliado en espera (F) no sube.
- AliadoAPie.PonerAbordo pasó de private a internal para que AliadoEnCarro lo use.

## Criterios
- [x] Subes, el aliado camina a la puerta del copiloto (lado opuesto al conductor), juega Carro_Entrar y queda dentro (PlayMode, entrada simulada)
- [x] No vuela: su altura máxima mientras sube es menor de 1.2 m
- [x] Manejas 2 s y el aliado va con el carro
- [x] Frenas, bajas con E y el aliado baja contigo con Carro_Salir, con los pies en el piso y junto a ti
- [x] En la moto no sube; con dos aliados solo sube uno (PlayMode)
- [x] Capturas: docs/capturas/s1c-T-706-aliado-camina-a-la-puerta.png, s1c-T-706-aliado-subiendo.png, s1c-T-706-aliado-bajando.png

## Pruebas
- EditMode: ReglaAbordarAliadoPruebas (4). PlayMode IA: AliadoCarroT706Pruebas (3) y AliadosPruebas (T-405 y T-630) siguen pasando.
