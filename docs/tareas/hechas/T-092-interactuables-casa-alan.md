# T-092 Cama, espejo y taller como interactuables
Dueño: Alan | Rama: alan/T-092-interactuables-casa | Modelo: Sonnet 5.5, medium
Depende de: T-086, T-087 y que `IInteractuable` (Core/Jugador) e `IGuardable` (Core/Guardado) estén en main.
**Espera a Tony.** Hoy esos contratos solo existen en origin/tony/fase-5. Cómo saber si llegó: `git grep -l "interface IInteractuable" main -- Assets/_Proyecto/Core`.

## Objetivo
La cama (dormir: avanza el reloj a la mañana), el espejo y el banco del taller responden a E. `RelojMundo` implementa `IGuardable` con clave `reloj`.

## Qué hacer
Implementar `IInteractuable` en cada `PuntoUso` y `IGuardable` en RelojMundo; dormir usa el servicio de reloj existente. Pruebas de PlayMode con entrada simulada.

## Carpetas permitidas
Assets/_Proyecto/Mundo/, Tests de Mundo. No tocar Core/.

## Criterios de aceptación
- [ ] Dormir pasa de noche a la mañana y guardar/cargar conserva la hora
- [ ] `bash herramientas/verificar.sh todo` pasa

## Fuera de alcance
El menú de reparación (Tony T-F5).
