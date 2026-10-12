# T-063 Deudas menores de T-F3: prueba inestable, HUD repetido, textos del HUD y captura
Dueño: Tony | Rama: tony/T-063-deudas-t-f3 | Modelo: Sonnet 5.5, medium
Depende de: nada (T-F3 integrada en tony/fase-3)

## Objetivo
Quitar las cuatro observaciones menores que dejó la revisión de T-F3, antes de que T-F4 toque Combate y el HUD: la prueba
`GolpeBloqueoPruebas.Golpe_ClicsSeguidos...` deja de fallar al azar, las pruebas del HUD dejan de crear un segundo HUD, los textos
fijos del HUD pasan a datos y la captura T-059-2 muestra el aviso de cerrado que pedía su criterio.

## Contexto
- docs/revision-tony.md (T-057, T-059) y docs/bitacora/2026-10-12-tony-T-059.md: la prueba mide con `Time.time` y falla con la
  máquina cargada; HudAPiePruebas y BaseHudObjetivo instancian `JugadorAPie.prefab` (que desde T-059 ya trae su HUD anidado) y
  además otro `HudAPie.prefab`; la captura T-059-2 salió con "E · Abrir Clandestino" en vez del aviso de cerrado por horario.
- D-005: los valores y textos de presentación van en un `Config*` del área. FormatoHud tiene textos fijos que ve el jugador
  ("RADIO · sin señal", "REGRESA", el de cerrado por horario y similares).
- Regla de la prueba inestable: se arregla la medición, no se afloja lo que comprueba (sigue exigiendo preparación + recarga
  entre impactos, con la misma tolerancia de 0.05 s).

## Qué hacer
1. `Tests/PlayMode/Combate/GolpeBloqueoPruebas.cs`: haz determinista `Golpe_ClicsSeguidos_SalenAlRitmoDeLaRecarga...`. Por
   ejemplo, mide los tiempos con el tiempo de juego acumulado por frame (`Time.deltaTime` sumado en la prueba) o fija
   `Time.captureDeltaTime` durante la prueba y lo restaura al final. Mismas aserciones.
2. Pruebas del HUD (`Tests/PlayMode/UI/HudAPiePruebas.cs`, `BaseHudObjetivo.cs` y cualquier otra que lo haga): usa el HUD anidado
   del jugador en vez de instanciar otro `HudAPie.prefab`. Lo mismo con `ServiciosProgresion.prefab` si se duplica.
3. Textos del HUD a datos: los textos fijos que ve el jugador en `UI/HudAPie/FormatoHud.cs` (y en `TextoInteraccion.cs` si los
   tiene) pasan a `ConfigHudAPie` con los mismos valores por defecto; FormatoHud los recibe como parámetro o lee el config. El
   asset `ConfigHudAPie_Base.asset` se actualiza con el creador de UI (no a mano). Las pruebas de EditMode de FormatoHud y
   TextoInteraccion se ajustan para pasar los textos y siguen comprobando lo mismo.
4. Captura: vuelve a tomar docs/capturas/T-059-2.png en Pruebas_APie frente al Clandestino con el reloj de prueba fuera de
   20:00-06:00, de modo que se lea el aviso "CERRADO · abre 20:00" (o el texto equivalente del config).

## Carpetas permitidas
Assets/_Proyecto/Tests/PlayMode/Combate/GolpeBloqueoPruebas.cs, Assets/_Proyecto/Tests/PlayMode/UI/,
Assets/_Proyecto/Tests/EditMode/UI/, Assets/_Proyecto/UI/HudAPie/, Assets/_Proyecto/UI/Datos/, Assets/_Proyecto/UI/Editor/,
docs/capturas/T-059-2.png
No tocar: Core/, Combate/ (solo su prueba), Personaje/, las carpetas y escenas de Alan, Pruebas_Tony.unity.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa
- [ ] La prueba de Combate pasa 5 corridas seguidas de PlayMode (filtro `-testFilter GolpeBloqueoPruebas`), con las mismas
      aserciones que antes (`git diff` de la prueba no cambia los números de las aserciones)
- [ ] Durante las pruebas del HUD hay exactamente un `HudAPie` en la escena (aserción nueva en la base de las pruebas)
- [ ] `git grep -n "\"RADIO\|\"REGRESA\|\"CERRADO" -- Assets/_Proyecto/UI/HudAPie/` no encuentra textos para el jugador escritos
      en el código; los mismos textos están en `ConfigHudAPie_Base.asset`
- [ ] docs/capturas/T-059-2.png muestra el aviso de cerrado por horario frente al Clandestino (captura revisada)

## Criterios de sensación (para un humano)
- Ninguno nuevo: el HUD se ve igual que antes.

## Pruebas automáticas
- PlayMode: GolpeBloqueoPruebas (5 corridas), HudAPiePruebas, HudObjetivoPruebas, HudInteraccionPruebas, HudCarroPruebas.
- EditMode: FormatoHudPruebas, TextoInteraccionPruebas.

## Fuera de alcance
- Cambiar cómo se ve el HUD o su tamaño de letra; traducir textos; cualquier cambio de reglas de Combate
