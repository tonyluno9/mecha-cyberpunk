# T-1310: clips de Blender ronda 2 (playtest 17-nov, punto 10)

Rama `tony/T-1310-clips-ronda2`. Escrito sin abrir Unity: NADA está compilado ni probado todavía.

## Para terminar (con Unity cerrado, desde la raíz)
```
"/c/Program Files/Unity/Hub/Editor/6000.3.25f1/Editor/Unity.exe" -batchmode -quit -projectPath . -logFile Logs/t1310.log -executeMethod MechaCyberpunk.Personaje.Editor.ImportarClipsRonda2T1310.Aplicar
```
Luego `bash herramientas/verificar.sh todo`. `Aplicar` importa los clips, crea `Personaje/Datos/Resources/ConfigAnimacionParkour.asset`, borra la copia limpia vieja de Moto_Bajar y reconstruye el controlador base. Sin correrlo, el parkour no hace nada (la config no existe). Log con prefijo `[T-1310]`.

## Qué quedó
- FBX copiados a `Assets/Arte/Personajes/PJ01/Clips/` (los existentes conservan su .meta; los nuevos los crea Unity): Caminar x2, Loco_Idle x2, Escalera x5, Moto_Bajar y los nuevos Parkour x3, CoberturaRifle x3, Carro_SacarConductor, Conductor_SerSacado.
- a) Caminar e idle: reemplazan a los actuales por el mismo nombre (el controlador ya elige con/sin implante por el parámetro ConImplante).
- b) Parkour (`TreparAPie`, `CaidaJugador`, `ReglaParkour`, `ConfigAnimacionParkour`): espacio frente a un muro de 0.6 a 1.3 m y menos de 0.8 m de grosor pasa al otro lado con SaltarBarda; un borde de 1.3 m o más usa TreparBorde (el agarre dura lo que el clip); al aterrizar de más de 4 m rueda con RodarCaida, pierde 2 por metro sobre 4 (tope 30 % de la vida, nunca la deja en 0) y no muere.
- c) Moto_Bajar: el clip nuevo reemplaza al .anim limpio (borrado); PoseMoto mide el recorrido del clip, no cambia.
- d) Cobertura con rifle: estados nuevos CoberturaRifleEntrar/Idle/Disparo en el controlador base, se eligen al cubrirse con el parámetro nuevo `ArmaLargaEquipada` (VisualPersonaje).
- e) Robo de carro, solo el lado del conductor: `ConductorRobado` (Trafico) manda `SerSacado` y `VisualPersonaje` juega PJ01_Conductor_SerSacado acelerado para durar sacar + caer (1.25 s).

## Qué no quedó
- e) El clip del jugador PJ01_Carro_SacarConductor está en la config pero no se juega: `AnimacionCarro` sube al asiento sin esperar y meter 2.67 s antes de Carro_Entrar toca Vehiculos y el tiempo del montable.
- El alineado fino con el hueco de la puerta del conductor.
- Nada probado en Unity: revisar a ojo SaltarBarda, TreparBorde y RodarCaida (los clips traen avance y altura en la cadera; aquí el movimiento lo hace el código y el clip va en el sitio, como SalirArriba de la escalera).
- Escala de importación: se usa la de `ImportadorClipsPJ01` (1.88, la de PJ01.fbx), no 1.8367 de la bitácora.

## Pruebas nuevas
- EditMode `ReglaParkourT1310Pruebas` (clasificar, rodar, daño de caída).
- PlayMode `ClipsRonda2T1310Pruebas`: config con los 5 clips, clips de cobertura y moto existen, saltar una barda deja del otro lado, caer de 8 m no mata y juega RodarCaida.
- `ClipsPJ01JugadorT985Pruebas` ahora espera 83 clips en el controlador (excluye parkour y robo de carro).
