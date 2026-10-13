# T-074 Carro viejo: física y manejo
Dueño: Alan | Rama: alan/T-074-carro-fisica-manejo | Modelo: Sonnet 5.5, medium
Depende de: nada (sale de alan/fase-2). Fase A-F2, hito H3.

## Objetivo
Existe `CarroViejo.prefab`, un carro greybox que se maneja con teclado: acelera, frena, da reversa, gira según la velocidad,
sube banquetas de 20 cm sin atorarse, no se voltea en una vuelta cerrada y no atraviesa muros. Es la base de todo A-F2: el
jugador tiene que manejar para llegar a cada sitio del barrio (docs/diseno/ciudad.md).

## Contexto
- Diseño: docs/diseno/ciudad.md (solo usas tus carros; empiezas con uno viejo; hay que manejar para llegar), demo.md (regresas
  a casa en carro), personaje-progresion.md (tercera persona opcional al manejar: es de T-075).
- Hoja de ruta: A-F2 y H3. Propuestas D-alan-10 y D-alan-11 en docs/decisiones-pendientes-alan.md (valores y teclas).
- Convenciones: .claude/rules/unity-csharp.md (lógica pura aparte, valores en `Config*`, teclas en `.inputactions` creado por
  código de editor, como hace `CreadorBarrio.CrearControles` con ControlesCamaraLibre).
- Esta tarea NO implementa `IMontable` (es T-075): el carro expone `TomarControl(bool)` para prender o apagar su entrada, y las
  pruebas lo llaman directo.
- Física: `Rigidbody` dinámico + suspensión por rayos (un rayo hacia abajo por rueda, sin `WheelCollider`). Es la forma más
  controlable y probable de este tipo de carro arcade. El collider de la carrocería empieza a 0.45 m del piso para que las ruedas
  (los rayos) suban la banqueta y la caja no choque con el borde.

## Qué hacer
1. Lógica pura en `Vehiculos/Carro/` (sin `MonoBehaviour`, con `dt` como parámetro):
   - `CalculoSuspension.Fuerza(compresion, velocidadCompresion, rigidez, amortiguamiento)`: resorte con amortiguador, nunca
     negativa (la rueda no jala al carro hacia el piso).
   - `CalculoManejo`: fuerza del motor según entrada y velocidad hacia adelante (se corta al llegar a la máxima), freno cuando la
     entrada es contraria a la marcha, reversa solo con el carro casi detenido (< 0.5 m/s) y S sostenida, ángulo de dirección que
     baja con la rapidez (máximo a 0 m/s, mínimo a velocidad máxima), agarre lateral (cancela el derrape) que baja con el freno de
     mano, y freno de estacionamiento cuando no hay control (`TomarControl(false)`).
2. `Vehiculos/Datos/ConfigCarro.cs` y `ConfigCarro_Viejo.asset` (creado por código): masa 1100 kg, velocidad máxima 16 m/s,
   reversa 5 m/s, de 0 a 14 m/s en unos 6 s, freno de 16 m/s a 0 en menos de 3.5 s, dirección 32° a 0 m/s y 10° a la máxima,
   suspensión (largo de reposo, rigidez, amortiguamiento), agarre lateral, centro de masa bajo, fuerza antivuelco. Todo con
   `[Tooltip]` con unidad y `[Min]`/`[Range]`.
3. `Vehiculos/Carro/CarroViejo.cs` (MonoBehaviour): en `FixedUpdate` aplica la suspensión en las 4 ruedas con
   `AddForceAtPosition`, el motor y el freno en las ruedas que tocan piso, el agarre lateral y el antivuelco. Los rayos ignoran
   triggers y los colliders del propio carro (compara `transform.root`). Expone `Velocidad` (m/s con signo), `Rapidez`,
   `RuedasEnPiso`, `TieneControl`, `EntradaActual` (para las ruedas visuales de T-085) y `TomarControl(bool)`.
4. Entrada: `Vehiculos/Carro/ControlesCarro.inputactions`, creado por un creador de editor en un asmdef nuevo
   `Vehiculos/Editor/MechaCyberpunk.Vehiculos.Editor.asmdef` (solo Editor; referencia Core, Vehiculos e InputSystem). Acciones:
   Acelerar (W y flecha arriba), Frenar (S y flecha abajo), Girar (eje A/D y flechas izquierda/derecha), FrenoMano (Espacio),
   CambiarCamara (C, la usa T-075) y Mirar (delta del ratón, la usa T-075). El carro solo lee la entrada con control.
