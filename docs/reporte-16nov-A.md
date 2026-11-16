# Reporte de la Sesión A del 16-nov

Esta sesión hizo la parte A de `docs/playtest-16nov.md` más los puntos 15, 16 y 17 que pediste. Fueron 19 tareas, T-1001 a T-1019, cada una en su
rama y mezclada a main con `--no-ff`. Antes de cada merge corrieron las pruebas del área; desde T-1010, también el EditMode completo.

Lo hice yo (Opus): la cámara, los menús M30/M31/M32, las armas especiales (incluido el rifle de plasma), las prótesis de ataque, la revisión de
las capturas y el arreglo del recorrido de la demo. Lo mecánico lo hicieron subagentes Sonnet en un worktree aparte y lo revisé antes de mezclar.

## 1. Arreglos (puntos 1 a 9)

| Punto | Tarea | Qué quedó |
| --- | --- | --- |
| 1 Cámara | T-1001 | Sin apuntar, como CAM04: personaje casi centrado (centro X 0.444), cuerpo completo. Apuntando, como CAM05: se acerca a 1.5 m y pasa al hombro derecho, el personaje queda a la izquierda y la cruceta libre. Con pared a la derecha cambia sola al hombro izquierdo, sin atravesarla. |
| 2 De día | T-1002 | La partida nueva y la demo empiezan a las 10:00 (`ConfigRelojMundo_Pandillas` y `_Mundo`). El ciclo y el guardado no cambian. |
| 3 Golpe | T-1003 | Al golpear, el cuerpo se gira solo hacia el oponente vivo más cercano al frente: radio 2.5 m, cono ±70°, sin muro en medio. Apuntando no gira. |
| 4 NPC | T-1004 | Pandilleros, peatones, policía, guardias de Nova, meseras, aliados y jefes usan los clips de PJ01 (caminar, correr, idle, saltar). Sus máquinas de estados se conservan. |
| 5 Doc | T-1005 | El Doc dice "Elige tu prótesis, te regalo la de piernas" y ya no la instala solo. En la clínica, EXO DE PIERNAS I sale **GRATIS** y la compras tú. El nivel II se compra en cuanto tienes el I, sin liga. |
| 6 Patrullas | T-1006 | Detecta atascos: reversa con giro, y si sigue atorada se recoloca en un cruce que la cámara no ve. Volcada a la vista, se endereza. El volante ya no tiembla. |
| 7 Ruedas | T-1007 | Las ruedas de V01a, V01b, V05 y Patrulla (y sus variantes de tráfico) se midieron contra su hueco: 0.90 × el hueco, apoyadas en el piso. La corrección de la moto (T-976) está en main y en la escena del build. |
| 8 Drones | T-1008 | Vuelan con inercia: aceleración y frenado suaves, giro limitado, se inclinan al acelerar y al girar, y flotan un poco. |
| 9 Nova | T-1009 | El Capitán y los guardias se defienden si los golpeas o les disparas, y pegan fuerte. Guardia: vida 300, golpe 30. Capitán: vida 1200, golpe 40, carga 60. Sin provocarlos no atacan. |

## 2. Menús (punto 15)

- **T-1010, rueda M30**: anillo de sectores turquesa con el resaltado en amarillo y un octágono al centro con el nombre y las balas.
  - Solo muestra lo que llevas, con un mínimo de 8 sectores.
  - Con la rueda abierta se esconde todo el HUD, también el minimapa (era un pendiente del 15-nov).
  - Con las 10 armas y 3 objetos quedan 13 sectores (`docs/capturas/t1010/rueda-todas-las-armas.png`).
- **T-1011, barra M31**: "[1] PIERNAS" arriba, el dibujo de línea de la prótesis (I o II, de H41) en la caja y el medidor abajo.
  - Lista en turquesa, activa en amarillo, recargando en rosa.
  - Las casillas [4] LÁSER y [5] MISILES salen solo si tienes la prótesis.
