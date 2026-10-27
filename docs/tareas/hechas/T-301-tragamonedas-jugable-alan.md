# T-301 Tragamonedas jugable (A-F5, casino)
Dueño: Alan | Rama: alan/T-301-tragamonedas-jugable | Modelo: Sonnet 5.5, medium
Depende de: T-300; `ICartera` e `IInteractuable` en main (Core/Economia y Core/Jugador).

## Objetivo
Una máquina que se usa con E: cobra la apuesta de la cartera, gira con una animación corta, muestra los 3 símbolos y paga.

## Qué hacer
1. `Casino/Tragamonedas/TragamonedasMundo.cs`: `IInteractuable` (registro en `OnEnable` y `OnDisable`). Texto "Jugar · $N" o "Sin dinero · $N". `Usar()` cobra con `ICartera.IntentarPagar(apuesta, MotivoDinero.Casino)`, gira con el azar inyectado y abona el premio con `Abonar(premio, MotivoDinero.Casino)`. Mientras gira, `PuedeUsar` es false. La apuesta sale de `DatosTragamonedas`.
2. Prefab `Casino/Prefabs/Tragamonedas.prefab` low-poly (cuerpo, pantalla con 3 carretes de uGUI Text World Space, luz de neón) creado por `Casino/Editor/CreadorTragamonedas.cs`.
3. Las fichas no existen todavía (T-302): esta tarea paga en dinero directo.

## Carpetas permitidas
Assets/_Proyecto/Casino/, Assets/_Proyecto/Tests/PlayMode/Casino/ (con su asmdef), docs/capturas/. No tocar Core/ ni carpetas de Tony.

## Criterios de aceptación
- [x] Con un doble de cartera: sin dinero no gira ni cobra; con dinero cobra una vez por giro; el premio se abona una vez
- [x] Con entrada simulada (E con un jugador de prueba cerca) el giro termina y el texto vuelve a "Jugar"
- [x] Sin errores en consola; captura de la máquina: docs/capturas/T-301-1.png
- [x] `bash herramientas/verificar.sh todo` pasa

## Fuera de alcance
Fichas y cajera (T-302), el interior del casino (T-303).

## Resultado (2026-10-27, Sonnet 5.5)
- `TragamonedasMundo` (IInteractuable, registro en OnEnable/OnDisable), `Casino/Prefabs/Tragamonedas.prefab` y `CreadorTragamonedas` (crea también los materiales en `Casino/Materiales/`).
- Pruebas: `Tests/PlayMode/Casino/TragamonedasMundoPruebas.cs`, 9 pruebas con doble de cartera, azar con semilla y un jugador de prueba que usa la E simulada (teclado de prueba). El jugador de prueba reproduce la regla de InteraccionAPie (más cercano a 2.5 m); no usa el código de Tony porque las áreas de dueños distintos no se referencian.
- Si apagan la máquina a media vuelta, el giro ya cobrado termina en el acto y paga el premio.
- Captura: docs/capturas/T-301-1.png (la regenera la prueba `Captura_DeLaMaquinaConUnPremio`).
- Verificado: `verificar.sh todo` con el prefab previo al ajuste de la captura: compilar OK, EditMode 942/942, PlayMode 898/907 (0 fallos; 9 omitidas, capturas de Tony). Tras el ajuste visual final (texto girado 180° para leerse de frente, letra más chica, cámara de la captura) solo se repitió PlayMode del área Casino: 9/9. No se repitió la suite completa.
