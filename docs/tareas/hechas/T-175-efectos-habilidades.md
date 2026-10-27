# T-175 Los efectos de las habilidades: proyectil, cono, escudo, embestida y aturdir
Dueño: Tony | Rama: tony/T-175-efectos-habilidades | Modelo: Sonnet 5.5, medium
Depende de: T-174

## Objetivo
Cualquier mecha con un `LanzadorHabilidades` puede lanzar las 8 habilidades de T-174: los misiles vuelan y explotan, el lanzallamas, la
descarga, el martillo y la onda de calor pegan en un cono, el escudo baja el daño que entra y la embestida lanza al mecha hacia adelante.
La descarga deja aturdido al que la recibe (no se mueve ni golpea ni bloquea). Es la pieza de Combate que usan el jugador (T-176) y los
jefes (T-178); aquí todavía no hay teclas, ni munición, ni enfriamiento (eso lo lleva quien lanza).

## Contexto
- Diseño: combate.md (misiles, brazos de precisión, bloqueo), mecha-taller.md (tech: escudos y descargas).
- Decisiones: D-tony-134 (sin Core: los proyectiles pegan como `TipoDanio.Disparo`, el resto como `TipoDanio.Golpe`; aturdir vive en
  `PeleadorMecha`), D-tony-136 (valores), D-tony-122 (colores por rama; nada en rojo puro).
- Código: `Combate/Golpe/BarridoGolpe.cs` (SphereCast sin capas propias; no golpearse a sí mismo comparando `transform.root`),
  `Combate/Golpe/GolpeMecha.cs` (cómo arma `InfoDanio`), `Combate/Vida/VidaMecha.cs` (`RecibirDanio` aplica `MultiplicadorDanioRecibido` y la
  guardia; ojo: `PiedadRival` ya usa `MultiplicadorDanioRecibido`, el escudo NO lo reutiliza), `Combate/Peleador/PeleadorMecha.cs`
  (`controlados`, `ActivarControl`, `ControlActivo`), `IA/Rival/RivalBasico.cs` (está en los `controlados` del rival; su preparación de golpe).
- Pruebas modelo: `Tests/PlayMode/Combate/GolpeBloqueoPruebas.cs`, `VidaMechaPruebas.cs` (mechas armados por código, `SacoDePrueba`).

## Qué hacer
1. `Combate/Habilidades/LanzadorHabilidades.cs` (MonoBehaviour en la raíz del mecha): orígenes serializados (`origenBrazo`, `origenTorso`;
   si faltan, la raíz), `bool Lanzar(DatosHabilidad h, int nivel, bool desdeTorso)` que ejecuta el efecto con la potencia de
   `ReglaHabilidad.Potencia`, `event Action<DatosHabilidad> Lanzada`, `EscudoActivo`, `Embistiendo`. Devuelve false (sin efecto) si el mecha
   está en KO o si ya está embistiendo. Nunca daña a su propia raíz.
2. Efectos (un archivo por clase; lo que se puede, en lógica pura con `dt`):
   - Proyectil: `ProyectilHabilidad` (greybox: cápsula chica emisiva con el color de la rama y una estela corta) que avanza con su velocidad,
     revisa choques con SphereCast cada cuadro, explota al tocar algo o al pasar su alcance: daño a todo `IRecibeDanio` en el radio (una vez por
     raíz), con `TipoDanio.Disparo`. Salva: `cantidad` proyectiles repartidos en ±`dispersion`. Se destruye solo; ninguno vive más de 3 s.
   - Cono: daño instantáneo a cada raíz con `IRecibeDanio` dentro del alcance y del ángulo (360° = alrededor), con `TipoDanio.Golpe`; empuje
     (mueve el `CharacterController` del golpeado hacia afuera esa distancia en 0.2 s, sin atravesar paredes); `rompeGuardia` aplica el daño
     sin la guardia (agrega a `VidaMecha` una forma de recibir daño que ignore la guardia, sin cambiar el contrato de `IRecibeDanio`); `aturdir`
     llama a `PeleadorMecha.Aturdir`. Greybox: un destello en cono del color de la rama, 0.25 s.
   - Escudo: `VidaMecha` gana un factor propio (`FactorEscudo`, 1 = sin escudo) que se multiplica aparte de `MultiplicadorDanioRecibido`;
     el lanzador lo pone en (1 − reducción) durante duración × potencia y lo regresa a 1 al terminar, en KO y en `OnDisable`. Greybox: esfera
     translúcida turquesa alrededor del mecha (sin cambiar `.mat` en ejecución: `MaterialPropertyBlock` o material compartido).
   - Embestida: mueve al mecha con su `CharacterController` la distancia en la duración; se detiene al tocar un peleador (le hace el daño una
     vez, `TipoDanio.Golpe`, y lo empuja) o una pared. Durante la embestida el mecha no gira.