- **T-1012, tienda M32**: El Mercado lleva el título al centro, la lista angosta con el dinero grande, y a la derecha el nombre del arma en grande.
  - Abajo del nombre: su holograma 3D girando sobre una rejilla, las barras DAÑO/CARGADOR/CADENCIA/ALCANCE y el botón COMPRAR.
  - Las demás tiendas quedan como antes.

## 3. Armas nuevas (punto 16)

- **T-1013, datos y disparo**: subfusil (automático), revólver (6 recámaras), lanzagranadas, granada y rifle de plasma, con 4 niveles cada uno y sus balas en El Mercado.
  - Lanzagranadas: la granada vuela en arco hacia la mira y explota al chocar, con daño en área (radio 4.5 a 6 m).
  - Granada: al apuntar se ve el arco amarillo; se lanza con clic, rebota y explota al acabarse la mecha (2.2 s).
  - Rifle de plasma:
    - Mantener el clic carga 3 s, con una bola de energía cian en la boca; si sueltas antes, se cancela.
    - Luego sale un rayo de 5 s que sigue a la mira, atraviesa a la gente, daña todo lo que toca y para en las paredes.
    - Gasta una celda (3 en el cargador).
  - Tambor: el del revólver y el del lanzagranadas giran una recámara por tiro.
- **T-1014, modelos**: los 4 modelos de `imageassets/modelos3d/armas_nuevas` más una granada hecha en Blender.
  - Están en la mano (con agarres), en el piso (se recogen con E), en la rueda, en el HUD, en el banco de Tino y en el holograma de El Mercado.
  - Los tambores se separaron de la malla para que giren.
  - Los hologramas del subfusil, el revólver, el lanzagranadas y el rifle de plasma son los tuyos de `imageassets/ui/hologramas`. El de la granada lo generó el subagente.
- **T-1015, revisión**: revisé el rifle de plasma en la calle y corregí la bola de carga (no se veía).
  - Las armas especiales ahora disparan en `LateUpdate`, cuando el arma ya está en su lugar.
  - Capturas en `docs/capturas/t1013/`.
- **T-1016**: captura de El Mercado con el rifle de plasma (`docs/capturas/t1012/mercado-rifle-plasma.png`).

## 4. Prótesis de ataque (punto 17)

- **T-1017**: brazo láser [4] (Ƀ2,500) y hombrera de misiles [5] (Ƀ3,500).
  - Se compran con el Doc: en la clínica, **[TAB] PRÓTESIS DE ATAQUE** abre la tienda; Enter compra y Esc vuelve.
  - El láser dispara un rayo rojo desde el brazo, 4 s a 80/s, que atraviesa a la gente.
  - La hombrera lanza 4 misiles teledirigidos a los enemigos de enfrente; cada uno explota en área.
  - Se guardan con la partida y salen en la barra M31.
- **T-1018**: se ven en el cuerpo. El brazo láser va en el antebrazo derecho. La hombrera resultó ser un arnés: dos hombreras, correas y el lanzador arriba. Lo puse en el pecho, a lo ancho de los hombros, con el lanzador sobre el hombro izquierdo. El rayo y los misiles salen de los modelos.

## 5. Recorrido de la demo

**T-1019**: el recorrido de la demo se trababa en EL INVENTOR (7 bugs encadenados). El error estaba en el bot, no en el juego.
- Desde T-1005 el Doc primero habla y la clínica abre un momento después; el bot pulsaba Esc antes, y la clínica se quedaba abierta tapando todo.
- Ahora el bot espera y la cierra cuando aparece.
- **RecorridoDemoPruebas: 3/3.** La demo llega de principio a fin al juego libre.
- Quedan 4 bugs anotados que no la detienen:
  - El bot tarda en llegar al Doc, a EL INVENTOR y a EL HACKER (topes al caminar).
  - El marcador de EL HACKER queda a 165 m del personaje, en Neón Alto (zona de la Sesión B).

