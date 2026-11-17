# T-1326: tienda de armas, girar el arma y navegar con mouse

Pedido de Tony (playtest 17 nov). Sin Unity esta noche: no se compiló ni corrió nada; lo revisa el orquestador.

## Qué se hizo
- El Mercado (`VistaMercadoArmas`): el arma 3D se gira arrastrando con el botón izquierdo sobre el holograma, la rueda sobre él la acerca, y Q/E la giran sin ratón (InputActions serializadas). `PantallaTienda.Update` llama a `Atender()` solo si está arriba de la pila.
- `ReglaGiroHolograma` (nueva, lógica pura): dirección de Q/E y arrastre equivalente en px.
- `PantallaTienda`: botón SALIR (`BotonSalir`, arriba a la derecha, `ClicSalir()` = Esc) y las pestañas COMPRAR/VENDER ahora se eligen con clic y resaltan al pasar el cursor. Las filas ya tenían hover y clic (T-1314).
- Banco de Tino (`PantallaMejoraArmas`): ya tenía arrastrar, rueda, filas, piezas, filtros y botón de mejora con ratón. Se agregó botón SALIR en la cabecera (`ClicSalir()`) y giro con Q/E.
- Otras pantallas con arma 3D: ninguna (la rueda de armas del juego no es tienda).

## Pruebas
- EditMode `GiroHologramaT1326Pruebas` (5).
- PlayMode `TiendaArmasMouseT1326Pruebas` (4): arrastrar gira / sin botón o fuera del holograma no gira / rueda acerca; Q y E giran; hover, clic elige y compra, SALIR cierra; SALIR de Tino cierra.
- No ejecutadas (sin Unity).

## Decisiones
- Q/E como respaldo porque las flechas ya cambian pestaña y fila.
- No se tocaron precios ni escenas/prefabs (todo se arma por código al abrir).
