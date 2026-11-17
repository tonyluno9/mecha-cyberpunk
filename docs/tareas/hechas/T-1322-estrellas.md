# T-1322: estrellas y respuesta policial

Rama `tony/T-1322-estrellas`. Viene de docs/playtest-17nov-b.md, sección 4. Sin compilar ni correr pruebas (no había Unity): lo hace el orquestador.

## Qué se hizo
- Core/Calor/EstrellasBusqueda: constantes de estrellas por acción (disparar a patrulla 2, reventarla 3, disparar a policía 2, matarlo 3, disparar a un carro frente a la policía 2), `SubirA(calor, estrellas)`, `EnMaximo(estrellas)` y el evento `EstrellasCambiaron(int)`.
- Gancho para T-1323: `EstrellasBusqueda.EstrellasCambiaron` + `EnMaximo(5)` (el valor máximo es 5; "pasar de 5" se engancha ahí). Lo publica GestorPersecucion.
- Trafico/Persecucion/PatrullaMundo: disparar a la patrulla = 2 estrellas, reventarla = 3.
- IA/APie/PandilleroAPie: disparar a un policía = 2, matarlo = 3.
- Trafico/Persecucion/VigilanteDisparoACarros + ReglaDisparoACarro: un carro (no patrulla) pierde vida justo después de un disparo, a menos de 80 m del jugador y con un policía que lo ve (`TestigosDelito.VeUnPolicia`): 2 estrellas en ese momento, sin esperar la explosión.
- GestorPersecucion: con 5 estrellas suma `PatrullasExtraCincoEstrellas` (3) y `DronesExtraCincoEstrellas` (3) a lo del nivel (se reajusta al cruzar las 5, aunque el nivel de calor no cambie). Las patrullas extra usan el prefab de camioneta si existe (`PonerCamioneta` / campo `prefabCamioneta`), si no la patrulla actual. Usa los drones de Trafico.
- RadioPolicia + `ConfigPersecucion.IdSonidoRadio`: suena al bajar los policías de una patrulla y al llegar a 5 estrellas (enfriamiento 15 s).
- ConfigPersecucion: campos nuevos con valor por defecto en código (el asset existente no cambia).

## Aplicar
No hay: no se tocó ninguna escena ni prefab ni asset. (El hueco de la camioneta es opcional y se llena con `GestorPersecucion.PonerCamioneta` o el campo serializado.)

## Pruebas
EditMode: Tests/EditMode/Trafico/EstrellasT1322Pruebas.cs. No hay PlayMode (no se pudo correr nada en esta sesión).

## Decisiones
- El calor sube en pasos del 5 % hasta alcanzar las estrellas pedidas (los umbrales viven en Progresion). Con los datos actuales el nivel 1 (25) ya muestra 2 estrellas, así que "2 estrellas" no cambia al primer golpe pero reventar (3) y matar policía (3) sí suben más.
- No se tocó RespuestaNova ni lo de Nova, ni la persecución del inicio, ni barriles.
- "Más policías" = más patrullas (cada una baja a sus policías); no se cambió PoliciasPorPatrulla.
- Dron derribado sigue enfriando 25 (no se pidió cambiarlo); el tiempo de escape no cambia.
- Posible riesgo: pruebas existentes que asuman calor exacto tras golpear a un policía (SubirAPrimeraEstrella sigue existiendo pero ya no se llama).

## Assets que faltan
- Modelo de camioneta (hueco: `prefabCamioneta` de GestorPersecucion).
- Naves que bajan policías con sogas: NO se hicieron (falta el modelo).
- Sonido de radio de policía: no existe en Assets/Arte/Audio. Hueco: `IdSonidoRadio` vacío. Al llegar el clip, agregar el ID a CatalogoSonidos, a ReglaMezclaSonidos (IdsEncendidos) y sus pruebas, y ponerlo en ConfigPersecucion_Trafico.
