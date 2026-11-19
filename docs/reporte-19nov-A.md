# Reporte 19-nov, sesión A: look de las zonas nuevas contra las referencias

Trabajo de 08:28 a 10:10 (hora de México). Opus dirigió e integró en `integ/look-19nov-A`. Cuatro subagentes Sonnet hicieron una
tarea cada uno en su rama: T-1461 (look), T-1462 (aéreas), T-1463 (Malecón) y T-1464 (Autopista). Se numeraron desde 1461 porque la
sesión B ya usaba T-1440 a T-1443.
Hubo 5 rondas de fotos y correcciones y una de arreglos finales. Cada ronda se corrió con un solo Unity en modo batch.
El paso a paso está en docs/progreso-19nov-A.md.

## Las 9 comparaciones finales (referencia a la izquierda, juego a la derecha)
Están en `docs/capturas/_vista19b/` y salen de `Vista19b.Fotos`, y la copia de cada ronda está en `Logs/vista19b-r0` … `r5` y `final`
(r0 es como estaba al empezar).

| Toma | Archivo | Cómo quedó | Qué no quedó |
| --- | --- | --- | --- |
| K11 Malecón | malecon-k11.png | Gente caminando y recargada en el barandal, en pose natural. Carros con luces en la avenida y fachadas con neón a la derecha. Fila de palmeras y faroles, cables y skyline con torres y neón sobre el mar. Tono azul-gris con bruma. | Sigue más frío y azul que la referencia: el paseo casi no se baña de naranja y los charcos reflejan poco. La avenida no curva en cuadro, porque la curva real es muy suave. No se ve la playa. |
| K12 muelle | malecon-k12.png | Paseo con faroles, palmeras y gente, y la avenida con carros y fachadas de neón. | Sigue la forma blanca sobre el mar al centro-izquierda; no se identificó del todo (no es el faro de T-1413 ni sus haces, que ya se atenuaron). Una cruz blanca en el cielo es el dron NV01. |
| A11 Malecón aérea | malecon-a11.png | Mar a la izquierda con el faro chico y su haz. Paseo con palmeras y faroles encendidos, la avenida con carros y fachadas de neón a la derecha. Ya no flotan bloques ni paneles. | La avenida casi no curva, la playa se lee como una franja lisa (falta textura de arena) y la cámara queda más lejos que en la referencia. |
| K13 Cerro | cerro-k13.png | Tiene el look nuevo: sodio, niebla y charcos con reflejo de neón en la calle. | La composición es de la sesión B. |
| A13 Cerro aérea | cerro-a13.png | La niebla baja con la altura y se lee la zona. Ya no está la rejilla. | Siguen arriba unas torres y paneles de vidrio que parecen flotar (unos del skyline este, otros sin identificar). Las casas del Cerro se ven como cubos de juguete (sesión B). |
| K15 Puerto | puerto-k15.png | La más parecida en tono: sodio, neón turquesa y magenta, bruma, grafiti en los contenedores y charcos. | La composición es de la sesión B. |
| A15 Puerto aérea | puerto-a15.png | Niebla baja; se ven grúas con balizas, barco, contenedores y faroles. | Los halos de los faroles se ven como discos naranjas desde arriba. Composición de B. |
| K18 Autopista | autopista-k18.png | Bajo el tablero: chatarra AU03, carros deshechos, tambos, charcos y faroles de sodio arriba. | No tiene la composición de la referencia: falta la columna gruesa en primer plano al centro, la rampa curva a la izquierda y el túnel iluminado a la derecha. Desde la calle sur el túnel queda a la izquierda de cualquier columna, y la zona norte no tiene piso. Necesita rehacer la geometría (ver Pendientes). |
| A18 Autopista aérea | autopista-a18.png | Tablero en curva con faroles y estelas de tráfico. El ramal curvo entra a dos bocas de túnel iluminadas en el acantilado a la derecha, con el barrio y la calle con neón abajo a la izquierda. | Una torre alta de ventanas queda al centro (no es la torre de reflectores; no es de la Autopista). Las columnas del tablero casi no se ven y falta el barrio denso con chatarra bajo la curva. |

## Qué se hizo, por punto del pedido

