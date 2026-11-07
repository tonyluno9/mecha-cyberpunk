# T-720 Mercado de armas: balas de cada arma
Dueño: Tony (S1) | Rama: tony/T-720-mercado-armas | Modelo: Sonnet 5.5
Depende de: T-416 (El Mercado ya vendía escopeta, fusil y sniper), T-679 (modelos W02-W04)

## Objetivo
En El Mercado (callejón de los puestos, junto al tianguis) se compran escopeta, fusil y sniper y las balas de cada arma, con precio y dinero de la cartera, aviso de que no te alcanza y el arma comprada lista en el inventario (teclas 3, 4 y 5).

## Qué se hizo
- Balas de reserva por arma: `DatosMunicionArma` (objeto de tienda), `ModeloReservaBalas` (lógica pura) y `ReservaBalas` (guardable, clave `reserva_balas`). La escopeta, el fusil y el sniper gastan reserva al recargar; la pistola y los puños siguen infinitos (no se toca la pistola que le gustó a Tony).
- `CargadorPistola.Fuente` (`IFuenteBalas`): sin fuente, balas infinitas; con fuente, la recarga toma de la reserva y sin reserva no recarga.
- Cada arma recibe una vez su reserva inicial al tenerse por primera vez (escopeta 16, fusil 60, sniper 20), venga de la tienda o de otra parte.
- `PantallaTienda`: las balas de un arma solo se ofrecen a quien tiene el arma; comprar suma `BalasPorPaquete` a la reserva (no al inventario) y cobra; "NO TE ALCANZA" sin dinero; aviso de máximo con la reserva llena. La fila muestra `LLEVAS balas/máximo`.
- HUD H10: con arma de reserva la segunda cifra son las balas que quedan.
- `CreadorMunicionArmas` (editor) crea 3 `DatosMunicionArma` y 3 ofertas (escopeta Ƀ60 por 8, fusil Ƀ120 por 60, sniper Ƀ150 por 10) y las pone en el catálogo y en `DatosTienda_MercadoNegro`.

## Criterios
- [x] El Mercado vende escopeta, fusil y sniper con precio y cobra de la cartera
- [x] El Mercado vende balas de cada arma, solo si ya tienes el arma
- [x] Sin dinero avisa NO TE ALCANZA y no entrega nada
- [x] Con la reserva llena avisa el máximo y no cobra
- [x] Recargar gasta reserva; sin reserva no recarga
- [x] El arma comprada se saca con 3, 4 y 5

## Pruebas
- EditMode Personaje: ReservaBalasT720Pruebas
- PlayMode Personaje: MercadoArmasT720Pruebas (compra cada arma y sus balas con flechas y Enter)
