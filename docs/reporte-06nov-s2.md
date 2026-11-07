# Reporte sesión 2, 6-nov tarde (docs/playtest-06nov-b.md, SESIÓN 2)

Trabajé de 15:30 a 20:15 en la carpeta principal. Cada tarea va en su rama `tony/T-8xx-*`, con sus pruebas, merge de origin/main,
merge `--no-ff` a main y push. Como `main` está abierto en el worktree de mecha-s1, la integración se hizo en HEAD separado sobre
origin/main con `git push origin HEAD:main`. Usé los IDs T-850 a T-867 para no chocar con la sesión 1. Plan: docs/plan-06nov-s2t.md.

Para no confundir mis fallas con las que ya había, comparé contra las 61 fallas de PlayMode de la base de la noche del 6-nov
(`Logs/resultados-playmode-base-s2n.xml`). Al final: compila y EditMode da 1858/1858 sobre main.

## Lo que quedó (todo en main)

| Tarea | Qué | Evidencia |
| --- | --- | --- |
| T-850 | V09 taxi y V10 pickup en el tráfico. Los zips se llamaban `V09.zip`/`V10.zip` y la raíz del FBX venía girada 90° en X: sin ese giro, el taxi quedaba parado de punta y la pickup de lado. La pickup además iba en reversa. | s2t-carros-nuevos.png, CarrosS2nPruebas |
| T-851 | Huecos de bala: ya no sale el círculo alrededor (era el halo gris del fondo de D01/D02) y el cráter queda opaco, siempre con textura (antes las astillas claras salían medio transparentes). | s2t-huecos-antes-despues.png, HuecosBalaS2tPruebas |
| T-852 | Holograma del arma en el HUD sin cuadro de fondo: fuera el panel azul oscuro y el marco queda solo con sus líneas. | s2n-hud-arma.png, HudHologramaArmaPruebas |
| T-853 | Casa: el piso del cuarto estaba a la misma altura que la banqueta (por eso parpadeaba la textura de cuadros). Medidor a tamaño real. Fuera el mazo de cables, el cargador CY10 y el banco de trabajo de la banqueta; el tablero de encargos pasa a la pared con letrero ENCARGOS. Hueco de la entrada listo (`Hueco_Entrada_P11`). | s2t-casa-*.png, s2t-cuarto-*.png, CasaS2tPruebas |
| T-854 | Espejo que refleja de verdad: el jugador ve SU personaje (cámara en el ojo reflejado con corte oblicuo). El EspejoJugador (el muñeco) queda apagado. | s2t-espejo-frente/lado.png, EspejoS2tPruebas, ReglaEspejoPruebas |
| T-855 | Luz de los postes: se lee la malla, se encuentra la cabeza de la lámpara y la luz va debajo, al piso, con cono de 80°. Va en las farolas C07 y en los N09, que no tenían luz (la suya es turquesa, con LuzNocturna). El poste no recibe su propia luz. | s2t-poste-malla-*.png, s2t-poste-noche-antes/despues-*.png, PostesS2tPruebas |
| T-856 | Fuera lo inventado con primitivos: 389 grupos de balcones, toldos, macetas y plantas de cubo (T508), 21 tejados Lamina y los puentes de cubos. Banquetas con la textura T01 y losas parejas. | s2t-recorrido-*.png, LimpiezaS2tPruebas |
| T-857 | Puentes: son los N02 de Tony, ya sin el puente simulado. No hay carros arriba. | PuentesS2tPruebas |
| T-858 | Callejones con salida. El mapa de paso a pie de toda la ciudad (s2t-mapa-paso.png) muestra que todo se alcanza desde la casa; las zonas sueltas son interiores con puerta. Los dos callejones sin salida ahora llegan a otra calle: el del mercado a la orilla norte y el de la casa a la orilla oeste. | s2t-mapa-paso.png, s2t-callejon-*.png, SalidasS2tPruebas |
| T-859 | Piezas flotando: había 24 (cámaras CY08, plataformas de dron, lámparas N01, un registro de medidores) y quedaron pegadas a su pared. El detector da cero. | s2t-flotantes-*.png, FlotantesS2tPruebas |
| T-860 | Menos niebla: la de noche baja de 0.02 a 0.009, y se quitaron las cortinas de niebla del callejón y de Suburbios que tapaban la ciudad. | s2t-niebla-*-antes/despues.png |
| T-861 | Nombres de calles: letreros C15b del kit en los postes de los cruces, con el color de la zona (EJE CENTRAL, CALLE DE LOS CABLES, CALLE DEL MERCADO, AV. COYOTE, AV. NEÓN, PERIFÉRICO). | s2t-calles-*.png, CallesS2tPruebas |
| T-862 | Taller: fuera la pantalla LED de media puerta y el letrero de hotel atravesado. Queda un solo letrero, EL CANEPA / TALLER MECÁNICO, y Canepa usa el modelo del Mecánico (Visual_Mecanico, con Idle) dentro del taller. | s2t-taller-*.png, TallerS2tPruebas |
| T-863 | Casino VivaCoin abierto las 24 horas. | CasinoInteriorPruebas, PuertasPruebas |
| T-864 | Doc: la silla ya te sienta aunque estés sano (revisión gratis); antes no se ofrecía y parecía rota. El Doc te regala el EXO DE PIERNAS I la primera vez que hablas con él. El consultorio es sólido por fuera. | s2t-doc-*.png, SillaClinicaPruebas, RegaloImplanteS2tPruebas, DocS2tPruebas |
| T-865 | Tino un 12 % más grande, con su frase "El arma que quieras, la mejoramos. Acércate al banco.". El menú se arma al cargar la escena y abre al instante. | s2t-tino.png, MenuArmasPruebas |
| T-866 | Implantes: EXO DE PIERNAS, EXO DE BRAZOS (ahora también recarga más rápido) y VISOR (ves a los enemigos a través de las paredes: VisorRayosX). Se compran con el Doc. | VisorRayosXPruebas |
| T-867 | Comparaciones lado a lado, referencia contra juego. | s2t-ref-*.png |

