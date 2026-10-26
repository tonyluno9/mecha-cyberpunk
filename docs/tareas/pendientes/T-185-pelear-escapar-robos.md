# T-185 Pelear y escapar: peleas callejeras de misión, calor, robos con chatarra y fallar por KO
Dueño: Tony | Rama: tony/T-185-pelear-escapar | Modelo: Sonnet 5.5, medium
Depende de: T-184

## Objetivo
Los pasos que faltan funcionan: `Pelear` levanta una pelea callejera en el punto (la pandilla del callejón, las escoltas del camión, la pelea
sin jaula) que se gana en el mecha, y `Escapar` pide bajar el calor a 0 o llegar a casa antes de cobrar. Así los robos y las ilegales se
sienten distintos del barrio: suman calor, te obligan a esconderte y pagan en chatarra que luego vendes en el mercado negro. Si tu mecha cae
en la pelea de misión, la misión falla.

## Contexto
- Diseño: economia-misiones.md ("Vendes ... chatarra de robos en el mercado negro"), ciudad.md ("De noche ... aparecen las pandillas"; "El calor
  lo persiguen drones y patrullas ... y baja si te escondes en el barrio o llegas a casa"), combate.md ("Usar el mecha en la calle te da calor
  policial").
- Decisiones: D-tony-35 (la emboscada fija objetivos opcionalmente), D-tony-147 (fallar), D-tony-150 (Pelea referencia Progresion).
- Código: `Pelea/Emboscada/DirectorEmboscada.cs` (`Empezar`, `Habilitada`, `Terminada`, `Rivales`), `Pelea/Datos/DatosEmboscada.cs`
  (`fijaObjetivos`, poses, prefab del rival), `Pelea/Prefabs/Emboscada.prefab`; `Mecha/Calor/CalorPorMecha.cs` (el mecha en la calle suma
  calor); `ServicioCalor`; `ServicioMisiones.CumplirPasoExterno` y `FallarExterno` (T-184); `EventosLugares.JugadorEntro` (`IdLugar.Casa`).
- Pruebas modelo: `Tests/PlayMode/Pelea/RecorridoEmboscadaPruebas.cs` y `RecorridoEmboscadaBase.cs` (llamar al mecha, subir, pelear con
  entrada simulada y atajos de daño), `MisionesAPiePruebas` (T-184).

## Qué hacer
1. `Escapar` en `ServicioMisiones`: al empezar el paso, texto "PIERDE EL CALOR: escóndete o llega a casa" y sin punto (objetivo al lugar
   `Casa` si hay un `ILugar` Casa; si no, sin flecha). Se cumple cuando `ICalor.Valor` llega a 0 o el jugador entra a `Casa`. Sin `ICalor`
   registrado se cumple al instante (warning una vez). Si el paso empieza con calor 0, se cumple en el siguiente cuadro (no se paga sin pasar
   por el paso: la prueba lo comprueba con el orden de eventos).
2. `Pelea/Misiones/PeleaDeMision.cs` (Pelea ya referencia Progresion): escucha a `ServicioMisiones`; cuando el paso es `Pelear`, crea una
   emboscada en la pose del punto con el `DatosEmboscada` que le toca a esa misión según `Pelea/Datos/ConfigPeleasMision_Base` (tabla id de
   misión → `DatosEmboscada`, con `fijaObjetivos` en false porque la misión pone los suyos), la habilita y la empieza al acercarse. Al
   `Terminada`: `CumplirPasoExterno(Pelear)` y la retira. Si el peleador del jugador (su mecha) queda KO durante esa pelea: `FallarExterno(KO)`,
   la emboscada se retira sin dejar rivales. Si la misión termina por otra causa (abandono, tiempo, carga), también se retira.
   `DatosEmboscada_Mision_Callejon` (2 pandilleros), `_Baldio` (3) y `_Camion` (2 escoltas, un poco más duros: +20 % de vida), creados con un
   creador idempotente de `Pelea/Editor/` usando el rival de la emboscada que ya existe. Prefab `Pelea/Prefabs/PeleasDeMision.prefab` (lo coloca
   el creador de la escena; Alan lo puede colocar en Ciudad).
3. Texto de los pasos de pelea: "Llama a tu mecha (M)" mientras va a pie y "<texto del paso>" en el mecha (como la emboscada). La pelea de
   misión no es una pelea del flujo (no hay campana, presentador ni premio de liga); las habilidades no se usan en ella (D-tony-138 sigue).
4. `CreadorEscenaMisiones`: agrega `PeleasDeMision.prefab`; deja el lugar `Casa` con su zona a 25 m o más de los botines.
5. Pruebas:
   - EditMode `Tests/EditMode/Pelea/ConfigPeleasMisionPruebas.cs`: cada misión con paso `Pelear` (T-183) tiene su `DatosEmboscada` en la tabla y
     ninguna sobra; los de misión tienen `fijaObjetivos` en false.
   - PlayMode `Tests/PlayMode/Progresion/Misiones/MisionesPeleaPruebas.cs` en Pruebas_Misiones con entrada simulada (caminar, M para llamar al
     mecha, E para subir, F/Espacio/Q para encender, ratón para pelear; atajo permitido: `RecibirDanio` a los rivales solo después de daño real
     del jugador, como en T-180; `AvanzarHasta` por código para cambiar de día o de franja):
     a) `robo_bodega` (aceptada por código, de noche): E en la bodega suma 50 de calor (nivel 2), el paso pasa a Escapar; caminar hasta `Casa`
        lo cumple; +6 chatarra, corporaciones −10, barrio +3, `MotivoDinero` sin cobro (pago $0);
     b) `robo_bodega` otra vez al día siguiente: esperar sin subir al mecha hasta que el calor baje a 0 también lo cumple (medir el tiempo:
        debe coincidir con `DatosCalor_Base`);
     c) `barrio_callejon`: llamar al mecha, subir, vencer a los 2 pandilleros, bajar y volver con el taquero: $100, barrio +12, 1 ESCONDITE;
     d) `ilegal_pelea` con el mecha del jugador puesto en KO por código (atajo anotado): la misión falla con `KO`, corporaciones sin la penalización
        que no le toca (la misión no sube corporaciones) y barrio −3; no queda ningún rival de la emboscada en la escena;
     e) vender la chatarra del paso a) en el mercado negro (E, pestaña VENDER) cobra 6 × $50.
     Consola sin errores; `MedidorFrames` sin tirones fuera de su tolerancia.

