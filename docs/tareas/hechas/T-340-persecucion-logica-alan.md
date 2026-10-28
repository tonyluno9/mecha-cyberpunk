# T-340 Lógica de persecución de drones y patrullas (A-F5)
Dueño: Alan | Rama: alan/T-340-persecucion-logica | Modelo: Sonnet 5.5, medium
Depende de: `ICalor` (Core/Calor) en main. Sin Unity.

## Objetivo
Reglas puras de cuántos perseguidores hay por nivel de calor y cómo se acercan.

## Qué hacer
`Trafico/Persecucion/CalculoPersecucion.cs`: `Cantidad(nivel)` (0 sin calor; 1 dron en nivel 1; 2 drones en nivel 2; 2 drones y una patrulla en nivel 3), velocidad, distancia de seguimiento y de captura. Números en `ConfigPersecucion`.

## Carpetas permitidas
Assets/_Proyecto/Trafico/, Assets/_Proyecto/Tests/EditMode/Trafico/. No tocar Core/ ni carpetas de Tony.

## Criterios de aceptación
- [x] Cantidades por nivel como arriba; un nivel fuera de rango se recorta; sin números mágicos
- [x] `bash herramientas/verificar.sh todo` pasa