### 1. Luz y piso (T-1461, `LookNocheT1461`, prefab nuevo `T1461_Look`)
- **Piso mojado:**
  - Asfalto de las zonas con smoothness 0.78 (`M_T07_AsfaltoMojado` en el Malecón) y banqueta con 0.5-0.6.
  - Charcos irregulares de 2 a 9 m con el reflejo plano de `ReflejoCharcos`, más densos junto a faroles y neones, y hasta 60 bajo el tablero de K18.
  - 48 sondas de reflexión con box projection, 12 por zona y cada 60 m, horneadas a las 23 h. Pesan 11 MB.
- **Faroles de sodio:** intensidad 240, alcance 27 m (18 m en el Cerro) y color (1, 0.5, 0.18). Llevan halo suave en la niebla, núcleo para el bloom y un plano naranja en el piso.
- **Neón:** 30 luces de neón nuevas y 220 derrames de color en pared y piso.
- **Posproceso y niebla:**
  - Contraste 14, sombras azul-gris (sin negros puros), altas luces cálidas, balance de blancos +10 y bloom con umbral 1.2.
  - Niebla de densidad 0.009 y color azul-gris (0.30, 0.40, 0.50).
- **Luces en la escena:** 1361 (eran 1305; el tope seguro es 1400).
- **Avisos:**
  - La niebla vive en `ConfigCicloDia_Mundo`, que también usan los barrios viejos, así que su color y densidad base les cambian un poco.
  - Para el Cerro y el Puerto, los cambios de luces y piso van como overrides de su instancia en la escena. Sus prefabs no se tocaron.
  - En la ronda 1, los pasos viejos `LookNocheT1392/T1399.Aplicar` regeneraban los charcos dentro de los prefabs de la sesión B. Se restauraron y esos pasos no se volvieron a correr.
  - En la ronda 2, `PisoReal` tocó materiales de carros, de T535 y `Cerro_Terreno`. Se restauraron y se acotó a materiales de zona.

### 2. Vista desde arriba (T-1462, `AereasT1462` y componentes nuevos en Mundo/Reloj)
- **`NieblaPorAltura`:** la niebla baja con la altura de la cámara. A nivel de calle es igual que antes. A más de 12 m pasa a niebla lineal, con los primeros 150-200 m claros y el horizonte cubierto poco a poco. La aplican `CicloDiaNoche` (en el juego) y `ColocarS2cLook.Foto` (en las fotos).
- **`OcultarPorAltura`:** apaga fondos que se ven rotos desde arriba y deja todo igual a nivel de calle. Apaga estos grupos:
  - los paneles pintados `T513_Fondo` y la bruma de altura (que hacía la mancha blanca de A18);
  - el skyline SK01 (el este a más de 25 m y el oeste a más de 100 m);
  - las torres de vidrio de fondo de la Autopista;
  - los cubos de las afueras T530.
- **Lo que se quitó o se cambió:**
  - Tramo_3 (el panel oscuro sobre el mar de K11) queda apagado.
  - Bajo el mapa hay un suelo lejano mate (`SueloLejano`) que solo existe por encima de 25 m.
  - Se probó una "ciudad de respaldo" con ventanas, se veía como una rejilla y se quitó.
- **Diagnóstico:** `Diagnosticar` y `Validar` miden por píxel qué renderer se ve en cada región de las aéreas.

### 3. Malecón (T-1463, `ZonaMaleconT1463`)
- **Cámaras:** `Vista19b_K11` y `Vista19b_A11`.
- **Paseo:**
  - Fila de faroles ML01 cada 15 m, con palmera AR03 en medio, 26 faroles con spot de sodio y un charco de luz naranja.
  - 4 bancas PZ01 y basura, huacales, tambos y botes.
  - Se saltan las bocas de las uniones y lo que se encima.
- **Faro:** faro A11 nuevo con su haz sobre un islote de escollera, a la izquierda de A11. El faro lejano de T-1413 se asentó en sus rocas y se atenuó.
- **Gente:**
  - 28 personas fijas NP07–NP14 en paseo y banqueta (`PoseQuieta` en el juego).
  - En el editor se muestrea el clip para que no salgan en pose T.
  - `PoblarFoto` pone carros con luces en los carriles y gente caminando para la foto, donde los ponen los sistemas del juego (tráfico zona "Malecon" y `GrupoPeatones`).
- **Playa:** material `T1463_Arena`.

