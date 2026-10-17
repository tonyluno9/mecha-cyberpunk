# T-162 Recorrido de las ligas (cierre del bloque 2 de T-F7)
Dueño: Tony | Rama: tony/T-162-recorrido-ligas | Modelo: Sonnet 5.5, medium
Depende de: T-152 a T-161

## Objetivo
Una prueba de PlayMode recorre, con entrada simulada y en Pruebas_Economia, el camino de las ligas: retar al primer rival de la liga Sótano
desde la cartelera, apostar con el momio de la liga, ganar, perder y volver a retar, tomar una revancha, vencer al jefe en su furia, subir a la
liga Calle, guardar y cargar el progreso y, con atajos, vencer al campeón de la última liga. Si pasa, el bloque "ligas" está cerrado del lado de
Tony y la prueba de cierre de T-F7 ("vencer a los campeones de las 4 ligas", H7) tiene su base automática.

## Contexto
- Diseño: personaje-progresion.md (4 ligas de 5 rivales; campeón con pelea de jefe; subir de liga; perder no es game over), economia-misiones.md.
- Decisiones: D-tony-92 a D-tony-103. Tareas: T-152 a T-161.
- Ejemplos: `Tests/PlayMode/Progresion/RecorridoEconomiaPruebas.cs` (T-151) y `RecorridoPrologoPruebas.cs` (T-135): estructura, `MedidorFrames`
  (D-tony-60), `ServicioGuardado.RutaForzada`, atajos permitidos, registro de errores, helper de mapas de acciones (T-140).

## Qué hacer
1. `Tests/PlayMode/Progresion/RecorridoLigasPruebas.cs` en Pruebas_Economia, de noche y en `Libre`, con entrada simulada (caminar, E, flechas,
   Enter, Esc, ratón para pelear, G si conviene). Atajos permitidos, como en T-151 y anotados en la bitácora: `Colocar` cerca del mostrador o del
   banco; `RecibirDanio` al rival solo después de daño real al jugador; dinero inicial por código; `ServicioLigas.Poner` solo donde se indica.
   Números calculados desde los assets (calendario, apuestas, `ReglaReparacion`), nunca escritos a mano. Pasos, comprobados uno por uno:
   a) inicio: liga 1, siguiente 1, 0 campeones; dinero inicial fijado por código;
   b) cartelera: elige TORNILLO, apuesta el monto mayor de `DatosApuestaPelea_Liga1` y gana: + premio + apuesta × (1 + momio); siguiente 2;
   c) banco: repara todo (cobra lo de la regla);
   d) cartelera: elige LA RATA y pierde (sin apostar): el progreso no cambia; repara y vuelve a retarla y gana: siguiente 3;
   e) cartelera: revancha contra TORNILLO: paga la mitad de su premio y el progreso sigue en 3;
   f) `Poner(liga 1, siguiente 5)`: la cartelera muestra al jefe; pelea contra DON FIERRO: "PELEA DE JEFE" en la arena, `FuriaIniciada` al bajar
      de la mitad, gana; al bajar a pie el HUD dice "¡CAMPEÓN DE LA LIGA SÓTANO!" y "SE ABRE LA LIGA CALLE"; liga 2, siguiente 1, 1 campeón;
   g) F5 guarda; `Reiniciar` el servicio de ligas y cambiar dinero por código; F9 carga: liga 2, siguiente 1, 1 campeón, dinero guardado; no
      aparece otra vez el aviso de liga;
   h) `Poner(liga 4, siguiente 5, 3 campeones)`: pelea contra EL CACIQUE (con atajo de daño) y gana: 4 campeones, "¡CAMPEÓN DEL CLANDESTINO!", la
      cartelera ya no tiene "siguiente" ni "bloqueada" y ofrece las 5 revanchas de la liga 4;
   i) consola sin errores ni excepciones; `MedidorFrames` sin tirones fuera de su tolerancia; la partida real del humano no se toca.
2. Correr la suite completa: `bash herramientas/verificar.sh todo` y `bash herramientas/verificar.sh playmode-por-area`; anotar el resultado por
   área en docs/revision-tony.md.
3. docs/revision-tony.md: sección "Ligas (T-F7, bloque 2) cerradas: qué probar jugando" con la lista para un humano (los criterios de sensación
   de T-156 a T-161 juntos, en orden de juego), la tabla de rivales (T-157) y de premios y momios (T-158), las preguntas abiertas para Tony
   (D-tony-103) y los pedidos opcionales a Alan (D-tony-102).

## Carpetas permitidas
Assets/_Proyecto/Tests/PlayMode/Progresion/, docs/capturas/, docs/revision-tony.md (sección de esta tarea)
No tocar: código del juego (si el recorrido encuentra un error, se anota en la bitácora y se pide una tarea de corrección; no se arregla aquí),
Core/, escenas, todo lo de Alan.

## Criterios de aceptación (los comprueba el verificador)
- [ ] RecorridoLigasPruebas pasa 3 corridas seguidas; la bitácora anota el tiempo de juego de cada paso (a a i)
- [ ] El dinero final y el progreso final coinciden con los calculados desde los assets (la prueba los compara y los imprime)
- [ ] `bash herramientas/verificar.sh todo` pasa en una sola llamada (salvo los 9 fallos conocidos de Alan en Arena) y `playmode-por-area` no
      marca ningún área "LIMITE"
- [ ] Capturas docs/capturas/T-162-1.png (cartelera con el jefe), T-162-2.png (jefe en furia desde la cabina) y T-162-3.png (aviso de liga en el HUD)
- [ ] docs/revision-tony.md tiene la sección de cierre con la lista para un humano, las tablas, las preguntas y los pedidos a Alan

## Criterios de sensación (para un humano)
- Pruebas_Economia, Play, sin ayuda, unos 15 minutos: pelea la liga Sótano entera de verdad (sin L) hasta vencer a DON FIERRO. ¿Cada rival se
  siente más difícil? ¿La furia del jefe obliga a cambiar de forma de pelear? ¿El dinero alcanza para reparar entre peleas sin sobrar? ¿Subir a
  la liga Calle se siente como un logro? ¿Las revanchas a mitad de premio sirven para juntar dinero o se sienten como castigo?

## Pruebas automáticas
- PlayMode: RecorridoLigasPruebas.

## Fuera de alcance
- Integración en Ciudad.unity y en el clandestino de Alan (se escribe cuando Alan coloque `MostradorPeleas.prefab`, como T-136). Balance real de las
  ligas 3 y 4 (con el bloque de piezas). Prueba humana completa de H7 (al final de T-F7).
