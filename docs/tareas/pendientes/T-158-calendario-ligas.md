# T-158 Calendario de las ligas: premios, momios y presentadores por liga
Dueño: Tony | Rama: tony/T-158-calendario-ligas | Modelo: Sonnet 5.5, medium
Depende de: T-154, T-155, T-157

## Objetivo
Existe el calendario de las 4 ligas: un cartel por cada uno de los 20 rivales, con lo que paga, la apuesta que acepta (momio, tope, montos y
pieza en juego) y lo que dice el presentador. Cada liga paga más que la anterior, el campeón paga mucho más, y los momios suben con la liga.
Una prueba de balance vigila esas reglas para que Tony ajuste números en los assets sin romperlas.

## Contexto
- Diseño: personaje-progresion.md ("Cada una paga más y tiene rivales más duros"), economia-misiones.md ("Las peleas del clandestino son la
  fuente principal de dinero"; apuestas; quedar en negativo es intencional).
- Decisiones: D-tony-80 y D-tony-86 (reglas y precios de la apuesta casera; la apuesta máxima no pasa del premio; la pieza en juego vale menos
  que el premio), D-tony-48. Propuesta nueva: D-tony-98 (premios y momios por liga).
- Tipos: `Pelea/Datos/CartelPelea.cs` (T-155), `Economia/Apuestas/DatosApuestaPelea.cs` (T-144), `Pelea/Datos/ConfigPresentador.cs`
  (`{liga}` desde T-155), `Progresion/Datos/DatosLigas_Base.asset` (T-154), prefabs `IA/Prefabs/Ligas/RivalLiga<L>_<N>.prefab` (T-157).
- Ejemplos: `Economia/Editor/CreadorApuestas.cs`, `Pelea/Editor/CreadorFlujoPeleaCasera.cs` (presentador de la casera),
  `Tests/EditMode/Economia/BalanceEconomiaPruebas.cs` (T-150: reglas sobre assets reales).

## Qué hacer
1. `Economia/Editor/CreadorApuestasLiga.cs`: 8 assets en Economia/Datos/ (`DatosApuestaPelea_Liga1` a `_Liga4` y `DatosApuestaPelea_Jefe1` a
   `_Jefe4`), idempotente y sin pisar valores existentes:

   | Apuesta | Momio | Máxima | Montos | Pieza en juego |
   | --- | --- | --- | --- | --- |
   | Liga1 | 1.0 | $500 | 100 / 250 / 500 | Brazo de chatarra desde $250 |
   | Jefe1 | 1.5 | $1,000 | 250 / 500 / 1,000 | Brazo de chatarra desde $500 |
   | Liga2 | 1.2 | $1,000 | 250 / 500 / 1,000 | ninguna |
   | Jefe2 | 1.8 | $2,000 | 500 / 1,000 / 2,000 | ninguna |
   | Liga3 | 1.4 | $1,500 | 500 / 1,000 / 1,500 | ninguna |
   | Jefe3 | 2.0 | $3,000 | 1,000 / 2,000 / 3,000 | ninguna |
   | Liga4 | 1.6 | $2,000 | 500 / 1,000 / 2,000 | ninguna |
   | Jefe4 | 2.5 | $5,000 | 1,000 / 2,500 / 5,000 | ninguna |

   (Las piezas de las ligas 2 a 4 llegan con el bloque de piezas.)
2. Presentadores en Pelea/Datos/: `ConfigPresentador_Liga` ("LIGA {liga} DEL CLANDESTINO", "¡{alias} CONTRA {rival}!", grito en la 2) y
   `ConfigPresentador_Jefe` ("¡NOCHE DE CAMPEONES EN LA LIGA {liga}!", "EL CAMPEÓN... ¡{rival}!", "EL RETADOR... ¡¡{alias}!!", grito en la 3).
   Sin presentación en revancha.
3. `Pelea/Datos/CalendarioLigas.cs` (ScriptableObject): una fila por liga, cada una con sus `CartelPelea` en orden (clase `[Serializable]` para
   la fila). `Cartel(liga, indice)` (1-based; null si no existe), `CantidadLigas`, `RivalesEn(liga)`. Asset `Pelea/Datos/CalendarioLigas_Base.asset`
   con `Pelea/Editor/CreadorCalendarioLigas.cs` (idempotente; carga los prefabs de IA por ruta, sin referenciar IA). Premios (D-tony-98):

   | Liga | Rivales 1 a 4 (ganar) | Jefe (ganar) | Perder (todos) |
   | --- | --- | --- | --- |
   | 1 Sótano | 600, 700, 800, 900 | 2,000 | 0 |
   | 2 Calle | 1,000, 1,100, 1,200, 1,300 | 3,000 | 100 |
   | 3 Distrito | 1,500, 1,650, 1,800, 1,950 | 4,500 | 150 |
   | 4 Campeonato | 2,200, 2,400, 2,600, 2,800 | 7,500 | 200 |

   Cada cartel: su prefab `RivalLiga<L>_<N>`, `liga` = L, `esJefe` solo en N = 5, apuesta `Liga<L>` o `Jefe<L>`, presentador de liga o de jefe.
4. EditMode `Tests/EditMode/Pelea/BalanceLigasPruebas.cs` sobre los assets reales, una prueba por regla con mensaje que diga qué número la rompe:
   a) el calendario tiene tantas ligas y rivales como `DatosLigas_Base`, y cada cartel tiene prefab, apuesta y presentador, `liga` correcta y
   `esJefe` solo en el último; b) dentro de cada liga el premio sube con el índice y el jefe paga al menos el doble que el rival 1; c) el rival 1
   de cada liga paga más que el rival 4 de la anterior; d) el momio sube de liga en liga y el del jefe es mayor que el de su liga; e) la apuesta
   máxima no pasa del premio de ganar de su cartel y los montos van de menor a mayor sin pasar la máxima; f) perder paga como mucho el 20 % de
   ganar; g) el rival 1 de la liga 1 paga al menos lo de la casera (`DatosPremioPelea` de la casera); h) el premio de cada jefe es mayor que
   reparar de KO el mecha del jugador (`ReglaReparacion` con la vida de `DatosMecha_Jugador`); i) la pieza en juego, si hay, vale menos que el
   premio; j) `fraccionPremioRevancha` está entre 0 y 1 sin incluirlos.

