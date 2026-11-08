# Reporte sesión 1 — noche del 9 de noviembre (carpeta mecha-s1)

Todo se integró en `tony/s1-09nov` y quedó en main con push. Último merge: 23c4770.

Builds hechos con main 23c4770 el 8-nov, hora del sistema:
- **Demo:** `C:\Users\Angel Perez\Desktop\videojuego\mecha-s1\Builds\Demo\CyberCity.exe`, a las 12:35. El .exe conserva su fecha vieja; lo nuevo está en CyberCity_Data.
- **Juego:** `C:\Users\Angel Perez\Desktop\videojuego\mecha-s1\Builds\Juego\CyberCity.exe`, a las 12:36. Build Success, 0 errores.

Detalle de cada tarea en docs/tareas/hechas/T-xxx-*.md. Capturas en docs/capturas/s1e-T-xxx-*.png y pj01-*.png.

## Hecho

| Tarea | Qué quedó |
| --- | --- |
| T-737 (urgente) | PJ01 se rompía al apuntar por la IK vieja (ApuntadoIK), que escribía huesos encima del clip. Ahora el jugador usa los clips puros (`ClipPuro`). Lo que el retarget no logra igualar quedó en **docs/pedido-blender-09nov.md**. |
| T-738 (A6) | Agarre de la pistola: `AgarrePistolaPJ01` gira solo la pistola hacia el punto de mira, con un error menor a 1.3°, y aplica un retroceso de 4° en el disparo. |
| T-739 (A8) | Caminar y correr con y sin implante: se importaron los 4 clips. El parámetro `ConImplante` sale del nivel de Piernas o Brazos. Sin implante, el cuerpo va a 1.29 m/s caminando y 3.19 m/s corriendo. |
| T-740 (A9) | Implantes visibles con PJ01_Implantes.fbx: 6 piezas IMP01, IMP02 e IMP03 en nivel 1 y 2, sobre los huesos de PJ01. |
| T-741 (A7) | Verificación de PJ01: capturas de todo el plan. |
| T-742 (B1) | Carros V01a, V01b y V05 armados por piezas, incluida la patrulla V05. Llevan carrocería, puertas con bisagra y 4 llantas RU01 que giran; las delanteras doblan. La puerta del lado por donde subes abre y cierra junto con Carro_Entrar y Carro_Salir. Física y choques iguales que antes. |
| T-743 (B2) | Motos V07, V08 y C19 con la llanta RU02, y C19 con su cuerpo C19CUERPO. Las llantas giran y la delantera dobla. V07 y V08 siguen con su cuerpo de siempre, al que se le borraron las llantas horneadas. |
| T-744 (B6) | Los aliados que te siguen son AL10 (gorra) y AL11 (trenzas), en Humanoid, alternándose. Miden 1.79 m animados, siguen, pelean y suben y bajan del carro. |
| T-745 | Dron derribable: cae, explota chico y baja la búsqueda. Barriles que explotan: dañan en área, empujan, se prenden y explotan en cadena. El trazo de bala termina donde pega o se desvanece. Explosiones, humo y fuego con el Particle Pack en URP, sin rosa. |
| T-746 | Enemigos y policías muertos se quedan tirados 45 s (tope de 12 cuerpos). Sueltan dinero siempre, balas a veces y la pistola si la traían. La patrulla ya no gira sobre su eje. El policía mide 1.80 m: la prueba medía mal (2.7 m). |
| T-747 | Mapa grande (M) y minimapa con las calles reales, estilo M10 (color por barrio, contornos), sacados de una captura cenital de la ciudad. T-721: los nombres ya no se enciman. Los "bugs de la tienda del CIC" no lo eran: desde T-814 la venta es en el banco de Tino, que abre con E, cobra con Enter y cierra con Esc. |
| T-748 | RecorridoDemoPruebas en verde: 0 bugs en 240 s. Era la prueba: al recolocar al jugador junto al banco lo dejaba en el techo del CIC. |

