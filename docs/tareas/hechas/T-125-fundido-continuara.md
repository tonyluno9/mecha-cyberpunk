# T-125 Fundido a negro y pantalla de "continuará"
Dueño: Tony | Rama: tony/T-125-fundido-continuara | Modelo: Sonnet 5.5, medium
Depende de: nada

## Objetivo
Existen las dos pantallas de historia que usan las tareas siguientes: un fundido a negro (para tapar teletransportes, cambios de escena,
dormir y la entrada a la arena) y la pantalla de fin de la demo "CONTINUARÁ" con un resumen corto y dos salidas (seguir jugando o volver
al título). Las dos en un prefab de UI que los directores instancian o anidan.

## Contexto
- Diseño: demo.md (pregunta abierta: "qué pasa al terminar la primera pelea casera: pantalla de 'continuará' o seguir jugando"; propuesta
  D-tony-70: las dos), estetica-hud.md (HUD mínimo flotante a pie, neón; en el mecha nada flota: el fundido es la única excepción porque
  tapa toda la vista un momento).
- Decisiones: D-010 (uGUI `Text`), D-052 (HUD a pie como Canvas Screen Space Overlay). Propuesta nueva: D-tony-70.
- Ejemplo de UI hecha por creador: `UI/Editor/CreadorHudAPie.cs` y `UI/Prefabs/HudAPie.prefab`. La herramienta de capturas no dibuja
  Canvas Overlay (T-056): para las capturas se pasa el Canvas a Screen Space Camera solo en memoria.
- Entrada: solo Input System (el Input Manager viejo está apagado).

## Qué hacer
1. `UI/Historia/PantallaFundido.cs`: Canvas Overlay con el orden de dibujo más alto del proyecto y una imagen negra a pantalla completa
   que no bloquea clics cuando es transparente. `IEnumerator Fundir(float alfaFinal, float segundos)` (usa tiempo sin escala),
   `Fijar(float alfa)`, `bool Opaco`, evento `Terminado` y `static PantallaFundido Actual` (la última habilitada; null si no hay), para
   que otras áreas de Tony la encuentren sin `Find`. Un fundido nuevo corta el anterior sin saltos. Valores por defecto en
   `UI/Datos/ConfigPantallasHistoria.cs` (`ConfigPantallasHistoria_Base.asset`): 0.6 s de salida y 0.6 s de entrada.
2. `UI/Historia/PantallaContinuara.cs`: Canvas Overlay, fondo negro, "CONTINUARÁ…" grande en neón y un resumen de tres líneas que le pasan
   (`Mostrar(string alias, int dinero, bool ganoCasera)`: alias, dinero con `FormatoDinero` y "Ganaste tu primera pelea casera" o
   "Perdiste tu primera pelea casera, vuelve cuando lo repares"). Tras `segundosMinimos` (4 s) aparece "ENTER · seguir jugando   ESC ·
   salir al título"; antes de eso las teclas no hacen nada. Eventos `SeguirJugando` y `SalirAlTitulo`; `Ocultar()`. Mientras se ve, el
   cursor queda libre y el resto del juego no recibe esas teclas (no hace falta pausar el tiempo). Textos en el config.
3. `UI/Prefabs/PantallasHistoria.prefab` con las dos (creador `UI/Editor/CreadorPantallasHistoria.cs`, idempotente). Empieza
   transparente y con "continuará" oculta. No registra contratos ni busca nada al arrancar.
4. Teclas: una `InputAction` propia por tecla (Enter y Esc), sin tocar ControlesAPie ni ControlesMecha.

## Carpetas permitidas
Assets/_Proyecto/UI/ (Historia/ nueva, Datos/, Prefabs/, Editor/), Assets/_Proyecto/Tests/PlayMode/UI/, Assets/_Proyecto/Tests/EditMode/UI/,
docs/capturas/
No tocar: Core/, todo lo de Alan, escenas, el HUD a pie y las pantallas de cabina existentes.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa
- [ ] PlayMode `PantallaFundidoPruebas`: `Fundir(1, 0.6)` llega a alfa 1 entre 0.55 y 0.75 s y avisa `Terminado` una vez; un `Fundir(0, …)`
      a mitad de camino baja desde el alfa actual sin saltar a 1; transparente no bloquea un clic simulado sobre un botón debajo
- [ ] PlayMode `PantallaContinuaraPruebas` (teclado simulado): `Mostrar("EL RAYO", 1250, true)` muestra el alias, "$1,250" y el texto de
      ganar; Enter antes de 4 s no avisa nada; después de 4 s, Enter avisa `SeguirJugando` una vez y Esc avisa `SalirAlTitulo` una vez
- [ ] El prefab arranca con el fundido transparente y "continuará" oculta (prueba que lo instancia)
- [ ] Capturas docs/capturas/T-125-1.png (fundido a medias sobre una escena de pruebas) y T-125-2.png ("continuará" con resumen y teclas)
- [ ] Consola sin errores

## Criterios de sensación (para un humano)
- Mirar T-125-2.png: ¿"CONTINUARÁ…" se siente cierre de demo cyberpunk y no pantalla de error? ¿El resumen dice lo justo?
- ¿0.6 s de fundido se siente cinematográfico o lento? (se ajusta en el config)

## Pruebas automáticas
- PlayMode: PantallaFundidoPruebas, PantallaContinuaraPruebas.

## Fuera de alcance
- Quién llama al fundido y a "continuará" (T-126, T-128, T-130, T-131, T-133); subtítulos del flashback (T-131); música y sonido