## Builds

Se hicieron desde main `5600608`, que ya trae los cambios de la Sesión B, uno por uno y como proceso aparte.
- **Demo**: `C:\Users\Angel Perez\Desktop\videojuego\mecha-cyberpunk\Builds\Demo\CyberCity.exe` (ConstruirDemoPandillas).
  - Terminó a las **08:00** del 16-nov, hora de México.
  - `Logs/build-demo-16nov.log` dice "Build Finished, Result: Success".
- **Juego**: `C:\Users\Angel Perez\Desktop\videojuego\mecha-cyberpunk\Builds\Juego\CyberCity.exe` (ConstruirJuego).
  - Terminó a las **08:34**.
  - `Logs/build-juego-16nov.log` dice "Build Finished, Result: Success".
- El ejecutable `CyberCity.exe` conserva la fecha del 9-nov porque es el mismo player de Unity; los datos de `CyberCity_Data` son de hoy.
- Antes de `git pull` y del recorrido se esperó a que no hubiera ningún Unity de `.sesiones/s2` (06:56). Los builds se hicieron con la Sesión B corriendo pruebas en paralelo, porque no paraba de lanzar corridas.
- El Unity de esta sesión quedó cerrado. A las 08:34 seguía abierto uno de la Sesión B con sus pruebas.

## Pendientes y notas

- **Prótesis en el cuerpo, falta que las veas**: el lanzador de la hombrera queda junto a la cabeza, del lado izquierdo, y puede estorbar un poco la vista en tercera persona. Sus números están en `ColocarProtesisT1018.Todas`.
- **Ruedas horneadas**: V02, V03, V04, V06, V09 y V10 son una sola malla con las ruedas incluidas; no se pudieron ajustar sin separar la malla en Blender. C19 no tiene llanta.
- **NPC con PJ01**: se pierden las animaciones propias de SKULLS, del jefe y de Nova (burla, golpe de Nova). Todos caminan y corren igual que el jugador, como pediste.
- **Tira de casillas del HUD a pie**: sigue con 5 casillas, porque las 10 armas no caben. El arma en la mano sí muestra su holograma.
- **Piezas del banco de Tino**: los modelos nuevos no traen cargador, cañón ni mira separados, así que en el banco se ve el holograma completo sin piezas.
- **Pruebas que fallan y ya fallaban antes de esta sesión**:
  - `NeonAltoT505.EntradaDelCasino`
  - `LuzK01S2o.Postes`: entró con la Sesión B (T-1102/T-1112)
  - `HudH10.Estrellas`
  - `ArmasPruebas.ComprarYUsarEscopeta`
  - `CuerposBotinT746`
  - `PandillaAPie` ×8
  - `JefeNovaT693.CadaTresGolpes`
  - `CanonAlPuntoDeMiraT681.CuatroArmas`
  - `AnimatorPJ01T733` ×6
  - `RecorridoRonda2.MercadoTianguis`
  - `RecorridoNombres`: el bot no llega al mostrador caminando; El Mercado siempre está abierto, así que no es por empezar de día.
- **Pruebas intermitentes**: `NpcCaminanConClipsPJ01T1004.Peaton` falla dentro de su clase y pasa sola. `PersecucionRealistaT587.Patrulla_QueCortaElPaso` depende de la carga de la máquina.
- **Memoria**: varias corridas largas se cortaron porque el Unity de la Sesión B corría al mismo tiempo (~5 GB cada uno). El recorrido de la demo se relanzó como proceso aparte.
- **Disco**: quedaban 4 GB libres. Borré el worktree de los subagentes (`../mecha-a2`; sus ramas están en origin) y quedaron 14 GB para los builds.
- **Carpeta suelta**: queda `C:\b\`, fuera del repo, con 8 renders de Blender que dejó un subagente. Se puede borrar.
