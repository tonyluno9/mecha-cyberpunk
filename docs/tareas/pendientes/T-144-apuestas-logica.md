# T-144 Apuestas: lógica, datos y resolución en el flujo de pelea
Dueño: Tony | Rama: tony/T-144-apuestas-logica | Modelo: Sonnet 5.5, medium
Depende de: T-141, T-142

## Objetivo
El jugador puede apostar dinero a que gana su próxima pelea: se cobra al apostar, se paga según el momio si gana y se pierde si pierde
(junto con el premio). Si apuesta lo suficiente, además se lleva la pieza que el rival puso en juego, que va a su inventario para venderla.
Todo funciona sin pantalla (la pantalla del mostrador es T-145), resuelto por `DirectorPelea` al terminar la pelea.

## Contexto
- Diseño: economia-misiones.md ("Apuestas sobre ti y pierdes: pierdes la apuesta y el premio"; "piezas ganadas en peleas por apuesta"),
  personaje-progresion.md ("Perder una pelea no es game over: pierdes lo apostado").
- Contrato: `Core/Economia/IApuestasPelea.cs` (T-141): léelo primero, sus `///` mandan.
- Decisiones: D-039, D-tony-48 (el premio lo paga `DirectorPelea` una vez por pelea; Pelea referencia Economia), D-tony-65 y D-tony-66 (la
  casera con `EntradaArena`, sin revancha). Propuestas nuevas: D-tony-80 (reglas) y D-tony-86 (valores).
- Ejemplos: `Economia/Cartera/ServicioCartera.cs`, `Economia/Premios/DatosPremioPelea.cs`, `Pelea/Flujo/DirectorPelea.cs` (`AlResultado`),
  `Tests/PlayMode/Pelea/PremioPeleaPruebas.cs` (pelea con ArenaDePrueba y cartera instanciada).

## Qué hacer
1. `Economia/Apuestas/DatosApuestaPelea.cs` (ScriptableObject): `momio` (> 0), `apuestaMaxima`, `montos` (lista de montos que ofrece la
   pantalla, en orden), `piezaEnJuego` (`DatosObjeto`, opcional) y `apuestaMinimaPorPieza`. Asset `DatosApuestaPelea_Casera` (momio 1.0,
   máximo $500, montos 100, 250, 500, pieza Brazo de chatarra desde $250) con su creador. Valores de D-tony-86.
2. `Economia/Apuestas/ModeloApuestas.cs` (C# puro, sin Unity): `Anunciar(alias, datos)`, `PuedeApostar`, `Apostar(monto, ICartera)`,
   `Cancelar(ICartera)`, `Cerrar()` (empezó la pelea: ya no se apuesta ni se cancela), `Resolver(bool gano, ICartera, inventario)` y
   `Devolver(ICartera)` (la pelea no terminó). Reglas (D-tony-80):
   - una apuesta por pelea; se cobra con `IntentarPagar(monto, Apuesta)`; con dinero insuficiente o negativo no se apuesta;
   - gana: `Abonar(apostado + redondeo(apostado × momio), Apuesta)`; si apostó al menos `apuestaMinimaPorPieza`, recibe `piezaEnJuego`
     en el inventario; si no cabe, se le paga su `valorVenta` con motivo `Venta`;
   - pierde: no se devuelve nada; `Resuelta` avisa el negativo;
   - después de `Resolver` o `Devolver`, nada queda abierto hasta el siguiente `Anunciar`.
3. `Economia/Apuestas/ServicioApuestas.cs`: implementa `IApuestasPelea` sobre el modelo, se registra en `Registro<IApuestasPelea>`, busca la
   cartera y el inventario por registro, y expone a Tony (no al contrato) `Anunciar`, `Cerrar`, `Resolver` y `Devolver`. Va en
   `ServiciosEconomia.prefab` (con el creador). No se guarda (D-tony-80).
4. `DirectorPelea`: campo opcional `DatosApuestaPelea apuesta`. `AnunciarApuesta()` público: si tiene datos, anuncia la pelea al
   `ServicioApuestas` con el alias del rival (del `DatosRival` de su prefab o del rival asignado). Al empezar el conteo cierra la apuesta;
   en `AlResultado` la resuelve (después de pagar el premio) y avisa `ApuestaResuelta(int neto)`; si el director se destruye o se `Reinicia`
   con la apuesta cerrada sin resultado, la devuelve. Sin datos de apuesta o sin servicio, todo sigue como hoy (sin warning repetido).
5. `FlujoPeleaCasera.prefab`: asigna `DatosApuestaPelea_Casera` con su creador. `FlujoPelea.prefab` (el que usa Alan) no cambia.
6. EditMode `Tests/EditMode/Economia/ModeloApuestasPruebas.cs` y PlayMode `Tests/PlayMode/Pelea/ApuestaPeleaPruebas.cs` (como
   PremioPeleaPruebas: ArenaDePrueba, `ServiciosEconomia.prefab` instanciado y el flujo casero).

## Carpetas permitidas
Assets/_Proyecto/Economia/ (Apuestas/, Datos/, Editor/, Prefabs/ solo con el creador), Assets/_Proyecto/Pelea/ (Flujo/, Editor/, Prefabs/ solo
con el creador), Assets/_Proyecto/Tests/EditMode/Economia/, Assets/_Proyecto/Tests/PlayMode/Pelea/, Assets/_Proyecto/Tests/PlayMode/Economia/
No tocar: Core/, `FlujoPelea.prefab`, UI/, Progresion/, escenas, todo lo de Alan.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa (salvo los 9 fallos conocidos de Alan en Arena)
- [ ] ModeloApuestasPruebas: sin anunciar no se apuesta; dos apuestas en la misma pelea, monto 0, monto mayor al máximo o sin dinero fallan sin
      cambiar nada; tras `Cerrar` no se cancela; `Resolver` ganando con $250 y momio 1.0 abona $500 y entrega la pieza; con $100 no hay pieza;
      inventario lleno paga el valor de venta con motivo Venta; perdiendo `Resuelta` avisa -250
- [ ] ApuestaPeleaPruebas (PlayMode, con daño real y `RecibirDanio` permitido solo para rematar al rival): con $500 apuesta $250 y gana la
      casera: dinero final $500 − 250 + 500 + 600 = $1,350 y 1 Brazo de chatarra en el inventario; apuesta $250 y pierde: dinero $250 e inventario
      igual; cancelar antes del conteo devuelve el dinero; apostar después de "¡PELEA!" falla; destruir el flujo a media pelea devuelve la apuesta
- [ ] Sin `ServicioApuestas` en escena, PremioPeleaPruebas y FlujoPeleaPruebas siguen en verde y la consola no tiene errores

## Criterios de sensación (para un humano)
- Ninguno todavía (la pantalla es T-145).

## Pruebas automáticas
- EditMode: ModeloApuestasPruebas. PlayMode: ApuestaPeleaPruebas.

## Fuera de alcance
- Pantalla de apuesta y mostrador (T-145). Momios por liga y rival (bloque de ligas). Apuestas del casino (Alan, A-F5). Guardar una apuesta abierta.
