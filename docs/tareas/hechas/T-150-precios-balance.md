# T-150 Precios de la economía y pruebas de balance
Dueño: Tony | Rama: tony/T-150-precios-balance | Modelo: Sonnet 5.5, medium
Depende de: T-144, T-146, T-147, T-148, T-149

## Objetivo
Todos los números de la economía después de la demo (premios, apuestas, momios, kits, ventas, reparación, renta) están en sus assets con los
valores de D-tony-86, y una prueba de EditMode lee esos assets y falla si alguien rompe las reglas de balance (por ejemplo, un objeto que se
vende por más de lo que cuesta, o un kit que repara más barato que el taller). Así Tony puede mover precios en el inspector sin miedo.

## Contexto
- Diseño: economia-misiones.md (las peleas son la fuente principal de dinero; quedar en negativo es intencional; preguntas abiertas "precios y
  premios" y "cuánto cuesta la renta"), mecha-taller.md (precios de piezas y reparaciones: abierta).
- Decisiones: D-tony-44 (números de la demo, siguen), D-tony-86 (tabla de este bloque y reglas de balance, propuesta).
- Assets: `Economia/Datos/DatosEconomia_Base`, `DatosPremioPelea_*`, `DatosApuestaPelea_Casera` (T-144), `DatosOferta_KitReparacion` (T-146),
  `DatosObjeto_*` y `CatalogoObjetos_Base` (T-142), `Mecha/Datos/ConfigKitReparacion` (T-148), `Mecha/Datos/DatosMecha_Jugador` (vida máxima).

## Qué hacer
1. Revisar que cada asset tiene el valor de la tabla de D-tony-86 y corregir con los creadores de datos (nunca editando el YAML a mano) lo
   que no coincida. Si un creador pisa valores ya ajustados en el inspector, hazlo idempotente (solo crea si no existe), como CreadorReloj de Alan.
2. EditMode `Tests/EditMode/Economia/BalanceEconomiaPruebas.cs` que carga los assets reales y comprueba (cada regla, una prueba con mensaje
   que diga qué número rompe qué regla):
   a) el premio de la casera al ganar alcanza para una renta (premio ≥ renta);
   b) reparar de KO a vida completa cuesta más que el premio de la casera (perder duele);
   c) un kit cuesta más por punto de vida que el taller (precio del kit ÷ (cura × vida máxima del mecha del jugador) > costo por punto);
   d) ningún `DatosObjeto` con oferta se vende por su precio de compra o más;
   e) la pieza en juego de cada `DatosApuestaPelea` vale menos que el premio de esa pelea al ganar;
   f) en cada `DatosApuestaPelea`: momio entre 0.2 y 3, montos en orden creciente, todos ≤ `apuestaMaxima`, y `apuestaMaxima` ≤ premio al ganar;
   g) el kit cabe en el inventario (máximo ≥ tope de kits por pelea).
3. Tabla final de precios en la bitácora y en docs/revision-tony.md (para que Tony la ajuste jugando).

## Carpetas permitidas
Assets/_Proyecto/Economia/ (Datos/, Editor/), Assets/_Proyecto/Mecha/Datos/ y Mecha/Editor/ (solo los creadores de datos de kits),
Assets/_Proyecto/Pelea/Editor/ (solo si el premio vive en un creador de Pelea), Assets/_Proyecto/Tests/EditMode/Economia/
No tocar: Core/, código de juego fuera de los creadores de datos, escenas, todo lo de Alan.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa (salvo los 9 fallos conocidos de Alan en Arena)
- [ ] BalanceEconomiaPruebas a) a g) pasan con los assets reales
- [ ] Si se cambia a mano (en memoria, dentro de la prueba o en una prueba aparte) el valor de venta del brazo a $1,300, la regla d) falla con un
      mensaje que nombra el objeto (se muestra en la bitácora la corrida que falla)
- [ ] Correr dos veces todos los creadores de datos de Economia no cambia ningún asset (git status limpio tras la segunda corrida)
- [ ] La tabla de la bitácora coincide con los assets (el verificador compara 5 valores al azar)

## Criterios de sensación (para un humano)
- Juega una semana de juego en Pruebas_Economia (J para pasar días): peleas, apuestas, kits, ventas, reparaciones y renta. ¿Se vive al día,
  con decisiones (apostar o no, kit o taller, reparar todo o a medias) y sin quedar atorado sin salida?

## Pruebas automáticas
- EditMode: BalanceEconomiaPruebas.

## Fuera de alcance
- Premios por liga, precios de piezas por rama y nivel, munición (sus bloques). "Una pelea paga más que una misión de barrio" (bloque de misiones).