## Carpetas permitidas
Assets/_Proyecto/Pelea/ (Datos/, Editor/), Assets/_Proyecto/Economia/ (Datos/, Editor/), Assets/_Proyecto/Tests/EditMode/Pelea/
No tocar: Core/, IA/ (solo se leen sus prefabs), Progresion/, `FlujoPelea.prefab`, `FlujoPeleaCasera.prefab`, escenas, todo lo de Alan.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa (salvo los 9 fallos conocidos de Alan en Arena)
- [ ] BalanceLigasPruebas pasa las reglas a a j; BalanceEconomiaPruebas (T-150) sigue en verde
- [ ] Cambiar a mano en un asset temporal el premio del jefe de la liga 2 a $1,500 hace fallar la regla b con un mensaje que lo nombra (se
      comprueba y se revierte; la bitácora lo anota)
- [ ] Los creadores corridos dos veces no cambian ningún asset (md5 igual); cada asset va con su .meta

## Criterios de sensación (para un humano)
- Con la tabla en la mano: ¿la subida de premios de liga en liga se siente como progreso (el jefe de la liga 1 paga más de 3 veces la casera)?
  ¿Momios de 1.0 a 2.5 hacen tentador apostar fuerte en una pelea de jefe? Se ajustan en los assets; la prueba de balance avisa si rompes algo.

## Pruebas automáticas
- EditMode: BalanceLigasPruebas.

## Fuera de alcance
- Usar el calendario en el mostrador (T-159). Piezas en juego de las ligas 2 a 4 (bloque de piezas). Momios que cambian según el historial.
