# T-401 Carros: tercera persona, subir y bajar con E, robar los estacionados
Dueño: Tony (Vehiculos/ es de Alan; Tony lo autorizó) | Rama: tony/T-401-carros-robar | Modelo: Sonnet 5.5, medium | MODO RÁPIDO
Depende de: T-400 (jugador en tercera persona, CreadorCiudadPandillas y ConstruirJuego).

## Objetivo
En la calle hay carros estacionados. Caminas a uno, pulsas E, suena su alarma, sube el calor y lo manejas con la cámara detrás.
E otra vez (parado) y bajas.

## Contexto
- `Vehiculos/Carro/CarroViejo.cs` ya es `IMontable` (Core/Jugador/IMontable.cs) y `CamaraCarro.cs` ya tiene tercera persona con C.
  El jugador sube con E desde `Personaje/Jugador/AccionMontar.cs` / `InteraccionAPie.cs`.
- En Barrio.prefab hay carros de adorno (`Carro_N`, algunos con `Carro_N_Visual_T250`). No se tocan en el prefab: se apagan en la escena.
- Calor: `Registro<ICalor>.Actual.Sumar(30, MotivoCalor.Otro)` (Core). Umbrales 25/50/75 en `Progresion/Datos/DatosCalor_Base.asset`.
- Decisiones: D-tony-208 (cámara), D-tony-209 (robo).

## Carpetas permitidas
Vehiculos/, Personaje/ (si hace falta para subir o bajar), Progresion/Prologo/Editor/CreadorCiudadPandillas.cs,
Escenas/Ciudad_Prologo.unity (solo con código), Tests/PlayMode/Vehiculos/, docs de Tony.
No tocar: Core/, Mundo/Prefabs/Barrio.prefab, Radio/.

## Qué hacer
1. **Cámara**: `CamaraCarro` arranca en tercera persona; el ratón orbita alrededor del carro (giro libre, inclinación −10° a 40°) y
   a los 1.5 s sin mover el ratón vuelve detrás del carro (ya existe `esperaRegreso`). Con más rapidez se aleja hasta +2 m
   (6 m a 0 km/h, 8 m a 80 km/h). C sigue cambiando a primera persona. Valores en `ConfigCamaraCarro_Viejo`.
2. **Estacionado y robable**: `Vehiculos/Carro/CarroEstacionado.cs`: escucha `OcupadoCambio(true)`. La primera vez: alarma 3 s
   (luces que parpadean y un sonido simple si hay clip; si no, solo luces), `ICalor.Sumar(30, Otro)` y queda como tuyo (ya no suma calor).
   El carro viejo de la historia (`CarroViejo` de la escena) es tuyo desde el inicio: no suma.
3. **Subir y bajar**: confirma que E sube a 2.5 m o menos de `PuntoSubida` y baja solo parado (ya lo decide el carro). El modelo del
   jugador se oculta dentro (T-400) y la cámara del hombro vuelve al bajar mirando hacia donde mira el carro.
4. **Escena**: `CreadorCiudadPandillas.T401()`: elige 6 `Carro_N` de adorno junto a la banqueta (2 por zona: oeste, centro, este),
   los apaga en la escena y en su lugar instancia `Vehiculos/Prefabs/CarroViejo.prefab` con `CarroEstacionado`, misma posición y giro,
   bajo la raíz `GTA_T401`. Variedad: tinte de carrocería distinto en cada uno con MaterialPropertyBlock. Idempotente.

## Pruebas (solo estas 2 nuevas, PlayMode con InputTestFixture)
- `RobarCarroPruebas.RobarSubeCalorYManeja`: jugador a 2 m de un carro con `CarroEstacionado`; E → modo Carro, calor ≥ 25, la cámara
  del carro está detrás (producto punto entre el frente del carro y carro→cámara < −0.5); W 2 s → el carro avanzó ≥ 5 m.
- `RobarCarroPruebas.BajarParado`: con el carro parado, E → modo APie, el jugador está a menos de 3 m del carro y un segundo
  subir y bajar no vuelve a sumar calor.

## Al terminar
- Build: `-executeMethod MechaCyberpunk.Progresion.Prologo.Editor.ConstruirDemo.ConstruirJuego` (ver T-400) → Builds/Juego/MechaCyberpunk.exe.
- docs/guia-para-jugar.md, sección "Juego de pandillas": 3 líneas de qué probar.
- Bitácora de una línea: docs/bitacora/<fecha>-tony-T-401.md. Fila de docs/estado-tony.md en "hecha". Merge a tony/fase-g.

## Sensación (Tony)
- ¿La cámara del carro se siente de GTA o se pierde en las vueltas? ¿Robar se siente travieso? ¿Bajar del carro es rápido?

## Fuera de alcance
- Sacar a un conductor, tráfico que se pueda robar, daño del carro, atropellar.