## Carpetas permitidas
Assets/_Proyecto/Progresion/Misiones/, Assets/_Proyecto/Pelea/Misiones/ (nueva), Assets/_Proyecto/Pelea/Datos/ (solo con el creador),
Assets/_Proyecto/Pelea/Editor/, Assets/_Proyecto/Pelea/Prefabs/PeleasDeMision.prefab (nuevo, con el creador),
Assets/_Proyecto/Progresion/Prologo/Editor/CreadorEscenaMisiones.cs, Assets/_Proyecto/Escenas/Tony/Pruebas_Misiones.unity (solo con el creador),
Assets/_Proyecto/Tests/EditMode/Pelea/, Assets/_Proyecto/Tests/PlayMode/Progresion/Misiones/, docs/capturas/
No tocar: Core/, `Emboscada.prefab` y `DirectorEmboscada` salvo lo mínimo para retirarla (si hace falta un método, que no cambie el prólogo:
RecorridoEmboscadaPruebas y PrologoNochePruebas deben seguir en verde), UI/, IA/, las otras escenas, todo lo de Alan.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa (salvo los fallos conocidos de Alan en Arena) y `playmode-por-area` no marca ningún área "LIMITE"
- [ ] MisionesPeleaPruebas pasa 3 corridas seguidas con los valores del paso 5, calculados desde los assets e impresos en la bitácora
- [ ] Las pruebas de la emboscada del prólogo siguen en verde (RecorridoEmboscadaPruebas, PrologoNochePruebas)
- [ ] Captura docs/capturas/T-185-1.png: la cabina frente a los pandilleros del callejón con el texto del paso, revisada a ojo

## Criterios de sensación (para un humano)
- Pruebas_Misiones, de noche: el robo de la bodega y escapar. ¿Robar y tener que esconderte se siente tenso o solo es esperar a que baje el
  calor? ¿La pandilla del callejón se siente como defender al taquero? ¿Ir a vender la chatarra después se siente como el cierre del robo?

## Pruebas automáticas
- EditMode: ConfigPeleasMisionPruebas. PlayMode: MisionesPeleaPruebas.

## Fuera de alcance
- Patrullas y drones que persiguen (Alan, con `ICalor`). Usar el favor ESCONDITE para escapar (T-187). Pantallas para aceptar (T-186).
