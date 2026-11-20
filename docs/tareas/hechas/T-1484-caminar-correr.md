# T-1484 Caminar y correr perfectos
Dueño: Tony | Rama: tony/T-1484-caminar-correr | Modelo: Sonnet 5.5
Carpetas: Assets/_Proyecto/Personaje/ (sin Combate/ ni Movimiento/CamaraHombro.cs), Tests/EditMode/Personaje*, Tests/PlayMode/Personaje*, docs/

## Objetivo
Que caminar y correr del jugador a pie se vean y se sientan perfectos: pies sin patinar, arranques y frenadas suaves, giros con inclinación, pies bien en escaleras y pendientes. Solo clips que ya existen; lo que falte va a docs/animaciones-faltan.md.

## Fase 1: cómo está armado hoy (leído del código)
- Visual del jugador: `Visual_PJ01` (Personaje/Visual/Prefabs), dentro de `JugadorAPie`. Controlador `Assets/Arte/Personajes/Animaciones/ControladorBase.controller` con `Override_Jugador`. Los estados se llaman por papel (Loco_Caminar, Loco_Correr...) y el override (T-749) los cambia por clips de Blender: Loco_Caminar -> PJ01_Caminar_SinImplante, Loco_Correr -> PJ01_Correr_SinImplante, con implante PJ01_*_ConImplante, lados y atrás PJ01_Loco_Caminar{Atras,Der,Izq}. La lista exacta sale en Logs/t1484-medir.txt.
- Parámetro de velocidad: `Velocidad` (m/s horizontales, `VisualPersonaje.LeerEstadoDelPadre` del CharacterController, suavizado exponencial `suavizadoVelocidad` = 12/s) y `VelX`/`VelZ` (dirección local, suavizado `suavizadoDireccion` = 14/s), `Giro` (°/s), `ConImplante` (0 o 1).
- Máquina de estados (capa 0, ConstructorControladorBase): Idle -> Caminar con Velocidad > 0.1; Caminar -> Correr con Velocidad > 5.5 (vuelve con < 4.95); Frenar (trigger al soltar corriendo, `ReglasAnimacion.DebeFrenar`); GirarDer/GirarIzq en el sitio con Giro > 70 °/s; agachado: Agacharse -> AgachadoQuieto/AgachadoCaminar.
- Caminar y Correr son árboles: raíz 1D por `ConImplante` (0 y 1), y cada rama un árbol 2D SimpleDirectional2D por VelX/VelZ con adelante (0,1), atrás (0,-1), derecha (1,0), izquierda (-1,0). No hay mezcla por rapidez: un solo clip adelante, sin trote ni correr de lado/atrás (Correr mezcla los clips de caminar de lado, ver docs/animaciones-faltan.md).
- Velocidades del jugador: DatosPersonaje_Base caminar 4.5 m/s, correr 7.5 m/s (EfectosAPie y Habilidades pueden multiplicar); agachado `ReglasAnimacion.VelocidadAgachadoCaminando` 1.6 m/s (DatosCubierta.FactorAgachado). `ConfigControlesAPie_Base`: aceleración 30 m/s2, frenado 40 m/s2, control en aire 0.3. Giro del cuerpo hacia donde camina: `ConfigCamaraHombro.GiroCuerpo` 720 °/s con `MoveTowardsAngle` (casi instantáneo, sin inclinación).
- Rapidez de la animación: T-724 puso `ConfigurarClips(0,0)` (Animator.speed = 1) y `IKPiesYMirada.Pies = false` en `AnimacionJugador.ClipsDeTony`. Los clips medidos antes (Loco_Caminar 1.0-1.3 m/s, Loco_Correr 2.5-3.2 m/s) van mucho más lento que el cuerpo (4.5 y 7.5 m/s): los pies patinan, anotado en T-724. `VisualPersonaje.AjustarRapidez` ya sabe escalar Animator.speed por velocidad real / velocidad del clip, pero está apagado para el jugador.
- Cuerpo: aceleración lineal rápida (MoveTowards de `MovimientoAPie`), giro sin suavizar, sin inclinación; `IKPiesYMirada` tiene IK de pies con raycast (apagado en el jugador); escaleras a pie las anima `AnimacionEscalera`/`TreparAPie` aparte.

## Medición
`MechaCyberpunk.Personaje.Editor.MedirLocomocionT1484.Medir` (batch, `-executeMethod`). Escribe Logs/t1484-medir.txt con, por clip: duración, averageSpeed y apparentSpeed, velocidad del cuerpo que deja quieto al pie de apoyo (respecto de la cadera y de la raíz, con componentes adelante/derecha), pasos por ciclo, cadencia, longitud de paso y de zancada. Arriba vuelca las velocidades y configs actuales, el override del jugador, el controlador (estados, árboles, transiciones) y los campos serializados de VisualPersonaje.

