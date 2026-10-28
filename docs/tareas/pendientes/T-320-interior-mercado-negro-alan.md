# T-320 Interior del mercado negro, abierto de día (A-F5)
Dueño: Alan | Rama: alan/T-320-interior-mercado-negro | Modelo: Sonnet 5.5, medium
Depende de: T-094, T-043; `UI/Prefabs/MostradorMercadoNegro.prefab` de Tony en main (D-tony-81).

## Objetivo
El mercado negro se entra de día: un cuarto de puestos con el mostrador de Tony colocado (comprar kits, munición y piezas; vender piezas viejas).

## Qué hacer
1. `Mundo/Editor/CreadorMercadoNegroInterior.cs`: interior bajo `Lugar_MercadoNegro` con puestos, cajas, luces de sodio y el prefab `MostradorMercadoNegro` de Tony colocado frente a la entrada (se lee el prefab, no se edita). Cierra con la noche.
2. El `DatosLugar` del mercado deja de estar "cerrado en la demo"; ajustar `CerradosPruebas` y el letrero.
3. Se agrega a `CreadorTodo` y se puede repetir.

## Carpetas permitidas
Assets/_Proyecto/Mundo/, Tests de Mundo, docs/capturas/. No tocar Core/ ni carpetas de Tony.

## Criterios de aceptación
- [ ] A las 12:00 la puerta abre y a las 22:00 no; el mostrador queda a menos de 3 m de la entrada, es un `IInteractuable` y funciona solo con el lugar abierto
- [ ] Con entrada simulada se entra, se abre el mostrador y se compra un kit sin errores (cartera de prueba)
- [ ] Capturas de día, fuera y dentro: docs/capturas/T-320-1..2.png
- [ ] `bash herramientas/verificar.sh todo` pasa

## Fuera de alcance
La clínica (T-321).

## WIP (sin verificar)
Código escrito pero **no compilado ni probado**: esta máquina solo tiene Unity 6000.6.5f1 y el proyecto pide 6000.3.25f1
(abrirlo con otra versión reimporta y modifica ProjectSettings/Packages, fuera de esta tarea). Pendiente en una máquina con 6000.3.25f1:
1. `CreadorTodo.Crear` (genera `.meta`, materiales `MercadoInt_*`, Barrio.prefab, DatosLugar_MercadoNegro y escenas).
2. `bash herramientas/verificar.sh todo`; corregir lo que salga (nuevo: `Tests/PlayMode/Mundo/MercadoNegroInteriorPruebas.cs`).
3. Capturas `docs/capturas/T-320-1..3.png` (las genera `Mercado_Capturas_DiaFueraYDentro`), revisarlas, y mover la tarea a hechas.
Hecho en código: `Mundo/Editor/CreadorMercadoNegroInterior.cs` (cuarto 10 × 10 m, 4 puestos, luces de sodio, `ZonaInterior`, mostrador de Tony
en (-1.1, 4.8) local, a ~2.9 m de la Entrada), `CreadorLugares` abre el mercado (9:00 a 18:00), `VestirCerrados`/`VestirEntradas` en versión abierta,
`CreadorTodo` lo incluye, y pruebas de Mundo actualizadas (Lugares, Puertas, Cerrados, EntradasNeon).
