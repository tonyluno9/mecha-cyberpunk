# T-1409 Peatones por zona

Rama: tony/T-1409-peatones-zona. Tarea de Core autorizada por Tony. Carpetas: Core/Mundo/, Personaje/Visual/, Personaje/Datos/, Trafico/ (ZonaTrafico, PeatonMundo), Arte/Editor/, Tests/EditMode/PersonajeVisual/.

## Qué se hizo (código escrito sin Unity; falta correrlo)
- Core/Mundo/IZonaDelMundo.cs y ZonasMundo.cs: contrato "¿en qué zona cae este punto?". Las zonas se registran en `Registro<IZonaDelMundo>`.
- ZonaTrafico implementa IZonaDelMundo y se registra en OnEnable/OnDisable (las cajas de T-1345/T-1389a ya cubren Malecon, Puerto, Cerro, Autopista).
- Personaje/Visual/SelectorPeatonZona.cs: lógica pura (BuscarZona, Elegir con pesos y tirada inyectada).
- Personaje/Visual/ConfigPeatonesZona.cs: por zona, peso de la persona de siempre y lista de visuales con peso. Está en Personaje.Visual y no en Personaje porque PeatonVariado (que la lee) está en Visual y Personaje referencia a Visual, no al revés. El asset va en Personaje/Datos/.
- PeatonVariado: campo `configZona`. Sin config, todo igual que en T-1389b (elige en Awake). Con config, elige en Start (el grupo coloca al peatón después de crearlo): si la zona está en la tabla usa sus pesos; si no hay zona o no está, usa la lista general con su probabilidad.
- PeatonMundo: refresca la lista de renderers al esconder o restablecer el cuerpo, porque el visual por zona se crea después de su Awake.
- Arte/Editor/PeatonesZonaT1409.cs: `Aplicar()` crea ConfigPeatonesZona_Base.asset y lo conecta a Peaton.prefab. Informe en Logs/t1409-peatones-zona.txt.

## Pesos
Persona de siempre 2 en cada zona; propios de la zona 3 por variante; NP13 y NP14 1 por variante en todas.
- Malecon: NP07, NP08, NP09. Puerto: NP11. Cerro: NP10, NP12. Autopista: solo NP13, NP14.

## Pendiente de verificar con Unity
Compilar, correr `MechaCyberpunk.Arte.Editor.PeatonesZonaT1409.Aplicar` y las pruebas PeatonesZonaT1409Pruebas (EditMode). Revisar en Play que un peatón del Malecón sale pescador, turista o corredor.
