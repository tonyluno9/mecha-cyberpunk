# T-171 Piezas de rama en juego en las apuestas de las ligas
Dueño: Tony | Rama: tony/T-171-piezas-en-juego | Modelo: Sonnet 5.5, medium
Depende de: T-166 (y T-168 para el criterio de ponerla en el taller)

## Objetivo
Apostar fuerte en una pelea de liga vuelve a valer la pena: cada liga pone en juego una pieza de rama de su nivel (y su jefe, otra),
que al ganar llega al inventario y se puede poner en el taller o vender en el mercado negro. (El jefe pone un torso o una cabina, que
dan más vida; mismo nivel y mismo valor que la de su liga.) Reemplaza al "Brazo de chatarra" que quedó
como pieza en juego de la liga 1 hasta que hubiera piezas por rama (D-tony-98). La pelea casera del prólogo sigue con el Brazo de chatarra.

## Contexto
- Diseño: economia-misiones.md ("Vendes ... piezas ganadas en peleas por apuesta ... en el mercado negro"), mecha-taller.md ("Una pieza de
  nivel 3 no está disponible hasta llegar a la liga 3": se gana en la liga 3, así que no se adelanta).
- Decisiones: D-tony-80 (si apostaste al menos el mínimo de la pieza te la llevas; si no cabe, te pagan su valor de venta), D-tony-95
  (revanchas a mitad de premio), D-tony-98 (premios, momios y topes por liga), D-tony-110 (los creadores solo escriben al crear). Propuesta
  nueva: D-tony-121 (tabla de abajo y "en una revancha no hay pieza en juego").
- Código: `Economia/Apuestas/DatosApuestaPelea.cs` (`piezaEnJuego`, `apuestaMinimaPorPieza`, `Crear`), assets
  `Economia/Datos/DatosApuestaPelea_Liga1..4` y `_Jefe1..4`, `Economia/Editor/CreadorApuestasLiga.cs`, `Progresion/Prologo/MostradorPeleas.cs`
  (arma el cartel de una revancha, línea ~234), pruebas `Tests/EditMode/Pelea/BalanceLigasPruebas.cs` (regla i: la pieza vale menos que el
  premio), `Tests/EditMode/Economia/BalanceEconomiaPruebas.cs`, `Tests/PlayMode/Progresion/ApuestaMostradorPruebas.cs`, `RecorridoLigasPruebas.cs`.
- Las piezas son `DatosPieza` (Mecha), pero los assets de apuestas son de Economia: el creador de Economia las carga por ruta como
  `DatosObjeto` (sin referenciar Mecha).

## Qué hacer
1. Con el creador (un paso explícito de "ajustar piezas en juego", porque el creador no pisa assets existentes), pon en los assets:
   | Apuesta | Pieza en juego | Apuesta mínima por la pieza |
   | --- | --- | --- |
   | Liga 1 (rivales 1 a 4) | PIERNAS CUERPO A CUERPO I | $250 |
   | Jefe 1 (DON FIERRO) | TORSO MILITAR I | $500 |
   | Liga 2 | BRAZO IZQ. TECH II | $500 |
   | Jefe 2 (LA MATRACA) | TORSO CUERPO A CUERPO II | $1,000 |
   | Liga 3 | BRAZO DER. FUEGO III | $750 |
   | Jefe 3 (EL ALCALDE) | CABINA TECH III | $1,500 |
   | Liga 4 | PIERNAS MILITAR IV | $1,000 |
   | Jefe 4 (EL CACIQUE) | TORSO FUEGO IV | $2,500 |
   La apuesta mínima por la pieza es la mitad del tope de esa apuesta. `DatosApuestaPelea_Casera` no cambia (Brazo de chatarra).
2. Revancha de la cartelera: la pelea se arma con una apuesta igual pero sin pieza en juego (copia en ejecución con `DatosApuestaPelea.Crear`),
   y la pantalla de apuesta no anuncia pieza. Revisa también la revancha con R tras ganar una pelea de liga: si repite el cartel con pieza,
   quítasela igual; anota lo que hiciste.
3. Actualiza las pruebas que esperaban el Brazo de chatarra en la liga 1 (sin bajar lo que comprueban: ahora esperan la pieza de la tabla).
   Agrega a BalanceLigasPruebas: la pieza en juego de la liga L (rivales y jefe) es una pieza de rama de nivel L; la mínima por pieza ≤ tope.
4. PlayMode `Tests/PlayMode/Progresion/PiezaEnJuegoLigaPruebas.cs` en Pruebas_Economia con entrada simulada: retar a TORNILLO apostando
   $250 o más y ganar: el inventario tiene PIERNAS CUERPO A CUERPO I; en el banco del taller se ponen desde "PIEZAS DEL MECHA" (si T-169 ya
   está; si no, con `TallerPiezas` por código); una revancha contra TORNILLO ganada con la misma apuesta no da otra pieza. Atajos como en
   T-162 (daño al rival solo después de daño real al jugador), anotados.

## Carpetas permitidas
Assets/_Proyecto/Economia/Editor/, Assets/_Proyecto/Economia/Datos/ (solo los `DatosApuestaPelea_*` con el creador),
Assets/_Proyecto/Progresion/Prologo/MostradorPeleas.cs, Assets/_Proyecto/Pelea/ (solo si la revancha con R lo pide),
Assets/_Proyecto/UI/Peleas/ (solo si la pantalla de apuesta muestra la pieza de otra forma), Assets/_Proyecto/Tests/EditMode/Pelea/,
Assets/_Proyecto/Tests/EditMode/Economia/, Assets/_Proyecto/Tests/PlayMode/Progresion/, Assets/_Proyecto/Tests/PlayMode/Pelea/
No tocar: Core/, Mecha/ (las piezas ya existen), escenas, todo lo de Alan.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa (salvo los 9 fallos conocidos de Alan en Arena)
- [ ] Los 8 assets tienen la pieza y la mínima de la tabla (una prueba EditMode los lee); la casera sigue con el Brazo de chatarra
- [ ] BalanceLigasPruebas (con las reglas nuevas) y BalanceEconomiaPruebas pasan; RecorridoLigasPruebas y RecorridoEconomiaPruebas siguen en verde
- [ ] PiezaEnJuegoLigaPruebas pasa: la pieza ganada llega al inventario, se puede poner, y la revancha no da otra
- [ ] Correr el creador dos veces no cambia ningún asset (md5 igual)

## Criterios de sensación (para un humano)
- Pruebas_Economia: apuesta $250 contra TORNILLO. ¿La pieza en juego en la pantalla de apuesta te tienta? ¿Ganarla y ponértela en el taller
  se siente como un premio de verdad?

## Pruebas automáticas
- EditMode: BalanceLigasPruebas, BalanceEconomiaPruebas. PlayMode: PiezaEnJuegoLigaPruebas.

## Fuera de alcance
- Piezas al azar o que cambian por noche. Apostar en el casino de Alan. Balance de las ligas contra las piezas (T-172).
