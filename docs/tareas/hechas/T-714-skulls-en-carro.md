# T-714 Los SKULLS de refuerzo llegan en carro
Dueño: Tony (S1, autorizado en Trafico e IA esta noche) | Rama: tony/T-714-skulls-en-carro | Modelo: Sonnet 5.5
Depende de: T-704, T-708, T-709, T-713

## Objetivo
Cuando peleas contra los SKULLS en su territorio, llegan refuerzos en carro desde fuera de la vista, se estacionan, se bajan por las puertas y pelean.

## Qué se hizo
- ReglaRefuerzos (pura): con 3 SKULLS vencidos en su zona sale un carro; uno a la vez y 60 s de enfriamiento.
- GestorRefuerzos (lo agrega GestorPersecucion en su Awake, como ya hace con los testigos): vigila el respeto de las zonas de los SKULLS por ITerritorio (cada SKULL vencido suma 10), solo con el jugador a pie y en una zona que sigue siendo de ellos. Manda el carro con TraficoCalle.EnviarRefuerzo y, cuando llega, baja a 2 SKULLS por las dos puertas (prefab Trafico/Resources/SkullRefuerzo, ya en alerta).
- TraficoCalle.EnviarRefuerzo reusa el pool (T-704): toma un carro apagado, o uno que ande fuera de la vista y a más de 45 m (nadie lo ve desaparecer), y lo hace salir en un nodo de calle fuera de la vista y lejos del jugador, elegido como las patrullas de T-708 (por el tiempo real de la ruta: objetivo 12 s, mínimo 7 s).
- CarroTrafico.IniciarRefuerzo: el carro sigue la ruta por las calles (sin elegir esquinas al azar), el pool no lo apaga y no se reubica si se atora; frena suave al llegar y se estaciona al último nodo o a 12 m del jugador. Con su tráfico, choques y robo igual que cualquier carro del pool.
- PandilleroAPie.siempreAlerta: el SKULL de refuerzo pelea sin necesitar un grupo con territorio.
- Trafico/Editor/CreadorSkullRefuerzoT714 crea el prefab SkullRefuerzo (variante de PandilleroArmado_Skulls). No se tocó ninguna escena: el gestor aparece solo al correr.

## Criterios
- [x] Con 3 vencidos sale un carro; con 2 no
- [x] Nace fuera de la vista (con margen) y a más de 60 m del jugador
- [x] Llega manejando en un tiempo real (19.6 s en la prueba: salió a 205 m)
- [x] De las dos puertas se bajan 2 SKULLS en alerta, que disparan y le bajan la vida al jugador
- [x] Un carro a la vez y con enfriamiento
- [x] Matar a un SKULL de refuerzo sigue siendo igual: cae

## Pruebas
- EditMode Trafico: ReglaRefuerzosT714Pruebas (5)
- PlayMode IA: RefuerzoSkullsT714Pruebas (3), con el jugador real, el tráfico real y calles de prueba; capturas docs/capturas/s1c-T-714-*.png

## Fuera de alcance
- Que los de refuerzo cuenten como vencidos de la misión del Tianguis (no suman respeto, para no llamar más refuerzos en cadena)
- Que se cubran en el carro como los policías con la patrulla (se cubren con lo que haya cerca, como cualquier SKULL)
