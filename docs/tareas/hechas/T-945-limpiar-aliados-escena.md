# T-945: aliados fuera de la ciudad

Con la escena libre, se quitaron de `Ciudad_Prologo.unity` los 3 objetos Reclutable y los 2 componentes PandillaJugador y HudAliados, y se borraron los scripts marcador que dejó T-930. Ya no queda código de aliados.

Quedan por nombre, sin efecto, las ramas `Reclutable_*` de dos ayudantes de editor antiguos (`ConectarVisualesT510` y `T520`): solo buscan objetos que ya no existen.

Pruebas: EditMode; PlayMode `HistoriaPandillasPruebas`, `HistoriaDemoPruebas`, `HudCyberfixPruebas`, `MapaContornosT934Pruebas`.
