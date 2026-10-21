# T-173 Recorrido de las piezas (cierre del bloque 3 de T-F7)
Dueño: Tony | Rama: tony/T-173-recorrido-piezas | Modelo: Sonnet 5.5, medium
Depende de: T-163 a T-172

## Objetivo
Una prueba de PlayMode recorre, con entrada simulada y en Pruebas_Economia, el camino de las piezas: comprar la primera pieza en el taller,
ver que el nivel siguiente está bloqueado, ganar una pieza apostando en la liga, ponérsela y vender la vieja en el mercado negro, mezclar
ramas en los dos brazos, notar el cambio peleando, subir de liga y desbloquear el nivel siguiente, quedar bloqueado por renta y guardar y
cargar todo. Si pasa, el bloque "piezas por ramas y niveles" está cerrado del lado de Tony.

## Contexto
- Diseño: mecha-taller.md (criterios: "Puedo tener un brazo de fuego y el otro militar"; "Una pieza de nivel 3 no está disponible hasta
  llegar a la liga 3"; "Un mecha de cuerpo a cuerpo corre más y aguanta más que uno militar, y el militar pega más fuerte"),
  economia-misiones.md ("Puedo vender una pieza vieja en el mercado negro"; taller bloqueado por renta no deja mejorar).
- Decisiones: D-tony-113 a D-tony-126. Tareas: T-163 a T-172.
- Ejemplos: `Tests/PlayMode/Progresion/RecorridoLigasPruebas.cs` (T-162) y `RecorridoEconomiaPruebas.cs` (T-151): estructura, `MedidorFrames`,
  `ServicioGuardado.RutaForzada`, atajos permitidos, registro de errores, helper de mapas de acciones.

## Qué hacer
1. `Tests/PlayMode/Progresion/RecorridoPiezasPruebas.cs` en Pruebas_Economia, de noche y en `Libre`, con entrada simulada (caminar, E,
   flechas, Enter, Esc, ratón para pelear, F/Espacio/Q para encender). Atajos permitidos, anotados en la bitácora: `Colocar` cerca del
   banco, del mostrador o del mercado; dinero inicial por código; `RecibirDanio` al rival solo después de daño real al jugador;
   `ServicioLigas.Poner` y el adeudo de renta por código solo donde se indica. Números calculados desde los assets. Pasos, uno por uno:
   a) inicio: liga 1, las 5 piezas de serie, dinero fijado por código;
   b) taller: "PIEZAS DEL MECHA", compra PIERNAS CUERPO A CUERPO I (cobra su precio); las piezas de nivel II salen "SE ABRE EN LA LIGA 2";
   c) sube al mecha y enciende: la pantalla de piezas de la cabina lista las piernas nuevas; W sostenida: velocidad medida = la compuesta;
   d) cartelera: TORNILLO apostando al menos la mínima por pieza y gana: la pieza en juego de la liga 1 llega al inventario (si es igual a la
      puesta, queda en el inventario); repara en el banco;
   e) taller: compra BRAZO IZQ. FUEGO I y BRAZO DER. MILITAR I (dos ramas en los brazos); los brazos de serie quedan en el inventario;
      pone otra vez el brazo izquierdo de serie desde el inventario (sin cobrar) y vuelve a poner el de fuego; en la siguiente pelea, un golpe
      al rival quita el daño compuesto;
   f) mercado negro: vende las piernas de serie y la pieza repetida (pagan su valor de venta) y ya no están en el inventario;
   g) `Poner(liga 3, siguiente 1, 2 campeones)`: en el taller el nivel III ya se compra y el IV sale "SE ABRE EN LA LIGA 4";
   h) renta vencida sin dinero (por código): el banco dice lo de hoy, la pantalla de piezas no se abre y nada se compra;
   i) F5 guarda con 20 de daño; poner las de serie y cambiar el dinero por código; F9 carga: mismas 5 piezas, misma vida máxima, 20 de
      daño, mismo inventario y dinero;
   j) consola sin errores ni excepciones; `MedidorFrames` sin tirones fuera de su tolerancia; la partida real del humano no se toca.
2. Correr la suite completa: `bash herramientas/verificar.sh todo` y `bash herramientas/verificar.sh playmode-por-area`; anotar el resultado por
   área en docs/revision-tony.md.
3. docs/revision-tony.md: sección "Piezas (T-F7, bloque 3) cerradas: qué probar jugando" con la lista para un humano (los criterios de
   sensación de T-166 a T-172 juntos, en orden de juego), la tabla de bonos y precios (T-166), la de piezas en juego (T-171), la de razones
   de balance (T-172), las preguntas abiertas para Tony (D-tony-126) y lo que queda para Alan (D-tony-124).

## Carpetas permitidas
Assets/_Proyecto/Tests/PlayMode/Progresion/, docs/capturas/, docs/revision-tony.md (sección de esta tarea)
No tocar: código del juego (si el recorrido encuentra un error, se anota en la bitácora y se pide una tarea de corrección; no se arregla aquí),
Core/, escenas, todo lo de Alan.

## Criterios de aceptación (los comprueba el verificador)
- [ ] RecorridoPiezasPruebas pasa 3 corridas seguidas; la bitácora anota el tiempo de juego de cada paso (a a j)
- [ ] El dinero, el inventario y las piezas finales coinciden con lo calculado desde los assets (la prueba los compara y los imprime)
- [ ] `bash herramientas/verificar.sh todo` pasa en una sola llamada (salvo los 9 fallos conocidos de Alan en Arena) y `playmode-por-area` no
      marca ningún área "LIMITE"
- [ ] Capturas docs/capturas/T-173-1.png (pantalla de piezas con ramas mezcladas y el nivel siguiente bloqueado), T-173-2.png (cabina con
      brazos de dos ramas durante el encendido) y T-173-3.png (mercado negro vendiendo una pieza vieja), revisadas a ojo
- [ ] docs/revision-tony.md tiene la sección de cierre con la lista para un humano, las tablas, las preguntas y lo de Alan

## Criterios de sensación (para un humano)
- Pruebas_Economia, Play, sin ayuda, unos 20 minutos: pelea la liga Sótano de verdad (sin L) comprando piezas con lo que ganas. ¿Cada pieza
  se nota al pelear? ¿Cuerpo a cuerpo se siente más aguantador y militar más pegador? ¿El dinero alcanza para reparar Y mejorar, o hay que
  escoger (que es lo que se busca)? ¿Llegas a DON FIERRO con ganas de ponerte el nivel I que te falta?

## Pruebas automáticas
- PlayMode: RecorridoPiezasPruebas.

## Fuera de alcance
- Integración en el taller de la casa de Alan en Ciudad.unity (se escribe cuando Alan coloque el mecha y el mostrador, como T-136). Armas y
  habilidades por pieza (bloque 4). Prueba humana completa de H7 (al final de T-F7).
