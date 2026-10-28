# T-303 Interior del casino, abierto de noche (A-F5)
Dueño: Alan | Rama: alan/T-303-interior-casino | Modelo: Sonnet 5.5, medium
Depende de: T-301, T-302, T-094 (exterior), T-043 (patrón de interiores).

## Objetivo
El casino se puede entrar de noche: sala con 6 tragamonedas, cajera, luz cálida y neón, y cierra de día.

## Qué hacer
1. `Mundo/Editor/CreadorCasinoInterior.cs`: interior bajo `Lugar_Casino` (como `CreadorBar`), 6 `Tragamonedas.prefab`, la cajera, columnas, alfombra y luces. Se agrega a `CreadorTodo` en orden y se puede repetir.
2. El `DatosLugar` del casino deja de estar "cerrado en la demo"; el horario real es de noche. Actualizar `CerradosPruebas` y el letrero.
3. Sonda de luz y audio como en el bar (música dentro, sin lluvia).

## Carpetas permitidas
Assets/_Proyecto/Mundo/, Assets/_Proyecto/Casino/, Tests de Mundo y Casino, docs/capturas/. No tocar Core/ ni carpetas de Tony.

## Criterios de aceptación
- [ ] A las 22:00 la puerta abre y a las 12:00 no; adentro hay 6 máquinas y la cajera registradas como `IInteractuable`
- [ ] Con entrada simulada se entra, se cambian fichas y se juega una vez sin errores ni atravesar paredes
- [ ] Capturas de noche, fuera y dentro: docs/capturas/T-303-1..3.png
- [ ] `bash herramientas/verificar.sh todo` pasa

## Fuera de alcance
Apuestas a peleas, más juegos.
