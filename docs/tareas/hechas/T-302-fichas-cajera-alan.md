# T-302 Fichas y cajera del casino (A-F5)
Dueño: Alan | Rama: alan/T-302-fichas-cajera | Modelo: Sonnet 5.5, medium
Depende de: T-300, T-301; `IGuardable` (Core/Guardado) en main.

## Objetivo
El casino juega con fichas: la cajera cambia dinero por fichas y fichas por dinero, y las fichas se guardan con la partida.

## Qué hacer
1. `Casino/Fichas/CarteraFichas.cs` (lógica pura) y `ServicioFichas : IGuardable` con clave `fichas`: saldo entero, `Comprar`, `Retirar`, `Cobrar`; un texto guardado inválido se rechaza avisando.
2. `Casino/Cajera/CajeraMundo.cs` (`IInteractuable`): "Cambiar $100 por fichas" y "Cobrar fichas"; cobra con `ICartera` y `MotivoDinero.Casino`.
3. La tragamonedas (T-301) pasa a apostar y pagar con fichas.

## Carpetas permitidas
Assets/_Proyecto/Casino/, Tests de Casino, docs/capturas/. No tocar Core/ ni carpetas de Tony.

## Criterios de aceptación
- [x] El saldo de fichas nunca es negativo; guardar y cargar lo conserva y un texto inválido avisa sin cambiar nada
- [x] Cambiar y cobrar conserva el valor (tipo de cambio en los datos) sin crear ni perder dinero
- [x] La tragamonedas no gira sin fichas
- [x] `bash herramientas/verificar.sh todo` pasa

## Fuera de alcance
Apuestas a peleas en el casino (necesitan el prefab de Tony de `IApuestasPelea`).