5. Creador `Vehiculos/Editor/CreadorCarro.cs` (`[MenuItem("Mecha Cyberpunk/Crear carro (T-074)")]` y método estático `Crear`
   para batchmode): crea el config, los controles y `Vehiculos/Prefabs/CarroViejo.prefab` greybox de 4.2 × 1.45 × 1.8 m
   (carrocería, cabina más angosta, 4 ruedas cilíndricas visuales sin collider, hijos vacíos `Rueda_DI`, `Rueda_DD`, `Rueda_TI`,
   `Rueda_TD` en los ejes). Materiales greybox en `Vehiculos/Materiales/`. `Rigidbody` con interpolación y detección continua.
   Correrlo dos veces deja los assets iguales y no pisa un config que ya existe.

## Carpetas permitidas
Assets/_Proyecto/Vehiculos/, Assets/_Proyecto/Tests/EditMode/Vehiculos/ y Assets/_Proyecto/Tests/PlayMode/Vehiculos/
No tocar: Core/, Mundo/, las escenas (el carro llega al barrio en T-078), las carpetas de Tony, ProjectSettings/.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa, con las pruebas nuevas contadas en el total
- [ ] EditMode `CalculoSuspensionPruebas` y `CalculoManejoPruebas`: la suspensión nunca da fuerza negativa y crece con la
      compresión; el motor no empuja más allá de la velocidad máxima; S con marcha adelante frena y no da reversa; S con el carro
      a menos de 0.5 m/s da reversa hasta 5 m/s; el ángulo de dirección a velocidad máxima es menor que a 0 m/s; sin control, el
      freno de estacionamiento detiene el carro
- [ ] PlayMode (piso plano de 200 × 200 m creado por la prueba, sin escena): soltado a 0.5 m del piso, en 2 s se asienta con las
      4 ruedas en el piso y su altura oscila menos de 2 cm después
- [ ] PlayMode con entrada simulada (InputTestFixture): W 6 s desde parado llega a 14 m/s o más y nunca pasa de 16.5 m/s
- [ ] PlayMode: a 16 m/s, S lo deja a menos de 0.5 m/s en menos de 3.5 s; sostener S ya parado lo mueve hacia atrás a no más de
      5.5 m/s
- [ ] PlayMode: parado, A o D durante 2 s lo giran menos de 1°; a 10 m/s con D sostenida da una vuelta completa en un círculo de
      8 a 20 m de radio
- [ ] PlayMode: a 16 m/s con dirección a tope 3 s, `transform.up.y` nunca baja de 0.7 (no se voltea) y nunca pasa más de 0.3 s
      con menos de 2 ruedas en el piso
- [ ] PlayMode: un escalón de 0.2 m de alto (banqueta) cruzado de frente a 5 m/s: el carro termina arriba y sigue a más de 2 m/s
- [ ] PlayMode: contra un muro de 4 m de grueso a 16 m/s, el carro nunca queda del otro lado ni con posición NaN, y se detiene
- [ ] PlayMode: con `TomarControl(false)`, W y las flechas no lo mueven, y en una rampa de 5° se desliza menos de 0.2 m en 5 s
- [ ] Captura del carro greybox manejando en el piso de prueba: docs/capturas/T-074-1.png
- [ ] Consola sin errores ni avisos del carro en todas las pruebas

## Criterios de sensación (para un humano)
- Se siente como un carro viejo y pesado pero obediente: acelera sin prisa, frena firme y no derrapa solo.
- Subir una banqueta da un brinquito creíble, no un golpe seco ni un salto.

## Pruebas automáticas
- EditMode (Tests/EditMode/Vehiculos/): CalculoSuspensionPruebas, CalculoManejoPruebas.
- PlayMode (Tests/PlayMode/Vehiculos/): ManejoCarroPruebas, con InputTestFixture y un piso, un escalón, una rampa y un muro
  creados por la prueba.

## Fuera de alcance
- Subir y bajar, cámaras (T-075); colocarlo en el barrio (T-078); modelo final y ruedas que giran (T-085)
- Sonido del motor (A-F4), daño del carro, combustible, personalización (A-F5)
