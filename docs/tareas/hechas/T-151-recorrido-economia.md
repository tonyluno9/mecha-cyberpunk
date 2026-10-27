# T-151 Recorrido de la economía completa (cierre del primer bloque de T-F7)
Dueño: Tony | Rama: tony/T-151-recorrido-economia | Modelo: Sonnet 5.5, medium
Depende de: T-140 a T-150

## Objetivo
Una prueba de PlayMode recorre, con entrada simulada y en Pruebas_Economia, el ciclo completo de la economía: comprar un kit, apostar,
pelear y usar el kit, ganar dinero y una pieza, venderla, reparar, perder una apuesta, reparar a medias y guardar y cargar. Si pasa, el bloque
"economía completa" está cerrado del lado de Tony y queda listo para que el casino y el mercado negro de Alan (A-F5) lo usen.

## Contexto
- Diseño: economia-misiones.md (criterios "Puedo vender una pieza vieja en el mercado negro"; apuestas; quedar en negativo), mecha-taller.md
  (kits), personaje-progresion.md (perder no es game over).
- Decisiones: D-tony-80 a D-tony-87. Tareas: T-142 a T-150.
- Ejemplos: `Tests/PlayMode/Mecha/RecorridoTallerPruebas.cs` (T-112) y `Tests/PlayMode/Progresion/RecorridoPrologoPruebas.cs` (T-135):
  estructura, `MedidorFrames` (D-tony-60), ruta de guardado forzada (`ServicioGuardado.RutaForzada`), atajos permitidos y registro de errores.
  Usa el helper de mapas de acciones de T-140.

## Qué hacer
1. `Tests/PlayMode/Progresion/RecorridoEconomiaPruebas.cs` en Pruebas_Economia, todo con entrada simulada (caminar, E, flechas, Enter, Esc, G,
   ratón para pelear). Atajos permitidos (como en T-135, y anotados en la bitácora): `Colocar` cerca de un mostrador o del banco, `RecibirDanio`
   sobre el rival solo después de daño real al jugador, dar dinero o vida inicial por código antes de empezar. Pasos, con los números
   calculados desde los assets (no escritos a mano) y comprobados en cada paso:
   a) inicio: $500 (dinero inicial), mecha en su taller con vida completa, inventario vacío;
   b) mercado: compra 1 kit (−precio del kit);
   c) mostrador de peleas: apuesta $250 y pelea la casera; recibe daño real, usa G una vez (kits 0, la vida sube lo de la regla) y gana:
      +premio, +apuesta × (1 + momio) y +1 Brazo de chatarra; vuelve a pie;
   d) mercado: vende el brazo (+valor de venta, motivo Venta);
   e) banco: "REPARAR TODO" cobra exactamente `ReglaReparacion` del daño medido y deja la vida completa;
   f) mostrador: apuesta $100 y pierde con golpes reales (el mecha queda en el piso de emergencia o en KO): −$100, sin premio ni pieza;
   g) banco: "REPARAR LA MITAD" cobra lo de la regla y repara la mitad del daño;
   h) F5 guarda; cambia dinero e inventario por código; F9 carga: dinero, inventario y vida del mecha vuelven a lo guardado; no hay apuesta abierta;
   i) consola sin errores ni excepciones en todo el recorrido; `MedidorFrames` sin tirones fuera de su tolerancia.
2. Correr la suite completa: `bash herramientas/verificar.sh todo` y `bash herramientas/verificar.sh playmode-por-area` (T-141); anotar en
   docs/revision-tony.md el resultado por área.
3. docs/revision-tony.md: sección "Economía completa (T-F7, bloque 1) cerrada: qué probar jugando" con la lista para un humano (los criterios de
   sensación de T-143 a T-150 juntos y en orden de juego), la tabla de precios de T-150 y los pedidos a Alan (D-tony-81: colocar
   `MostradorMercadoNegro.prefab` dentro de su mercado negro, con su `ILugar` `MercadoNegro`; el casino puede usar `IApuestasPelea`).

## Carpetas permitidas
Assets/_Proyecto/Tests/PlayMode/Progresion/, docs/capturas/, docs/revision-tony.md (sección de esta tarea)
No tocar: código del juego (si el recorrido encuentra un error, se anota en la bitácora y se pide una tarea de corrección; no se arregla aquí),
Core/, escenas, todo lo de Alan.

## Criterios de aceptación (los comprueba el verificador)
- [ ] RecorridoEconomiaPruebas pasa 3 corridas seguidas; la bitácora anota el tiempo de juego de cada paso (a a i)
- [ ] El dinero final del recorrido coincide con el calculado desde los assets (la prueba lo compara y lo imprime)
- [ ] `bash herramientas/verificar.sh todo` pasa en una sola llamada (salvo los 9 fallos conocidos de Alan en Arena) y `playmode-por-area` no
      marca ningún área "LIMITE"
- [ ] Capturas docs/capturas/T-151-1.png (pantalla de apuesta antes de pelear), T-151-2.png (cabina usando el kit) y T-151-3.png (venta del brazo)
- [ ] docs/revision-tony.md tiene la sección de cierre con la lista para un humano y los pedidos a Alan

## Criterios de sensación (para un humano)
- Pruebas_Economia, Play, sin ayuda, unos 10 minutos: compra, apuesta, pelea, vende, repara y paga la renta (J). ¿El dinero se siente escaso y
  cada decisión pesa? ¿Apostar es tentador? ¿Hay un momento en que te sientes atorado sin salida? (no debería: siempre se puede reparar fiado y
  volver a pelear)

## Pruebas automáticas
- PlayMode: RecorridoEconomiaPruebas.

## Fuera de alcance
- Integración en Ciudad.unity y en el mercado negro de Alan (se escribe cuando A-F5 esté en main, como T-073 y T-136). Ligas (siguiente bloque).
