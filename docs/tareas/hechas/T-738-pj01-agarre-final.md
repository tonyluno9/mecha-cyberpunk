# T-738 PJ01 agarre final de la pistola (A6)
Dueño: Tony | Rama: tony/T-738-pj01-agarre-final | Integrada en tony/s1-09nov

## Problema
Con el clip puro (T-737) la mano retargeteada de PJ01 no queda como en Blender: la pistola con el agarre de agarre.txt salía 22-29 grados de la mira, con la corredera ladeada 65-80 grados y,
visto de lado, los dedos sobre la corredera.

## Qué se hizo (sin tocar huesos)
- AgarrePistolaPJ01 (Personaje/Visual, en el Animator de PJ01; lo agrega AnimacionJugador.ClipsDeTony solo si el rig tiene R_Hand): cada cuadro gira SOLO la pistola, en el pivote de la empuñadura,
  para que el cañón vaya de la boca al punto de mira con la corredera arriba. La posición no se toca: la empuñadura sigue en la palma. La mano izquierda queda como el clip (sin IK).
- Por qué no un giro fijo por clip: se midió el giro de R_Hand en el espacio del cuerpo y oscila hasta 24 grados dentro del ciclo de caminar de lado (dispersión 24.2 derecha, 19.5 izquierda,
  14-16 atrás y adelante; 2.3 quieto), así que un offset constante no baja de 3 grados. Se prefirió compensar por cuadro.
- Referencia de Tony guardada: AgarresArmas.CuaternionTony y PosicionPJ01 siguen en el nodo Pistola; con AgarrePistolaPJ01.Usar = false la pistola vuelve a ese agarre (prueba) para cuando llegue el
  export de Blender (docs/pedido-blender-09nov.md).
- ApuntadoIK expone HayMira y PuntoMira (solo lectura).
- Limitación: el retroceso del clip de Disparo ya no levanta el cañón (la pistola sigue la mira); la mano sí retrocede. Si se quiere, se le suma un pitch corto al disparar.

## Medido en Play (AgarreFinalPJ01T738Pruebas, cañón contra el punto de mira a 30 m, cámara nivelada)
| Caso | Antes (agarre.txt) a la mira / ladeo | Después a la mira (máx.) | Ladeo corredera | Empuñadura a la palma |
| --- | --- | --- | --- | --- |
| Apuntar quieto | 29.2 / 66.8 | 0.4 | 0.0 | 0.051 m |
| Disparo (primer cuadro) | 28.5 / 65.5 | 1.3 | 0.0 | 0.034 m |
| Lateral derecha | 53.5 / 78.0 | 0.7 | 0.0 | 0.051 m |
| Lateral izquierda | 80.0 / 81.4 | 0.9 | 0.0 | 0.051 m |
| Atrás | 38.1 / 80.9 | 0.6 | 0.0 | 0.051 m |
| Adelante | 22.9 / 75.2 | 0.4 | 0.0 | 0.051 m |
Pendiente: con la cámara inclinada arriba o abajo el cañón sigue la mira pero la mano no sube (el clip no tiene esa inclinación); el cuerpo sigue sin apuntar de verdad a la altura de la mira.

## Capturas (revisadas)
docs/capturas/pj01-agarre-final-{apuntar,disparo,lateral}-{frente,lado,cerca}.png. De cerca la palma queda detrás de la empuñadura y los dedos abajo/alrededor; la corredera queda libre.

## Pruebas
- PlayMode AgarreFinalPJ01T738Pruebas (3 de ángulos + 1 de la referencia de Tony); T-736, T-737, T-733, T-734, T-732, ArmaEnMano y T-681 siguen verdes (ver el reporte).
- EditMode 2117/2117.
