# T-193 La clínica: mostrador, pantalla e instalar, y la escena Pruebas_Implantes
Dueño: Tony | Rama: tony/T-193-clinica | Modelo: Sonnet 5.5, medium
Depende de: T-191, T-192

## Objetivo
A pie, con E en el mostrador de la clínica, se abre una pantalla con los 4 espacios: ves lo que traes, lo que sigue, cuánto cuesta y qué
hace, y el implante "probado" en tu cuerpo antes de pagar. Al instalar se cobra, la pantalla funde a negro, pasan 2 horas y despiertas con
el implante puesto. Queda armada `Pruebas_Implantes.unity`, la escena de todo el bloque.

## Contexto
- Diseño: personaje-progresion.md ("Los implantes se instalan en una clínica dentro del mercado negro"); ciudad.md (el mercado negro abre de
  día); estetica-hud.md (pantallas a pie: paleta turquesa, magenta, naranja).
- Decisiones: D-tony-81 (los mostradores son prefabs de Tony que Alan coloca; abierto según el `ILugar` con su `IdLugar`, sin él abierto),
  D-tony-87 (escena de pruebas con teclas), D-tony-132 (pantalla de piezas: filas, gris, avisos en naranja suave, Enter que abre no cuenta).
  Propuestas: D-tony-165 (tabla), D-tony-166 (reglas de la clínica), D-tony-172 (escena y ensamblado de pruebas).
- Contratos: `IImplantes`, `IInteractuable`, `ILugar` (`IdLugar.Clinica`), `ICartera`, `IRelojMundo.AvanzarHasta`, `IPuntoAparicion`.
- Código: `ServicioImplantes`, `ReglaImplantes` (T-191); `CuerpoJugador.MostrarPrueba` y `ImplantesDePrueba` (T-192);
  `UI/Tiendas/MostradorTienda.cs` y `PantallaTienda.cs` (modelo de mostrador y pantalla), `UI/Taller/PantallaPiezasTaller.cs` (filas por ranura,
  gris, avisos), `UI/Misiones/CongelaControlAPie.cs`, `UI/Historia/PantallaFundido.cs` y `PantallaSubtitulos.cs`,
  `Personaje/Prefabs/VistaPreviaApariencia.prefab`; `Progresion/Prologo/Editor/CreadorEscenaEconomia.cs` (taller, arena con
  `MostradorPeleas.prefab`, mercado) y `CreadorEscenaMisiones.cs` (letreros sin deformar, lugares, captura aérea), `CreadorEscenaPrologo`
  (ayudas: cajas, lugares, apariciones, jugador).

## Qué hacer
1. `UI/Clinica/MostradorClinica.cs` (`IInteractuable`, texto "E · CLÍNICA"; "CLÍNICA · CERRADA" sin abrir si el `ILugar` Clinica está cerrado;
   sin ese lugar, abierta) y `UI/Clinica/PantallaClinica.cs` con `UI/Datos/ConfigPantallaClinica` (textos y colores):
   - Título "CLÍNICA DEL DOC" y el dinero. Una fila por espacio: "OJOS · OJOS DE LECHUZA" (lo instalado, o "—") y a la derecha la opción
     siguiente según `ReglaImplantes`: "OJOS DE ÁGUILA · $3,500" (si ya tienes el 1, "PAGAS LA DIFERENCIA"), "SE ABRE EN LA LIGA 3" en gris,
     "NO TE ALCANZA" en naranja suave, o "LO MEJOR QUE HAY".
   - Abajo, la descripción de la opción elegida ("CORRES Y SALTAS MÁS"). A un lado, la vista previa del cuerpo con la opción elegida puesta
     de prueba (`MostrarPrueba`), que se quita al mover la elección o al salir.
   - ↑ ↓ eligen (sin dar la vuelta); Enter sobre una opción `Instalar` pide confirmar ("¿INSTALAR POR $2,500? ENTER SÍ · ESC NO"); el segundo
     Enter cobra con `ServicioImplantes.Instalar`; en gris no hace nada; sin dinero avisa y no cobra. Esc sale sin cobrar y el control vuelve
     un cuadro después. El Enter o la E que abren no cuentan.
   - Al instalar: fundido a negro (≈1.5 s), `AvanzarHasta(hora + 2)`, subtítulo "EL DOC: Listo. No lo andes presumiendo." y el HUD a pie
     avisa "IMPLANTE INSTALADO: <NOMBRE>"; la pantalla se cierra. Sin `PantallaFundido` en la escena, se instala sin fundido.
   - Prefab `UI/Prefabs/MostradorClinica.prefab` con su creador idempotente en `UI/Editor/` (como `CreadorMostradorTienda`).
