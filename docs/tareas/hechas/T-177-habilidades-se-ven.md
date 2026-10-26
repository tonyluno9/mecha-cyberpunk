# T-177 Las habilidades se ven: pantalla de la cabina, ayuda de controles, taller y mercado negro
Dueño: Tony | Rama: tony/T-177-habilidades-se-ven | Modelo: Sonnet 5.5, medium
Depende de: T-176

## Objetivo
El piloto sabe en todo momento qué habilidades lleva, cuánta munición le queda y cuándo vuelven a estar listas, en una pantalla del
tablero (todo diegético en el mecha). La ayuda de controles enseña las teclas 1 y 2 la primera vez. En el taller, al escoger una pieza, se
ve qué habilidad trae y cuánto acorta los enfriamientos; en el mercado negro se compra la munición hasta su máximo.

## Contexto
- Diseño: estetica-hud.md ("En el mecha todo es diegético en la cabina"; neón azul, magenta, naranja y turquesa), combate.md.
- Decisiones: D-tony-83 (pantalla de kits "KITS n · G"), D-tony-122 (colores por rama; nada en rojo puro), D-tony-132 (pantalla de piezas del
  taller: la diferencia solo muestra lo que cambia, textos y colores en `ConfigPantallaPiezas_Base`), D-tony-137 (munición en el mercado
  negro). Propuesta nueva: D-tony-138 (textos de estado).
- Código: `UI/Cabina/PantallaKitsCabina.cs` + `UI/Datos/ConfigPantallaKits.cs` + `Mecha/Editor/CreadorKitReparacion.cs` (modelo de pantalla
  del tablero: World Space, se prende con las luces de la cabina, sin texto encimado), `UI/Cabina/PantallaAyudaCabina.cs` +
  `UI/Datos/ConfigAyudas.cs` + `AyudasVistas` (T-134), `Mecha/Taller/ReglaTallerPiezas.cs` + `OpcionPieza.cs` (diferencias) y
  `UI/Taller/PantallaPiezasTaller.cs`, `UI/Tiendas/MostradorTienda.cs` y `PantallaTienda.cs` (ya listan lo que el catálogo vende y avisan el
  máximo). `UsoHabilidades` (T-176).

## Qué hacer
1. `UI/Cabina/PantallaHabilidadesCabina.cs` + `UI/Datos/ConfigPantallaHabilidades.cs` (+ `_Base.asset`): dos filas, una por ranura, en un lugar
   libre del tablero (sin tapar la vista al frente ni las pantallas de vida, calor y kits). Cada fila: tecla, nombre y nivel en el color de la
   rama, munición ("1 · MISIL II · ×4"), y su estado: "LISTO", una barra de enfriamiento con los segundos que faltan, "SIN MUNICIÓN" (naranja
   suave) o "—" si la ranura no tiene habilidad. Fuera de pelea las filas salen en gris. Con las dos ranuras con habilidad y sin munición,
   una línea abajo: "SOLO BRAZOS". Se actualiza con `UsoHabilidades.Cambio` y, mientras haya un enfriamiento corriendo, la barra cada cuadro
   sin crear objetos (sin basura por cuadro). Sin `UsoHabilidades` o con las dos ranuras vacías, la pantalla está apagada.
2. Escudo activo: el marco de la pantalla y una franja del borde del cristal en turquesa mientras dure (sin cambiar `.mat` en ejecución).
3. Ayuda de controles: la primera pelea en la que el mecha lleva al menos una habilidad, la línea "1 · {habilidad del brazo}  2 · {del torso}"
   (solo las que hay) hasta que la persona usa una o pasa el tiempo máximo; se recuerda en `AyudasVistas` como las otras.
4. Taller: la diferencia de una opción de pieza agrega la habilidad cuando cambia ("HABILIDAD 1 · MISIL II · MISILES"; "SIN HABILIDAD" si la
   pieza nueva no trae) y la reducción de enfriamiento ("ENFRIAMIENTO −6 %"), con los textos en el config de la pantalla.
