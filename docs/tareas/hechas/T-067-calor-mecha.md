# T-067 Calor policial por usar el mecha en la calle
Dueño: Tony | Rama: tony/T-067-calor-mecha | Modelo: Sonnet 5.5, medium
Depende de: T-062, T-066

## Objetivo
El calor policial existe en el juego: `ServicioCalor` registra `ICalor` (los drones y patrullas de Alan lo leerán en A-F5) y el
mecha lo sube al llamarlo y mientras se maneja por la calle. El jugador lo ve en una pantalla de la cabina y, a pie o en carro,
en el HUD cuando hay calor.

## Contexto
- Diseño: combate.md ("Usar el mecha en la calle te da calor policial"; "Cuánto calor da usarlo en la calle" es pregunta
  abierta); ciudad.md (el calor lo persiguen drones y patrullas y baja al llegar a casa: eso es de Alan con `ICalor.Borrar`).
- Contrato: Core/Calor/ICalor.cs y MotivoCalor.cs (T-034, comentarios actualizados en T-062). Modelo ya hecho:
  `Progresion/Calor/ModeloCalor.cs` con `DatosCalor_Base` (0 a 100, niveles desde 25, 50 y 75, enfría 5/s tras 10 s, D-tony-8).
- Propuesta: D-tony-31 en docs/decisiones-pendientes-tony.md (+10 al llamar, 2 por segundo dentro del mecha en la calle; en la
  arena no hay calor; dónde se ve).
- "En la calle" = el mecha está `EnCalle` según `LlamadoMecha` (T-066) y el jugador va dentro. Un mecha en arranque `Directo`
  (arena) nunca da calor. Así no hace falta saber en qué lugar está el jugador (en el mecha los triggers de Alan no lo ven).
- `ServiciosProgresion.prefab` ya va anidado en `JugadorAPie.prefab` (T-059): un servicio de calor por jugador.

## Qué hacer
1. `Progresion/Calor/ServicioCalor.cs : MonoBehaviour, ICalor`: envuelve `ModeloCalor` con `DatosCalor_Base`, llama `Avanzar` en
   `Update`, se agrega a `Registro<ICalor>` en `OnEnable` y se quita en `OnDisable`. Agrégalo a `ServiciosProgresion.prefab` con
   el creador de Progresion.
2. `Mecha/Calor/CalorPorMecha.cs` en el mecha, con `ConfigCalorMecha` (Mecha/Datos/): cuando un llamado resulta `Llega`, suma
   `calorAlLlamar` (10) con `MechaEnCalle`; cada frame que el jugador va dentro y el mecha está `EnCalle`, suma
   `calorPorSegundo * dt` (2/s). Busca el `ICalor` en el registro (y lo vuelve a buscar si aparece después); sin `ICalor`, no hace
   nada ni da error.
3. Pantalla de la cabina `UI/Cabina/PantallaCalorCabina.cs` (diegética, junto a la de vida): "CALOR" con 3 marcas que se llenan
   por nivel y una barra fina del valor; color de nivel en `ConfigPantallaCalor` (UI/Datos/). Agrégala al prefab del mecha con un
   creador de Mecha/Editor.
4. HUD a pie y en carro (UI/HudAPie/): grupo de calor visible solo con nivel mayor que 0, con el nivel en marcas naranjas.
   Textos y colores en `ConfigHudAPie`.
5. Tecla de prueba solo en escenas de pruebas: `Progresion/Pruebas/CalorDePrueba.cs` (9 suma 25, 0 borra), puesta en Pruebas_Calle.
6. Pruebas:
   - EditMode `Tests/EditMode/Mecha/CalorPorMechaPruebas.cs` si la regla de cuánto sumar queda como función pura (dentro y
     `EnCalle` suma; a pie, `EnTaller`, `Llegando` o arranque `Directo` no suma).
   - PlayMode `Tests/PlayMode/Progresion/ServicioCalorPruebas.cs`: con `ServiciosProgresion.prefab` hay un `ICalor` registrado;
     sumar, enfriar tras el retraso y borrar funcionan con el reloj de juego; al destruirlo el registro queda vacío.
   - PlayMode `Tests/PlayMode/Mecha/CalorMechaPruebas.cs` (Pruebas_Calle, entrada simulada): llamar suma 10; subir, encender y
     manejar 10 s deja el calor en 10 + 20 (±1) y nivel 1; bajar y esperar 10 s más 4 s hace que baje 20 (±1); en Pruebas_Tony
     con un `ServicioCalor` en la escena, pelear 10 s no suma calor.
   - PlayMode `Tests/PlayMode/UI/HudCalorPruebas.cs`: el grupo de calor está oculto en 0, aparece en nivel 1 y muestra 3 marcas en
     nivel 3; en modo Mecha el HUD no muestra nada y la pantalla de la cabina sí.

## Carpetas permitidas
Assets/_Proyecto/Progresion/, Assets/_Proyecto/Mecha/, Assets/_Proyecto/UI/Cabina/, Assets/_Proyecto/UI/HudAPie/,
Assets/_Proyecto/UI/Datos/, Assets/_Proyecto/UI/Editor/, Assets/_Proyecto/UI/Prefabs/HudAPie.prefab (con su creador),
Assets/_Proyecto/Escenas/Tony/Pruebas_Calle.unity (con su creador), Assets/_Proyecto/Tests/EditMode/Mecha/,
Assets/_Proyecto/Tests/PlayMode/Progresion/, Assets/_Proyecto/Tests/PlayMode/Mecha/, Assets/_Proyecto/Tests/PlayMode/UI/
No tocar: Core/ (ICalor no cambia), Personaje/ (solo su prefab se regenera si el creador de Progresion lo pide),
Pruebas_Tony.unity, las carpetas y escenas de Alan.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa
- [ ] ServicioCalorPruebas, CalorMechaPruebas y HudCalorPruebas pasan (y CalorPorMechaPruebas si existe)
- [ ] Con `JugadorAPie.prefab` solo en una escena vacía hay exactamente un `ICalor` registrado
- [ ] Capturas: cabina con calor nivel 2 (docs/capturas/T-067-1.png) y HUD a pie con calor nivel 1 (T-067-2.png)
- [ ] Consola sin errores en las pruebas

## Criterios de sensación (para un humano)
- Manejando el mecha por la calle, ¿se nota que el calor sube sin que la pantalla distraiga de la pelea?
- ¿2 puntos por segundo se siente justo (nivel 3 en unos 30 s de mecha en la calle), o hay que bajarlo o subirlo?

## Pruebas automáticas
- PlayMode: ServicioCalorPruebas, CalorMechaPruebas, HudCalorPruebas. EditMode: CalorPorMechaPruebas si aplica.

## Fuera de alcance
- Drones, patrullas, retenes y que el calor baje al llegar a casa (Alan, A-F5, con `ICalor`)
- Guardar el calor (T-F5); calor por pelear a pie o por disparar
