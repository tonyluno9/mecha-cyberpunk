# T-400 El jugador en tercera persona en la ciudad (y el mecha fuera)
Dueño: Tony | Rama: tony/T-400-tercera-persona (sale de tony/fase-g) | Modelo: Sonnet 5.5, medium | MODO RÁPIDO
Depende de: nada. Es la primera de la fase G (docs/diseno/pandillas.md).

## Objetivo
Abrir Builds/Juego y caminar por Ciudad_Prologo con un muñeco humanoide animado visto desde atrás del hombro: correr, saltar,
golpear y disparar la pistola apuntando con clic derecho. El mecha no aparece ni se puede llamar.

## Contexto
- Jugador actual (primera persona): `Personaje/Jugador/JugadorAPie.cs`, `Personaje/Movimiento/ControladorAPie.cs` y `MiradaAPie.cs`,
  `Personaje/Combate/GolpeAPie.cs`, `PistolaAPie.cs`, `ArmasAPie.cs`. El prefab lo genera `Personaje/Editor/CreadorJugadorAPie.cs`
  (lleva anidados HudAPie, ServiciosEconomia y ServiciosProgresion). La entrada es `Personaje/Entrada/ControlesAPie.inputactions`.
- La escena la generó Alan con `Mundo/Editor/CreadorPrologoCiudad.cs` (copia Ciudad.unity). **No la vuelvas a generar**: tiene el arte
  de T-250. Tampoco regeneres Barrio.prefab; si te ves obligado, corre después `MechaCyberpunk.Arte.Editor.ColocarT250.Colocar`.
- Decisiones: D-tony-203 (cámara), D-tony-204 (muñeco y Animator), D-tony-205 (creador de la ciudad), D-tony-206 (el mecha fuera),
  D-tony-207 (build), D-tony-222 (pruebas viejas).

## Carpetas permitidas
Personaje/, UI/HudAPie/ y UI/Editor/, Progresion/Prologo/ (Editor incluido), Assets/Arte/Personajes/ (nueva),
Escenas/Ciudad_Prologo.unity (solo con código), Tests/PlayMode/Personaje/, docs de Tony.
No tocar: Core/, Mecha/ (el código del mecha se queda igual), Radio/, ProjectSettings/, Packages/.

## Qué hacer
1. **Muñeco**: `Personaje/Editor/CreadorMunecoHumano.cs` crea en `Assets/Arte/Personajes/MunecoHumano/` un humanoide low-poly de
   cubos (1.8 m: Cadera, Torso, Cabeza, BrazoSup/Inf y PiernaSup/Inf de cada lado con su pivote, `ManoDer` vacía para el arma),
   materiales URP Lit y un AnimatorController hecho con `UnityEditor.Animations`. Parámetros: `Velocidad` (float, m/s), `EnPiso` (bool),
   `Apuntando` (bool), `Arma` (int 0 a 3), `Golpe`, `Disparo` y `Recarga` (trigger), `KO` (bool). Clips generados por código sobre las
   rotaciones de los pivotes: Quieto, Caminar, Correr (árbol de mezcla por Velocidad), Saltar, Golpe y Apuntar (capa de brazos), KO.
   Prefabs: `ModeloHumano.prefab` (el que usarán pandilleros y aliados) y `ModeloJugador.prefab` (variante para el jugador).
   El modelo final se cambia en esos prefabs sin tocar código: el código solo habla con el Animator por esos nombres.
2. **Cámara de hombro**: `Personaje/Movimiento/CamaraHombro.cs` + `Personaje/Datos/ConfigCamaraHombro.cs` (asset `_Base`): la cámara
   deja de ser hija de los ojos y órbita con el ratón. Normal: 0.55 m a la derecha, 1.55 m de alto, 3.2 m atrás, FOV 60.
   Apuntando (clic derecho): 0.7 / 1.5 / 1.6 m, FOV 48. Inclinación −40° a +60°, suavizado 12, SphereCast de 0.2 m contra muros.
