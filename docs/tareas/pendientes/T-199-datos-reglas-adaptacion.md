# T-199 La adaptación como datos y reglas: hábitos, memoria y niveles
Dueño: Tony | Rama: tony/T-199-datos-reglas-adaptacion | Modelo: Sonnet 5.5, medium
Depende de: nada (puede ir a la par de T-198)

## Objetivo
Las reglas de la adaptación existen como lógica pura probada y como datos que Tony balancea: qué hábito del jugador se vio en una pelea,
cómo lo recuerda el rival, cuándo sube o baja cada adaptación de nivel, y los números de las tres respuestas (GUARDIA, FINTAS, RETIRADA).
Todavía nadie las usa en una pelea (eso empieza en T-200). Con esto el bloque tiene una sola fuente de números y sus reglas de balance
ya comprobadas contra los assets reales.

## Contexto
- Diseño: combate.md ("El rival se adapta a tu forma de pelear en peleas repetidas"; "Puedo bloquear un golpe y contraatacar con buen timing").
- Decisiones: D-012 (golpe en tres tiempos), D-tony-165 y D-tony-171 g (SINAPSIS y sus pisos). Propuestas: D-tony-192 (tres hábitos, tres
  respuestas), D-tony-193 (umbrales), D-tony-194 (memoria), D-tony-195 (quién usa cada asset), D-tony-196 (números), D-tony-197 (balance),
  D-tony-199 (dónde vive).
- Modelos: `IA/Rival/ReglaPiedad.cs` y `IA/Datos/ConfigPiedadRival.cs` (regla pura + datos), `IA/Datos/DatosHabilidadRival.cs`,
  `IA/Editor/CreadorHabilidadesJefes.cs` (creador idempotente), `Tests/EditMode/IA/ReglaPiedadPruebas.cs` y `RivalesLigaPruebas.cs` (pruebas
  contra assets reales).
- Para la regla de la guardia: `Mecha/Datos/DatosMecha_Jugador.asset` (preparación de serie 0.25 s), `Mecha/Piezas/ReglaReaccionImplantes.cs`
  y `Progresion/Datos/Implantes/DatosImplante_implante_sistemanervioso_2.asset` (SINAPSIS MILITAR: 0.175 s), los 21 `DatosMecha` de los rivales
  que se adaptan (`IA/Datos/Ligas/DatosMecha_Liga*_*.asset` y `IA/Datos/DatosMecha_Casero.asset`; hoy todos suben la guardia en 0.15 s).

## Qué hacer
1. `Combate/Lectura/TipoAdaptacion.cs`: `Guardia` (responde a PUÑOS), `Fintas` (a CUBRIRSE), `Retirada` (a CASTIGAR); "solo se agregan valores
   al final". `Combate/Lectura/CambioAdaptacion.cs`: `readonly struct` con `Tipo`, `NivelAntes`, `NivelDespues`, `Subio`, `Bajo`. Van en Combate
   y no en IA porque UI los leerá en T-203 sin ver IA (D-tony-199).
2. `IA/Datos/DatosAdaptacionRival.cs` (ScriptableObject, `[Tooltip]` con unidad y `[Min]`/`[Range]` en cada número; arreglos por nivel de
   largo 2, índice 0 = nivel I, validados en `OnValidate`), con los valores de D-tony-193 a D-tony-196 por defecto:
   - Observación: `segundosMinimos` 5; PUÑOS `golpesLanzadosMinimos` 8 y `golpesPorMinutoMinimo` 24; CUBRIRSE `golpesRivalMinimos` 4 y
     `fraccionCubiertaMinima` 0.5; CASTIGAR `golpesConectadosMinimos` 4 y `fraccionCastigoMinima` 0.4.
   - Memoria: `umbralesNivel` {1, 3}, `nivelMaximo` (1 o 2), `olvidoPorPelea` 1, `maximoActivas` 2, `derrotasSinAprender` 2.
   - GUARDIA: `probabilidadGuardia` {0.4, 0.65}, `reaccionGuardia` 0.06 s, `sostenerTrasImpacto` 0.3 s.
   - FINTAS: `extraAvisoMaximo` {0.25, 0.4} s, `probabilidadAmago` {0.2, 0.35}, `fraccionAmago` 0.6, `esperaTrasAmago` 0.3 s.
   - RETIRADA: `probabilidadRetirada` {0.45, 0.7}, `factorVelocidadRetirada` {0.8, 0.9}, `demoraRetirada` 0.15 s, `margenRetirada` 0.75 m.
   Sin campos de vida, daño, alcance, recarga ni velocidad absoluta (regla a de D-tony-197).
