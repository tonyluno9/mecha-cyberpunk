# T-040 Barrio greybox y cámara libre
Dueño: Alan | Rama: alan/T-040-barrio-greybox | Modelo: Sonnet 5.5, medium
Depende de: T-030 en main (carpeta Mundo/ y su ensamblado de pruebas), T-020 (Pruebas_Alan.unity). Parte B: T-031 en main.

## Objetivo
Existe un barrio greybox de 6 manzanas con calles, vueltas y finales creíbles, con los cinco lugares reales en su sitio, y una
cámara libre propia de Alan para recorrerlo a pie sin depender del jugador de Tony. Es la base de Ciudad.unity y del hito H2.

## Contexto
- Diseño: docs/diseno/ciudad.md: casa en el extremo pobre; clandestino y bar frente a frente en el centro; casino a una cuadra;
  mercado negro en un callejón cerca del clandestino; para ir de casa al clandestino hay que dar al menos una vuelta; las calles
  terminan en algo creíble (retén, obra, autopista elevada), nunca en un muro invisible; unas 6 manzanas.
  docs/diseno/estetica-hud.md: low-poly, referencia de calle con neón (aquí solo greybox).
- Decisiones: D-014 (1 unidad = 1 m; el mecha mide 4 m y también camina por estas calles), D-026, D-029 (Ciudad.unity y
  Pruebas_Alan.unity son de Alan). Propuestas: D-alan-2 (cámara libre), D-alan-3 (barrio como prefab y medidas) en
  docs/decisiones-pendientes-alan.md.
- Mundo/ solo referencia Core, Unity.InputSystem y UnityEngine.UI (T-030). Las teclas van en un `.inputactions` propio de Alan,
  no en ControlesMecha de Tony.
- Escenas y prefabs con un creador de editor: `Mundo/Editor/CreadorBarrio.cs`, asmdef `MechaCyberpunk.Mundo.Editor` (solo
  Editor, referencia Core y Mundo). Las medidas del trazado pueden ser constantes con nombre en el creador.
- Pruebas con entrada simulada: `InputTestFixture`, como en Tests/PlayMode/Mecha/ControladorMechaPruebas.cs (solo leerlo).
- Cómo saber si llegó T-031: `git show main:Assets/_Proyecto/Core/Jugador/IJugador.cs`.

## Qué hacer

### Parte A: sin esperar a Tony (solo necesita T-030)
1. Cámara libre en `Mundo/Pruebas/`: `CamaraLibre.cs` con `CharacterController` (radio 0.35 m, ojos a 1.75 m), `ControlesCamaraLibre.inputactions`
   (WASD, ratón para mirar, Shift correr, F alterna caminar y volar; al volar Espacio sube y Ctrl baja, sin colisiones) y
   `Mundo/Datos/ConfigCamaraLibre.cs` con su asset (velocidad a pie 4.5 m/s, corriendo 8, volando 20, sensibilidad, gravedad).
2. Prefab `Mundo/Prefabs/Barrio.prefab`, generado por el creador:
   - 3 × 2 manzanas de unos 80 × 80 m (hijos `Manzana_1` a `Manzana_6`), calles de 14 m con banquetas de 3 m y 0.2 m de alto.
   - Edificios de cubos de 8 a 30 m de alto con colliders; la zona de la casa más baja y deteriorada (extremo pobre).
   - Los cinco lugares como edificios distintos, cada uno con un hueco de puerta hacia la calle y un hijo vacío `Entrada`
     frente a ella, del lado de la calle: `Lugar_Casa` (en una esquina extrema), `Lugar_Clandestino` y `Lugar_Bar` frente a
     frente en la calle central, `Lugar_Casino` a una cuadra, `Lugar_MercadoNegro` en un callejón de unos 6 m cerca del
     clandestino. Por ahora los huecos de puerta están tapados con un panel con collider (T-042 pone las puertas).
   - Cada salida del barrio cerrada por un final visible con collider: al menos un `Final_Reten` (barrera y caseta), un
     `Final_Obra` (vallas y conos) y un `Final_Autopista` (pilares y muro de contención de la autopista elevada).
   - Farolas (`Farola_n`) cada 25 m más o menos, con una `Light` apagada (T-041 las prende de noche).
   - Materiales greybox en `Mundo/Materiales/`.
3. `Escenas/Ciudad.unity`: el barrio, una luz direccional de noche y niebla leve. Sin cámara de pruebas.
4. `Escenas/Pruebas_Alan.unity`: agrega el barrio y la cámara libre frente a la casa; quita la cámara fija de T-020 y deja la
   jaula fuera del barrio, a más de 100 m.

