# Sonidos de CYBERFIX (T-407)
Los IDs son los de la LISTA 1 de docs/listas-equipo-cyberfix.md. El `GestorSonidos` (Progresion/Sonido) los toca por `Sonidos.Reproducir*` (Core/Sonido).
Un ID sin archivo no suena y avisa una sola vez. Los WAV finales van en `Assets/Arte/Audio/<SEC>/<ID>.wav` (o `<ID>_01.wav`...); los builds corren
`CreadorCatalogoSonidos.Crear()` antes de construir y entran solos. Si hay final, gana sobre el provisional (`Assets/Arte/Audio/Provisional/<SEC>/`, generado por código).
Volumen por sección (General x sección): Ambiente y Música arrancan en 0 (silencio por defecto, plan de la noche); el AmbienteSonoro de Alan está apagado en la escena.

| ID | Conectado en | Provisional / final |
| --- | --- | --- |
| ARM_PIS_DISPARO | Personaje/Combate/PistolaAPie | provisional |
| ARM_ESC_DISPARO | PistolaAPie | provisional |
| ARM_SUB_DISPARO | PistolaAPie | provisional |
| ARM_PIS_RECARGA | PistolaAPie | provisional |
| ARM_ESC_RECARGA_CARTUCHO | PistolaAPie | provisional |
| ARM_SUB_RECARGA | PistolaAPie (fusil de asalto; el ID no se renombra) | provisional |
| ARM_SNI_DISPARO | PistolaAPie (sniper, T-416) | provisional |
| ARM_SNI_RECARGA | PistolaAPie (sniper, T-416) | provisional |
| ARM_SNI_ZOOM | Personaje/Combate/ZoomSniper (T-416) | provisional |
| ARM_VACIO | PistolaAPie | provisional |
| ARM_CAMBIAR | Personaje/Combate/ArmasAPie | provisional |
| ARM_BALA_IMPACTO_CONCRETO | PistolaAPie | provisional |
| ARM_BALA_IMPACTO_CARNE | PistolaAPie | provisional |
| ARM_LASER_AVISO | IA/APie/PistolaPandillero | provisional |
| PUN_GOLPE_AIRE | Personaje/Combate/GolpeAPie | provisional |
| PUN_GOLPE_CARNE | GolpeAPie | provisional |
| JUG_PASO_ASFALTO | Personaje/Movimiento/ControladorAPie | provisional |
| JUG_SALTO | ControladorAPie | provisional |
| JUG_CAER | ControladorAPie | provisional |
| JUG_DANIO | Personaje/Vida/VidaAPie | provisional |
| PAN_RIVAL_ALERTA | IA/APie/PandilleroAPie | provisional |
| PAN_RIVAL_CAE | PandilleroAPie | provisional |
| PAN_JEFE_ENTRADA | IA/APie/JefePandilla | provisional |
| PAN_ALIADO_RECLUTA | IA/Aliados/Reclutable | provisional |
| VEH_ALARMA_ROBO | Vehiculos/Carro/CarroEstacionado | provisional |
| VEH_VIEJO_MOTOR_RALENTI | Vehiculos/Carro/CarroViejo (bucle, tono por rapidez) | provisional |
| CAL_SUBE_NIVEL | Progresion/Calor/ServicioCalor | provisional |
| ZON_RESPETO_SUBE | Progresion/Territorio/ServicioTerritorio | provisional |
| ZON_CONQUISTADA | ServicioTerritorio | provisional |
| ECO_COMPRA | UI/Tiendas/PantallaTienda | provisional |
| ECO_SIN_DINERO | PantallaTienda | provisional |
| ECO_ARMA_RECOGER | Personaje/Combate/ArmaEnPiso | provisional |
| ECO_CLINICA_DESPERTAR | Progresion/Prologo/Clinica/DoctorClinica | provisional |
| UI_MOVER | PantallaTienda | provisional |
| UI_ACEPTAR | PantallaTienda | provisional |
| UI_CANCELAR | PantallaTienda | provisional |
| AMB_CALLE_NOCHE | Progresion/Sonido/GestorSonidos (por lugar y zona, cruce de 1 s) | sin archivo (apagado, sin provisional) |
| AMB_INTERIOR_CASA | Progresion/Sonido/GestorSonidos (por lugar y zona, cruce de 1 s) | sin archivo (apagado, sin provisional) |
| AMB_MERCADO_NEGRO | Progresion/Sonido/GestorSonidos (por lugar y zona, cruce de 1 s) | sin archivo (apagado, sin provisional) |
| AMB_TIANGUIS | Progresion/Sonido/GestorSonidos (por lugar y zona, cruce de 1 s) | sin archivo (apagado, sin provisional) |

Faltan de conectar (T-408 a T-413): UI_ERROR, UI_OBJETIVO_*, UI_TITULO_INICIO, UI_CONTINUARA, UI_GUARDADO, ARM_ESC_BOMBEO, JUG_PASO_ASFALTO_MOJADO/INTERIOR y el resto de los P1 de la lista.
