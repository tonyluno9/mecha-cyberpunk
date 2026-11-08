# T-739 PJ01 caminar y correr con y sin implante (A8)
Dueño: Tony | Rama: tony/T-739-pj01-implante-locomocion | Integrada en tony/s1-09nov
Fuente: imageassets/blender/animaciones_implantes (LEEME.txt).

## Clips
Se importaron los cuatro a Animaciones/Locomocion/ como Humanoid con Loop Time: Loco_Caminar_SinImplante, Loco_Caminar_ConImplante, Loco_Correr_SinImplante, Loco_Correr_ConImplante
(el regex de bucle de ImportadorMixamo ahora acepta Correr\w*; ImportadorMixamo.AplicarLocomocionImplantes los vuelve a aplicar).
Escala del esqueleto (el problema de los de agachado, T-733): medida con MedirImplantesT739 y con prueba EditMode, la cadera local mide igual que la de Loco_Caminar / Loco_Correr
(1.016 y 0.945), así que NO hizo falta copiar la descripción de otro avatar.
Duración: caminar 1.033 s (los dos), correr 0.700 s (los dos). Diferencia Sin contra Con sobre PJ01: caminar 12.6 grados máx. (LeftUpperArm), correr 13.3 (LeftUpperArm): son los brazos.

## Duplicados: lo que dice el LEEME no coincide con el proyecto
El LEEME dice que Loco_Caminar y Loco_Correr del proyecto ya son los ConImplante. Medido, NO lo son: Loco_Caminar dura 1.292 s y avanza a 0.997 m/s; Caminar_ConImplante dura 1.033 s y avanza a 1.284 m/s
(y los md5 son distintos; sobre PJ01 difieren hasta 15.6 grados en caminar y 33 en correr). Por eso se importaron los cuatro y Loco_Caminar / Loco_Correr se quedan para los demás papeles (aliados, enemigos, NPC),
que no cambian. Si Tony confirma que debían ser los mismos, se borran dos clips y se apunta el árbol a Loco_Caminar / Loco_Correr.

## Decisión de diseño (la más limpia)
- Parámetro de Animator ConImplante (float 0 o 1). El estado Caminar y el Correr del controlador base son un árbol 1D sobre ConImplante con dos árboles 2D (direcciones): el 0 usa Loco_Caminar / Loco_Correr y el 1,
  Loco_Caminar_ConImplante / Loco_Correr_ConImplante (atrás y de lado son los mismos clips en los dos). Cambiar el parámetro no reinicia el Animator ni crea objetos en juego.
- El override del papel Jugador (ClipsPorPapel) cambia Loco_Caminar y Loco_Correr por los SinImplante. Los demás papeles dejan ConImplante en 0 y siguen con Loco_Caminar / Loco_Correr, como antes.
- AnimacionJugador pone ConImplante cada vez que cambia: 1 si Registro<IImplantes>.Actual (contrato de Core, lo implementa ServicioImplantes y lo instala RegaloImplanteDoc/la clínica) tiene Nivel(Piernas) > 0 (IMP01)
  o Nivel(Brazos) > 0 (IMP02); ojos y sistema nervioso no cuentan. Sin servicio registrado se actúa como sin implante.
- Por defecto (sin implantes) el jugador usa los SinImplante: Tony debe saber que al empezar la partida caminar y correr cambian respecto a la versión anterior (era Loco_Caminar 0.997 m/s).

## Velocidades
Medidas con MedicionClips (misma regla que T-733/T-736), a escala 1: caminar sin 1.293, con 1.284; correr sin 3.190, con 3.052 m/s (ReglasAnimacion.VelocidadClip*Implante, con prueba EditMode).
El jugador sigue con Animator.speed 1 (T-724: clip tal como viene), así que no se aplican al Animator; quedan para quien quiera ajustar el paso (el cuerpo va a 4.5 y 7.5 m/s, los pies siguen resbalando como se avisó).

## Retroceso del disparo (pedido del coordinador)
AgarrePistolaPJ01 suma un salto corto al disparar: el cañón sube hasta 4 grados y baja en los primeros 0.1 s del clip Pistola_Disparo (RetrocesoGrados, 0 lo apaga). Prueba en AgarreFinalPJ01T738Pruebas.

## Pruebas
- EditMode ImplanteLocomocionT739EditPruebas (clips Humanoid con bucle, escala, velocidades, controlador y overrides); se ajustaron ClipsTonyT724 y ControladorBase a la nueva estructura. EditMode 2124/2124.
- PlayMode ImplanteLocomocionPJ01T739Pruebas 3/3: cambia el implante (piernas nivel 1 y 2, brazos, ojos y sistema nervioso, sin servicio) y comprueba el clip activo de la capa base al caminar y al correr.
- Capturas: docs/capturas/pj01-implante-loco-{caminar-sin,caminar-con-piernas,correr-sin,correr-con-brazos}-{frente,lado}.png (con implante los brazos van separados del cuerpo).
