# T-930 Quitar los aliados para siempre

Rama: tony/T-930-sin-aliados. Playtest 09-nov.

- Borrado: AliadoAPie, AliadoEnCarro, ReglaAliado, ReglaAbordarAliado, IPandillaJugador (Core), prefabs Aliado y Aliado_AL11, CreadorAliado, CreadorAliadosT744 y sus pruebas (EditMode y PlayMode, TuPandillaPruebas incluida). Los modelos AL10/AL11 de Assets/Arte siguen.
- Marcadores temporales (mismo archivo y guid): PandillaJugador y HudAliados se quitan solos en Awake; Reclutable destruye su GameObject. Se borran cuando la limpieza final quite esos objetos de Ciudad_Prologo.
- Historia: el paso Chato ya no pide reclutar; avanza solo en el primer Update (sin objetivo, con la ayuda de apuntar y disparar). Se conserva el enum y el guardado.
- Mapa y minimapa: sin puntos de aliado ni fila ALIADOS en la leyenda. PandilleroAPie ya no apunta a aliados.
- Guardado: la clave vieja de pandilla no la lee nadie; se ignora.
- Pruebas: EditMode 2254/2254; PlayMode HistoriaPandillas, HistoriaDemo, HudCyberfix, Minimapa, MapaPlayT747, ZonaHud, MenusCyberfix, PausaMapaH10: 14 pasaron, 2 ignoradas (solo captura), 0 fallas.
