# Reporte 23-nov tarde (sesión B): T77, T78, T79

Todo está en main (último merge 09393ca) y subido. Builds hechos uno por uno con Unity cerrado:
`ConstruirDemo.ConstruirDemoPandillas` → Builds/Demo/CyberCity.exe y `ConstruirDemo.ConstruirJuego` → Builds/Juego/CyberCity.exe
(los dos con "Build Finished, Result: Success").

| Tarea | Estado | Rama | Capturas |
|---|---|---|---|
| T77 Solo 2 SKULLS al inicio | HECHA | tony/T-1720-skulls-dos | docs/capturas/_tareas/T77/ |
| T78 Inicio guiado con candados | HECHA | tony/T-1721-candados-inicio | docs/capturas/_tareas/T78/ |
| T79 Campo de tiro en la tienda de Tino | HECHA | tony/T-1722-campo-tiro | docs/capturas/_tareas/T79/ |
| Prueba final + arreglo del dinero al Continuar | HECHA | tony/T-1723-final-23nov-tarde | docs/capturas/_tareas/FINAL/ |

## T77: solo 2 SKULLS
La causa estaba en la reubicación de pandilleros "atorados" (T66): los SKULLS que acosan contaban como atorados y los de la ciudad
aparecían junto al jugador, cayendo. Ahora el que acosa no se reubica y la escena no se arma dos veces. Medido: 2 SKULLS en 3 partidas
nuevas y 1 Continuar, sin caídas. En la prueba final volvieron a salir 2 (FINAL/01_skulls_inicio.png).

## T78: inicio guiado con candados
- Estado de la historia guardado en la partida y un **GestorCandados** que, según el paso, pone los candados del mundo (Core/CandadosMundo):
  policía y calor, pandillas hostiles, NOVA, robo de carros y tiendas (solo abre la del paso: el Doc o Tino).
- Paso nuevo **USA TU PRÓTESIS** entre el Doc y Tino.
- Comprobado en Play con una partida nueva:
  1. SKULLS y cinemáticas: todo bloqueado.
  2. VE CON EL DOC: si te alejas sale el aviso suave "VE CON EL DOC" (sin paredes) y un carro ajeno dice **AÚN NO** en vez de "Subir al carro".
  3. En la clínica solo se ofrecen las **PIERNAS GRATIS**; lo demás dice AÚN NO DISPONIBLE, no puedes salir sin instalarlas y el dinero no baja.
  4. Al salir salen las ventanas del tutorial de la prótesis (al centro, con ENTENDIDO).
  5. En el banco de Tino no puedes salir sin comprar un arma **y** ponerle una mejora. Abajo sale "PARA SALIR: COMPRA UN ARMA" y luego
     "PARA SALIR: PONLE UNA MEJORA".
  6. Al alejarte del CIC sale la cinemática de la chica y la ventana **"YA ERES LIBRE DE RECORRER CYBER CITY"**, y todos los candados se quitan.
- Guardar y Continuar: a la mitad (en VE CON EL DOC) los candados vuelven tal cual; con el mundo libre, todo sigue libre.
- Arreglos de paso:
  - La noche fija también cubre el paso nuevo.
  - La línea de lo que falta en el banco ahora se lee: está abajo, con fondo propio.
  - Al Continuar en el paso del Doc ya no se vuelve a dar el dinero del maletín. El fallo venía de T10: daba $40,500 en vez de $20,500.

## T79: campo de tiro
- Botón **CAMPO DE TIRO** en la barra del banco de Tino. Al pulsarlo hay un fundido y apareces en una sala blanca aislada, lejos de la ciudad.
- Barrera de costales CB02 que no deja pasar: el jugador avanza 5 m y se detiene en los costales.
- Todas las armas y **munición infinita**: el cargador no baja (pistola 8|8, escopeta 4|40, subfusil 25|240). No gasta dinero, no hay
  policía ni calor, y la vida no baja.
- Objetivos quietos de NOVA: centinela E15, robot pesado E16, perro E17, élite E18 y guardia. Reciben daño, caen y reaparecen a los 4 s.
  Arriba al centro hay un marcador de daño y precisión.
- Con **E** se abre el menú de entrenamiento: arma, objetivo, cuántos, distancia, reiniciar y salir. **SALIR** te regresa a la tienda de Tino,
  donde estabas, con el banco abierto, y tu arsenal queda como era.
- Comprobado con 4 armas (pistola, escopeta, francotirador, subfusil) contra centinela, robot pesado, élite y perro.
- Mientras estás en el campo la historia no avanza ni muestra avisos. Antes, entrar al campo en el paso de Tino podía disparar la cinemática
  de la chica.
- Arreglado: al salir, la red de seguridad del jugador te regresaba a la sala, porque había guardado la sala como "punto seguro".

## Pruebas
- EditMode por clase: 36/36 de T78 (candados, gestor, noche fija) y 93/93 del conjunto final (campo de tiro, arsenal prestado, cargador,
  ayudas del HUD, banco, T78).

## Qué debe probar Tony a mano
El editor no recibe teclado ni ratón simulados. Lo que pide teclas lo probé llamando al mismo método del juego (Usar, Pulsar, clic de la UI,
disparo por código), así que esto falta con las manos:
1. **Prótesis (T78):** Tab, elegir PIERNAS y correr con SHIFT. Debe pasar al objetivo VE CON TINO. Yo salté ese paso con F8.
2. **Carro con candado:** pulsar E frente a un carro durante el inicio. Debe decir AÚN NO y no subirte.
3. **Campo de tiro:** E para el menú, apuntar y disparar con el ratón, Esc para cerrar el menú y saltar contra los costales.
4. **Partida nueva completa en el build** (Builds/Juego/CyberCity.exe).

## Detalles pendientes (no bloquean)
- Los maniquíes del campo quedan en pose T (no tienen animador).
- Los perros en distancia "cerca" quedan medio tapados por los costales.
- En el campo se ve el minimapa de la ciudad con "REGRESA · VE CON TINO".
- Con la pantalla muy ancha y baja, la línea "PARA SALIR" del banco se encima con las estadísticas. En 16:9 no pasa.

## Ojo
- El Play del editor escribió tu autoguardado real (partida_auto.json). El respaldo de antes de empezar está en
  %TEMP%\respaldo_saves\CYBERCITY_1452.
