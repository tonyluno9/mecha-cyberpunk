# T-066 Llamar al mecha desde el taller, zonas permitidas y regreso
Dueño: Tony | Rama: tony/T-066-llamado-mecha | Modelo: Sonnet 5.5, medium
Depende de: T-062, T-064 (T-065 antes en el orden, porque las dos tocan el prefab y la entrada)

## Objetivo
A pie, con M, el jugador llama a su mecha: si está en una zona permitida, el mecha deja el taller y en unos 6 segundos baja del
cielo en el punto de llegada; si no, el HUD dice que ahí no puede llegar. Con el mecha vacío en la calle, M lo manda de vuelta al
taller, y regresa solo si el jugador se aleja. Mientras llega no se puede subir.

## Contexto
- Diseño: combate.md ("Llamas al mecha y llega desde tu taller en unos segundos, solo en ciertas zonas"; "en una zona prohibida:
  no llega"). Contratos: Core/Mecha/IPuntoLlamadoMecha.cs y Core/Mecha/ITallerMecha.cs (T-062; lee sus `///`).
- Propuestas: D-tony-30 (reglas y tiempos del llamado) y D-tony-36 en docs/decisiones-pendientes-tony.md.
- Lo que ya hay: `MontajeMecha` con arranque Estacionado (T-064), el método de T-065 para agregar acciones a
  `ControlesMecha.inputactions` sin cambiar ids, el HUD a pie (UI/HudAPie/) que se ve a pie y en carro.
- El jugador se encuentra por `Registro<IJugador>.Actual` (Core). El llamado solo funciona con el jugador en modo APie.
- Referencias nuevas (mismo dueño): `MechaCyberpunk.UI` referencia `MechaCyberpunk.Mecha` para mostrar los avisos del llamado.
  Mecha no referencia UI.

## Qué hacer
1. Lógica pura `Mecha/Llamado/ReglaLlamado.cs`: con la posición del jugador, la lista de puntos (pose, radio, habilitado) y el
   estado del mecha, decide un `ResultadoLlamado`: `Llega` (con el índice del punto: el habilitado más cercano que tenga al
   jugador dentro de su radio horizontal), `ZonaProhibida`, `YaViene` (llegando), `Regresa` (el mecha está vacío en la calle: M lo
   devuelve), `Ocupado` (alguien va dentro) o `Nada` (regresando).
2. `Mecha/Llamado/LlamadoMecha.cs` en el mecha, con `ConfigLlamadoMecha` (Mecha/Datos/: llegada 6 s, descenso 1.5 s desde 25 m,
   regreso 2 s, regreso solo a más de 60 m). Estados `EnTaller`, `Llegando`, `EnCalle` y `Regresando`, con evento `EstadoCambio`
   y `SegundosParaLlegar`. Se agrega a `Registro<LlamadoMecha>` para que el HUD lo encuentre.
   - Al arrancar (Estacionado): se coloca en `ITallerMecha.Estacionamiento` si hay taller registrado; si no, su casa es su pose
     inicial. Queda `EnTaller`.
   - Acción LlamarMecha (M) en un mapa nuevo "Llamado" de `ControlesMecha.inputactions`, leída solo si el jugador va a pie.
   - `Llega`: estado `Llegando`; el mecha desaparece del taller (renderers y collider apagados); en el último `descenso` baja en
     vertical hasta la `Llegada` y al tocar piso queda `EnCalle`. Durante `Llegando`, una marca de neón en el piso de la llegada y
     `PuedeSubir` false (MontajeMecha consulta al llamado).
   - `Regresa` (M otra vez, mecha vacío en la calle) o jugador a más de 60 m del mecha vacío: `Regresando` 2 s (sube y
     desaparece) y vuelve `EnTaller` al estacionamiento.
   - Avisa cada intento con `LlamadoIntentado(ResultadoLlamado)`.
3. Dobles de lo de Alan en `Mecha/Pruebas/`: `PuntoLlamadoDePrueba` (IPuntoLlamadoMecha con gizmo del radio) y `TallerDePrueba`
   (ITallerMecha con un cobertizo greybox). Agrégalos a Pruebas_Calle con su creador: taller detrás del inicio, un punto en la
   calle con radio 30 m y una parte de la calle fuera de toda zona (más de 75 m del inicio). El mecha de la escena arranca en el taller.
4. HUD a pie (UI/HudAPie/ y `ConfigHudAPie`): aviso temporal según el resultado ("MECHA EN CAMINO · 5", con cuenta atrás mientras
   llega; "AQUÍ NO PUEDE LLEGAR TU MECHA"; "TU MECHA REGRESA AL TALLER"). Textos en el config, no en el código.
5. Pruebas:
   - EditMode `Tests/EditMode/Mecha/ReglaLlamadoPruebas.cs`: dentro y fuera del radio (horizontal: la altura no cuenta), punto
     deshabilitado, dos zonas encimadas (gana la llegada más cercana), cada estado del mecha, lista vacía y null.
   - PlayMode `Tests/PlayMode/Mecha/LlamadoMechaPruebas.cs` (Pruebas_Calle, entrada simulada): a) camina a la zona y pulsa M: el
     mecha deja el taller, `PuedeSubir` false mientras llega y a los 6 s (±0.2) está a menos de 0.1 m de la llegada y `EnCalle`;
     b) sube con E (y enciende); c) baja, pulsa M: regresa y a los 2 s está en el estacionamiento; d) fuera de la zona, M no mueve el
     mecha y el HUD muestra el aviso de zona prohibida; e) en carro o dentro del mecha, M no hace nada; f) con el mecha vacío en la
     calle, alejarse 60 m lo manda al taller; g) sin taller registrado, regresa a su pose inicial.
   - PlayMode `Tests/PlayMode/UI/HudLlamadoPruebas.cs`: los tres avisos aparecen y se van; la cuenta atrás baja.

