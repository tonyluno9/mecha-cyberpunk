# T-076 Arte de Blender a Unity: FBX, materiales y prefabs del kit urbano y del bar
Dueño: Alan | Rama: alan/T-076-arte-fbx-materiales-prefabs | Modelo: Sonnet 5.5, medium
Depende de: nada (sale de alan/fase-2). Lo usan T-080 a T-083 y T-085.

## Objetivo
Los 9 props del kit urbano y los 4 del bar, hechos en Blender, están en Unity como modelos FBX con materiales URP (neón que
brilla de verdad) y un prefab con colliders por pieza, listos para vestir el barrio. Es el primer paso para que el barrio
"se vea chingón, no sencillo" (docs/revision-alan.md, revisión de la fase 1).

## Contexto
- Las fuentes ya están en el repo, en `Assets/Arte/Fuentes~/` (Unity ignora las carpetas que terminan en `~`):
  `kit-urbano/` (kit_urbano.py, 9 .glb: poste_luz, puesto_tacos, letrero_tacos, letrero_farmacia, letrero_cantina,
  azotea_ac_tinaco, transformador_poste, basurero_grafiti, fachada_modular; y renders .png de referencia) y `bar/` (bar.py,
  bar2.py, prop_barra, prop_mesa, prop_dj, prop_letrero en .glb y .png). En Blender están en metros, con el origen en la base y
  el frente hacia -Y. Lee el encabezado de kit_urbano.py y las tablas de materiales de los dos scripts.
- El proyecto NO tiene glTFast, así que los .glb no sirven en Unity. Los .glb que hoy están en `Assets/Arte/Mundo/Bar/` se borran
  (con su .meta); se quedan solo en Fuentes~.
- Blender headless: `"C:\Program Files\Blender Foundation\Blender 5.2\blender.exe" -b -P <script>.py -- <args>` (la ruta
  está en .claude/maquina.local.md). Propuesta D-alan-18 en docs/decisiones-pendientes-alan.md.
- Regla 6 de CLAUDE.md: los assets de Unity se crean con código de editor, nunca escribiendo YAML a mano.

## Qué hacer
1. `Assets/Arte/Fuentes~/exportar_fbx.py`: importa cada .glb de kit-urbano/ y bar/ y lo exporta como FBX a
   `Assets/Arte/Mundo/KitUrbano/<nombre>.fbx` y `Assets/Arte/Mundo/Bar/<nombre>.fbx` (metros, Y arriba, solo mallas, sin
   cámaras ni luces). Escribe además `Assets/Arte/Materiales/materiales.json` (por material: color base, rugosidad, metálico,
   color y fuerza de emisión) y `Assets/Arte/Mundo/dimensiones.json` (ancho, alto y fondo de cada pieza en metros, ya en ejes de
   Unity). Si dos fuentes traen un material con el mismo nombre y valores distintos, gana el del kit urbano y se anota. El script
   es reproducible: correrlo dos veces da los mismos archivos.
2. Unity, en `Mundo/Editor/` (el asmdef Mundo.Editor ya existe):
   - Un `AssetPostprocessor` para modelos bajo `Assets/Arte/`: escala del archivo a 1 unidad = 1 m, conversión de ejes horneada
     (raíz sin rotación), sin animaciones, sin cámaras ni luces, normales del archivo, y materiales remapeados por nombre a los de
     `Assets/Arte/Materiales/`.
   - `CreadorArte.Crear` (`[MenuItem("Mecha Cyberpunk/Crear arte (T-076)")]`): crea o actualiza un `.mat` URP/Lit por material del
     JSON (los de emisión con `_EMISSION` y color HDR = color × fuerza × un factor de `ConfigArte`; los "Vidrio" transparentes),
     reimporta los FBX y crea un prefab por pieza en `Mundo/Prefabs/Kit/` con estos nombres, que usan las tareas siguientes:
     Poste_Luz, Puesto_Tacos, Letrero_Tacos, Letrero_Farmacia, Letrero_Cantina, Azotea_AC_Tinaco, Transformador_Poste,
     Basurero_Grafiti, Fachada_Modular, Prop_Barra, Prop_Mesa, Prop_DJ y Prop_Letrero (raíz vacía con el modelo de hijo, `BoxCollider`s
     simples donde se puede chocar: base del poste, puesto, basurero, transformador, barra, mesas, cabina de DJ; letreros y
     azoteas sin collider), con la base en y = 0 y el frente hacia +Z.
   - Agrega `CreadorArte.Crear` al principio de `CreadorTodo.Crear` (antes que CreadorBarrio, porque los creadores que siguen
     usarán estos prefabs) y actualiza su comentario.
3. Escena de vitrina NO guardada: un método de editor `CreadorArte.Vitrina()` que abre una escena nueva sin guardar con las 13
   piezas en fila, de noche, con una cámara, para las capturas.

## Carpetas permitidas
Assets/Arte/ (Fuentes~/, Materiales/, Mundo/), Assets/_Proyecto/Mundo/Editor/, Assets/_Proyecto/Mundo/Datos/ (ConfigArte),
Assets/_Proyecto/Mundo/Prefabs/Kit/, Assets/_Proyecto/Tests/EditMode/Mundo/
No tocar: Core/, Packages/ (no se instala glTFast), ProjectSettings/, las escenas, Barrio.prefab (lo visten T-080 a T-083).

## Criterios de aceptación (los comprueba el verificador)
- [ ] El script de Blender corre headless sin errores y deja 13 FBX, materiales.json y dimensiones.json (salida del comando)
- [ ] `git ls-files Assets/Arte/Mundo` no muestra ningún .glb; los .fbx y .png quedan en LFS (`git lfs ls-files`)
- [ ] `bash herramientas/verificar.sh todo` pasa
- [ ] EditMode `ArtePruebas`: cada uno de los 13 prefabs existe; el tamaño de sus renderers coincide con dimensiones.json
      (±2 %); su base está en y = 0 (±2 cm); la raíz no tiene rotación ni escala distinta de 1
- [ ] EditMode: ningún renderer de los prefabs usa un material fuera de `Assets/Arte/Materiales/` ni el material por defecto;
      todos son URP/Lit; los de neón tienen `_EMISSION` y emisión con algún canal ≥ 2 (HDR)
- [ ] EditMode: el frente queda hacia +Z: en fachada_modular, el promedio de z de los vértices del material Concreto_Manchas
      (zócalo y hueco del local, que están al frente) es mayor que el centro de su caja
- [ ] EditMode: los prefabs con collider (poste, puesto, basurero, transformador, barra, mesa, DJ) tienen al menos un
      `BoxCollider` que no es trigger; letreros y azotea no tienen collider
- [ ] Correr `CreadorTodo.Crear` dos veces seguidas deja `git status` igual que después de la primera
- [ ] Captura de la vitrina de noche con las 13 piezas y el neón encendido: docs/capturas/T-076-1.png; y de cerca el poste, el
      puesto de tacos y la fachada: T-076-2.png

## Criterios de sensación (para un humano)
- Las piezas se ven igual que los renders de Blender (Fuentes~/*.png): mismos colores, el neón brilla y nada sale negro,
  rosa (shader roto) ni volteado.

## Pruebas automáticas
- EditMode (Tests/EditMode/Mundo/): ArtePruebas (tamaños, orientación, materiales y colliders).

## Fuera de alcance
- Colocar las piezas en el barrio (T-080 a T-083); posproceso y bloom (T-079)
- Piezas nuevas de Blender (letreros: T-082; carro: T-085); LODs; texturas pintadas a mano
