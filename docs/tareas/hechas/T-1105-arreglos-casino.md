# T-1105: arreglos del casino (punto 14)

Pedido de Tony: la mesera del bar de fichas está al revés; Sergio (el Hacker) con la silla al revés y lejos de su compu, moverlo a otra esquina y sentado bien; las sillas de las mesas de frente a su mesa; la puerta de entrada vista desde dentro con la misma textura que el resto del casino. Lo demás (lámparas del techo, máquinas, meseras que caminan) no se toca.

## Qué estaba mal y qué se hizo
- Cajera `Cajera_R01`: el modelo R01 tiene el frente en -Z local y estaba girada 180. Giro 0 (`ColocarT930Casino.GiroCajera`): mira al salón (-z). La charola (`BrazoCharola` mide el cuerpo, no la raíz) y `CajeraMundo` siguen bien.
- Sillas de las mesas I11: el frente del modelo I12 es -Z local (respaldo en +Z, medido en la malla). La silla del centro ya miraba bien, las laterales giraban el lado contrario. Ahora cada una usa `ColocarT1105Casino.YawSilla` (LookRotation hacia el centro de su mesa); `ColocarS2oCasino` usa lo mismo.
- Sergio (corregido): sentado de verdad. La silla I12 de Sergio va a escala 0.7 (el asiento de 0.62 m era más alto que sus piernas) y `PoseSentado.alturaCadera` queda en asiento + 0.1 m; la prueba mide, tras 2 s de Play, cadera entre asiento+0.05 y +0.25, muslos horizontales, pies cerca del piso y silla de frente al escritorio (dot > 0.9). Las capturas se leen dos frames después del render automático de la cámara (antes se tomaban antes de la pose de LateUpdate y se veía parado). Original: su escritorio I02 tiene el lado de uso hacia +x con yaw 270; la silla estaba con el frente al lado contrario (de espaldas al escritorio) y él (su frente de modelo es su Visual.forward, que usa `PoseSentado`) también. Se movió escritorio, silla, `LuzHacker` y `Apoyo_SERGIO` (con su `PersonajeApoyo`, así que la interacción se movió con él) a la esquina noroeste: escritorio pegado al muro oeste en (61.6, 38.5), silla en (62.9, 38.5) con yaw 90 (frente al escritorio) y Sergio sentado en ella mirando -x. Esquina elegida con un mapa de ocupación de renderers del salón (sin máquinas ni mesas; a 12 m de la entrada y 20 m de la barra). Sentado: `PoseSentado` (pose por huesos de T-639); no hay clip de sentado en el proyecto.
- Puerta de entrada: desde dentro, sobre el arco dorado se veía la fachada ocre con ventana (hueco del muro frontal entre `MuroFrenteIzq` y `MuroFrenteDer`). Panel `T1105_MuroSobreArco` (z 45.5 a 48.5, y 3.0 a 5.7, grosor 0.3 al ras del muro) con la textura del muro del casino a su escala (`T1105_MuroSobreArco.mat`). Empieza donde acaba la hoja de la puerta (no tapa el paso) y queda detrás de `CuerpoTecho` visto desde la calle.
- `Assets/Arte/Editor/ColocarT1105Casino.cs` (idempotente, edita la escena en su lugar, sin rehacer el casino). `ColocarT930Casino` usa sus constantes para que correrlo no deshaga los cambios.

## Criterios
- [x] Cajera mira a -z (frente medido con los muslos del cuerpo animado, dot con -z > 0.9).
- [x] Cada una de las 12 sillas mira a su mesa a menos de 20 grados.
- [x] Sergio a menos de 0.6 m de su silla, cadera a la altura del asiento, mirando a su pantalla a menos de 25 grados.
- [x] Se habla con Sergio con E desde 1.5 m (entrada simulada, `HackerEsquinaT1105Pruebas`).
- [x] Rayos desde el centro del casino hacia la zona sobre el arco pegan en material de muro del casino; el panel queda tapado por la fachada desde la calle.

## Pruebas (con Unity en batchmode)
- Nuevas: `CasinoArreglosT1105Pruebas` (PlayMode, 6 de 6 pasan, más la captura) y `HackerEsquinaT1105Pruebas` (PlayMode, 1 de 1).
- Ajustada: `HackerSentadoPruebas` (ya fallaba desde T-930 porque `Set_Hacker` está apagado; ahora usa el escritorio I02): 3 de 3.
- Sin romper: MeseraCharolaT930 1/1, CapturasCasinoT930 1/1, CasinoInterior 7/7, MeserasCasinoS2p 2/2, CasinoPuertaS2o 1/1, MeseraDeFrenteS2 1/1, FichasYCajera 11/11, TragamonedasMundo 9/9, PersonajesApoyo 1/1, RevisarCiudadT995 (EditMode) 2/2.
- EditMode del área Mundo: 365, 364 pasan, 1 falla ya existente en main (`LuzK01S2oPruebas.Postes_ElNaranjaQuedaBajoCadaPoste`, de T-1102, ajena).
- RecorridoDemoPruebas (3 pruebas: NavMesh 1/1 y VideoDemoT979 1/1 pasan; DemoDePrincipioAFin falla): se atora en el tramo 09 EL INVENTOR (jugador atorado en (-101,-13) camino a (-74.5,13.5), lejos del casino), así que no llega a EL HACKER; no tiene que ver con esta tarea (no corrí la base para compararlo). El tramo del Hacker (E desde 1.5 m) lo cubre HackerEsquinaT1105Pruebas.

## Capturas (`docs/capturas/t-1105/`)
cajera-desde-el-salon, sergio-de-lado, sergio-por-detras, sergio-tres-cuartos, esquina-de-sergio, mesas-desde-arriba, entrada-desde-dentro.

## Pendiente
- Un clip de sentado de verdad para Sergio (hoy es pose por huesos).
