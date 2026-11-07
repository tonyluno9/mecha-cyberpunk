# Reporte sesión 1 — 6 y 7 de noviembre (carpeta mecha-s1)

Tony pidió parar después de T-696. Todo lo de abajo está en main con push (último commit de tareas: 54c2cd2).
Detalle de cada tarea en docs/tareas/hechas/T-xxx-*.md y capturas en docs/capturas/s1n-* y s1t-*.

## Hecho

| Tarea | Qué quedó |
| --- | --- |
| Menú | Pantalla de inicio nueva (escena Inicio.unity, ciudad 3D de noche, logo con glitch, opciones en pestañas, créditos, carga asíncrona con consejos). Merge aef6081. |
| T-670 | Jugador y NPC miden 1.80 m animados (medido con BakeMesh). Corrida ColocarS2.Escala. |
| T-671 | Con arma y sin apuntar: camina y corre normal, el arma cuelga de la mano. Se quitó la capa Piernas de T-651. |
| T-672 / T-682 | Al apuntar: mismo encuadre que al caminar, centrado y solo con zoom (FOV 40). La subida de T-672 se deshizo. |
| T-673 / T-674 / T-683 | Ctrl agacha (alterna), Shift agachado más rápido, Espacio agachado rueda; de pie no se rueda. Agachado con animación real (Cobertura_Entrar y Rifle_AgachadoCaminar) y capa de brazos sin pose de arma. Rodada más lenta, sin hundirse. |
| T-675 | Golpes sin spam; la reacción al daño es corta y se corta con golpe o rodada. |
| T-676 | Destello chico de 0.04 s (ya con F01), un sonido fijo por arma, sin sonidos de impacto, trazo finísimo y sin disparar sin línea de vista. |
| T-677 | Headshot ×3 (hueso de la cabeza o 15 % de arriba del collider). |
| T-678 | Importador ImportarS1n (menú S1n > Meter assets nuevos), documentado en docs/meter-assets-06nov.md. |
| T-679 | Escopeta, fusil y francotirador con modelos W02-W04: en la mano, en el piso, en el HUD y en los datos del banco de Tino. |
| T-680 | Policía C01 (sí trae esqueleto): prefab IA/Prefabs/PoliciaAPie. |
| T-681 | Al apuntar, el cañón de las 4 armas va a menos de 3° del punto de mira (antes 13° y volteada). |
| T-684 | Escudo de energía: 30 puntos, se recarga, barra en el HUD, contrato IEscudo para que el Doc lo mejore. |
| T-685 | Expendedoras (casa, tianguis, Nova) con 5 consumibles: bebida energética, papitas, venda, café y batería de escudo. Se eligen con Tab y se usan con F o con las teclas 6 a 0. |
| T-686 | Los enemigos sueltan su dinero (se recoge al pasar) y su arma (se recoge con E). |
| T-687 | Disparar desde el carro y la moto con clic derecho (apuntado libre). |
| T-688 | Pruebas rojas investigadas y arregladas las nuestras (tabla en T-688-pruebas-rojas.md). |
| T-689 | Pantalla HAS MUERTO con estilo del juego y luego la clínica. |
| T-690 | Arresto: pantalla ARRESTADO, multa del 25 % del dinero, apareces en casa y se limpia la búsqueda. |
| T-691 | El fuego se apaga a los 4 s, al rodar o al alejarte 5 m. Daño "misterioso" de Nova: no se reprodujo en 30 s medidos; quedó una prueba de guardia. |
| T-692 | Nada sale volando: el empujón de la explosión ahora es de 12 m/s y antes era de cientos. La explosión daña alrededor y la patrulla recibe daño y explota. |
| T-693 | Capitán Nova (jefe): telegrafía, golpe de área, recuperación castigable y carga cada 3 golpes. Se esquiva rodando. |
| T-694 | Conductor más alto en el asiento: la cabeza a media ventana en 10 carros, patrulla incluida. |
| T-695 | Moto: el conductor va quieto en el asiento, aceleración calibrada (reparto más lenta), frena en 0.5 s, humo al centro. |
| T-696 | Carros: motor 6200 a 8800 N y freno 12500 a 20000 N. Choque con tope de 15 (menos que la pistola, 25). En moto a 11 m/s o más te avienta. Sin luces ni sonido de carro robado. El aviso de fuego usa chispas. |
| T-697 | Las 3 rojas viejas de Vehiculos arregladas (eran de las pruebas). |

