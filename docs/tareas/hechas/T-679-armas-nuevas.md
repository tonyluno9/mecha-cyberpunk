# T-679 Armas nuevas
Dueño: Tony | Rama: tony/T-679-armas-nuevas | Modelo: Sonnet 5.5

## Objetivo
Escopeta (W02), fusil (W03) y francotirador (W04) con su modelo de Tripo: en la mano, en el piso, en el HUD y en los datos del banco de Tino.

## Qué se hizo
- `Assets/Arte/Editor/ColocarS1nArmas.cs` (lo llama `ImportarS1n`): material, prefab `Wxx_Nombre.prefab` (cañón a +Z, empuñadura en el origen, hijo Boca), prefab `Wxx_Nombre_Piso.prefab` (con `ArmaEnPiso`, de lado) y el modelo en el objeto Escopeta / Fusil / Sniper de la mano del jugador (cajas ocultas, luz de la boca en la punta).
- `AgarresArmas`: las armas largas se agarran por la empuñadura (como la pistola) y la mano izquierda va al guardamano (0.36-0.38 m; más lejos no alcanza el brazo).
- Destello: tamaños de `AjustarDestelloT676` al doble porque F01 ocupa menos de la mitad de la imagen.
- HUD: H02-H04 ya estaban en `PiezasUI.armasHolograma` (T-816). Banco de Tino: las 4 armas ya traen icono e imagen holográfica en `ConfigMenuArmas_Base` (T-814); se dejó la imagen (no el modelo 3D) porque el menú anima piezas llamadas Bateria/Corredera y los modelos nuevos son una sola malla.
- Estadísticas y sonidos ya existían (CreadorArmas, ARM_ESC/FUS/SNI_DISPARO): escopeta 8 perdigones y 20 m, fusil automático, francotirador 70+ de daño, 150 m y zoom.

## Pruebas
PlayMode `ArmasNuevasS1Pruebas` (6, con entrada simulada y capturas con CAPTURAR_S1=1); EditMode `ArmasNuevasS1EditPruebas` (3).
Capturas: `docs/capturas/s1t-T-679-*.png` (mano, apuntar y piso de las 4 armas) y `s1t-T-678-destello-pistola.png`.
