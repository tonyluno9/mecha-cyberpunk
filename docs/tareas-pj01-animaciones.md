# Tareas: PJ01 (protagonista nuevo) + animaciones de pistola y agachado (Tony, Blender, 7-nov)

Origen: `imageassets/blender/pistola/` (leer primero `agarre.txt`: agarre, tiempos de eventos, qué es cada clip).
Modelo PJ01 (Tripo, rig de 78 huesos, mano derecha = R_Hand): `imageassets/modelos3d/personajes/PJ01/tripo_convert_b5fb242c-...fbx`
(+ su carpeta .fbm de texturas). Mide 0.98 m en el archivo: en el juego 1.80 m.
Los clips son FBX con esqueleto mixamorig, 30 fps, Humanoid: configurarlos igual que los que ya están en
`Assets/Arte/Personajes/Animaciones/` (mismo tipo, mismo avatar de origen).
**No copiar** `*_respaldo.fbx`, `.blend` ni `.blend1`.

**Paso 0 en CADA tarea:** cerrar Unity y Unity Hub, confirmar que no queda ningún Unity.exe, borrar Temp/UnityLockfile; si el candado
vuelve a aparecer, repetirlo.

## A1. Importar los clips
- REEMPLAZAR (ya existen en Animaciones/Pistola/; sustituir solo el .fbx y CONSERVAR su .meta): Pistola_Disparo, Pistola_Correr,
  Pistola_ApuntarCaminar_Derecha, Pistola_ApuntarCaminar_Izquierda.
- AGREGAR en Animaciones/Pistola/ con Loop Time: Pistola_ApuntarLateral_Derecha, Pistola_ApuntarLateral_Izquierda, Pistola_ApuntarAtras,
  Pistola_AgachadoIdle, Pistola_AgachadoCaminar. Sin loop: Pistola_Sacar, Pistola_Guardar.
- AGREGAR en Animaciones/Dano/ sin loop: Pistola_Dano_Frente, Pistola_Dano_Espalda, Pistola_Dano_Fuerte.
- AGREGAR en Animaciones/Locomocion/ con loop: Loco_AgachadoIdle, Loco_AgachadoCaminar, Loco_AgachadoLateral_Derecha,
  Loco_AgachadoLateral_Izquierda, Loco_AgachadoAtras. Sin loop: Loco_Agacharse, Loco_Pararse, Loco_Rodar.
- Sin cambios: Pistola_Apuntar, Pistola_Caminar, Pistola_Idle, Pistola_Recargar, Pistola_RecargarCaminar y los Loco_* de Movimiento_base.
- Prueba: cada clip importa como Humanoid, con su loop correcto y duración > 0.

## A2. PJ01 como modelo del jugador
- Importar PJ01 a `Assets/Arte/Personajes/PJ01/` como Humanoid (mapear el avatar desde sus 78 huesos; R_Hand = mano derecha), a 1.80 m
  con el **Scale Factor** del import (caso A de agarre.txt: lossyScale de R_Hand ≈ 1). Material URP con sus texturas.
- Ponerlo como Visual del jugador (en lugar del actual), conservando lo que funciona: espejo, cámara, collider, ColocarS2.Escala.
- La creación de personaje (sexo, etc.) queda para después: no tocarla, solo que no se rompa.

## A3. Animator del jugador (ControladorBase y sus overrides)
Estados y transiciones con estas reglas:
- La pistola NO se ve aunque esté seleccionada; solo aparece al apuntar: entrar a apuntar = Pistola_Sacar; dejar de apuntar =
  Pistola_Guardar. No hay funda.
- Apuntando: Pistola_Apuntar quieto; caminar adelante Pistola_Caminar; de lado Pistola_ApuntarLateral_Derecha/Izquierda; atrás
  Pistola_ApuntarAtras (blend tree 2D por velocidad local). Disparo con Pistola_Disparo; recarga con Pistola_Recargar / RecargarCaminar.
- Mientras apunta NO corre (correr solo es Loco_Correr, sin pistola).
- Agachado: Loco_Agacharse → Loco_AgachadoIdle / AgachadoCaminar / AgachadoLateral_Derecha/Izquierda / AgachadoAtras → Loco_Pararse.
  Con pistola apuntando agachado: Pistola_AgachadoIdle / Pistola_AgachadoCaminar. **No dispara agachado**: para disparar se para.
- Rodar: Loco_Rodar (1 s, 2.2 m). Usar Apply Root Motion o que el controlador mueva 2.2 m en ese segundo. Al terminar sigue agachado
  (AgachadoIdle o AgachadoCaminar) o se levanta con Loco_Pararse. Quitar la pose procedural de agachado y la rodada simulada anteriores.
- Que lo mismo funcione con los otros personajes que usan ControladorBase (sin romper aliados ni enemigos).

## A4. Animation Events
- Pistola_Sacar a los **0.30 s**: ACTIVAR la pistola en la mano.
- Pistola_Guardar a los **0.53 s**: DESACTIVAR la pistola.

## A5. Capa de daño
- Pistola_Dano_Frente / Espalda / Fuerte (0.47 s, empiezan y terminan en Pistola_Apuntar) en una capa aditiva o de override de
  torso del Animator, para que el jugador siga apuntando y disparando mientras se estremece. Frente o espalda según de dónde viene
  el daño; Fuerte con daño grande.

## A6. Agarre de la pistola en PJ01
- Pistola W01 hija de R_Hand (o del socket ManoDer con 0/0/0 y escala 1). Caso A:
  localPosition (0.0129, 0.1181, 0.0080), localRotation Quaternion (-0.05021, 0.72950, 0.66647, 0.14534) = Euler (-80.74, 115.55, 48.50),
  localScale 1. En Play revisar R_Hand.lossyScale: si sale ≈ 1.84, usar el caso B de agarre.txt.
- Usar `W01_Pistola_agarre.fbx` (pivote en la empuñadura, cañón +Z) si el prefab actual no tiene ese pivote.
- Actualizar `AgarresArmas.cs` (caso "Pistola") y su prueba `ArmaEnManoS1Pruebas.cs` con el agarre nuevo.
- Las armas largas (escopeta, fusil, francotirador) siguen con su agarre actual; revisar que no se rompan con PJ01.

## A7. Verificación
- En Play con capturas en docs/capturas/pj01-*.png revisadas: apuntar, caminar de lado apuntando, retroceder apuntando, agachado (con y
  sin pistola), sacar y guardar, daño frente/espalda/fuerte, rodar. La pistola apunta al frente, en la mano, mide ~21 cm.
- Pruebas: clips importados, reglas del Animator (no corre apuntando, no dispara agachado, la pistola solo visible al apuntar),
  eventos en sus tiempos, agarre. Pruebas del personaje y RecorridoDemoPruebas en verde.
