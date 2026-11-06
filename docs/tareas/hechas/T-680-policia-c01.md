# T-680 Policía C01
Dueño: Tony | Rama: tony/T-680-policia-c01 | Modelo: Sonnet 5.5

## Objetivo
Usar C01 en el policía a pie, con el reemplazo listo por si no trae esqueleto.

## Qué se hizo
- C01 trae esqueleto (78 huesos). "Policia" entra al flujo de personajes de Tripo (PostprocesadorAssetsT250, CreadorVisualTripo): Visual_Policia con las animaciones humanoides.
- `ColocarS1nPolicia` (lo llama ImportarS1n): crea `IA/Prefabs/PoliciaAPie.prefab` (de PandilleroArmado, con pistola W01 y `DatosPandilleroAPie_Policia`). Sin esqueleto usaría man con material azul marino y lo cambia solo cuando llegue C01 con esqueleto.
- Notas en docs/meter-assets-06nov.md y docs/decisiones-pendientes-tony.md. La lógica de bajarse y disparar va en un lote posterior.

## Pruebas
EditMode `PoliciaS1EditPruebas` (3, incluye el reemplazo man azul marino); PlayMode `PoliciaS1Pruebas` (1). Capturas `docs/capturas/s1t-T-680-*.png`.
