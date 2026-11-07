# T-708 Patrulla llega
Dueño: Tony (S1, autorizado en Trafico, Vehiculos e IA esta noche) | Rama: tony/T-708-patrulla-llega | Modelo: Sonnet 5.5
Depende de: T-707

## Objetivo
Al subir la búsqueda las patrullas nacen fuera de la vista y llegan manejando con un tiempo real, sin aparecer junto al jugador.

## Qué hacer
1. CalculoLlegadaPatrulla elige el punto de salida por el tiempo de ruta por las calles (mínimo 7 s, objetivo 12 s, ConfigPersecucion).
2. GestorPersecucion usa además el margen de vista del pool del tráfico (CarroTrafico.PuntoEnVista).

## Criterios de aceptación
- [x] Nace fuera del frustum (con margen) y lejos del jugador
- [x] Tarda al menos 7 s en llegar por las calles

## Pruebas automáticas
- EditMode: CalculoLlegadaPatrullaT708Pruebas
- PlayMode: PatrullaLlegaT708Pruebas (y las de T-664 siguen verdes)

## Fuera de alcance
- Policías a pie (T-709)
