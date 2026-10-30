# Guía para jugar el build provisional (Tony)

## Juego de pandillas (Builds/Juego, fase G desde T-400)
`Builds/Juego/Cyberfix.exe`: arranca en Ciudad_Prologo (sin mechas; docs/diseno/pandillas.md). Cada tarea de T-400 a T-406 agrega
aquí 3 líneas de qué probar. La demo final de 15 minutos queda en `Builds/Demo/Cyberfix.exe` (T-406).

### Cómo abrir Builds/Juego
Doble clic en `Builds/Juego/Cyberfix.exe` (en la raíz del repo; no está en git, se regenera con
`-executeMethod MechaCyberpunk.Progresion.Prologo.Editor.ConstruirDemo.ConstruirJuego` o el menú Mecha Cyberpunk > Demo > Construir juego de pandillas).
Arranca directo en Ciudad_Prologo, a las 19:00, delante de la casa.

### T-400 tercera persona (qué probar)
- Camina con WASD, corre con Shift y salta con Espacio: la cámara va sobre el hombro y el ratón la orbita; ¿marea o se siente bien? ¿el muñeco se ve vivo?
- Clic derecho apunta (la cámara se acerca y sale la retícula); con 2 sacas la pistola y clic izquierdo dispara; con 1 y clic izquierdo golpeas.
- Dos muñecos de prueba están a 8 y 12 m de la puerta de la casa para pegarles; no hay mecha ni emboscada.

### T-401 carros (qué probar)
- Camina a un carro estacionado (6 con colores distintos, cerca de la banqueta) y pulsa E a 2.5 m o menos: suena la alarma 3 s (luces que parpadean), sube el calor y ya es tuyo.
- Maneja con WASD: la cámara va detrás y el ratón la orbita; sin mover el ratón 1.5 s vuelve atrás y se aleja un poco con la velocidad; C cambia a primera persona. ¿Se siente de GTA o se pierde en las vueltas?
- Frena y pulsa E para bajar: vuelves a la cámara de hombro mirando hacia donde iba el carro. Subir otra vez no suma calor. ¿Robar se siente travieso? ¿Bajar es rápido?

### T-402 pandilleros armados (qué probar)
- Acércate a las banquetas del centro (Calaveras, magenta) y del este (Víboras, verde ácido): a menos de 35 m te disparan; ves un láser rojo 0.35 s antes de cada tiro y ellos se cubren de lado. ¿Ves venir el tiro o te matan sin avisar?
- Véncelos con la pistola (2 y clic): cada uno suelta $40 y suma +8 de calor, cada tiro tuyo en la calle +2; con calor 1 llegan drones y desde calor 2 también te disparan. ¿Da miedo o estorba?
- Si caes, despiertas en la clínica del mercado negro (cobra $100, calor a cero); tu vida se regenera sola tras 6 s sin daño; los grupos reaparecen a los 40 s si estás lejos.
### T-403 cuatro armas (qué probar)
- Con $400 ve al mercado negro (siempre abierto), pulsa E y compra la ESCOPETA ($350); sal con Esc y pulsa 3: 8 perdigones en cono, pega fuerte de cerca. También 4 = subfusil (mantén el clic), 1 = puños, 2 = pistola y la rueda del ratón cambia de arma (solo las que tienes). ¿Cada arma se siente distinta?
- La tienda ofrece solo la mejora siguiente de cada arma (nivel I, II, III: más daño, más cargador, recarga más rápida); el HUD dice "ESCOPETA II 5/6". ¿Se nota el nivel II? ¿Valen los precios?
- Los puños también suben de nivel (más daño y golpe más rápido). Las balas de reserva son infinitas.

### T-407 CYBERFIX, sonidos y mercado (qué probar)
- La ventana y el .exe se llaman CYBERFIX / Cyberfix.exe. Camina al mercado negro: la puerta se abre sola al acercarte; hay un vendedor tras el mostrador, un letrero y "E · Mercado negro". Compra la escopeta con E, flechas y Enter.
- El Tianguis (calle principal entre x -47 y 47) tiene un arco magenta "EL TIANGUIS" y unos 11 puestos con lona de color. La esquina de la pantalla dice EL TIANGUIS al entrar. ¿Se reconoce como tianguis?
- Disparos, golpes, pasos, saltos, compras y menús suenan con sonidos provisionales (generados). La lluvia, la música y los ambientes están apagados. ¿Los disparos pegan o estorban?

## Build viejo con mechas (antes de la fase G)

