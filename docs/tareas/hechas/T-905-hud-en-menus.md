# T-905: el HUD se apaga con los menús abiertos

`OcultadorHudMenus` (lo agrega `HudH10`) apaga los lienzos del HUD mientras `PilaPantallas` tenga algo y los enciende al cerrar. Evita que la barra de vida y el minimapa se crucen con los títulos de pausa, mapa, tienda e inventario. La cinemática conserva su propio ocultador.

Revisión de T-902 a T-904: las capturas las hizo Sonnet con esta guía; las revisó el director y este fue el único defecto que pidió cambio.

Pruebas: PlayMode `PausaMapaH10Pruebas` (comprueba que el HUD se apaga), `HudCinematicaT726Pruebas`, más las de pausa, mapa, tienda e inventario. Capturas: docs/capturas/ui-pausa.png, ui-mapa.png, ui-tienda.png y ui-inventario.png.