3. `IA/Adaptacion/PerfilEstilo.cs` (clase pura): lo que pasó en una pelea: `GolpesLanzados`, `GolpesConectados`, `ConectadosCastigando`,
   `GolpesRival` (golpes del rival que llegaron al jugador), `GolpesRivalCubiertos`, con métodos `RegistrarGolpeLanzado()`,
   `RegistrarConectado(bool rivalRecuperando)`, `RegistrarGolpeRival(TipoBloqueo bloqueo)` y `Reiniciar()`.
4. `IA/Adaptacion/ReglaEstilo.cs` (estática, pura): `bool Vio(TipoAdaptacion tipo, PerfilEstilo perfil, float segundos, DatosAdaptacionRival datos)`
   con los umbrales del punto 2 (PUÑOS = golpes lanzados por minuto; CUBRIRSE = cubiertos / golpes del rival; CASTIGAR = conectados
   castigando / conectados). Con `segundos` menor que el mínimo no se vio nada.
5. `IA/Adaptacion/MemoriaRival.cs` (`[Serializable]`: `idRival`, `evidencias` int[3] indexado por `TipoAdaptacion`, `peleas`, `derrotasSeguidas`)
   y `IA/Adaptacion/ListaMemoriasRival.cs` (`[Serializable]`, lista de memorias, para `JsonUtility`).
6. `IA/Adaptacion/ReglaAdaptacion.cs` (estática, pura):
   - `int Nivel(MemoriaRival memoria, TipoAdaptacion tipo, DatosAdaptacionRival datos)`: el mayor nivel cuyo umbral alcanza su evidencia, sin
     pasar de `nivelMaximo`; con memoria null, 0.
   - `List<CambioAdaptacion> Aprender(MemoriaRival memoria, bool[] vistos, bool ganoElJugador, DatosAdaptacionRival datos)`, en este orden:
     niveles de antes; `peleas` + 1; `derrotasSeguidas` (0 si ganó el jugador, + 1 si perdió); puede subir solo si `derrotasSeguidas`, ya
     contando esta pelea, es menor que `derrotasSinAprender` (la primera derrota enseña, la segunda seguida ya no); cada tipo visto suma 1
     (solo si puede subir), cada tipo no visto resta `olvidoPorPelea` (sin bajar de 0); la
     evidencia no pasa del umbral del nivel máximo; si quedan más de `maximoActivas` tipos con nivel ≥ 1, baja a nivel 0 (evidencia = umbral
     del nivel I − 1) el de menos evidencia (empate: primero el que no se vio en esta pelea, luego el que va después en el enum); devuelve un
     cambio por cada tipo cuyo nivel cambió.
7. `IA/Editor/CreadorAdaptacionRival.cs` (menú "Mecha Cyberpunk/Crear T-199 adaptación" y método para batch): crea
   `IA/Datos/DatosAdaptacionRival_Base.asset` (`nivelMaximo` 1) y `_Avanzado.asset` (`nivelMaximo` 2); no pisa los valores de uno que ya
   existe; dos corridas no cambian nada. T-200 a T-202 lo extienden.
