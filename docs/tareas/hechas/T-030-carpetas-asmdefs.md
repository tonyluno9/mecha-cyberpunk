# T-030 Carpetas y ensamblados por dueño
Dueño: Tony (tarea de Core, D-032) | Rama: tony/T-030-carpetas-asmdefs | Modelo: Sonnet 5.5, medium
Depende de: nada (sale de main, que ya tiene T-001, T-002 y la fase 1)

## Objetivo
Existen las carpetas y los asmdefs de todas las áreas de D-028, las pruebas quedan en un ensamblado por área y
`herramientas/verificar.sh` las corre todas. Con esto Alan empieza Mundo/ sin tocar nada compartido, y una prueba vigila
que ningún área referencie a un área de la otra persona.

## Contexto
- Decisiones: D-004 (un asmdef por área), D-026 (Tony = sistemas, Alan = mundo), D-028 (carpetas nuevas), D-029 (escenas),
  D-032 (Core lo hace Tony). Propuesta de esta tarea: D-tony-1 en docs/decisiones-pendientes-tony.md.
- Hoy hay un solo ensamblado de pruebas por modo: `Tests/EditMode/MechaCyberpunk.Tests.EditMode.asmdef` y
  `Tests/PlayMode/MechaCyberpunk.Tests.PlayMode.asmdef`, que referencian todas las áreas. Si Alan agrega pruebas ahí,
  los dos editan el mismo archivo y una prueba rota de uno tumba las del otro. Por eso se parte en un ensamblado por área.
- Las pruebas de Tony ya cruzan áreas propias (por ejemplo `Tests/PlayMode/Combate/VidaJugadorPruebas.cs` usa Mecha, Combate
  y UI). Por eso cada ensamblado de pruebas de Tony referencia todas las áreas de Tony, y los de Alan todas las de Alan.
- `herramientas/verificar.sh` (línea con `-assemblyNames "MechaCyberpunk.Tests.$modo"`) corre un solo ensamblado. Unity
  acepta varios nombres separados por `;` en `-assemblyNames`.
- Es una tarea de Core: en cuanto el verificador la apruebe se integra a main con push (paso 1e de la skill trabajar),
  porque Alan la necesita para crear Mundo/.

## Qué hacer
1. Crea un asmdef por carpeta nueva, con el mismo formato que `Mecha/MechaCyberpunk.Mecha.asmdef`
   (`name` y `rootNamespace` = `MechaCyberpunk.<Área>`, `autoReferenced: true`):

   | Carpeta | Dueño | Referencias |
   | --- | --- | --- |
   | Pelea/ | Tony | MechaCyberpunk.Core, Unity.InputSystem, UnityEngine.UI |
   | Personaje/ | Tony | MechaCyberpunk.Core, Unity.InputSystem |
   | Economia/ | Tony | MechaCyberpunk.Core |
   | Progresion/ | Tony | MechaCyberpunk.Core |
   | Radio/ | Tony | MechaCyberpunk.Core, Unity.InputSystem |
   | Mundo/ | Alan | MechaCyberpunk.Core, Unity.InputSystem, UnityEngine.UI |
   | Vehiculos/ | Alan | MechaCyberpunk.Core, Unity.InputSystem |
   | Casino/ | Alan | MechaCyberpunk.Core, Unity.InputSystem, UnityEngine.UI |
   | Trafico/ | Alan | MechaCyberpunk.Core |

   No cambies los asmdefs que ya existen (Core, Mecha, Combate, UI, IA, Arena ni los de Editor).
2. Crea `Assets/_Proyecto/Escenas/Tony/` con un `.gitkeep`.
3. Parte las pruebas en un ensamblado por área:
   - Borra `Tests/EditMode/MechaCyberpunk.Tests.EditMode.asmdef` y `Tests/PlayMode/MechaCyberpunk.Tests.PlayMode.asmdef`
     (con su .meta).
   - Crea `Tests/EditMode/<Área>/MechaCyberpunk.Tests.EditMode.<Área>.asmdef` y
     `Tests/PlayMode/<Área>/MechaCyberpunk.Tests.PlayMode.<Área>.asmdef` para Core, las 9 áreas de Tony (Mecha, Combate, UI,
     IA, Pelea, Personaje, Economia, Progresion, Radio) y las 5 de Alan (Arena, Mundo, Vehiculos, Casino, Trafico).
   - Copia de los asmdefs actuales: `rootNamespace` MechaCyberpunk.Tests, `overrideReferences: true` con `nunit.framework.dll`,
     `defineConstraints` UNITY_INCLUDE_TESTS, `autoReferenced: false`; EditMode con `includePlatforms: ["Editor"]` y PlayMode con
     `includePlatforms: []`.
   - Referencias: todas llevan UnityEngine.TestRunner, UnityEditor.TestRunner, Unity.InputSystem y MechaCyberpunk.Core; las de
     PlayMode también Unity.InputSystem.TestFramework. Además:
     - Core: nada más.
     - Áreas de Tony: las 9 áreas de Tony y UnityEngine.UI.
     - Áreas de Alan: las 5 áreas de Alan y UnityEngine.UI.
   - Las carpetas de pruebas que quedan sin .cs llevan un `.gitkeep`. No muevas ni edites el código de las pruebas existentes.