3. `PeleadorMecha.Aturdir(segundos)`: si tiene el control, apaga sus `controlados` (golpe, guardia, movimiento, el cerebro del rival) durante
   ese tiempo y luego los vuelve a encender solo si `ControlActivo` sigue en true; `EstaAturdido`; `ActivarControl(false)` corta el aturdimiento;
   un aturdimiento nuevo no se suma: se queda el que termina más tarde. `ControlActivo` no cambia (el flujo de pelea no se entera).
   `RivalBasico` (IA): al apagarse a media preparación de golpe, la cancela y no deja el brazo ni el color de aviso pegados.
4. `Combate/Datos/ConfigEfectosHabilidad.cs` + `_Base.asset` con lo de presentación (tamaño del proyectil, duración del destello, alfa del
   escudo); los valores de juego salen de `DatosHabilidad`.
5. PlayMode `Tests/PlayMode/Combate/HabilidadesPruebas.cs` (mechas armados por código, sin entrada; `EventosPelea.LimpiarSuscriptores()`):
   a) MISIL I a un saco a 10 m: le quita 20 al llegar (±0.01) y el proyectil ya no existe 3 s después; a 30 m (más que el alcance) no le quita nada;
   b) MISIL contra un mecha con la guardia arriba (bloqueo normal): entra 20 × (1 − reducción del bloqueo);
   c) SALVA I: 3 proyectiles; un saco grande enfrente recibe 24 en total, nunca más de 8 por proyectil;
   d) LANZALLAMAS II: un saco dentro del cono a 3 m pierde 17.5; uno a 3 m pero a 90° del frente no pierde nada; el propio mecha no pierde nada;
   e) MARTILLO I contra la guardia arriba: entra 16 completo; con la guardia abajo también 16;
   f) DESCARGA I a un rival `RivalBasico` con control: pierde 6 y durante 1.0 s no se mueve ni golpea (`EstaAturdido`); a los 1.1 s vuelve;
      si el flujo le quita el control a mitad, al terminar el aturdimiento sigue sin control;
   g) ESCUDO I: un golpe de 10 entra como 5 durante 3 s y como 10 después; con la piedad del rival del tutorial activa, los dos factores se
      multiplican y al terminar el escudo la piedad sigue;
   h) EMBESTIDA I sin nada enfrente: avanza 5 m (±0.2) en 0.3 s (±0.05); contra una pared a 2 m se detiene en la pared; contra un saco a 3 m
      le quita 14 una sola vez y lo empuja;
   i) ONDA DE CALOR I: un saco a 2 m atrás también pierde 15;
   j) 50 lanzamientos seguidos (todos los efectos) no dejan objetos de efecto vivos 4 s después.

## Carpetas permitidas
Assets/_Proyecto/Combate/ (Habilidades/, Vida/, Peleador/, Datos/, Materiales/, Editor/), Assets/_Proyecto/IA/Rival/RivalBasico.cs (solo la
cancelación de la preparación al apagarse), Assets/_Proyecto/Tests/PlayMode/Combate/, Assets/_Proyecto/Tests/EditMode/Combate/
No tocar: Core/, Mecha/, prefabs (los pone T-176 y T-178), escenas, todo lo de Alan.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa (salvo los 9 fallos conocidos de Alan en Arena)
- [ ] HabilidadesPruebas a) a j) pasan; las pruebas de Combate e IA que ya existían siguen en verde
      (`AREAS='Combate IA Mecha' bash herramientas/verificar.sh playmode-por-area`)
- [ ] Ningún archivo de Core/ cambió (`git diff --stat main -- Assets/_Proyecto/Core` vacío)
- [ ] Tras la suite, `git status` no muestra materiales ni assets de datos modificados
- [ ] Captura docs/capturas/T-175-1.png (un misil en vuelo y el destello del lanzallamas vistos desde fuera, en una escena de prueba), revisada a ojo

## Criterios de sensación (para un humano)
- Ninguno todavía (se sienten al usarlas desde la cabina en T-176 y T-177).

## Pruebas automáticas
- PlayMode: HabilidadesPruebas. EditMode: lo que se separe en lógica pura (paso de proyectil, cono, embestida).

## Fuera de alcance
- Teclas, munición, enfriamiento y pantallas (T-176, T-177). Que los rivales las usen (T-178). Sonido y partículas finales (greybox).
  Daño a cosas del mundo de Alan (los efectos solo buscan `IRecibeDanio`; si algo de Alan lo implementa, recibe el daño normal).
