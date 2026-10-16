# T-127 Ensamblado del prólogo y escena de pruebas Pruebas_Prologo
Dueño: Tony | Rama: tony/T-127-escena-prologo | Modelo: Sonnet 5.5, medium
Depende de: T-120, T-121

## Objetivo
Existe el lugar donde Tony arma y prueba el prólogo sin esperar a Alan: `Escenas/Tony/Pruebas_Prologo.unity`, un barrio greybox con la
casa y su taller, una calle con una vuelta, el clandestino con su arena, el callejón de la emboscada y el carro, todo con los dobles de
Tony de los contratos de Alan. Y existe el ensamblado `MechaCyberpunk.Progresion.Prologo`, encima de todas las áreas de Tony, donde van el
director del prólogo y lo que encadena áreas (T-128 en adelante).

## Contexto
- Decisiones: D-055 y D-068 (una escena de pruebas la genera solo su creador; los dobles de Alan viven en `<Área>/Pruebas/`), D-034
  (Registro<T>), D-053 (objetivos intermedios en las esquinas). Propuestas nuevas: D-tony-62 (ensamblado) y D-tony-69 (escena).
- Lección de T-F5 (T-121): el creador arma todo en un orden fijo y es idempotente; las tareas siguientes agregan su parte DENTRO de
  `Crear` (métodos por sección), no con métodos sueltos.
- Dobles que ya existen: `Pelea/Pruebas/ArenaDePrueba.prefab`, `Mecha/Pruebas/TallerDePrueba` y `PuntoLlamadoDePrueba`,
  `Personaje/Pruebas/LugarDePrueba`, `ZonaLugarDePrueba`, `PuntoAparicionDePrueba`, `CarroDePrueba.prefab`, `Progresion/Pruebas/RelojDePrueba`,
  `GuardadoDePrueba`, `Economia/Pruebas/DineroDePrueba`. Prefabs: `Personaje/Prefabs/JugadorAPie.prefab`, `Mecha/Prefabs/MechaEstacionado.prefab`,
  `Pelea/Prefabs/Emboscada.prefab`.
- La regla de subensamblados de ReglasEnsamblados llega con T-120.

## Qué hacer
1. Ensamblados: `Progresion/Prologo/MechaCyberpunk.Progresion.Prologo.asmdef` (referencia Core, Combate, IA, Mecha, Pelea, Personaje,
   Progresion, Economia, UI, Unity.InputSystem y UnityEngine.UI; nadie lo referencia) y `Progresion/Prologo/Editor/MechaCyberpunk.Progresion.Prologo.Editor.asmdef`
   (solo editor; esas mismas más Prologo). Los asmdef de pruebas de Tony que lo necesiten (EditMode y PlayMode de Progresion) lo
   referencian. Referencias por nombre, nunca por GUID.
