# Reporte sesión 1 — 8 de noviembre (carpeta mecha-s1)

Tony pidió parar después de A6. Todo se integró en `tony/s1-08nov` y quedó en main con push (último merge ebc1546).
Build de prueba con main: Builds/Demo/CyberCity.exe de mecha-s1, generado el 8-nov a las 01:39.
Detalle de cada tarea en docs/tareas/hechas/T-xxx-*.md. Capturas en docs/capturas/s1d-T-xxx-*.png y pj01-*.png.

## Hecho

| Tarea | Qué quedó |
| --- | --- |
| T-723 | Cámara con Cinemachine 3.1.7. A pie va detrás del hombro, con el personaje centrado al 42 % del alto (como CAM01). El zoom solo cambia al apuntar. El Deoccluder evita que atraviese paredes, y en el cuarto ya se ve al personaje. En carro y moto la cámara solo se subió (no se pasó a Cinemachine). |
| T-724 | Caminar y correr son exactamente Loco_Caminar y Loco_Correr (md5 igual al zip). Se quitó lo que había encima: el clip iba acelerado ×2.2 y tenía IK de pies. |
| T-725 | HUD con los gráficos H20-H26. Las barras ya no llevan las palabras VIDA ni ESCUDO: emblema, escudo delgado y vida con número. También: marco 9-slice del minimapa, casillas, íconos, estrellas y muñequito por partes. Con DOTween, la parte perdida de la barra espera y baja suave. |
| T-726 | En las cinemáticas se oculta el HUD y solo queda "ENTER: SALTAR", con la tecla real. Opciones > Controles muestra las teclas reales. |
| T-727 | Pruebas rojas de UI arregladas: minimapa y título de zona eran pruebas viejas. Se retiró PrologoCiudadPruebas (obsoleta) y se quitó la dependencia del orden en Historia, Teléfono, HUD y Marcador. |
| T-728 | El asalto es a una persona de afuera y no te quitan tu dinero. Objetivos: "Róbate un carro", luego "Persíguelos" y "Atrapa al asaltante". El carro huye a 14 m/s. Si lo atrapas, la víctima te da 150. Ningún vehículo aparece encima de otro (EspacioLibre en tráfico, refuerzos, asalto y patrullas). |
| T-729 | Policías a 1.80 m. La policía ataca primero al criminal armado. Dejas de ser buscado a los 45 s sin que te vean, y las estrellas parpadean solo los últimos 8 s. Se apagó el silbido (ARM_BALA_ROZA). |
| T-731 (A1) | Se importaron los 22 clips de Tony: 4 reemplazados con su .meta y 18 nuevos, todos Humanoid y con su loop. |
| T-732 (A2) | PJ01 es el modelo del jugador: 1.80 m medido animado, material URP, espejo y cámara. |
| T-733 (A3) | Animator con los clips de Tony. La pistola solo se ve al apuntar (Sacar y Guardar). Al apuntar usa un blend 2D (lateral y atrás) y no corre. Agachado con sus clips, sin disparar. Rodar con Loco_Rodar (2.2 m en 1 s). Se quitaron la pose procedural y la rodada simulada. Entró el Loco_Caminar ajustado de Tony (0.997 m/s). |
| T-734 (A4) | Eventos: Pistola_Sacar activa la pistola a los 0.30 s y Pistola_Guardar la desactiva a los 0.53 s. Cubre los casos de soltar antes, disparar al instante y volver a apuntar. |
| T-735 (A5) | Capa de daño aditiva (Frente, Espalda y Fuerte, este último con 25 % de la vida o más): el jugador sigue apuntando y disparando mientras se estremece. |
| T-736 (A6) | Pistola W01_Pistola_agarre en R_Hand (caso A: lossyScale 1). Mide 21 cm y apunta al frente. Las armas largas conservan su agarre. Entró el Loco_Correr ajustado de Tony (2.51 m/s). |

Pruebas al cierre: compilación OK en main. EditMode 2111/2112 (la roja es CarrosS2nPruebas.Importador_SinZip, de la sesión 2).
PlayMode se corrió por clase y con filtros, no completo: memoria justa, y una corrida se cortó.

## Falta

- **A7** (verificación de PJ01): capturas de todo el plan juntas y RecorridoDemoPruebas en verde. Las capturas de A2-A6 ya están; falta la pasada final.
- **A8 y A9**: no encontré su definición. docs/tareas-pj01-animaciones.md llega hasta A7 y no aparecen en ningún otro doc. Hay que decir qué son.
- Del playtest 07-nov (sesión 1):
  - T-730, dron derribable: el código está en el stash `t729-wip` de mecha-s1 y no está integrado.
  - Patrullas con el mismo giro que el carro del jugador: el intento rompió 10 pruebas y se revirtió.
  - Cuerpos de enemigos y policías que se quedan tirados, y su botín.
  - Barriles que explotan.
  - Trazo de bala que termina donde pega.
  - Explosiones y humo con el Particle Pack en URP (el paquete ya está en main).
  - Mapa grande con las calles reales: captura cenital al estilo M10.
  - T-721: nombres encimados en el mapa.
- RecorridoDemoPruebas falla ("El bot venció a los SKULLS y al jefe sin que la prueba los matara"). Ya fallaba antes de A2. Builds/Juego no se hizo.
- Pruebas rojas conocidas:
  - PoliciaS1Pruebas mide al policía en 2.7 m: hay que revisar si T-729 lo agrandó de más.
  - PatrullaFisicaT707.
  - HeadshotPandilleroT677.
  - ArmasPruebas.ComprarYUsarEscopeta.
  - BarrioAlanPruebas.
  - CarroAlanPruebas.
  - SonidoArmasT593.
  - RecorridoRonda2 y RecorridoAPie (Bar).

## Para revisar o decidir

- **Pies que resbalan:** con los clips a velocidad 1, el cuerpo avanza a 4.5 m/s al caminar y 7.5 m/s al correr, más rápido que lo que avanzan los clips (1.0 y 2.5 m/s). Opciones: dejarlo así, bajar la velocidad del cuerpo o usar clips más rápidos.
- **Agarre de la pistola:** visto de lado, los dedos parecen rodear la corredera por arriba en vez de la empuñadura (pj01-agarre-pistola-apuntando-lado.png). La mano izquierda queda cerca pero no agarra (AgarreIzq se afinó para la pistola vieja).
- **Silbato:** no hay un silbato de policía. Se apagó el silbido de la bala que roza; si era otro sonido, hay que decir cuál.
- **Clips de agachado:** Loco_AgachadoIdle y Loco_AgachadoCaminar salen de Blender con otra escala de esqueleto. Usan el avatar de Loco_Pararse (`ImportadorMixamo.UsarAvatarDeLocoPararse`); si se vuelven a exportar, hay que correrlo otra vez.
- **Ciudad_Prologo.unity:** cambió una línea en A2 (modelo inicial PJ01). Avisar a la sesión 2.
- **Aliados y enemigos con pistola:** comparten el ControladorBase y también juegan Pistola_Sacar (0.87 s) antes de apuntar.
- **Stashes en mecha-s1:** "t729-wip" (dron) y "restos de corridas batch 07-nov", más los viejos.
