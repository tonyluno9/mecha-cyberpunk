# T-180 Recorrido de las habilidades (cierre del bloque 4 de T-F7)
Dueño: Tony | Rama: tony/T-180-recorrido-habilidades | Modelo: Sonnet 5.5, medium
Depende de: T-174 a T-179

## Objetivo
Una prueba de PlayMode recorre, con entrada simulada y en Pruebas_Economia, el camino de las habilidades: comprar munición en el mercado
negro, comprar una pieza con habilidad en el taller, verla en la cabina, usarla en una pelea de liga respetando su enfriamiento, quedarse sin
munición y terminar a puños, cubrirse con el escudo, pelear contra un jefe que usa la suya y guardar y cargar la munición. Si pasa, el bloque
"habilidades con cooldown y munición" está cerrado del lado de Tony.

## Contexto
- Diseño: combate.md (criterio "Un misil no se puede volver a usar hasta que pase su cooldown, y cuando se acaba la munición no dispara";
  "Te quedas sin munición en plena pelea: solo te quedan los brazos"), mecha-taller.md (tech con cooldown más corto).
- Decisiones: D-tony-133 a D-tony-142. Tareas: T-174 a T-179.
- Ejemplos: `Tests/PlayMode/Progresion/RecorridoPiezasPruebas.cs` (T-173) y `RecorridoLigasPruebas.cs` (T-162): estructura, `MedidorFrames`,
  `ServicioGuardado.RutaForzada`, atajos permitidos, registro de errores, helper de mapas de acciones.

## Qué hacer
1. `Tests/PlayMode/Progresion/RecorridoHabilidadesPruebas.cs` en Pruebas_Economia, de noche y en `Libre`, con entrada simulada (caminar, E,
   flechas, Enter, Esc, ratón para pelear, F/Espacio/Q para encender, 1 y 2 para las habilidades). Atajos permitidos, anotados en la bitácora:
   `Colocar` cerca del banco, del mostrador o del mercado; dinero inicial por código; `RecibirDanio` al rival solo después de daño real al
   jugador; `ServicioLigas.Poner` solo donde se indica. Números calculados desde los assets. Pasos, uno por uno:
   a) inicio: liga 1, piezas de serie, sin munición, dinero fijado por código;
   b) mercado negro: compra 3 MISILES y 2 CELDAS (cobra cada una); la pestaña VENDER no ofrece munición;
   c) taller: compra BRAZO IZQ. MILITAR I y TORSO TECH I en "PIEZAS DEL MECHA"; la diferencia mostró "HABILIDAD 1 · MISIL I · MISILES";
   d) sube al mecha y enciende: la pantalla de habilidades dice "1 · MISIL I · ×3" y "2 · ESCUDO I · ×2", en gris (fuera de pelea); 1 no gasta;
   e) cartelera: TORNILLO. En pelea, 1 dispara y el rival pierde el daño del MISIL I; 1 otra vez antes del enfriamiento no gasta; tras el
      enfriamiento (medido, con la reducción de la pieza tech) dispara; tercer misil; con 0 misiles la fila dice "SIN MUNICIÓN" y el rival se
      termina a puños; gana;
   f) revancha (o LA RATA): 2 activa el ESCUDO; un golpe real del rival durante el escudo quita la mitad de lo normal (se mide con el impacto);
      después del escudo, completo;
   g) `Poner(liga 1, siguiente 4)` y pelea contra DON FIERRO: se ve su aviso y su EMBESTIDA llega (el jugador pierde su daño o lo bloquea);
      gana el jugador (atajos de T-162);
   h) F5 guarda con 1 celda; cambiar la munición y el dinero por código; F9 carga: misma munición, mismas piezas y mismo dinero;
   i) consola sin errores ni excepciones; `MedidorFrames` sin tirones fuera de su tolerancia; la partida real del humano no se toca.
2. Correr la suite completa: `bash herramientas/verificar.sh todo` y `bash herramientas/verificar.sh playmode-por-area`; anotar el resultado por
   área en docs/revision-tony.md.
3. docs/revision-tony.md: sección "Habilidades (T-F7, bloque 4) cerradas: qué probar jugando" con la lista para un humano (los criterios de
   sensación de T-174 a T-179 juntos, en orden de juego), la tabla de habilidades y munición (de la bitácora de T-179), la de jefes, las preguntas
   abiertas para Tony (D-tony-142) y lo que queda para Alan (D-tony-141).
4. docs/guia-para-jugar.md: en los controles del mecha, "Habilidad del brazo / del torso: 1 / 2" y dónde se compra la munición.

## Carpetas permitidas
Assets/_Proyecto/Tests/PlayMode/Progresion/, docs/capturas/, docs/revision-tony.md (sección de esta tarea), docs/guia-para-jugar.md
No tocar: código del juego (si el recorrido encuentra un error, se anota en la bitácora y se pide una tarea de corrección; no se arregla aquí),
Core/, escenas, todo lo de Alan.

## Criterios de aceptación (los comprueba el verificador)
- [ ] RecorridoHabilidadesPruebas pasa 3 corridas seguidas; la bitácora anota el tiempo de juego de cada paso (a a i)
- [ ] El dinero, la munición, el inventario y las piezas finales coinciden con lo calculado desde los assets (la prueba los compara y los imprime)
- [ ] `bash herramientas/verificar.sh todo` pasa en una sola llamada (salvo los 9 fallos conocidos de Alan en Arena) y `playmode-por-area` no
      marca ningún área "LIMITE"
- [ ] Capturas docs/capturas/T-180-1.png (cabina con el misil en vuelo hacia TORNILLO y la fila 1 enfriando), T-180-2.png (cabina con "SIN
      MUNICIÓN" mientras se pelea a puños) y T-180-3.png (DON FIERRO en su aviso antes de embestir), revisadas a ojo
- [ ] docs/revision-tony.md tiene la sección de cierre con la lista para un humano, las tablas, las preguntas y lo de Alan; la guía tiene las teclas

## Criterios de sensación (para un humano)
- Pruebas_Economia, Play, sin ayuda, unos 20 minutos: la liga Sótano de verdad (sin L) con una pieza de habilidad y la munición que puedas
  pagar. ¿Las habilidades se sienten como un as bajo la manga o como lo principal? ¿Te quedaste sin munición alguna vez y tuviste que pelear
  a puños? ¿El dinero alcanza para reparar, mejorar Y cargar munición, o hay que escoger (lo que se busca)? ¿La embestida de DON FIERRO te
  enseñó a esperar su aviso?

## Pruebas automáticas
- PlayMode: RecorridoHabilidadesPruebas.

## Fuera de alcance
- Integración en el taller de la casa y el mercado negro de Alan en Ciudad.unity (se escribe cuando Alan los abra, como T-136). Prueba humana
  completa de H7 (al final de T-F7).