2. `Progresion/Pruebas/PuntoHistoriaDePrueba.cs`: doble de `IPuntoHistoria` (id, pose del transform, cuenta las llamadas a `Activar`).
3. `Progresion/Prologo/Editor/CreadorEscenaPrologo.cs` (idempotente, orden fijo, un método por sección) arma `Escenas/Tony/Pruebas_Prologo.unity`
   (1 unidad = 1 m; distancias aproximadas, lo importante es que se respete la forma):
   - Piso de 200 × 200 m, luz direccional de noche suave y edificios de cajas a los lados de las calles.
   - Calle este-oeste en z = 0 de x = -70 a x = 40 y calle norte-sur en x = 40 de z = 0 a z = 75 (una vuelta en la esquina (40, 0, 0)).
   - Casa en el extremo oeste (centro (-70, 0, 8)): cuatro muros con un hueco de puerta hacia la calle, `LugarDePrueba` Casa con su
     `ZonaLugarDePrueba` en el interior, aparición "Casa_Cama" (dentro, a pie) y "Casa_Puerta" (afuera, a pie). Dos objetos vacíos
     "Sitio_Cama" y "Sitio_Espejo" dentro, para T-130.
   - Taller junto a la casa: `TallerDePrueba` con su estacionamiento y banco, y `MechaEstacionado.prefab`.
   - Clandestino al norte (puerta en (40, 0, 75)): muros, `LugarDePrueba` Clandestino con zona, `ArenaDePrueba.prefab` detrás (a unos
     25 m de la puerta, dentro del edificio o de la jaula), aparición "Clandestino_Salida" en (40, 0, 72) mirando al sur, y un objeto vacío
     "Sitio_Mostrador" junto a la puerta por dentro, para T-129.
   - Callejón: sale de la calle norte-sur hacia el oeste en z = 40, de 8 m de ancho y 25 m de fondo, con `Emboscada.prefab` en su entrada
     y +Z hacia el fondo; `PuntoLlamadoDePrueba` cuyo radio cubre la entrada del callejón y la calle frente a ella.
   - Carro: `CarroDePrueba.prefab` en (40, 0, 20), mirando al sur; puntos de historia "Prologo_Carro" (junto al carro) y
     "Prologo_Esquina_1" (en la esquina (40, 0, 0)).
   - `RelojDePrueba` (día 1, 21:00), `JugadorAPie.prefab` con `idAparicion` "Casa_Puerta", `GuardadoDePrueba` y `DineroDePrueba`. Un
     solo `AudioListener` activo y una sola cámara activa al arrancar.
4. PlayMode `Tests/PlayMode/Progresion/EscenaPrologoPruebas.cs`: carga la escena y comprueba lo registrado (ver criterios).

## Carpetas permitidas
Assets/_Proyecto/Progresion/Prologo/ (nueva, con Editor/), Assets/_Proyecto/Progresion/Pruebas/PuntoHistoriaDePrueba.cs (nuevo),
Assets/_Proyecto/Escenas/Tony/Pruebas_Prologo.unity (con su creador), los asmdef de Assets/_Proyecto/Tests/EditMode/Progresion/ y
Tests/PlayMode/Progresion/, Assets/_Proyecto/Tests/PlayMode/Progresion/, docs/capturas/
No tocar: Core/, todo lo de Alan, las otras escenas, los prefabs de otras áreas (se usan como están).

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa, incluida ReglasEnsamblados con los asmdef nuevos
- [ ] EscenaPrologoPruebas: un `IJugador` en "Casa_Puerta" (±0.1 m); dos `IMontable` (mecha estacionado y carro); un `IArenaPelea`, un
      `ITallerMecha`, un `IPuntoLlamadoMecha`, un `IRelojMundo` (de noche); apariciones "Casa_Cama", "Casa_Puerta" y "Clandestino_Salida";
      puntos de historia "Prologo_Carro" y "Prologo_Esquina_1"; lugares Casa y Clandestino; el mecha a menos de 0.1 m del estacionamiento
- [ ] La entrada del callejón queda dentro del radio del punto de llamado y a más de 12 m del recorrido casa-esquina (la emboscada no se
      dispara caminando de la casa a la esquina) — medido en la prueba
- [ ] Un solo `AudioListener` y una sola cámara activa al arrancar; consola sin errores en 3 s de juego
- [ ] Correr `CreadorEscenaPrologo.Crear` dos veces no cambia el archivo la segunda vez
- [ ] Captura docs/capturas/T-127-1.png: vista desde arriba del barrio (casa, vuelta, callejón, carro y clandestino se distinguen)

## Criterios de sensación (para un humano)
- Pruebas_Prologo, Play: camina de la casa al clandestino. ¿La vuelta y las distancias se sienten como un barrio chico (unos 40 s a
  pie)? ¿El callejón se ve como un lugar de emboscada?

## Pruebas automáticas
- PlayMode: EscenaPrologoPruebas.

## Fuera de alcance
- Los flujos de pelea, el mostrador, la cama y el espejo (los agregan T-128, T-129 y T-130 en este creador); el director (T-132);
  parecerse a Ciudad.unity de Alan
