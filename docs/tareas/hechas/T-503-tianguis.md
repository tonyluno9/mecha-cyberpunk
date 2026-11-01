# T-503 Paso A3: El Tianguis como la referencia
Dueño: Tony | Rama: tony/T-503-tianguis | Modelo: Sonnet 5.5 | docs/plan-ciudad-ui-personajes.md, paso A3
Hecho: colocador idempotente `ColocarT503.Colocar` (menú Mecha Cyberpunk/T-503/Colocar El Tianguis, Assets/Arte/Editor/) que rehace la raíz `T503_Tianguis` en la calle principal (x -37 a 37): 8 puestos con lona N03a/N03b a las orillas, 2 expendedoras CY06 con luz de color, 2 proyectores CY01 con hologramas de comida (quads con textura generada: taco, tazón, refresco; más 4 sobre puestos), 11 series de focos cálidos en zigzag (142 focos emisivos, cable LineRenderer, 6 luces sin sombra), 7 vapores ligeros (14 partículas máx.; incluye los 3 puestos de tacos que ya había) y 5 charcos de color. Un carril central de 6.8 m, las bocacalles, las puertas y los puntos de personajes quedan libres; la calle de la pelea con los SKULLS (x -37, z 77 a 110) no se toca. Arreglo extra: `ColocarT501.CrearPerfil` guarda los overrides del PerfilSuburbios como sub-assets (antes quedaba vacío) y `Regenerar perfil de Suburbios` lo rehace en su lugar con el mismo GUID.
Pruebas: EditMode TianguisT503Pruebas 8/8 (incluye el perfil de Suburbios), PlayMode TianguisT503PlayPruebas 3/3, RecorridoDemoPruebas (ver bitácora). Capturas: docs/capturas/ref-tianguis-noche-antes, -despues y -comparacion.

## FPS (build de medición, 1920x1080, Medio, hora 23)
| Punto | Antes | Después | 1% bajo después |
| --- | --- | --- | --- |
| tianguis | 213.6 | 213.1 | 112.0 |
| calle | 208.0 | 199.9 | 108.5 |
Muy por encima de 60.

## Qué se ajustó (2 vueltas)
1. Primer pase: focos blancos y enormes (emisivo 4.2 y bloom) y cables gruesos -> emisivo naranja 2.2, focos de 13 cm, cable de 2 cm.
2. Segunda: color cálido correcto; queda por debajo de la referencia en densidad de puestos (8 de ~20: la calle ya traía puestos de T-407 y fachadas con piezas) y sin lonas rasgadas.

## Bitácora
- 2026-11-01: hecho; los puestos van a las orillas y el carril central queda libre.
- 2026-11-01: RecorridoDemoPruebas 2/2 (el bot no cayo KO esta vez; los puestos van a las orillas).
