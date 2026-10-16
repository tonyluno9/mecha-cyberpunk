# T-131 Flashback de los papás: lógica, datos y escena de pruebas
Dueño: Tony | Rama: tony/T-131-flashback | Modelo: Sonnet 5.5, medium
Depende de: T-120, T-124, T-125, T-127

## Objetivo
El flashback se juega como una secuencia de planos de cámara con subtítulos y fundidos, saltable, que al terminar despierta al jugador
en la ciudad (etapa `Despertar`). La lógica y los textos son de Tony (`Flashback.prefab` + `DatosFlashback`); el escenario y los puntos de
cámara son de Alan en Flashback.unity (A-F3), así que aquí se prueba con una escena greybox propia.

## Contexto
- Diseño: concepto-historia.md ("el flashback muestra que de niño tus papás se fueron y te dejaron el mecha"; "el hilo principal es el
  misterio de tus papás, ligado a una corporación"), demo.md (paso 5).
- Contratos: `IPuntoHistoria` (T-120: `Id`, `Pose` de la cámara, `Activar(bool)`; prefijo `Flashback_`), `IHistoria`. Piezas:
  `ServicioHistoria`, `CambioEscena`, `DatosEscenas` (T-124), `PantallaFundido` (T-125), `PuntoHistoriaDePrueba` (T-127),
  `ServiciosProgresion.prefab`.
- Decisiones: D-029 (Flashback.unity es de Alan; Tony entrega un prefab que Alan coloca). Propuesta nueva: D-tony-68.
- La escena del flashback no tiene `JugadorAPie`: el prefab trae los servicios que necesita para cargar y guardar la partida.

## Qué hacer
1. `Progresion/Prologo/Datos/DatosFlashback.cs` + `DatosFlashback_Base.asset` (con creador): lista de planos (id del punto, segundos,
   subtítulo), segundos de fundido entre planos (0.5) y texto de salto. Seis planos de unos 8 a 10 s, textos propuesta (D-tony-68, Tony los
   cambia en el asset): "Tenías ocho años la última vez que los viste." / "Mamá le dio una vuelta más a cada tornillo del mecha." / "Papá
   dijo que volverían antes de que amaneciera." / "Se fueron en una camioneta sin placas, con un logo que no conocías." / "No volvieron." /
   "Solo quedó el mecha. Y las preguntas." Ids "Flashback_Plano_1" a "Flashback_Plano_6".
2. `UI/Historia/PantallaSubtitulos.cs`: Canvas Overlay con una línea de subtítulo abajo al centro (sombra para leerse sobre cualquier fondo)
   y el aviso pequeño "Mantén ESPACIO para saltar".
3. `Progresion/Prologo/DirectorFlashback.cs` + `Progresion/Prologo/Prefabs/Flashback.prefab` (con creador; trae su cámara con el único
   `AudioListener`, `PantallaSubtitulos`, `PantallasHistoria.prefab` y `ServiciosProgresion.prefab`):
   - al arrancar (un frame después, para que la partida se cargue y los puntos se registren) corre los planos en orden: busca el punto por
     id en `Registro<IPuntoHistoria>`, pone la cámara en su `Pose`, llama `Activar(true)`, muestra el subtítulo, espera los segundos y llama
     `Activar(false)`; entre planos, fundido. Si falta un punto: se queda en la pose anterior (o la del prefab), avisa con warning y sigue;
   - mantener Espacio 1 s salta al final (con fundido); el saltar no se dispara con un toque corto;
   - al final: `Avanzar(Despertar)` y `CambioEscena.Ir(RutaPara(Despertar))` (que guarda). Usa `DatosEscenas_Demo` por defecto
     (serializado). Si la etapa al arrancar no es `Flashback` (Play directo en la escena), corre igual y al final solo avisa.
4. `Progresion/Prologo/Editor/CreadorEscenaFlashback.cs` (idempotente) arma `Escenas/Tony/Pruebas_Flashback.unity`: un cuarto greybox de la
   casa de la infancia (mesa, puerta, el mecha viejo hecho de cajas, una camioneta afuera), seis `PuntoHistoriaDePrueba` con los ids del
   asset mirando a algo distinto cada uno, y `Flashback.prefab` con `DatosEscenas_Pruebas`.
5. PlayMode `Tests/PlayMode/Progresion/FlashbackPruebas.cs` (con `CambioEscena.Interceptar` y `RutaForzada` temporal).

## Carpetas permitidas
Assets/_Proyecto/Progresion/Prologo/ (Datos/, Prefabs/, Editor/), Assets/_Proyecto/UI/Historia/, Assets/_Proyecto/UI/Editor/ (si hace falta),
Assets/_Proyecto/Escenas/Tony/Pruebas_Flashback.unity (con su creador), Assets/_Proyecto/Tests/PlayMode/Progresion/,
Assets/_Proyecto/Tests/EditMode/Progresion/, docs/capturas/
No tocar: Core/, todo lo de Alan (Flashback.unity no existe todavía y no se crea aquí), otras escenas.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa
- [ ] FlashbackPruebas: en Pruebas_Flashback con etapa `Flashback`, la cámara está en la pose de cada punto (±0.01 m) al empezar su plano, el
      subtítulo es el del asset, cada punto recibe `Activar(true)` y `Activar(false)` una vez; dura la suma de los planos y fundidos (±0.5 s);
      al final la etapa es `Despertar` (guardada) y la escena pedida es la ciudad de `DatosEscenas_Pruebas`
- [ ] Mantener Espacio 1 s termina en menos de 3 s con el mismo final; un toque de 0.2 s no salta
- [ ] Sin el punto "Flashback_Plano_3" corre completo con un warning y sin errores
- [ ] Un solo `AudioListener` y una sola cámara activa; consola sin errores
- [ ] EditMode: el asset tiene 6 planos con id, segundos mayores que 0 y subtítulo no vacío
- [ ] Capturas docs/capturas/T-131-1.png y T-131-2.png (dos planos distintos con su subtítulo)

## Criterios de sensación (para un humano)
- Abre Pruebas_Flashback.unity y Play: ¿los textos cuentan lo de tus papás sin explicar de más y dejan el misterio de la corporación?
  ¿Unos 55 s se sienten bien o largos? ¿Se lee el subtítulo? (Los textos y tiempos se cambian en DatosFlashback_Base.)

## Pruebas automáticas
- EditMode: DatosFlashbackPruebas. PlayMode: FlashbackPruebas.

## Fuera de alcance
- El escenario real y la animación de los papás (Alan, Flashback.unity, A-F3); voces y música; mostrar la apariencia del jugador de niño
