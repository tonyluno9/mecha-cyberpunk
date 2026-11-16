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

## Pendiente
- Las espaldas de las casas (caras laterales de las cajas "Cuerpo" hechas con KitS2c.Losa/MallaCaja) tienen el orden de vértices invertido: desde afuera no se dibuja la pared y se ven las fachadas de atrás a través. Las escaleras nuevas quedan pegadas a muros que se ven huecos desde la calle trasera. Corrección sugerida (tarea de arte): invertir el giro de las 4 caras laterales en MallaCaja o poner _Cull=Off en S2c_Cuerpo_*.
- TreparAPie no limpia puntoCercano si el jugador se teletransporta dentro del trigger de una escalera (solo teletransporte, no pasa jugando).
