# T53: qué clip usa cada estado (Override_HumanoJugador = Override_Jugador, clip por clip)

El override se genera copiando los pares de Override_Jugador (ver HumanoJugadorT53.CrearOverride); la lista exacta sale en docs/capturas/_tareas/T53/clips_override.txt al correr Aplicar().

| Estado | Clip |
| --- | --- |
| Idle | PJ01_Loco_Idle |
| Caminar (mezcla 1D por Velocidad, 1.62 m/s) | PJ01_Caminar |
| Trote (4.53 m/s) | PJ01_Trote (atrás/derecha/izquierda: PJ01_Trote_Atras/Derecha/Izquierda) |
| Correr rápido (6.25 m/s) | PJ01_CorrerRapido |
| Saltar | PJ01_Salto_Despegue, PJ01_Salto_Aire, PJ01_Salto_Aterrizaje (caída larga PJ01_Loco_Caida) |
| Agacharse | PJ01_Loco_Agacharse / PJ01_Loco_Pararse; agachado: PJ01_Loco_AgachadoIdle, AgachadoCaminar, AgachadoAtras, AgachadoLateral_* |
| Cubrirse | PJ01_Cobertura_Entrar, PJ01_Cobertura_Disparo y Disparo2 |
| Sacar / guardar pistola | PJ01_Pistola_Sacar / PJ01_Pistola_Guardar (eventos AlSacarPistola / AlGuardarPistola) |
| Sacar / guardar arma larga | PJ01_Rifle_Sacar / PJ01_Rifle_Guardar (T60: eventos nuevos, EventosArmaLargaT60.Aplicar) |
| Apuntar | PJ01_Pistola_Apuntar; PJ01_Rifle_Apuntar |
| Caminar apuntando (adelante, atrás, lados) | PJ01_Pistola_Caminar / PJ01_CaminarApuntando_Atras / _Derecha / _Izquierda; arma larga PJ01_Rifle_ApuntarCaminar |
| Disparar | PJ01_Pistola_Disparo; PJ01_Rifle_DisparoCaminar |
| Recargar | PJ01_Pistola_Recargar, PJ01_Pistola_RecargarCaminar; PJ01_Rifle_Recargar |
| Golpes | PJ01_Golpe1_Directo (der), PJ01_Golpe2_Gancho (izq), PJ01_Punos_Fuerte; guardia PJ01_Punos_Guardia |
| Daño | PJ01_Dano_Frente, PJ01_Dano_Espalda (aditivos), PJ01_Pistola_Dano_Fuerte |
| Muertes | PJ01_Muerte_Frente, PJ01_Muerte_Espaldas, PJ01_Muerte_Desmayo, PJ01_Muerte_Explosion/Atropello/CaidaAltura |

## Reglas de combate del enemigo (ReglaArmaEnemigo, PandilleroAPie)
Guardada -> pelea: Sacando (SacarArma + sonido de sacar; espera visual.ArmaEnPose o 1.2 s) -> Lista (dispara; cargador pistola 12, escopeta 6, subfusil 30)
-> sin balas: Recargando (visual.Recargar, 2.2 s, sonido de recarga del arma) -> Lista. Sin pelea 3 s: GuardarArmaYSalirDePelea.
Sonido de disparo = SonidosArmasT1472.IdDisparo del arma (ARM_W01 pistola, ARM_W02 escopeta, ARM_SUB subfusil), 3D en la boca. Apuntando o sacando: velocidad maxima 1.8 m/s;
la velocidad del Animator es la real del CharacterController (VelocidadVista), nunca la pedida.
