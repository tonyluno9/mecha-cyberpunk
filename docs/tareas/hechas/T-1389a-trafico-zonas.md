# T-1389a Tráfico con todos los carros, por zona

Rama: tony/T-1389a-trafico-zonas. Estado: código escrito SIN compilar ni correr (el worktree no tiene Library); el orquestador corre los métodos y las pruebas.

## Qué hace
- `Carro_V11` (camioneta de policía) se arma con `CreadorCarrosT1342.ArmarUno("V11", 4.7)` y de ahí sale `Trafico/Prefabs/PatrullaCamioneta.prefab` (PatrullaMundo con el cuerpo de V11, como T-707).
  `GestorPersecucion.prefabCamioneta` ya existía (T-1322, SerializeField): se conecta en `Mundo/Prefabs/Barrio.prefab`, solo ese campo. Con 5 estrellas las patrullas de más son camionetas. No se tocó Core/.
- `CarroTrafico_MT01` y `CarroTrafico_MT02`: se crean igual que V08 (otra moto que ya está en el tráfico); `CarroTrafico` no tiene código especial de motos, así que no hizo falta código nuevo.
  Entran al Malecón, al Cerro y a la Autopista.
- `Trafico/Datos/DisenoZonasTrafico.cs`: tabla de cajas y carros por zona (una sola fuente para el editor y las pruebas).
  - Malecón: V22-V24, V12, V15, V09 (taxi), MT01, MT02. Puerto: V17, V18, V16, V10, V03, V19, V04. Autopista: casi todo, V16-V18 repetidos (más camiones), MT01.
  - Cerro: V01a, V01b, V02, V03, V10, V04, V14, MT01, MT02 (sin V16-V18). Las 4 zonas viejas siguen (Avenida suma V09 y V10).
- `ZonasTraficoT1389.Aplicar()` rehace ConfigCarrosZonas (8 zonas) y los prefabs ZonaTrafico_* (incluye Autopista y Cerro). Las cajas y su colocación en la escena son de T-1364 (Arte/Editor/ColocarT1364ZonasTrafico): ahí renombré las cajas de la autopista de "Avenida" a "Autopista" y agregué la caja "Cerro" (x -480..-295, z -170..204.5). Después de Aplicar hay que correr MechaCyberpunk.Arte.Editor.ColocarT1364ZonasTrafico.Aplicar.

## Métodos
`-executeMethod MechaCyberpunk.Trafico.Editor.ZonasTraficoT1389.Aplicar` (todo). Por pasos: `.ArmarV11`, `.CrearMotos`, `.Crear`, `.CrearZonas`, y `CreadorPatrullaCamionetaT1389.Crear`.

## Pruebas
`TraficoZonasT1389Pruebas` (EditMode, Tests/EditMode/Trafico). Corren después de Aplicar.

## Pendiente / notas
- Los tipos de V12-V24 (convertible, taxi...) se infirieron de T-1345; ajustar listas en DisenoZonasTrafico si alguno no corresponde.
- ZonaTrafico.De devuelve la primera zona activa: por eso las cajas no se encimen.
- El Barrio.prefab es de Alan (Mundo/): solo cambia `prefabCamioneta`.
