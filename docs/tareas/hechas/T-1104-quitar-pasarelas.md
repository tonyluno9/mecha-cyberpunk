# T-1104: quitar las pasarelas PS01 de Neón Alto (punto 13)

Pedido de Tony: eliminar por completo las escaleras raras de la calle de Nova y la pasarela si sin escaleras no tiene sentido. Decisión del líder: se quitan completas.

## Qué se hizo
- `Assets/Arte/Editor/ColocarT1104SinPasarelas.cs` (idempotente, `Quitar`): apaga la raíz `T910_Pasarelas` (PS01 y rampas T996) y vuelve a encender los props que T-996 apagó al pie de las escaleras si no se encimarían con nada ni flotarían (OverlapBox y rayo al suelo). `S2n_Puentes` y los N02 siguen apagados.
- Decisión simple: `ColocarT910.Pasarelas` termina llamando a `ColocarT1104SinPasarelas.Aplicar`, así correrlo no las deja vivas; `ColocarT996.Escaleras` solo procesa PS01 activas, así que no hace nada.
- Se reencendieron 10 props en la zona (huacal C17, cargadores CY10 x3, neones N07 x6): ninguno se encima ni flota. Son más que los 7 de T-996 porque otras pasadas de limpieza (T-505/T-809) también los habían apagado por estorbar a puentes y escaleras.
- Pruebas ajustadas: `PasarelaNovaT996Pruebas` (ahora: sin PS01 ni rampas activas; W de punta a punta en x 41/47/53, z 52..80), `PiezasCalleT910Pruebas` (EditMode: pasarela apagada; PlayMode: sin foto de pasarela), `PuentesS2tPruebas` (sin PS01).

- Carro robable 7 (GTA_T511) estaba en (52.8, 75) en medio del paso a pie; se pega al borde (x 55.3) con `ColocarT1104CarroBanqueta`. El TearDown de `PasarelaNovaT996Pruebas` usa un nombre de escena único.

## Evidencia
- [x] EditMode Mundo: 365 total, 364 pasan, 1 falla ya existente en main (`LuzK01S2oPruebas.Postes_ElNaranjaQuedaBajoCadaPoste`, luz de T-1102, ajena a esta tarea).
- [x] PlayMode `PasarelaNovaT996Pruebas`: 3 de 3 pasan (con capturas).
- [x] Capturas: `docs/capturas/t-1104/calle-desde-sur.png`, `calle-desde-norte.png`: calle libre, nada flotando.
