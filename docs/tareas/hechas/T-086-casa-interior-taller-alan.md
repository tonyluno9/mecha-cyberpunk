# T-086 Casa por dentro con el taller
Dueño: Alan | Rama: alan/T-086-casa-interior-taller | Modelo: Sonnet 5.5, medium
Depende de: T-043 (patrón de interior sin carga), T-076 (kit de Blender). Contratos ya en main: `ITallerMecha`.

## Objetivo
La casa deja de ser una caja: sala-dormitorio y un taller con espacio para el mecha, entrando por la puerta sin carga.

## Qué hacer
1. `Mundo/Editor/CreadorCasa.cs` (como CreadorBar) arma el interior de Lugar_Casa dentro de Barrio.prefab: sala con cama (cubos por ahora, ver T-087), cocineta y el taller al fondo: piso despejado de 2 m de radio y 5 m de alto, banco de trabajo, estantes, lámpara fluorescente.
2. `Mundo/Lugares/TallerCasa.cs`: implementa `ITallerMecha` (Estacionamiento sobre el piso del taller con +Z hacia la salida; PuntoInteraccion junto al banco) y se registra en `Registro<ITallerMecha>` en OnEnable/OnDisable. Uno por escena.
3. El taller tiene una salida ancha (6 m) hacia la calle o el patio, para que el mecha pueda salir; la salida queda libre.
4. Luces bajo `ZonaInterior`, máximo 6.
5. Regenerar con `CreadorTodo.Crear` y rehornear reflejos.

## Carpetas permitidas
Assets/_Proyecto/Mundo/, Assets/_Proyecto/Tests/EditMode/Mundo/, Assets/_Proyecto/Tests/PlayMode/Mundo/, docs/capturas/.
No tocar: Core/, carpetas de Tony.

## Criterios de aceptación
- [ ] Entrar a la casa y cruzar al taller en menos de 10 s sin carga ni atorarse (rayos sin huecos como BarInteriorPruebas)
- [ ] `Registro<ITallerMecha>` tiene exactamente 1 al cargar la escena; el cilindro de 2 m × 5 m sobre Estacionamiento está libre de colliders
- [ ] Estacionamiento mira hacia la salida (comprobado con un rayo)
- [ ] Capturas del taller y de la sala: docs/capturas/T-086-1.png y T-086-2.png
- [ ] `bash herramientas/verificar.sh todo` pasa

## Fuera de alcance
Cama y espejo interactivos (T-092), reparación (Tony T-F5), el mecha dentro del taller (lo coloca Tony).