## Falta (de la sesión 1 de playtest-06nov.md y playtest-06nov-b.md)

- Vehículos: los carros del tráfico que chocan se quedan detenidos y el conductor reacciona; carros que aparecen o desaparecen
  a la vista; carros flotando; robar un carro con conductor (sale, cae, se asusta, es testigo y llama con Inter_Telefono); el aliado
  se sube y se baja por la puerta con animación; algunos carros del tráfico van muy lentos.
- Policía: patrullas con la misma física de carro y conductor IA, sólidas, robables; robar una patrulla te busca siempre (GPS);
  llegan manejando con tiempo real; los policías (ya existe PoliciaAPie con C01) se bajan, se cubren con la patrulla y disparan;
  el dron dispara de verdad con trazos y sonido de bala rozando; dispararle a la policía tiene efecto.
- Enemigos: comprobar en la ciudad que las balas enemigas chocan con todo (T-676 puso línea de vista); SKULLS en cobertura
  agachados (asoman, disparan, se esconden); bug de los SKULLS (solo aparecen 3, reaparecen, la misión se traba); los SKULLS
  llegan en carro.
- Robots guardia de Nova: T-693 hizo pelear al Capitán Nova. Si "el robot de Nova debe hacer daño" era por los GuardiaNova,
  falta.
- Misión: objetivo siempre claro en el HUD y el mapa; cinemática de la historia del dinero al salir de casa (asalto, persecución,
  llanta ponchada, recuperar el dinero); mercado de armas con escopeta, fusil, francotirador y balas; revisar el tutorial de punta a punta.
- Mapa: separar los dos nombres encimados junto al ícono del mecánico.
- Final: RecorridoDemoPruebas con el tutorial completo y Builds/Juego.



## Para revisar o decidir

- Pruebas en rojo que no son de estas tareas:
  - CarrosS2nPruebas.Importador_SinZip: V09 ya está en el repo (sesión 2).
  - RecorridoRonda2Pruebas.MercadoTianguisEscopeta: el ambiente arranca en 0.8 (Mundo).
  - RecorridoAPiePruebas (entrada del Bar): ya fallaba al empezar la noche.
  - HistoriaDemoPruebas.RecorridoDocInventorHacker y PersonajesApoyoPruebas.RecorridoProfes: esperan la frase vieja del Doc,
    "Aguanta, mijo", y la sesión 2 la cambió. Hay que actualizar las pruebas al diálogo nuevo.
- LanzamientoMotoT696Pruebas: no tiene una corrida limpia (la corrida grande se cortó por el lockfile). Correrla sola.
- No se corrió el PlayMode completo (la memoria está justa con dos Unity); se corrieron las clases de cada tarea.
- La moto no se encogió el 20 %: en el 06-nov-b dijiste que te gusta cómo te ves en ella.
- Para arte: animaciones Crouch Idle, Crouched Walking y Forward Roll (docs/animaciones-faltan.md); cristales de los carros
  opacos en la textura (no se ve al conductor desde afuera); fajo de dinero provisional; íconos de los consumibles.
- Lo ideal para el arresto es un contrato en Core (hoy Trafico avisa con SendMessage). Anotado en decisiones-pendientes-tony.md.
- Quedó un stash viejo de T-684 (stash@{0}) en mecha-s1 y ProjectSettings/SceneTemplateSettings.json sin versionar.
