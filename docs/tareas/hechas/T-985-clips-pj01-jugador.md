# T-985: los 80 clips de PJ01 se usan en el jugador y no queda ningún clip de Mixamo

Pedido de Tony (14-nov, Sesión A, punto 2). Carpetas: Assets/_Proyecto/Personaje/ (incluye Visual/), Assets/Arte/Personajes/Animaciones/ (controlador y overrides,
regenerados con código), Assets/_Proyecto/Tests/EditMode/Personaje*/ y PlayMode/Personaje*/, docs/.
No tocar Mundo, Casino, techos ni pandillas (la otra sesión).

## Qué se pide
1. Auditoría clip por clip: los 80 FBX de `imageassets/blender/pj01_clips/` (copias en `Assets/Arte/Personajes/PJ01/Clips/`) deben estar en un estado del Animator
   del jugador (ControladorBase + Override_Jugador) y ese estado debe alcanzarse en su momento del juego.
2. Ningún clip que no sea PJ01_* en el controlador efectivo del jugador (Mixamo viejo: Burla, Presumir, Carro_Pitar, o cualquier otro que salga en la auditoría).
3. Conectar lo que falte. Conocidos al empezar: PJ01_Pistola_ApuntarCaminar_Derecha/Izquierda (giro en el sitio apuntando), PJ01_Punos_Guardia (sin estado),
   Burla/Presumir/Carro_Pitar con clip de Mixamo en el jugador.
4. Capturas de cada clip en su estado, revisadas.

## Pruebas
EditMode que recorre el controlador efectivo del jugador y falla si hay un clip no PJ01 o si falta uno de los 80. PlayMode de los estados nuevos.

## Resultado (hecha)
Auditoría antes (EditMode `ClipsPJ01JugadorT985Pruebas`, imprime la tabla estado -> clip en el log): 3 clips no PJ01 en el jugador
(Base/Burla -> Skull_Burla, Base/Presumir -> Nova_Flexing, Arriba/Pitar -> Carro_Pitar) y 3 PJ01 sin estado
(Pistola_ApuntarCaminar_Derecha, Pistola_ApuntarCaminar_Izquierda, Punos_Guardia). Los otros 77 ya estaban en estados.
Después: 0 no PJ01, 80/80 usados.

Conectado y cómo se dispara:
- GirarApuntarDer/Izq (base, PJ01_Pistola_ApuntarCaminar_*): desde Idle con Giro > 70 °/s (o < -70), quieto, Apuntando y TipoArma pistola. Va antes que GirarDer/Izq. Sale a Caminar por velocidad, a Idle al dejar de apuntar o al terminar.
- PunosGuardia (capa Arriba, PJ01_Punos_Guardia): parámetro nuevo EnGuardia = VisualPersonaje.EnGuardia (2.5 s tras el último golpe de puños, sin arma levantada). Solo si GuardiaTrasGolpe (lo enciende AnimacionJugador; los enemigos no cambian). Los golpes terminan en la guardia.
- Burla y Presumir del jugador -> PJ01_Punos_Guardia (el jugador nunca los lanza; los otros papeles conservan sus clips). Pitar -> PJ01_Carro_Manejar (decisión: no hay clip PJ01 de claxon; los brazos quedan en la pose de manejar).
- Huérfano hallado: VerTelefono (PJ01_Inter_VerTelefono) solo lo ponían los NPC. GestosJugador.SacarTelefono ahora pone ViendoTelefono y lo quita cuando el control a pie vuelve (pantalla del teléfono cerrada).
- Revisado por código que sus parámetros se ponen desde el juego: ConImplante (AnimacionJugador), salto/caída (EnSuelo/VelY), agachado, rodar, pistola/rifle (TipoArma, Apuntando, Disparar, Recargar), golpes, muertes/levantarse (Caido), carro (AnimacionCarro), moto (PoseMoto), cobertura, interacciones (GestosJugador).

Pruebas: EditMode completo 2367/2367. PlayMode con filtro (ClipsPJ01JugadorT985, ControladorBaseVisual, RetargetPJ01T749, MotoBajarSuaveT976, CarroPJ01T963, AnimatorPJ01T733): 40/40, 0 fallos.
Capturas: docs/capturas/t985/PJ01_*.png (80, una por clip jugada en su estado del Animator del jugador, herramienta CapturasJugadorT985) y docs/capturas/t985/_hoja.png (hoja de contacto, revisada: sin T-pose ni vacías).
