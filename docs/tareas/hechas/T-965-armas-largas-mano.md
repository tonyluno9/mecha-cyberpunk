# T-965: escopeta, fusil y francotirador en la mano de PJ01 (Sesión A, A6)

**Hecho.** Las tres armas largas cuelgan de R_Hand (socket ManoDer) con el agarre del W03 en `06_Rifle_Inter_Cobertura.blend`: (0.003, 0.068, 0.003) m y (1.6, -169.3, -0.5)°.
Escopeta y francotirador usan el mismo (las tres tienen la empuñadura en el origen, T-679/T-970).

## Conversión (medida, no supuesta)
- Posición: como la pistola en T-749: R_Hand de Unity girado 180° en Y y PJ01 a 1.88 en Unity contra 1.837 en Blender → (-0.0031, 0.0696, -0.0031) m.
  El "Scale Factor 1.8367" del pedido es la escala de PJ01 en Blender; en Unity va a 1.88 (ver T-960).
- Giro: se probaron 16 conversiones contra el propio clip PJ01_Rifle_Apuntar (`AgarreArmasLargasT965.Candidatos`). La buena cambia de mano el cuaternión de Blender
  (Y y Z con signo contrario), gira 180° en Y por el R_Hand y pasa el arriba de Blender (Z) al de Unity (-90° en X): el cañón queda a 7° de la línea entre las manos
  (la izquierda va 4 cm al lado del guardamanos) y la mira a 1° de arriba. Las demás dejaban el cañón al revés o de lado.
- Mano izquierda (AgarreIzq): donde la pone PJ01_Rifle_Apuntar con este agarre, (-0.038, 0.006, 0.273) m, bajo el guardamanos.
- Los modelos mixamorig siguen con su agarre de siempre. No se tocaron los modelos ni las piezas de las armas (Sesión B).

## Capturas (revisadas contra 06_vista_rifle)
`docs/capturas/pj01-rifle-mano-frente.png`: el fusil al hombro, la mejilla junto a la mira roja y la mano izquierda bajo el guardamanos, como en Blender.

## Pruebas
EditMode 2353/2353 (`AgarresArmasT650Pruebas` actualizada al agarre de Blender). PlayMode 30/30: `ArmasNuevasS1Pruebas` (la Sesión B avisó de 2 fallos tras T-964: escopeta apuntando a 0.83 de frente y el zoom del francotirador; con este agarre pasan), `ArmaEnManoS1Pruebas`, `AgarrePJ01T736Pruebas`, `AgarreFinalPJ01T738Pruebas`, `ApuntarClipsPJ01T749Pruebas`, `RetargetPJ01T749Pruebas`.
