# Sonidos con archivo y sin gancho en el código (T-470)

Fuente: prueba `IdsSonidoPruebas` (escribe Logs/sonidos-ids.txt). Cada ID tiene su archivo final en `Assets/Arte/Audio/<SEC>/<ID>_NN.wav`;
falta la llamada `Sonidos.ReproducirEn("ID", posicion)` / `Sonidos.Reproducir("ID")` (Core/Sonido). Para la Sesión A.
Rutas desde `Assets/_Proyecto/`. Las variantes `_01`.. se eligen solas.

| ID(s) | Archivo .cs | Método donde debería sonar |
| --- | --- | --- |
| ARM_FUS_DISPARO, ARM_FUS_RECARGA | Personaje/Combate/PistolaAPie.cs | `IdDisparo` / `IdRecarga`: hoy piden ARM_SUB_* (sin archivo final); el catálogo ya los redirige a ARM_FUS_* (alias). Renombrar a ARM_FUS_* |
| ARM_FUS_RAFAGA_COLA, ARM_ESC_BOMBEO | PistolaAPie.cs | `Update` al soltar el gatillo del fusil / tras cada disparo de escopeta (bombeo) |
| ARM_PIS_SACAR, ARM_MEJORA | Personaje/Combate/ArmasAPie.cs | al cambiar a la pistola (junto a ARM_CAMBIAR) / al mejorar un arma |
| ARM_SNI_CERROJO | PistolaAPie.cs | `Disparar` del sniper, 0.5 s después del disparo |
| ARM_CASQUILLO | PistolaAPie.cs | `Disparar`, al caer el casquillo (ReproducirEn en el suelo) |
| ARM_BALA_IMPACTO_METAL | PistolaAPie.cs | `Disparar`, junto a ARM_BALA_IMPACTO_CONCRETO cuando el impacto es en un carro o metal |
| ARM_BALA_ZUMBIDO, ARM_DISPARO_LEJANO | IA/APie/PistolaPandillero.cs | al disparar el pandillero: zumbido cerca del jugador, disparo lejano si está a más de 30 m |
| CAL_BAJA_NIVEL | Progresion/Calor/ServicioCalor.cs | donde baja el nivel de calor (pareja de CAL_SUBE_NIVEL) |
| CAL_DRON_DISPARO, CAL_DRON_ESCANEO, CAL_DRON_ZUMBIDO | Trafico/Persecucion/DronMundo.cs | `Disparar`; escaneo en `Update`; zumbido en bucle (`Sonidos.ReproducirBucle`) |
| CAL_PATRULLA_SIRENA, CAL_PATRULLA_MEGAFONO | Trafico/Persecucion/PatrullaMundo.cs | `Sirena` (bucle) y al detectar al jugador |
| ECO_DINERO_RECOGER, ECO_CASINO_TRAGAMONEDAS | Economía (donde se suma dinero recogido) / Casino/ | al recoger dinero del suelo; al girar la tragamonedas |
| JUG_PASO_ASFALTO_MOJADO, JUG_PASO_INTERIOR, JUG_PASO_METAL | Personaje/Movimiento/ControladorAPie.cs | donde hoy suena JUG_PASO_ASFALTO: elegir por superficie o por IdLugar |
| JUG_CORRER_RESPIRA, JUG_ROPA_MOVER, JUG_VIDA_BAJA, JUG_REGENERA, JUG_CAE_DERROTADO | ControladorAPie.cs, Personaje/Vida/VidaAPie.cs | respiración al correr mucho; ropa al moverse; vida < 25 %; al regenerar; al quedar KO |
| PUN_BLOQUEO, PUN_GOLPE_FUERTE, PUN_PATADA | Personaje/Combate/GolpeAPie.cs | junto a PUN_GOLPE_*: fuerte si es el golpe cargado, patada si es la patada; bloqueo en `CubiertaYRodada` |
| PAN_ALIADO_CAE, _ESPERA, _SIGUE, _SUBE_NIVEL | IA/Aliados/ (Reclutable.cs y su seguimiento) | al caer, al quedarse, al seguirte, al subir de nivel |
| PAN_JEFE_CAE, PAN_RIVAL_DANIO, PAN_RIVAL_PROVOCA | IA/APie/JefePandilla.cs, PandilleroAPie.cs | al morir el jefe; al recibir daño; al provocar antes de atacar |
| PAN_SILBIDO_LLAMAR, PAN_VECINO_AUXILIO | IA/Aliados/, Trafico/Peatones/ | silbido al llamar aliados; el vecino pide auxilio al ver una pelea |
| UI_CONTINUARA, UI_TELEFONO | UI/Historia/ (pantalla CONTINUARÁ), UI del teléfono | al mostrar la pantalla; al sonar el teléfono |
| VEH_CHOQUE_FUERTE, VEH_CHOQUE_LEVE | Vehiculos/Carro/ (OnCollisionEnter) | según la velocidad del impacto |
| VEH_CLAXON_VIEJO, VEH_CLAXON_TRAFICO | Vehiculos/Carro/CarroViejo.cs, Trafico/ | tecla de claxon; tráfico al frenar |
| VEH_FORZAR_PUERTA, VEH_PUERTA_ABRIR, VEH_PUERTA_CERRAR | Vehiculos/Carro/CarroEstacionado.cs | `AlCambiarOcupado` y al forzar la cerradura |
| VEH_VIEJO_ARRANQUE, VEH_VIEJO_MOTOR_ACELERA | CarroViejo.cs | al subir; mezclar con el bucle de ralentí al acelerar |
| VEH_FRENO_DERRAPE, VEH_LLANTAS_MOJADO | CarroViejo.cs | al frenar fuerte; al rodar sobre calle mojada |
| VEH_MOTO_MOTOR, VEH_PRO_MOTOR_ACELERA, VEH_PRO_MOTOR_RALENTI, VEH_PASA_CERCA | Vehiculos/ (moto y carro pro, aún sin clase) / Trafico | motor en bucle con `FijarTono`; PASA_CERCA al cruzar un carro de tráfico junto al jugador |
| ZON_ENTRAR_PROPIA, ZON_ENTRAR_RIVAL | Progresion/Territorio/ServicioTerritorio.cs (o AvisoZona del HUD) | al cambiar de zona el jugador, según el dueño |
| MUN_* (AIRE_ACONDICIONADO, BASURERO_GOLPE, CAMA, CHISPAS_ELECTRICAS, CORTINA_METALICA, GENERADOR, HOLOGRAMA, NEON_ZUMBIDO, PERRO_LADRA, PUERTA_CASA_ABRIR/CERRAR, REJA, SOLDADORA, VIDRIO_ROTO) | Mundo/ (Alan) y Progresion/Prologo (casa, cama) | emisores 3D en bucle (`ReproducirBucle` con el Transform del objeto) o puntuales al interactuar: puerta de la casa, cama, reja y cortina del mercado |
| AMB_CALLEJON, AMB_LLUVIA_SUAVE | GestorSonidos / ReglaAmbienteZona | nadie avisa "callejón" ni lluvia; cuando exista un `IdLugar` o zona de callejón, agregar el caso en `ReglaAmbienteZona.Elegir` |
