# T-161 Pelea de jefe en la arena, subir de liga y campeón del clandestino
Dueño: Tony | Rama: tony/T-161-subir-liga | Modelo: Sonnet 5.5, medium
Depende de: T-155, T-159

## Objetivo
Una pelea de jefe se anuncia como tal en la pantalla de la arena y, al ganarla, se celebra. Al volver a pie después de vencer por primera vez
al campeón de una liga, el HUD lo dice en grande y avisa que se abre la siguiente; al vencer al campeón de la última, "¡CAMPEÓN DEL
CLANDESTINO!". Así subir de liga se siente como un momento y no como un número que cambió.

## Contexto
- Diseño: personaje-progresion.md ("vencerlo te sube de liga"; "Al vencer al campeón de una liga se abre la siguiente"), estetica-hud.md (en
  el mecha todo es diegético: la pantalla de la arena es del mundo; a pie hay un HUD mínimo).
- Contratos: `IProgresoLigas.CampeonVencido` y `NombreLiga` (T-152; no se avisa al cargar ni en revancha). `DirectorPelea.Info` y `CartelActual` (T-155).
- Decisiones: D-tony-55 (avisos del HUD a pie), D-tony-65. Propuestas nuevas: D-tony-96 (pelea de jefe) y D-tony-100 (subir de liga).
- Código: `Pelea/Flujo/PantallaPelea.cs` (`MostrarLinea`, `MostrarPremio`, `MostrarApuesta`, `MostrarKO`), `Pelea/Datos/ConfigFlujoPelea.cs`,
  `Pelea/Editor/CreadorFlujoPeleaCasera.cs` (`AgregarLineaApuesta`: agrega un `Text` a la pantalla de forma idempotente), `UI/HudAPie/HudAPie.cs`
  (aviso de dinero con temporizador), `UI/Datos/ConfigHudAPie.cs`, `UI/HudAPie/FormatoHud.cs`.

## Qué hacer
1. Pantalla de la arena (Pelea): campo opcional `textoJefe` en `PantallaPelea` (null = no hay letrero; así `FlujoPelea.prefab` de Alan no cambia).
   Si `Info.EsJefe`, muestra "PELEA DE JEFE" (en `ConfigFlujoPelea`, color naranja) desde la presentación hasta "¡PELEA!", y en el resultado,
   si ganó el jugador, "¡VENCISTE AL CAMPEÓN!". En peleas sin jefe no aparece nada. Los creadores de `FlujoPeleaCasera.prefab` y
   `FlujoPeleaTutorial.prefab` agregan el `Text` (idempotente, como `AgregarLineaApuesta`).
2. HUD a pie (UI): `HudAPie` escucha `CampeonVencido` de `Registro<IProgresoLigas>` (también si el servicio se registra después). Muestra un
   aviso grande al centro arriba durante `segundosAvisoLiga` (5 s): "¡CAMPEÓN DE LA LIGA {0}!" y debajo "SE ABRE LA LIGA {1}"; si era la
   última liga, "¡CAMPEÓN DEL CLANDESTINO!" sin segunda línea. Textos, color y segundos en `ConfigHudAPie`. No tapa el aviso de dinero ni la
   línea de renta (posición propia). Si el jugador está en el mecha cuando llega el aviso, espera a que vuelva a pie (o se muestra al bajar).
3. PlayMode `Tests/PlayMode/Pelea/PeleaJefePruebas.cs` (cartel de jefe con un rival de prueba; daño real y `RecibirDanio` para rematar) y
   `Tests/PlayMode/UI/AvisoLigaPruebas.cs` (con un doble de `IProgresoLigas` o `ServicioLigas` real), EditMode para el formato del aviso si sale puro.

## Carpetas permitidas
Assets/_Proyecto/Pelea/ (Flujo/, Datos/, Editor/, Prefabs/ solo con creadores), Assets/_Proyecto/UI/ (HudAPie/, Datos/, Editor/, Prefabs/ solo con
creadores), Assets/_Proyecto/Tests/EditMode/UI/, Assets/_Proyecto/Tests/PlayMode/Pelea/, Assets/_Proyecto/Tests/PlayMode/UI/, docs/capturas/
No tocar: Core/, `FlujoPelea.prefab`, Progresion/, IA/, escenas, todo lo de Alan.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa (salvo los 9 fallos conocidos de Alan en Arena)
- [ ] PeleaJefePruebas: con un cartel de jefe, "PELEA DE JEFE" está visible en la presentación y el conteo y se oculta con "¡PELEA!"; al ganar se ve
      "¡VENCISTE AL CAMPEÓN!"; al perder no; en una pelea con cartel normal y en la casera no aparece ningún letrero
- [ ] AvisoLigaPruebas: `CampeonVencido(1)` muestra "¡CAMPEÓN DE LA LIGA SÓTANO!" y "SE ABRE LA LIGA CALLE" y a los 5 s se va; `CampeonVencido(4)`
      muestra "¡CAMPEÓN DEL CLANDESTINO!" sin segunda línea; cargar una partida con 2 campeones no muestra ningún aviso; con el jugador en el mecha el
      aviso aparece al bajar
- [ ] El tutorial del prólogo muestra "PELEA DE JEFE" (liga 4, jefe; T-155) y RecorridoPrologoPruebas sigue en verde
- [ ] Capturas docs/capturas/T-161-1.png (arena con "PELEA DE JEFE" desde la cabina) y T-161-2.png (HUD a pie con el aviso de liga)

## Criterios de sensación (para un humano)
- Pruebas_Economia: L hasta el jefe de la liga 1 y pelea contra DON FIERRO (o usa la cartelera). ¿El letrero y el presentador hacen que se sienta
  una pelea grande? Al ganar y bajar, ¿el aviso de liga se siente como un logro? ¿5 s es mucho o poco?
- ¿Mostrar "PELEA DE JEFE" en la noche del prólogo suma o le quita sorpresa?

## Pruebas automáticas
- PlayMode: PeleaJefePruebas, AvisoLigaPruebas.

## Fuera de alcance
- Luces de la jaula en la pelea de jefe (Alan, con `EventosPelea.PeleaPresentada`, opcional). Cinemática de campeón. Música especial.
