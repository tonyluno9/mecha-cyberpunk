---
paths:
  - "Assets/**/*.cs"
  - "Assets/**/*.asmdef"
---

# Convenciones de C# para Unity

## Nombres
- Código en español, sin tildes ni ñ en identificadores: `RecibirDanio`, `VidaMaxima`, `Anio`. Comentarios en español con tildes.
- Namespace igual al ensamblado: `MechaCyberpunk.<Área>`, con subcarpeta si ayuda (`MechaCyberpunk.Arena.Flujo`).
- PascalCase para tipos, métodos, propiedades, eventos y constantes. camelCase para campos privados y variables locales, sin prefijos.
- Interfaces con `I`: `IRecibeDanio`. Booleanos como pregunta: `EstaKO`, `PuedeGolpear`. Métodos como verbos: `AplicarDanio`.
- ScriptableObjects de datos: `Datos*` (estadísticas que se balancean) o `Config*` (sensación y presentación). Sus assets: `DatosMecha_Jugador.asset`.

## Archivos
- Un MonoBehaviour o ScriptableObject por archivo, con el mismo nombre que la clase. Unity lo necesita para serializar.
- Un tipo por archivo también para structs, enums e interfaces públicas.

## Lógica pura separada
- Las reglas que se pueden probar (cálculos, temporizadores, máquinas de estados) van en clases C# simples, sin `MonoBehaviour` ni `UnityEngine.Object`.
- El MonoBehaviour lee la entrada, llama a la lógica y aplica el resultado a transforms, física y visuales.
- La lógica pura recibe el tiempo como parámetro (`dt`) en vez de leer `Time.deltaTime`, y recibe el azar inyectado (`System.Random` o `Func<float>`).

## Datos y números
- Nada de números mágicos. Todo valor que se ajusta al balancear sale de un ScriptableObject.
- Estadísticas de un peleador (vida, velocidad, daño, tiempos de golpe y bloqueo): `DatosMecha` y `DatosRival` en Core/.
- Valores de sensación o presentación de un área (sensibilidad del ratón, sacudida de cámara, colores de aviso): un `Config*` propio en `<Área>/Datos/`.
- Se permiten 0, 1, -1 y constantes con nombre para hechos que no se balancean (`const float GradosPorVuelta = 360f`).
- Teclas y botones salen de assets `.inputactions` o de `InputAction` configuradas en el inspector, nunca escritas en el código.

## Serialización
- `[SerializeField] private` en vez de campos públicos. Expón propiedades de solo lectura.
- `[Tooltip]` con la unidad (m, s, m/s, °/s, puntos) y `[Min]` o `[Range]` en cada valor numérico.
- Unity no serializa interfaces: usa `[SerializeField] GameObject` y `GetComponent<IInterfaz>()` en `Awake`.
- Valida las referencias en `Awake`. Si falta una, `Debug.LogError` con el nombre del objeto y `enabled = false`.

## Ciclo de vida
- Cachea componentes en `Awake`. Nada de `GetComponent`, `Find` ni `FindObjectsByType` dentro de `Update`.
- Suscríbete a eventos en `OnEnable` y desuscríbete en `OnDisable`.
- El proyecto tiene activadas las Enter Play Mode Options, así que el estado estático puede sobrevivir entre partidas.
  Todo estado estático se limpia con `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]`.

## Unity en este proyecto
- Entrada solo con Input System (`UnityEngine.InputSystem`). `UnityEngine.Input` está apagado.
- Los mechas usan `CharacterController`. Para teletransportar uno, desactívalo, mueve el transform y vuelve a activarlo.
- Sin capas ni tags personalizados: viven en ProjectSettings/, que solo cambia una tarea de Core.
  Para no golpearte a ti mismo, compara `transform.root`.
- Texto en pantallas del mundo con uGUI `Text` en un Canvas World Space. TextMeshPro queda para cuando una tarea de Core lo importe.
- Materiales greybox con URP/Lit o URP/Unlit, en `<Área>/Materiales/`.
- `Debug.Log` con prefijo del área (`[Combate] ...`) y nunca en cada frame.

## Pruebas
- NUnit. EditMode para la lógica pura; PlayMode solo cuando haga falta un MonoBehaviour vivo.
- Archivos en `Tests/EditMode/<Área>/`, clase `<Clase>Pruebas`, métodos `Metodo_Condicion_Resultado`.
- Cada prueba debe fallar si el comportamiento se rompe: sin `Assert.Pass`, sin pruebas sin aserciones.
- Si una prueba usa `EventosPelea`, llama a `EventosPelea.LimpiarSuscriptores()` en `SetUp` y `TearDown`.
