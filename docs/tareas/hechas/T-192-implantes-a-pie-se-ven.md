# T-192 Brazos y piernas a pie, y los implantes se ven en el cuerpo
Dueño: Tony | Rama: tony/T-192-implantes-a-pie | Modelo: Sonnet 5.5, medium
Depende de: T-190, T-191

## Objetivo
Con brazos instalados el golpe a pie pega más, con piernas corres más rápido y saltas más alto, y los 4 implantes se ven en el cuerpo del
jugador (espejo, vista previa) y, en primera persona, en el puño. Es la mitad "a pie" del criterio "un implante instalado se ve en mi
apariencia y cambia lo que dice que cambia".

## Contexto
- Diseño: personaje-progresion.md (Brazos: más daño a pie; Piernas: corres y saltas más; "Un implante instalado se ve en mi apariencia");
  estetica-hud.md (paleta: turquesa, magenta, naranja; nada en rojo puro).
- Decisiones: D-tony-15 (el jugador a pie no es IPeleador), D-tony-63 (cuerpo greybox, no se ve el propio cuerpo en primera persona),
  D-tony-122 (sin cambiar materiales en ejecución: `sharedMaterial` o `MaterialPropertyBlock`). Propuestas: D-tony-165 (efectos),
  D-tony-167 (cómo se ven).
- Contratos: `IImplantes`, `EfectosImplantes` (T-190). Se busca `Registro<IImplantes>.Actual`; sin él, `EfectosImplantes.Ninguno`
  (el prólogo y Pruebas_APie no cambian). Hay que reaccionar a `Cambio` y a `Registro<IImplantes>.Agregado` (el servicio puede llegar
  después que el jugador).
- Código: `Personaje/Movimiento/ControladorAPie.cs` (lee `datos.VelocidadCorrer` y `datos.AlturaSalto` y se los pasa a `MovimientoAPie`),
  `Personaje/Combate/GolpeAPie.cs` (daño desde `DatosPersonaje.DanioGolpe`; el puño es un cubo hijo de Manos),
  `Personaje/Apariencia/CuerpoJugador.cs` y `Personaje/Editor/CreadorApariencia.cs` (primitivas por implante cosmético; `CuerpoJugador.prefab`,
  `VistaPreviaApariencia.prefab`), `Personaje/Editor/CreadorJugadorAPie.cs`.
- Pruebas modelo: `Tests/PlayMode/Personaje/ControladorAPiePruebas.cs`, `GolpeAPiePruebas.cs`, `CuerpoJugadorPruebas.cs`, `BaseJugadorAPie.cs`.

## Qué hacer
1. A pie: `ControladorAPie` usa `VelocidadCorrer × multiplicadorCorrer` y `AlturaSalto × multiplicadorSalto`; caminar no cambia.
   `GolpeAPie` pega `DanioGolpe × multiplicadorGolpeAPie`. La pistola no cambia (D-tony-165). Si el efecto cambia a media carrera o a medio
   salto, el cambio entra en el siguiente cálculo, sin saltos de posición.
2. El cuerpo (`CuerpoJugador` y su creador): una pieza greybox por espacio, prendida según `IImplantes.Nivel`, independiente del implante
   cosmético: OJOS una banda delgada sobre los ojos con brillo turquesa; BRAZOS los dos antebrazos cromados; PIERNAS espinillas cromadas;
   SISTEMA NERVIOSO tres placas en la nuca. Nivel 2: la misma pieza con una franja magenta. `CuerpoJugador` tiene una forma de mostrar un
   implante "de prueba" encima de lo instalado (`MostrarPrueba(espacio, nivel)` y `QuitarPrueba()`), para la vista previa de la clínica
   (T-193). `VistaPreviaApariencia` y el espejo ya usan `CuerpoJugador`: no hace falta otra cosa para que se vea ahí.
