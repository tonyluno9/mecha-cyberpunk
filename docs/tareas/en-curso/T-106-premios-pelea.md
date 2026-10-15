# T-106 Premios de pelea
Dueño: Tony | Rama: tony/T-106-premios-pelea | Modelo: Sonnet 5.5, medium
Depende de: T-102

## Objetivo
Ganar una pelea del clandestino paga: al resultado, el flujo de pelea abona el premio a la cartera del jugador y la pantalla de
la arena lo dice ("+$600"). Cada prefab de pelea trae su premio (la casera paga menos que el campeonato del tutorial), así la
pelea es la fuente principal de dinero y la reparación se siente como costo real.

## Contexto
- Diseño: economia-misiones.md ("Las peleas del clandestino son la fuente principal de dinero"; "Ganas una pelea pero el costo
  de reparar el mecha supera el premio: quedas en negativo. Es intencional"; "Una pelea paga más que una misión de barrio"),
  personaje-progresion.md (perder: pierdes lo apostado; sin apuestas todavía, perder paga 0).
- Decisiones: D-042 (cómo encuentra todo FlujoPelea.prefab), D-066 (presentador y FlujoPeleaTutorial.prefab), D-072 (Pelea ya
  referencia Progresion). Propuestas: D-tony-44 (casera 600 / 0; tutorial 1500 / 0) y D-tony-48 (quién paga y cuándo).
- Contratos: Core/Economia/ICartera.cs (`Registro<ICartera>`, `Abonar`, `MotivoDinero.Premio`).
- Código: Pelea/Flujo/DirectorPelea.cs (`maquina.ResultadoListo` → `EventosPelea.PublicarFin`), Pelea/Flujo/PantallaPelea.cs
  (muestra GANASTE / PERDISTE y "R: revancha"), Pelea/Editor/CreadorFlujoPelea.cs y CreadorPresentador.cs (los dos prefabs),
  Economia/FormatoDinero.cs (T-102), Economia/Prefabs/ServiciosEconomia.prefab (T-102).
- En Pruebas_Tony no hay cartera (no hay jugador a pie): las pruebas instancian `ServiciosEconomia.prefab`. Pruebas_Tony no se toca.

## Qué hacer
1. `Economia/Premios/DatosPremioPelea.cs` (ScriptableObject): `premioGanar` y `premioPerder` ($, enteros >= 0). Assets en
   Economia/Datos/: `DatosPremioPelea_Casera` (600 / 0) y `DatosPremioPelea_Tutorial` (1500 / 0). Créalos con el creador de
   Economia (idempotente).
2. `DirectorPelea`: campo opcional `DatosPremioPelea premio` (Pelea referencia `MechaCyberpunk.Economia`, mismo dueño). Al
   resultado (cuando la máquina avisa `ResultadoListo`), si hay premio y el monto del lado ganador es mayor que 0, lo abona a
   `Registro<ICartera>.Actual` con `MotivoDinero.Premio` una sola vez por pelea, y avisa `event Action<int> PremioPagado`. Cada
   revancha es otra pelea y paga otra vez. Sin cartera registrada: un warning (una sola vez por director) y la pelea sigue igual.
   Sin premio asignado: nada cambia (como hoy).
3. `PantallaPelea.MostrarPremio(int monto)`: una línea "+$600" (con `FormatoDinero`, color y tamaño en `ConfigFlujoPelea`) bajo
   GANASTE, que se borra al empezar la revancha. Con monto 0 no muestra nada.
4. Creadores: `FlujoPelea.prefab` con `DatosPremioPelea_Casera` y `FlujoPeleaTutorial.prefab` con `DatosPremioPelea_Tutorial`
   (agrega la referencia a Economia en el asmdef de Pelea/Editor). Regenera los prefabs con sus creadores, conservando todo lo demás.
5. Pruebas:
   - EditMode `Tests/EditMode/Economia/DatosEconomiaAssetsPruebas.cs` (o una nueva): los dos assets de premio con sus valores, y
     cada prefab de flujo con su premio asignado.
   - PlayMode `Tests/PlayMode/Pelea/PremioPeleaPruebas.cs` (Pruebas_Tony más un `ServiciosEconomia.prefab` instanciado):
     a) gana el jugador: el dinero sube exactamente 600, una vez, con motivo `Premio`, y la pantalla muestra "+$600";
     b) pierde el jugador: el dinero no cambia y no hay línea de premio;
     c) gana, R, gana otra vez: +1200 en total; la línea se borró al empezar la revancha;
     d) sin `ServiciosEconomia`: un solo warning y la pelea termina normal, sin errores;
     e) `FlujoPeleaTutorial.prefab` (Pruebas_Tutorial): ganar paga 1500.

## Carpetas permitidas
Assets/_Proyecto/Economia/ (Premios/ nueva, Datos/, Editor/), Assets/_Proyecto/Pelea/ (Flujo/, Datos/, Editor/, Prefabs/ y su
asmdef), Assets/_Proyecto/Tests/EditMode/Economia/, Assets/_Proyecto/Tests/PlayMode/Pelea/
No tocar: Core/, IA/, Mecha/, Pruebas_Tony.unity, Pruebas_Tutorial.unity (salvo regenerarla sin cambios de contenido), la emboscada
(no paga), las carpetas y escenas de Alan.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa
- [ ] PremioPeleaPruebas (a-e) pasa; FlujoPeleaPruebas y PresentadorPruebas siguen pasando
- [ ] Ganar paga el premio exacto una vez por pelea; perder no paga (prueba automática)
- [ ] Captura docs/capturas/T-106-1.png: pantalla de la arena con GANASTE y "+$600" legibles desde la cabina
- [ ] Ningún asmdef de Tony referencia un área de Alan; Pelea sigue sin referenciar IA ni Mecha
- [ ] Consola sin errores en las pruebas (el warning de "sin cartera" solo en la prueba d)

## Criterios de sensación (para un humano)
- ¿600 por ganar la casera contra unos 650 de reparar tras una pelea dura se siente como "vivo al día", que es la idea, o es
  demasiado castigo?
- ¿"+$600" en la pantalla de la arena se nota en el momento del triunfo?

## Pruebas automáticas
- EditMode: assets de premio. PlayMode: PremioPeleaPruebas.

## Fuera de alcance
- Apuestas, premio por liga y por rival, límite de revanchas pagadas (T-F7); premio por la emboscada; mostrar el dinero en el HUD
  a pie (T-110)
