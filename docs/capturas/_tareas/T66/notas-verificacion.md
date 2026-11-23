# T66: SKULL flotando en la calle del Tianguis (verificador A, 23-nov)

Causa medida en Play: no es un SKULL del Tianguis. Son pandilleros de los grupos Bando_Viboras / Bando_Calaveras (Rina_Frontera y Rina_NeonAlto, en modo rina) y de Acoso_* que convergen al MISMO punto (p. ej. (-19, 0, 0) en el Tianguis y (110, 0.3, -44) en la refineria: 12 pandilleros apilados en un punto).
Sus CharacterController no se separan; los de arriba quedan de pie sobre las capsulas de los de abajo (isGrounded=True a y=1.4 a 1.99, un caso a 2.2) y se ven flotando con el arma lista.
Es transitorio (caen al repartirse), pero en el pico se ve exactamente como la captura. Archivos: Assets/_Proyecto/IA/APie/ConductaRina.cs (destino = posicion del rival, sin separacion) y PandilleroAPie.cs (persecucion sin separacion; solo hay separacion en puestos de cubierta).
Arreglo propuesto: separacion entre pandilleros al acercarse (parar a ~0.8 m del companero) o excluir CharacterController de pandilleros entre si (excludeLayers / Physics.IgnoreCollision en Awake), y que las dos rinas no compartan el punto (110,-44).
Captura donde se ve en juego: docs/capturas/_tareas/T67/minimapa-pelea-tianguis.png (pandillero flotando sobre el jugador).
