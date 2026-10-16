# T-068 Rival que no te deja perder y rival del tutorial
Dueño: Tony | Rama: tony/T-068-piedad-rival-tutorial | Modelo: Sonnet 5.5, medium
Depende de: nada nuevo (T-021 y T-022 ya están en tony/fase-3)

## Objetivo
Un rival puede llevar "piedad": sus golpes nunca dejan al jugador en KO y, cuando la vida del jugador baja mucho, se debilita
(pega menos, ataca menos seguido, se mueve más lento y recibe más daño) de forma visible, para que siempre ganes. Con eso nace el
rival del tutorial (la pelea de campeonato del prólogo) y la base de la pandilla de la emboscada. El rival normal sigue igual:
la primera pelea casera (T-F6) sí se puede perder.

## Contexto
- Diseño: demo.md ("En el tutorial, si la vida baja mucho, el rival se debilita para que siempre ganes"; "No puedo perder ni la
  pelea del prólogo ni la emboscada"; "La primera pelea casera sí se puede perder"). Hoja de ruta T-F4: "con vida baja los
  rivales no te matan".
- Propuesta: D-tony-32 en docs/decisiones-pendientes-tony.md (la piedad es dato del rival; valores).
- Lo que ya hay: `RivalBasico` (pega con `DatosMecha.DanioGolpe` por `BarridoGolpe`), `CerebroRival` (esperas), `VidaMecha`
  (Combate, aplica el bloqueo después del daño pedido), `PeleadorMecha.PeleaPreparada` (revancha), `DatosRival` (Core, no se
  toca). `IA` ya referencia `Combate` (D-tony-9).
- Nombre del rival: hoy `PeleadorMecha.Nombre` devuelve el nombre de su `DatosMecha`; el presentador (T-070) necesita el alias
  del rival. Arréglalo aquí: el rival le da a su `PeleadorMecha` el `Alias` de su `DatosRival`.

## Qué hacer
1. Lógica pura `IA/Rival/ReglaPiedad.cs`: con la vida actual y máxima del objetivo y `ConfigPiedadRival`, da el daño a pedir
   (nunca deja al objetivo por debajo de `fraccionPiso` de su vida máxima, 15 %; en el piso pide 0) y si toca debilitarse (vida
   del objetivo en `umbralDebilitar` o menos, 35 %; una vez débil sigue débil hasta reiniciar).
2. `ConfigPiedadRival` (IA/Datos/): fracción de piso 0.15, umbral 0.35, multiplicadores al debilitarse: daño 0.5, espera entre
   golpes 1.6, velocidad 0.7 y daño recibido 1.5; color o parpadeo del aviso de debilidad.
3. `IA/Rival/PiedadRival.cs` en el rival: lee la vida de su objetivo (`IPeleador.Vida`), le pasa a `RivalBasico` el daño a pedir y
   los multiplicadores (agrega en RivalBasico y CerebroRival lo mínimo para aceptarlos, sin cambiar su comportamiento cuando no
   hay piedad), y al debilitarse lo muestra (greybox: el cuerpo se oscurece y parpadea, o chispas simples). `PeleaPreparada` lo
   reinicia.
4. `Combate/Vida/VidaMecha.cs`: `MultiplicadorDanioRecibido` (por defecto 1) que se aplica antes del bloqueo.
5. `Combate/Peleador/PeleadorMecha.cs`: nombre para mostrar opcional; `RivalBasico` lo fija con el `Alias` de su `DatosRival` en
   `Awake`. El jugador sigue devolviendo el nombre de su `DatosMecha`.
6. Rival del tutorial: `IA/Datos/DatosRival_Tutorial.asset` (alias "EL CAMPEÓN", provisional), `DatosMecha_Tutorial.asset` (vida
   120, daño 12: pega más que el de prueba para que se sienta campeón) y `IA/Prefabs/RivalTutorial.prefab` con `PiedadRival`, hechos
   con el creador de IA.
7. Pruebas:
   - EditMode `Tests/EditMode/IA/ReglaPiedadPruebas.cs`: el daño pedido nunca baja la vida del objetivo del piso; en el piso pide
     0; vida null o máxima 0 no explota; se debilita en el umbral y se queda débil; reiniciar lo quita.
   - PlayMode `Tests/PlayMode/IA/PiedadRivalPruebas.cs` (Pruebas_Tony con `FlujoPelea` de pruebas y `RivalTutorial` como rival):
     a) el jugador no toca nada 120 s de juego (con `Time.timeScale` 4 si hace falta): su vida nunca llega a 0 y nunca baja del
     15 %; b) después de debilitarse, cada golpe del rival quita como mucho la mitad de su daño normal; c) desde vida baja, un
     jugador simulado que mira al rival y hace clic seguido lo deja en KO y gana; d) control: con `RivalBasico` (sin piedad) el
     jugador quieto sí llega a KO; e) la revancha (R) quita la debilidad.

## Carpetas permitidas
Assets/_Proyecto/IA/, Assets/_Proyecto/Combate/Vida/VidaMecha.cs, Assets/_Proyecto/Combate/Peleador/PeleadorMecha.cs,
Assets/_Proyecto/Tests/EditMode/IA/, Assets/_Proyecto/Tests/PlayMode/IA/
No tocar: Core/ (DatosRival no cambia), Pelea/, Mecha/, Pruebas_Tony.unity (las pruebas arman lo suyo en tiempo de juego),
las carpetas y escenas de Alan.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa, incluidas RivalBasicoPruebas, RivalEnEscenaPruebas y FlujoPeleaPruebas sin cambios
- [ ] ReglaPiedadPruebas y PiedadRivalPruebas pasan (casos a a e)
- [ ] `IPeleador.Nombre` de `RivalTutorial` es "EL CAMPEÓN" y el del jugador sigue siendo el de `DatosMecha_Jugador`
- [ ] Captura del rival del tutorial debilitado, desde la cabina: docs/capturas/T-068-1.png (se distingue del rival normal)
- [ ] Consola sin errores en las pruebas

## Criterios de sensación (para un humano)
- En Pruebas_Tony contra el rival del tutorial: ¿se siente como una pelea de verdad hasta que bajas mucho, y luego la ayuda se
  nota poco (no parece que el rival se rindió)? ¿Se ve que el rival está débil?

## Pruebas automáticas
- EditMode: ReglaPiedadPruebas. PlayMode: PiedadRivalPruebas.

## Fuera de alcance
- Presentador y prefab de flujo del tutorial (T-070); pandilla y turnos de ataque (T-069)
- Indicaciones de tutorial en la cabina (T-F6); adaptación del rival a tu forma de pelear (T-F7)