Build de Windows con las escenas de prueba de Tony más Ciudad_Prologo y Clandestino de Alan (T-136/T-137 aún pendientes: todavía no hay demo encadenada).
No está en git (`Builds/` está ignorado): se regenera con un script de editor temporal. Actualizado al bloque 6 (implantes, pandilla a pie y clínica, T-190 a T-197) y a los assets 3D de T-250.

## Cómo abrirlo
- `Builds/Provisional/MechaCyberpunk.exe` (en la raíz del repo). Doble clic. Arranca en CreacionPersonaje (título y creación de personaje).
- `Builds/Provisional_Implantes/MechaCyberpunk.exe`: las mismas 13 escenas, pero arranca directo en Pruebas_Implantes. Úsalo para probar el bloque 6 (ver "Implantes y la clínica" abajo).
- `Builds/Provisional_Assets/MechaCyberpunk.exe`: arranca en Ciudad_Prologo con los modelos 3D de T-250 (build del 28-oct, antes del bloque 6).
- `Builds/Provisional_Misiones/`: build viejo del bloque 5 (no se regeneró); Pruebas_Misiones sigue dentro de Provisional como escena 8.

## Escenas incluidas (en el orden del build)
| # | Escena | Qué es |
| --- | --- | --- |
| 0 | CreacionPersonaje | Título y creación de personaje; arranca aquí |
| 1 | Pruebas_Prologo | El prólogo (ciudad de prueba de Tony) |
| 2 | Pruebas_Flashback | El flashback |
| 3 | Pruebas_Tony | Banco de pruebas del mecha y la pelea |
| 4 | Pruebas_Economia | Taller, banco, mercado negro, mostrador de peleas |
| 5 | Pruebas_Calle | Calle de prueba con rival |
| 6 | Pruebas_APie | Jugador a pie |
| 7 | Pruebas_Tutorial | Tutorial |
| 8 | Pruebas_Misiones | Misiones, reputación, teléfono y favores (el bloque 5) |
| 9 | Pruebas_Implantes | Implantes, clínica, pandilla a pie y el doctor (el bloque 6); en Provisional_Implantes es la escena 0 |
| 10 | Ciudad_Prologo | Ciudad de Alan con los assets de T-250 |
| 11 | Clandestino | Arena de Alan con los assets de T-250 |
| 12 | Pruebas_Assets | Los 38 modelos de T-250 en filas por zona |

Cómo moverse entre ellas: solo el flujo del juego cambia de escena (CambioEscena): título -> prólogo -> flashback -> prólogo, siguiendo la historia.
No hay menú para saltar a las demás; las escenas 3 a 8 están en el build pero no hay forma de llegar a ellas desde el juego. Para probarlas
una por una, cambia el orden en el script de build (la primera escena es la que arranca) o usa el editor.

## Controles
A pie (ControlesAPie): 
| Acción | Tecla |
| --- | --- |
| Moverse | W A S D |
| Mirar | Ratón |
| Correr | Shift izquierdo |
| Saltar | Espacio |
| Interactuar (subir/bajar del mecha, bancos, mostradores) | E |
| Atacar | Clic izquierdo |
| Puños / pistola | 1 / 2 |
| Recargar | R |
| Soltar/recuperar cursor | Esc |

En el mecha (ControlesMecha):
| Acción | Tecla |
| --- | --- |
| Moverse / mirar | W A S D / ratón |
| Golpear / bloquear | Clic izquierdo / clic derecho |
| Soltar cursor | Esc |
| Confirmar | F |
| Levantar | Espacio |
| Cristal | Q |
| Llamar al mecha | M |
| Habilidad del brazo / del torso (solo en pelea) | 1 / 2 |

Habilidades: el brazo izquierdo y el torso con habilidad (BRAZO IZQ. MILITAR I, TORSO TECH I...) gastan una munición por uso y se enfrían. La munición
(misiles, celdas, cargas, combustible) se compra solo en el mercado negro, pestaña COMPRAR; no se vende. Sin munición solo quedan los brazos.

Pantallas (menús de piezas, apuestas, tienda): flechas y Enter para elegir, Esc para cerrar. Las ayudas de controles (T-134) salen solas
en pantalla según lo que hagas. Teclas de prueba (solo en escenas de prueba): P iniciar pelea, H hacerse daño, G usar kit, T entrar a la arena,
F5/F9 guardar/cargar, 8 y 9 y 0 cargan deuda/brazo/kit o calor, J saltar reloj, L vencer rival de la liga, O siguiente objetivo.

