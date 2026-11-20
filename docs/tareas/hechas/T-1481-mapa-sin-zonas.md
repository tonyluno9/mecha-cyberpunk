# T-1481 · Mapa grande y minimapa sin las zonas nuevas

Rama: tony/T-1481-mapa-sin-zonas. Carpetas: UI/ y sus pruebas.

## Qué se hizo
Las zonas Malecón, Colonia del Cerro, Autopista y Puerto se quitaron del mapa grande, del minimapa y del HUD.
Se restauró de `8b33987^1` (con `git checkout`) todo lo que tocaron los commits a2c0d4c (T-1352) y 386ca02:

- `UI/Mapa/BarriosMapa.cs`, `Minimapa.cs`, `MapaImagen.cs` (cuadro -200..200 x -140..140, 6 px/m, 2 px de vista por metro), `PantallaMapa.cs` (zoom máximo 3x otra vez, sin desplazar por el centro del cuadro), `CalculoCallesMapa.cs` (sin `TramosDeLinea`).
- `UI/Editor/CapturadorMapaCiudad.cs` y `UI/Editor/EstiloM10.cs` (sin el mar azul).
- `UI/HudH10/HudH10.cs`: el título de zona vuelve a usar solo `territorio.Nombre(zona)` (los 4 barrios).
- `UI/Resources/Mapa/MapaCiudad.png`: imagen de antes (el .meta no cambió, se conserva).
- `Tests/EditMode/UI/MapaCiudadT747Pruebas.cs` y `Tests/PlayMode/UI/MapaPlayT747Pruebas.cs`: expectativas de antes.

## Qué se conservó
Los cambios de sonido de UI (T-1474) en Inventario, Teléfono, Pausa y Tienda no se tocaron: eran de otros archivos.
`ZonaHudPruebas` no se tocó (nunca mencionó zonas nuevas).

## Supuestos
- El zoom máximo 6x (antes 3x) se subió por el mapa más grande; se revirtió a 3x con el resto.
- Ya nadie en UI usa `BarriosMapa.ZonaNuevaEn` ni `CalculoCallesMapa.TramosDeLinea`. Fuera de UI hay pruebas (Trafico, PersonajeVisual) que hablan de "zonas nuevas" pero no usan esas APIs; son de otras tareas.

## Para comprobar en Unity
- Compilar y correr `MapaCiudadT747Pruebas` (EditMode) y `MapaPlayT747Pruebas` (PlayMode).
- La imagen restaurada es la de antes de las zonas y no hace falta regenerarla. Solo si la escena de la ciudad cambió respecto a esa base, correr el capturador con el método estático `MechaCyberpunk.UI.Editor.CapturadorMapaCiudad.Generar` (ver el archivo para el namespace y cómo se invoca). Sobrescribe MapaCiudad.png; es seguro con la escena ya revertida y con Unity cerrado a otras sesiones.