## Plan de la fase 2 (con las medidas)
1. Árbol de locomoción por velocidad real (Simple1D por `Velocidad` o 2D con magnitud): umbrales = velocidad medida de cada clip (caminar, correr, y un punto de trote si el clip de caminar y el de correr quedan lejos), con `Animator.speed` / multiplicador del hijo para que a la velocidad del jugador (4.5 y 7.5) los pies no patinen sin acelerar el clip fuera de un rango natural. Decidir caminar y correr con números: la velocidad a la que el jugador camina probablemente se acerque más al clip PJ01_Correr que al de caminar; si es así se baja la velocidad de caminar del jugador (dato en DatosPersonaje) o se usa el clip más natural, y se argumenta con la medición.
2. Elegir entre Loco_Caminar (Mixamo) y PJ01_Caminar_SinImplante: el que a la velocidad correcta tenga menor rapidez extra y cadencia natural (1.6 a 2.2 pasos/s al caminar).
3. Arranque y frenada: aceleración y frenado de Config más suaves (lógica en `MovimientoAPie`, pruebas EditMode), amortiguado del parámetro (`suavizadoVelocidad`) y transición caminar -> correr -> frenar sin saltos (histéresis, Frenar solo desde correr, mezclas de duración fija).
4. Giro: suavizar la rotación del cuerpo (aceleración angular en `ControladorAPie.GirarCuerpo`, lógica pura probada) y una inclinación ligera del torso/cuerpo según el giro (parámetro `Giro` ya existe); sin tocar CamaraHombro.
5. Foot IK en escaleras, banquetas y pendientes: revisar y reactivar `IKPiesYMirada` para el jugador (ya hay raycast y peso); medir que no tire de los pies del clip en piso plano y baje la cadera.
6. Escaleras: revisar `AnimacionEscalera` y el paso de caminar a subir.
7. Pruebas EditMode de la lógica pura y PlayMode con entrada simulada (arranque, correr, frenar, giro, escalón).

## Dudas
- Si Tony quiere mantener 4.5 m/s al caminar (parece rápido para un caminar natural) o se puede bajar con el clip real.
- Si hay clips de trote o de correr de lado/atrás en Mixamo que Tony pueda bajar (hoy no existen; ver docs/animaciones-faltan.md).

## Fase 2 (hecha, 19-nov noche)
Medidas (Logs/t1484-medir.txt, averageSpeed de la raíz sobre Visual_PJ01): PJ01_Caminar 1.66 m/s, PJ01_Correr 4.25 m/s, de lado 1.61, atrás 1.04.
El jugador va a 4.5 m/s (paso normal) y 7.5 m/s (Shift) con Animator.speed = 1: los pies patinaban 2.7 veces al caminar y 1.76 al correr.

- **Trote en el paso normal**: a 4.5 m/s nadie camina. `Override_Jugador` pone PJ01_Correr_{Sin,Con}Implante en el estado Caminar
  (`ConstructorControladorBase.ClipsSoloJugadorT1484`, se aplica con `AjustarLocomocionT1484.Aplicar`). Los NPC siguen con PJ01_Caminar.
- **Rapidez por velocidad real**: `VisualPersonaje.ConfigurarClipsDireccionales` + `ReglasAnimacion.VelocidadClipDireccional`: la referencia es la mezcla
  del árbol 2D (adelante, atrás, lados) y la rapidez queda entre 0.7 y 1.6. A 4.5 m/s el trote va a 1.06 (pies quietos); a 7.5 m/s a 1.6 (patina 9 %).
  De lado y atrás (apuntando) llega al tope 1.6: ahí todavía patina (faltan clips de trote lateral y hacia atrás).
- **IK de pies solo de desniveles** (`IKPiesYMirada.SoloDesnivel`): sube o baja el pie lo que el piso bajo él difiere del piso bajo el centro
  (zona muerta 3 cm, tope 35 cm) y baja la cadera con el pie más bajo. En plano no toca el clip (antes aplanaba la zancada y por eso T-724 lo apagó).
- **Giro suave** (`ReglasAnimacion.GirarSuave`, `ControladorAPie.SuavizadoGiroCuerpo` 14/s, tope 720 °/s): caminando el cuerpo arranca rápido y llega suave;
  apuntando sigue a la cámara sin retraso.
- **Inclinación en curvas** (`ReglasAnimacion.InclinacionEnGiro`, `IKPiesYMirada.InclinarEnGiros`): 30 % de la inclinación física, tope 10°, sobre la cadera
  en el pase de IK (los pies se quedan en su lugar).
- Sin cambiar: velocidades del jugador, aceleración (30 m/s²) y frenado (40 m/s²) de `ConfigControlesAPie_Base`, controlador base, escaleras (AnimacionEscalera).
- Pruebas: EditMode `LocomocionT1484Pruebas` (8). PlayMode ajustadas al trote: JugadorVisualPruebas, ClipsTonyT724Pruebas, AnimacionDireccionT640Pruebas,
  VerificacionPJ01T741Pruebas (y EditMode ClipsTonyT724, ClipsPJ01JugadorT985).
