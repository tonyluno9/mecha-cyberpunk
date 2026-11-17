# T-1316 Pantallas ARRESTADO, HAS MUERTO y carga tipo GTA

Origen: docs/playtest-17nov.md, punto 16 (sesión B). Áreas: UI/ (Historia, Inicio) y dos líneas en Progresion/Prologo (textos).

## Hecho
- `PantallaAviso` (UI/Historia) como M33/M34: el juego se ve detrás atenuado (fondo al 55 %), panel de esquinas cortadas con marco
  del color del aviso, título Teko con neón y glitch, detalle de dos líneas (la segunda más chica). Tras 1.7 s ya visible el fondo
  se cubre del todo en 0.45 s (antes de que el arresto mueva al jugador a casa; la muerte ya tenía su fundido).
- ARRESTADO: "-25 %   $ 778" (el porcentaje en rojo) y "Vuelves a casa" (ConsecuenciaArresto). HAS MUERTO: rojo, con viñeta roja
  oscura en las orillas y "Despiertas en la clínica del Doc" (DoctorClinica, DatosDoctor).
- Carga: `FotosCarga` + `ReglaFotosCarga` (UI/Inicio) muestran L01-L06 (Resources/Carga) a pantalla completa, empezando en una al
  azar: 6.5 s cada una, zoom de 1.06 a 1.16, paneo lento en una dirección propia y fundido de 1.3 s a la siguiente. El neón animado
  de antes queda al 20 % encima. El logo, el consejo y la barra no cambian.

## Pruebas
- EditMode `ReglaFotosCargaT1316Pruebas` (turno, fundido, zoom, el paneo nunca destapa la orilla); EditMode UI completo 340/340.
- PlayMode `PantallasT1316Pruebas` 3/3 (con InicioPruebas 14/14), ArrestoT690Pruebas 2/2, HasMuertoT689Pruebas 1/1.
- Capturas revisadas en docs/capturas/t-1316/ (arrestado y has-muerto sobre una foto real de la calle; carga-1 y carga-2).