**Pruebas al cierre:**
- Compilación OK en main.
- EditMode 2237/2237 en la última tarea (T-747).
- RecorridoDemoPruebas: DemoDePrincipioAFin pasa, y NavMesh también.
- PlayMode se corrió por clase y con filtros, sin corrida completa, porque la memoria no alcanza.

## Falta

- **Patrullas con la misma física que el carro del jugador:** solo se limitó el giro. Pasarlas a EntradaCarro necesita una tarea de Vehiculos; el intento anterior rompió 10 pruebas.
- **Aliados sin arma en la mano:** al disparar solo se ve el láser. Ya pasaba con el modelo anterior.
- **Aliado al subir al carro:** no abre la puerta (T-706).
- **Hélices del dron:** no giran, porque son parte de la misma malla que el dron.
- **Pruebas rojas conocidas:**
  - Ya estaban antes de esta noche: PatrullaFisicaT707, HeadshotPandilleroT677, BalasCiudadT711, CiudadCarrosPruebas (CarroRobable_16) y CarrosS2nPruebas.Importador_SinZip, que es de la sesión 2.
  - EspacioLibreT728: corrida sola se cuelga sin escribir resultados. Pasa igual sin los cambios de la noche.
  - No se volvieron a correr esta noche: ArmasPruebas.ComprarYUsarEscopeta, BarrioAlan, CarroAlan y SonidoArmasT593.

## Para revisar o decidir

- **Pedido a Blender** (docs/pedido-blender-09nov.md): exportar los clips ya aplicados a PJ01 como Generic, o con T-pose exacta, y apuntar con las dos manos. Con el retarget actual las manos quedan a unos 25 cm.
- **LEEME de animaciones_implantes:** dice que Loco_Caminar y Loco_Correr ya son la versión ConImplante, pero medidos no lo son. Se usan los 4 clips por separado.
- **Llantas pesadas:** RU01 y RU02 tienen unos 19 mil triángulos cada una, con Mesh LOD. Si el tráfico se siente pesado, conviene decimarlas en Blender.
- **Texturas que no cuadran:** las puertas de V01a se ven más grises que la carrocería, por la textura que traen de Tripo. La llanta delantera de V08 queda algo más grande que la trasera.
- **Motos V07 y V08:** cuando lleguen sus cuerpos, se cambia la ruta en `CreadorMotosPiezasT743.Tabla`.
- **Mapa M10:** el estilo sale de las alturas reales, no está pintado. Un artista puede reemplazar `UI/Resources/Mapa/MapaCiudad.png`.
- **Marca de quemado:** se ve como varios puntos oscuros, porque el Particle Pack no trae una textura de quemadura.
- **Barriles y búsqueda:** reventar un barril no sube la búsqueda; reventar un carro sí.
- **Core:**
  - T-745 agregó una excepción en `Tests/EditMode/Core/ReglasEnsambladosPruebas.cs`, para que la prueba PlayMode de Personaje vea Trafico y Vehiculos.
  - Los efectos (`EfectosExplosion`) y el trazo (`TrazadorBala`) quedaron duplicados entre áreas de distinto dueño. Conviene una tarea de Core que los pase a un contrato.
- **Escenas:** `Ciudad_Prologo.unity` cambió una línea en T-744 (`prefabAliadoSegundo`). Hay que avisar a la sesión 2.
- **Banco de Tino en el recorrido:** a veces E no lo abre en 4 s y la prueba recoloca al jugador. Vale la pena revisar si la E falla en el juego real.
- **Botín:** el arma suelta es siempre la pistola de quien la traía, no "a veces", porque así lo piden las pruebas de T-686.
- **Ya avisado antes:** silbato (se apagó ARM_BALA_ROZA), pies que resbalan (bajó con los clips SinImplante) y la línea de Ciudad_Prologo de A2.
- **Stashes en mecha-s1:** "t729-wip" (lo del dron ya se aplicó en T-745 y se puede borrar), "restos de corridas batch 07-nov", ruido de pruebas y los viejos. `ProjectSettings/SceneTemplateSettings.json` sigue sin versionar.
