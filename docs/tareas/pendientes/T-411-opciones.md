# T-411 Opciones: sensibilidad, invertir Y, volúmenes, calidad, resolución y pantalla completa
Dueño: Tony | Rama: tony/T-411-opciones | Modelo: Sonnet 5.5, medium | MODO RÁPIDO (docs/ronda3.md)
Depende de: T-407 (volumen por sección), T-408 (kit) y T-410 (se abre desde la pausa).

## Objetivo
Una pantalla de OPCIONES donde ajustas la sensibilidad del ratón, invertir Y, el volumen general y por sección, la calidad, la resolución
y la pantalla completa. Cada cambio se aplica al instante y se guarda, aparte de las partidas.

## Contexto
- Guía de estilo: docs/diseno/ui-cyberfix.md. Kit: `UI/Kit/` (deslizador, conmutador y selector de T-408).
- Sensibilidad actual: `Personaje/Datos/ConfigControlesAPie.cs` (`SensibilidadRaton`), aplicada en `Personaje/Movimiento/ControladorAPie.cs`
  y `MiradaAPie.cs`; en el carro, `Vehiculos/Carro/CamaraCarro.cs` (`config.Sensibilidad`).
- Volumen: `ISonidos.FijarVolumen(SeccionSonido, float)` (T-407): General, Efectos, Voces, Ambiente, Musica, Interfaz.
- Calidad: ProjectSettings solo tiene los niveles "Mobile" y "PC", con URP 17.3.
- Pausa: `UI/Pausa/PantallaPausa.cs` (T-410) con el botón OPCIONES desactivado.
- Decisión: D-tony-243. **Cambio de Core autorizado por Tony**: `Core/Opciones/IOpciones.cs` (lo leen Personaje y Vehiculos).

## Carpetas permitidas
Core/Opciones/ (nuevo), Progresion/Opciones/ (nuevo; su asmdef puede referenciar el runtime de URP), Personaje/Movimiento/, Vehiculos/Carro/,
UI/ (Opciones/ nuevo, Pausa/, Kit/, Editor/, Datos/), Progresion/Prologo/Editor/CreadorCiudadPandillas.cs, Escenas/Ciudad_Prologo.unity (solo con código),
Tests/EditMode/Progresion/, Tests/PlayMode/UI/, docs de Tony. No tocar: Radio/, Mundo/, ProjectSettings/.

## Qué hacer
1. **Contrato** `IOpciones`: `float Sensibilidad` (multiplicador de 0.25 a 3), `bool InvertirY` y `event Action Cambio`, en
   `Registro<IOpciones>`. ControladorAPie y CamaraCarro multiplican su sensibilidad y voltean Y si hace falta.
2. **`Progresion/Opciones/`**: `ReglaOpciones` (pura: valores por defecto, límites y JSON de ida y vuelta) y `ServicioOpciones` (IOpciones).
   Lee y escribe `opciones.json` en `persistentDataPath` (con `FijarRuta` para pruebas) en cada cambio. Aplica: volumen general y por
   sección por `Sonidos`; calidad BAJA, MEDIA o ALTA sobre una **copia en memoria** del asset de URP (renderScale 0.75/0.9/1, distancia de
   sombras 40/70/120 m, MSAA 1/2/4; sin tocar ProjectSettings ni el asset); resolución (las de `Screen.resolutions` sin repetir ancho x alto)
   y pantalla completa (FullScreenWindow o Windowed) con `Screen.SetResolution`. Por defecto: sensibilidad 1, Y normal, volúmenes al 80 %,
   ALTA, resolución nativa y pantalla completa. Un archivo roto se ignora (se usan los valores por defecto) y deja un solo warning.
3. **`UI/Opciones/PantallaOpciones.cs`** (prefab con el kit; canvas de orden 70): "OPCIONES" y las filas SENSIBILIDAD ("1.00x"),
   INVERTIR Y (SÍ/NO), VOLUMEN GENERAL, EFECTOS, VOCES, AMBIENTE, MÚSICA, INTERFAZ (de 0 a 100 %, de 5 en 5), CALIDAD, RESOLUCIÓN,
   PANTALLA COMPLETA y VOLVER. ← → cambian el valor de la fila elegida, el clic y la rueda también. Al mover un volumen suena UI_MOVER a
   ese volumen. Esc o VOLVER regresan a quien la abrió. Se apila en `PilaPantallas`. El botón OPCIONES de la pausa se activa.
4. `T411()` pone el `ServicioOpciones` y la pantalla bajo `GTA_T411`; así las opciones se aplican desde que arranca la escena.

## Criterios de aceptación
- [ ] Cuatro ← en VOLUMEN GENERAL (del 80 %) dejan el general en 60 % en el gestor al momento; EFECTOS solo cambia los sonidos de efectos.
- [ ] Con INVERTIR Y en SÍ, mover el ratón hacia arriba baja la cámara; con sensibilidad 2x, el mismo movimiento gira el doble.
- [ ] Al cerrar y abrir el servicio con el mismo archivo, todas las opciones siguen igual; con el archivo roto salen las de por defecto.
- [ ] CALIDAD BAJA cambia el renderScale de la copia a 0.75 y el asset de URP del proyecto queda sin cambios (git status limpio).
- [ ] Captura revisada a ojo y corregida si se ve mal: docs/capturas/T-411-1.png (opciones con VOLUMEN GENERAL elegido) en 1920x1080 y 2560x1440.

## Pruebas (solo estas 2 nuevas)
- EditMode `ReglaOpcionesPruebas.LimitesYArchivo`: los valores fuera de rango se recortan, el JSON va y vuelve igual y el texto roto da los
  valores por defecto.
- PlayMode `OpcionesPruebas.RecorridoOpciones` (InputTestFixture, ruta temporal): Ciudad_Prologo; Esc → pausa → ↓ hasta OPCIONES → Enter;
  ← cuatro veces en VOLUMEN GENERAL; ↓ hasta INVERTIR Y y Enter; Esc vuelve a la pausa y Esc continúa; el ratón hacia arriba baja la
  cámara; un `ServicioOpciones` nuevo con el mismo archivo lee los mismos valores. Toma la captura.

## Al terminar
- Builds: `ConstruirDemo.ConstruirJuego` → Builds/Juego/Cyberfix.exe y `ConstruirDemo.ConstruirDemoPandillas` → Builds/Demo/Cyberfix.exe.
- docs/guia-para-jugar.md, sección de pandillas: 3 líneas de qué probar.
- Bitácora de una línea, fila de docs/estado-tony.md en "hecha", merge a tony/fase-g y a main con push (autorizado en docs/ronda3.md).
  Nunca force push; `git restore` a lo que Unity cambia solo.

## Sensación (Tony)
- ¿La sensibilidad por defecto está bien? ¿BAJA se nota más fluida en una compu lenta? ¿Te falta alguna opción (FOV, subtítulos)?

## Fuera de alcance
- Cambiar las teclas, el FOV, los subtítulos y el idioma.
