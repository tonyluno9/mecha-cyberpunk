# T-141 Contrato de apuestas de pelea y PlayMode por área en verificar.sh
Dueño: Tony (tarea de Core, D-032) | Rama: tony/T-141-contrato-apuestas | Modelo: Sonnet 5.5, medium
Depende de: nada (conviene después de T-140)

## Objetivo
En Core/ está el contrato de las apuestas del jugador sobre su próxima pelea (`IApuestasPelea`), escrito antes de usarse (D-027): Tony lo
implementa en Economia (T-144) y lo usa en el mostrador de peleas (T-145); el casino de Alan (A-F5, "apuestas sobre las peleas") lo podrá leer
y usar para tomar apuestas sin implementar nada. Además, `herramientas/verificar.sh` gana un modo que corre PlayMode un ensamblado por
llamada con límite de tiempo, para que un cuelgue en un área no tumbe la verificación de las demás.

## Contexto
- Diseño: economia-misiones.md ("Complementos: ... apuestas"; "El casino ... con apuestas sobre las peleas"; "Apuestas sobre ti y pierdes:
  pierdes la apuesta y el premio"), personaje-progresion.md ("Perder una pelea no es game over: pierdes lo apostado").
- Decisiones: D-026 (las apuestas son de Tony), D-027, D-032, D-034 (Registro<T>), D-039 (dinero entero; `IntentarPagar` solo si alcanza).
  Propuestas nuevas: D-tony-78 y D-tony-80.
- Estilo de contratos: Core/Economia/ICartera.cs, Core/Economia/IRenta.cs, Core/Economia/ITienda.cs. Los `///` SON el contrato; el revisor los
  lee con la mirada de Alan.
- `MotivoDinero.Apuesta` ya existe; no se cambia `MotivoDinero`.
- Es tarea de Core: se integra a main en cuanto el verificador la apruebe, y se avisa en docs/revision-tony.md (sección "Para Alan").

## Qué hacer
1. `Core/Economia/IApuestasPelea.cs`: la apuesta del jugador sobre SU próxima pelea del clandestino (nunca sobre peleas ajenas). Miembros:
   - `bool Abierta`: hay una próxima pelea anunciada que todavía no empieza; solo entonces se puede apostar o cancelar.
   - `string Rival`: alias del rival de esa pelea (vacío si no está abierta).
   - `float Momio`: lo que gana el jugador por cada peso apostado si gana (1.0 = paga uno a uno: apuesta $250, recibe $500). Mayor que 0.
   - `int ApuestaMaxima`: tope de esa pelea ($).
   - `int Apostado`: lo apostado en la pelea abierta; 0 si nada. Una sola apuesta por pelea.
   - `bool PuedeApostar(int monto)`: abierta, sin apuesta previa, 0 < monto <= ApuestaMaxima y la cartera alcanza (`ICartera.PuedePagar`). No cambia nada.
   - `bool Apostar(int monto)`: si `PuedeApostar`, cobra con `ICartera.IntentarPagar(monto, MotivoDinero.Apuesta)` y devuelve true.
   - `bool Cancelar()`: mientras está abierta, devuelve lo apostado con `MotivoDinero.Apuesta` y deja `Apostado` en 0.
   - `event Action Cambio` (tras cualquier cambio de los anteriores) y `event Action<int> Resuelta` (al terminar la pelea: el cambio neto para
     el jugador, positivo lo ganado sin contar lo que se le devuelve, negativo lo perdido; 0 si no había apuesta no se avisa).
   Comentario de clase: la implementa Tony (`ServicioApuestas`, Economia/, en `ServiciosEconomia.prefab`, T-144) y se busca en
   `Registro<IApuestasPelea>`; quien anuncia la próxima pelea y la resuelve es Tony (mostrador y flujo de pelea), nadie más. Alan puede leerla y
   llamar `Apostar`/`Cancelar` desde el casino; perder significa perder lo apostado y el premio. Mientras no esté en main, usa un doble.
2. `Core/Guardado/IGuardable.cs`: en "Claves en uso" agrega `inventario` (Tony, T-142). Las apuestas no se guardan (D-tony-80): no se agrega clave.
   En el `///` de `Core/Economia/ITienda.cs`, cambia "Por ahora nadie la registra ... no la leas de Registro todavía" por: la registra el
   `MostradorTienda` de Tony (UI/, T-146) mientras está activo; Alan coloca el prefab del mostrador en su local y no implementa `ITienda` (D-tony-81).
3. Prueba EditMode `Tests/EditMode/Core/ContratosApuestasPruebas.cs` con un doble mínimo que cumpla el contrato tal como lo dicen los `///`
   (apostar sin estar abierta falla, dos apuestas en la misma pelea fallan, cancelar devuelve el dinero con motivo Apuesta, monto 0 o mayor
   al tope falla). Sirve de ejemplo para el doble de Alan.
4. `herramientas/verificar.sh`: modo nuevo `playmode-por-area`. Corre PlayMode un ensamblado por llamada de Unity (la misma lista de
   `ensamblados_modo PlayMode`), cada uno con su archivo `Logs/resultados-playmode-<Area>.xml` y `Logs/playmode-<Area>.log`, y un límite de
   tiempo por área (variable `LIMITE_AREA` en segundos, 900 por defecto). Si un área pasa el límite, cierra SOLO el Unity que lanzó, la marca
   "LIMITE" y sigue con la siguiente. Imprime una línea por área ("Economia 9/9", "Arena FALLO 8/17 ...", "Mecha LIMITE 900 s") y al final el
   total; sale con 0 solo si todas pasaron. Los modos existentes no cambian. Documéntalo en docs/herramientas-verificacion.md.

## Carpetas permitidas
Assets/_Proyecto/Core/Economia/, Assets/_Proyecto/Core/Guardado/IGuardable.cs (solo comentarios), Assets/_Proyecto/Tests/EditMode/Core/,
herramientas/verificar.sh, docs/herramientas-verificacion.md
No tocar: el resto de Core/, Economia/ (la implementación es de T-144), ProjectSettings/, Packages/, todo lo de Alan.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa (salvo los 9 fallos conocidos de Alan en Arena, listados por nombre)
- [ ] ContratosApuestasPruebas pasa (EditMode) y ReglasEnsambladosPruebas sigue en verde
- [ ] `bash herramientas/verificar.sh playmode-por-area` imprime una línea por cada ensamblado de PlayMode y un total, y deja un XML por área en Logs/
- [ ] Con `LIMITE_AREA=20 bash herramientas/verificar.sh playmode-por-area`, al menos un área sale "LIMITE", la corrida sigue con la siguiente y
      al terminar no queda ningún Unity.exe de editor vivo lanzado por el script (comprobado con `Get-Process Unity`)
- [ ] Grep: `IApuestasPelea.cs` no referencia nada fuera de Core; ningún área de Alan cambió

## Criterios de sensación (para un humano)
- Leer los `///` de `IApuestasPelea` con la mirada de Alan: ¿basta para poner una ventanilla de apuestas en el casino sin preguntar?

## Pruebas automáticas
- EditMode: ContratosApuestasPruebas.

## Fuera de alcance
- `ServicioApuestas` y su uso (T-144, T-145). Apuestas sobre peleas de otros (no está en el diseño). El casino (Alan, A-F5).
