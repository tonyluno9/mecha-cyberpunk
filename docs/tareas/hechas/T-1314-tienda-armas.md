# T-1314: tienda de armas (El Mercado) solo armas y balas, con cursor

Pedido de Tony (playtest 17 nov, punto 14).

## Qué se hizo
- `PantallaTienda`: en El Mercado el cursor se ve y queda libre (se guarda y devuelve el estado previo al cerrar), se asegura el EventSystem; clic en una fila la elige y otro clic sobre la elegida compra (el botón COMPRAR ya existía).
- `ReglaVistaMercado.SeOfrece`: de las armas solo se ofrece la base (nivel 0) que aún no se tiene; las mejoras por nivel (I, II, III) ya no salen, son del banco de Tino. Las balas siguen ofreciéndose solo al que tiene el arma ("Celdas de plasma" para el rifle de plasma).
- Pruebas: EditMode `MercadoSoloArmasBalasT1314Pruebas` (12/12); PlayMode `TiendaArmasT1314Pruebas` (cursor, filas sin niveles, clic elige/compra, cierre devuelve el cursor); `MostradorTiendaPruebas` actualizada (FilasDeArmas = 8).
- Captura: `docs/capturas/t1314/tienda-armas.png`.

## Resultados
EditMode ensamblado UI: 322/322. PlayMode (tiendas): 18, 16 pasan; fallan `Registro_ElMostradorRegistraSuTiendaYListaElKitYLasMuniciones` (conteo viejo de ofertas, falla en la base, no relacionado) y `Cic_Abre...` (pasa sola, falla por orden/tiempo).

## Decisiones
- Kit de reparación y municiones del mecha (Misiles, Combustible, Celdas, Cargas) se dejan en El Mercado: sus pruebas viejas y el juego del mecha dependen de ellos. Si Tony los quiere fuera, es filtrar por tipo en `Refiltrar`.
- Nombres de balas ya eran claros; no se renombró ningún asset.
