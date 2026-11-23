# Reporte 23-nov noche 2 (sesión B): T80 a T86

Las 7 tareas quedaron HECHAS, una rama por tarea, con merge --no-ff a main y push.
- Builds hechos uno por uno con Unity cerrado, los dos con "Build Finished, Result: Success":
  - ConstruirDemoPandillas → Builds/Demo/CyberCity.exe.
  - ConstruirJuego → Builds/Juego/CyberCity.exe. El primer intento falló porque un archivo de compilación seguía bloqueado
    después del build de la demo; el reintento salió bien.
- Tus partidas (partida_auto.json, partida_1.json, opciones.json) se respaldaron antes de probar y se restauraron al final; los md5 son
  idénticos. El respaldo sigue en Desktop/videojuego/respaldo_partidas_23nov_noche2.

| Tarea | Estado | Rama | Capturas |
|---|---|---|---|
| T81 Cámara lenta después del menú de Tab | HECHA | tony/T-1730-camara-lenta | docs/capturas/_tareas/T81/ |
| T80 Aviso de MANTÉN TAB al inicio | HECHA | tony/T-1731-aviso-tab | docs/capturas/_tareas/T80/ |
| T82 SKULLS y NPCs con la velocidad de los clips del jugador | HECHA | tony/T-1732-npcs-velocidad | docs/capturas/_tareas/T82/ (lista-npcs.md) |
| T84 Tienda de Tino guiada y SALIR visible | HECHA | tony/T-1733-tino-guiado | docs/capturas/_tareas/T84/ |
| T83 Carros camino al Doc | HECHA | tony/T-1734-carros-al-doc | docs/capturas/_tareas/T83/ |
| T86 Sin números de daño | HECHA | tony/T-1735-campo-holo (junto con T85) | docs/capturas/_tareas/T86/ |
| T85 Campo de tiro holográfico | HECHA | tony/T-1735-campo-holo | docs/capturas/_tareas/T85/ |

## T81: cámara lenta (la más importante)
Causa: la rueda de Tab ponía el tiempo en 0.25 y guardaba "la escala de antes". Si salía un aviso con la rueda abierta, el aviso guardaba ese
0.25 y al cerrarse lo volvía a poner, así que el juego se quedaba lento para siempre.

Ahora hay un solo control del tiempo (Core/Juego/ControlTiempo). Cada pantalla pide su velocidad (rueda de prótesis, rueda de armas, avisos,
pausa) y al soltarlas todas vuelve a 1. También se limpia al cambiar de escena, al empezar Nueva partida y al Continuar.

Comprobado en Play con 10 vueltas por todos los caminos que se pueden simular:
- soltar Tab;
- activar piernas y cerrar;
- aviso encima de la rueda y luego ENTENDIDO;
- ENTENDIDO con la rueda abierta.

Con la rueda abierta el tiempo va a 0.25 y fixedDeltaTime a 0.005; después siempre vuelve a 1 y 0.02. En la prueba final se repitió 5 veces más
y otra en el mundo libre. Hay 8 pruebas EditMode de ControlTiempo.

## T80: aviso de TAB al inicio
Causa: lo que guarda la primera visita al Doc no se borraba al empezar otra partida sin cerrar el juego. Además, el tutorial no revisaba si ya
tenías prótesis.

Ahora se borra antes del paso del Doc y el tutorial solo corre si tienes piernas o brazos instalados. En una partida nueva no sale nada durante
los SKULLS, las cinemáticas ni el camino al Doc; el aviso sale justo al instalar las piernas.

## T82: velocidades de los NPCs
Los SKULLS ya usaban los clips del jugador, pero el script de la historia los movía a 2.2 m/s al caminar y a 5.5 o 6.2 al correr. Ahora caminan
a la velocidad de PJ01_Caminar (1.62) y corren a la de PJ01_Trote (4.53), multiplicadas por la escala de su modelo (1.18 y 1.19).
- Medido: 1.92 m/s caminando y 5.41 corriendo, que es lo que recorre el clip a esa escala.
- La lista de cada NPC con su clip y velocidad, antes y después, está en docs/capturas/_tareas/T82/lista-npcs.md.
- Corregí:
  - pandilleros, policía y Nova: 4.53 al correr;
  - caminar en las confrontaciones: 1.62;
  - peatones: 1.62, con la huida al doble y poco más (×2.8);
  - acoso: 4.53.
