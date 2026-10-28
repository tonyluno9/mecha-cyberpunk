# T-196 Caer en la calle: el doctor te levanta, te cobra y el mecha vuelve solo al taller
Dueño: Tony | Rama: tony/T-196-doctor | Modelo: Sonnet 5.5, medium
Depende de: T-193, T-195

## Objetivo
Si caes en la calle (KO a pie, o tu mecha en KO fuera de una pelea del clandestino), la pantalla funde a negro y despiertas horas después en
la camilla de la clínica. El doctor te cobra por levantarte (si no te alcanza, te quedas debiendo), el calor se borra, la misión en curso
falla y el mecha, si andaba en la calle, regresa solo a tu taller muy dañado. Vuelves a casa por tu cuenta. Perder en la calle duele pero
nunca atora el juego.

## Contexto
- Diseño: personaje-progresion.md ("Si mueres en la calle, despiertas con el doctor de la clínica, que te cobra por levantarte. Vuelves a casa
  por tu cuenta y el mecha regresa solo a tu taller muy dañado"; propuesta "No tienes dinero para pagarle al doctor: queda como deuda";
  pregunta abierta "Cuánto cobra el doctor"; criterio "Al morir en la calle despierto en la clínica con menos dinero, y el mecha está dañado en
  el taller").
- Decisiones: D-039 y D-tony-85 (la deuda es dinero negativo; `CargarDeuda`), D-tony-42 y D-tony-59 (piso de emergencia del 15 % del mecha de la
  historia), D-tony-58 (el prólogo se reinicia al despertar: el doctor no actúa fuera de `Libre`), D-tony-62 (`Progresion.Prologo` es el
  ensamblado de arriba, el único que ve a la vez Personaje, Mecha, Pelea, Progresion y UI), D-tony-147 (fallar una misión por KO). Propuestas:
  D-tony-170 (reglas del doctor), D-tony-173 (lo de Alan).
- Contratos: `IJugador` (`Colocar`, `Modo`), `IPuntoAparicion` ("Clinica_Camilla", "Casa_Cama"), `ICartera.CargarDeuda` (`MotivoDinero.Clinica`),
  `ICalor.Borrar`, `IRelojMundo.AvanzarHasta`, `IObjetivos`, `IHistoria` (`Libre`), `EventosPelea` (`PeleaIniciada`/`PeleaTerminada`: dentro de
  una pelea del flujo, perder es asunto del flujo, no del doctor).
- Código: `Personaje/Vida/VidaAPie.cs` (`KO`, `Restaurar`), `Mecha/Montaje/MontajeMecha.cs` (`IntentarBajar`), `Mecha/Llamado/LlamadoMecha.cs`
  (`VolverAlTallerYa`), `Mecha/Estado/MechaPersistente.cs` (`FijarFraccionVida`, que no baja del piso), `Pelea/Misiones/PeleaDeMision.cs` (ya falla
  la misión por KO del mecha), `ServicioMisiones.FallarExterno`, `UI/Historia/PantallaFundido.cs` y `PantallaSubtitulos.cs`,
  `Progresion/Prologo/DirectorPrologo.cs` (modelo de secuencia con fundido, reloj, colocar y estado), `Progresion/Prologo/Editor/CreadorMostradorPeleas.cs`
  (modelo de creador de prefab de este ensamblado).

## Qué hacer
1. `Progresion/Prologo/Clinica/DoctorClinica.cs` con `Progresion/Prologo/Datos/DatosDoctor_Base` (cobro, horas, fracción de vida del mecha,
   espera antes del fundido, textos; valores de D-tony-170). Prefab `Progresion/Prologo/Prefabs/DoctorClinica.prefab` con su creador
   idempotente. Actúa solo con la historia en `Libre` (o sin `IHistoria`):
   - Dispara con el KO de la `VidaAPie` del jugador, o con el KO del mecha del jugador cuando no hay una pelea del flujo en curso (pelea de
     misión, calle). Una sola vez por caída, aunque lleguen los dos.
   - Espera unos segundos (la cámara ya cae), funde a negro, y con la pantalla negra: baja al jugador del mecha si iba montado (si
     `IntentarBajar` no sirve en KO, agrega a `MontajeMecha` una bajada forzada que no cambie lo de siempre); si hay misión en curso y no terminó
     ya, `FallarExterno(KO)` (sin doble castigo); si el mecha estaba fuera del taller, `VolverAlTallerYa` y `FijarFraccionVida(min(fracción
     actual, fracción del doctor))`; si estaba en el taller, no se toca; `ICalor.Borrar`; `AvanzarHasta(hora + horas)`; `VidaAPie.Restaurar`;
     coloca al jugador en "Clinica_Camilla" (sin ese punto, en "Casa_Cama"; sin ninguno, donde cayó, con un warning); cobra con
     `CargarDeuda(cobro, MotivoDinero.Clinica)` (puede dejar el dinero en negativo).
   - Destapa la pantalla con el subtítulo del doctor ("EL DOC: Otra vez tú. Son quinientos, mijo." o "... Me los quedas debiendo." si quedaste
     en negativo), el HUD anuncia el cobro, y fija el objetivo "VUELVE A CASA" hacia el lugar `Casa` (se quita al entrar a `Casa` o al aceptar
     una misión). Durante la secuencia no hay control ni se puede guardar.
2. Colocar `DoctorClinica.prefab` con los creadores: en `Pruebas_Implantes` (`CreadorEscenaImplantes`) y en `Pruebas_Misiones`
   (`CreadorEscenaMisiones`, con una camilla "Clinica_Camilla" de prueba en un sitio libre a 15 m o más de los puntos de misión).
3. Pruebas:
   - EditMode `Tests/EditMode/Progresion/ReglaDoctorPruebas.cs` (si la decisión se saca a una regla pura): cuándo actúa (Libre sí, prólogo no,
     dentro de pelea del flujo no), qué fracción queda al mecha (dentro y fuera del taller, en KO y con vida), el texto con y sin deuda.
   - PlayMode `Tests/PlayMode/Progresion/Implantes/CaerEnLaCallePruebas.cs` con entrada simulada (atajos permitidos y anotados: dinero, hora e
     implantes por código, `Colocar` cerca de la pandilla, KO del mecha por código en la misión):
     a) Pruebas_Implantes, 23:00, $300, mecha en el taller: caminar a la pandilla y quedarse quieto hasta el KO → despierta en la camilla a las
        05:00 del día siguiente con la vida llena, dinero −$200 (el HUD lo dice), calor 0, objetivo "VUELVE A CASA"; el mecha no cambió; caminar
        a `Casa` quita el objetivo;
     b) igual pero con el mecha llamado a la calle con 80 % de vida: vuelve al taller con 20 %; con 10 %: queda en el piso del 15 %;
     c) Pruebas_Misiones, LOS DEL CALLEJÓN en el mecha con su KO por código: la misión falla una sola vez con `KO` (reputación según
        D-tony-147, sin doble castigo), y la secuencia del doctor lleva al jugador a pie a la camilla con el mecha en el taller;
     d) en una pelea del flujo (arena de Pruebas_Implantes), perder no llama al doctor;
     e) con la historia fuera de `Libre` (doble de historia), el KO a pie no llama al doctor;
     f) reparar después en el banco cuesta lo calculado desde los assets para la vida que dejó el doctor;
     g) consola sin errores; `MedidorFrames` sin tirones fuera de su tolerancia; la partida real del humano no se toca.
   - Captura docs/capturas/T-196-1.png: el despertar en la camilla con el subtítulo del doctor y el aviso del cobro, revisada a ojo.

