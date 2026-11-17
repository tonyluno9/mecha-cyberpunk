# T-1311: prótesis de ataque en el cuerpo (PJ01_Implantes_Ataque.fbx)

Rama: tony/T-1311-protesis-ataque. Dueño: Tony. Origen: docs/playtest-17nov.md punto 11.

## Qué hay
- Assets/Arte/Personajes/PJ01/Implantes/PJ01_Implantes_Ataque.fbx y texturas IMP04_Ataque_* e IMP05_Ataque_* (basecolor y normal, copiadas de la carpeta .fbm). Los .meta los crea Unity al importar.
- Editor/ColocarProtesisAtaqueT1311.cs: ajusta la importación (misma escala que PJ01_Implantes.fbx), crea los materiales URP y mete en Visual_PJ01 (Giro/Modelo/ImplantesAtaque) las dos mallas pesadas a los huesos de PJ01 (apagadas) y los puntos de salida SalidaLaser (hijo de R_Forearm, copia de IMP04_Salida) y SalidaMisiles (hijo de Spine02, copia de IMP05_Salida). Idempotente, log "[T-1311]".
- Habilidades/PiezasProtesisAtaquePJ01.cs: guarda las piezas y salidas. ProtesisVisiblesAPie las usa (si el cuerpo las trae) en vez del prefab de T-1018: encendidas solo con la prótesis; ProtesisAtaqueAPie ya dispara desde PuntaLaser y SalidaMisiles.
- Pruebas: Tests/PlayMode/Personaje/ProtesisAtaqueCuerpoT1311Pruebas.cs. Se aflojaron VerificarModelo y la prueba de seguir el hueso en ProtesisEnElCuerpoT1018Pruebas (ahora la pieza es una malla pesada, no hija del hueso).

## Comando (con Unity cerrado, desde la raíz)
```
"/c/Program Files/Unity/Hub/Editor/6000.3.25f1/Editor/Unity.exe" -batchmode -quit -projectPath . -executeMethod MechaCyberpunk.Personaje.Editor.ColocarProtesisAtaqueT1311.Aplicar -logFile Logs/t1311.log
```
Después: commitear Visual_PJ01.prefab, los .meta nuevos y los materiales.

## Pendiente (no se pudo verificar, sin Unity)
- Nada se compiló ni corrió. Falta correr el comando, las pruebas T1311 y T1018 y revisar una captura (los límites de tamaño de T1018 se aflojaron a ciegas).
