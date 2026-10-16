# T-088 Callejón de la emboscada y punto de llamado
Dueño: Alan | Rama: alan/T-088-callejon-emboscada-llamado | Modelo: Sonnet 5.5, medium
Depende de: T-080, T-081. Contratos ya en main: `IPuntoLlamadoMecha`, `IPuntoAparicion`. `Emboscada.prefab` (Pelea/Prefabs) ya está en main.

## Objetivo
Un callejón cerca de la casa donde pasa la emboscada: espacio para la pelea y cielo abierto para que baje el mecha, más el punto de llamado que cubre la zona.

## Qué hacer
1. `Mundo/Lugares/PuntoLlamadoMundo.cs` implementa `IPuntoLlamadoMecha` (Llegada, Radio > 0, Habilitado) y se registra en OnEnable/OnDisable.
2. `Mundo/Editor/CreadorCallejon.cs`: callejón de unos 10 × 30 m entre dos edificios, con basura, cajas, neón y un punto de aparición `Callejon_Entrada` a pie. `Llegada` en el centro: círculo libre de 3 m de radio y 6 m de alto, sin cables ni toldos encima.
3. Punto(s) de llamado que cubran el callejón y la calle frente a la casa; interior de casa y bar quedan fuera (zona prohibida).
4. Colocar `Emboscada.prefab` de Tony en la escena sin editarlo (D-029) y comprobar que sus marcadores caen sobre piso libre; si no, ajustar el callejón, no el prefab.
5. Regenerar con `CreadorTodo.Crear`.

## Carpetas permitidas
Assets/_Proyecto/Mundo/, Tests de Mundo, docs/capturas/. Leer (no editar) Pelea/Prefabs/Emboscada.prefab.

## Criterios de aceptación
- [ ] Hay ≥1 `IPuntoLlamadoMecha` habilitado; el cilindro de 3 m × 6 m sobre Llegada está libre y hay suelo debajo
- [ ] Un punto dentro del Radio, y un punto del interior de la casa fuera de todos los Radios (prueba)
- [ ] De Callejon_Entrada a Llegada hay camino a pie sin atravesar muros
- [ ] Capturas de día y de noche: docs/capturas/T-088-1.png y T-088-2.png
- [ ] `bash herramientas/verificar.sh todo` pasa

## Fuera de alcance
La lógica de la emboscada y el vuelo del mecha (Tony T-F4), llegar en carro (T-061).
