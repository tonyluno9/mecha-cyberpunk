# T-942 Banco de Tino: todas las armas en 3D (hecha)

- ConfigMenuArmas_Base.asset: escopeta, fusil y francotirador con su modelo W02/W03/W04 (`AsignarModelosArmasT942.Asignar`, también en CreadorMenuArmas). Las tres FBX quedan con Read/Write activado para poder partir la malla.
- `CalculoPiezasArma` (puro): reparte los triángulos en Cañón (tramo hacia la `Boca`), Cargador (cuelga debajo de la zona central, tramo medio) y resto.
- `VistaHologramaArma`: una malla única sin piezas con nombre se parte al mostrar el holograma en tres mallas nuevas (el asset no se modifica) y se registran igual que las de la pistola; la Mira sigue saliendo con CrearMira.
- Límite conocido: el corte es por triángulo, así que el borde del cargador queda algo irregular (en el fusil salen empuñadura y cargador juntos, en la escopeta la empuñadura).
- Capturas: docs/capturas/ui-banco-{escopeta,fusil,francotirador}.png.
- Pruebas: CalculoPiezasArmaPruebas (EditMode), BancoTodasArmasT942Pruebas 3/3 y MenuArmasPruebas 1/1 (PlayMode; la escopeta ahora es 3D y se compra su cargador con el precio de DatosArma_escopeta_1).