### 4. Autopista (T-1464, `ZonaAutopistaT1464`)
- **Ramal curvo de T-1415:** abierto (sin reja ni letreros de OBRA) y con tiras emisivas.
- **Boca principal:** marcos emisivos y dos luces.
- **Tablero:** faroles AU01 cada ~20 m y 52 cintas de estela roja y blanca.
- **Bajo el tablero:** columnas de la rampa engrosadas a ~5 m, chatarra, carros deshechos, tambos (con collider y `BarrilExplosivo`), huacales, basura y charcos.
- **Torre de reflectores:** movida a (-300, 0, 110).
- **Cámaras y foto:** `Vista19b_K18` y `Vista19b_A18`. `PoblarFoto` pone carros en el tablero y peatones en la calle baja.

### 5. Vida
Malecón y Autopista ya tienen gente, carros estacionados y en tráfico (en la foto y en el juego), basura, cajas y bancas.
- **Peatones:** en el juego salen de `GrupoPeatones` (T-1409) por cada `CalleZona` con banquetas. `ValidarJuego` lo comprueba por datos, pero no se probó en PlayMode.
- **Ojo:** la zona Autopista solo trae NP13 y NP14.

## Pruebas
- **EditMode de Mundo** (incluye las zonas, el look, los encimados de T-995, las uniones y el greybox): **755/755**.
  - **Primera corrida (747/755):** 4 pruebas guardaban los valores viejos de sodio y niebla y se actualizaron a los de T-1461. Las otras 4 eran bugs reales que se corrigieron:
    - tambos sin `BarrilExplosivo`;
    - faroles encimados en barreras;
    - palmeras en la boca U1;
    - playa sin textura.
  - **Encimados contra objetos fuera del prefab:** los quitó `ArreglosSesionA19nov.QuitarEncimadosNuevos`, que mira la escena completa. Quitó 3 piezas de la fila y 5 rocas del islote del faro A11, que se encimaban entre sí.
- **Pruebas nuevas:** `NieblaPorAlturaPruebas` y `PoseQuietaPruebas` pasaron en la primera corrida (10/10) y después se agregaron más.
- **No se corrió:** PlayMode ni el EditMode completo de otras áreas, por la memoria.

## Builds
- `Builds/Demo/CyberCity.exe` (`ConstruirDemoPandillas`): Build Finished, Result: Success, 0 errores (09:59).
- `Builds/Juego/CyberCity.exe` (`ConstruirJuego`): Build Finished, Result: Success, 0 errores (10:03).
- No se midieron los FPS de este cambio. Se agregaron 56 luces, 48 sondas y muchos planos aditivos, así que conviene correr `-medirFpsZonas`.

## Tamaños
- **Escena:** `Ciudad_Prologo.unity` pesa 56 MB (el tope es 100).
- **Prefabs:**
  - T1354_Malecon: 12.5 MB.
  - T1356_Autopista: 15.5 MB.
  - T1461_Look: 1 MB.
- **Sondas:** 11.7 MB.

## Pendientes
- **K18:** hay que construir la composición: un tablero curvo hacia un túnel que quede a la derecha de una columna vista desde la calle. Puede ser con una pieza de tablero en curva (está en docs/assets-faltan.md) o moviendo la cámara al norte del ramal con piso nuevo.
- **K11 y K12:** más naranja cálido en el paseo y reflejos más fuertes en el asfalto. Los núcleos y el baño de sodio de T-1461 casi no se notan, y no se encontró la causa.
- **Lo que no se identificó:** la forma blanca de K12, la torre alta al centro de A18 y los paneles de vidrio de A13.
- **Faroles del Malecón:** el espaciado a veces sale de 15 ± 2.5 m, por los huecos de las bocas de uniones y los encimados (Validar de T-1463 lo marca).
- **Para la sesión B** (anotado en docs/tareas/en-curso/T-1462-aereas.md): `T1355_Cerro/Terreno/Ladera` se ve como pared lisa desde arriba, y el patio y el mar del Puerto son losas grandes. Lo que B pidió en docs/para-sesion-A-19nov.md ya está hecho: niebla baja en las aéreas, más sodio cálido y charcos con reflejo en el Puerto (por override de su instancia).
- **Assets que faltan:** sección nueva en docs/assets-faltan.md (arena, clips de NP, NP01–NP06, tablero curvo, shader de estela y columna AU04 gruesa).
