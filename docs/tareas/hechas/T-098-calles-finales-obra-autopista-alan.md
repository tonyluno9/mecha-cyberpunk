# T-098 Pulido de calles y finales creíbles (retén, obra, autopista)
Dueño: Alan | Rama: alan/T-098-calles-finales | Modelo: Sonnet 5.5, medium
Depende de: T-077 (finales sólidos y retenes), T-080 (kit urbano).

## Objetivo
Los bordes del barrio se ven como una ciudad que sigue: un retén con luces, una obra con vallas y andamios, y una autopista elevada al fondo, en lugar de muros lisos.

## Qué hacer
1. `Mundo/Editor/VestirFinales.cs`: sobre los finales sólidos de T-077 agrega vallas de obra con luces ámbar, un andamio y grúa (cubos), conos, y un tramo de autopista elevada (pilares y losa con luces de carros en movimiento falso con emisión) visible desde el barrio.
2. Pulido de calles: grietas, tapas de alcantarilla, líneas de cruce peatonal en los cruces, sin tocar colliders de las calles.
3. Regenerar con `CreadorTodo.Crear` sin cambiar nada al repetir.

## Carpetas permitidas
Assets/_Proyecto/Mundo/, Tests de Mundo, docs/capturas/.

## Criterios de aceptación
- [ ] Los finales siguen sin dejar pasar (FinalesCarroPruebas pasa) y el carro sigue pudiendo dar la vuelta (CamaraLibreCarroPruebas)
- [ ] Capturas de cada final, revisadas: docs/capturas/T-098-1..3.png
- [ ] `bash herramientas/verificar.sh todo` pasa