5. Creador (idempotente) que monta la pantalla en `MechaJugador.prefab` (modelo `CreadorKitReparacion`).
6. PlayMode:
   - `Tests/PlayMode/UI/PantallaHabilidadesPruebas.cs` con `MechaEstacionado.prefab` piloteado y entrada simulada: a) con piezas de serie la
     pantalla está apagada; b) con BRAZO IZQ. MILITAR I y 2 misiles, en pelea: "1 · MISIL I · ×2" y "LISTO"; al pulsar 1, "×1" y la barra
     baja de 8 s a 0 (se lee el texto de los segundos en 3 momentos); c) con 0 misiles, "SIN MUNICIÓN"; con las dos ranuras sin munición, "SOLO
     BRAZOS"; d) fuera de pelea, gris; e) ESCUDO activo: marco turquesa, y vuelve al color normal al terminar; f) `MedidorFrames` sin tirones
     y sin basura por cuadro con la barra corriendo (perfilado de memoria o conteo de asignaciones, como lo permita el helper).
   - Casos nuevos en `Tests/PlayMode/Mecha/AyudaCabinaPruebas.cs` (la línea sale la primera vez y no la segunda) y en
     `Tests/PlayMode/UI/PantallaPiezasPruebas.cs` (la diferencia muestra la habilidad y el enfriamiento).
   - `Tests/PlayMode/UI/MostradorTiendaPruebas.cs`: el mercado negro de Pruebas_Economia lista las 4 municiones con su precio; comprar
     misiles hasta 6 cobra cada uno y el 7.º avisa el máximo sin cobrar.

## Carpetas permitidas
Assets/_Proyecto/UI/ (Cabina/, Datos/, Taller/, Tiendas/ solo si hace falta para la munición, Editor/, Prefabs/ solo con creadores),
Assets/_Proyecto/Mecha/Taller/ (diferencias de la opción), Assets/_Proyecto/Mecha/Editor/ (creador de la pantalla),
Assets/_Proyecto/Mecha/Prefabs/ (solo con el creador), Assets/_Proyecto/Tests/PlayMode/UI/, Assets/_Proyecto/Tests/PlayMode/Mecha/,
Assets/_Proyecto/Tests/EditMode/Mecha/, Assets/_Proyecto/Tests/EditMode/UI/
No tocar: Core/, Combate/, Economia/ (los datos ya están), escenas, todo lo de Alan.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa (salvo los 9 fallos conocidos de Alan en Arena)
- [ ] PantallaHabilidadesPruebas a) a f) pasan; los casos nuevos de AyudaCabinaPruebas, PantallaPiezasPruebas y MostradorTiendaPruebas pasan
      y las pruebas que ya existían en UI y Mecha siguen en verde
- [ ] El creador corrido dos veces no cambia `MechaJugador.prefab` (md5 igual); tras la suite `git status` no muestra materiales modificados
- [ ] Capturas, revisadas a ojo: docs/capturas/T-177-1.png (cabina en pelea con MISIL enfriando y ESCUDO listo; el texto no se encima con
      otras pantallas y se lee), T-177-2.png (cabina con "SIN MUNICIÓN" y "SOLO BRAZOS"), T-177-3.png (PIEZAS DEL MECHA con la línea de habilidad)

## Criterios de sensación (para un humano)
- En pelea: ¿ves de reojo si el misil está listo sin dejar de mirar al rival? ¿La barra de enfriamiento se entiende sin leer los segundos?
- En el taller: ¿la línea de habilidad te hace querer una pieza por lo que trae, además de por los números?

## Pruebas automáticas
- PlayMode: PantallaHabilidadesPruebas, AyudaCabinaPruebas, PantallaPiezasPruebas, MostradorTiendaPruebas.

## Fuera de alcance
- Sonidos de la cabina al lanzar o al quedarse sin munición (quedan para el pase de audio). Pantalla de la arena de Alan. HUD a pie.
