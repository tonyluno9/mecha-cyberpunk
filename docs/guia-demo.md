# Guía de la demo (15 minutos)

La demo es el prólogo completo: del título a "continuará", en la ciudad de Alan. Unos 11 a 15 minutos para alguien que nunca la ha jugado.

## Cómo abrirla
- `Builds/Demo/MechaCyberpunk.exe` (en la raíz del repo; `Builds/` no está en git). Doble clic. Arranca en pantalla completa; Alt+Enter pasa a ventana.
- Se regenera con `ConstruirDemo.Construir` (ver docs/herramientas-verificacion.md, "Build de la demo").
- Escenas, en este orden: CreacionPersonaje (título), Ciudad_Prologo (la ciudad y el clandestino de Alan), Flashback (la casa de la infancia).
- Las ayudas de controles salen solas en pantalla según lo que hagas. Esc suelta el cursor.

## El recorrido (lo que verá la persona)
| # | Qué pasa | Teclas | Minutos aprox. |
| --- | --- | --- | --- |
| 1 | Título: NUEVA PARTIDA y crear el personaje (nombre, apariencia) | Ratón, flechas y Enter | 1 |
| 2 | Pelea del campeonato en el clandestino | Clic izq. golpe, clic der. bloquear (justo antes del golpe = bloqueo perfecto), WASD | 2 |
| 3 | Salir a pie al carro ("Ve a tu carro") y subir | WASD, ratón, E | 1 |
| 4 | Manejar a casa ("Maneja a casa"): la pandilla sale al paso en el callejón | WASD o flechas, Espacio freno, E bajar (detenido) | 1 |
| 5 | Bajar, ir a la zona de llamado y llamar al mecha | M | 0.5 |
| 6 | Subir al mecha, encenderlo (F, mantener Espacio, Q) y pelear con la pandilla | E, F, Espacio, Q, clic izq./der. | 2 a 3 |
| 7 | Bajar del mecha (E, quieto), devolverlo (M), regresar a casa en el carro y entrar por la puerta | E, M, WASD | 1.5 |
| 8 | Dormir en la cama y ver el flashback | E | 1.5 |
| 9 | Despertar a las 20:00 con el mecha al 45 % y $500; reparar en el banco de la casa | E | 1 |
| 10 | Caminar al clandestino (unos 140 m), hablar con el encargado y ganar la primera pelea casera | WASD, E, combate | 3 |
| 11 | "Continuará", vuelta al título, CONTINUAR: juego libre | Enter | 0.5 |

## Qué observar
1. ¿Se entiende cada objetivo sin que nadie explique nada? (Arriba sale el texto del objetivo y una flecha.)
2. Al salir del clandestino, ¿la persona sabe que tiene que manejar y se encuentra con la pandilla? (D-tony-187.)
3. El encendido del mecha (F, Espacio, Q): ¿lo saca solo con las ayudas de la pantalla?
4. Las peleas: ¿se sienten pesadas y justas? ¿Usa el bloqueo?
5. ¿Hay tirones al cambiar de escena (título a ciudad, cama a flashback y de vuelta)?
6. ¿Cuánto tardó de principio a fin? Anótalo: la meta es 10 a 20 minutos.

## Lo conocido
- La prueba automática (PrologoCiudadPruebas) recorre todo esto con entrada simulada en unos 190 s de juego; una persona tarda más por leer, dudar y pelear de verdad.
- La partida se guarda en `%USERPROFILE%/AppData/LocalLow/DefaultCompany/Mecha Cyberpunk (demo)/` (cambió con el nombre del producto; las partidas de los builds provisionales no se ven aquí).
