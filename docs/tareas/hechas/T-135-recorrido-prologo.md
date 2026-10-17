# T-135 Recorrido del prólogo completo (cierre de T-F6 de Tony)
Dueño: Tony | Rama: tony/T-135-recorrido-prologo | Modelo: Sonnet 5.5, medium
Depende de: T-121 a T-134

## Objetivo
La prueba de cierre de T-F6 del lado de Tony (H6 sin el mundo de Alan), jugada con entrada simulada y cambios de escena reales: crear el
personaje, ganar la pelea de campeonato, sobrevivir a la emboscada, volver a casa en carro, dormir, ver el flashback, despertar con el mecha
fregado, pelear la casera y llegar a "CONTINUARÁ". No agrega sistemas: une T-121 a T-134 y lo mide.

## Contexto
- Hoja de ruta, T-F6: "PlayMode que recorre el prólogo completo con entrada simulada y una prueba humana de 15 minutos (H6)". H6 completo
  necesita Ciudad.unity, la casa y Flashback.unity de Alan (A-F3 y A-F4): esa integración es T-136 (bloqueada).
- Escenas de Tony usadas: Escenas/Tony/CreacionPersonaje.unity (T-126), Pruebas_Prologo.unity (T-127 a T-133), Pruebas_Flashback.unity
  (T-131). `DatosEscenas_Demo` apunta a ellas hasta T-136.
- Ejemplos de recorrido largo con entrada simulada: Tests/PlayMode/Pelea/RecorridoEmboscadaPruebas.cs (T-072),
  Tests/PlayMode/Mecha/RecorridoTallerPruebas.cs (T-112) y las pruebas de T-132 y T-133. Tope de frame con `MedidorFrames` (T-122).
- Decisiones: D-tony-56 a D-tony-74 (docs/decisiones-pendientes-tony.md).

## Qué hacer
1. PlayMode `Tests/PlayMode/Progresion/RecorridoPrologoPruebas.cs`, con `ServicioGuardado.RutaForzada` en una carpeta temporal y SIN
   `CambioEscena.Interceptar` (los cambios de escena son reales), empezando por cargar CreacionPersonaje.unity:
   a) creación: escribe nombre y alias ("El Rayo"), cambia cara, ropa e implante, EMPEZAR: carga Pruebas_Prologo en etapa `PeleaTutorial`;
   b) pelea de campeonato: el jugador está en su mecha en la arena, la presentación grita "EL RAYO", pelea con entrada simulada y gana
      (se permite rematar al rival con `RecibirDanio` tras daño real al jugador);
   c) salida y emboscada: a pie en la salida, camina SIN atajos siguiendo la flecha del objetivo hasta que se activa la emboscada; M, E,
      encendido (F, Espacio mantenido, Q) y gana (mismo permiso de rematar);
   d) regreso: baja del mecha, camina al carro, sube con E, maneja hacia la casa siguiendo el objetivo (se permite acortar con
      `IJugador.Colocar` solo si el tramo pasa de 40 m, anotado), baja y entra a la casa;
   e) dormir: E en la cama: carga Pruebas_Flashback; mira el primer plano completo y salta el resto manteniendo Espacio (la reproducción
      completa la prueba T-131): carga Pruebas_Prologo en `PeleaCasera`;
   f) presente: despierta en la cama con dinero 500 y el mecha al 45 %; repara en el banco (el dinero baja el costo exacto); va al
      clandestino (atajo permitido si pasa de 40 m; si el mostrador está cerrado, `AvanzarHasta(20.5)` anotado), E en el mostrador y gana la
      casera (+$600);
   g) "continuará": muestra "EL RAYO", el dinero y "Ganaste"; Esc tras 4 s: carga CreacionPersonaje.unity y aparece el título con CONTINUAR;
      CONTINUAR vuelve a Pruebas_Prologo con "continuará" a la vista; Enter → etapa `Libre` y el jugador camina con W.
   En todo el recorrido: la vida del mecha nunca llega a 0 antes de la casera; un solo `IJugador` y un solo `AudioListener` por escena;
   consola sin errores ni excepciones; `MedidorFrames` cumple; dura menos de 240 s de juego. La prueba imprime el tiempo de cada tramo.
2. Capturas: docs/capturas/T-135-1.png (creación lista para EMPEZAR), T-135-2.png (presentación en la arena), T-135-3.png (emboscada desde la
   cabina), T-135-4.png (un plano del flashback con subtítulo), T-135-5.png ("continuará").
3. Si algo falla en código de juego, no se arregla aquí: se anota con la evidencia y se vuelve corrección de la tarea que lo hizo.

## Carpetas permitidas
Assets/_Proyecto/Tests/PlayMode/Progresion/, docs/capturas/
No tocar: el código de juego de ninguna área, escenas y prefabs (si un creador necesita un ajuste, es corrección de su tarea), Core/,
todo lo de Alan.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa (todas las pruebas de Tony; los fallos de Alan conocidos, si siguen, se listan aparte)
- [ ] RecorridoPrologoPruebas (a-g) pasa en 3 corridas seguidas
- [ ] Medido en la prueba: etapas en el orden `PeleaTutorial`, `SalidaClandestino`, `Emboscada`, `RegresoACasa`, `Dormir`, `Flashback`,
      `Despertar`, `PeleaCasera`, `Continuara`, `Libre`; +$1,500 y +$600; reparación con el costo exacto; vida del mecha > 0 en la noche
- [ ] La partida real (`Application.persistentDataPath/partida.json`) no se tocó
- [ ] Capturas T-135-1 a T-135-5 revisadas
- [ ] Tiempo de juego por tramo anotado en el resumen (sirve para estimar los 15 minutos de H6)

## Criterios de sensación (para un humano, prueba humana de H6 del lado de Tony)
- Una persona que no conozca el juego abre CreacionPersonaje.unity, da Play y juega hasta "continuará" sin ayuda, con las escenas de Tony.
  ¿Lo termina? ¿En cuánto tiempo (meta: unos 15 minutos con la ciudad real; con el barrio de pruebas será menos)? ¿Dónde se atoró o dudó?
- ¿No se puede perder ni la pelea del campeonato ni la emboscada, y la casera sí se siente perdible?
- ¿El salto del futuro (campeón con $1,500) al presente (mecha fregado y $500) se entiende gracias al flashback?

## Pruebas automáticas
- PlayMode: RecorridoPrologoPruebas.

## Fuera de alcance
- Ciudad.unity, la casa y Flashback.unity de Alan, el carro de Alan y Build Settings (T-136 y T-137, bloqueadas)
