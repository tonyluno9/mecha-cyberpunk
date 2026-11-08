# T-725 HUD con los gráficos del chat de arte
Dueño: Tony | Rama: tony/T-725-hud-graficos | Modelo: Sonnet 5.5, medium
Depende de: T-700, T-701 (HUD H10), pedido-arte-08nov-b (H20 a H26)

## Objetivo
El HUD H10 usa los gráficos que hizo el chat de arte (imageassets/ui/hud/H20 a H26) en vez de los dibujados por código, con el acomodo de H10_HUD.png.

## Qué hacer
1. `ImportadorGraficosH10` (UI/Editor) quita el negro de los PNG (brillo a alfa), recorta y deja sprites en UI/Iconos/H10/. El marco del minimapa lleva 9-slice.
2. Barras sin las palabras VIDA ni ESCUDO: emblema a la izquierda, escudo delgado y corto arriba, vida debajo y el número al lado.
3. Marco del minimapa (H22), casillas de objeto (H26) con los íconos de H23, estrellas (H25) y muñequito (H24, partido en cabeza, torso, brazos y piernas).
4. Las barras bajan con DOTween: el relleno marca la vida al instante; la parte perdida se queda un momento y luego baja (`BarraConPerdida`; tiempos en ConfigHudH10).
5. El muñequito sigue marcando implantes y daño en rojo.
6. Si un gráfico faltara, queda el dibujo de código (no se inventa).

## Carpetas permitidas
Assets/_Proyecto/UI/, Assets/_Proyecto/Tests/EditMode/UI/ y PlayMode/UI/, docs/

## Criterios de aceptación
- [x] Marcos y rellenos de las barras, emblema, estrellas, muñequito, íconos, casilla y marco del minimapa son sprites de UI/Iconos/H10/
- [x] No hay textos VIDA ni ESCUDO; el número va a la derecha de la barra
- [x] Al recibir daño la parte perdida se queda y luego baja hasta la vida; al curar sube junto con el relleno
- [x] El muñequito sigue poniéndose rojo con daño y marcando implantes
- [x] Capturas s1d-T-725-* revisadas contra H10_HUD.png

## Pruebas automáticas
- EditMode: HudH10GraficosT725Pruebas (la config trae todos los sprites, borde 9-slice del marco, partes del muñequito del mismo tamaño)
- PlayMode: HudH10GraficosT725Pruebas (sprites en el HUD vivo, la pérdida se queda y baja, curación, escudo, muñequito rojo)
