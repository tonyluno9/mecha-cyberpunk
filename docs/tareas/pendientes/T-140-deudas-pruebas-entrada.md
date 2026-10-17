# T-140 Deudas: el cuelgue de PlayMode de Economia y un helper para el estado de los mapas de acciones
Dueño: Tony | Rama: tony/T-140-deudas-pruebas-entrada | Modelo: Sonnet 5.5, medium
Depende de: nada (es la primera de T-F7)

## Objetivo
Toda la suite de PlayMode corre de punta a punta en una sola llamada, incluido el ensamblado de Economia, que hoy se cuelga en esta
máquina en cuanto una prueba instancia al jugador. El arreglo es de raíz, no "correr un ensamblado por llamada". Y la limpieza del estado de
los mapas de acciones (hoy copiada en 8 archivos de pruebas) queda en un solo helper que usan todas las pruebas que la necesitan. Sin esto,
el bloque de economía de T-F7 no se puede verificar: casi todas sus pruebas viven en Economia o instancian `ServiciosEconomia`.

## Contexto
- docs/revision-tony.md, "T-F6 cerrada": "Economia PlayMode: se cuelga al correr cualquier prueba que instancia el jugador (probado también
  en el commit de T-107, antes de T-129: no es regresión de T-F6)". docs/bitacora/2026-10-17-tony-T-129.md: "esa corrida se queda colgada
  tras el primer aviso de ServicioCartera" y "correr varios ensamblados de PlayMode en una sola llamada con un Unity residual vivo cuelga la corrida".
- Pistas (sin confirmar): `ServicioCarteraPruebas` y `ServicioRentaPruebas` son `InputTestFixture`, instancian `JugadorAPie.prefab` y NO
  descartan el estado de los mapas de acciones; `PeleaCaseraPruebas` (T-129) encontró que sin descartarlo las pruebas siguientes heredan
  controles liberados (`ArgumentNullException statePtr`). Puede ser la misma causa o no: hay que demostrarlo con el log.
- Código repetido que se vuelve helper: `ReiniciarMapasDeAcciones` y el `TearDown` de `Tests/PlayMode/Progresion/PeleaCaseraPruebas.cs`, y sus
  copias en AyudaCabinaPruebas, CamaEspejoPruebas, FlashbackPruebas, PrologoNochePruebas, PrologoPresentePruebas, RecorridoPrologoPruebas y
  AyudasControlesPruebas.
- Hallazgo de T-135: `InputTestFixture.Press` justo después de `Release` en el mismo cuadro vuelve a pegar la tecla (`Encender()` de
  PrologoNochePruebas y RecorridoEmboscadaPruebas). No falla hoy, pero es la misma familia de fragilidad.
- Precedente de helper compartido por pruebas de varias áreas: `Progresion/Pruebas/MedidorFrames.cs` (todas las pruebas de PlayMode de Tony
  referencian `MechaCyberpunk.Progresion`).
- Propuesta: D-tony-77.

## Qué hacer
1. Reproducir y diagnosticar, con Unity cerrado y un límite de tiempo por corrida (`timeout 900` en Bash), corriendo el ensamblado de
   Economia por clase (`-testFilter MechaCyberpunk.Tests.Economia.ServicioCarteraPruebas`, luego `ServicioRentaPruebas`) y después por prueba.
   Anota en la bitácora: qué prueba es la primera que no termina, las últimas 40 líneas del log antes del cuelgue y la causa demostrada
   (con la prueba que lo confirma: el cuelgue aparece sin el arreglo y desaparece con él). Antes de cada corrida confirma que no queda un
   Unity.exe de editor vivo de una corrida anterior (si queda uno tuyo, ciérralo; si es de la persona, detente como dice CLAUDE.md).
2. Arreglar la causa de raíz donde esté (prueba, servicio o prefab). Si la causa está en código del juego (Economia/ o Personaje/), el
   cambio es el mínimo y la bitácora lo justifica. Prohibido: `[Ignore]`, `[Timeout]` como "arreglo", quitar pruebas o marcarlas explícitas.
3. Helper `Progresion/Pruebas/MapasDeAccionesDePrueba.cs` (estático, todo dentro de `#if UNITY_EDITOR`, como MedidorFrames): un método para
   el Setup (descarta el estado de resolución de todos los `InputActionAsset` del proyecto con los dispositivos nuevos del fixture) y uno para
   el TearDown (antes y después de `base.TearDown()`, como hace hoy PeleaCaseraPruebas). Documenta en su `///` por qué existe y cuándo
   llamarlo (cualquier `InputTestFixture` que instancie un prefab con `.inputactions` o cargue una escena de Tony).
4. Usar el helper en las 8 pruebas que hoy copian el código (borrando las copias) y en las dos clases de Tests/PlayMode/Economia/.
5. Quitar el `Press` tras `Release` en el mismo cuadro en PrologoNochePruebas y RecorridoEmboscadaPruebas (un `yield return null` entre los
   dos, o el patrón que deje la bitácora), y buscar con Grep el mismo patrón en el resto de Tests/PlayMode de Tony y corregirlo igual.
6. Bitácora con el diagnóstico (paso 1) y la lista de archivos que pasaron al helper.

## Carpetas permitidas
Assets/_Proyecto/Tests/PlayMode/ de las áreas de Tony (Economia, Mecha, Progresion, UI, Pelea, Personaje, Combate, IA),
Assets/_Proyecto/Progresion/Pruebas/, y solo si el diagnóstico lo demuestra necesario: Assets/_Proyecto/Economia/ y Assets/_Proyecto/Personaje/
No tocar: Core/, herramientas/ (el modo por área de verificar.sh es de T-141), ProjectSettings/, Packages/, todo lo de Alan, escenas.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` termina (sin colgarse) en una sola llamada, con PlayMode de todas las áreas de Tony incluida
      Economia; los únicos fallos permitidos son los 9 conocidos de Alan en Arena (JaulaClandestinoPruebas, cápsula x8 y cubo), listados por nombre
- [ ] PlayMode solo de Economia (`-assemblyNames MechaCyberpunk.Tests.PlayMode.Economia`) pasa 3 corridas seguidas, cada una en menos de 5 minutos
- [ ] PlayMode de Progresion y Economia juntos en una llamada (en ese orden en `-assemblyNames`) pasa
- [ ] La bitácora nombra la prueba donde se colgaba, la causa y la evidencia de que el arreglo la quita (corrida sin y con el arreglo)
- [ ] Grep: ningún archivo de Tests/PlayMode tiene su propia copia de `asset.devices = new InputDevice` (solo el helper)
- [ ] Grep: no queda `Release(...)` seguido de `Press(...)` de la misma tecla sin un `yield` entre ellos en Tests/PlayMode de Tony

## Criterios de sensación (para un humano)
- Ninguno: no cambia nada jugable.

## Pruebas automáticas
- Las existentes; si el diagnóstico encuentra una causa reproducible, una prueba de PlayMode en Economia que la cubra (por ejemplo: dos
  pruebas seguidas que instancian el jugador y pulsan una tecla simulada, la segunda responde).

## Fuera de alcance
- El modo "PlayMode por área con límite de tiempo" de verificar.sh (T-141). Los 9 fallos de Alan en Arena. Fragilidades por tiempo de frame ya
  tratadas en T-122.
