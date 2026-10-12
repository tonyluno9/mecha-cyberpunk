# T-001 Contratos de Core
Dueño: Tony | Rama: tony/T-001-contratos-core | Modelo: Sonnet 5.5, medium
Depende de: nada. Es la primera tarea y bloquea a todas las de la fase 1.
Revisión: además del subagente `revisor`, el compañero lee el pull request antes de mergear, porque sus tareas usan estos contratos.

## Objetivo
Core/ tiene los contratos con los que se hablan el jugador, el rival y el flujo de pelea: recibir daño, la información
del daño, la vida, los peleadores, los eventos de pelea y los datos del mecha y del rival. Con esto Tony y el compañero
trabajan en paralelo toda la fase 1 sin volver a tocar Core/.

## Contexto
- Diseño: docs/diseno/combate.md y docs/diseno/mecha-taller.md. Las ramas y piezas del mecha vienen después;
  aquí solo van los campos que usa la fase 1.
- Decisiones: D-004, D-005, D-006, D-007, D-009 y D-012 en docs/decisiones.md.
- Assets/_Proyecto/Core/MechaCyberpunk.Core.asmdef ya existe, sin referencias, y debe seguir así.
- Quién usa cada contrato:

| Contrato | Lo implementa | Lo usa |
| --- | --- | --- |
| IRecibeDanio, InfoDanio, ResultadoDanio | VidaMecha (T-013), rival (T-021), PeleadorDePrueba (T-021) | Golpe (T-012), rival (T-021) |
| IVida, ModeloVida | VidaMecha (T-013), rival (T-021) | Pantalla de vida (T-013), flujo (T-022) |
| IPeleador, LadoPelea | Jugador (T-013), rival (T-021) | Flujo (T-022) |
| EventosPelea, ResultadoPelea | Publica: flujo (T-022) y quien recibe daño | Sacudida de cabina (T-013), pantalla de arena (T-022) |
| DatosMecha | Assets: Mecha/Datos (T-010), IA/Datos (T-021), Combate/Pruebas (T-012) | T-010, T-012, T-013, T-021 |
| DatosRival | Asset: IA/Datos (T-021) | T-021 |

## Qué hacer
Namespace `MechaCyberpunk.Core`, un tipo por archivo, con comentarios `///` breves en español.
Los nombres y firmas de abajo son el contrato: si alguno no se puede hacer así, para y explica por qué.

1. `Core/Danio/TipoDanio.cs`: `public enum TipoDanio { Golpe }`.
2. `Core/Danio/TipoBloqueo.cs`: `public enum TipoBloqueo { Ninguno, Normal, Perfecto }`.
3. `Core/Danio/InfoDanio.cs`: `public readonly struct InfoDanio` con propiedades `float Cantidad`, `TipoDanio Tipo`,
   `Vector3 Punto`, `Vector3 Direccion` e `IPeleador Atacante` (puede ser null), y un constructor con las cinco.
4. `Core/Danio/ResultadoDanio.cs`: `public readonly struct ResultadoDanio` con `float DanioAplicado`,
   `TipoBloqueo Bloqueo` y `bool CausoKO`, constructor con los tres y `public static ResultadoDanio Ignorado`
   (0, Ninguno, false).
5. `Core/Danio/IRecibeDanio.cs`: `ResultadoDanio RecibirDanio(InfoDanio info);`. En el comentario deja escrito el contrato:
   si el que recibe ya está en KO o `Cantidad <= 0`, devuelve `ResultadoDanio.Ignorado` y no publica nada;
   en cualquier otro caso aplica el daño (o lo bloquea) y después publica `EventosPelea.PublicarImpacto` con el resultado,
   aunque un bloqueo perfecto lo haya dejado en 0.
6. `Core/Peleadores/LadoPelea.cs`: `public enum LadoPelea { Jugador, Rival }`.
7. `Core/Peleadores/IVida.cs`: `float Actual { get; }`, `float Maxima { get; }`, `bool EstaKO { get; }`,
   `event Action Cambio;` (cuando cambia `Actual`) y `event Action KO;` (una sola vez por pelea).
8. `Core/Peleadores/ModeloVida.cs`: clase C# pura que implementa `IVida`.
   - Constructor `ModeloVida(float maxima)`. Si `maxima <= 0`, lanza `ArgumentOutOfRangeException`.
   - `float Aplicar(float cantidad)`: resta y devuelve lo que de verdad restó. Nunca baja de 0.
     Si ya está en KO o `cantidad <= 0`, devuelve 0 y no avisa nada.
     Avisa `Cambio` si el valor cambió, y `KO` una sola vez al llegar a 0.
   - `void Restaurar()`: vuelve a `Maxima`, quita el KO y avisa `Cambio`. Después puede volver a caer en KO.
9. `Core/Peleadores/IPeleador.cs`:
   - `string Nombre { get; }`, `LadoPelea Lado { get; }`, `Transform Raiz { get; }`, `IVida Vida { get; }`
   - `bool ControlActivo { get; }` y `void ActivarControl(bool activo);`: el flujo congela a los dos peleadores con esto.
   - `void PrepararPelea(Pose inicio);`: restaura la vida, coloca al peleador en la pose (si tiene CharacterController,
     lo desactiva para moverlo y lo vuelve a activar), lo pone de pie y deja `ControlActivo` en false.
10. `Core/Pelea/ResultadoPelea.cs`: `public readonly struct ResultadoPelea` con `LadoPelea Ganador`
    y `float DuracionSegundos`, y su constructor.
