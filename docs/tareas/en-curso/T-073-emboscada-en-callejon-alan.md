# T-073 Emboscada en el callejón de Alan (H4, lado de Tony)
Dueño: Tony | Rama: tony/T-073-emboscada-en-callejon-alan | Modelo: Sonnet 5.5, medium
Depende de: T-072, T-060
**Espera A-F3.** Motivo: necesita en main el callejón de la emboscada con `Emboscada.prefab` colocado, al menos un
`IPuntoLlamadoMecha` que cubra la entrada del callejón, la casa con su `ITallerMecha` y `MechaEstacionado.prefab` colocado
(pedido D-tony-37), todo en Pruebas_Alan.unity o Ciudad.unity. El orquestador la deja en docs/tareas/bloqueadas/ hasta que esté en main.
Cómo saber si llegó: `git grep -l "IPuntoLlamadoMecha" main -- Assets/_Proyecto/Mundo/` y
`git grep -l "ITallerMecha" main -- Assets/_Proyecto/Mundo/` encuentran al menos un archivo cada uno, y la tarea de Alan del
callejón está en docs/tareas/hechas/.

## Objetivo
La emboscada de Tony funciona sin cambios en el barrio real de Alan: el mecha descansa en el taller de la casa, el jugador
camina al callejón, la pandilla aparece, el mecha llega volando al punto de Alan, se enciende y gana la pelea sin poder perder.
Es el lado de Tony del hito H4. Es solo integración: si algo falla del lado de Alan, se anota, no se arregla aquí.

## Contexto
- Hoja de ruta, H4. D-029 (las escenas de Alan son suyas: la prueba no las guarda ni las edita; lo que falte se instancia en
  tiempo de juego). Propuestas D-tony-35 y D-tony-37 en docs/decisiones-pendientes-tony.md.
- Lee las tareas de Alan ya hechas de A-F3 para saber la escena, el id del punto de aparición cerca del callejón y dónde
  quedó el taller.
- Los tests de Tony no referencian ensamblados de Alan: todo se encuentra por `Registro<IJugador>`, `Registro<IPuntoAparicion>`,
  `Registro<IPuntoLlamadoMecha>`, `Registro<ITallerMecha>` y `Registro<IMontable>`. La cámara libre de Alan también es
  `IJugador`: apágala como en T-060.
- Reutiliza el recorrido de T-072 (sácalo a una base común si hace falta) para no duplicar la entrada simulada.

## Qué hacer
1. `Tests/PlayMode/Pelea/CallejonAlanPruebas.cs`: carga la escena de Alan con `EditorSceneManager.LoadSceneInPlayMode`, instancia
   `JugadorAPie.prefab` en el punto de aparición más cercano al callejón (o el que diga A-F3), pone el reloj de noche si hace
   falta y, con entrada simulada, hace el recorrido de T-072 a) a f): al empezar, el mecha está a menos de 0.1 m del
   `Estacionamiento` del taller; la llegada es la del punto de llamado de Alan.
2. Comprueba que, al bajar en el callejón, la pose de salida queda libre (no dentro de un muro de Alan) y que el mecha regresa al
   taller de la casa.
3. Si hace falta un ajuste del lado de Tony (por ejemplo la búsqueda de salida al bajar en un callejón angosto), se hace en su área
   y se anota. Si falta algo del lado de Alan, se anota para docs/revision-tony.md y la tarea queda bloqueada con la evidencia.

## Carpetas permitidas
Assets/_Proyecto/Tests/PlayMode/Pelea/ y, solo para ajustes del lado de Tony, Assets/_Proyecto/Mecha/, Assets/_Proyecto/Pelea/ e
Assets/_Proyecto/Personaje/
No tocar: nada de Alan (Mundo/, Arena/, Vehiculos/, Pruebas_Alan.unity, Ciudad.unity), Core/.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa con CallejonAlanPruebas contada
- [ ] CallejonAlanPruebas pasa: emboscada `Terminada` una vez, la vida del mecha nunca llega a 0, el jugador termina a pie y el
      mecha en el `Estacionamiento` del taller de Alan
- [ ] Durante la prueba hay un solo `IJugador` registrado (el de Tony) y un solo `IMontable` de modo Mecha
- [ ] `git status` después de correr las pruebas no muestra cambios en escenas de Alan
- [ ] Captura del mecha bajando del cielo en el callejón de Alan: docs/capturas/T-073-1.png
- [ ] Consola sin errores durante la prueba

## Criterios de sensación (para un humano)
- Tony, en Ciudad.unity, sale del clandestino, camina al callejón, llama al mecha y gana la emboscada sin ayuda: la escala del
  mecha cabe en el callejón y la llegada del cielo se ve bien entre los edificios de Alan.

## Pruebas automáticas
- PlayMode (Tests/PlayMode/Pelea/): CallejonAlanPruebas.

## Fuera de alcance
- Llegar al callejón en carro (H3, Alan con T-061); el director del prólogo que encadena todo (T-F6)
- Cambiar cualquier cosa del mundo
