# T-145 Apostar en el mostrador de peleas
Dueño: Tony | Rama: tony/T-145-pantalla-apuesta | Modelo: Sonnet 5.5, medium
Depende de: T-143, T-144

## Objetivo
Al pulsar E en el mostrador de peleas del clandestino se abre una pantalla de apuesta: el jugador ve contra quién pelea, el premio, el
momio y la pieza en juego, elige cuánto apostar con las flechas y pelea con Enter (o se va con Esc sin pagar nada). Al terminar, la pantalla
de la arena dice cuánto ganó o perdió en la apuesta. Es la primera parte jugable de las apuestas.

## Contexto
- Diseño: economia-misiones.md (apuestas; "pierdes la apuesta y el premio"), estetica-hud.md (pantallas de neón, mínimas).
- Decisiones: D-tony-66 (`MostradorPeleas`: textos y orden de permisos; no cambia), D-tony-64 (pantallas con teclado ← → ↑ ↓ Enter Esc),
  D-tony-67 (el espejo congela al jugador mientras se ve; la E que cierra no debe reabrir en el mismo cuadro: arreglado en T-130).
  Propuestas: D-tony-80 y D-tony-86.
- Ejemplos: `UI/Creacion/PantallaCreacion.cs` (navegación con teclado), `UI/Historia/PantallaEspejo.cs` y `Progresion/Prologo/EspejoJugador.cs`
  (abrir una pantalla desde un `IInteractuable` y congelar al jugador), `Pelea/Flujo/PantallaPelea.cs` (`MostrarPremio`),
  `Tests/PlayMode/Progresion/PeleaCaseraPruebas.cs` (mostrador con entrada simulada). Usa el helper de mapas de acciones de T-140.

## Qué hacer
1. `UI/Peleas/PantallaApuesta.cs` + prefab (con creador en UI/Editor/): pantalla a pantalla completa, semitransparente, con: título
   ("PELEA CASERA"), rival ("CONTRA EL CHATARRAS"), premio ("PREMIO $600"), momio ("PAGA 1 A 1"), fila de montos ("SIN APUESTA", "$100",
   "$250", "$500") con el elegido resaltado, la línea de la pieza ("+ SU BRAZO DE CHATARRA si apuestas $250 o más") y la ayuda
   ("← → apuesta · Enter pelear · Esc salir"). Los montos que el jugador no puede pagar se ven apagados y las flechas los saltan.
   API para quien la abre: mostrar con los datos y dos callbacks (pelear con monto, salir). Los textos van en un `ConfigPantallaApuesta`.
2. `MostradorPeleas`: si su flujo tiene datos de apuesta y hay `IApuestasPelea`, E anuncia la apuesta (`DirectorPelea.AnunciarApuesta`) y
   abre la pantalla en vez de pelear directo; congela al jugador mientras está abierta (como el espejo). Enter con monto > 0: `Apostar`; si
   falla, no pelea y la pantalla lo dice; luego `EntradaArena.Empezar(flujo)`. Enter con "SIN APUESTA": pelea sin apostar. Esc: cierra,
   cancela lo anunciado, devuelve el control y no cobra nada. Sin datos de apuesta, el mostrador se comporta como hoy.
3. `PantallaPelea.MostrarApuesta(int neto)`: debajo del premio, "APUESTA +$500" en el color del premio o "APUESTA −$250" en rojo;
   `DirectorPelea` la llama con su `ApuestaResuelta`. Sin apuesta no se muestra nada.
4. Pruebas_Economia (T-143): nada nuevo que colocar (el mostrador ya está); si el creador del mostrador cambia, vuelve a correr los creadores.
5. PlayMode `Tests/PlayMode/Progresion/ApuestaMostradorPruebas.cs` en Pruebas_Economia, todo con entrada simulada (W, ratón, E, flechas,
   Enter, Esc), con `RecibirDanio` permitido solo para rematar al rival después de daño real.

## Carpetas permitidas
Assets/_Proyecto/UI/ (Peleas/, Datos/, Editor/, Prefabs/), Assets/_Proyecto/Progresion/Prologo/ (MostradorPeleas y su creador),
Assets/_Proyecto/Pelea/Flujo/ (PantallaPelea, DirectorPelea) y Pelea/Editor/, Assets/_Proyecto/Escenas/Tony/Pruebas_Economia.unity (solo con
su creador), Assets/_Proyecto/Tests/PlayMode/Progresion/, Assets/_Proyecto/Tests/PlayMode/UI/, docs/capturas/
No tocar: Core/, Economia/ (salvo que falte algo de T-144: anótalo en la bitácora), `FlujoPelea.prefab`, otras escenas, todo lo de Alan.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa (salvo los 9 fallos conocidos de Alan en Arena)
- [ ] ApuestaMostradorPruebas a): con $500, E en el mostrador abre la pantalla con "CONTRA EL CHATARRAS" y "PREMIO $600"; → → elige $250;
      Enter: tras el fundido pelea; gana: al volver a pie el dinero es $1,350, hay 1 Brazo de chatarra y la pantalla de la arena mostró "APUESTA +$500"
- [ ] b) apuesta $250 y pierde con golpes reales del rival: dinero $250, inventario igual, la pantalla de la arena mostró "APUESTA −$250"
- [ ] c) E y luego Esc: no pelea, el dinero no cambia, y con W el jugador vuelve a moverse; la E que abrió no reabre la pantalla al cerrarla
- [ ] d) con $80, las flechas no salen de "SIN APUESTA" y Enter pelea sin apuesta (dinero tras perder: $80)
- [ ] e) consola sin errores; PeleaCaseraPruebas (T-129) sigue en verde
- [ ] Capturas docs/capturas/T-145-1.png (pantalla de apuesta con $250 elegido) y T-145-2.png (pantalla de la arena con premio y apuesta)

## Criterios de sensación (para un humano)
- Pruebas_Economia, Play: E en el mostrador. ¿Se entiende a la primera cuánto apuestas, cuánto ganarías y qué pieza está en juego?
- ¿Elegir y pelear se siente como apostar en un sótano (rápido, tenso) y no como un menú de tienda?
- Al perder, ¿"APUESTA −$250" duele lo justo?

## Pruebas automáticas
- PlayMode: ApuestaMostradorPruebas.

## Fuera de alcance
- Apuestas del casino (Alan). Ligas, varios rivales y momios por rival (bloque de ligas). Sonido.