3. **Movimiento**: `ControladorAPie` mueve relativo al giro de la cámara y el cuerpo gira hacia donde camina (720°/s); apuntando, el
   cuerpo mira a donde mira la cámara y camina de lado. Conserva las velocidades y el salto de `DatosPersonaje`.
   `AnimacionJugador.cs` pasa velocidad, piso, arma, apuntar, golpe, disparo y recarga al Animator del modelo.
4. **Golpe y pistola**: el golpe sale hacia donde mira el cuerpo (si apuntas, hacia la cámara). La pistola lanza el rayo desde el
   centro de la cámara para hallar el punto y luego desde la boca del arma hasta ese punto. Acción nueva `Apuntar` (clic derecho)
   en el creador de la entrada (no edites el JSON a mano). Pistola y boca van en `ManoDer`. Se apagan los visuales de primera
   persona (Manos, Cuerpo) sin borrar sus scripts. En modo Carro el modelo se oculta.
5. **HUD**: retícula en el centro solo apuntando o con un arma de fuego en la mano (`UI/HudAPie/`).
6. **Regenera** JugadorAPie.prefab con su creador.
7. **La ciudad**: `Progresion/Prologo/Editor/CreadorCiudadPandillas.cs` con `T400()` y `Todo()` (las demás tareas agregan `T401()`...).
   Abre Ciudad_Prologo, crea o rehace solo la raíz `GTA_T400` y, sin borrar nada, apaga (SetActive false) MechaEstacionado,
   Emboscada, DirectorPrologo y MostradorPeleas; apaga la ayuda "Llamar al mecha" de ConfigAyudas si sale a pie. Pon a la historia
   en etapa Libre al arrancar (mira `Progresion/Pruebas/HistoriaLibreDePrueba.cs`) para que nada mande al título ni al mecha.
   Deja al jugador en `Casa_Puerta` y la hora inicial a las 19:00. Pon en `GTA_T400` dos `MunecoAPie` de prueba a 8 y 12 m de la
   casa, para pegarles. Idempotente: correrlo dos veces deja la escena igual.
8. **Build**: `ConstruirDemo.ConstruirJuego()` (método nuevo, con `[MenuItem]`): solo Ciudad_Prologo como escena 0, salida
   `Builds/Juego/MechaCyberpunk.exe`, Windows 64. Usa `BuildPlayerOptions.scenes`; no cambies `EditorBuildSettings` ni `PlayerSettings`.
   En batchmode sale con 0 si el build salió bien.

## Pruebas (solo estas 2 nuevas, PlayMode con InputTestFixture; base en Tests/PlayMode/Personaje/BaseJugadorAPie.cs)
- `TerceraPersonaPruebas.CaminarYCamara`: W 1 s con el ratón quieto → el jugador avanzó ≥ 3 m hacia donde mira la cámara; la cámara
  queda entre 2.5 y 4 m del jugador, atrás y a su derecha. Clic derecho → la distancia baja a menos de 2 m.
- `TerceraPersonaPruebas.ApuntarYDisparar`: con la pistola (2), clic derecho y clic izquierdo hacia un `MunecoAPie` a 8 m → le baja la vida.
Compila todo. Si una prueba vieja de Personaje falla por el cambio de cámara, márcala `[Ignore("T-400: tercera persona")]` (D-tony-222).

## Al terminar
- Build: `-batchmode -quit -projectPath . -executeMethod MechaCyberpunk.Progresion.Prologo.Editor.ConstruirDemo.ConstruirJuego
  -logFile Logs/build-juego.log`. Existe Builds/Juego/MechaCyberpunk.exe. Si ProjectSettings/ cambió, `git restore ProjectSettings/`.
- docs/guia-para-jugar.md, sección "Juego de pandillas": 3 líneas de qué probar.
- Bitácora de una línea: docs/bitacora/<fecha>-tony-T-400.md. Fila de docs/estado-tony.md en "hecha". Merge a tony/fase-g.

## Sensación (Tony)
- ¿La cámara del hombro marea o se siente bien? ¿Apuntar se siente rápido? ¿El muñeco se ve vivo al correr y saltar?

## Fuera de alcance
- Las otras armas (T-403), el carro (T-401), enemigos (T-402). Rueda del ratón para cambiar arma (T-403).
