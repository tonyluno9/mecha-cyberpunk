# T-084 Tráfico mínimo de fachada
Dueño: Alan | Rama: alan/T-084-trafico-fachada | Modelo: Sonnet 5.5, medium
Depende de: T-078 (carro en el barrio y su recorrido), T-077 (cierres de las calles).

## Objetivo
La ciudad se siente viva al manejar: unos pocos carros circulan por la calle principal y se detienen si algo les estorba, y la
autopista elevada del norte tiene tráfico continuo. Es fachada: no se puede subir a esos carros, no chocan entre sí y no tapan el
camino para siempre.

## Contexto
- Hoja de ruta A-F2: "tráfico mínimo de fachada". ciudad.md: solo usas tus carros; no se roban carros (los de tráfico NO son
  `IMontable`). Peatones, drones y patrullas que persiguen son de A-F5.
- Calles (CreadorBarrio): la principal en z = 0 de x = -134 a 134 (carril hacia el este en z ≈ -3.5, hacia el oeste en z ≈ 3.5);
  las norte-sur en x = ±47 cruzan la principal; la autopista elevada corre a lo largo del norte (calzada en z ≈ 99, y ≈ 10.5)
  y hoy mide lo mismo que el barrio. Propuesta D-alan-16: solo la calle principal y la autopista, sin tráfico en las norte-sur,
  para no tener bloqueos en los cruces.
- Área: Trafico/ (asmdef MechaCyberpunk.Trafico, de Alan). Un creador en un asmdef nuevo `Trafico/Editor/` (solo Editor) que
  `CreadorTodo` (Mundo/Editor) llama después de CreadorCarro: Mundo.Editor pasa a referenciar Trafico.Editor (mismo dueño;
  esta tarea lo pide). La geometría de la autopista es de CreadorBarrio (Mundo/), y aquí se alarga.

## Qué hacer
1. Lógica pura en `Trafico/`: `CalculoTrafico` (avance por una ruta de puntos con vuelta, velocidad objetivo que baja en las curvas
   y según la distancia al obstáculo de enfrente, frenando a 0 a la distancia de seguridad). Valores en `ConfigTrafico` + asset:
   crucero 9 m/s, distancia de detección 10 m, de seguridad 3 m, aceleración y frenado suaves.
2. `Trafico/CarroTrafico.cs`: `Rigidbody` cinemático (`MovePosition`/`MoveRotation`), sigue su `RutaTrafico` y cada `FixedUpdate`
   hace un `BoxCast` al frente (ignora triggers, a sí mismo y el piso) para frenar ante cualquier cosa: el carro del jugador, la
   cámara libre o el jugador a pie, otro carro de tráfico, un cierre. Greybox de 4.2 × 1.4 × 1.8 m con 3 colores y luces traseras
   que se encienden al frenar.
3. Rutas (`Trafico/RutaTrafico.cs`, puntos en un hijo): una vuelta cerrada por la calle principal (hacia el este por su carril,
   vuelta en U frente al retén del este, hacia el oeste, vuelta en U frente a la obra del oeste) con 4 carros repartidos; y dos
   carriles de la autopista con 4 carros cada uno, sin `BoxCast` (no hay nada que los estorbe), que al llegar al final de su
   carril reaparecen al principio. La calzada de la autopista se alarga a ±400 m en CreadorBarrio para que la reaparición quede
   lejos, entre la niebla.
4. Creador `Trafico/Editor/CreadorTrafico.cs`: `Trafico/Prefabs/TraficoBarrio.prefab` (rutas y carros) instanciado en Ciudad.unity
   y Pruebas_Alan.unity. `CreadorTodo` lo llama después de CreadorCarro.
5. El piloto de prueba de RecorridoCarroPruebas (T-078) ya frena ante obstáculos; esa prueba debe seguir pasando con el tráfico
   prendido.

## Carpetas permitidas
Assets/_Proyecto/Trafico/, Assets/_Proyecto/Mundo/Editor/, Assets/_Proyecto/Escenas/Ciudad.unity,
Assets/_Proyecto/Escenas/Pruebas_Alan.unity, Assets/_Proyecto/Tests/EditMode/Trafico/, Assets/_Proyecto/Tests/PlayMode/Trafico/
y Assets/_Proyecto/Tests/PlayMode/Vehiculos/ (solo si RecorridoCarroPruebas necesita esperar al tráfico)
No tocar: Core/, Vehiculos/ (salvo lo dicho), Clandestino.unity, las carpetas de Tony.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa (incluidas RecorridoCarroPruebas y FinalesCarroPruebas)
- [ ] EditMode `CalculoTraficoPruebas`: sin obstáculo llega a crucero; con un obstáculo a la distancia de seguridad la velocidad
      objetivo es 0; baja en curvas cerradas; el avance por la ruta da la vuelta al llegar al final
- [ ] PlayMode `TraficoPruebas` en Pruebas_Alan con `Time.timeScale` 4 durante 45 s (3 min de juego): cada carro de la calle recorre
      300 m o más; en ningún momento dos carros de tráfico se traslapan (bounds); ninguno sale del asfalto de la calle principal
- [ ] PlayMode: el CarroViejo detenido en el carril hacia el este hace que el carro de tráfico que viene se detenga a 1.5 m o más de
      él; al quitarlo, sigue en menos de 3 s. Lo mismo con la cámara libre parada en el carril
- [ ] PlayMode: los carros de la autopista recorren su carril y reaparecen; ninguno reaparece a menos de 150 m de la cámara libre
- [ ] PlayMode: ningún carro de tráfico es `IMontable` ni `IJugador`
- [ ] Capturas en tercera persona manejando por la calle principal con tráfico de noche (docs/capturas/T-084-1.png) y la autopista
      vista desde la calle norte-sur (T-084-2.png)
- [ ] Consola sin errores en 3 minutos

## Criterios de sensación (para un humano)
- La calle principal se siente con vida pero se puede manejar sin estorbos; los carros frenan de forma natural.
- La autopista de fondo hace que el barrio se sienta parte de una megaciudad.

## Pruebas automáticas
- EditMode (Tests/EditMode/Trafico/): CalculoTraficoPruebas.
- PlayMode (Tests/PlayMode/Trafico/): TraficoPruebas.

## Fuera de alcance
- Tráfico en las calles norte-sur, semáforos, cláxones, choques con daño, peatones (A-F5)
- Patrullas y drones que persiguen con calor (A-F5)