### Parte B: cuando T-031 esté en main
5. `CamaraLibre` implementa `IJugador` (`Modo` APie, `Camara` la suya, `Colocar` con el CharacterController apagado) y se agrega
   a `Registro<IJugador>` en `OnEnable`. Así las puertas y zonas de Alan (T-042 y T-043) se prueban sin el jugador de Tony.
6. `Mundo/Lugares/PuntoAparicion.cs : MonoBehaviour, IPuntoAparicion` y tres puntos en el barrio: `Casa_Puerta` (APie),
   `Casa_Cochera` (Carro) y `Clandestino_Puerta` (APie), cada uno sobre la banqueta, mirando a la calle.

## Carpetas permitidas
Assets/_Proyecto/Mundo/, Assets/_Proyecto/Tests/EditMode/Mundo/, Assets/_Proyecto/Tests/PlayMode/Mundo/,
Assets/_Proyecto/Escenas/Ciudad.unity y Assets/_Proyecto/Escenas/Pruebas_Alan.unity
No tocar: Core/, Arena/ (salvo mover la instancia de la jaula en Pruebas_Alan), las carpetas y escenas de Tony,
ProjectSettings/ ni los asmdefs de pruebas.

## Criterios de aceptación (los comprueba el verificador)
Parte A:
- [ ] `bash herramientas/verificar.sh todo` pasa y `verificar.sh ensamblados` lista MechaCyberpunk.Tests.PlayMode.Mundo
- [ ] PlayMode con entrada simulada: con W 2 s la cámara avanza entre 7 y 10 m; con Shift y W, más; mover el ratón gira la vista
- [ ] PlayMode con entrada simulada: caminando 4 s de frente contra un edificio, la cámara queda fuera de su collider
- [ ] PlayMode con entrada simulada: con F vuela, sube con Espacio y atraviesa un edificio; con F otra vez vuelve a caminar
- [ ] PlayMode sobre el prefab: hay 6 manzanas y los 5 `Lugar_*` con su `Entrada`; la línea recta a 1 m de alto entre la
      entrada de la casa y la del clandestino choca con un edificio (hay que dar vuelta); las entradas del clandestino y del
      bar están a menos de 30 m y se miran (producto punto de sus forward < -0.7); el casino está a menos de 140 m del
      clandestino; el mercado negro a menos de 80 m del clandestino; la casa es el lugar más lejano del centro del barrio
- [ ] PlayMode sobre el prefab: 64 rayos horizontales a 1 m de alto desde el centro del barrio, de 1 km de largo, chocan todos
      con algo, y lo que chocan en el borde tiene un `MeshRenderer` activo (no hay muros invisibles)
- [ ] PlayMode con entrada simulada: caminando por una calle hasta su final, la cámara se detiene en él y no sale del barrio
- [ ] Capturas desde la cámara libre: frente a la casa, en la calle del clandestino y el bar, y en un final:
      docs/capturas/T-040-1.png a -3.png; las calles se distinguen de las banquetas y los finales se ven
- [ ] Consola sin errores al dar Play en Ciudad.unity y Pruebas_Alan.unity
Parte B:
- [ ] PlayMode: con la cámara libre en escena, `Registro<IJugador>.Actual` es ella y `Colocar` la mueve sin atravesar nada;
      los tres puntos de aparición están en `Registro<IPuntoAparicion>` con su `Id` y su `Modo`
- [ ] Si T-031 no estaba en main al terminar la parte A, la tarea queda "hecha" con la parte B anotada en docs/revision-alan.md

## Criterios de sensación (para un humano)
- Caminando con la cámara libre de la casa al clandestino, el barrio se siente de verdad: hay vueltas, las calles no parecen un
  tablero vacío y los finales se ven creíbles.
- La escala sirve para una persona a pie y para un mecha de 4 m por las calles principales.

## Pruebas automáticas
- PlayMode (Tests/PlayMode/Mundo/): CamaraLibrePruebas (entrada simulada) y BarrioPruebas (estructura y rayos).
- EditMode (Tests/EditMode/Mundo/): si sacas cuentas de la cámara a lógica pura (por ejemplo, el giro con límites de inclinación),
  pruébalas aquí.

## Fuera de alcance
- Puertas, horarios y letreros (T-042); interiores (T-043 y A-F3); día y noche (T-041)
- Carro, tráfico y peatones (A-F2 y A-F5); arte final, neón y lluvia (A-F4)
- Jugador de Tony en estas escenas (T-F3)
