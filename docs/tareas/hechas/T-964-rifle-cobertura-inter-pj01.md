# T-964: arma larga, cobertura e interacciones con los clips de Blender (Sesión A, A5)

**Hecho.** El jugador usa los clips de Blender en:
- **Arma larga** (escopeta, fusil y francotirador comparten estados): `PJ01_Rifle_Idle`, `Caminar`, `Correr`, `Apuntar`, `ApuntarCaminar`, `ApuntarCorrer`, `DisparoCaminar` y `Recargar`.
- **Cobertura** (son con pistola, como dice la bitácora): `PJ01_Cobertura_Entrar` (también la pose congelada de CoberturaMantener), `Disparo` y `Disparo2`.
- **Interacciones**: `PJ01_Inter_AbrirPuerta`, `Boton`, `Recoger`, `Telefono` y `VerTelefono` (bucle).

## Inter_Telefono (37 s)
Revisado en doce poses (`docs/capturas/pj01-telefono-a/b-*.png`): saca el teléfono (0-3.4 s), se lo lleva al oído (3.4-6.8 s), habla 25 s y lo guarda al final.
El gesto usa solo el tramo útil: cuadros 0-235 (7.8 s). Un evento `AlTelefonoEnOreja` a los 6.5 s (cuadro 195) llega a `ReceptorEventosArma` y `VisualPersonaje` lo avisa con el evento `TelefonoEnOreja`. El evento va antes del 88 %, donde el estado sale a Idle (con el evento al 93 % no llegaba). Medido: de Idle a Idle en 7.0 s. Los NPC siguen con el clip de Mixamo completo.

## Capturas (revisadas contra 06_vista_frente, 06_vista_lado y 06_vista_rifle)
`docs/capturas/pj01-rifle-*.png` y `pj01-cobertura-inter-*.png`: las manos quedan donde va el arma larga (la derecha al pecho, la izquierda adelante), pies en el piso. El arma en la mano es T-965 (A6).

## Pruebas
EditMode 2354/2354 (`ClipsBlenderPJ01Pruebas`: overrides y recorte del teléfono con su evento). PlayMode 44/44: `TelefonoPJ01T964Pruebas` (nueva), `AgacharseT673Pruebas`, `AgarrePJ01T736Pruebas`, `ArmaEnManoS1Pruebas`, `GestosInteraccionPruebas`, `ControladorBaseVisualPruebas`, `AgarresArmasT650Pruebas`, `AgarreFinalPJ01T738Pruebas` y las de cobertura.