## 10 cosas que Tony debe probar
1. El título y crear un personaje: que se pueda completar y pase al prólogo.
2. Que el prólogo avance al flashback y regrese sin quedarse trabado.
3. Caminar, correr y saltar a pie: sensación y cámara.
4. Subir al mecha con E, encenderlo y bajar; llamarlo con M.
5. Golpear y bloquear en una pelea (P para iniciarla): peso y daño.
6. Los kits de reparación en plena pelea (G) y reparar en el banco.
7. Comprar y vender piezas en el mercado negro (flechas y Enter).
8. Apostar en el mostrador de peleas y ver el pago.
9. Guardar con F5, cerrar el juego, abrir y cargar con F9.
10. Fluidez: que no haya tirones ni errores al cambiar de escena.

## Qué probar del bloque 5 (en Provisional_Misiones)
1. Teléfono (T): ¿se siente rápido o estorba? Cambia de pestaña con las flechas y ciérralo con Esc o T.
2. Misión de barrio con Doña Lupe (E junto a ella): ¿la flecha y el texto del HUD te llevan sin perderte? ¿Recoger y entregar con E se siente natural o falta algo?
3. Los tres vecinos (Doña Lupe, el taquero, la vecindad): ¿se sienten de barrio?
4. Contrato corporativo por teléfono de día: ¿el aviso "BARRIO −12" te hace pensar dos veces antes de aceptar otro? Prueba también de noche ("NADIE CONTESTA").
5. De noche, el contacto del bar (E): ¿se siente escondido y de noche? Acepta el robo de la bodega y escapa: ¿es tensión real o solo esperar a que baje el calor?
6. La pandilla del callejón: ¿se siente como defender al taquero? ¿Vender la chatarra después se siente como el cierre del robo?
7. Favores: ¿se sienten como algo que el barrio te debe? ¿El escondite te salvó en un robo? ¿30 % de descuento en una compra es generoso o tacaño?
8. Reputación: ¿JUSTICIERO y ENEMIGO suenan al juego? ¿Sentiste el jalón entre barrio y corporaciones? Mira el grafiti GRAFITI del muro oeste.
9. F5 y F9 con una misión en curso: la misión se cancela sin castigo y no sale un aviso falso de reputación.
10. En general, 20 minutos de un día y una noche: ¿las misiones dan ganas de hacerlas entre peleas o son relleno?

## Misiones y reputación (Pruebas_Misiones)
Teléfono: T (contratos de día, misión, reputación, favores). Flechas ← → cambian de pestaña, ↑ ↓ y Enter eligen, Esc o T cierran. CONTRATOS lista las
corporativas solo de día (de noche "NADIE CONTESTA"); MISIÓN muestra la que llevas y permite abandonar (Enter dos veces, castiga); REPUTACIÓN muestra
las dos barras (barrio y corporaciones); FAVORES muestra DESCUENTO, INFORMACIÓN y ESCONDITE (máximo 3 de cada uno).
Dónde se aceptan: los encargos de barrio con E junto a los vecinos (Doña Lupe, el taquero, la vecindad), las corporativas por teléfono de día, y las
ilegales y los robos con E en el contacto del bar, de noche. Una misión a la vez; la flecha y el texto del HUD te llevan paso a paso (E para recoger y entregar).
Los favores: DESCUENTO baja 30 % tu siguiente compra en el mercado negro; INFORMACIÓN abre EL SOPLO con el contacto del bar; ESCONDITE se usa con E
en un escondite del barrio (azotea, lavandería) cuando hay calor: lo borra y cumple el paso de escapar. Pruebas_Misiones es la escena para probarlo todo
(J pasa al día siguiente a las 8:00; F5 guarda y F9 carga: la misión en curso se cancela sin castigo).



## Implantes y la clínica (Pruebas_Implantes)
Escena para probarlos: `Pruebas_Implantes` (Play, de día a las 10:00; hay pista de 30 m, cajas de salto, muñecos, espejo, taller, mostrador de peleas y casa).
- **Clínica**: de día (8:00 a 20:00) camina al mostrador "CLÍNICA" y pulsa E. Flechas arriba/abajo eligen la pieza (OJOS, BRAZOS, PIERNAS, SISTEMA NERVIOSO), Enter pide confirmar, Enter otra vez instala, Esc sale. Cada operación cuesta dinero y 2 horas. Los de nivel 2 se abren en la liga 3 y cobran solo la diferencia. De noche dice "CERRADA".
- **Qué hace cada uno**: OJOS muestra la vida del rival y avisa su próximo golpe (en la cabina y a pie); BRAZOS pega más fuerte a pie; PIERNAS corre más rápido y salta más alto; SISTEMA NERVIOSO acelera tu golpe y tu guardia en el mecha. En el espejo de la casa (E) los ves puestos.
- **Pandilla de noche**: desde las 21:00 hay 3 pandilleros a pie en el sitio de la pandilla; persiguen, nunca pegan más de 2 a la vez y pagan al caer. De día no están.
- **Si caes en la calle** (a pie, de noche): despiertas en la camilla de la clínica a las 05:00, el doc te cobra $500 (puedes quedar en deuda), el calor se borra y el objetivo dice VUELVE A CASA; el mecha, si estaba en la calle, vuelve dañado al taller.
- **Teclas de prueba** (`ImplantesDePrueba`): 3 instala los 4 implantes de nivel 1 sin cobrar, 4 los de nivel 2, 5 los quita. 1 y 2 cambian entre puños y pistola; F5 guarda y F9 carga.

