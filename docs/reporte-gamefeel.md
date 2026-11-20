# Reporte: cómo se siente jugar (19-nov-2026, noche)

Pedido de Tony: caminar, correr, disparar, golpear y manejar sin fallas. Todo está en `main` (merge --no-ff de `integ/gamefeel`).
Detalle técnico de cada punto en `docs/tareas/hechas/T-1484` a `T-1489`. Builds al final.

## Qué cambió y qué probar

### 1. Caminar y correr (T-1484)
Medí los clips: el de caminar de PJ01 avanza 1.66 m/s y el de correr 4.25 m/s, pero el jugador va a 4.5 m/s normal y 7.5 m/s con Shift,
con la animación siempre a velocidad 1. Por eso los pies patinaban (2.7 veces al caminar).
- El paso normal ahora es un **trote** (el clip de correr a 1.06x): a 4.5 m/s nadie camina. Con Shift el mismo clip va a 1.6x.
  Los NPC siguen caminando con su clip.
- La rapidez de la animación sigue a la velocidad real, también de lado y hacia atrás (entre 0.7x y 1.6x).
- Pies en escalones, banquetas y rampas: el IK solo corrige el desnivel; en piso plano el pie hace exactamente lo del clip.
- El cuerpo gira suave hacia donde caminas (antes giraba de golpe) y se inclina un poco hacia dentro de la curva al dar vuelta corriendo.
- Apuntando el giro sigue a la cámara sin retraso, como antes.

**Prueba:** camina y corre en línea recta mirando los pies (no deben deslizarse); haz zigzag corriendo (inclinación leve);
sube una banqueta o escalera; apunta y camina de lado (aquí todavía patina un poco: falta un trote lateral, ver abajo).
Si prefieres un caminar de verdad, se baja `velocidadCaminar` en `DatosPersonaje_Base` a unos 2 m/s y se quita `ClipsSoloJugadorT1484`.

### 2. Cámara a pie (T-1485)
Balanceo leve al moverse (más al correr), FOV +7° al correr, transición suave al apuntar, sin saltos al despegarse de una pared,
y un temblor común (Cinemachine Impulse) que usan disparos, golpes y daño.
**Prueba:** corre (balanceo y FOV), quédate quieto (nada se mueve), apunta y suelta, camina pegado a una pared.

### 3. Disparar (T-1486)
Cada una de las 10 armas tiene su patada de cámara, su retroceso del arma, su temblor, sus casquillos dorados y su dispersión que crece
en ráfaga (la mitad apuntando). La bala ya no sale hacia atrás si el blanco está pegado al cañón. Al matar sale una cruz roja con otro sonido.
**Prueba:** dispara cada arma y compara la patada; ráfaga larga con fusil o subfusil (se abre y se cierra al soltar); pega y mata a un enemigo.

### 4. Golpear (T-1487)
Combo de 3 golpes con buffer de clic (un clic durante la recuperación ya no se pierde), el tercero más fuerte; asistencia al enemigo más
cercano (gira y se acerca hasta 1.2 m sin atravesarlo); congelamiento de 50 ms (80 el final), temblor y empuje del golpeado.
Esta noche arreglé el empuje: con muchos FPS casi no movía al enemigo (el CharacterController ignoraba los pasos de menos de 1 mm).
**Prueba:** tres clics rápidos a un pandillero; espera 0.7 s y empieza otra vez; pega cerca de una pared (no lo empuja a través).

### 5. Manejar (T-1488)
Un perfil por tipo (compacto, sedán, camioneta, deportivo, pesado, moto) ya asignado a los 30 carros: peso, aceleración, frenos.
La nariz baja al frenar y la carrocería rebota y se asienta, se inclina en curva; derrape con freno de mano que se endereza solo;
cámara que se aleja y abre el FOV con la velocidad; temblor al chocar según la fuerza; motor con marchas (el tono sube y cae al cambiar);
subir, bajar y robar se acomodan a la puerta antes de animar; motos estables paradas y sin saltito al arrancar.
**Prueba:** compacto contra camión (V16 a V18); frenar a fondo a 60 km/h; freno de mano en curva; chocar un muro despacio y rápido;
escuchar los cambios de marcha; robar un carro con conductor; moto parada y arrancando.