11. `Core/Pelea/EventosPelea.cs`: `public static class EventosPelea` con
    - `event Action<int> ConteoCambio` (3, 2, 1 y 0, donde 0 significa "¡pelea!")
    - `event Action PeleaIniciada`
    - `event Action<ResultadoPelea> PeleaTerminada`
    - `event Action<IPeleador, InfoDanio, ResultadoDanio> ImpactoRecibido` (objetivo, daño, resultado)
    - `PublicarConteo(int)`, `PublicarInicio()`, `PublicarFin(ResultadoPelea)` y `PublicarImpacto(IPeleador, InfoDanio, ResultadoDanio)`
    - `public static void LimpiarSuscriptores()` para las pruebas, y un método privado con
      `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]` que la llama.
12. `Core/Datos/DatosMecha.cs`: ScriptableObject con
    `[CreateAssetMenu(menuName = "Mecha Cyberpunk/Datos del mecha", fileName = "DatosMecha_Nuevo")]`.
    Campos `[SerializeField] private` con `[Tooltip]` que diga la unidad y `[Min]` o `[Range]`, y una propiedad
    de solo lectura en PascalCase por campo. Valores iniciales:

| Campo | Unidad | Inicial | Para qué |
| --- | --- | --- | --- |
| nombre | texto | "Mecha" | Nombre en pantallas |
| vidaMaxima | puntos | 100 | Vida al empezar la pelea |
| velocidadMaxima | m/s | 4 | Velocidad al caminar |
| aceleracion | m/s² | 6 | Qué tan rápido arranca |
| desaceleracion | m/s² | 10 | Qué tan rápido frena |
| velocidadGiroMaxima | °/s | 90 | Giro máximo del cuerpo |
| danioGolpe | puntos | 10 | Daño de un golpe |
| alcanceGolpe | m | 3 | Medido desde el centro del mecha hacia adelante |
| preparacionGolpe | s | 0.25 | Del clic (o la decisión del rival) al impacto. En el rival es el aviso visible |
| recargaGolpe | s | 0.8 | Del impacto a poder golpear otra vez |
| tiempoSubirGuardia | s | 0.15 | Del clic derecho a que la guardia quede arriba |
| reduccionBloqueo | 0 a 1 | 0.7 | Parte del daño que quita un bloqueo normal |
| ventanaBloqueoPerfecto | s | 0.2 | Si la guardia quedó arriba hace menos que esto, no entra daño |

13. `Core/Datos/DatosRival.cs`: ScriptableObject con
    `[CreateAssetMenu(menuName = "Mecha Cyberpunk/Datos del rival", fileName = "DatosRival_Nuevo")]`, mismas reglas:

| Campo | Unidad | Inicial | Para qué |
| --- | --- | --- | --- |
| alias | texto | "Rival" | Nombre del rival |
| mecha | DatosMecha | vacío | Las estadísticas de su mecha |
| distanciaAtaque | m | 2.6 | A esta distancia o menos decide golpear |
| distanciaMinima | m | 2 | No se acerca más que esto |
| esperaEntreAtaquesMin | s | 1.2 | Espera mínima entre golpes |
| esperaEntreAtaquesMax | s | 2.5 | Espera máxima entre golpes |

    En `OnValidate`: si la espera máxima es menor que la mínima, iguálala a la mínima; si la distancia mínima es mayor
    o igual que la de ataque, bájala; y si `mecha` no es null y `distanciaAtaque > mecha.AlcanceGolpe`, `Debug.LogWarning`.
14. No crees assets de datos: cada dueño crea los suyos en su carpeta.

## Carpetas permitidas
Assets/_Proyecto/Core/ y Assets/_Proyecto/Tests/EditMode/Core/
No tocar: el asmdef de Core (ni agregarle referencias), las demás áreas, Escenas/, ProjectSettings/ ni Packages/.

## Criterios de aceptación (se comprueban en el editor)
Esta tarea no tiene nada jugable; sus criterios se comprueban abriendo Unity.
- [ ] Unity abre el proyecto sin errores ni advertencias nuevas en la consola
- [ ] En Assets > Create > Mecha Cyberpunk aparecen "Datos del mecha" y "Datos del rival"
- [ ] Un asset nuevo de cada tipo muestra todos los campos con sus valores iniciales, y al pasar el ratón sobre un campo se ve la unidad (borra los assets de prueba al terminar)
- [ ] En un DatosRival, si escribo una espera máxima menor que la mínima, al soltar el campo se corrige sola
- [ ] `git diff --stat main...HEAD` solo muestra archivos en Core/, Tests/EditMode/Core/ y docs/

## Pruebas automáticas
EditMode, en Tests/EditMode/Core/:
- ModeloVidaPruebas: aplicar daño baja la vida; nunca baja de 0 y devuelve lo que de verdad restó; KO se avisa una sola vez
  aunque llegue más daño; después del KO, `Aplicar` devuelve 0 y no avisa `Cambio`; cantidades de 0 o negativas se ignoran;
  `Restaurar` vuelve al máximo y permite otro KO; el constructor con máximo 0 lanza excepción.
- EventosPeleaPruebas: cada `Publicar*` llega al suscriptor con sus argumentos; `LimpiarSuscriptores` quita a los suscriptores.
- DatosPruebas: un `DatosMecha` recién creado con `ScriptableObject.CreateInstance` tiene todos sus valores mayores que 0
  y `ReduccionBloqueo` entre 0 y 1; un `DatosRival` recién creado tiene distancia mínima menor que la de ataque
  y espera mínima menor o igual que la máxima.

## Fuera de alcance
- Cualquier MonoBehaviour, escena, prefab o asset de datos
- Efectos de vida baja, munición, misiles, piezas y ramas del mecha, daño persistente
- Capas, tags y cambios en ProjectSettings/
