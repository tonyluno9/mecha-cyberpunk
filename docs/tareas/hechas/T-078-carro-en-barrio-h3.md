# T-078 El carro en el barrio: de la casa al clandestino (cierre de H3, lado de Alan)
Dueño: Alan | Rama: alan/T-078-carro-en-barrio-h3 | Modelo: Sonnet 5.5, medium
Depende de: T-075 y T-077.

## Objetivo
El carro viejo está estacionado frente a la casa en Ciudad.unity y Pruebas_Alan.unity. Con la cámara libre se camina al carro, se
sube, se maneja hasta el clandestino dando al menos una vuelta, sin atravesar nada, y se baja frente a su puerta. Es la prueba de
cierre de A-F2 y del hito H3 del lado del mundo; con ella Tony puede desbloquear T-061.

## Contexto
- Hoja de ruta: A-F2 ("de casa al clandestino en carro dando al menos una vuelta, sin atravesar nada") y H3.
- Medidas del barrio (CreadorBarrio): calle principal en z = 0, calles norte-sur en x = ±47, todas de 14 m, banquetas de 3 m de
  0.2 m de alto. La casa (Manzana_4) mira al este hacia la calle x = -47; su `Entrada` queda cerca de (-55.5, 0.2, -77) y el punto
  `Casa_Cochera` (modo Carro) 12 m al norte. El clandestino está en (0, z ≈ 22) mirando al sur, con su `Entrada` cerca de
  (0, 0.2, 8.5); el bar enfrente. Ruta natural: al norte por x = -47 hasta la calle principal, vuelta a la derecha y al este hasta x = 0.
- Los carros greybox estacionados de `CrearAmbientacion` evitan los puntos de `puntosLibres`; hoy la cochera no está ahí.
- El carro se coloca con un creador de Vehiculos/Editor (asmdef de T-074). `CreadorTodo` (Mundo/Editor) tiene que llamarlo, así
  que Mundo.Editor pasa a referenciar Vehiculos.Editor (mismo dueño; esta tarea lo pide).
- Diseño: ciudad.md, "unas 6 manzanas, alrededor de 1 minuto y medio cruzándolas en carro". Propuesta D-alan-17: aquí se mide, no
  se agranda el barrio.

## Qué hacer
1. CreadorBarrio: agrega el punto de la cochera (y 8 m alrededor) a `puntosLibres` para que no aparezcan carros greybox ni botes
   ahí.
2. `CreadorCarro.ColocarEnEscenas()`: instancia `CarroViejo.prefab` en Ciudad.unity y Pruebas_Alan.unity, en la calle junto a
   `Casa_Cochera`, a 1.2 m del borde de la banqueta, mirando al norte, con el `PuntoSubida` del lado de la banqueta de la casa.
   `CreadorTodo.Crear` lo llama después de CreadorBar y antes de CreadorPeleaClandestino; actualiza su comentario. En
   Pruebas_Alan, la cámara libre empieza en la `Entrada` de la casa mirando al carro.
3. `Tests/PlayMode/Vehiculos/RecorridoCarroPruebas.cs`, en Pruebas_Alan a las 22:00 (reloj con `AvanzarHasta`):
   a) la cámara libre camina con entrada simulada de la puerta de la casa al `PuntoSubida` (W y giro con el ratón según el ángulo
      al objetivo) y sube con E;
   b) un piloto de prueba (código de la prueba) maneja siguiendo puntos de ruta: gira con A/D según el ángulo al siguiente punto,
      acelera con W, frena con S si un rayo al frente choca con algo a menos de 8 m, y se detiene frente al clandestino;
   c) detenido, E baja y la cámara libre camina hasta la `Entrada` del clandestino.
   Durante el manejo, en cada `FixedUpdate`, `Physics.ComputePenetration` de los colliders del carro contra todo collider no
   trigger cercano (que no sea el carro ni el piso o las banquetas sobre las que rueda) da menos de 0.1 m.
4. Mediciones (en la bitácora de la tarea): tiempo de la casa al clandestino con el piloto, y tiempo de punta a punta de la calle
   principal con W a fondo. Si el "minuto y medio" de ciudad.md queda lejos, se anota la cifra para docs/revision-alan.md; no se
   cambian las medidas del barrio.
5. Para el orquestador, al integrar (no es trabajo del implementador): anotar en docs/revision-alan.md que T-061 de Tony se
   desbloquea cuando alan/fase-2 llegue a main, con las teclas del carro y la regla de `PuedeBajar` (encabezado de CarroViejo.cs).

## Carpetas permitidas
Assets/_Proyecto/Vehiculos/, Assets/_Proyecto/Mundo/Editor/, Assets/_Proyecto/Escenas/Ciudad.unity,
Assets/_Proyecto/Escenas/Pruebas_Alan.unity, Assets/_Proyecto/Tests/PlayMode/Vehiculos/ y Assets/_Proyecto/Tests/PlayMode/Mundo/
No tocar: Core/, Clandestino.unity (salvo lo que regenere CreadorTodo sin cambios), las carpetas de Tony, ProjectSettings/.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa (incluidas BarrioPruebas, BarInteriorPruebas y FinalesCarroPruebas)
- [ ] En Ciudad.unity y Pruebas_Alan.unity hay exactamente un `IMontable` (el carro), a menos de 6 m de `Casa_Cochera`, y a
      menos de 8 m de él no hay ningún carro greybox ni bote
- [ ] RecorridoCarroPruebas pasa: sube, llega a menos de 6 m de la `Entrada` del clandestino en menos de 60 s de juego, el giro
      acumulado del carro es de 80° o más (dio al menos una vuelta), baja en modo APie y camina hasta la entrada
- [ ] En esa prueba: penetración máxima medida < 0.1 m, ningún frame de más de 100 ms (`Time.unscaledDeltaTime` máximo) y un solo
      `AudioListener` prendido en todo momento
- [ ] Correr `CreadorTodo.Crear` dos veces seguidas deja `git status` igual que después de la primera
- [ ] Las dos mediciones del paso 4 están en docs/bitacora/ con su valor en segundos
- [ ] Capturas de noche: el carro estacionado visto desde la puerta de la casa (docs/capturas/T-078-1.png), primera persona
      saliendo a la calle principal (T-078-2.png) y tercera persona frente al clandestino y el bar (T-078-3.png)
- [ ] Consola sin errores en 3 minutos de manejo en el editor vivo (el verificador maneja con `eval` o la prueba repetida)

## Criterios de sensación (para un humano)
- Pruebas_Alan.unity, Play: caminar al carro, E, manejar de la casa al clandestino y bajarse se siente como un juego; las calles
  dan para maniobrar y la vuelta en la esquina es natural.
- ¿El barrio se siente del tamaño correcto en carro? (ver la medición del paso 4)

## Pruebas automáticas
- PlayMode (Tests/PlayMode/Vehiculos/): RecorridoCarroPruebas, con InputTestFixture.

## Fuera de alcance
- El jugador de Tony en el carro (T-061, de Tony); colocar el prefab del jugador de Tony en las escenas (espera T-059 en main)
- Tráfico (T-084); agrandar el barrio; estacionarse en la cochera de la casa por dentro (A-F3)
