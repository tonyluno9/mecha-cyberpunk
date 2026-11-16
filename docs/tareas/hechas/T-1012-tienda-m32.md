# T-1012: El Mercado como el mockup M32 (playtest 16-nov, punto 15)

- Si la tienda vende armas (El Mercado) la pantalla cambia a la vista `VistaMercadoArmas`, como
  `imageassets/referencias/menus/M32_TiendaArmas.png`: título "EL MERCADO" al centro; la lista angosta a la izquierda con el dinero grande
  arriba y la fila elegida en amarillo; a la derecha el nombre grande, el holograma 3D del banco de Tino girando sobre una rejilla en
  perspectiva, las barras DAÑO / CARGADOR / CADENCIA / ALCANCE (`ReglaVistaMercado`) y el botón COMPRAR (o SIN DINERO), que también se pulsa
  con el ratón. Las demás tiendas (CIC, expendedoras) quedan como antes.
- `ConfigPantallaTienda_Base` apunta a `ConfigMenuArmas_Base` (`MercadoM32T1012.Aplicar`).
- Las balas muestran el arma que usan; lo del mecha (kit, misiles...) muestra el nombre sin holograma.
- Se usan los tamaños de letra del kit: con tamaños nuevos la reconstrucción del atlas de la fuente hacía pasar de 65000 vértices a un texto
  largo de otra pantalla (MenusCyberfixPruebas).
- Pruebas: TiendaMercadoT1012Pruebas, MenusCyberfix, MostradorTienda, RecorridoEconomia: 12/12.
- Captura revisada: `docs/capturas/t1012/mercado-m32.png`.