## Carpetas permitidas
Assets/_Proyecto/Progresion/Prologo/Clinica/ (nueva), Assets/_Proyecto/Progresion/Prologo/Datos/ (solo DatosDoctor y su asset),
Assets/_Proyecto/Progresion/Prologo/Editor/ (creador del prefab, CreadorEscenaImplantes, CreadorEscenaMisiones), Assets/_Proyecto/Progresion/Prologo/Prefabs/DoctorClinica.prefab
(nuevo, con el creador), Assets/_Proyecto/Mecha/Montaje/ (solo la bajada forzada, si hace falta), Assets/_Proyecto/Escenas/Tony/Pruebas_Implantes.unity y
Pruebas_Misiones.unity (solo con sus creadores), Assets/_Proyecto/Tests/EditMode/Progresion/, Assets/_Proyecto/Tests/PlayMode/Progresion/Implantes/, docs/capturas/
No tocar: Core/, `DirectorPrologo` y el prólogo, `PeleaDeMision` (salvo que falte algo mínimo, anotado), Personaje/ (si `VidaAPie` necesita algo,
se anota y se pide), las otras escenas, todo lo de Alan.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa (salvo los fallos conocidos de Alan en Arena) y `playmode-por-area` no marca ningún área "LIMITE"
- [ ] CaerEnLaCallePruebas pasa 3 corridas seguidas con los valores del paso 3, calculados desde los assets e impresos en la bitácora
- [ ] Las pruebas del prólogo, de la emboscada, de las misiones y del montaje siguen en verde (RecorridoPrologoPruebas, RecorridoEmboscadaPruebas,
      PrologoNochePruebas, MisionesPeleaPruebas, RecorridoMisionesPruebas, MontajeMechaPruebas o como se llamen)
- [ ] Correr los creadores dos veces no cambia el prefab ni las escenas (md5 igual)
- [ ] Captura T-196-1 revisada a ojo

## Criterios de sensación (para un humano)
- Pruebas_Implantes de noche: déjate tumbar por la pandilla. ¿Despertar con el doc y la deuda se siente como un castigo justo que da ganas de
  volver más fuerte, o como un fastidio? ¿El camino de vuelta a casa sobra o le da peso a la caída?

## Pruebas automáticas
- EditMode: ReglaDoctorPruebas (si hay regla pura). PlayMode: CaerEnLaCallePruebas.

## Fuera de alcance
- El doctor en Ciudad.unity (Alan coloca la camilla y el prefab cuando quiera; con T-136 se revisa), intereses o cobradores de la deuda,
  perder objetos del inventario al caer, morir en el carro. Hackeo.