8. Pruebas EditMode en `Tests/EditMode/IA/`:
   - `ReglaEstiloPruebas`: cada hábito justo en su umbral y justo debajo; mínimos de muestras; pelea más corta que el mínimo; una pelea con dos
     hábitos a la vez.
   - `ReglaAdaptacionPruebas`: primera pelea siempre en 0; 1 pelea con PUÑOS da GUARDIA I en Base y en Avanzado; 3 seguidas dan II solo en
     Avanzado; olvido (una pelea sin el hábito baja la evidencia en 1); tope (10 peleas con PUÑOS en Avanzado y luego 3 sin él dejan GUARDIA
     en 0); con los 3 hábitos vistos quedan 2 activos y el desempate es el del punto 6; la primera derrota enseña, la segunda seguida no sube
     nada pero sí olvida, y la siguiente victoria vuelve a enseñar; los `CambioAdaptacion` devueltos son exactos; `ListaMemoriasRival` va y vuelve por `JsonUtility` igual.
   - `BalanceAdaptacionPruebas` (con los assets reales): reglas a, b, d, e y f de D-tony-197 que son números: probabilidades ≤ 0.65 / 0.35 / 0.7,
     factores ≤ 1, extra ≤ 0.4 s, umbrales {1, 3}, `nivelMaximo` 1 y 2, `maximoActivas` ≤ 2; y la ventana de la guardia: para cada uno de los 21
     rivales que se adaptan, `reaccionGuardia` + su `TiempoSubirGuardia` ≤ preparación de serie del jugador − 1/30 s y ≥ preparación con SINAPSIS
     II (`ReglaReaccionImplantes` con el asset del implante) + 1/30 s. La prueba imprime las cuentas.

## Carpetas permitidas
Assets/_Proyecto/Combate/Lectura/ (solo TipoAdaptacion.cs y CambioAdaptacion.cs, nuevos), Assets/_Proyecto/IA/Adaptacion/ (nueva),
Assets/_Proyecto/IA/Datos/ (solo DatosAdaptacionRival.cs y sus 2 assets), Assets/_Proyecto/IA/Editor/CreadorAdaptacionRival.cs (nuevo),
Assets/_Proyecto/Tests/EditMode/IA/, docs/bitacora/
No tocar: Core/, IA/Rival/, los prefabs, el resto de Combate/, UI/, Mecha/ y Progresion/ (solo se leen sus assets), escenas, todo lo de Alan.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa (salvo los fallos conocidos de Alan en Arena, listados por nombre en docs/revision-tony.md)
- [ ] ReglaEstiloPruebas, ReglaAdaptacionPruebas y BalanceAdaptacionPruebas pasan; ninguna prueba vieja cambió
- [ ] Correr CreadorAdaptacionRival dos veces no cambia los dos assets (md5 igual) y sus valores son los de D-tony-193 a D-tony-196
- [ ] Grep: las clases de IA/Adaptacion/ de esta tarea no heredan de `MonoBehaviour` ni `ScriptableObject` y no leen `Time` ni `UnityEngine.Random`
- [ ] La bitácora imprime la cuenta de la ventana de la guardia (0.06 + 0.15 = 0.21 s contra 0.25 − 1/30 y 0.175 + 1/30)

## Criterios de sensación (para un humano)
- Abrir DatosAdaptacionRival_Base en el inspector: ¿se entiende cada número por su tooltip sin abrir el código? ¿Los umbrales de los
  hábitos (24 golpes por minuto, la mitad de los golpes con la guardia arriba, 40 % de golpes castigando) se parecen a cómo peleas tú?

## Pruebas automáticas
- EditMode: ReglaEstiloPruebas, ReglaAdaptacionPruebas, BalanceAdaptacionPruebas.

## Fuera de alcance
- Mirar una pelea y guardar la memoria (T-200), la guardia del rival (T-201), fintas y retirada (T-202), pantallas (T-203). Una cuarta
  respuesta contra las habilidades (D-tony-201 d). Hackeo.
