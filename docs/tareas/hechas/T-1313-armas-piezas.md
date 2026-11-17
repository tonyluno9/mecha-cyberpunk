# T-1313: armas nuevas en piezas (W05-W08) y tambor que gira

Punto 13 de docs/playtest-17nov.md.

## Qué se hizo
- FBX de piezas de Blender copiados a `Assets/Arte/Armas/<carpeta>/W0x_Piezas.fbx` (subfusil: Armazon, Canon, Cargador, Mira; revólver y lanzagranadas: Armazon, Canon, Tambor; rifle de plasma: Armazon, Canon, Celda, Mira).
- `UI/Editor/ArmasPiezasT1313.cs`: Importar, ColocarPrefabs (reutiliza `ArmasPiezasT970.ArmarPrefab`, mismo GiroModelo: mide cañón a +Z en los cuatro), ConfigurarBanco y Capturar.
- Banco de Tino: subfusil Cargador/Canon/Mira, revólver y lanzagranadas Tambor/Canon sin mira, rifle Celda/Canon/Mira (`ConfigMenuArmas_Base` y `CreadorMenuArmas`).
- `TamborArma.GirarVuelta` y `OnEnable`: el tambor da una vuelta completa (0.6 s) al activarse el visual del arma (sacarla en la rueda o al cambiar de arma). No se tocó ArmasEspecialesAPie.
- `Arte/Editor/ColocarT1014Armas.Colocar` ya no pisa el prefab si existe `W0x_Piezas.fbx` (lo llamaba `ImportarS1nPruebas` y deshacía el trabajo); usa ArmarPrefab, como ColocarS1nArmas.

## Pruebas
- EditMode `ArmasPiezasT1313Pruebas` (10): Modelo con Armazon y Canon, cañón a +Z, Boca en la punta (2 cm), un solo Tambor con el centro quieto al girar 60 grados (1 mm), nombres de pieza del banco existen en el prefab.
- PlayMode `BancoArmasPiezasT1313Pruebas` (4: el banco separa cada pieza) y `TamborVueltaT1313Pruebas` (2: vuelta completa al sacar el arma y termina en reposo).
- Armas existentes: PlayMode T1014, S1, T942, T993, MenuArmas 26/26 (con las nuevas); EditMode UI + PersonajeVisual 576/576.

## Decisiones
- La prueba T1014 del tambor ahora espera a que termine la vuelta de sacar el arma antes de medir un disparo.
- En el banco, el cargador del subfusil se mueve ~1 cm (el pedestal limita la separación hacia abajo), por eso el umbral de movimiento de la prueba nueva es 5 mm.
- Capturas en `docs/capturas/t-1313/` (w0x-armada y w0x-separada, separada a mano).