3. Primera persona: con BRAZOS el puño (y el antebrazo si lo hay) se ve cromado (franja magenta en el nivel 2). Nada más del cuerpo se ve.
4. Herramienta de prueba `Progresion/Pruebas/ImplantesDePrueba.cs` (solo escenas de Tony, como `DineroDePrueba`): con una tecla instala los 4 de
   nivel 1 sin cobrar, con otra los de nivel 2 y con otra los quita (`QuitarTodos`). Elige teclas que no choquen con las que ya existen en las
   escenas de prueba (anótalas en la bitácora y en la ayuda de la herramienta).
5. Pruebas:
   - EditMode `Tests/EditMode/Personaje/EfectosAPiePruebas.cs`: el cálculo de velocidad al correr, altura de salto y daño con `Ninguno`, con
     PIERNAS I y II y BRAZOS I y II (valores desde los assets).
   - PlayMode `Tests/PlayMode/Personaje/ImplantesAPiePruebas.cs` con el prefab del jugador a pie y un doble de `IImplantes` registrado en la
     prueba, con entrada simulada: correr (Shift + W) 3 s mide la distancia y la compara con base × multiplicador (±5 %); saltar (Espacio) mide la
     altura máxima de los pies contra la pedida (±5 %); clic izquierdo frente a un `MunecoAPie` mide el daño (exacto); sin doble registrado todo
     es como antes; cambiar el doble a media prueba cambia el siguiente golpe.
   - PlayMode en `CuerpoJugadorPruebas.cs` (o archivo nuevo): cada espacio y nivel prende solo su pieza; quitar los implantes las apaga;
     `MostrarPrueba` se suma a lo instalado y `QuitarPrueba` lo devuelve; el implante cosmético sigue funcionando igual.
   - Captura docs/capturas/T-192-1.png: la vista previa del cuerpo con los 4 implantes de nivel 2 (y el implante cosmético "Ojo LED rojo"),
     de frente y de lado; T-192-2.png: el puño cromado en primera persona.

## Carpetas permitidas
Assets/_Proyecto/Personaje/ (Movimiento/, Combate/, Apariencia/, Editor/, Prefabs/ solo con creadores), Assets/_Proyecto/Progresion/Pruebas/ImplantesDePrueba.cs,
Assets/_Proyecto/Tests/EditMode/Personaje/, Assets/_Proyecto/Tests/PlayMode/Personaje/, docs/capturas/
No tocar: Core/, Progresion/Implantes/ (solo se usa), Mecha/, UI/, escenas (la herramienta la coloca T-193), todo lo de Alan.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa (salvo los fallos conocidos de Alan en Arena)
- [ ] EfectosAPiePruebas e ImplantesAPiePruebas pasan 3 corridas seguidas; la bitácora imprime distancia, altura y daño medidos contra los
      esperados para cada caso
- [ ] Las pruebas viejas a pie y del prólogo siguen en verde (ControladorAPiePruebas, GolpeAPiePruebas, CuerpoJugadorPruebas,
      CreacionPersonajePruebas, RecorridoPrologoPruebas)
- [ ] Correr los creadores de Personaje dos veces no cambia los prefabs (md5 igual); ningún `.mat` cambia al dar Play
- [ ] Capturas T-192-1 y T-192-2 revisadas a ojo: cada implante se distingue y ninguno usa rojo puro

## Criterios de sensación (para un humano)
- Pruebas_APie con la herramienta de prueba: ¿correr con PIERNAS II se siente rápido sin perder el control en las esquinas? ¿El salto más
  alto se siente ágil o flotado? ¿Pegar con BRAZOS II se nota en el muñeco (parpadeo, KO más rápido)?
- En el espejo: ¿se ve que traes cromo, aunque sea greybox?

## Pruebas automáticas
- EditMode: EfectosAPiePruebas. PlayMode: ImplantesAPiePruebas, CuerpoJugadorPruebas.

## Fuera de alcance
- La clínica (T-193), ojos y sistema nervioso (T-194), enemigos a pie (T-195), modelos finales de los implantes (arte, Alan o una tarea de
  arte), sonido. Hackeo.
