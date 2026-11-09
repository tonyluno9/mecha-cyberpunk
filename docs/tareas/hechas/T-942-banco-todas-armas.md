# T-942 Banco de Tino: todas las armas en 3D (hecha)

- ConfigMenuArmas_Base.asset: escopeta, fusil y francotirador con su modelo W02/W03/W04 (`AsignarModelosArmasT942.Asignar`, también en CreadorMenuArmas). Las mallas se muestran completas, sin partirlas.
- Si el modelo no trae objetos Cargador o Cañón, `VistaHologramaArma` arma piezas holográficas limpias con primitivas: una caja bajo la zona central (Cargador) y un cilindro en la punta con su extremo en la `Boca` (Cañón). Colocación y tamaños, proporcionales al largo del arma, salen de `CalculoPiezasPrimitivas` (lógica pura). La pistola no cambia.
- La separación hacia abajo se limita (`FactorSobrePedestal`) para que la pieza quede siempre sobre el pedestal.
- Capturas: docs/capturas/ui-banco-{escopeta,fusil,francotirador}.png (cargador separado).
- Pruebas: CalculoPiezasPrimitivasPruebas (EditMode), BancoTodasArmasT942Pruebas 3/3 y MenuArmasPruebas 1/1 (PlayMode; la escopeta es 3D y se compra su cargador al precio de DatosArma_escopeta_1).
