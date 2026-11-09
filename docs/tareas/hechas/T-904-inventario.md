# T-904: inventario nuevo (solo consulta)

- Pantalla nueva `PantallaInventario`, armada por código sobre la guía H10: categorías con ícono (Resources/Menus/Cat_*) a la izquierda, cuadrícula de casillas con filo de rareza al centro y detalle a la derecha. Se abre con I (InputAction serializada, `<Keyboard>/i`, que nadie más usaba) solo con `PilaPantallas` vacía, fuera de pausa; I o Esc la cierran. Cursor libre, el tiempo sigue y el jugador a pie no se mueve mientras está abierta. ← → ↑ ↓ eligen casilla, Q y E cambian de categoría, el ratón elige con clic.
- Lo que muestra es real: armas y su nivel de `ArsenalJugador` (rareza = nivel), balas de `ReservaBalas`, objetos de `ServicioInventario` (consumibles y otros) e implantes de `IImplantes`. Se actualiza con los eventos `Cambio` mientras está abierta.
- Lógica pura en `ReglaInventarioUI`, `EntradaInventario` y `CategoriaInventario`; la lectura de servicios en `FuenteInventario`.
- Existe en la ciudad sin tocar escenas: `PantallaMapa.Awake` le agrega el componente al mismo objeto.
- Decisión: las armas con balas limitadas (escopeta, fusil, sniper) también salen en MUNICIÓN con su reserva; pistola y puños no (balas infinitas). Sin ícono de categoría (TODO, OTROS) se dibuja la inicial.

Pruebas: `ReglaInventarioUIPruebas` (EditMode, 12), `InventarioPruebas` (PlayMode, 2). Captura: docs/capturas/ui-inventario.png.
