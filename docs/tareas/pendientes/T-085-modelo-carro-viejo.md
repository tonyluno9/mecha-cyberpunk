# T-085 Modelo del carro viejo (Blender) con ruedas, luces e interior
Dueño: Alan | Rama: alan/T-085-modelo-carro-viejo | Modelo: Sonnet 5.5, medium
Depende de: T-076 (cadena Blender a FBX y materiales), T-075 (cámaras del carro). Mejor al final de la fase, después de T-084.

## Objetivo
El carro greybox se vuelve un sedán compacto viejo de barrio, low-poly y con carácter (defensa chueca, rines de acero, faros
redondos, calaveras rojas), con interior para la primera persona (tablero con medidores que brillan, volante, asientos), ruedas
que giran y viran, faros que alumbran de noche y luces de freno. La física y el contrato no cambian.

## Contexto
- El prefab `Vehiculos/Prefabs/CarroViejo.prefab` (T-074 y T-075): 4.2 × 1.45 × 1.8 m, puntos `Rueda_DI`, `Rueda_DD`, `Rueda_TI`,
  `Rueda_TD`, `Asiento`, `PuntoSubida`, `CamaraCarro`; `CarroViejo` expone `Velocidad`, `EntradaActual` y `RuedasEnPiso`.
- Blender headless (ruta en .claude/maquina.local.md); estilo y funciones de apoyo en `Assets/Arte/Fuentes~/kit-urbano/kit_urbano.py`
  (bevel bajo, materiales con nombre, origen en la base, frente hacia -Y). Los FBX pasan por el postprocesador y los materiales
  de T-076 (agrega los nuevos a materiales.json; el vidrio es transparente).
- Sin marcas reales: un sedán genérico de los 90, como los que se ven de taxi o de carro de familia en México.
- Las ruedas visuales y las luces son presentación: valores en un `ConfigVisualCarro` y lógica pura aparte.

## Qué hacer
1. `Assets/Arte/Fuentes~/carro/carro_viejo.py`: el carro en objetos separados con nombre fijo: Carroceria, Interior (tablero,
   asientos, pilares), Volante (pivote en su eje), Vidrios, Faros, Calaveras, Rueda_DI, Rueda_DD, Rueda_TI, Rueda_TD (pivote en el
   centro de cada rueda, radio de unos 0.32 m). Unos 8 000 a 15 000 triángulos. Exporta `Assets/Arte/Vehiculos/CarroViejo/carro_viejo.fbx`,
   un render de referencia .png en Fuentes~/carro/, y actualiza materiales.json y dimensiones.json. Pintura con un color base
   gastado (un material `Pintura_Carro` que Unity pueda variar después).
2. Creador del carro (T-074): el modelo reemplaza la carrocería y las ruedas greybox; las ruedas del modelo quedan en los puntos de
   rueda de la física (se ajustan esos puntos al modelo, sin cambiar los valores del config). La cámara de primera persona se
   ajusta a los ojos del conductor dentro del modelo, con el tablero a la vista.
3. `Vehiculos/Carro/VisualCarro.cs` + lógica pura `CalculoVisualCarro`: las ruedas giran según la velocidad (ángulo = distancia /
   radio), las delanteras viran con la dirección, el volante gira con la dirección; las calaveras suben su emisión al frenar y en
   reversa; dos faros (`Light` de tipo spot) que se prenden mientras el carro está ocupado o de noche (por `Registro<IRelojMundo>`
   de Core, sin referenciar Mundo/); medidores del tablero con emisión cian.

## Carpetas permitidas
Assets/Arte/Fuentes~/carro/, Assets/Arte/Vehiculos/, Assets/Arte/Materiales/, Assets/Arte/Mundo/dimensiones.json,
Assets/_Proyecto/Vehiculos/, Assets/_Proyecto/Mundo/Editor/ (solo si el postprocesador de T-076 necesita aceptar la carpeta del
carro), Assets/_Proyecto/Escenas/Ciudad.unity y Pruebas_Alan.unity (solo lo que regenere CreadorTodo),
Assets/_Proyecto/Tests/EditMode/Vehiculos/ y Assets/_Proyecto/Tests/PlayMode/Vehiculos/
No tocar: Core/, Trafico/, Clandestino.unity, las carpetas de Tony.

## Criterios de aceptación (los comprueba el verificador)
- [ ] El script de Blender corre headless sin errores y deja el FBX y su render
- [ ] `bash herramientas/verificar.sh todo` pasa, con todas las pruebas de T-074, T-075, T-077 y T-078 sin cambios de criterio
- [ ] EditMode: el modelo mide de 4.0 a 4.4 m de largo, 1.6 a 1.85 m de ancho y 1.3 a 1.5 m de alto; tiene los 10 objetos con su
      nombre; triángulos entre 8 000 y 15 000; los pivotes de las ruedas quedan a menos de 5 cm de su punto de rueda de la física
- [ ] EditMode `CalculoVisualCarroPruebas`: el ángulo girado por la rueda es distancia / radio; las calaveras brillan más con freno
      o reversa que en marcha
- [ ] PlayMode con entrada simulada: tras 3 s con W, el giro acumulado de una rueda trasera coincide con la distancia recorrida /
      radio (±10 %); con D sostenida, las ruedas delanteras y el volante giran hacia la derecha y las traseras no viran; con S en
      marcha, la emisión de las calaveras sube
- [ ] PlayMode: a las 22:00 con el carro ocupado, los dos faros están prendidos; a las 12:00 desocupado, apagados
- [ ] Capturas de noche: tercera persona en la calle principal (docs/capturas/T-085-1.png), primera persona con el tablero
      (T-085-2.png) y el carro estacionado frente a la casa (T-085-3.png); render de Blender en Fuentes~/carro/
- [ ] Consola sin errores

## Criterios de sensación (para un humano)
- Es "mi carro viejo": se ve gastado y con personalidad, y combina con el barrio.
- Desde dentro se siente que voy manejando un carro de verdad; las ruedas y el volante acompañan lo que hago.

## Pruebas automáticas
- EditMode (Tests/EditMode/Vehiculos/): ModeloCarroPruebas, CalculoVisualCarroPruebas.
- PlayMode (Tests/PlayMode/Vehiculos/): VisualCarroPruebas.

## Fuera de alcance
- Sonido del motor (A-F4); daño visible, colores y personalización (A-F5); animación de puertas
- Usar este modelo en el tráfico (los carros de tráfico siguen greybox)
