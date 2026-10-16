# T-122 Pruebas frágiles: tope de frame, ritmo del rival y materiales que cambian solos
Dueño: Tony | Rama: tony/T-122-pruebas-fragiles | Modelo: Sonnet 5.5, medium
Depende de: nada (conviene después de T-121 porque las dos regeneran cosas de Pruebas_Calle)

## Objetivo
`verificar.sh todo` pasa a la primera en esta máquina y deja el árbol limpio, para que el recorrido largo de T-F6 (T-135) no herede
fallos al azar: los recorridos ya no fallan por un solo frame lento, `RivalBasicoPruebas.Ritmo_*` mide bien, y correr las pruebas no
modifica materiales del proyecto.

## Contexto
- Revisión de T-F5 (docs/revision-tony.md, deuda d y notas de T-104, T-105, T-107, T-108): `RecorridoEmboscadaPruebas` y
  `RecorridoAPiePruebas` fallan a veces porque exigen que ningún frame dure más de 100 ms (`MaxFrameSegundos`); los primeros frames
  y los tirones del editor en batch lo rompen. `RivalBasicoPruebas.Ritmo_CuatroGolpesSeguidos` es intermitente. `Rival_Cuerpo.mat`,
  `Cabina_Grietas.mat` y `Cabina_AlarmaPrendida.mat` cambian solos al correr las pruebas (los crean `CreadorRivalBasico` y
  `CreadorDanioCabina`; algo los vuelve a escribir o los modifica en Play).
- D-070: una prueba inestable se arregla en la medición, no se afloja lo que comprueba. Propuesta nueva: D-tony-60.
- `ProjectSettings/ProjectSettings.asset` que cambia solo en `SENTIS_ANALYTICS_ENABLED` NO es de esta tarea (regla 10 de CLAUDE.md).

## Qué hacer
1. `Progresion/Pruebas/MedidorFrames.cs`: clase C# pura (sin MonoBehaviour) que recibe la duración de cada frame y decide si el
   recorrido cumple el tope: ignora los frames de un calentamiento (por tiempo, `segundosCalentamiento`, por defecto 2 s), y cumple si
   como mucho `maxFramesLentos` (por defecto 3, o el 1 % de los frames medidos si es mayor) pasan de `topeBlando` (0.1 s) y ninguno
   pasa de `topeDuro` (0.5 s). Da un resumen de texto (frames medidos, máximo, percentil 99, cuántos lentos) para el mensaje del assert.
   Va en Progresion/Pruebas/ porque todos los ensamblados de pruebas de Tony referencian Progresion.
2. `RecorridoEmboscadaPruebas`, `RecorridoAPiePruebas` y `RecorridoTallerPruebas` (y cualquier otra de Tony con `MaxFrameSegundos`)
   usan `MedidorFrames` en lugar de su máximo propio. El mensaje de fallo incluye el resumen.
3. `RivalBasicoPruebas.Ritmo_*`: encuentra por qué falla a veces (mide en frames o en tiempo real lo que depende del tiempo de juego,
   espera fija demasiado justa, etc.) y arregla la medición sin cambiar lo que comprueba. Anota la causa en el resumen.
4. Materiales: encuentra qué escribe `Rival_Cuerpo.mat`, `Cabina_Grietas.mat` y `Cabina_AlarmaPrendida.mat` durante las pruebas (un
   creador que se corre desde una prueba, un `renderer.material`/`sharedMaterial` modificado en Play sobre el asset, un
   `EditorUtility.SetDirty` sin cambios reales) y quítalo: en Play se usa una instancia del material o un `MaterialPropertyBlock`, y los
   creadores no reescriben un material idéntico.
5. Corre `bash herramientas/verificar.sh todo` dos veces seguidas desde un árbol limpio.

## Carpetas permitidas
Assets/_Proyecto/Progresion/Pruebas/MedidorFrames.cs (nuevo), Assets/_Proyecto/Tests/EditMode/Progresion/,
Assets/_Proyecto/Tests/PlayMode/ de las áreas de Tony (Pelea, Personaje, Mecha, IA), y solo para el punto 4 el código que modifica los
materiales en IA/ y Mecha/ (y sus creadores)
No tocar: Core/, todo lo de Alan, escenas, ProjectSettings/, herramientas/ (si `verificar.sh` necesitara un cambio, se anota como tarea de Core).

## Criterios de aceptación (los comprueba el verificador)
- [ ] EditMode `MedidorFramesPruebas`: ignora el calentamiento; 3 frames de 0.2 s cumplen y 4 no (con pocos frames medidos); uno de 0.6 s
      no cumple; el resumen trae el máximo y el percentil 99
- [ ] `bash herramientas/verificar.sh todo` pasa dos veces seguidas (salvo los fallos de Alan conocidos de `JaulaClandestinoPruebas`)
- [ ] `RecorridoEmboscadaPruebas`, `RecorridoAPiePruebas` y `RivalBasicoPruebas.Ritmo_*` pasan en 3 corridas seguidas de PlayMode
- [ ] Después de las dos corridas, `git status` no muestra cambios en Assets/ (ni en esos tres materiales); solo puede aparecer
      ProjectSettings.asset por `SENTIS_ANALYTICS_ENABLED`, que se restaura
- [ ] Ninguna prueba subió `MaxFrameSegundos` ni quitó una comprobación: el verificador compara con `git diff tony/fase-5`

## Criterios de sensación (para un humano)
- Ninguno.

## Pruebas automáticas
- EditMode: MedidorFramesPruebas. PlayMode: las de recorrido y RivalBasicoPruebas, ya existentes.

## Fuera de alcance
- Fallos de Alan (`JaulaClandestinoPruebas`); acelerar las pruebas largas; cambiar herramientas/verificar.sh
