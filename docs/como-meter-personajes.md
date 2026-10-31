# Cómo meter un personaje 3D (modelo de Mixamo)

Para Tony y Alan. Un personaje = una carpeta con FBX + un comando. El código del juego no cambia: el modelo vive en un prefab `Visual_<Nombre>` con su Animator, y un componente puente (`VisualPersonaje`) lo mueve.

## 1. Descargar de Mixamo (mixamo.com, gratis con cuenta Adobe)
1. Sube o elige el personaje. Descarga **uno**: Format **FBX for Unity (.fbx)**, **Skin: With Skin**, sin "Keyframe Reduction" (o "none"), Pose **T-pose**. Guárdalo como `Modelo.fbx`.
2. Para cada animación (busca el nombre en inglés, ver tabla): Format **FBX for Unity (.fbx)**, **Skin: Without Skin**, **30 fps**, Keyframe Reduction **none**. Marca **In Place** en las de caminar, correr y saltar. Guarda con el nombre de la tabla.

| Archivo | Qué es | Sugerencia en Mixamo |
| --- | --- | --- |
| Modelo.fbx | el personaje (With Skin) | cualquiera |
| Idle.fbx | quieto | Breathing Idle |
| Caminar.fbx | caminar en el sitio | Walking (In Place) |
| Correr.fbx | correr en el sitio | Running (In Place) |
| Saltar.fbx | salto | Jump |
| Golpe.fbx | puñetazo | Punching / Cross Punch |
| RecibirGolpe.fbx | recibir golpe | Hit Reaction |
| Caer.fbx | caer / KO | Falling Back Death |
| ApuntarPistola.fbx | apuntar pistola (bucle) | Pistol Idle |
| DispararPistola.fbx | disparar pistola | Pistol Fire / Shoot |
| ApuntarLarga.fbx | apuntar arma larga (bucle) | Rifle Idle / Rifle Aiming Idle |
| DispararLarga.fbx | disparar arma larga | Firing Rifle |
| Recargar.fbx | recargar | Reloading |
| Cubrirse.fbx | agachado en cubierta (bucle) | Crouch Idle |
| Rodar.fbx | rodar | Forward Roll |

También se reconocen los nombres en inglés tal cual (Walking.fbx, Running.fbx, Hit Reaction.fbx, Rifle Idle.fbx...). Si falta alguno no pasa nada: ese estado queda sin movimiento y el comando lo avisa.

## 2. Carpeta
Pon todo en `Assets/Arte/Personajes/<Nombre>/` (por ejemplo `Assets/Arte/Personajes/Pandillero1/`). Los FBX van por Git LFS como el resto del arte.

## 3. El comando
- Menú: selecciona la carpeta `<Nombre>` en el Project y usa **Mecha Cyberpunk > Personajes > Crear visual desde FBX**.
- Por consola (Unity cerrado): `Unity.exe -batchmode -quit -projectPath . -executeMethod MechaCyberpunk.Personaje.Visual.Editor.CreadorVisualPersonaje.CrearDesdeLineaDeComandos -nombre Pandillero1` (sin `-nombre` rehace todas las carpetas con FBX).
Hace: importa el modelo como Humanoid, copia su Avatar a las animaciones, las deja en el sitio y con bucle donde toca (Idle, Caminar, Correr, ApuntarPistola, ApuntarLarga, Cubrirse), arma `Assets/Arte/Personajes/<Nombre>/<Nombre>.controller` y el prefab `Assets/_Proyecto/Personaje/Visual/Prefabs/Visual_<Nombre>.prefab`. Se puede repetir sin miedo: actualiza lo mismo.

## 4. Qué revisar
- En la consola no hay avisos "falta la animación" ni "no generó un Avatar Humanoid". Si sale lo segundo, el FBX del modelo no era humanoide o no tenía piel.
- Abre `Visual_<Nombre>.prefab`: el hijo `Modelo` tiene Animator con el controller y el Avatar; Apply Root Motion apagado.
- Arrastra el prefab a una escena de prueba, en Play cambia a mano los parámetros del Animator (Velocidad, Apuntando, TipoArma...) y mira que cada estado se vea bien y no se hunda ni flote.
- Si una animación se desplaza o gira sola, vuelve a bajarla con In Place.

## Estados y parámetros del Animator
Estados: Idle, Caminar, Correr, Saltar, Golpe, RecibirGolpe, Caer, ApuntarPistola, DispararPistola, ApuntarLarga, DispararLarga, Recargar, Cubrirse, Rodar.
Parámetros: `Velocidad` (float, m/s: >0.1 camina, >4 corre), `EnSuelo` (bool), `Apuntando` (bool), `TipoArma` (int: 0 ninguna, 1 pistola, 2 arma larga), `Cubierto` (bool), `Caer` (bool), triggers `Disparar`, `Recargar`, `Golpe`, `Herido`, `Rodar`.

## VisualPersonaje (el puente)
`Personaje/Visual/VisualPersonaje.cs`, en la raíz del prefab. Con `Leer del padre` activo calcula solo `Velocidad` y `EnSuelo` del CharacterController (o Rigidbody, o del movimiento) del objeto padre: no necesita referenciar nada del juego. Lo demás se le da por código: propiedades `Apuntando`, `TipoArma`, `Cubierto`, `Caido`, y métodos `Disparar()`, `Recargar()`, `Golpear()`, `Herir()`, `Rodar()`.

## Qué tiene que conectar la Sesión A (después, no está hecho)
Hoy el muñeco de prueba (T-400) usa otro Animator (`Velocidad`, `EnPiso`, `Arma`, `KO`, `Disparo`, `Recarga`). Para usar `Visual_<Nombre>`:
1. Cambiar el modelo dentro de `JugadorAPie.prefab`, `ModeloHumano.prefab`/`ModeloJugador.prefab` (Assets/Arte/Personajes/MunecoHumano/) o de cada prefab de pandillero/aliado/profe/peatón por una instancia de `Visual_<Nombre>` (VisualPersonaje en la raíz, sin quitar el `Animator` que buscan los scripts: está en el hijo `Modelo`).
2. En `Personaje/Jugador/AnimacionJugador.cs` (`Update` y `AlDisparar`), `IA/APie/PandilleroAPie.cs` (~líneas 271, 447, 504-507, 723) y `IA/Aliados/AliadoAPie.cs` (~268-271, 301): reemplazar los `animador.Set...` por `VisualPersonaje` (Velocidad/EnSuelo salen solos; `Apuntando`, `TipoArma` = Pistola o ArmaLarga, `Caido` = KO, `Disparar()`, `Recargar()`, `Golpear()`). Para cubrirse y rodar: `Personaje/Movimiento/CubiertaYRodada.cs` (`Cubierto`, `Rodando`) llama `visual.Cubierto = ...` y `visual.Rodar()`. Para recibir daño: `Personaje/Vida/VidaAPie.cs` llama `visual.Herir()`.
3. Alternativa sin tocar scripts: conservar los nombres viejos añadiéndolos como parámetros extra al controller (`EnPiso`, `Arma`, `KO`, `Disparo`, `Recarga`) — no se hizo para no mezclar dos juegos de nombres.
