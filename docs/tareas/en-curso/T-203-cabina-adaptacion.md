# T-203 La cabina te dice qué te leyó el rival
Dueño: Tony | Rama: tony/T-203-cabina-adaptacion | Modelo: Sonnet 5.5, medium
Depende de: T-200 (puede ir a la par de T-201 y T-202: solo usa `ILecturaAdaptacion` y un doble)

## Objetivo
Una pantalla de la cabina muestra lo que el rival que tienes enfrente aprendió de ti ("GUARDIA I · TIRAS MUCHOS PUÑOS"), hace parpadear la
fila cada vez que lo usa y, al terminar la pelea, te dice qué aprendió u olvidó y una pista para cambiar de estilo. Sin esto la adaptación
se sentiría como un rival que "se puso difícil" sin razón; con esto es un duelo de lecturas.

## Contexto
- Diseño: estetica-hud.md ("En el mecha todo es diegético en la cabina"; neón azul, magenta, naranja y turquesa), combate.md.
- Decisiones: D-tony-122 (sin cambiar materiales en ejecución), D-tony-167 (sin rojo puro: alarma y furia), D-tony-168 (OJOS en Combate/Lectura).
  Propuestas: D-tony-192 (los tres hábitos), D-tony-198 (qué dice la pantalla), D-tony-199.
- Contrato de Tony: `Combate/Lectura/ILecturaAdaptacion.cs`, `TipoAdaptacion`, `CambioAdaptacion` (T-199, T-200).
- Código modelo: `UI/Cabina/PantallaOjosCabina.cs` (elige rival, se prende con `EncendidoMecha`, no crea objetos por cuadro), `UI/Datos/ConfigPantallaOjos.cs`,
  `UI/Editor/CreadorPantallaOjosCabina.cs` (agrega la pantalla a `MechaJugador.prefab`, idempotente), `UI/Cabina/PantallaHabilidadesCabina.cs`.
- Pruebas modelo: `Tests/PlayMode/UI/PantallaHabilidadesPruebas.cs`, `Tests/PlayMode/UI/PantallaPiezasCabinaPruebas.cs`.

## Qué hacer
1. `UI/Datos/ConfigPantallaAdaptacion.cs` y `ConfigPantallaAdaptacion_Base.asset` (lo crea el creador; no pisa valores existentes): título
   "TE TIENE ESTUDIADO"; por `TipoAdaptacion`: nombre ("GUARDIA", "FINTAS", "RETIRADA"), hábito ("TIRAS MUCHOS PUÑOS", "TE CUBRES MUCHO",
   "LE PEGAS CUANDO FALLA") y pista ("PÉGALE JUSTO DESPUÉS DE SU GOLPE", "NO TE ESCONDAS: ATACA TÚ", "PÉGALE RÁPIDO O ATACA PRIMERO");
   formatos "APRENDIÓ: {0} {1}" y "OLVIDÓ: {0}"; niveles en romanos (I, II); distancia máxima 60 m; parpadeo 0.6 s; aviso de fin 8 s;
   colores (azul neón de base; ni rojo puro, ni el naranja del aviso, ni el turquesa de OJOS).
2. `UI/Cabina/PantallaAdaptacionCabina.cs`: elige el `ILecturaAdaptacion` registrado más cercano al mecha a la distancia máxima o menos (se
   reelige cada 0.5 s y al registrarse o quitarse uno, no en cada cuadro). Oculta si no hay rival, o si no tiene nada activo y no hay aviso de
   fin pendiente. Muestra el título y una fila por adaptación activa ("GUARDIA I · TIRAS MUCHOS PUÑOS"). `Usada` → esa fila parpadea.
   `Aprendio` → líneas de fin ("APRENDIÓ: FINTAS I", "OLVIDÓ: GUARDIA") y la pista del primero que subió, hasta que pasa el aviso o empieza la
   siguiente pelea. `NivelesCambiados` → refresca. Se prende y apaga con el encendido del mecha, como OJOS. Los textos solo cambian cuando
   cambia el valor; nada de objetos nuevos por cuadro.
3. `UI/Editor/CreadorPantallaAdaptacionCabina.cs`: la agrega a `MechaJugador.prefab` en el tablero, donde no tape el centro de la vista ni las
   pantallas de vida, habilidades, piezas y OJOS (anotar dónde en la bitácora). Idempotente.
4. Pruebas PlayMode `Tests/PlayMode/UI/PantallaAdaptacionCabinaPruebas.cs`:
   - con un doble de `ILecturaAdaptacion`: sin doble, oculta; con GUARDIA 1 muestra "GUARDIA I · TIRAS MUCHOS PUÑOS"; `Usada(Guardia)` hace
     parpadear la fila y vuelve; `Aprendio [Fintas 0→1, Guardia 1→0]` muestra las dos líneas y la pista de FINTAS; con dos dobles elige al
     más cercano; uno a 61 m no cuenta; con el encendido apagado no se ve; un doble que se destruye no deja errores;
   - con un rival de verdad (arena + flujo + MechaJugador + `RivalLiga1_1` + `MemoriaRivales`, entrada simulada con el ratón; atajo anotado:
     `RecibirDanio` al rival tras daño real): tras una pelea a PUÑOS, en el resultado aparece "APRENDIÓ: GUARDIA I" y la pista; en la revancha
     la fila "GUARDIA I" está en la pantalla desde la presentación.
   - Capturas docs/capturas/T-203-1.png (la cabina en pelea con dos filas) y T-203-2.png (el resultado con APRENDIÓ y la pista), revisadas a ojo.

## Carpetas permitidas
Assets/_Proyecto/UI/Cabina/, Assets/_Proyecto/UI/Datos/ (solo ConfigPantallaAdaptacion y su asset), Assets/_Proyecto/UI/Editor/,
Assets/_Proyecto/Mecha/Prefabs/MechaJugador.prefab (solo con el creador), Assets/_Proyecto/Tests/PlayMode/UI/, docs/capturas/, docs/bitacora/
No tocar: IA/, Combate/ (solo se lee `ILecturaAdaptacion`), Core/, Pelea/, el resto de pantallas de la cabina, escenas, todo lo de Alan.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa (salvo los fallos conocidos de Alan en Arena) y `playmode-por-area` no marca ningún área "LIMITE"
- [ ] PantallaAdaptacionCabinaPruebas pasa 3 corridas seguidas
- [ ] Las pruebas de las otras pantallas de la cabina siguen en verde (PantallaHabilidadesPruebas, PantallaPiezasCabinaPruebas, OjosSinapsisPruebas)
- [ ] Correr el creador dos veces no cambia `MechaJugador.prefab` (md5 igual)
- [ ] Capturas T-203-1 y T-203-2 revisadas a ojo: la pantalla no tapa el centro ni otra pantalla, se lee a la distancia de la cabina y no usa rojo puro

## Criterios de sensación (para un humano)
- En una revancha: ¿lees "TE TIENE ESTUDIADO · GUARDIA I" antes del conteo y entiendes qué cambió? ¿El parpadeo cuando te bloquea te ayuda o
  distrae? ¿La pista del final te dan ganas de probar otro estilo, o se siente como que el juego te regaña?

## Pruebas automáticas
- PlayMode: PantallaAdaptacionCabinaPruebas.

## Fuera de alcance
- La línea del presentador (D-tony-191), sonido de aviso, la escena de pruebas y su panel de depuración (T-204).
