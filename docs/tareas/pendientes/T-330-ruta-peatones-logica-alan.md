# T-330 Lógica de rutas de peatones (A-F5)
Dueño: Alan | Rama: alan/T-330-ruta-peatones | Modelo: Sonnet 5.5, medium
Depende de: nada de Tony.

## Objetivo
Las reglas de cómo camina un peatón por una banqueta: avanzar por puntos, esperar en las esquinas y dar la vuelta en los finales, sin Unity.

## Qué hacer
`Trafico/Peatones/CalculoPeaton.cs`: estado (índice de punto, espera), `Avanzar(dt)` con velocidad y espera como parámetros, azar inyectado para la espera. Números en `ConfigPeatones` (velocidad, esperas, densidad por franja del día).

## Carpetas permitidas
Assets/_Proyecto/Trafico/, Assets/_Proyecto/Tests/EditMode/Trafico/. No tocar Core/ ni carpetas de Tony.

## Criterios de aceptación
- [ ] Llega al último punto en el tiempo esperado; espera en las esquinas; da la vuelta; con la misma semilla repite
- [ ] Sin números mágicos; `bash herramientas/verificar.sh todo` pasa

## Fuera de alcance
Los peatones en la escena (T-331).