### T-404 zonas, respeto y mapa (qué probar)
- Arriba a la izquierda el HUD dice la zona (LA FOSA tuya al oeste, EL TIANGUIS de los Calaveras al centro, NEÓN ALTO de las Víboras al este) y tu respeto; cada pandillero vencido en una zona rival suma 10. Atajo de prueba: F6 suma 10 de respeto donde estés y F7 te da $500 (se apagan en la demo final). ¿7 pandilleros antes del jefe es mucho o poco?
- Con 70 de respeto sale "¡EL JEFE TE BUSCA!" y aparece su jefe en la banqueta (250 de vida, escopeta que lanza 8 perdigones, aviso de 0.5 s). Véncelo: "EL TIANGUIS ES TUYO", respeto 100 y su grupo ya no reaparece. Si caes antes, pierdes 10 de respeto. ¿El jefe es un reto?
- M abre el mapa del barrio (6 manzanas del color de su dueño, nombre, respeto y un punto blanco eres tú); M o Esc lo cierran y mientras está abierto no te mueves ni disparas. ¿Se entiende?

### T-405 tu pandilla (qué probar)
- En LA FOSA (oeste, cerca de la casa) hay 3 chavos: EL CHATO (gratis, respeto 10), LA GÜERA ($200, respeto 40) y EL TOPO ($350, respeto 70). Con F6 subes respeto y con F7 te dan $500; E junto a ellos los recluta (si falta algo, el texto dice qué).
- Los aliados naranjas te siguen, corren si te alejas, disparan a los pandilleros y suben de nivel (3 vencidos y 8); abajo a la izquierda ves su nivel y vida, y en el mapa (M) son puntos naranjas. F los manda esperar o seguir.
- Si cae uno, vuelve a los 45 s; los pandilleros también les disparan. ¿Ayudan o estorban? ¿Se nota subir de nivel?

### T-406 la demo de 15 minutos (Builds/Demo/MechaCyberpunk.exe, sin atajos F6/F7)
- Pasos: despiertas en tu casa a las 19:00 con puños y $0 (objetivo SAL A LA CALLE; WASD, ratón) → afuera 2 Calaveras le pegan a DOÑA LUPE, defiéndela con clic izquierdo (no te matan: con poca vida dejan de pegar 3 s) → el último suelta una pistola, E la recoge (clic derecho apunta, clic izquierdo dispara) → E con EL CHATO lo une gratis → sale a conquistar EL TIANGUIS (M abre el mapa, F manda esperar o seguir a tus aliados): vence Calaveras hasta 70 de respeto, aparece el jefe con la flecha → 4 s después sale "CONTINUARÁ..." y Enter te deja seguir jugando libre.
- Pistas que salen solas: robar un carro con E y el mercado negro vende escopetas ($). Los grupos del tianguis no aparecen hasta que te mandan ahí.
- Observa: ¿dura unos 15 minutos? ¿Se entiende qué hacer sin leer esta guía? ¿El final deja ganas de más? ¿La pelea con el jefe es buen cierre? Con Builds/Juego (no final) F6 salta un paso de la historia.

### T-414 nombres nuevos y Biits
- Mira: el HUD de dinero dice "Ƀ340" (símbolo de Biits); la tienda se titula EL MERCADO; el HUD de zona dice "SUBURBIOS · TUYA" en tu casa y "EL TIANGUIS · SKULLS" en el centro (ya no se encima con la radio); M abre el mapa con título MEGALOPOLIS.
- Ve al bar y al casino: los letreros de neón dicen EL FOX (cian) y VIVACOIN (naranja) y los modelos viejos están apagados; la clínica dice I+D+I.
- Observa: ¿se lee bien la Ƀ?, ¿"SKULLS" y "SNAKES" sin artículo suenan bien?, ¿los letreros nuevos tapan bien a los viejos (son pequeños)?

