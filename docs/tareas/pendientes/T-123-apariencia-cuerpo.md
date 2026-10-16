# T-123 Apariencia del jugador: catálogo, cuerpo y vista previa
Dueño: Tony | Rama: tony/T-123-apariencia-cuerpo | Modelo: Sonnet 5.5, medium
Depende de: T-120

## Objetivo
El jugador tiene una apariencia que se elige (cara, ropa e implante visible), se guarda en su perfil y se ve en un cuerpo low-poly
de greybox. Existe una vista previa reutilizable (cámara, luz y el cuerpo girando) que usan la pantalla de creación (T-126) y el
espejo de la casa (T-130).

## Contexto
- Diseño: personaje-progresion.md ("eliges nombre, alias y apariencia (cara, ropa e implantes visibles)"; "tu apariencia se ve en el
  espejo de tu casa y en las cinemáticas"), demo.md ("pocas opciones de apariencia"), estetica-hud.md (low-poly, neón azul, magenta,
  naranja y turquesa).
- Contrato: `AparienciaJugador` e `IPerfilJugador.Apariencia` (T-120). Hoy `ServicioPerfil` (Progresion/Perfil/) devuelve
  `Predeterminada` y guarda solo nombre y alias (clave `perfil`, T-111).
- Decisiones: D-005 (datos en assets del área), D-055 (un solo prefab del jugador; los servicios anidados), D-tony-50 (guardado).
  Propuesta nueva: D-tony-63.
- Personaje/ no referencia Progresion: el cuerpo lee la apariencia por `Registro<IPerfilJugador>` (Core) o se la pasan.
- Nada de arte final: primitivas de Unity con materiales URP de color (como el greybox del mecha y los muñecos).

## Qué hacer
1. `Personaje/Datos/CatalogoApariencia.cs` (ScriptableObject) y `CatalogoApariencia_Base.asset` (con su creador de editor): listas de
   opciones con nombre corto para la pantalla:
   - Caras (4): tono de piel y peinado (por ejemplo "Rapado", "Cresta", "Trenza", "Gorra"); el peinado es una pieza de primitivas.
   - Ropa (4): chamarra magenta, sudadera turquesa, chaleco naranja, overol azul (color y una pieza distintiva).
   - Implantes (4, el 0 es "Ninguno"): brazo cromado, ojo LED rojo, placa en la sien.
   Los nombres y colores son propuesta (D-tony-63) y se cambian en el asset.
2. `Progresion/Perfil/ReglaApariencia.cs` (pura): normaliza una apariencia (índices negativos a 0). `ServicioPerfil` implementa
   `Apariencia` de verdad: `FijarApariencia(AparienciaJugador)` (normaliza, avisa `Cambio` solo si cambió), la guarda y la carga con
   el perfil (campos `cara`, `ropa`, `implante` en el JSON de la clave `perfil`; un texto viejo sin esos campos carga nombre y alias y
   deja la apariencia en `Predeterminada`).
3. `Personaje/Apariencia/CuerpoJugador.cs` + `Personaje/Prefabs/CuerpoJugador.prefab` (con creador): un cuerpo de persona de unos
   1.75 m hecho de primitivas (cabeza, torso, brazos, piernas) que aplica una `AparienciaJugador` con el catálogo (índices fuera de
   rango se toman con módulo, sin errores). Si no le pasan una, sigue a `Registro<IPerfilJugador>.Actual` y se repinta en `Cambio`.
   No tiene colliders ni sombras que estorben a la cámara del jugador.
4. `Personaje/Apariencia/VistaPreviaApariencia.cs` + `Personaje/Prefabs/VistaPreviaApariencia.prefab` (con creador): un escenario
   pequeño lejos del mundo (por ejemplo a 1000 m bajo el piso) con `CuerpoJugador`, una luz y una cámara que dibuja en una
   `RenderTexture` (expuesta para que una `RawImage` de la UI la muestre); el cuerpo gira despacio y se puede girar con ← → si la
   pantalla lo pide. Se prende y apaga (`Mostrar(bool)`) sin dejar cámaras activas cuando está apagada. No debe sumar un
   `AudioListener`.

## Carpetas permitidas
Assets/_Proyecto/Personaje/ (Datos/, Apariencia/ nueva, Prefabs/, Editor/), Assets/_Proyecto/Progresion/Perfil/,
Assets/_Proyecto/Tests/EditMode/Personaje/, Assets/_Proyecto/Tests/EditMode/Progresion/, Assets/_Proyecto/Tests/PlayMode/Personaje/,
Assets/_Proyecto/Tests/PlayMode/Progresion/, docs/capturas/
No tocar: Core/, todo lo de Alan, escenas existentes (Pruebas_Tony, Pruebas_APie, Pruebas_Calle), `JugadorAPie.prefab` (el cuerpo no se
anida en el jugador en esta tarea).

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa
- [ ] EditMode: `ReglaApariencia` (negativos a 0); `ServicioPerfil` guarda y carga la apariencia; un JSON viejo `{"nombre":"A","alias":"B"}`
      carga nombre y alias con apariencia `Predeterminada`; `FijarApariencia` igual no avisa `Cambio`
- [ ] PlayMode `CuerpoJugadorPruebas`: con 4×4×4 apariencias el cuerpo cambia de color de piel/ropa y muestra o esconde la pieza del
      implante según el catálogo; un índice 7 se toma como 7 % 4 sin errores; sin apariencia dada, sigue al perfil registrado y se repinta
      al llamar `FijarApariencia`
- [ ] PlayMode `VistaPreviaAparienciaPruebas`: encendida, su cámara dibuja en la RenderTexture; apagada, no queda ninguna cámara suya
      activa; nunca hay más de un `AudioListener` activo
- [ ] Captura docs/capturas/T-123-1.png: cuatro cuerpos lado a lado con apariencias distintas (se distinguen cara, ropa e implante)
- [ ] Consola sin errores en las pruebas

## Criterios de sensación (para un humano)
- Abrir T-123-1.png o colocar CuerpoJugador.prefab en una escena vacía: ¿las 4 caras, 4 ropas y 3 implantes se distinguen de un
  vistazo y se ven low-poly cyberpunk y no como error? ¿La persona se ve de 1.75 m junto al mecha de 4 m?

## Pruebas automáticas
- EditMode: ReglaAparienciaPruebas, casos nuevos de ServicioPerfil. PlayMode: CuerpoJugadorPruebas, VistaPreviaAparienciaPruebas.

## Fuera de alcance
- La pantalla de creación (T-126) y el espejo (T-130); ver el propio cuerpo en primera persona; modelos finales de Tripo; que un
  implante instalado cambie la apariencia (T-F7)
