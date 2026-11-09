# T-749: clips de Blender en PJ01

**Hecho.** Clips de `imageassets/blender/pj01_clips/` en `Assets/Arte/Personajes/PJ01/Clips/`, Humanoid con el avatar de PJ01. Se probó primero Humanoid (lo pedido); no hizo falta Generic.

## Causas encontradas (medidas contra `W01_Pistola_agarre.blend`)
1. El agarre: R_Hand de PJ01 en Unity tiene los ejes girados 180° en Y respecto a agarre.txt. Nuevo `GiroPJ01 = Y180 * CuaternionTony` y posición con X y Z negadas, ajustada a la escala 1.88. Antes salía el ladeo de 65-79° que T-737 atribuyó al retargeting.
2. El cuerpo de los clips mira a -Z. `rotationOffset = 180` (`GiroCuerpoClips`).
3. La cadera venía de la capa base (Mixamo) y giraba el torso hasta 46°. `Root` entra en `MascaraTorso`.
4. El retroceso comparaba el nombre `Pistola_Disparo`; ahora termina en él (el clip es `PJ01_Pistola_Disparo`).

## Medidas
- Humanoid contra el mismo clip Generic, 10 clips, 4 tiempos: peor cañón 0.46°, peor manos 0.13 cm.
- En juego, sin la compensación de `AgarrePistolaPJ01`: cañón a la mira 0.7-3°, ladeo hasta 2.7°, manos 10.3-11.1 cm (Blender: 10.1-10.5, a escala menor); los pies no se despegan al apuntar.
- Capturas: docs/capturas/pj01-t749-*.png.

## Pruebas
`RetargetPJ01T749Pruebas`, `ApuntarClipsPJ01T749Pruebas` (con `T749_CAPTURAS=1` escribe las capturas), `AgarreFinalPJ01T738Pruebas`, `AgarrePJ01T736Pruebas`, `AgarresArmasT650Pruebas`. `DiagPiesT749` se quitó; `MedidaPoseClipsPJ01` queda porque la usan las pruebas.

Clips que faltan: docs/pedido-blender-10nov.md.
