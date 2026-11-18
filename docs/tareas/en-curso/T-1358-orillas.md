# T-1358: abrir las orillas y límites del mundo nuevos

Rama: tony/T-1358-orillas (sale de 9220518). Plan: docs/plan-zonas-18nov.md. Script: `MechaCyberpunk.Arte.Editor.ColocarT1358Orillas.Aplicar`.
Estado: código escrito, SIN correr (sin Library en el worktree). Falta correr Aplicar y las pruebas.

## Qué hace Aplicar (idempotente)
Dentro de T530_Afueras apaga (SetActive false, no borra nada), por bounds:
- Todos los `LimiteInvisible` de los 6 finales (frente y laterales).
- Los 6 edificios de fondo (Edificio con 120 m de lado largo y 30 m o menos de fondo).
- Todo `Edificio` (incluye remates y flanqueantes) cuyos bounds toquen un rectángulo de unión: U1 x -54..-40 / z -240..-187, U2 x 40..54 / z -240..-187,
  U4 x -320..-249 / z -7..7, U9 x 249..310 / z -7..7, U7 x -125..-100 / z 187..240 (esperado: el remate de x = -111 al norte).
- Las `Ventanas` y los LODGroup de S2_Calles de cada edificio apagado (si no quedarían flotando).
La lista exacta de lo apagado queda en Logs/t1358-orillas.txt (para deshacer: SetActive(true) de esas rutas). También lista "PENDIENTE" si algo
que no es de T530 aún tiene collider en una unión.

Crea la raíz T1358_Orillas con 5 `LimiteInvisible` (BoxCollider, sin malla, 30 m): Norte z=290 (x -480..345), Oeste x=-480 (z -330..290),
Este x=345 (z -45..290), MarSur z=-268 (x -480..337, cierra la esquina) y MarEste x=336 (z -268..-45), y un objeto `SalidaDelAgua`.

## SalidaDelAgua (Mundo/Ambiente)
Si el jugador queda más de 3 s con y < -2.5 dentro del área del mar, lo pone en el punto de orilla más cercano (calle del Malecón, z=-215 / U1-U2).
Un carro (cuerpo con Rigidbody que no es el jugador) se apaga (SetActive false), como hace TraficoCalle con los carros perdidos.
La rampa de la playa y la escalera del muelle son geometría de T-1354/T-1357: este componente es la red de seguridad. Los puntos de orilla son
una suposición (calle z=-215) y hay que ajustarlos al Malecón real. Si el muelle/escollera/faro pasan de z=-268 o x=336, mover los muros
(Aplicar avisa "ADVERTENCIA ... toca un limite nuevo" si ya existen raíces T1354..T1357).

## Pruebas
- EditMode: Tests/EditMode/Mundo/OrillasT1358Pruebas.cs ([Category("Escena")], corre Aplicar y guarda la escena) y CalculoSalidaDelAguaPruebas.cs.
- Las pruebas citadas (FinalesCarroPruebas, PulidoFinalesPruebas, BarrioPruebas, KitCallePruebas) usan Pruebas_Alan, no Ciudad_Prologo: no suponen
  las afueras cerradas, no se tocaron. CierresCiudadT986Pruebas usa Ciudad_Prologo y solo cuenta cierres: abrir calles no la rompe (por verificar).
  EscalaBarridoT530Pruebas sigue válida (los LimiteInvisible apagados siguen existiendo).