4. `herramientas/verificar.sh`:
   - Arma la lista de ensamblados de cada modo sola: cada `Assets/_Proyecto/Tests/<Modo>/*/MechaCyberpunk.Tests.<Modo>.*.asmdef`
     cuya carpeta tenga al menos un `.cs` (en ella o en subcarpetas). Pásala unida con `;` a `-assemblyNames`.
     Así un área nueva con pruebas entra sola y una carpeta vacía no estorba.
   - Agrega el modo `bash herramientas/verificar.sh ensamblados`: imprime, sin abrir Unity, la lista de EditMode y la de
     PlayMode, un nombre por línea, y sale con 0.
   - Si la lista de un modo queda vacía, ese paso sale como `FALLO` (igual que hoy con `total=0`). El resto del formato de
     salida y los códigos de salida no cambian.
5. Prueba `Tests/EditMode/Core/ReglasEnsambladosPruebas.cs`:
   - Lee todos los `.asmdef` bajo `Assets/_Proyecto/` (System.IO y `JsonUtility` a una clase serializable con `name` y
     `references`).
   - Saca el dueño de cada uno por su nombre: `MechaCyberpunk.Core` y `MechaCyberpunk.Tests.<Modo>.Core` son de Core;
     `MechaCyberpunk.<Área>`, `MechaCyberpunk.<Área>.Editor` y `MechaCyberpunk.Tests.<Modo>.<Área>` son del dueño del área
     (tabla de dueños escrita en la prueba, la misma de CLAUDE.md).
   - La regla va en un método estático que recibe los datos y devuelve la lista de errores, para poder probarla con datos
     inventados. Errores: Core referencia un ensamblado del proyecto; un ensamblado referencia uno de la otra persona;
     falta el asmdef de un área de D-028; hay un asmdef con un nombre que no está en la tabla (un área nueva se agrega a la
     tabla a propósito); una referencia escrita como `GUID:` (en este proyecto se usan nombres).
   - Casos: el proyecto real no da errores; datos inventados con IA → Mundo dan un error que nombra a los dos; Core → Mecha
     da error; un nombre desconocido da error.
6. Actualiza docs/herramientas-verificacion.md: el párrafo de `-assemblyNames`, el modo `ensamblados` y la regla de
   "una prueba nueva solo cuenta si su carpeta tiene asmdef".

## Carpetas permitidas
Assets/_Proyecto/Pelea/, Personaje/, Economia/, Progresion/, Radio/, Mundo/, Vehiculos/, Casino/ y Trafico/ (solo el asmdef y
su .meta), Assets/_Proyecto/Escenas/Tony/.gitkeep, Assets/_Proyecto/Tests/EditMode/ y Assets/_Proyecto/Tests/PlayMode/ (solo
asmdefs, .gitkeep y la prueba nueva), herramientas/verificar.sh y docs/herramientas-verificacion.md.
No tocar: código de Core/, Mecha/, Combate/, UI/, IA/ y Arena/ ni sus asmdefs, el código de las pruebas existentes, escenas,
prefabs, ProjectSettings/, Packages/ ni CLAUDE.md.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo`: compilar OK; PlayMode 64/64 (las mismas pruebas que en main: ninguna se perdió
      al partir el ensamblado); EditMode 51 más las nuevas de ReglasEnsamblados, todas pasan
- [ ] `bash herramientas/verificar.sh ensamblados` lista en EditMode Core, Mecha y Combate, y en PlayMode Core, Mecha y
      Combate (las carpetas que hoy tienen pruebas), y no lista las carpetas vacías
- [ ] ReglasEnsamblados pasa con el proyecto real y su caso inventado IA → Mundo da error
- [ ] Las 9 carpetas nuevas tienen su asmdef con las referencias de la tabla (lectura de los archivos)
- [ ] `git ls-files` muestra Escenas/Tony/.gitkeep y los .gitkeep de las carpetas de pruebas vacías, cada uno con el .meta de
      su carpeta
- [ ] `git diff main --stat` no toca nada fuera de las carpetas permitidas
- [ ] El log de compilación no tiene advertencias nuevas de asmdefs (por ejemplo, referencias que no se resuelven)

## Criterios de sensación (para un humano)
- Alan, o Tony leyendo como Alan: las referencias de Mundo, Vehiculos, Casino y Trafico le alcanzan para A-F1 y A-F2.

## Pruebas automáticas
- EditMode (Tests/EditMode/Core/): ReglasEnsambladosPruebas, con los casos del paso 5.
- Todas las existentes deben seguir pasando con el mismo conteo.

## Fuera de alcance
- Código de las áreas nuevas, carpetas Datos/, Pruebas/, Prefabs/ o Editor/ dentro de ellas (las crea la tarea que las use)
- Contratos de Core (T-031 a T-034)
- Mover Arena/ o cambiar sus referencias
