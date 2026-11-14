# T-993: armas grandes en el holograma del banco de Tino

Causa: el arma se escala a 1 m de largo y la cámara estaba a 3.5 m (config) con campo de visión 28°. A esa distancia el ancho visible
es ~2.8 m, así que el arma ocupaba ~36-39 % del ancho. El panel además recorta la textura arriba y abajo (92.4 % del alto) y la
distancia era fija, sin mirar el tamaño real del arma.

Arreglo: `VistaHologramaArma.EncuadrarArma` proyecta las mallas con la cámara (de lado, inclinación inicial) y ajusta la distancia hasta
que el arma ocupe `LlenadoAncho` (0.80) del ancho o `LlenadoAlto` (0.75) del alto del panel, lo que limite primero. La rueda ahora va de
`DistanciaEncuadre * ZoomMinimo (0.6)` a `DistanciaEncuadre * ZoomMaximo (1.5)`. `PantallaMejoraArmas` le dice a la vista qué fracción
del alto de la textura muestra el panel (`FijarFraccionAltoVisible`). `ConfigMenuArmas`: quitados distancia, distanciaMinima y
distanciaMaxima; nuevos llenadoAncho, llenadoAlto, zoomMinimo y zoomMaximo (valores por defecto, el asset no cambió).

## Medidas (fracción del panel, mallas proyectadas con bounds locales; el alto es algo mayor que el real)

| Arma | Antes ancho / alto | Después ancho / alto | Distancia antes -> después |
| --- | --- | --- | --- |
| Pistola | 0.385 / 0.630 | 0.472 / 0.777 | 3.5 -> 2.85 m |
| Escopeta | 0.354 / 0.252 | 0.757 / 0.565 | 3.5 -> 1.63 m |
| Fusil | 0.358 / 0.343 | 0.762 / 0.749 | 3.5 -> 1.64 m |
| Francotirador | 0.361 / 0.233 | 0.768 / 0.511 | 3.5 -> 1.65 m |

La pistola es alta (la empuñadura), por eso la limita el alto (0.78) y no el ancho.

## Pruebas
- PlayMode `Tests/PlayMode/UI/HologramaArmaT993Pruebas.cs` (5): cada arma con ancho >= 70 % o alto >= 65 % y nunca > 95 %; la rueda acerca y
  aleja desde el encuadre con topes relativos y el pedestal queda debajo del arma. Con CAPTURAR_T993=1 guarda las capturas.
- Con las de MenuArmas y BancoTodasArmasT942: 11 de 11. EditMode completo: 2444, 2443 pasaron, falla NeonAltoT505 (conocido).

## Capturas (docs/capturas/t993/)
antes-pistola.png, pistola.png, escopeta.png, fusil.png, francotirador.png
