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
- [ ] Con un doble de cartera: sin dinero no gira ni cobra; con dinero cobra una vez por giro; el premio se abona una vez
- [ ] Con entrada simulada (E con un jugador de prueba cerca) el giro termina y el texto vuelve a "Jugar"
- [ ] Sin errores en consola; captura de la máquina: docs/capturas/T-301-1.png
- [ ] `bash herramientas/verificar.sh todo` pasa

## Fuera de alcance
Fichas y cajera (T-302), el interior del casino (T-303).
