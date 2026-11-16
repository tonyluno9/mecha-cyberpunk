# T-1101: escaleras de mano para subir a los techos de las casas

Rama: tony/T-1101-escaleras-techos. Pedido de Tony (playtest 16-nov, punto 10).

## Diagnóstico
Prueba nueva EscalerasTechosT1101Pruebas (Ciudad_Prologo real, jugador real, W simulada, desde 1,5 m frente a cada escalera): las 17 escaleras viejas (14 de S2_Techos/Accesos y 3 de rutas de barda) suben todas a un techo con piso en menos de 8 s. No hubo que arreglar ninguna vieja. Los fallos intermitentes que aparecieron en la prueba eran de la prueba, no del juego:
- TreparAPie recuerda el punto cercano y un teletransporte no manda OnTriggerExit: el jugador se enganchaba a la escalera anterior (la prueba reinicia el componente tras colocar al jugador).
- Las pandillas de la calle (GrupoAcoso) disparan al jugador y lo dejan KO mientras sube (la prueba las apaga).

## Qué se hizo
- ColocarT1101Escaleras (Arte/Editor, menú "Mecha Cyberpunk/S2/T-1101 Escaleras de mano en casas", idempotente, raíz T1101_Escaleras): sondeo por física de las paredes de todas las casas (S2c_Suburbios, S2c_CalleDorada, S2c_Mercado, Barrio): pared lisa a 3 alturas, nada visible ni sólido pegado a la pared (OverlapBox + bounds de renderers), 1,7 m de banqueta plana delante (>= 1,2 m libres de paso), techo plano con piso y capsula libre en la salida, casa sin acceso previo, >= 4 m de cualquier puerta/interactuable, >= 5 m de otros accesos, fuera de la franja x=-110.5 +-8 (T-1102).
- 13 escaleras nuevas con el mismo modelo greybox metálico de ColocarS2Techos (largueros y peldaños cada 0,3 m, material S2b_EscaleraGreybox) y su PuntoTrepar: Suburbios 5, CalleDorada 3, Mercado 5, Barrio 0.
- Barrio quedó en 0: sus muros tienen fachadas, letreros o cornisas pegados, o caen en la franja de T-1102.

## Criterios
- [x] Todas las escaleras suben (30 de 30: 17 viejas + 13 nuevas) en <= 8 s: EscalerasTechosT1101Pruebas.
- [x] >= 12 nuevas repartidas: 13, con modelo visible y a >= 4 m de las 35 puertas e interactuables.
- [x] 3 casas nuevas: 2 s caminando por el techo con W sin atravesarlo.
- [x] Capturas docs/capturas/t-1101/ (4 escaleras desde la calle a 1,7 m, 2 desde el techo) revisadas.

## Evidencia
- PlayMode EscalerasTechosT1101Pruebas: 4 total, 4 pasan, 0 fallan (con T1101_CAPTURAS=1). TechosS2Pruebas 6/6, TreparAPiePruebas 5/5, EmboscadaTechoT999Pruebas 5 (4 pasan, 1 saltada: captura).
- EditMode RevisarCiudadT995Pruebas|FlotantesS2tPruebas: 3/3. verificar.sh compilar: OK.

## Correcciones tras la revisión del líder
- Paredes invisibles: KitS2c.MallaCaja tenía invertido el triángulo de las 4 caras laterales (cross(eu, ev) opuesto al normal). Corregido en KitS2c (Cara voltea el orden si hace falta) y en los assets ya generados con CorregirCarasT1101 (Arte/Editor, menú S2, idempotente): recorre las mallas .asset de Assets/Arte, solo cajas de 24 vértices y 12 triángulos, y voltea el triángulo cuyo normal geométrico contradice el del vértice (245 mallas, 1960 triángulos; los normales no cambian, ya apuntaban hacia afuera). Sin re-correr colocadores de la ciudad, sin _Cull Off. Antes/después: escalera-1 ya no deja ver las casas de dentro de la manzana.
- Esquirlas negras en techo-1: eran el modelo del propio jugador visto por dentro (la cámara de la foto está en su cabeza). Los renderers del jugador se ocultan durante la foto; no hay basura en la escena (cables, lluvia y partículas descartados). Verificado en 5 techos.
- Capturas nuevas (11): 4 escaleras desde la calle, 2 desde el callejón (callejon-1/2), 5 desde techos distintos (techo-1 a 5).
- Pruebas tras la corrección: EscalerasTechosT1101Pruebas 4 total, 3 pasan, 0 fallan, 1 saltada (captura, sin T1101_CAPTURAS); TechosS2Pruebas 6/6; EditMode RevisarCiudadT995Pruebas|FlotantesS2tPruebas|SinGreyboxT506Pruebas 12/12; compilar OK.

## Pendiente
- TreparAPie no limpia puntoCercano si el jugador se teletransporta dentro del trigger de una escalera (solo teletransporte, no pasa jugando).
- Barrio quedó sin escaleras nuevas (muros con fachadas pegadas o en la franja de T-1102).