- Robots y perro se quedaron como estaban.
- Cambié velocidades en Trafico/, que es carpeta de Alan: ConfigPeatones (1.4 → 1.62) y NpcSentado. Revísalo con él.

## T84: tienda de Tino guiada
- En la primera visita todo queda oscuro y bloqueado menos el control del paso, que lleva un borde amarillo que pulsa y la etiqueta
  "PASO n DE 5":
  1. elegir la pistola;
  2. COMPRAR;
  3. MEJORAR;
  4. instalar la mejora;
  5. CAMPO DE TIRO ("prueba tu arma") o SALIR.
- Solo son obligatorias la pistola y una mejora. Esc y SALIR no cierran el banco antes del paso 5.
- SALIR del banco ahora está arriba y siempre se ve; antes, en la pestaña MEJORAR lo tapaba el botón CAMPO DE TIRO.
- Arreglado al probar: el oscurecido no tapaba la pantalla de compra porque esa pantalla se dibujaba encima. Ahora la guía va encima de todo.

## T83: carros camino al Doc
Desde que decides quedarte (el dinero) ya puedes subirte a un carro y manejar; lo comprobé en Play. Durante el empujón y las cinemáticas
siguen bloqueados. La policía, las pandillas y NOVA siguen sin reaccionar hasta el mundo libre.

## T86: sin números de daño
Los únicos números de daño del juego eran los del campo de tiro; los quité. Los objetivos ahora llevan barra de vida y en la ciudad siguen solo
las barras de T43. El marcador de precisión del campo se queda.

## T85: campo de tiro holográfico
- Cubo oscuro con rejilla cian en piso, paredes y techo y niebla oscura; la niebla de la ciudad se restaura al salir.
- Una sola línea de costales CB02 pegados, con material holográfico, que frena al jugador (se detiene a 4.4 m).
- Objetivos holográficos en idle, ya no en pose T, con destello al aparecer.
- Terminal holográfica con "PRESIONA E PARA CONFIGURAR", que aparece a menos de 2.5 m.
- Perros sobre una plataforma de 0.9 m para que los costales no los tapen.
- Arreglado al probar: los costales aparecían en la ciudad (a nivel del suelo) en vez de en la sala.

## Pruebas
- EditMode por clase: 251 pruebas en total; 1 falla, la de Peaton.prefab (prefab de Alan), que no se tocó y ya fallaba antes.
- Prueba final en Play, partida nueva de principio a fin hasta "YA ERES LIBRE":
  - sin aviso de Tab al inicio;
  - carros libres camino al Doc;
  - prótesis gratis y aviso de Tab al instalarla;
  - Tab 5 veces y una más en el mundo libre, siempre de regreso a velocidad 1;
  - Tino guiado de los pasos 2 a 5;
  - campo de tiro y regreso;
  - la chica y el mundo libre.

  Capturas en docs/capturas/_tareas/FINAL2/.

## Qué debe probar Tony a mano
El editor no recibe teclado ni ratón simulados, así que estas acciones las hice llamando al mismo código del juego:
1. **Tab con teclado y ratón**: mantener Tab, elegir PIERNAS con clic y también soltando Tab, correr con Shift, y abrir y cerrar varias veces.
   El juego nunca debe quedarse lento.
2. **Tienda de Tino**: hacer clic en algo que no sea el paso resaltado (no debe responder) y usar flechas, Enter y Esc durante la guía.
3. **Campo de tiro**: acercarte a la terminal, pulsar E, disparar con el ratón y saltar contra los costales.
4. **SKULLS al irse corriendo**: en Play, la huida queda detrás de la cinemática del maletín. Mira si se ven bien en el juego.
5. **Partida nueva completa en el build** (Builds/Juego/CyberCity.exe).

## Detalles
- En el campo de tiro, la terminal queda grande a la derecha del jugador y tapa un poco el arma en el HUD.
- La línea de DirectorRecorrido de T80 entró en el commit de T84, porque los dos tocaban ese archivo.
