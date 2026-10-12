# T-034 Contratos de calor y objetivos
Dueño: Tony (tarea de Core, D-032, con su implementación en Progresion/) | Rama: tony/T-034-contratos-calor-objetivos | Modelo: Sonnet 5.5, medium
Depende de: T-030 (carpeta Progresion/), T-032 (`IdLugar`, `ILugar`)

## Objetivo
En Core/ existen los contratos del calor policial (`ICalor`) y del objetivo actual de la historia (`Objetivo`, `IObjetivos`),
y en Progresion/ su primera implementación en lógica pura. Así los drones y patrullas de Alan pueden reaccionar al calor
de Tony, y el mundo de Alan puede dibujar dónde está el objetivo que Tony fija.

## Contexto
- Diseño: docs/diseno/ciudad.md (con calor te persiguen drones y patrullas; baja si te escondes o llegas a casa, donde baja
  a cero), docs/diseno/combate.md (usar el mecha en la calle da calor), docs/diseno/demo.md (en el prólogo un objetivo guía
  al jugador de vuelta), docs/diseno/estetica-hud.md (a pie el HUD muestra el objetivo).
- Decisiones: D-027. Propuestas: D-tony-6 (los modelos van en el área de Tony) y D-tony-8 (calor de 0 a 100 en 3 niveles,
  vive en Progresion/) en docs/decisiones-pendientes-tony.md.
- Quién implementa: `ICalor` e `IObjetivos` los implementa Tony (Progresion/). Alan los consume desde `Registro<ICalor>` y
  `Registro<IObjetivos>` (A-F4 y A-F5) y en sus pruebas usa dobles propios. Alan también puede llamar a `Enfriar` y `Borrar`
  desde el mundo (escondites, llegar a casa) y a `Sumar` (chocar con una patrulla).

## Qué hacer
1. Core, un tipo por archivo y con comentarios `///` de contrato:
   - `Core/Calor/MotivoCalor.cs`: `enum { MechaEnCalle, Pelea, Choque, Reten, Otro }`.
   - `Core/Calor/ICalor.cs`: `float Valor` (de 0 a `Maximo`), `float Maximo`, `int Nivel` (0 sin calor; 1 a 3 según umbrales),
     `event Action<float> ValorCambio`, `event Action<int> NivelCambio` (solo cuando el nivel cambia),
     `void Sumar(float cantidad, MotivoCalor motivo)`, `void Enfriar(float cantidad)`, `void Borrar()`.
     Contrato: nunca sale de [0, Maximo]; cantidades <= 0 no hacen nada.
   - `Core/Objetivos/Objetivo.cs`: struct de solo lectura con `string Texto`, `IdLugar Lugar`, `bool TienePosicion`,
     `Vector3 Posicion`; constructores `Objetivo(string texto, IdLugar lugar)` y `Objetivo(string texto, Vector3 posicion)`;
     `static Objetivo Ninguno`.
   - `Core/Objetivos/IObjetivos.cs` (solo lectura para el mundo): `bool HayObjetivo`, `Objetivo Actual`,
     `event Action<Objetivo> Cambio`. Contrato escrito en el comentario: para dibujarlo, el mundo usa `Posicion` si
     `TienePosicion`; si no, busca en `Registro<ILugar>` el lugar con ese `Id` y usa su `PosicionEntrada`.
2. Progresion (lógica pura):
   - `Progresion/Datos/DatosCalor.cs` (ScriptableObject): `maximo` (100), `umbralesNivel` (25, 50, 75: desde ese valor se
     está en el nivel 1, 2 y 3), `retrasoEnfriamiento` (s sin sumar calor antes de que empiece a bajar, 10),
     `enfriamientoPorSegundo` (puntos/s, 5). Con `[Tooltip]` y `[Min]`, y `OnValidate` que mantiene los umbrales en orden y
     dentro de [0, maximo]. Asset `Progresion/Datos/DatosCalor_Base.asset` creado con un creador de editor
     (`Progresion/Editor/`, asmdef `MechaCyberpunk.Progresion.Editor` solo para Editor).
   - `Progresion/Calor/ModeloCalor.cs : ICalor`, construido con un `DatosCalor`. Además de `ICalor`: `void Avanzar(float dt)`
     que enfría solo después de `retrasoEnfriamiento` segundos sin `Sumar`.
   - `Progresion/Objetivos/ModeloObjetivos.cs : IObjetivos`, con `Fijar(Objetivo)` y `Quitar()`; avisa `Cambio` solo si el
     objetivo cambió (mismo texto, lugar y posición no avisa).

## Carpetas permitidas
Assets/_Proyecto/Core/Calor/, Core/Objetivos/, Assets/_Proyecto/Progresion/, Assets/_Proyecto/Tests/EditMode/Core/ y
Assets/_Proyecto/Tests/EditMode/Progresion/
No tocar: el resto de Core/, las demás áreas, escenas, ProjectSettings/, Packages/ ni herramientas/.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa con las pruebas nuevas contadas
- [ ] ModeloCalorPruebas pasa: sumar sube el valor y nunca pasa de `Maximo`; el nivel es 0, 1, 2 y 3 en 0, 25, 50 y 75
      exactos y 24.9 sigue en 0; `NivelCambio` avisa una vez al cruzar un umbral y no avisa si el nivel no cambia;
      `Avanzar` no enfría antes del retraso y después enfría `enfriamientoPorSegundo × dt`; un `Sumar` reinicia el retraso;
      nunca baja de 0; `Borrar` deja 0 y nivel 0 con un solo aviso de cada evento; cantidades <= 0 no hacen nada
- [ ] ModeloObjetivosPruebas pasa: sin objetivo `HayObjetivo` es false y `Actual` es `Ninguno`; `Fijar` avisa una vez;
      fijar el mismo objetivo no avisa; `Quitar` avisa y deja `Ninguno`; un objetivo por lugar no tiene posición
- [ ] `DatosCalor_Base.asset` existe con 100, 25/50/75, 10 s y 5 puntos/s (prueba EditMode que lo carga)
- [ ] ReglasEnsamblados sigue pasando y `git diff main --stat` solo toca las carpetas permitidas

## Criterios de sensación (para un humano)
- Alan, o Tony leyendo como Alan: con `ICalor` e `IObjetivos` sabe cómo harán sus drones para saber si persiguen y cómo
  dibujar el objetivo, sin preguntar.

## Pruebas automáticas
- EditMode (Tests/EditMode/Progresion/): ModeloCalorPruebas, ModeloObjetivosPruebas y la del asset.

## Fuera de alcance
- Componentes de escena del calor y de los objetivos, ganar calor con el mecha en la calle (T-F4)
- HUD con el objetivo (T-F3), marcador del objetivo en el mundo (Alan, A-F4)
- Persecuciones de drones y patrullas (Alan, A-F5)
