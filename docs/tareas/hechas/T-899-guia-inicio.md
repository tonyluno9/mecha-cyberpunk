# T-899: guía de estilo y menú de inicio sobre el HUD H10

- `docs/guia-estilo-ui.md` fija la base visual de todos los menús: la del HUD H10, sin papel picado ni rótulos pintados.
- `UI/Kit/TrazoH10.cs` (colores, marcos con un corte, reglas, texto en Rajdhani) y `UI/Kit/AnimH10.cs` (DOTween con tiempo sin escala) son el kit común.
- El menú de inicio deja el panel rojo a toda altura: logo arriba, panel compacto abajo a la izquierda y, a la derecha, la última partida real y un consejo. Las opciones entran escalonadas y el aviso es coral porque es una alerta.
- El aviso ahora sí se cierra con un clic (antes su imagen no recibía el rayo).

Pruebas: PlayMode `InicioTrazoH10Pruebas`, más las que ya tenía el menú. Capturas: docs/capturas/ui-inicio.png y ui-inicio-aviso.png.
