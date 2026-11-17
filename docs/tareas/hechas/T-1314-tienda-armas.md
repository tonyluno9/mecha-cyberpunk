# T-1314: tienda de armas (El Mercado) solo armas y balas, con cursor

Pedido de Tony (playtest 17 nov, punto 14).

## Qué se hizo
- `PantallaTienda`: en El Mercado el cursor se ve y queda libre (se guarda y devuelve el estado previo al cerrar), se asegura el EventSystem; clic en una fila la elige y otro clic sobre la elegida compra (el botón COMPRAR ya existía).
- `ReglaVistaMercado.SeOfrece`: de las armas solo se ofrece la base (nivel 0) que aún no se tiene; las mejoras por nivel (I, II, III) ya no salen, son del banco de Tino. Las balas siguen ofreciéndose solo al que tiene el arma ("Celdas de plasma" para el rifle de plasma).
- Pruebas: EditMode `MercadoSoloArmasBalasT1314Pruebas` (12/12); PlayMode `TiendaArmasT1314Pruebas` (cursor, filas sin niveles, clic elige/compra, cierre devuelve el cursor); `MostradorTiendaPruebas` actualizada (FilasDeArmas = 8).
- Captura: `docs/capturas/t1314/tienda-armas.png`.

## Resultados
EditMode ensamblado UI: 322/322. PlayMode (tiendas): 18, 16 pasan; fallan `Registro_ElMostradorRegistraSuTiendaYListaElKitYLasMuniciones` (conteo viejo de ofertas, falla en la base, no relacionado) y `Cic_Abre...` (pasa sola, falla por orden/tiempo).

## Segunda pasada (pedido del orquestador)
- El Mercado ya no vende el Kit de reparación ni las municiones del mecha (Misiles, Combustible, Celdas, Cargas): `ReglaVistaMercado.EsArmaOBalas` filtra en `Refiltrar` solo cuando la pantalla va en modo Mercado (armas a la venta).
- Dónde se compran ahora: en ningún lado. Se revisó el catálogo y los DatosTienda: solo `DatosTienda_MercadoNegro` ofrecía el kit y esas 4 municiones (Bar: bebida, Clínica: implante de ojos, Expendedora: consumibles, CIC: sin ofertas propias de esto). El mecha no está en la demo.
- "SNIPER" se muestra como "FRANCOTIRADOR" ("Balas de francotirador" para sus balas) con `ReglaVistaMercado.Nombre`; solo es texto, los ids y los assets no cambian.
- `MostradorTiendaPruebas.Registro_...` y `Comprar_ConDinero_TresKits...` ya fallaban en la base (origin/main: esperaba 28 ofertas y hay 53; esperaba 10 filas y había 15), medido corriendo la clase con los archivos de origin/main. El número se ajustó a 53 con comentario.
- Pruebas viejas reescritas con comentario "T-1314" (ahora comprueban que ya no salen): `MostradorTiendaPruebas` (4), `ArmasPruebas`, `ArmasMercadoPruebas` (SNIPER I se da por código), `MercadoArmasT720Pruebas`, `TiendaMercadoT1012Pruebas`, `FavoresPruebas` y `RecorridoMisionesPruebas` (el descuento se prueba con la escopeta), `RecorridoHabilidadesPruebas` (misiles y celdas por código).

## Decisiones
- Nombres de balas ya eran claros; no se renombró ningún asset.
- Revisión (Opus): RecorridoEconomiaPruebas compraba el kit en El Mercado: ahora comprueba que ya no sale y lo compra por código (1/1). El material Armas_CorteInterior de las piezas de T-1313 lleva una textura oscura lisa (ArmasPiezasT1313.TexturaCorte) para que SinGreyboxT506 no lo tome por greybox. EditMode completo: 2628/2629 antes de ese arreglo; SinGreyboxT506 9/9 después.
