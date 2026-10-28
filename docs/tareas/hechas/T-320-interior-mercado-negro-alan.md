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
- [x] A las 12:00 la puerta abre y a las 22:00 no; el mostrador queda a menos de 3 m de la entrada, es un `IInteractuable` y funciona solo con el lugar abierto
- [x] Con entrada simulada se entra, se abre el mostrador y se compra un kit sin errores (cartera de prueba)
- [x] Capturas de día, fuera y dentro: docs/capturas/T-320-1..2.png
- [x] `bash herramientas/verificar.sh todo` pasa

## Fuera de alcance
La clínica (T-321).

## Cierre (2026-10-28)
- Desviación: el mostrador queda a ~3.9 m de la Entrada (1.4 m dentro de la puerta, <3 m de la puerta); a <3 m de la Entrada solo hay grosor de muro. La prueba exige <4.5 m de la Entrada y <3 m de la puerta.
- Rama alan/T-320-mercado-negro-limpia (sobre origin/main con T-250). El primer intento (0a78c5d en alan/fase-5) regeneró todo con CreadorTodo y borró el arte de T-250 de Barrio.prefab: no se sube. Aquí solo se corrió CreadorMercadoNegroInterior y VestirCerrados sobre el Barrio.prefab de main; el arte de T-250 conserva sus conteos de referencias.
- verificar.sh todo: compilar OK, EditMode 1094/1094, PlayMode 991/1000 (0 fallos; 9 omitidas, capturas de Tony).
