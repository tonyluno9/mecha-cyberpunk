# T-136 El prólogo en Ciudad.unity y Flashback.unity de Alan (H6, lado de Tony)
Dueño: Tony | Rama: tony/T-136-prologo-en-ciudad-alan | Modelo: Sonnet 5.5, medium
Depende de: T-135, T-073, T-061
**Espera A-F3 y A-F4.** Motivo: necesita en main la ciudad completa de Alan con la casa (taller, cama y espejo), el clandestino por dentro
con su arena, el callejón de la emboscada, el carro (A-F2) y Flashback.unity, con los prefabs de Tony colocados y los puntos con nombre
del pedido D-tony-72. El orquestador la deja en docs/tareas/bloqueadas/ hasta que esté en main.
Cómo saber si llegó: `git ls-tree -r main --name-only | grep Escenas/Flashback.unity` existe; `git grep -l "IPuntoHistoria" main --
Assets/_Proyecto/Mundo/` encuentra al menos un archivo; `git grep -l "DirectorPrologo" main -- Assets/_Proyecto/Escenas/Ciudad.unity`
encuentra el prefab colocado (por GUID del prefab); T-073 y T-061 ya no están bloqueadas; las tareas de A-F3 y A-F4 de Alan están en
docs/tareas/hechas/.

## Objetivo
El prólogo de Tony corre sin cambios de código en el mundo real de Alan: de la creación de personaje a Ciudad.unity (pelea en el clandestino,
emboscada en el callejón, regreso en el carro de Alan, dormir en la cama de la casa), a Flashback.unity y de vuelta a la ciudad para la
pelea casera y "continuará". Es el lado de Tony del hito H6. Es integración: si algo falta del lado de Alan, se anota, no se arregla aquí.

## Contexto
- Hoja de ruta: H6 ("una persona nueva termina la demo en unos 15 minutos sin ayuda"), A-F3 y A-F4. D-029 (las escenas de Alan son
  suyas: la prueba no las guarda ni las edita).
- Pedido a Alan (D-tony-72, docs/revision-tony.md): colocar `DirectorPrologo.prefab`, `CamaJugador.prefab`, `EspejoJugador.prefab`,
  `MostradorPeleas.prefab` (Progresion/Prologo/Prefabs/), `MechaEstacionado.prefab`, `Emboscada.prefab` y `JugadorAPie.prefab` en Ciudad.unity;
  `Flashback.prefab` en Flashback.unity; apariciones "Casa_Cama", "Casa_Puerta" y "Clandestino_Salida"; `IPuntoHistoria` "Prologo_Carro" (junto
  a donde queda su carro al salir del clandestino), "Prologo_Esquina_N" en las vueltas del regreso a casa y "Flashback_Plano_1" a
  "Flashback_Plano_6"; su `RelojMundo` como `IGuardable` (clave `reloj`); su carro como `IMontable`.
- Lee las tareas de Alan ya hechas de A-F2 a A-F4 para saber ids, teclas del carro y cualquier diferencia.
- Los tests de Tony no referencian ensamblados de Alan: todo se encuentra por los registros de Core.

## Qué hacer
1. `DatosEscenas_Demo` apunta a Escenas/Tony/CreacionPersonaje.unity, Escenas/Ciudad.unity y Escenas/Flashback.unity (con su creador);
   `DatosEscenas_Pruebas` sigue con las escenas de Tony.
2. `DatosPrologo_Base`: ids de la ruta de regreso según los que puso Alan. `ConfigAyudas_Base`: el texto del carro con las teclas del carro
   de Alan.
3. PlayMode `Tests/PlayMode/Progresion/PrologoCiudadPruebas.cs`: el recorrido de T-135 a) a g) con las escenas de Alan (`RutaForzada`
   temporal, cambios de escena reales), reutilizando la base de T-135; atajos solo en tramos de manejo de más de 40 m (anotados). Cada tramo
   imprime su tiempo.
4. Si hace falta un ajuste del lado de Tony (por ejemplo la salida del mecha en un clandestino angosto o la búsqueda de la cama), se hace en
   su área y se anota. Si falta algo de Alan, se anota para docs/revision-tony.md con la evidencia y la tarea queda bloqueada.

## Carpetas permitidas
Assets/_Proyecto/Progresion/ (Datos/ y Prologo/Datos/ con sus creadores), Assets/_Proyecto/UI/Datos/ConfigAyudas_Base.asset,
Assets/_Proyecto/Tests/PlayMode/Progresion/, docs/capturas/ y, solo para ajustes del lado de Tony, las áreas de Tony
No tocar: nada de Alan (Mundo/, Arena/, Vehiculos/, Ciudad.unity, Flashback.unity, Pruebas_Alan.unity), Core/, ProjectSettings/.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa con PrologoCiudadPruebas contada
- [ ] PrologoCiudadPruebas pasa con las mismas comprobaciones de T-135 (etapas en orden, premios, reparación exacta, vida > 0 en la noche,
      consola sin errores, un `IJugador` y un `AudioListener` por escena)
- [ ] `git status` después de correr las pruebas no muestra cambios en escenas de Alan
- [ ] Tiempos por tramo anotados; la suma con caminatas y manejo reales está entre 10 y 20 minutos estimados (o se anota qué tramo sobra)
- [ ] Capturas docs/capturas/T-136-1.png a T-136-4.png (arena del clandestino de Alan, callejón, casa con la cama, un plano de Flashback.unity)

## Criterios de sensación (para un humano, prueba humana de H6)
- Una persona nueva abre CreacionPersonaje.unity, da Play y termina la demo sin ayuda en unos 15 minutos. Anota dónde dudó, cuánto tardó y
  si la ciudad se parece a la referencia (esto último es de Alan).

## Pruebas automáticas
- PlayMode: PrologoCiudadPruebas.

## Fuera de alcance
- Build de Windows y Build Settings (T-137); cambiar cualquier cosa del mundo; contenido de T-F7