### 6. Detalles (T-1489)
Polvo al caer de más de 1 m (más polvo entre más alto), gotas y sonido mojado al pisar charcos, temblor al recibir daño, y una viñeta roja
que late con menos de 30 % de vida. El indicador de dirección del daño ya apuntaba bien; tiene pruebas nuevas.
**Prueba:** brinca de un techo; corre por los charcos; deja que te disparen hasta bajar de 30 % y cúrate.

## Pruebas (por clase, sin PlayMode completo)
- EditMode: Vehiculos 359/359; Personaje + UI 923/923; Personaje + PersonajeVisual 596 (con las nuevas `LocomocionT1484Pruebas`).
- PlayMode: PesoCarroT1488 2/2, SensacionGolpeT1487 3/3, locomoción del jugador (JugadorVisual, ClipsTonyT724, AnimacionDireccionT640,
  VerificacionPJ01T741, AnimatorPJ01T733, VelocidadesAnimacionT652) en verde, tanda del jugador 29/30, vehículos 71/71, HUD 34/34.
- Pruebas viejas ajustadas porque el diseño cambió a propósito: FOV y balanceo leve al correr, combo con buffer, el trote en Caminar,
  los nombres PJ01 de los clips de lado y atrás (esa prueba ya fallaba en la base).
- Siguen fallando, igual que antes de esta noche: `ControladorAPiePruebas.APie_EspacioEnElPiso_SubeLosPiesEntre09Y13Metros`
  (el salto mide 0.45 m porque `alturaSalto` vale 0.45) y `CanonAlPuntoDeMiraT681Pruebas.CuatroArmas` (4.3° contra 4°; en la base 4.43°).
- No revisé capturas ni video del juego en movimiento: lo que se ve (pies, inclinación, casquillos) lo tiene que mirar Tony en el build.

## Qué falta
- Trote de lado y hacia atrás, y un sprint propio: sin ellos, apuntando de lado y con Shift los pies todavía patinan un poco.
- Arranque y frenada a pie: no cambié la aceleración (30 m/s²) ni el frenado (40 m/s²); se sienten rápidos y precisos. Si los quieres más
  pesados, se bajan en `ConfigControlesAPie_Base`.
- Retroceso del arma solo posicional (sin levantar el cañón, porque depende del IK de la mano).
- Las partículas de polvo, gotas y casquillos usan el material por defecto: pueden verse cuadradas (pendiente de arte).
- Sonidos propios del hitmarker (`IMP_HITMARKER`, `IMP_HITMARKER_MUERTE`) y del chapoteo: hoy reusan otros.
- Con el cabeceo nuevo, los carros de la IA y del tráfico también cabecean (misma física): mirar que la policía no se vea rara.

## Animaciones para pedir a Blender (rig PJ01, en bucle, sin root motion)
Lista completa con nombres de archivo en `docs/animaciones-faltan.md` (secciones del 19-nov). Las más importantes:
1. `PJ01_Loco_Sprint` a 7-7.5 m/s (el Shift).
2. `PJ01_Loco_TroteDer`, `PJ01_Loco_TroteIzq` y `PJ01_Loco_TroteAtras` a 4-4.5 m/s (moverse apuntando).
3. `PJ01_Disparo_Largas` (retroceso de hombro con escopeta y sniper) y `PJ01_Recargar_Largas`.
4. `Punos_GolpeFinal` (patada o gancho para el tercer golpe) y `Dano_EmpujadoAtras` (enemigo que sale volando).
5. `PJ01_Carro_Choque` (sacudida al chocar), `PJ01_Carro_Derrape_Bucle` (contravolante) y `PJ01_Moto_ApoyarPie` (pie al piso al parar).
6. Opcionales: arranque y frenada del trote, subir y bajar escalones caminando.

## Builds
Hechos uno por uno desde `.sesiones/s2` con el código de main a5f74de, sin errores, 2584 MB cada uno:
- Demo (DEMO_FINAL, sin teclas de prueba): `.sesiones/s2/Builds/Demo/CyberCity.exe` (21:28).
- Juego: `.sesiones/s2/Builds/Juego/CyberCity.exe` (21:30).
Los de `Builds/` en la carpeta principal son viejos (8 y 9 de noviembre). Unity quedó cerrado.
