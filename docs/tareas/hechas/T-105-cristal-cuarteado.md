# T-105 Cristal cuarteado
Dueño: Tony | Rama: tony/T-105-cristal-cuarteado | Modelo: Sonnet 5.5, medium
Depende de: T-104

## Objetivo
El cristal de la cabina se cuartea con el daño en dos niveles: grietas leves desde las orillas con la vida a la mitad y grietas
fuertes con un impacto en estrella cuando está muy baja. Las grietas nunca tapan el centro de la vista, así el jugador ve al
rival aunque el mecha esté fregado, y desaparecen al reparar.

## Contexto
- Diseño: combate.md ("el cristal se cuartea"; criterio: "Con vida baja veo el LED rojo y el cristal cuarteado"),
  estetica-hud.md (low-poly, neón azul, magenta, naranja y turquesa; en el mecha todo es diegético), mecha-taller.md ("Cómo se ve
  el mecha fregado" es pregunta abierta).
- Decisiones: D-011, D-061 (el cristal y la persiana del encendido). Propuesta: D-tony-43 (nivel 1 con vida <= 50 %, nivel 2 con
  vida <= 25 %; centro libre).
- Código: Mecha/Editor/CreadorCabina.cs (`Cristal`: quad de 1.8 × 1.0 m que mira al piloto, justo detrás del marco),
  Mecha/Editor/CreadorEncendidoMecha.cs (la persiana), Mecha/Editor/CreadorDanioCabina.cs y Mecha/Datos/ConfigMechaDaniado.cs
  (T-104), Combate/Vida/VidaMecha.cs (`FijarVida`, T-103).

## Qué hacer
1. `ConfigMechaDaniado`: agrega `umbralGrietas1` 0.5 y `umbralGrietas2` 0.25 (fracciones de vida), color de las grietas
   (blanco azulado, alfa ~0.55) y su ancho (m, ~0.004). Actualiza el asset `_Base`.
2. `Mecha/Danio/ReglaMechaDaniado.cs`: agrega `int NivelGrietas(float actual, float maxima, float umbral1, float umbral2)` → 0, 1
   o 2 (en KO, 2).
3. `Mecha/Cabina/CristalCuarteado.cs`: dos grupos hijos (`Grietas1` y `Grietas2`); muestra el grupo 1 desde el nivel 1 y los dos en
   el nivel 2. Se actualiza con `Cambio` de la vida. Expón `int Nivel` para las pruebas.
4. `CreadorDanioCabina`: genera las grietas como `LineRenderer` en el espacio local del cristal, a 1 cm del lado del piloto, con
   semilla fija (el prefab sale igual cada vez): nivel 1 = 3 a 4 grietas que entran desde las orillas y esquinas; nivel 2 = 5 o 6
   más y una estrella de impacto en una esquina superior. Ningún punto de ninguna grieta entra en el rectángulo central del 40 % del
   ancho y el 40 % del alto del cristal. Material URP Unlit transparente (Mecha/Materiales/). Agrega `CristalCuarteado` a la
   cabina de `MechaJugador.prefab`.
5. Pruebas:
   - EditMode `ReglaMechaDaniadoPruebas`: `NivelGrietas` con 60/100 → 0, 50/100 → 1, 26/100 → 1, 25/100 → 2, 0/100 → 2.
   - EditMode `Tests/EditMode/Mecha/GrietasPrefabPruebas.cs`: `MechaJugador.prefab` tiene `CristalCuarteado` con los dos grupos;
     ningún vértice de ningún `LineRenderer` cae en el rectángulo central (en coordenadas del cristal); correr el creador dos veces
     deja los mismos vértices.
   - PlayMode `Tests/PlayMode/Mecha/CristalCuarteadoPruebas.cs` (Pruebas_Tony): con vida 100 los dos grupos apagados; con
     `FijarVida(45)` solo el 1; con 20 los dos; en KO los dos; al `Reparar` ninguno.
6. Capturas: docs/capturas/T-105-1.png (vida 45, nivel 1) y T-105-2.png (vida 20, nivel 2 con la alarma de T-104), desde la
   cámara de la cabina con la persiana arriba y un rival enfrente.

## Carpetas permitidas
Assets/_Proyecto/Mecha/ (Cabina/, Danio/, Datos/, Editor/, Materiales/, Prefabs/), Assets/_Proyecto/Tests/EditMode/Mecha/,
Assets/_Proyecto/Tests/PlayMode/Mecha/, docs/capturas/
No tocar: Core/, Combate/, UI/, Pruebas_Tony.unity, las carpetas y escenas de Alan.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa
- [ ] ReglaMechaDaniadoPruebas, GrietasPrefabPruebas y CristalCuarteadoPruebas pasan
- [ ] Ningún vértice de grieta en el 40 % × 40 % central del cristal (prueba automática)
- [ ] El creador es determinista: dos corridas dejan los mismos vértices (prueba automática)
- [ ] Capturas T-105-1.png y T-105-2.png revisadas: grietas visibles en las orillas, el rival se ve en el centro
- [ ] Consola sin errores en las pruebas

## Criterios de sensación (para un humano)
- En Pruebas_Tony, baja la vida con H a 45 y luego a 20: ¿las grietas dicen "mecha fregado" de un vistazo sin estorbar la pelea?
- ¿Se ven low-poly y cyberpunk, o parecen un error de textura?

## Pruebas automáticas
- EditMode: ReglaMechaDaniadoPruebas, GrietasPrefabPruebas. PlayMode: CristalCuarteadoPruebas.

## Fuera de alcance
- Daño visible por fuera del mecha (humo, chispas, piezas abolladas): arte posterior; sonido del cristal
