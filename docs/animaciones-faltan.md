# Animaciones que faltan (T-520)

Los tres personajes de Tripo (woman, man, woman2) traen solo estas animaciones: caminar y correr (los tres), acuerdo/"agree" (los tres, de ahí sale el Idle),
y además enojo ("angry_01", man y woman) y miedo ("afraid", woman; ya se usa como Cubrirse de woman). Todo lo demás se resuelve con trucos mientras no lo bajes:

| Estado (nombre del archivo) | Qué hace hoy sin el archivo | Qué bajar de Mixamo |
| --- | --- | --- |
| ApuntarPistola | Idle + las manos al frente por IK (`ApuntadoIK`) | Pistol Idle |
| DispararPistola | Idle (el arma gira al frente con el IK; el destello y el sonido los pone el arma) | Pistol Fire / Shoot |
| ApuntarLarga | Idle + las dos manos al frente por IK | Rifle Aiming Idle (o Rifle Idle) |
| DispararLarga | Idle | Firing Rifle |
| Recargar | Idle | Reloading |
| Golpe | Idle (el golpe se ve por el puño y el daño) | Punching / Cross Punch |
| RecibirGolpe | Idle | Hit Reaction |
| Caer | El cuerpo se tumba de espaldas en 0.35 s (`VisualPersonaje`) | Falling Back Death |
| Cubrirse | woman usa "afraid"; man y woman2: Idle | Crouch Idle |
| Rodar | El juego gira el modelo (`CubiertaYRodada`) | Forward Roll |

Los clips son Humanoid: UNO de cada sirve para los tres personajes.

## Cómo bajarlas de Mixamo
1. mixamo.com con tu cuenta Adobe. No hace falta subir un personaje: se puede usar el de ejemplo, solo se baja el movimiento.
2. Busca el nombre en inglés de la tabla. Descarga con: Format **FBX for Unity (.fbx)**, **Skin: Without Skin**, **30 fps**, Keyframe Reduction **none**.
3. Deja la casilla **In Place** apagada solo en Rodar; en las demás, enciéndela si la animación se mueve del sitio.

## Cómo dejarlas para que entren solas
1. Renombra cada archivo exactamente como la columna 1, con extensión `.fbx` (`ApuntarPistola.fbx`, `DispararPistola.fbx`, `ApuntarLarga.fbx`, `DispararLarga.fbx`, `Recargar.fbx`, `Golpe.fbx`, `RecibirGolpe.fbx`, `Caer.fbx`, `Cubrirse.fbx`, `Rodar.fbx`).
   También se reconocen los nombres de Mixamo tal cual (`Pistol Idle.fbx`, `Rifle Aiming Idle.fbx`, `Reloading.fbx`...).
2. Cópialos a `Assets/Arte/Personajes/Animaciones/` (una sola carpeta para los tres). Van por Git LFS como el resto del arte.
3. Cierra Unity y corre (desde la raíz del repo, Git Bash):
   `"C:/Program Files/Unity/Hub/Editor/6000.3.25f1/Editor/Unity.exe" -batchmode -quit -projectPath . -executeMethod MechaCyberpunk.Personaje.Visual.Editor.CreadorVisualTripo.CrearTodos -logFile Logs/crear520.log`
   (o el menú Mecha Cyberpunk > Personajes > T-520 Crear visuales de Tripo).
4. Cada clip se importa Humanoid, en el sitio, con bucle donde toca (Idle, Caminar, Correr, ApuntarPistola, ApuntarLarga, Cubrirse), y se mete en el
   controlador de los tres personajes. Si traes **ApuntarPistola y ApuntarLarga**, las manos dejan de ir por IK solas (`ApuntadoIK.ManosPorIK`).
5. Revisa en `Logs/crear520.log` que no salga "Animación ignorada (nombre no reconocido)". Los prefabs del juego no cambian: ya usan estos controladores.

## T-510
Los siete modelos nuevos (SkullHombre, Doctor, Inventor, Hacker, RobotHombre, GuardiaNova, Mecanico) no traen ningún clip en su FBX. Usan los de woman2 y woman, así que los clips de Mixamo de arriba también les sirven. Al 2-nov-2026 no hay clips de Mixamo en `Assets/Arte/Personajes/Animaciones/` (la carpeta no existe): no se corrió la conexión. Hay además dos clips de la gente de fondo que salen de los FBX: "AcuerdoCompleto" (agree, asentir) y "Enojo" (angry, de man y woman).
