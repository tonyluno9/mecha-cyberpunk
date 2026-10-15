# T-104 Mecha dañado: más lento, peor manejo y alarma roja
Dueño: Tony | Rama: tony/T-104-mecha-daniado-lento-alarma | Modelo: Sonnet 5.5, medium
Depende de: T-103

## Objetivo
Con vida baja el mecha se siente fregado: camina más lento, arranca y gira peor, y una alarma roja parpadea en la cabina. Así el
daño persistente pesa en la siguiente pelea y el jugador entiende sin leer números que debe reparar.

## Contexto
- Diseño: combate.md ("Con vida baja, la cabina parpadea en rojo, el cristal se cuartea y el mecha va más lento y se maneja
  peor"; criterio: "Con vida baja veo el LED rojo y el cristal cuarteado, y el mecha va más lento"), estetica-hud.md (en el mecha
  todo es diegético; el audio del mecha también: alarmas).
- Decisiones: D-005 (valores en un Config del área), D-011 (diegético en la cabina). Propuesta: D-tony-43 (umbral 30 %,
  velocidad ×0.7, aceleración y frenado ×0.6, giro ×0.75, alarma a 2 Hz).
- Código: Mecha/Movimiento/ControladorMecha.cs (usa `datos.VelocidadMaxima`, `Aceleracion`, `Desaceleracion` y
  `VelocidadGiroMaxima`), Mecha/Movimiento/MovimientoPesado.cs y GiroPesado.cs (puras), Combate/Vida/VidaMecha.cs (con
  `FijarVida` de T-103), Mecha/Datos/ConfigMechaDaniado.cs (T-103), Mecha/Editor/CreadorCabina.cs (estructura de la cabina:
  `Tablero`, `MarcoPantallaVida`), UI/Cabina/PantallaVidaCabina.cs (ya se pinta con su color bajo; no se toca).
- El cristal cuarteado es T-105, no esta tarea.

## Qué hacer
1. `ConfigMechaDaniado`: agrega `umbralVidaBaja` 0.3 (fracción), `factorVelocidad` 0.7, `factorAceleracion` 0.6 (también
   frenado), `factorGiro` 0.75, `frecuenciaAlarma` 2 (Hz: mitad del periodo prendida, mitad apagada), color e intensidad de la luz
   de alarma y su alcance (m). Actualiza el asset `_Base` con su creador.
2. `Mecha/Danio/ReglaMechaDaniado.cs` (estática, pura): `bool VidaBaja(float actual, float maxima, float umbral)` (true si
   0 < actual/maxima <= umbral); `float Factor(bool vidaBaja, float factor)` (1 si no hay vida baja); `bool AlarmaPrendida(float
   segundos, float frecuencia)`.
3. `ControladorMecha`: busca la `VidaMecha` de su objeto y, con vida baja, multiplica velocidad máxima, aceleración, frenado y
   giro máximo por sus factores. Sin `ConfigMechaDaniado` asignado, se mueve como antes (no da error). Los rivales no usan
   `ControladorMecha`: no cambian.
4. `Mecha/Cabina/AlarmaCabina.cs`: una luz puntual roja y un LED (cubito con material emisivo rojo) en el tablero. Con vida baja
   parpadea a `frecuenciaAlarma`; en KO queda fija prendida; con vida arriba del umbral, apagada. Se actualiza con `Cambio` de la
   vida y el tiempo de juego. Expón `bool Prendida` para las pruebas.
5. Creador `Mecha/Editor/CreadorDanioCabina.cs` (`MenuItem`, idempotente): agrega la luz, el LED, su material
   (Mecha/Materiales/) y `AlarmaCabina` a la cabina de `MechaJugador.prefab`, y asigna `ConfigMechaDaniado_Base` al
   `ControladorMecha`. T-105 agregará aquí las grietas.
6. Pruebas:
   - EditMode `Tests/EditMode/Mecha/ReglaMechaDaniadoPruebas.cs`: 31/100 no es vida baja, 30/100 sí, 0/100 no (KO es otro caso),
     factores con y sin vida baja, `AlarmaPrendida` a 2 Hz en 0.1 s (sí), 0.3 s (no) y 0.6 s (sí).
   - PlayMode `Tests/PlayMode/Mecha/MechaDaniadoPruebas.cs` (Pruebas_Tony, entrada simulada con InputTestFixture):
     a) con vida máxima, W mantenida 3 s: velocidad hacia adelante medida = `VelocidadMaxima` de DatosMecha_Jugador (±0.1 m/s);
     b) con `FijarVida(25)`, igual: velocidad = 0.7 × esa (±0.1 m/s) y tarda más en llegar a ella (aceleración menor, medida);
     c) con vida 25, el giro máximo medido con ratón en X mantenido es 0.75 × `VelocidadGiroMaxima` (±5 %);
     d) con vida 25 la alarma cambia de prendida a apagada al menos 3 veces en 2 s; con vida 100 está apagada todo el tiempo; al
        `Reparar` se apaga; en KO queda prendida fija.
   - La prueba de ControladorMechaPruebas sigue pasando sin cambios (vida completa).
7. Captura docs/capturas/T-104-1.png: cabina con vida 25 y la alarma prendida (LED y luz roja visibles, sin tapar la vista).

## Carpetas permitidas
Assets/_Proyecto/Mecha/ (Movimiento/, Danio/ nueva, Cabina/, Datos/, Editor/, Materiales/, Prefabs/),
Assets/_Proyecto/Tests/EditMode/Mecha/, Assets/_Proyecto/Tests/PlayMode/Mecha/, docs/capturas/
No tocar: Core/, Combate/ (la vida ya tiene lo necesario desde T-103), IA/, UI/, Pruebas_Tony.unity, las carpetas y escenas de Alan.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa
- [ ] ReglaMechaDaniadoPruebas y MechaDaniadoPruebas (a-d) pasan; ControladorMechaPruebas sigue pasando
- [ ] Velocidad con vida 25 = 0.7 × la normal (±0.1 m/s) y giro = 0.75 × el normal (±5 %), medidos en PlayMode
- [ ] La alarma parpadea con vida baja, está apagada con vida alta y fija en KO (prueba d)
- [ ] Captura docs/capturas/T-104-1.png revisada: el rojo se ve y el centro de la vista queda libre
- [ ] Consola sin errores en las pruebas

## Criterios de sensación (para un humano)
- En Pruebas_Tony, con H baja la vida a 25 o menos y camina y gira: ¿se siente pesado y fregado pero todavía controlable, o
  frustra?
- ¿La alarma roja a 2 Hz se nota sin cansar la vista en una pelea de un minuto?

## Pruebas automáticas
- EditMode: ReglaMechaDaniadoPruebas. PlayMode: MechaDaniadoPruebas.

## Fuera de alcance
- Cristal cuarteado (T-105), sonido de alarma (cuando haya audio del mecha), daño por piezas (T-F7)