## Carpetas permitidas
Assets/_Proyecto/Mecha/, Assets/_Proyecto/UI/HudAPie/, Assets/_Proyecto/UI/Datos/, Assets/_Proyecto/UI/Editor/,
Assets/_Proyecto/UI/MechaCyberpunk.UI.asmdef, Assets/_Proyecto/UI/Prefabs/HudAPie.prefab (con su creador),
Assets/_Proyecto/Escenas/Tony/Pruebas_Calle.unity (con su creador), Assets/_Proyecto/Tests/EditMode/Mecha/,
Assets/_Proyecto/Tests/PlayMode/Mecha/, Assets/_Proyecto/Tests/PlayMode/UI/
No tocar: Core/, Personaje/, Pruebas_Tony.unity, las carpetas y escenas de Alan.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa
- [ ] ReglaLlamadoPruebas, LlamadoMechaPruebas y HudLlamadoPruebas pasan
- [ ] ReglasEnsamblados pasa con UI → Mecha y Mecha sin referencia a UI
- [ ] Medido en Unity vivo en Pruebas_Calle: 3 ciclos de llamar, subir, encender, bajar y devolver sin errores en consola
- [ ] Capturas: a pie con la marca de llegada y "MECHA EN CAMINO" (docs/capturas/T-066-1.png), el mecha bajando del cielo
      (T-066-2.png) y el aviso de zona prohibida (T-066-3.png)

## Criterios de sensación (para un humano)
- ¿Ver bajar al mecha del cielo se siente como algo grande y tuyo que llega? ¿6 segundos de espera se sienten bien o largos?
- ¿Se entiende por qué no llegó en la zona prohibida, sin leer nada más que el aviso?

## Pruebas automáticas
- EditMode: ReglaLlamadoPruebas. PlayMode: LlamadoMechaPruebas, HudLlamadoPruebas.

## Fuera de alcance
- Calor por llamar y por usar el mecha (T-067); sonido; efectos de polvo más allá de la marca greybox
- Que el mecha camine por la ciudad hasta ti (llega del cielo); aterrizar encima de alguien (si la llegada está ocupada, aterriza igual)