## Decisiones que tomé sin preguntar
- **Puentes sin escalera (T-857).** Para subir a la cubierta del N02 (8.4 m) harían falta cinco módulos L03 de 4.5×4.8 m, y bajo las
  puntas la banqueta mide 1.4 m. Por eso los puentes se quedan de adorno y pedí al chat de arte E01, una escalera recta de banqueta
  a puente (docs/pedido-arte-06nov-b.md §4).
- **"Mini ciudad".** No encontré una cuadra incoherente que no fuera del kit. La del sur de la casa usa fachadas del kit y se ve
  bien. Quité lo de primitivos (balcones, toldos, tejados, macetas, puentes) y dejé los edificios base y las salidas del mapa
  (T530), porque sin ellas se vería el vacío. Si era otra zona, dime cuál.
- **El set del hacker se queda.** Lo quité en T-856, pero es la silla, mesa y laptop de T-639; sin él el hacker se sentaba en el
  aire. Lo regresé.
- **El banco del mecha.** El banco de trabajo junto a la casa era el banco del taller del mecha. Quité el banco; el punto para
  reparar el mecha sigue en su lugar.
- **El regalo del Doc** va en un componente nuevo que escucha al Doc y no en DirectorPandillas, para no chocar con la sesión 1,
  que está rehaciendo el tutorial.
- **La silla sana no cobra.** Es una revisión gratis, para que siempre puedas sentarte.

## Pendiente / para Tony
- **NPC que atraviesan modelos** y **texturas rotas en muros**: no me alcanzó. No encontré texturas rotas en las fotos que
  revisé. Los obstáculos de navegación de los peatones están en scripts de Tráfico, que son de la sesión 1.
- **Azoteas lisas.** Al quitar los tejados de cubo quedaron lisas. La referencia tiene tinacos y antenas; se pueden poner con el kit
  (C09, CY04) en otra pasada.
- **Calle Obrera sin letreros.** Sus dos cruces no tienen poste cerca.
- **Modelos que llegan del chat de arte:** P11 entrada de la casa (el hueco está listo), IMP01-03 implantes, L15 letrero Canepa (ya
  hay letrero con texto en el C15a; si llega el L15 se cambia) y E01 escalera de puente.
- **Si se vuelve a correr `CreadorImplantes`,** hay que correr después `ImplantesS2t.Aplicar` (los nombres nuevos y la recarga del
  exo de brazos).

## Fallas que no son mías (ya estaban o vienen de la sesión 1)
- Las 61 de la base de anoche siguen igual: el mercado negro de 24 horas, la cámara libre, recorridos largos y otras.
- **Escudo de energía nuevo de la sesión 1:** absorbe 30 de daño. Por eso fallan `PersonajesApoyoPruebas.RecorridoProfes` (vida 70
  en vez de 40) y `PandillaAPiePruebas.Pelea_PersiguenYPegan...` (golpe de 10 en vez de 40). Hay que ajustar esas pruebas al escudo.
- `ControladorBaseJugadorPruebas` (2) y `EscalaJugadorT670Pruebas`: capas del Animator del jugador (sesión 1), y el Doc mide 1.98
  por el T-812 de anoche.

## Para la próxima vez
- Por poca memoria, la sesión cortó tres veces las pruebas largas de PlayMode que corrían en segundo plano. El Unity de la prueba
  siguió solo y leí su resultado al terminar. La sesión 1 corre su propio Unity al mismo tiempo.
- Si `verificar.sh` dice "UNITY ABIERTO" pero el único Unity vivo es el de mecha-s1, el `Temp/UnityLockfile` de aquí quedó viejo
  y se puede borrar.
- Unity no quedó abierto en esta carpeta.
