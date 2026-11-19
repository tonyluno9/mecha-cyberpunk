# T-1366: arreglos del Puerto

Rama tony/T-1360-barrido-cerro-puerto (sesión s2). Según el barrido (T-1360), los recorridos (T-1361) y las referencias K15/A15. Solo cambia `Assets/Arte/Mundo/Zonas/T1357_Puerto.prefab`; no rehace el prefab con el generador.

## Hecho (sin correr: lo corre el orquestador)
- `Assets/Arte/Editor/ArreglarPuertoT1366.cs`: `Aplicar()` (idempotente, informe `Logs/t1366-arreglos.txt`, cierra con `BarridoZonasT1360.BarrerZona`), `FotosAntes()` y `FotosDespues()` (`docs/capturas/bugs-18nov-B/t1366-<arreglo>-<n>-antes|despues.png`; correr `FotosAntes` ANTES de `Aplicar`).
- `Assets/_Proyecto/Tests/EditMode/Mundo/ArreglosPuertoT1366Pruebas.cs`: pasan solo después de `Aplicar`.

## Hallazgos y decisiones
- Las filas de contenedores YA están juntas (paso 2.55 m, contenedor de 2.465 m: hueco 0.085 m). El "hueco 2.64 m" del recorrido es el salto entre la fila f y la f+2 que elige la prueba entre contenedores tapados por otros (todos `@0.0`). Se apagan los colliders de contenedores tapados (los cubre el collider de su pila en el Bloque) para que la prueba solo vea contenedores expuestos. El resto (bloques, pasillos) no se mueve.
- Apoyo: cada pila se reapoya exacto (y alineado al de abajo) por mallas; los colliders de pila del Bloque y el LODGroup se recalculan.
- `Escalera_Fila` se corre a 2 m del centro de la columna oeste: "BAJAR" caminaba 6 m desde el centro y quedaba justo en el borde (6.1 m de media longitud).
- Sin escalera visual: no hay modelo adecuado (ver `docs/assets-faltan.md`, "18-nov D: Puerto").
- Grúas y barco, bodegas, pallets, huacales, tambos y montacargas: los prefabs ya traen collider; el generador los apagó. Se encienden. Pallets/montacargas a menos de 2.8 m del eje de una calle se apartan (el montacargas de (159,-130) estaba sobre el eje de Calle_1).
- Barco: línea de flotación (quilla bajo y = -3) y hueco 1 a 3 m al muelle; las patas de las grúas se llevan a y = -1; bitas dentro de las patas se corren en x.
- Parpadeo: cada Tramo sube 3 mm sobre el anterior y las rayas van por encima de todos los asfaltos. Los charcos están a 1.2 cm sobre el patio (y = 0), no coplanares con el asfalto (top 0.08): no se tocan.
- Carro atorado en (164.1, 0, -166): no es un obstáculo del mapa; el circuito de la prueba une el final de Calle_3 con el principio de Calle_4 en línea recta por el bloque 16. No se cambia ningún CalleZona (sus extremos son uniones con la sesión A); se corrige en la prueba.
- Atrapados (3): eran la punta de 7 m de postes (collider de caja de 7 m). El collider se recorta a 2.5 m.
- Entradas norte y oeste: de la sesión A, anotadas en `docs/bugs-mapa-18nov-B.md`.
- Look: 5 pallets y tambos extra por bodega (35 piezas de prefabs existentes). Sin grafitis (faltan).
