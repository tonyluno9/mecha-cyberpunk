# T-1318: techos (material de azotea, pretiles saltables, sin pared invisible)

Parte del punto 17 de docs/playtest-17nov.md.

## Qué se hizo
- Material nuevo `S2c_Azotea` (KitS2c.MatAzotea): concreto gastado del barrio (Barrio_Concreto) con tinte rojizo de impermeabilizante, misma escala en metros que la calle.
  Va en una losa de 2 cm sin collider ("AzoteaT1318") sobre la cara de arriba de cada azotea caminable activa: 133 (casas del kit, casas de CalleDoc y edificios bajos del barrio).
  No toca muros ni fachadas. KitS2c.Casa también la pone, para que una pasada futura no la pierda.
- Pretiles C05: el collider del prefab bajó de 1.00 m a 0.4 m (ArteT250.AltoColliderPretil, y ArteT250 lo repite si se regenera el prefab). La malla visible sigue en 1.00 m.
  alturaSalto del jugador es 0.45 m y el escalón máximo del controlador 0.4 m (solo se leyeron, no se cambiaron).
- Script único e idempotente: `Assets/Arte/Editor/ColocarT1318Techos.cs` (`Aplicar()` para -executeMethod; `Capturas()` para las fotos). Corrido dos veces: 133 losas las dos.

## Qué era la pared invisible
No existía un collider invisible en los bordes de los techos: se midieron todos los colliders sin malla visible (parkour, accesos, tablones, límites del mapa y de la jaula) y ninguno estaba en un borde de azotea.
La "pared" eran los 939 pretiles C05: su collider medía 1.00 m (igual que la malla), más de lo que se salta (0.45 m), así que de lado se sentía como pared sin salida.
Ningún collider de azotea sube más que su malla; `Aplicar` recorta si alguno lo hiciera (0 recortes) y quita colliders invisibles de muro/borde sobre techos (0 quitados; el filtro excluye puentes, jaula y límites del mapa).

## Pruebas
- EditMode `TechosT1318Pruebas` (4): collider de todo pretil <= alturaSalto - 0.05, prefab con collider bajo y malla de 1 m, sin invisibles de borde ni collider sobre la malla, toda azotea activa con su losa y el material.
  EditMode de Mundo completo: 372 de 372. PlayMode (TechosSalto, TechosS2, EscalerasTechosT1101, TreparAPie, EmboscadaTecho, PoliciaEscalera): 25 corridas, 23 pasan, 0 fallan, 2 omitidas por la clase. EscalerasTechosT1101.EscalerasNuevas_Caminar... se ajustó (T-1318): ya no es falla bajar por encima del pretil si hay suelo debajo.
- PlayMode `TechosSaltoT1318Pruebas` (1): jugador real en Ciudad_Prologo camina a un pretil y salta: cruza el pretil y cae del techo (>2 m).
  `TechosS2Pruebas.Pretil_CaminarCinco...` se anotó con T-1318 (caminar sin saltar sigue sin cruzar el pretil).
- Capturas en docs/capturas/t-1318/: azotea-desde-otro-techo.png, pretil-de-cerca.png, techos-desde-arriba.png.

## Decisiones
- Collider a 0.4 m y no ~1 m: el salto es 0.45 m (1 m no se salta) y el escalón del controlador 0.35 m (con 0.35 el jugador subía el pretil caminando y se caía del techo: lo vio EscalerasTechosT1101). La malla se queda igual (a pedido); al saltar se atraviesa visualmente la parte alta del pretil.
- Se probó asfalto con tinte (salió muy oscuro) y luego concreto; se quedó concreto rojizo.
- Los Casa_ inactivos (12 del kit, 43 edificios del barrio) no llevan losa: están apagados en la escena.
- No se corrió ningún Colocar viejo ni CreadorTodo.

## Ajustes finales (revisión del coordinador)
- Material: concreto del barrio (albedo, gris cálido 0.6/0.57/0.54) con la normal del asfalto de la calle (el grano se ve de cerca), UV a 2 m. El asfalto con tinte salía casi negro de lejos (opción B del encargo). Capturas de día y de noche (azotea-noche.png, hora 23).
- Pretil: malla visible a 0.5 m (padre "EscalaAlto" sin giro dentro del prefab C05, escala Y; el FBX trae giros y escalar el hijo no bajaba el alto) y collider a 0.4 m. ArteT250 y ColocarT1318Techos lo hacen igual. RevisarCiudadT995 salta los pretiles por el nombre del padre.
- Pruebas finales: EditMode Mundo 373/373; PlayMode (TechosSalto, TechosS2, EscalerasTechosT1101, TreparAPie, EmboscadaTecho): 24 corridas, 22 pasan, 0 fallan, 2 omitidas.