### T-415 tu pandilla con nombre y color
- Mira: el HUD de zona en tu casa dice "SUBURBIOS · LOS DE SUBURBIOS · RESPETO n" en naranja; reclutar al CHATO avisa "EL CHATO SE UNE A LOS DE SUBURBIOS"; al conquistar, "EL TIANGUIS ES DE LOS DE SUBURBIOS".
- Hasta T-413 solo cambia por código (ServicioPerfil.FijarPandilla("los neón", 3)): HUD, mapa (M) y el CHATO se repintan al instante.
- Observa: ¿"LOS DE SUBURBIOS" te gusta?, ¿los 4 colores se distinguen de SKULLS (magenta) y SNAKES (verde) en el mapa y de noche?

### T-416 fusil y sniper
- En El Mercado (E, flechas, Enter) compra FUSIL (Ƀ700) y SNIPER (Ƀ900); 4 saca el fusil (mantén el clic, dispara solo) y 5 el sniper (clic derecho = zoom de FOV 15 con mira negra y distancia).
- La pistola base también se vende (Ƀ150) para quien no la tenga; cada arma sube 3 niveles (I a III) en la misma tienda y la rueda del ratón recorre las 5 armas.
- Observa: ¿el sniper se siente poderoso?, ¿el zoom marea?, ¿el fusil se siente distinto del viejo subfusil?, ¿los precios cuadran con los Ƀ40 por pandillero?

### T-417 profes y peatones
- En Suburbios, a 10 a 25 m de tu casa, está el CIC (letrero turquesa): E con EL INVENTOR (TINO) abre la tienda de las 15 mejoras de armas. Más: EL DOC (JOEL) junto a I+D+I cura por Ƀ50, EL HACKER (SERGIO) en VivaCoin da una pista, EL MECÁNICO (CANEPA) junto al carro de tu casa borra el calor por Ƀ100, EL DIRECTOR (ESTRADA) en El Fox solo platica.
- Cada uno lleva su título, nombre y función sobre la cabeza (se lee a menos de 15 m); son muñecos de prueba hasta que lleguen sus modelos.
- Observa: ¿encuentras a cada profe?, ¿se entiende qué hace con solo leer el letrero?, ¿la calle se siente viva con 4 peatones de día y 6 de noche, o vacía?

### T-418 la historia de la demo con los profes y NOVA (cierre del paso 2; Builds/Demo/Cyberfix.exe)
- Nombres y Ƀ: al despertar sale "MEGALOPOLIS · SUBURBIOS"; todo dice SKULLS, SNAKES, El Mercado, I+D+I, El Fox, VivaCoin y los Biits (Ƀ); en El Mercado compra el SNIPER (5, clic derecho = zoom).
- La historia: tras el CHATO, E con EL DOC te cura gratis ("Aguanta, mijo..."), E con EL INVENTOR deja tu pistola en PISTOLA I gratis; vence SKULLS (VENCE A LOS SKULLS n/7) y a su jefe, ve con EL HACKER en VivaCoin: dice que NOVA les paga y les da armas a los SNAKES, y sale CONTINUARÁ.
- Después, en el juego libre, aparecen los SNAKES en Neón Alto: 70 de respeto saca a su jefe y al vencerlo Neón Alto es tuya. Observa: ¿se entiende sin explicarla?, ¿los profes rompen el ritmo?, ¿NOVA da ganas de seguir? (6 peatones de día y 8 de noche).

### T-408 marcador del objetivo y HUD ordenado (paso 3)
- Al despertar, un rombo naranja flota sobre la puerta con la distancia turquesa abajo ("10 m"); camina hacia él y la distancia baja. Date la vuelta: el rombo se apaga y una flecha naranja en el borde (izquierdo o derecho) apunta hacia la puerta. ¿Encuentras el objetivo sin pensar? ¿La flecha se entiende?
- M abre el mapa: el mismo rombo marca el objetivo (con contorno oscuro para que se vea sobre tu color). Arriba al centro va el objetivo en naranja ("SAL A LA CALLE", luego "DEFIENDE A DOÑA LUPE"); a menos de 3 m el rombo se apaga.
- El HUD ya no se encima: radio, renta y zona arriba a la izquierda; Biits, vida y ayuda de controles abajo a la izquierda; tu pandilla abajo a la derecha sobre el arma; subtítulos abajo al centro. ¿Te gustan los colores y la letra? ¿El rombo estorba al pelear?