2. `Progresion/Prologo/Editor/CreadorEscenaImplantes.cs` (idempotente; las tareas siguientes agregan lo suyo DENTRO de `Crear`) genera
   `Escenas/Tony/Pruebas_Implantes.unity`, greybox, con letreros sin deformar:
   - jugador a pie con `ServiciosProgresion`, reloj de prueba (empieza a las 10:00 de día; J como en las otras escenas), guardado (F5/F9),
     `DineroDePrueba`, `ImplantesDePrueba`, `LigasDePrueba`, historia libre, `PantallasHistoria` (fundido y subtítulos);
   - CLÍNICA: lugar `Clinica` (horario de prueba: abierto de 8:00 a 20:00, para poder probar "cerrada"), `MostradorClinica.prefab`, una camilla
     (caja) con un punto de aparición "Clinica_Camilla" (a pie) y `EspejoJugador.prefab` en la pared;
   - PISTA a pie: una recta de 30 m marcada cada 5 m y tres cajas de salto cuyas alturas calcula el creador desde `DatosPersonaje_Base`, el
     `stepOffset` del jugador y la tabla de implantes: una que se sube con el salto base, una que solo se sube con PIERNAS I y otra solo con
     PIERNAS II (letreros "BASE", "PIERNAS I", "PIERNAS II"); tres `MunecoAPie`;
   - TALLER con `MechaEstacionado.prefab`, su banco y un punto de llamado en la calle; ARENA de prueba con `MostradorPeleas.prefab`;
   - lugar `Casa` (zona y una aparición "Casa_Cama") y un sitio vacío "Sitio_Pandilla" en una calle a 25 m o más de la clínica (lo usa T-195).
   La bitácora guarda un mapa con las posiciones.
3. Pruebas PlayMode en un ensamblado nuevo `MechaCyberpunk.Tests.PlayMode.Progresion.Implantes` (`Tests/PlayMode/Progresion/Implantes/`,
   como el de Misiones), con entrada simulada (caminar, E, flechas, Enter, Esc, F5, F9):
   - `EscenaImplantesPruebas.cs`: la escena tiene todo lo del paso 2 (la camilla con su id, la clínica, el espejo, las 3 cajas con alturas
     coherentes con los assets, el mecha, el mostrador de peleas, "Sitio_Pandilla").
   - `ClinicaPruebas.cs` (dinero fijado por código, liga 1): E abre; ↓ a PIERNAS, Enter, Enter: −$2,000 con `MotivoDinero.Clinica`, la hora
     sube 2, `Nivel(Piernas)` = 1 y el HUD lo avisa; la fila de OJOS II dice "SE ABRE EN LA LIGA 3" y Enter no hace nada; con $1,000 BRAZOS I dice
     "NO TE ALCANZA" y no cobra; con dinero negativo tampoco; liga 3 por código: PIERNAS II cobra la diferencia ($3,000); Esc sale sin cobrar y
     el jugador camina en el cuadro siguiente; la vista previa muestra la opción elegida y la quita al salir; a las 21:00 la clínica dice
     "CERRADA" y E no abre; F5, cambiar implantes por código, F9: vuelve lo guardado.
   - Consola sin errores; `MedidorFrames` sin tirones fuera de su tolerancia; la partida real del humano no se toca.

## Carpetas permitidas
Assets/_Proyecto/UI/Clinica/ (nueva), Assets/_Proyecto/UI/Datos/ (solo ConfigPantallaClinica y su asset), Assets/_Proyecto/UI/Editor/,
Assets/_Proyecto/UI/Prefabs/MostradorClinica.prefab (nuevo, con el creador), Assets/_Proyecto/UI/HudAPie/ (solo el aviso de implante instalado),
Assets/_Proyecto/Progresion/Prologo/Editor/CreadorEscenaImplantes.cs (nuevo), Assets/_Proyecto/Escenas/Tony/Pruebas_Implantes.unity (nueva, solo con
el creador), Assets/_Proyecto/Tests/PlayMode/Progresion/Implantes/ (nueva, con su asmdef), docs/capturas/
No tocar: Core/, Progresion/Implantes/ (solo se usa; si falta algo pequeño del servicio, se anota y se pide), Personaje/, las otras escenas,
todo lo de Alan.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa (salvo los fallos conocidos de Alan en Arena) y `playmode-por-area` corre el área
      "Progresion.Implantes" sin marcar "LIMITE"
- [ ] EscenaImplantesPruebas y ClinicaPruebas pasan 3 corridas seguidas; los precios y la hora se calculan desde los assets y se imprimen
- [ ] Correr `CreadorEscenaImplantes` y el creador del mostrador dos veces no cambia la escena ni el prefab (md5 igual)
- [ ] Capturas docs/capturas/T-193-1.png (la pantalla de la clínica con una fila en gris, una con precio y la vista previa con el implante
      probado) y T-193-2.png (vista aérea de Pruebas_Implantes con sus letreros), revisadas a ojo

## Criterios de sensación (para un humano)
- Pruebas_Implantes, Play, con dinero de prueba: ¿la clínica se siente como un lugar turbio del mercado negro o como un menú? ¿Ver el
  implante en tu cuerpo antes de pagar ayuda a decidir? ¿El fundido y las 2 horas dan la idea de una operación?

## Pruebas automáticas
- PlayMode: EscenaImplantesPruebas, ClinicaPruebas.

## Fuera de alcance
- Efectos en el mecha (T-194), pandilleros (T-195), el doctor que te levanta (T-196), la clínica dentro del mercado negro de Alan (él coloca
  el prefab cuando quiera, D-tony-173), quitar o vender implantes, sonido del doctor. Hackeo.
