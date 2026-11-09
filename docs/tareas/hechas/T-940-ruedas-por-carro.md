# T-940 Ruedas por carro (hecha)

Las llantas RU01 se escalan por carro para llenar el paso de rueda. Radios en `CreadorCarrosPiezasT742.Tabla`: V01a 0.34 m, V01b 0.34 m, V05 0.32 m (antes 0.30, 0.30, 0.28). La patrulla es V05 (mismo radio). Solo V01a, V01b y V05 usan RU01; las motos no cambian.
Prefabs regenerados con `ArmarTodos` y `CreadorPatrullaPiezasT742.Armar`; `RuedasVisualesCarro.radio` sale de la misma tabla.
Capturas: docs/capturas/ui-ruedas-{v01a,v01b,v05,patrulla}.png (prueba RuedasPorCarroT940Pruebas con CAPTURAR_UI=1).
Pruebas: EditMode 87/87, PlayMode Vehiculos RuedasPuertasT742 17/17 + 4 nuevas, EscalaCarrosT696 10/10. RuedasPuertasT742 ya no fija radio 0.3.
