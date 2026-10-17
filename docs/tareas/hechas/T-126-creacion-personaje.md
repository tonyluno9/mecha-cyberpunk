# T-126 Pantalla de título y creación de personaje
Dueño: Tony | Rama: tony/T-126-creacion-personaje | Modelo: Sonnet 5.5, medium
Depende de: T-123, T-124, T-125

## Objetivo
La demo empieza en `Escenas/Tony/CreacionPersonaje.unity`: si hay partida guardada se elige "CONTINUAR" o "NUEVA PARTIDA"; en la
creación se escriben nombre y alias y se eligen cara, ropa e implante viendo el cuerpo en vivo; "EMPEZAR" empieza una partida nueva en la
etapa `PeleaTutorial` y pasa a la ciudad con un fundido. Es el paso 1 de demo.md.

## Contexto
- Diseño: demo.md (paso 1: "nombre, alias y pocas opciones de apariencia"), personaje-progresion.md (sin origen), estetica-hud.md.
- Piezas: `ServicioPerfil` (`Fijar`, `FijarApariencia`, T-123), `ReglaAlias` (16 caracteres), `CatalogoApariencia`,
  `VistaPreviaApariencia.prefab` (T-123), `ServicioHistoria`, `CambioEscena`, `DatosEscenas`, `ServicioGuardado.RutaForzada` (T-124),
  `PantallasHistoria.prefab` (T-125).
- Decisiones: D-029 (Tony crea las escenas de pantallas sin mundo bajo Escenas/Tony/), D-010 (uGUI Text), D-tony-50 (una ranura).
  Propuestas nuevas: D-tony-64 y D-tony-61.
- El `InputField` de uGUI no recibe texto simulado del Input System en batch: el campo de texto es propio y lee
  `Keyboard.current.onTextInput` (así una prueba escribe con `InputSystem.QueueTextEvent`).

## Qué hacer
1. `UI/Creacion/CampoTextoTeclado.cs`: campo de texto de una línea que, cuando está enfocado, agrega lo que llega por `onTextInput`
   (sin caracteres de control), borra con Backspace y no pasa de `ReglaAlias.LargoMaximo`; muestra un cursor parpadeante.
2. `UI/Creacion/PantallaCreacion.cs`, con dos paneles:
   - Título (solo si `ServicioGuardado` dice que hay partida): "CONTINUAR" y "NUEVA PARTIDA" con ↑ ↓ y Enter. Continuar carga la
     partida en esta escena, y con la etapa cargada (`DatosEscenas.RutaPara`) hace fundido y `CambioEscena.Ir`; si la etapa es `Ninguna`
     o `CreacionPersonaje`, abre la creación. Nueva partida pide confirmar ("Se borrará tu partida · ENTER confirmar · ESC volver").
   - Creación: Nombre, Alias, Cara ◄ ►, Ropa ◄ ►, Implante ◄ ► (con el nombre de la opción del catálogo) y EMPEZAR. ↑ ↓ o Tab cambian de
     fila, ← → cambian la opción, Enter en EMPEZAR confirma. A un lado, la `RawImage` de la vista previa con el cuerpo girando, que cambia
     al instante. Nombre y alias son obligatorios: sin ellos, EMPEZAR muestra "Escribe tu nombre y tu alias" y no avanza.
   - EMPEZAR: borra la partida anterior, fija perfil y apariencia, `Reiniciar` y `Avanzar(PeleaTutorial)` en la historia, fundido a negro
     y `CambioEscena.Ir(datos.RutaPara(PeleaTutorial))` (que guarda antes de cargar).
   Textos y tamaños en `UI/Datos/ConfigPantallaCreacion.cs` (`_Base.asset`). El ratón es opcional: si sumarlo es barato, clic en las
   flechas y en EMPEZAR; si no, se anota.
3. `UI/Editor/CreadorEscenaCreacion.cs` (idempotente) arma `Escenas/Tony/CreacionPersonaje.unity`: cámara con fondo oscuro y un solo
   `AudioListener`, `ServiciosProgresion.prefab` (perfil, historia, guardado), `VistaPreviaApariencia.prefab`, `PantallasHistoria.prefab`
   y la pantalla, que usa `DatosEscenas_Demo`. `MechaCyberpunk.UI.Editor` puede referenciar Personaje y Progresion (mismo dueño) para
   armarla.
4. PlayMode `Tests/PlayMode/UI/CreacionPersonajePruebas.cs` (teclado simulado, `RutaForzada` en carpeta temporal,
   `CambioEscena.Interceptar`):
   a) sin partida: arranca en la creación; escribe "Ana" en Nombre, Tab, "El Rayo" en Alias, → dos veces en Cara, → en Ropa, → tres
      veces en Implante (vuelve a "Ninguno"), baja a EMPEZAR y Enter: perfil "Ana"/"El Rayo", apariencia (2, 1, 0), etapa `PeleaTutorial`,
      la ruta pedida es la de la ciudad de `DatosEscenas_Demo` y el archivo guardado trae `perfil` e `historia`;
   b) con alias vacío, Enter en EMPEZAR no pide escena y muestra el aviso;
   c) Backspace borra y un alias de 20 letras queda en 16;
   d) con una partida guardada en etapa `PeleaCasera`: aparece el título; "CONTINUAR" pide la ruta de la ciudad; con etapa `Flashback`
      pide la del flashback; "NUEVA PARTIDA" + Enter de confirmación abre la creación vacía.

## Carpetas permitidas
Assets/_Proyecto/UI/ (Creacion/ nueva, Datos/, Editor/ y su asmdef), Assets/_Proyecto/Escenas/Tony/CreacionPersonaje.unity (con su creador),
Assets/_Proyecto/Tests/PlayMode/UI/, docs/capturas/
No tocar: Core/, todo lo de Alan, otras escenas, ProjectSettings/ (la escena no se agrega a Build Settings: es T-137).

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa
- [ ] CreacionPersonajePruebas a) a d) pasan
- [ ] La escena tiene un solo `AudioListener` y una sola cámara activa además de la de la vista previa (prueba que la carga)
- [ ] Ninguna prueba escribe la partida real (`RutaForzada` siempre puesta)
- [ ] Capturas docs/capturas/T-126-1.png (creación con el cuerpo en la vista previa y una opción elegida) y T-126-2.png (título con
      CONTINUAR y NUEVA PARTIDA)
- [ ] Consola sin errores

## Criterios de sensación (para un humano)
- Abre CreacionPersonaje.unity y Play: ¿se entiende sin explicación cómo moverse entre campos y cambiar opciones solo con el teclado?
  ¿Ver el cuerpo cambiar al instante se siente bien? ¿Crear el personaje toma menos de 1 minuto y medio?
- (Hasta T-127 la ciudad de pruebas no existe: EMPEZAR puede terminar en un error de escena faltante al jugar a mano; las pruebas usan
  `Interceptar`.)

## Pruebas automáticas
- PlayMode: CreacionPersonajePruebas.

## Fuera de alcance
- La ciudad, la arena y el director (T-127 en adelante); Build Settings (T-137); opciones de audio o video; ver la apariencia en primera
  persona
