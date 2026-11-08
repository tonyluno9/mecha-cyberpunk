# T-726 HUD fuera de las cinemáticas y teclas reales en Opciones
Dueño: Tony | Rama: tony/T-726-hud-cinematicas-opciones | Modelo: Sonnet 5.5, medium
Depende de: T-717 (asalto), T-718 (teclas reales), T-722 a y e

## Objetivo
En toda cinemática el HUD se oculta y solo queda "ENTER: SALTAR", con la tecla del Input System real. Opciones > Controles muestra las teclas reales, no texto fijo.

## Qué hacer
1. `HudCinematica` (UI/HudH10): toda cinemática llama `Entrar()` y `Salir()`; con ella activa `OcultadorHudCinematica` apaga cada cuadro los lienzos del HUD (HudAPie con HudH10 y avisos, minimapa, marcador del objetivo) y al salir los enciende.
2. La tecla de saltar es la acción `SaltarCinematica` del mapa `Interfaz` (UI/Resources/ControlesInterfaz.inputactions, con Enter y Enter del teclado numérico); el letrero sale de `GetBindingDisplayString` (`HudCinematica.TextoSaltar`). `AsaltoDinero` (T-717) usa todo esto en vez de leer `Keyboard.enterKey` y de un texto fijo.
3. `ControlesHud` (UI/Resources/ControlesHud.asset) guarda el ControlesAPie real y el de Interfaz; `TeclasHud.Buscar` cae a él si no hay jugador cargado (menú de inicio).
4. `PanelOpcionesInicio.TextoControles` se arma con `ReglaControlesInicio` (pura) y las teclas reales; mapa y pausa salen del teclado.

## Carpetas permitidas
Assets/_Proyecto/UI/, Assets/_Proyecto/Progresion/Prologo/Pandillas/AsaltoDinero.cs, pruebas de UI y Progresion, docs/

## Criterios de aceptación
- [x] En la cinemática del asalto no se ve el HUD (HudH10, minimapa, marcador); solo las barras y "ENTER: SALTAR"
- [x] Al saltar o acabar, el HUD vuelve
- [x] Enter (tecla simulada) salta la cinemática por la acción del Input System
- [x] Opciones > Controles cambia si se cambia la tecla en el Input System (probado con un override de la acción Recargar)

## Pruebas automáticas
- EditMode: ReglaControlesInicioPruebas
- PlayMode: HudCinematicaT726Pruebas (UI) y AsaltoDineroT717Pruebas (HUD oculto y letrero en la cinemática real)
