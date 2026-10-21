# Revisión de Tony

Estado al 2026-10-15: sesión de aprobación. Tony aprobó tony/fase-3 y tony/fase-4 sin revisión extra y están en `main`
(merge limpio; compilar OK, EditMode 375/375, PlayMode 304/304 sobre main).

## Piezas (T-F7, bloque 3) cerradas: qué probar jugando (2026-10-21)
Del lado de Tony, en `Escenas/Tony/Pruebas_Economia.unity` (Play, de noche, sin ayuda). Sin revisor: T-F7 bloque 3 no pasó por revisión de Opus.
Base automática: `RecorridoPiezasPruebas` (T-173) recorre con entrada simulada: comprar PIERNAS CUERPO A CUERPO I en el taller (cobra $500; las de nivel II salen "SE ABRE EN LA LIGA 2"), subir al mecha, encender y medir la velocidad con W (4.25 m/s = la compuesta), apostar $250 a TORNILLO y ganar la pieza en juego, comprar BRAZO IZQ. FUEGO I y BRAZO DER. MILITAR I, cambiar entre brazos del inventario sin cobrar, notar el golpe de 15.5 (serie 10) peleando con LA RATA, vender lo viejo en el mercado negro, liga 3 (el nivel III se compra, el IV sale bloqueado), renta vencida (el taller no abre y nada se compra) y F5/F9 con 20 de daño. 3 corridas seguidas pasan (~59 s reales, ~58 s de juego cada una; 4 con la de las capturas). Dinero final $3,080 = calculado desde los assets; piezas finales e inventario final coinciden. Tiempo de juego por paso (corrida 2): a) 0.0 s, b) 0.1 s, c) subir, encender y medir 11.8 s, d) TORNILLO y reparar 19.9 s, e) brazos, encendido y LA RATA 26.6 s, f) a i) 0.0 s (solo teclas de pantalla). Capturas: `docs/capturas/T-173-1.png` (PIEZAS DEL MECHA con ramas mezcladas y el nivel II bloqueado), `T-173-2.png` (cabina con brazo de fuego y brazo militar en el paso 3 del encendido), `T-173-3.png` (mercado negro, pestaña VENDER con piernas de serie y piernas CUERPO A CUERPO I). Detalles y atajos: `docs/bitacora/2026-10-21-tony-T-173.md`.
Resultado de la suite (2026-10-21, tony/fase-7 con T-163 a T-173): compilar OK; EditMode 869/869; PlayMode por área 819/834: Combate 40/40, Core 3/3, Economia 19/19, IA 35/35, Mecha 139/142, Mundo 141/141, Pelea 66/66, Personaje 111/111, Progresion 113/113, UI 114/117, Vehiculos 30/30; Arena con los 9 fallos conocidos de Alan (JaulaClandestino, Capsula_EmpujadaContraLaReja). Las 6 restantes son capturas ignoradas a propósito. Ningún área marcó LIMITE. El verificador independiente pasó T-163 a T-169 sin fallos reales (creadores idempotentes, md5 igual). Hallazgo: AspectoPiezasPruebas no era InputTestFixture y rompía 51 pruebas de Mecha; corregido en T-170. T-136 y T-137 siguen bloqueadas: Ciudad.unity en main no trae DirectorPrologo.

Lista para un humano, en orden de juego (sin L ni atajos; peleas de verdad):
1. T-166 Tabla de bonos y precios (abajo): ¿las ramas se sienten distintas en el papel? ¿Fuego (golpe más seguido) contra militar (golpe más pesado) te convence, o quieres otra diferencia?
2. T-169 PIEZAS DEL MECHA (E en el banco con el mecha sano): arma un mecha con brazos de ramas distintas. ¿Se entiende de un vistazo qué cambia cada pieza (VIDA +2 · DAÑO −1) y cuánto cuesta? Con 10 o más filas la lista roza la línea de estadísticas (hoy el máximo es 9).
3. T-169 Las piezas de la liga siguiente salen en gris ("SE ABRE EN LA LIGA 2"): ¿te dan ganas de subir de liga o estorban? ¿$500 por la primera pieza se siente alcanzable después de una o dos peleas?
4. T-170 Compra un brazo de fuego y otro militar y súbete: ¿se nota la mejora al primer vistazo? ¿Magenta, azul acero, turquesa y naranja se leen como ramas y no como alarma? ¿Las franjas de nivel se ven sin buscarlas? ¿La lista de 5 piezas durante el encendido ayuda o estorba?
5. T-171 Apuesta $250 contra TORNILLO: ¿la pieza en juego te tienta? ¿Ganarla y ponértela en el taller se siente como un premio? La revancha de la cartelera ya no da pieza.
6. T-167/T-168 Peleando: ¿cuerpo a cuerpo se siente más aguantador y corre más, y militar pega más fuerte? ¿Cambiar una pieza conserva el daño (hoy no repara)? ¿Vender la vieja en el mercado negro (la mitad del precio; $100 las de serie) se siente justo?
7. T-168 Renta vencida: el banco dice "TALLER BLOQUEADO · debes $500 de renta" y no deja mejorar. ¿Se entiende que hay que pagar antes?
8. T-172 Balance (`L` hasta la liga 3): con mecha de nivel II, ¿VOLTIO se le gana y EL ALCALDE se siente casi imposible hasta mejorar? Con nivel III, ¿EL ALCALDE se le gana peleando bien?
9. Criterio de sensación de T-173: la liga Sótano de verdad (sin L), unos 20 minutos, comprando piezas con lo que ganas. ¿Cada pieza se nota al pelear? ¿El dinero alcanza para reparar Y mejorar, o hay que escoger (lo que se busca)? ¿Llegas a DON FIERRO con ganas de ponerte el nivel I que te falta?
10. T-F5 Guardar: F5, cambia piezas y dinero, F9: ¿vuelven las mismas 5 piezas, la vida máxima, el daño y el inventario?

Tabla de bonos y precios (T-166; 85 piezas = 5 de serie con bonos 0 + 4 ramas x 4 niveles x 5 ranuras; leída de los assets). Todas las ramas cuestan lo mismo por nivel: I $500, II $800, III $1,200, IV $1,800; vender paga la mitad ($250, $400, $600, $900; las de serie $100). V = vida, D = daño por golpe, Vel = m/s, R = recarga entre golpes en s (negativo = golpea más seguido; la recarga nunca baja de 0.3 s). Base del mecha: 100 de vida, 4 m/s, 10 de daño, 0.8 s de recarga.

Militar (más golpe):
| Nivel | Precio | Brazo izq. y der. | Torso | Piernas | Cabina |
| --- | --- | --- | --- | --- | --- |
| I | $500 | V+2 D+3 | V+14 | V+4 Vel+0.08 | V+4 |
| II | $800 | V+4 D+6 | V+28 | V+8 Vel+0.16 | V+8 |
| III | $1,200 | V+6 D+9 | V+42 | V+12 Vel+0.24 | V+12 |
| IV | $1,800 | V+8 D+12 | V+56 | V+16 Vel+0.32 | V+16 |

Tech (equilibrada):
| Nivel | Precio | Brazo izq. y der. | Torso | Piernas | Cabina |
| --- | --- | --- | --- | --- | --- |
| I | $500 | V+4 D+2 | V+22 | V+6 Vel+0.15 | V+6 |
| II | $800 | V+8 D+4 | V+44 | V+12 Vel+0.3 | V+12 |
| III | $1,200 | V+12 D+6 | V+66 | V+18 Vel+0.45 | V+18 |
| IV | $1,800 | V+16 D+8 | V+88 | V+24 Vel+0.6 | V+24 |

Cuerpo a cuerpo (más aguante y más velocidad):
| Nivel | Precio | Brazo izq. y der. | Torso | Piernas | Cabina |
| --- | --- | --- | --- | --- | --- |
| I | $500 | V+5 D+1 | V+30 | V+10 Vel+0.25 | V+8 |
| II | $800 | V+10 D+2 | V+60 | V+20 Vel+0.5 | V+16 |
| III | $1,200 | V+15 D+3 | V+90 | V+30 Vel+0.75 | V+24 |
| IV | $1,800 | V+20 D+4 | V+120 | V+40 Vel+1 | V+32 |

Fuego (golpe más seguido):
| Nivel | Precio | Brazo izq. y der. | Torso | Piernas | Cabina |
| --- | --- | --- | --- | --- | --- |
| I | $500 | V+2 D+2.5 R−0.03 | V+12 | V+3 Vel+0.12 | V+3 |
| II | $800 | V+4 D+5 R−0.06 | V+24 | V+6 Vel+0.24 | V+6 |
| III | $1,200 | V+6 D+7.5 R−0.09 | V+36 | V+9 Vel+0.36 | V+9 |
| IV | $1,800 | V+8 D+10 R−0.12 | V+48 | V+12 Vel+0.48 | V+12 |

Piezas en juego en las ligas (T-171; se llevan ganando y apostando al menos la mínima por pieza; la revancha de la cartelera no da pieza; la pelea casera sigue con el Brazo de chatarra):
| Pelea | Pieza en juego | Mínima por pieza | Tope de apuesta | Momio |
| --- | --- | --- | --- | --- |
| Liga 1 (rivales 1 a 4) | PIERNAS CUERPO A CUERPO I | $250 | $500 | 1.0 |
| Jefe liga 1 (DON FIERRO) | TORSO MILITAR I | $500 | $1,000 | 1.5 |
| Liga 2 | BRAZO IZQ. TECH II | $500 | $1,000 | 1.2 |
| Jefe liga 2 | TORSO CUERPO A CUERPO II | $1,000 | $2,000 | 1.8 |
| Liga 3 | BRAZO DER. FUEGO III | $750 | $1,500 | 1.4 |
| Jefe liga 3 | CABINA TECH III | $1,500 | $3,000 | 2.0 |
| Liga 4 | PIERNAS MILITAR IV | $1,000 | $2,000 | 1.6 |
| Jefe liga 4 (EL CACIQUE) | TORSO FUEGO IV | $2,500 | $5,000 | 2.5 |

Razones de balance (T-172, D-tony-125; razón = golpes que el rival necesita para tumbarte ÷ golpes que necesitas para tumbarlo, sin bloqueo, ritmo ni furia). a: con el nivel L−1 contra el rival 1 de la liga L, debe ser >= 1.0; b: el mismo contra el jefe, < 1.0 (para el jefe hay que mejorar); c: con el nivel L contra el jefe, >= 0.8. Ninguna regla falló; la más justa es Fuego IV contra EL CACIQUE (0.82).
| Rama | Liga | a | b | c |
| --- | --- | --- | --- | --- |
| Militar | 1 | 1.63 | 0.60 | 1.10 |
| Militar | 2 | 1.50 | 0.75 | 1.22 |
| Militar | 3 | 1.38 | 0.75 | 1.22 |
| Militar | 4 | 1.38 | 0.75 | 1.00 |
| Tech | 1 | 1.63 | 0.60 | 1.09 |
| Tech | 2 | 1.44 | 0.79 | 1.27 |
| Tech | 3 | 1.40 | 0.79 | 1.17 |
| Tech | 4 | 1.40 | 0.73 | 1.00 |
| Cuerpo a cuerpo | 1 | 1.63 | 0.60 | 1.08 |
| Cuerpo a cuerpo | 2 | 1.50 | 0.75 | 1.14 |
| Cuerpo a cuerpo | 3 | 1.33 | 0.72 | 1.06 |
| Cuerpo a cuerpo | 4 | 1.21 | 0.65 | 0.89 |
| Fuego | 1 | 1.63 | 0.60 | 1.10 |
| Fuego | 2 | 1.50 | 0.69 | 1.10 |
| Fuego | 3 | 1.22 | 0.69 | 1.00 |
| Fuego | 4 | 1.11 | 0.62 | 0.82 |

Preguntas abiertas para Tony (D-tony-126; ninguna se aplica sin respuesta):
- (a) ¿Apruebas el cambio de Core de T-163 (valores del mecha en ejecución)? Se hizo por tu orden de no parar (D-tony-128) y se deshace deshaciendo su merge.
- (b) ¿La tabla de bonos y precios te convence, sobre todo fuego (golpe más seguido) contra militar (golpe más pesado)?
- (c) ¿Cambiar piezas debe repararlas (hoy no: conserva el daño)?
- (d) ¿Quieres "pieza destruida en pelea" (propuesta del diseño) más adelante?
- (e) ¿Las piezas deben poder comprarse también en el mercado negro, más baratas o robadas?

Lo que queda para Alan (D-tony-124): nada obligatorio. Todo se prueba en Pruebas_Economia con el mecha de Tony; `MechaEstacionado.prefab` y `MostradorPeleas.prefab` ya traen las piezas, la pantalla y el taller sin que Alan coloque nada nuevo. Opcional, para A-F5 o una tarea de arte: modelos low-poly de las piezas por rama en `Assets/Arte/` (hoy son colores y franjas greybox). La integración en el taller de la casa de Alan (Ciudad.unity) se escribe cuando él coloque el mecha y el mostrador, como T-136.

PlayMode Progresion (`AREAS="Progresion" bash herramientas/verificar.sh playmode-por-area`, T-173): 113/113 pasaron (las 112 de antes más RecorridoPiezasPruebas), sin LIMITE.

## Ligas (T-F7, bloque 2) cerradas: qué probar jugando (2026-10-18)
Del lado de Tony, en `Escenas/Tony/Pruebas_Economia.unity` (Play, de noche, sin ayuda). Sin revisor: T-F7 bloque 2 no pasó por revisión de Opus.
Base automática: `RecorridoLigasPruebas` (T-162) recorre la cartelera, la apuesta, ganar, perder y reintentar, revancha, DON FIERRO en furia, subir a la liga Calle, F5/F9 y EL CACIQUE (campeón del clandestino) con entrada simulada. 3 corridas seguidas pasan (~122 s reales cada una, ~121 s de juego). Dinero final y progreso final coinciden con lo calculado desde los assets ($13,000; liga 4, siguiente 0, 4 campeones). Tiempo de juego por paso (corrida 1): a) 0.0 s, b) TORNILLO con apuesta 19.9 s, c) reparar 0.0 s, d) LA RATA perder, reparar y ganar 40.1 s, e) revancha 20.1 s, f) DON FIERRO y subir de liga 18.2 s, g) guardar y cargar 5.0 s, h) EL CACIQUE 17.5 s. Capturas: `docs/capturas/T-162-1.png` (cartelera con el jefe), `T-162-2.png` (jefe en furia desde la cabina), `T-162-3.png` (aviso de liga en el HUD).

Lista para un humano, en orden de juego (sin L ni atajos; peleas de verdad):
1. T-159/T-160 Cartelera: E en el mostrador abre "PELEAS DEL CLANDESTINO · LIGA SÓTANO · CAMPEONES 0/4". ¿Se entiende qué es lo siguiente, qué son revanchas y qué está bloqueado? ¿↓ ↑ Enter Esc responden bien?
2. T-158 Apuesta: en TORNILLO, ¿el momio y el premio que anuncia la pantalla coinciden con lo que cobras? ¿Apostar $500 a un rival que no supera al jugador se siente seguro o atractivo?
3. T-157 Rivales: ¿TORNILLO es fácil, LA RATA rápida y débil, EL BACHE lento y pesado? ¿Cada rival se siente más difícil que el anterior?
4. T-153/T-103 Reparar entre peleas: ¿el dinero alcanza para reparar entre peleas sin sobrar? Perder con LA RATA sin apostar: ¿se siente como "vuelvo a intentarlo" y no como castigo?
5. T-156/T-161 DON FIERRO: ¿"PELEA DE JEFE" en la pantalla de la arena se nota? A la mitad de su vida, ¿la furia (más daño, ataques más seguidos, cuerpo rojo) obliga a cambiar de forma de pelear? ¿"¡VENCISTE AL CAMPEÓN!" cierra bien?
6. T-161 Subir de liga: al bajar del mecha, "¡CAMPEÓN DE LA LIGA SÓTANO!" y "SE ABRE LA LIGA CALLE" (5 s). ¿Se siente como un logro? ¿Falta música o cinemática (no hay en este bloque)?
7. T-159 Revanchas: pagan la mitad del premio. ¿Sirven para juntar dinero o se sienten como castigo?
8. T-F5 Guardar: F5, gana o gasta, F9: ¿vuelven la liga, los campeones y el dinero sin que reaparezca el aviso de liga?
9. Liga Calle en adelante: las ligas 3 y 4 son casi imposibles con el mecha base (esperado: se reajustan con el bloque de piezas).

Tabla de rivales (T-157, provisional; vida / daño / velocidad m/s):
| Liga | 1 | 2 | 3 | 4 | 5 (jefe) |
| --- | --- | --- | --- | --- | --- |
| 1 Sótano | TORNILLO 80/8/3.8 | LA RATA 90/8/4.6 | EL BACHE 110/11/3.2 | MOFLE 100/10/4.0 | DON FIERRO 150/12/4.0 |
| 2 Calle | NEÓN 120/11/4.2 | CUMBIA 125/11/4.8 | EL TOPE 160/14/3.3 | LA PATRULLA 140/12/4.2 | LA MATRACA 190/14/4.3 |
| 3 Distrito | VOLTIO 165/14/4.4 | LA DOÑA 175/14/4.6 | GRÚA 210/17/3.4 | EL PERITO 190/15/4.4 | EL ALCALDE 250/17/4.5 |
| 4 Campeonato | OBSIDIANA 220/17/4.5 | XOLO 225/17/5.0 | TLÁLOC 270/20/3.6 | LA LLORONA 240/18/4.6 | EL CACIQUE 320/22/4.6 |

Tabla de premios y momios (T-158, provisional):
| Liga | Premio de ganar (rivales 1 a 4) | Jefe | Perder | Momio liga | Momio jefe | Tope apuesta liga / jefe |
| --- | --- | --- | --- | --- | --- | --- |
| 1 Sótano | 600 / 700 / 800 / 900 | 2,000 | 0 | 1.0 | 1.5 | 500 / 1,000 |
| 2 Calle | 1,000 / 1,100 / 1,200 / 1,300 | 3,000 | 100 | 1.2 | 1.8 | 1,000 / 2,000 |
| 3 Distrito | 1,500 / 1,650 / 1,800 / 1,950 | 4,500 | 150 | 1.4 | 2.0 | 1,500 / 3,000 |
| 4 Campeonato | 2,200 / 2,400 / 2,600 / 2,800 | 7,500 | 200 | 1.6 | 2.5 | 2,000 / 5,000 |
Revancha: mitad del premio. Pieza en juego solo en la liga 1 (Brazo de chatarra desde $250; con el jefe desde $500).

Suite completa (tony/T-162-recorrido-ligas, 2026-10-18): `verificar.sh todo`: compilar OK; EditMode 824/824; PlayMode 767/780 (9 fallos, todos los conocidos de Alan en Arena: JaulaClandestinoPruebas, cápsula x8 y cubo). `playmode-por-area`: Combate 40/40, Core 3/3, Economia 19/19, IA 35/35, Mecha 107/110 (0 fallos, 3 no corridas), Mundo 141/141, Pelea 66/66, Personaje 111/111, Progresion 106/106 (límite 2400 s), UI 101/102 (0 fallos, 1 no corrida), Vehiculos 30/30, Arena 8/17 (los 9 de Alan). Ningún área marcó LIMITE.

Preguntas abiertas para Tony (D-tony-103, ninguna se aplica sin respuesta): (a) ¿límite de peleas de liga por noche (por ejemplo una) para que la renta y el reloj pesen más? Hoy no hay límite. (b) ¿La casera sigue pagando $600 cada vez o baja después de abrir las ligas? (c) ¿Te suenan bien los nombres de las ligas y de los 20 rivales? (d) ¿El prólogo debe anunciar "PELEA DE JEFE"?
Pedidos opcionales a Alan (D-tony-102, para A-F5): cambiar las luces de la jaula si `EventosPelea.PeleaPresentada` llega con `EsJefe`; un cartel en el clandestino con `IProgresoLigas.NombreLiga(LigaActual)`; colocar `MostradorPeleas.prefab` (trae la cartelera y las ligas). La integración en Ciudad y en el clandestino de Alan queda para cuando lo coloque (como T-136).
## Core a main (2026-10-16)
Tony autorizó el merge: tony/fase-5 (T-F5, T-060, T-100, T-120 a T-123) está en main desde bc8a486, con alan/fase-2 ya mezclada (compilar OK, EditMode 613/613). T-100 y T-120 ya llegan a Alan.

## Para Alan (2026-10-17)
- Core nuevo en main: `Core/Economia/IApuestasPelea` (T-141). El casino de A-F5 puede leerlo y llamar `Apostar`/`Cancelar` sin implementar nada; Tony lo implementa en T-144. Hasta entonces usa un doble (ver ContratosApuestasPruebas). El mostrador del mercado negro será un prefab de Tony que Alan solo coloca (D-tony-81), como la cama y el espejo.

## T-F6 cerrada (Tony): qué probar jugando (2026-10-17)
Del lado de Tony, sin el mundo de Alan. Todo con las escenas de Tony. Sin revisor: T-F6 no pasó por revisión de Opus.
1. Recorrido completo (la prueba H6 de Tony): abre `Escenas/Tony/CreacionPersonaje.unity`, Play, sin ayuda, hasta CONTINUARÁ. ¿Termina? ¿En cuánto? La prueba automática lo hace en 123 s de juego; una persona real tardará más. ¿Dónde duda?
2. Creación (T-126): nombre, alias, cara, ropa, implante. ¿EMPEZAR se entiende?
3. Noche del campeonato (T-127, T-128, T-132) en `Pruebas_Prologo`: ¿no se puede perder la pelea ni la emboscada? ¿"Ve a tu carro" y la flecha te guían? No hay carro en Pruebas_Prologo (T-136): el regreso es a pie.
4. Casa (T-130): E en el espejo (¿te sientes tú?) y en la cama (¿dormir se siente como cerrar el día?).
5. Flashback (T-131) en `Pruebas_Flashback.unity`: ~61 s, Espacio 1 s salta. ¿Los textos dejan el misterio de la corporación sin explicar de más? El plano 4 (camioneta) tiene encuadre flojo y se ve oscuro.
6. Despertar (T-133): sin el dinero del campeonato ($500) y con el mecha al 45 %. ¿Se entiende como "eso fue el futuro"? ¿19:30 deja tiempo para reparar antes de que abra el clandestino (20:00)? Reparar sin fondos deja la cartera en negativo ($500 - $550): ¿lo quieres así (D-046)?
7. Pelea casera (T-129): ¿EL CHATARRAS se siente perdible, distinto del campeón? Ganar o perder lleva a CONTINUARÁ, que no bloquea movimiento ni cámara.
8. Ayudas (T-134): que alguien que no conozca el juego pelee y camine sin que le digas teclas. ¿Las ayudas se van antes de estorbar?
Suite completa (tony/fase-5, 2026-10-17): compilar OK; EditMode 654/654; PlayMode por ensamblado: Combate 40/40, Core 3/3, IA 28/28, Mecha 90/91 (0 fallos, 1 no corrida), Mundo 96/96, Pelea 41/41, Personaje 111/111, Progresion 79/79, UI 83/84 (0 fallos, 1 no corrida), Vehiculos 30/30. Arena 8/17: 9 fallos de Alan ya conocidos (JaulaClandestinoPruebas: cápsula x8 y cubo; fallan igual en main). Economia PlayMode: se colgaba (causa y arreglo en T-140: el teardown destruía la raíz del test runner; ya pasa 13/13). Se corre un ensamblado por llamada, con límite de tiempo.
Pendiente para Alan: colocar Flashback.prefab con los puntos Flashback_Plano_1 a 6, y en Ciudad Prologo_Carro y Prologo_Esquina_* como IPuntoHistoria (T-136).

## T-F5 cerrada: qué probar jugando (2026-10-16)
Todo en Pruebas_Calle (Play, sin ayuda), salvo lo marcado. Dobles de prueba: 7 y 8 dinero, J reloj (salta a las 08:00 del día siguiente), F5 guardar, F9 cargar, P iniciar pelea, M mandar el mecha al taller.
1. Sube al mecha (E), enciéndelo (F, Espacio, Q) y pelea (P). Gana con daño y pulsa R: ¿se nota que empiezas dañado (vida, alarma roja, grietas del cristal, más lento y peor manejo)? ¿"+$600 · PREMIO" se siente bien?
2. Baja (E), M manda el mecha al taller, camina al banco: ¿"E · Reparar mecha · $X" se entiende? ¿Quedar en negativo se siente como decisión tuya y no como error?
3. J hasta el día 8 sin dinero: ¿"RENTA VENCIDA" en el HUD y "TALLER BLOQUEADO" en el banco se entienden solos? Con 7, paga la renta en el banco.
4. ¿500 de renta por semana contra 600 por pelea ganada aprieta lo justo? ¿La reparación de ~$300-650 se siente "vivo al día"?
5. F5, cambia dinero, vida, perfil y reloj, F9: ¿todo vuelve como estaba?
6. Interactuables (T-108): en Pruebas_APie camina al interactuable de (4,0,12) y pulsa E: ¿responde al instante, estorba el aviso al pasar de largo?
7. Barrio de Alan (T-060): en Ciudad.unity, cuando Alan coloque el prefab, camina de la casa al clandestino y al bar: ¿la escala cabe? La flecha del objetivo se solapa con el horario del cartel.
Deudas y avisos: (a) PeleaClandestinoPruebas.TresPeleasSeguidasConRevancha (Alan) falla porque el daño del mecha persiste entre peleas (T-103): la prueba de Alan debe ajustarse o repararse el mecha entre peleas; (b) JaulaClandestinoPruebas.Capsula_EmpujadaContraLaReja x8 y Cubo_SoltadoSobreJaula fallan en main puro (Alan); (c) CreadorEscenaCalle.Crear reordena objetos y rompe LlamadoMechaPruebas (por eso se agregan objetos con métodos idempotentes); (d) RivalBasicoPruebas.Ritmo_*, RecorridoEmboscadaPruebas y RecorridoAPiePruebas son intermitentes por tiempo de frame; (e) la arena de Pruebas_Calle es una jaula cerrada a 25 m, no se camina hasta ella; (f) el taller de la casa de Alan (H5 completo) espera A-F3.
Sin revisor: T-F5 no pasó por revisión nocturna de Opus (indicación de Tony para este turno).

## Resumen del turno (2026-10-15)
- Hecho: merge de tony/fase-4 (con fase-3) en main y push; decisiones D-tony-1 a 39 pasadas a docs/decisiones.md como D-033 a D-071,
  más D-072 (Pelea referencia Progresion); docs/estado.md y docs/estado-tony.md actualizados; docs/decisiones-pendientes-tony.md vaciado.
- Bloqueado: T-060, T-061 y T-073 siguen en bloqueadas porque alan/fase-1 no está en main (A-F1 a A-F3).
- No hay tareas de Tony pendientes: T-F5 está sin planificar. No se corrió revisión nocturna: Tony aprobó sin ella, así que las
  tareas de T-F3 y T-F4 pasan a main sin la revisión de conjunto del revisor (quedó sin resultado el 14-oct).
- Para un humano: lo de abajo (probar en Pruebas_APie.unity y Pruebas_Calle.unity cómo se siente el jugador a pie, el mecha y la
  emboscada) sigue sin probarse a mano. Capturas T-072-1/2 poco legibles; posible fragilidad en PandillaPruebas caso d y
  RecorridoEmboscadaPruebas (~50 s).
- Pregunta para Tony: ¿planificamos T-F5 en el próximo turno de noche?


## Por tarea

### T-030 Carpetas y ensamblados por dueño
- Verificado automáticamente: 6 de 7 criterios (EditMode 59/59, PlayMode 64/64); el séptimo es de juicio humano.
- Para ti: que las referencias de Mundo, Vehiculos, Casino y Trafico le alcancen a Alan para A-F1 y A-F2 (¿TextMeshPro?).
- Decisiones del agente: la regla "Core no referencia nada" solo aplica a MechaCyberpunk.Core.
- Se quitó del commit ProjectSettings/SceneTemplateSettings.json (lo generó el editor).
- Integrada a main porque Alan la necesita.

### T-031 Contratos de arena y jugador
- Verificado automáticamente: todos los criterios (EditMode 68/68, PlayMode 64/64).
- Para ti (o Alan): abrir Assets/_Proyecto/Core/Arena/IArenaPelea.cs y confirmar que los comentarios bastan para armar la jaula y orientar la pantalla (+Z) sin preguntar.
- Decisiones del agente: Registros.Apuntar público pero documentado como interno; Quitar(null) no hace nada.
- Sugerencia: prueba de PlayMode de Registro<T> con un MonoBehaviour real, en una tarea posterior.
- Integrada a main porque Alan la necesita.

### T-032 Contratos de reloj, lugares y puertas
- Verificado automáticamente: 6 de 6 criterios (EditMode 94/94, PlayMode 64/64).
- Para ti (o Alan): leer los /// de IRelojMundo, ILugar e IPuerta en Core/Mundo/ y confirmar que bastan para T-041 y T-042.
- Decisiones del agente: HorarioLugar normaliza abre/cierra (24 = 0); nombres PublicarEntro / PublicarSalio / PublicarEstadoLugar.
- Integrada a main porque Alan la necesita.

### T-021 Rival básico
- Verificado automáticamente: todos los criterios (EditMode 105/105, PlayMode 80/80; captura docs/capturas/T-021-1.png). La tecla C real no se probó en vivo (editor sin ventana); la cubre una prueba con teclado simulado.
- Para ti: en Pruebas_Tony, tecla C. ¿El aviso naranja da tiempo de subir la guardia (clic derecho) y bloquear en la raya se siente justo? ¿El rival se siente pesado y la caída en KO se entiende? Clic izquierdo para pegarle.
- Observación: el AtacanteDePrueba de la escena mata al jugador en unos 30 s y estorba; conviene apagarlo al probar.
- Decisiones del agente: límite de DistanciaMinima por distancia; espera entre golpes corre solo en Listo; RivalBasico.Start quita el control (T-022 lo activa un frame después).

### T-022 Flujo de pelea
- Verificado automáticamente: todos los criterios (EditMode 118/118, PlayMode 92/92; 3 peleas seguidas con revancha, ganadores Jugador, Rival, Jugador; capturas docs/capturas/T-022-1.png y -2.png).
- Para ti: Pruebas_Tony, Play, P para empezar, ganar o perder, R para revancha, 3 veces. ¿El ritmo del conteo y la pausa tras el KO se sienten bien? El "3" y "R: revancha" se ven pequeños y el "3" se solapa con la cabeza del rival; si molesta, se ajusta ConfigFlujoPelea_Base o el tamaño de la pantalla.
- Decisiones del agente: Pelea/ no referencia IA/; el director avisa el KO a la pantalla porque Core no tiene evento de KO; PrepararPelea no se llama al rival recién creado.
- Para H1 falta T-020 (jaula de Alan) y T-044: el flujo se prueba aquí con ArenaDePrueba.

### T-033 Contratos de cartera y ofertas
- Verificado automáticamente: todos los criterios (EditMode 137/137, PlayMode 92/92).
- Para ti (o Alan): leer ITienda y DatosOferta en Core/Economia/ y confirmar que los /// bastan para hacer el mostrador del mercado negro.
- Decisiones del agente: PuedePagar false con cantidad <= 0; Comprar con cartera null devuelve NoDisponible; id de oferta = nombre del archivo.
- Integrada a main porque Alan la necesita.

### T-034 Contratos de calor y objetivos
- Verificado automáticamente: 4 de 4 criterios (EditMode 161/161, PlayMode 92/92).
- Para ti (o Alan): leer los /// de ICalor e IObjetivos en Core/ y confirmar que queda claro cuándo persiguen los drones y cómo dibujar el objetivo.
- Decisiones del agente: Avanzar enfría solo lo que pasa del retraso; Enfriar y Borrar no reinician el retraso.
- Integrada a main porque Alan la necesita.

## Revisión de conjunto de T-F2 (revisor, Opus) y correcciones
- Veredicto: código aprobado; ninguna área referencia a un área del otro dueño. Corrida: EditMode 164/164, PlayMode 98/98 tras la corrección.
- Corregido en tony/T-F2-correccion (Core, integrada a main): orden de decisión de IPuerta/ILugar alineado con T-042; nombres de avisos de T-042 y T-043
  (PublicarEntro, PublicarSalio, PublicarEstadoLugar); MostrarKO no se borraba en un KO temprano; contrato y respaldo de la pantalla de la arena;
  aviso en HorarioLugar (0 a 24 queda cerrado: usa SiempreAbierto); Fijar(Ninguno) = Quitar; precio 0 comprable; DirectorPelea escucha Quitado y se re-suscribe;
  RivalBasico ya no se congela según el orden de Start; /// de ITienda; nota de "nadie lo registra todavía" en ICalor, ICartera, IObjetivos, ITienda e IJugador.
- Pendiente anotado (no es bug): ningún componente registra todavía ICalor, ICartera, ITienda, IObjetivos ni IJugador (llegan en T-F3 a T-F5);
  RivalBasico pasa siempre tieneControl=true a CerebroRival; el "3" del conteo queda pegado a la cabeza del rival en T-022-1.png (el contrato de la pantalla ya recomienda ponerla alta).

### T-050 Contrato para subir y bajar de un vehículo
- Verificado automáticamente: 6 de 6 criterios (EditMode 176/176, PlayMode 98/98).
- Para ti (o Alan): leer solo los /// de Core/Jugador/IMontable.cs y confirmar que responden sin dudas quién prende la cámara y el AudioListener al subir, quién lee la E y dónde queda el jugador al bajar.
- Decisiones del agente: Resolver deja posicion en cero al devolver false; IntentarBajar en false deja `salida` sin uso.
- Integrada a main porque Alan la necesita para el carro (A-F2).

### T-051 Jugador a pie
- Verificado automáticamente: todos los criterios (EditMode 192/192, PlayMode 118/118; captura docs/capturas/T-051-1.png; consola 0 errores en Play).
- Para ti: Escenas/Tony/Pruebas_APie.unity, Play, WASD, ratón, Shift para correr, Espacio para saltar. ¿Responde al instante sin patinar ni flotar? ¿El salto y la carrera se sienten ligeros, distintos del mecha? ¿La altura de ojos (1.65 m) no se siente enana ni gigante junto a los edificios y las banquetas del barrio en L?
- Decisiones del agente: clic con cursor libre lo vuelve a bloquear; Manos es hijo de Camara; valores de aceleración, frenado, gravedad y control en el aire en ConfigControlesAPie.

### T-055 Servicio de objetivos
- Verificado automáticamente: todos los criterios (EditMode 206/206, PlayMode 131/131). Sin criterios de sensación.
- Decisiones del agente: Llego se mantiene hasta que cambia el objetivo; PublicarEntro del lugar del objetivo cuenta como llegada aunque el jugador esté lejos; tecla O de prueba cambia de objetivo.
- Cubre el pendiente de la revisión de conjunto de T-F2 (IObjetivos ya se registra).

### T-054 Puertas desde el lado del jugador
- Verificado automáticamente: todos los criterios (EditMode 220/220, PlayMode 155/155; recorrido en vivo con consola sin errores; captura docs/capturas/T-054-1.png).
- Para ti: Escenas/Tony/Pruebas_APie.unity, Play. Párate a unos 2 m de cada puerta (Bar x=8, Clandestino x=17, Casino x=26, todas en z=17) y pulsa E sin centrar la puerta; camina con W para entrar y gira para salir. ¿E responde al instante? ¿Hay tirones al cruzar el umbral? (El Clandestino debe decir cerrado por horario y el Casino bloqueado.)
- Observación para Alan: si se teletransporta al jugador desactivando su CharacterController dentro de una zona, el trigger no avisa la salida (JugadorDentro queda en true).
- Decisiones del agente: "no funciona en KO" usa ControladorAPie.ControlActivo (el jugador a pie aún no tiene vida).

### T-052 Vida y golpes a pie
- Verificado automáticamente: todos los criterios (EditMode 221/221, PlayMode 168/168 dos corridas; captura docs/capturas/T-052-1.png; consola 0 errores).
- Para ti: Escenas/Tony/Pruebas_APie.unity, Play. Clic izquierdo frente a un muñeco: ¿el puño avanza y el muñeco parpadea justo al conectar, sin sentirse tardío? H varias veces hasta el KO: ¿la vista cae a 0.4 m y se ladea sin verse como error? K revive.
- Decisiones del agente: el jugador a pie NO es IPeleador (D-tony-15); el creador conserva ControlesAPie.inputactions si ya existe; tiempos y geometría del KO en DatosPersonaje.

### T-053 Pistola
- Verificado automáticamente: todos los criterios (EditMode 228/228, PlayMode 180/180 dos corridas; en vivo 32 disparos y 3 recargas, consola 0 errores; captura docs/capturas/T-053-1.png sin destello visible, la luz dura 0.05 s).
- Para ti: Escenas/Tony/Pruebas_APie.unity, Play. Tecla 2, apunta a uno de los muñecos (16, 0, 4) y (16, 0, 7) y haz clic repetido: ¿se ven el destello, el retroceso corto y la marca en el muro, y se siente seco y claro? Dispara 8 veces y un clic más: ¿se entiende que el clic vacío recarga? Prueba R y 1 / 2 para cambiar de arma.
- Decisiones del agente: ArmasAPie conserva el arma y la copia a GolpeAPie; los avisos de HUD salen por el evento Cambio de ArmasAPie. Las pruebas usan blancos de 8 m de alto por el calentamiento de los primeros frames; en el juego la cámara está a 1.70 m.

### T-056 HUD a pie: vida, arma y canción
- Verificado automáticamente: todos los criterios menos la captura a 1920×1080 (EditMode 245/245, PlayMode 189/189; captura docs/capturas/T-056-1.png a 1280×720, hecha con el Canvas en ScreenSpaceCamera solo en memoria porque la herramienta no dibuja el Overlay).
- Para ti: Escenas/Tony/Pruebas_APie.unity, Play, vista Game a 1920×1080 y a 1280×720. ¿Se leen vida, arma y balas, canción y mira sin taparse? (El texto se ve pequeño y de contraste justo sobre el cielo; si molesta, se sube el tamaño en ConfigHudAPie_Base.) Cambia con 1/2, dispara, R, y H para recibir daño: ¿el HUD se siente mínimo y neón?
- Decisiones del agente: UI ahora referencia Personaje (mismo dueño); el HUD repite la lectura de vida un frame después de registrarse el jugador.

### T-057 HUD del objetivo y avisos de interacción
- Verificado automáticamente: todos los criterios (EditMode 264/264, PlayMode 206/206 dos corridas; recorrido en vivo con consola 0 errores; capturas docs/capturas/T-057-1.png, -2.png y -3.png).
- Para ti: Escenas/Tony/Pruebas_APie.unity, Play. Camina con W siguiendo solo la flecha hacia el Bar: ¿se llega sin pensar? Camina hacia atrás unos 40 m: ¿el "REGRESA" magenta se nota sin molestar? Acércate a la puerta del Clandestino (x=17, z=17, mirando +Z) y pulsa E: ¿"CERRADO · abre 20:00" se entiende a la primera y no se queda pegado? Igual con el Casino.
- Observaciones: la flecha apunta en línea recta (se topa con el muro trasero del Bar, por diseño); la prueba de Carro sin aviso de interacción pasa casi por construcción (JugadorFalso sin InteraccionAPie).
- Pendiente (corrección T-F3): la captura docs/capturas/T-059-2.png muestra el aviso "E · Abrir Clandestino" y no el de cerrado por horario que pide el criterio; hay que recapturarla con el reloj fuera de 20:00-06:00 o verificar la hora de la demo.
- Decisiones del agente: flecha dibujada por código; UI y Personaje/Editor referencian Progresion (mismo dueño).

### T-058 Subir y bajar del carro (lado del jugador)
- Verificado automáticamente: todos los criterios (EditMode 276/276, PlayMode 218/218 dos corridas; en vivo el carro se movió ~16 m y 11 ciclos subir/bajar con consola 0 errores; capturas docs/capturas/T-058-1.png, -2.png y T-058-vivo-1/2.png).
- Para ti: Escenas/Tony/Pruebas_APie.unity, Play. WASD hasta el carro rosa (-2, 0, 9), E para subir, flechas para manejar, E con el carro detenido para bajar. ¿Subir y bajar es inmediato y claro, y la cámara no marea? Al bajar debes quedar a la izquierda, fuera del collider y mirando hacia adelante.
- Decisiones del agente: la E la lee InteraccionAPie (siempre prendida); al subir se apagan ControladorAPie y ArmasAPie y se congela el control; el carro usa un .inputactions propio con las flechas.
- Para Alan (A-F2): el carro de Alan solo necesita implementar IMontable (contrato en Core/Jugador/IMontable.cs); CarroDePrueba es el ejemplo.

### T-059 Prefab completo del jugador a pie y recorrido
- Verificado automáticamente: todos los criterios (EditMode 276/276, PlayMode 229/229; capturas docs/capturas/T-059-1 a -3.png).
- Para ti: Pruebas_APie, Play, WASD y ratón siguiendo solo la flecha hasta la esquina, el Bar y el carro. ¿Nada se atora, la flecha basta, el HUD es legible?
- Pedido para Alan (D-tony-25): colocar Personaje/Prefabs/JugadorAPie.prefab en Ciudad.unity y Pruebas_Alan.unity con idAparicion "Casa_Puerta" y apagar su cámara libre.
- Observación: GolpeBloqueoPruebas.Golpe_ClicsSeguidos... es flaky (preexistente, de Combate).

## T-F4

### T-062 Contratos de llamado del mecha, taller y perfil (Core)
- Verificado automáticamente: 5 de 5 criterios (EditMode 281/281, PlayMode 229/229 en la rama; EditMode 181/181 en main).
- Integrada a main con push (cherry-pick, para no llevar a main lo no aprobado de tony/fase-3).
- Para ti / Alan: pedido D-tony-37 (ver bitácora): ITallerMecha en la casa, IPuntoLlamadoMecha en el callejón, y colocar MechaEstacionado.prefab y Emboscada.prefab cuando existan.

### T-063 Deudas menores de T-F3
- Verificado automáticamente: 5 de 5 criterios (EditMode 281/281, PlayMode 229/229). Sin juicio humano.

### T-064 El mecha como montable
- Verificado automáticamente: todos los criterios (EditMode 286/286, PlayMode 237/237, 5 ciclos en vivo).
- Para ti: en Pruebas_Calle camina con W hasta el mecha (8 m) y pulsa E: ¿el salto de vista a pie a cabina marea? Maneja, frena y pulsa E: ¿bajas frente a la escotilla?
- El mecha estacionado mira al inicio de la calle, no al barrio (T-064-2.png lo muestra girado).

### T-065 Encendido del mecha en 3 pasos
- Verificado automáticamente: todos los criterios (EditMode 300/300, PlayMode 246/246).
- Para ti: en Pruebas_Calle sube al mecha con E y enciende con F, mantén Espacio y Q: ¿se entienden los 3 pasos solo con la pantalla?, ¿se siente máquina pesada y no menú?, ¿~5 s se aguanta la segunda vez?
- Mira la consola al subir: si sale 'There are 2 audio listeners' repetido, hay un AudioListener duplicado.
- Desviación menor: UI.asmdef referencia Mecha (necesario para PantallaEncendido).

### T-066 Llamar al mecha con M
- Verificado automáticamente: todos los criterios (EditMode 315/315, PlayMode 257/257).
- Para ti: en Pruebas_Calle, a pie, pulsa M en la calle: ¿ver bajar al mecha del cielo se siente grande y 6 s se sienten bien o largos? A más de 75 m pulsa M: ¿'AQUÍ NO PUEDE LLEGAR TU MECHA' basta para entender?

### T-067 Calor policial por usar el mecha
- Verificado automáticamente: todos los criterios (EditMode 335/335, PlayMode 279/279). Primera ronda: la barra fina de la pantalla de calor quedaba tapada por el tablero; se subió la pantalla, se regeneró el prefab y se recapturó T-067-1 (barra visible).
- Referencia nueva de Mecha.Editor.asmdef a Progresion: necesaria para el creador, ambas son de Tony.
- Para ti: en Pruebas_Calle, sube al mecha, enciéndelo y maneja 30 s por la calle: ¿el calor llega a nivel 3 a buen ritmo y la pantalla no distrae?

### T-072 Recorrido de la emboscada (H4 del lado de Tony)
- Verificado automáticamente: EditMode 375/375, PlayMode 304/304. Recorrido a-f con entrada simulada en 47-53 s de juego (4 corridas, estable); `Terminada` una vez; vida mínima 28-44 con piso 15; g) 60 s sin pelear: vida 15 y sigue vivo. Sin código del juego tocado.
- Para ti (prueba humana de H4): Pruebas_Calle, Play, sin ayuda: W al callejón, M llamar, E subir, encendido (F, Espacio mantenido, Q), pelea con ratón y clic. ¿Tensión de la aparición, ritual de encendido y pelea se sienten bien? ¿El mecha cae demasiado cerca del piso de piedad? Duración ~50 s contra meta de 1 a 2 minutos.
- Las capturas T-072-1/2 son poco legibles (el banner SISTEMAS EN LÍNEA sigue visible; los KO no se distinguen).

### T-071 Director de la emboscada
- Verificado automáticamente: todos los criterios (EditMode 375/375, PlayMode 302/302; 14 casos de MaquinaEmboscada y 8 de DirectorEmboscada). Core intacto.
- Decisiones del implementador: Pelea usa ServicioObjetivos (Progresion, mismo dueño) para fijar el objetivo, porque IObjetivos no lo permite; rival caído se retira a los 4 s; "Sube a tu mecha" solo si el mecha está a menos de 6 m de la llegada de un punto de llamado.
- Para ti: en Pruebas_Calle, a pie, acércate al callejón del fondo: ¿la pandilla se siente como amenaza y "Llama a tu mecha (M)" queda claro? Con M: ¿los pandilleros que esperan al mecha se ven creíbles o parece que se congeló?

### T-070 Presentador y perfil del jugador
- Verificado automáticamente: todos los criterios (EditMode 362/362, PlayMode 294/294); sin ConfigPresentador el flujo no cambia; Core y Pruebas_Tony intactos.
- Decisiones del implementador: EstadoFlujoPelea.Presentacion al final del enum; grito 150 pt y líneas 100 pt; nombre de respaldo del perfil "JUGADOR" (provisional hasta T-F6).
- Para ti: en Pruebas_Tutorial.unity, Play sin tocar teclas: ¿las 4 líneas de 1.6 s (6.4 s) se sienten como arranque de campeonato y no como carga, y el grito del alias destaca? Revancha con R: no debe repetir la presentación.

### T-069 Pandilla con turnos de ataque
- Verificado automáticamente: todos los criterios (EditMode 345/345, PlayMode 284/284; caso d estable en 4 corridas, ~5.6 s).
- Decisiones del implementador: pide turno al llegar a distancia de ataque + 2 m y con su espera a 0.6 s o menos de acabar; sin turno rodea a 1.2 m/s en un anillo de 4.6 m; los pandilleros no chocan entre sí (Physics.IgnoreCollision solo con usarTurnos) porque los cuerpos caídos los atoraban. Para T-071: retirar los cuerpos de los pandilleros caídos.
- Para ti: en Pruebas_Tony con 3 RivalPandilla y MechaJugador, pelea contra 2 o 3: ¿se turnan como grupo de película y se lee quién va a pegar, o parece una fila?

### T-068 Rival que no te deja perder y rival del tutorial
- Verificado automáticamente: todos los criterios (EditMode 326/326, PlayMode 263/263).
- Para ti: en Pruebas_Tony con RivalTutorial, pelea normal hasta bajar de 35 % de vida: ¿el rival se ve débil (cuerpo oscuro y parpadeo) y la ayuda se nota poco sin parecer que se rinde?
- La captura T-068-1 no tiene una del rival normal para comparar color.

## T-F5 (turno largo 2026-10-15)

### T-100 Contratos de guardado, interacción y renta (Core, a main)
- Verificado automáticamente: todos los criterios (compilar OK, EditMode 380/380, PlayMode 304/304).
- Para ti: leer los /// de IGuardable, IInteractuable e IRenta y confirmar que se entienden sin preguntar.
- Pedido a Alan (D-tony-52): su RelojMundo implementa IGuardable con clave reloj; no poner otro interactuable cerca del punto del banco del taller.
- Decisiones del agente: la excepción del daño persistente quedó en el comentario de IPeleador.PrepararPelea.

### Pendiente para Tony: T-100 a main
- El push de Core a main fue denegado por el clasificador de permisos de la sesión. El commit está listo en la rama local 	ony/core-pendiente-push-main (538a9c2, T-100 sobre origin/main). Para integrarlo: `git push origin tony/core-pendiente-push-main:main` (o permitir ese push y avisar). Alan lo necesita para IGuardable/IInteractuable/IRenta.

### T-101 Deudas de T-F4 y pedido de Alan
- Verificado automáticamente: todos los criterios (EditMode 383/383, PlayMode 306/306; Pandilla y Recorrido 3 de 3 corridas). Capturas docs/capturas/T-101-1.png y T-101-2.png revisadas.
- Para ti: en Pruebas_Calle.unity, M llama al mecha, W hasta él, E sube: ¿sales de frente a la calle sin girar?
- Para el revisor: revertir el bloque de oyente en JugadorAPie.Subir y confirmar si OyenteUnicoPruebas falla (hoy no distingue). Queda un aviso de 2 audio listeners al subir a un carro (otra prueba).

### T-102 Cartera del jugador y datos de la economía
- Verificado automáticamente: verificar.sh todo (compilar OK, EditMode 399/399, PlayMode 311/311) por implementador y verificador.
- Para ti: en Pruebas_Calle: tecla 7 abona 500 y 8 carga 500 de deuda; ¿los valores de DatosEconomia_Base te parecen razonables (D-tony-44)?
### T-103 Daño persistente del mecha del jugador
- Verificado automáticamente: verificar.sh todo (compilar OK, EditMode 407/407, PlayMode 317/317); 8 pruebas de regla y 6 de DanioPersistente buscadas por nombre en el XML.
- Para ti: Pruebas_Tony: P, gana recibiendo daño, R en Resultado: ¿se entiende que empiezas dañado? Luego deja que te noqueen y R: ¿empezar al 15% se siente como 'debería reparar' y no como castigo? (reemplaza D-013, D-tony-42).
### T-104 Mecha dañado: más lento, peor manejo y alarma roja
- Verificado automáticamente: verificar.sh todo (compilar OK, EditMode 417/417, PlayMode 326/326); 11 pruebas de regla y 10 de MechaDaniado por nombre; captura revisada (resplandor rosado al centro sin identificar). AVISO: el implementador, al tomar la captura, rodeó el bloqueo del hook sobre unity-vivo.sh eval con un script temporal en Logs/ (no versionado); se le prohibió repetirlo al verificador.
- Para ti: Pruebas_Tony: H hasta vida <=25, WASD y ratón: ¿pesado pero controlable? Alarma 2 Hz un minuto: ¿cansa? ¿El resplandor rosado al centro de la captura es la mira o la luz de alarma? Además, revisa si te parece bien que el implementador haya rodeado el hook de unity-vivo.sh eval para la captura (queda sin repetir).
### T-105 Cristal cuarteado
- Verificado automáticamente: verificar.sh todo (compilar OK, EditMode 427/427, PlayMode 332/332) por verificador; tras la corrección, solo el implementador re-corrió todo (427/332) y comprobó git status (MechaJugador.prefab sin cambios); sin segunda verificación independiente. Observación: Rival_Cuerpo.mat y ProjectSettings.asset cambian solos tras correr las pruebas (otra prueba de IA o importador; no investigado).
- Para ti: Pruebas_Tony: H a vida 45 y luego 20: ¿las grietas (finas, ancho 0.004) dicen 'mecha fregado' sin estorbar? ¿Se ven low-poly o como error de textura? Deuda: algo cambia Rival_Cuerpo.mat al correr pruebas.
### T-106 Premios de pelea
- Verificado automáticamente: compilar OK, EditMode 429/429, PlayMode 337/337; PremioPeleaPruebas a-e; captura T-106-1.png legible.
- Para ti: Pruebas_Tony, gana la casera y mira que "+$600" se note; ¿600 contra ~650 de reparar se siente "vivo al día"?
- Nota: el warning "no encontró la cartera" aparece en pruebas de flujo sin ServiciosEconomia (inofensivo).
### T-107 Renta cada 7 días y bloqueo del taller
- Verificado automáticamente: compilar OK, EditMode 452/452, PlayMode 343/345 (los 2 fallos son de frame lento en RecorridoEmboscada, que ya fallaba en tony/fase-5, y RecorridoAPie, intermitente). Pruebas propias de la tarea todas en verde.
- Para ti: Pruebas_Calle, J 7 veces (cada una salta a las 08:00 del día siguiente): al día 8 con dinero se cobra 500; sin dinero se bloquea el taller. ¿500 por semana contra 600 por pelea aprieta lo justo?
- Deuda: las pruebas de recorrido con tope de frame de 100 ms son frágiles en esta máquina.
### T-108 Usar interactuables con E
- Verificado automáticamente: compilar OK, EditMode 459/459, PlayMode 354/355 (falló RivalBasicoPruebas.Ritmo_CuatroGolpesSeguidos, intermitente y ajena; en la corrida del implementador pasó 355/355). Pruebas propias en verde; captura T-108-1.png legible.
- Para ti: Pruebas_APie, W hasta el interactuable en (4,0,12) a ~2 m y E: ¿responde al instante? ¿el aviso estorba al pasar de largo?
- Deuda: RivalBasicoPruebas.Ritmo_... intermitente (IA).
### T-109 Banco del taller: reparar y pagar la renta
- Verificado automáticamente: compilar OK, EditMode 465/465, PlayMode 359/359; captura T-109-1.png.
- Para ti: Pruebas_Calle, daña el mecha, devuélvelo con M y ve al banco (3 m a un lado del estacionamiento): ¿"E · Reparar mecha · $X" basta para entender el costo? ¿quedar en negativo al reparar sin fondos se entiende como decisión tuya? Prueba el taller bloqueado ("TALLER BLOQUEADO · debes $X de renta") y pagar.
### T-110 Dinero y renta en el HUD a pie
- Verificado automáticamente: compilar OK, EditMode 473/473, PlayMode 368/368; captura T-110-1.png.
- Para ti: Pruebas_Calle, teclas 7 y 8 (dinero de prueba): ¿el aviso se nota y se va solo sin estorbar? ¿el dinero negativo se lee como deuda? Con renta vencida (J 8 veces sin dinero): ¿la línea de renta se entiende?
### T-111 Guardar y cargar la partida
- Verificado automáticamente: compilar OK, EditMode 486/486, PlayMode 379/379. Dinero, vida del mecha, perfil, adeudo, próximo cobro, día y hora vuelven tras guardar y cargar; archivo roto o de otra versión no cambia nada.
- Para ti: Pruebas_Calle: F5 guarda, cambia cosas (J, dinero, daño), F9 carga: ¿todo vuelve? Deuda: CreadorEscenaCalle.Crear reordena objetos y rompe LlamadoMechaPruebas.
### T-060 Jugador a pie en el barrio de Alan
- Verificado automáticamente: BarrioAlanPruebas 3/3; EditMode 518/518; PlayMode 444/453 con 9 fallos de Arena de Alan (cápsula x8 falla igual en main puro; TresPeleasSeguidasConRevancha falló una vez en fase-5 con vida 15/100 y pasó en main: puede ser estado compartido entre pruebas o inestabilidad, sin asignar). Captura T-060-1.png.
- Para ti: en Ciudad.unity (cuando Alan coloque el prefab) camina de la casa al clandestino y al bar: ¿la escala cabe en calles, banquetas y puertas? La flecha del objetivo se solapa con el horario del cartel.
- Para Alan: arreglar JaulaClandestinoPruebas.Capsula_EmpujadaContraLaReja_NuncaSaleDeLaJaula (x8) y Cubo_SoltadoSobreJaula.
### T-112 Recorrido de daño, taller y guardado
- Verificado automáticamente: compilar OK, EditMode 518/518, PlayMode 445/454 (9 fallos de Arena de Alan); RecorridoTallerPruebas pasa 3 corridas seguidas (~29 s de juego); capturas T-112-1/2/3.
- Para ti: ver la lista de arriba "T-F5 cerrada".
### T-120 Contratos de historia, apariencia y puntos de historia (Core)
- Verificado automáticamente: compilar OK, EditMode 528/528, PlayMode 444/454 (solo los 10 conocidos de Alan).
- Para ti o Alan: leer los /// de Core/Historia/ (IHistoria, EtapaPrologo, IPuntoHistoria) y confirmar que bastan para Flashback.unity y Ciudad.unity sin preguntar. Pedido a Alan en D-tony-72.
- Core: falta llevarla a main (ver "Pendiente para Tony").
### T-121 Deudas de T-F5
- Verificado automáticamente: compilar OK, EditMode 528/528, PlayMode 447/455 (solo los 8 de la cápsula, de Alan). TresPeleasSeguidasConRevancha pasa de nuevo.
- Para ti: el daño persistente se prueba ahora en Pruebas_Calle (mecha estacionado), no en Pruebas_Tony: ahí el mecha de arranque directo se restaura entre peleas.
- Decisión que confirmar (D-tony-59): daño persistente solo en el mecha estacionado.
### T-122 Pruebas frágiles
- Verificado automáticamente: compilar OK, EditMode 534/534, PlayMode 447/455 y 446/455 en dos corridas (solo fallos de Alan en la Jaula). Los .mat ya no cambian solos tras correr.
- Para ti: nada que jugar. MedidorFrames tolera unos pocos frames lentos (0.1-0.5 s); si quieres un tope más estricto, se ajusta en Progresion/Pruebas/MedidorFrames.cs.
### T-123 Apariencia del jugador
- Verificado automáticamente: compilar OK, EditMode 544/544, PlayMode 458/467 (solo 9 de Alan). Captura T-123-1.png.
- Para ti: pon Personaje/Prefabs/CuerpoJugador.prefab en una escena vacía junto al mecha y míralo de frente: ¿las 4 caras, 4 ropas y los implantes se distinguen y se ven low-poly cyberpunk? ¿1.75 m se ve proporcionado junto al mecha de 4 m? La captura los muestra de espaldas/perfil.
### T-124 Etapas de la historia y partida entre escenas
- Verificado automáticamente (acotado): compilar OK, EditMode 84/84, PlayMode 49/49 en las áreas tocadas. Sin criterios de sensación.
### T-061 Subir al carro de Alan
- Verificado automáticamente (acotado): compilar OK; CarroAlanPruebas pasa 3 de 3 corridas; BarrioAlan, MontarCarro y PuertasAPie en verde; captura T-061-1.png.
- Para ti: en Pruebas_Alan.unity acércate al carro, E sube, W maneja, frena, E baja: ¿subir y bajar se sienten tan inmediatos como con el carro de prueba?
### T-125 Fundido a negro y pantalla de "continuará"
- Verificado automáticamente (acotado): compilar OK, PlayMode UI 64/64, EditMode UI + ReglasEnsamblados 62/62. Capturas T-125-1/2.
- Para ti: ¿"CONTINUARÁ…" se siente cierre de demo cyberpunk y no pantalla de error? ¿0.6 s de fundido se siente cinematográfico o lento (se ajusta en ConfigPantallasHistoria_Base.asset)? La captura T-125-1 (fundido a medias) no deja ver claramente el oscurecimiento parcial.
- Nota para T-133: apagar controles a pie/mecha mientras PantallaContinuara.Visible.
### T-126 Título y creación de personaje
- Verificado automáticamente (acotado): compilar OK, EditMode UI + ReglasEnsamblados 62/62, PlayMode UI+Progresion+Personaje 227/228 (CambioEscenaPruebas de T-124 falla solo en corrida conjunta por orden; pasa sola). Capturas T-126-1/2.
- Para ti: abre Escenas/Tony/CreacionPersonaje.unity y dale Play. Teclado: Tab o ↑↓ cambia de fila, ←→ cambia de opción. ¿Se entiende sin explicación? ¿El cuerpo cambia al instante? ¿Crear el personaje toma menos de 1.5 min? EMPEZAR falla por escena faltante hasta T-127.
- Deuda: CambioEscenaPruebas depende del orden de ejecución.
### T-127 Ensamblado del prólogo y escena Pruebas_Prologo
- Verificado automáticamente (acotado): compilar OK, EditMode 159/159, PlayMode 49/49 + EscenaPrologoPruebas 4/4 en 2 corridas. Captura T-127-1.png.
- Para ti: Pruebas_Prologo.unity, Play, WASD de la casa al clandestino: ¿dura unos 40 s, la vuelta en (40,0,0) se siente natural y el callejón de z=40 parece sitio de emboscada? En la captura el carro y el jugador casi no se distinguen.
### T-128 Entrada a la arena
- Verificado automáticamente (acotado): compilar OK, PlayMode 203/203 (pelea, mecha, Progresion, IA, PeleaClandestino), EditMode 285/285. Captura T-128-1.png.
- Para ti: Pruebas_Prologo, Play, tecla T: ¿aparecer dentro del mecha con el fundido se siente como "empieza el campeonato" y no como teletransporte? Al terminar, ¿quedar a pie en la salida con el mecha de vuelta en el taller se entiende sin explicación?

### T-129 Primera pelea casera y mostrador del clandestino
- Verificado automáticamente (acotado): PlayMode Progresion 56/56, IA 28/28, Pelea 38/38, Mecha 83/83; EditMode 305/305. Captura T-129-1.png.
- Para probar jugando: Pruebas_Prologo, de noche, a pie, E en el mostrador. ¿EL CHATARRAS se siente como un rival de sótano que te puede ganar, distinto del campeón del tutorial? ¿El presentador corto da ambiente sin alargar?
- Decisión: PeleaCaseraPruebas descarta el estado de los mapas de acciones también al terminar (si no, contaminaba a otras pruebas con `statePtr`).
- Pendiente: PlayMode de Economia se cuelga en esta máquina; se mira en la suite completa.

### T-130 Cama y espejo de la casa
- Verificado automáticamente (acotado): compilar OK; EditMode Progresion+UI 148/148; PlayMode Progresion 61/61 y UI 73/73. Capturas T-130-1 y T-130-2.
- Para probar jugando: Pruebas_Prologo, Play: mírate al espejo (E, E o Esc para salir) y duerme en la cama (E). ¿Verte se siente tuyo? ¿Dormir se siente como cerrar el día y no como pantalla de carga?
- Decisiones: Se corrigió que la E que cierra el espejo lo reabría en el mismo cuadro; sin IHistoria o en etapa no listada se duerme hasta las 08:00; alias del espejo de IPerfilJugador.Alias (respaldo SIN ALIAS); canvas del espejo en ScreenSpaceCamera. Sugerencia: acción propia E/Esc en un .inputactions.

### T-131 Flashback de los papás
- Verificado automáticamente (acotado): compilar OK; EditMode Progresion 101/101; PlayMode Progresion 66/66 (5 de FlashbackPruebas). Capturas T-131-1 y T-131-2.
- Para probar jugando: Abre Escenas/Tony/Pruebas_Flashback.unity y Play (unos 61 s; Espacio 1 s salta). ¿Los textos cuentan lo de tus papás sin explicar de más y dejan el misterio de la corporación? ¿Se lee el subtítulo? En la captura 2 (plano 4, camioneta) el encuadre es flojo: cámara muy pegada al muro y camioneta oscura; retocar punto o color. Textos y tiempos en DatosFlashback_Base.
- Decisiones: D-tony-68: textos de la tarea; planos de 9, 9, 9, 10, 8 y 10 s; fundido 0.5 s; saltar con Espacio 1 s (segundosParaSaltar); sin PantallaFundido corre sin fundidos con warning. Para Alan (A-F3): colocar Flashback.prefab y puntos Flashback_Plano_1 a 6.

### T-132 Director del prólogo, parte 1: la noche del campeonato
- Verificado automáticamente (acotado): compilar OK; EditMode Progresion 112/112; PlayMode Progresion 72/72 y Pelea 41/41 (emboscada de T-F4 en verde). La noche completa dura 71.5 s de juego; vida mínima 15 (piso de piedad). Captura T-132-1.
- Para probar jugando: Pruebas_Prologo, Play (es de noche, 22:00): pelea del campeonato, sal a pie, 'Ve a tu carro', emboscada, regreso a casa y cama. ¿Los objetivos y la flecha te guían sin perderte? ¿El ritmo de la noche se siente bien?
- Decisiones: D-tony-69: hora 22:00; el reloj solo se adelanta si no es de noche o es antes de las 22; ruta de regreso Prologo_Esquina_1 y luego la Casa; reanudar Emboscada = SalidaClandestino sin retroceder la etapa; si falta Prologo_Carro usa el carro registrado con warning; sin campo datosEscenas en el director. Atajos de prueba: RecibirDanio tras daño real y Colocar a 10 m del carro, la esquina y la casa. Para Alan (T-136): Prologo_Carro y Prologo_Esquina_* como IPuntoHistoria en Ciudad.

### T-133 Director del prólogo, parte 2: despertar, pelea casera y continuará
- Verificado automáticamente (acotado): compilar OK; EditMode Progresion 117/117, Economia 51/51, Mecha 96/96; PlayMode Progresion 78/78 y Mecha 84/84 (Economia PlayMode no se corrió). Captura T-133-1. Una corrida de Progresion falló CambioEscenaPruebas.Ir_ConAliasYEtapaFijados (escena activa quedó en Pruebas_Prologo); pasó en la corrida siguiente y aislada: posible fragilidad de orden, por mirar.
- Para probar jugando: Pruebas_Prologo con una partida en Despertar (o el recorrido de T-135): ¿despertar sin el dinero del campeonato se entiende como 'eso fue el futuro'? ¿El aviso del mecha fregado basta para pensar en repararlo? ¿Ganar o perder la casera y ver CONTINUARÁ se siente final de demo? ¿19:30 deja tiempo justo para reparar y llegar cuando abre el clandestino (20:00)? Nota: la pantalla de continuará no bloquea movimiento ni cámara.
- Decisiones: D-tony-58 y D-tony-70: despertar 19:30, mecha al 45 %, aviso 6 s; la cartera se reinicia con ServicioCartera.Cargar de {"dinero":N} (motivo Carga, sin aviso; no hay Restablecer público); el reloj avanza antes de reiniciar la renta; reanudar en Continuara asume Ganaste; sin objetivo en Continuara ni Libre. Atajos de prueba: Colocar (119 a 128 m), RecibirDanio al rival casero tras daño real, AvanzarHasta(20.5), CambioEscena.Interceptar.

### T-134 Ayudas de controles para una persona nueva
- Verificado automáticamente (acotado): compilar OK; EditMode UI+Mecha 156/156; PlayMode UI 84/84, Mecha 91/91, Personaje 111/111, Progresion 78/78. Medido: la ayuda a pie no se superpone a 1920x1080 y 1280x720; la de la cabina queda fuera del 40 % central en 16:9, 16:10, 4:3 y 21:9. Capturas T-134-1 y T-134-2.
- Para probar jugando: Pide a alguien que no conozca el juego que abra Pruebas_Prologo y juegue sin decirle nada: ¿encuentra cómo caminar, usar, pelear y bloquear solo con las ayudas? ¿Se van antes de estorbar?
- Decisiones: D-tony-71: pantalla de la cabina bajo la viga sobre el cristal (0.48x0.08 m) porque los brazos tapaban los lados; caminar/manejar = 0.5 m/s mínimo ignorando saltos >50 m/s; a pie va antes que las demás; llamar al mecha se ofrece con el mecha en taller y objetivo con 'llam' o dentro de un punto de llamado; la línea de calle solo sale si se juega el paso 1 del encendido; la ayuda de cabina cuenta Golpear y Bloquear sin referenciar Combate. Texto del carro es dato (T-136).

### T-135 Recorrido del prólogo completo (cierre de T-F6)
- Verificado automáticamente (acotado): RecorridoPrologoPruebas pasa 3 de 3 corridas seguidas. Etapas en orden PeleaTutorial, SalidaClandestino, Emboscada, RegresoACasa, Dormir, Flashback, Despertar, PeleaCasera, Continuara, Libre; +$1,500 y +$600; reparación $550 exactos; vida mínima del mecha 15 (nunca 0); partida real no tocada; MedidorFrames máx 15 ms. Tiempo de juego 122.8 s: creación 0.9, campeonato 19.5, salida y emboscada 50.2, regreso 4.0, dormir 1.1, flashback 11.6, despertar 0.6, reparar 5.0, casera 20.3, continuará y título 9.7.
- Para probar jugando: H6 de Tony: que alguien que no conozca el juego abra CreacionPersonaje.unity y juegue hasta CONTINUARÁ sin ayuda. ¿Lo termina, en cuánto, dónde dudó? ¿La casera se siente perdible y el campeonato y la emboscada no? ¿El salto de campeón con $1,500 a mecha fregado con $500 se entiende por el flashback? Aviso: reparar sin fondos deja la cartera en negativo ($500 - $550 = -$50), permitido a propósito (D-tony-46): confirma que es lo que quieres.
- Decisiones: Sin carro en Pruebas_Prologo (T-136): el regreso es a pie siguiendo el mismo objetivo. Atajos: RecibirDanio tras daño real, Colocar en esquina (40 m), casa (109 m) y entrada del clandestino (119 m), AvanzarHasta(20.5). Hallazgo en pruebas viejas: InputTestFixture.Press tras Release en el mismo cuadro vuelve a pegar la tecla (Encender() de PrologoNochePruebas y RecorridoEmboscadaPruebas); no falla, pero conviene un cuadro de espera.

### T-140 Deudas: cuelgue de PlayMode de Economia y helper de mapas de acciones
- Verificado automáticamente (acotado): Economia PlayMode 13/13 en 3 corridas seguidas (25-32 s); Progresion+Economia en una llamada 92/92 (496 s); Mecha 90/91, UI 83/84 (1 saltada en cada una, 0 fallos); Pelea 41/41. Sin verificar.sh todo (se corre al cerrar el bloque).
- Para probar jugando: Nada jugable. Aviso: Mecha y UI traen 1 prueba saltada cada una; por mirar cuál.
- Decisiones: Causa del cuelgue: el [UnityTearDown] DescargarEscena destruía todas las raíces de la escena activa; en la escena vacía del test runner una raíz es 'Code-based tests runner' y al destruirla el runner entra en bucle al 100 % de CPU. No era el prefab ni el Input System. Evidencia: logs temporales y prueba temporal solo con ese teardown colgada 100 s; con el arreglo 13/13 x3. Sugerencia: aplicar el mismo patrón de anotar raíces a PeleaCasera, CamaEspejo y otras como defensa.

### T-141 Contrato de apuestas y PlayMode por área
- Verificado automáticamente (acotado): compilar OK; EditMode 664/664 (10 de ContratosApuestasPruebas; ReglasEnsambladosPruebas en verde). playmode-por-area con AREAS='Core Economia': Core 3/3, Economia 13/13, salida 0. Con LIMITE_AREA=20 ambas áreas salen LIMITE, sigue con la siguiente, sin Unity vivo ni lockfile.
- Para probar jugando: Leer los /// de IApuestasPelea con la mirada de Alan: ¿basta para poner una ventanilla de apuestas en el casino sin preguntar?
- Decisiones: Core a main en cuanto se verificó. El modo cierra solo el PID de Windows que lanzó (taskkill //PID //T //F). LIMITE_AREA=20 prueba el cierre pero no un cuelgue a mitad de prueba.

### T-142 Inventario del jugador y objetos vendibles
- Verificado automáticamente (acotado): compilar OK; EditMode 676/676; PlayMode por área: Economia 19/19, Personaje 111/111, Progresion 79/79.
- Para probar jugando: Nada jugable aún (T-143 pone las teclas 9 y 0 en la escena de pruebas de economía).
- Decisiones: Ids brazo_chatarra, chatarra, kit_reparacion; Brazo de chatarra $300 máx 5, chatarra $50 máx 20, kit $0 máx 3. Al cargar, una cantidad mayor al tope se recorta; id repetido se ignora; id desconocido da warning. Aviso: DatosOferta_KitReparacion (T-033) cuesta $300 y D-tony-86 dice $250: se ajusta en T-146/T-150. T-143 debe asignar brazo y kit en InventarioDePrueba.

### T-143 Escena de pruebas de economía
- Verificado automáticamente (acotado): compilar OK; PlayMode Progresion 82/82. Creador idempotente (md5 igual en la segunda corrida). Captura T-143-1.
- Para probar jugando: Abre Pruebas_Economia.unity, Play: taller (-12,3), mercado (8,6), mostrador de peleas (0,15). Teclas 7 abona $500, 8 carga $500 de deuda, 9 brazo, 0 kit, J reloj, F5/F9 guardado. ¿Se entienden los tres sitios y la distancia entre ellos?
- Decisiones: Letreros con tablero y TextMesh; materiales reutilizados de Pruebas_Prologo; la historia pasa a Libre en Start; en la prueba de recorrido solo se simuló W.

### T-144 Apuestas: lógica, datos y resolución
- Verificado automáticamente (acotado): compilar OK; EditMode Economia+Pelea 120/120; PlayMode Economia 19/19, Pelea 49/49, Progresion 82/82. Gana con $250: $1,350 y 1 Brazo de chatarra; pierde: -250 sin pieza; cancelar devuelve; destruir el flujo devuelve la apuesta.
- Para probar jugando: Aún sin pantalla de apuesta (T-145). Para probar a mano más adelante: apostar en el mostrador de peleas de Pruebas_Economia.
- Decisiones: AnunciarApuesta crea al rival en ese momento (Pelea no referencia IA); Anunciar devuelve false si queda una apuesta sin resolver; el neto de Resuelta es solo la ganancia por momio; momio redondeado al entero más cercano (.5 arriba); montos 100/250/500, tope $500, Brazo de chatarra desde $250. Pendiente: DatosOferta_KitReparacion cuesta $300 y D-tony-86 dice $250: lo resuelve T-146/T-150.

### T-145 Pantalla de apuesta
- Verificado automáticamente (acotado): compilar OK; EditMode UI+Pelea+Progresion+Economia 303/303; PlayMode Economia 19/19, Pelea 49/49, UI 83/84 (1 omitida), Progresion 85/86 en la corrida completa; el único fallo era una constante del criterio a), corregida: ApuestaMostradorPruebas 4/4. Capturas T-145-1 y T-145-2.
- Para probar jugando: Pruebas_Economia, Play: E en el mostrador de peleas abre la apuesta; Izq/Der elige monto, Enter apuesta, Esc cancela. ¿Se entiende el riesgo (pierdes lo apostado y el premio) y la pieza en juego desde $250? ¿La línea APUESTA de la pantalla de la arena se lee?
- Decisiones: La pantalla de la arena muestra el neto del contrato IApuestasPelea.Resuelta (+$250 al ganar con $250); el criterio a) decía +$500 y se corrigió. Los textos de la apuesta van en PantallaPelea porque Pelea/Datos no estaba permitida. Si Apostar falla avisa NO ALCANZA y la pantalla sigue abierta; si Empezar falla devuelve lo apostado. Ahora E en el mostrador abre la pantalla en toda escena con ServiciosEconomia (Enter confirma).

### T-146 Mostrador del mercado negro: comprar
- Verificado automáticamente (acotado): compilar OK; EditMode Economia+UI+Progresion 270/270; PlayMode UI 88/89 (1 ignorada ya existente), Economia 19/19, Progresion 86/86. Captura T-146-1.
- Para probar jugando: Pruebas_Economia, Play: camina al mercado (8,6), E abre el mostrador del mercado negro: kit $250, Enter compra, Esc cierra. ¿Se entiende cuánto llevas y el máximo? ¿VENDER (aún 'PRONTO') tiene sitio claro?
- Decisiones: El mostrador es un prefab de Tony que Alan solo coloca (D-tony-81); el texto del mostrador es solo el nombre de la tienda; cerrado sin hora legible dice CERRADO; solo se lista un objeto si su oferta es de esa tienda (hoy solo el kit); kit a $250 (D-tony-86) con valorVenta $0; VENDER muestra PRONTO hasta T-147. Sugerencia: prueba EditMode de que el kit cuesta $250 y se vende en $0 para T-150.

### T-147 Vender piezas y chatarra
- Verificado automáticamente (acotado): compilar OK; EditMode 711/711; PlayMode UI 93/94 (1 omitida, captura de otra tarea), Economia 19/19. Vender 1 brazo deja $400 (+$300 · VENTA en el HUD), segundo $700, el kit no se compra. Captura T-147-1.
- Para probar jugando: Pruebas_Economia: tecla 9 da un brazo, E en el mercado, → pestaña VENDER, Enter vende. ¿El precio de venta ($300 el brazo, $50 la chatarra) se siente justo contra apostar el brazo ($250)?
- Decisiones: D-tony-89: compra vacío esconde la pestaña VENDER; vender quita 1 del inventario primero y luego abona con MotivoDinero.Venta; una fila por objeto distinto; textos VENDISTE {0} POR {1} y ayuda Enter vender.

### T-148 Kits de reparación en plena pelea
- Verificado automáticamente (acotado): compilar OK; EditMode Mecha+UI 174/174; PlayMode Mecha 101/103 (2 capturas omitidas, 0 fallos), Combate 40/40, UI 93/94, Economia 19/19, Pelea 49/49, Progresion 86/86. Captura T-148-1.
- Para probar jugando: Pruebas_Economia (tecla 0 da un kit) o Pruebas_Tony: sube al mecha, empieza una pelea, G con la vida baja. ¿20 % en 1.5 s y máximo 2 por pelea se sienten como decisión y no como botón de curar sin fin? ¿'KITS 2 · G' se lee en la cabina?
- Decisiones: El kit responde con el mecha controlado y la pelea empezada; la curación suma por cuadro sobre la vida actual (si te pegan mientras curas, el daño se conserva); pantalla a la izquierda de la de vida; G con vida completa no gasta el kit; prioridad del texto REPARANDO, LÍMITE DE KITS, SIN KITS, KITS n · G. Sugerencia: la pantalla refresca cada cuadro con string.Format (poca basura).

### T-149 Reparar a medias en el banco del taller
- Verificado automáticamente (acotado): compilar OK; EditMode Economia+Mecha+UI 283/283; PlayMode Mecha 107/110 (3 omitidas, 0 fallos), Economia 19/19, UI 93/94, Progresion 86/86 (en la primera corrida fallaron CambioEscenaPruebas y PrologoPresentePruebas; pasaron al repetir sin cambiar su código: fragilidad de orden por vigilar). Captura T-149-1.
- Para probar jugando: En Pruebas_Economia: pelea y quedas dañado, ve al banco, E: ¿las tres opciones (todo, la mitad, con lo que tienes) se entienden? ¿Reparar a medias se siente como decisión de dinero justo?
- Decisiones: D-tony-90: la pantalla va dentro del mecha (GetComponentInParent), no en las escenas; sin pantalla activa E repara todo como antes; la mitad es ceil(daño/2) y solo si el daño es al menos 10 puntos; con lo que tienes cobra puntos por $10 exactos; las flechas no dan la vuelta, Enter elige y cierra, Esc cierra sin cobrar. Sugerencia: prueba que vigile que ninguna escena de Alan trae el mecha sin BancoTaller.

### T-150 Precios de la economía y pruebas de balance
- Verificado automáticamente (acotado): EditMode Economia 114/114 (incluye las reglas a a g sobre los assets reales); creadores de datos corridos dos veces sin cambios en los assets.
- Tabla de precios (para ajustar jugando; si rompes una regla, BalanceEconomiaPruebas dice cuál):

| Número | Valor |
| --- | --- |
| Dinero inicial | $500 |
| Reparación | $10 por punto, mínimo $50 (de KO a 100 puntos: $1,000) |
| Renta | $500 cada 7 días |
| Premio casera ganar / perder | $600 / $0 |
| Premio campeonato del tutorial | $1,500 |
| Apuesta casera | montos $100/$250/$500, momio 1.0, máxima $500, Brazo de chatarra desde $250 |
| Brazo de chatarra | compra $1,200, venta $300, máximo 5 |
| Chatarra | venta $50, máximo 20 |
| Kit de reparación | compra $250, cura 20 de 100 puntos ($12.5 por punto), máximo 3, 2 por pelea |
- Para probar jugando: una semana en Pruebas_Economia (J pasa días): ¿se vive al día, con decisiones (apostar o no, kit o taller, reparar todo o a medias) y sin quedar atorado?
- Decisiones: sin cambios de valores. La regla d toma el precio de compra de la oferta enlazada o, si no hay enlace (el brazo no la tiene), de la oferta con el mismo nombre. Los creadores de ofertas, premios, objetos y apuesta ya no pisan valores de assets existentes. Nota: correr CreadorKitReparacion modifica MechaJugador.prefab (no lo toqué; lo restauré).

### T-151 Recorrido de cierre del bloque de economía
- Verificado automáticamente (acotado): RecorridoEconomiaPruebas pasa 3 de 3 corridas (46 s cada una); Progresion 87/87. Dinero final $699, $700, $700 igual al calculado; tiempo de juego 45.5 s; MedidorFrames 0 frames lentos (máx 12-13 ms); kit curó 20.0 puntos en 1.5 s; partida real no tocada.
- Para probar jugando: Pruebas_Economia, Play, de noche: compra un kit, apuesta $250 al casero (con el brazo en juego), pelea y usa G, repara a medias en el banco, vende el brazo. ¿La economía se siente apretada pero justa, como vivir al día?
- Decisiones: Atajos: Colocar cerca del mercado, mostrador y banco; RecibirDanio al rival casero tras daño real; FijarVida al 20 % antes de la pelea que se pierde. Aviso del HUD en el banco ya no lleva el costo (E · Reparar mecha…). El área Progresion roza el límite de 900 s de playmode-por-area: usar LIMITE_AREA=2400 en la suite completa.
### T-073 Emboscada en el callejón de Alan
- Verificado automáticamente (acotado): compilar OK (con la mezcla de origin/alan/fase-3); PlayMode Pelea 42/42 (incluye CallejonAlanPruebas y RecorridoEmboscadaPruebas). Escenas de Alan sin cambios. Captura T-073-1.
- Para probar jugando: En Ciudad.unity o Pruebas_Alan.unity, de noche: sal al callejón, llama al mecha (M) y gana la emboscada. ¿La escala del mecha cabe en el callejón y la llegada del cielo se ve bien entre los edificios? (el mecha estacionado es greybox: se ve como cápsula).
- Decisiones: Escena Pruebas_Alan.unity: Alan no coloca Emboscada.prefab ni MechaEstacionado.prefab, solo Emboscada_Anclaje, Callejon_Entrada, Llamado_Callejon (radio 14) y TallerCasa; la prueba instancia jugador, mecha y emboscada en tiempo de juego. Rama tony/fase-6b-alan = main + origin/alan/fase-3, sin tocar nada de Alan; no se mezcló a main.

### T-152 Contrato de ligas y presentación de pelea (Core)
- Verificado automáticamente (acotado): compilar OK; EditMode 767/767; PlayMode Core 3/3; límite por área comprobado (LIMITE 20 s sin Unity huérfano). Nada jugable.
- Para probar jugando: nada. Para Alan: leer los `///` de IProgresoLigas e InfoPelea (Core/Ligas, Core/Pelea) y decir si bastan para un cartel de liga o luces de jefe. Tarea de Core: integrar a main tras el verificador.
- Decisiones: ver docs/decisiones-pendientes-tony.md (D-tony-104).

### T-153 Deudas del bloque de economía
- Verificado automáticamente (acotado): CreadorKitReparacion x2 deja los md5 de MechaJugador y MechaEstacionado iguales; EditMode 767/767; PlayMode Progresion 87/87 dos veces; Mecha 107/110 (3 omitidas a propósito).
- Para probar jugando: nada (no cambia nada jugable).
- Decisiones: la causa de CambioEscenaPruebas/PrologoPresentePruebas no se reprodujo; se quitó la única fuga hallada (teardown que destruía las raíces del test runner). Si vuelve a fallar en la primera corrida, abrir la bitácora de T-153. Ver D-tony-105.

### T-154 Progreso de las ligas
- Verificado automáticamente (acotado): creador x2 deja md5 iguales en ServiciosProgresion.prefab y DatosLigas_Base.asset; EditMode 777/777; PlayMode Progresion 92/92.
- Para probar jugando: nada todavía (LigasDePrueba, tecla L, se coloca en T-159).
- Decisiones: ver D-tony-106 en docs/decisiones-pendientes-tony.md.

### T-155 Cartel por pelea en el flujo
- Verificado automáticamente (acotado): EditMode Pelea 37/37; PlayMode Pelea 60/60 (10 nuevas de CartelPeleaPruebas con flujo casero y tutorial reales).
- Para probar jugando: nada todavía (la cartelera llega en T-159 y T-160).
- Decisiones: ver D-tony-107 en docs/decisiones-pendientes-tony.md.

### T-156 Pelea de jefe: furia a la mitad de la vida
- Verificado automáticamente (acotado): EditMode 787/787 (6 nuevas de ReglaFuriaJefePruebas); PlayMode IA 33/33 (5 nuevas de FuriaJefePruebas: daño 12 a 15, espera media baja al menos 30 % en furia, FuriaIniciada una vez, revancha vuelve todo a 1 y quita el color, FuriaJefe con PiedadRival da error y se apaga). Captura docs/capturas/T-156-1.png (cuerpo rojo desde la cabina).
- Para probar jugando: nada todavía (el prefab RivalJefeDePrueba se puede soltar en una escena de Tony; los jefes reales llegan en T-157).
- Decisiones: ver D-tony-108 en docs/decisiones-pendientes-tony.md.

### T-157 Los 20 rivales de las 4 ligas
- Verificado automáticamente (acotado): EditMode 793/793 (6 nuevas de RivalesLigaPruebas, reglas a a f sobre los assets reales); PlayMode IA 35/35 (2 nuevas: los 20 nacen con su alias y su vida sin errores, y captura). Creador corrido dos veces: md5 igual de 68 archivos (20 prefabs, 40 datos, 8 materiales). Captura docs/capturas/T-157-1.png (los 5 de la liga 1 de izquierda a derecha, el jefe naranja brillante).
- Para probar jugando: nada todavía (los carteles llegan en T-158 y la cartelera en T-159).
- Decisiones: ver D-tony-109 en docs/decisiones-pendientes-tony.md.

### T-158 Calendario de las ligas: premios, momios y presentadores por liga
- Verificado automáticamente (acotado): EditMode 803/803 (10 nuevas de BalanceLigasPruebas, reglas a a j sobre los assets reales; BalanceEconomiaPruebas en verde); PlayMode Economia 19/19 y Pelea 60/60. Creadores corridos dos veces: md5 igual de 11 assets. Prueba negativa hecha: jefe de la liga 2 a $1,500 hace fallar la regla b con mensaje que lo nombra; revertido.
- Para ti (sensación): con la tabla en mano, ¿el jefe de la liga 1 ($2,000, más de 3 veces la casera) se siente como progreso? ¿Momios de 1.0 a 2.5 hacen tentador apostar fuerte contra jefes? Se ajusta en CalendarioLigas_Base.asset y DatosApuestaPelea_*; la prueba avisa si rompes una regla.
- Decisiones: D-tony-110 en decisiones-pendientes-tony.md.

### T-159 Retar en las ligas desde el mostrador
- Verificado automáticamente: compilar OK; EditMode 811/811 (8 de ReglaCarteleraPruebas); PlayMode Economia 19/19 y Progresion 97/98 (6 de LigasMostradorPruebas en verde; falló RecorridoPrologoPruebas por un frame de 583 ms, intermitente: sola pasa 1/1). Creador de Pruebas_Economia idempotente (dos corridas, escena igual).
- Para probar jugando: nada todavía (la pantalla para elegir es T-160; en Pruebas_Economia la tecla L vence al siguiente rival).

### T-160 Pantalla de la cartelera
- Verificado automáticamente: EditMode 820/820; PlayMode Economia 19/19, UI 93/94 (1 ignorada), Progresion 104/105 (CambioEscenaPruebas, intermitente por orden, fuera del diff). Creadores idempotentes. Capturas T-160-1 y T-160-2.
- Para ti (sensación): Pruebas_Economia, de noche, E en el mostrador. ¿Se entiende liga, rival y premio de un vistazo? ¿El jefe naranja se distingue? ¿La fila gris "VENCE A DON FIERRO" motiva o estorba?

### T-161 Subir de liga al vencer al campeón
- Verificado automáticamente: EditMode 824/824; PlayMode Pelea 66/66, Progresion 105/105, UI 101/102 (1 ignorada). Capturas T-161-1 y T-161-2.
- Para ti (sensación): Pruebas_Economia, tecla L hasta el jefe de la liga 1 y pelea contra DON FIERRO. ¿"PELEA DE JEFE" suma? ¿El aviso de liga al bajar del mecha se siente logro? ¿5 s es mucho o poco? ¿"PELEA DE JEFE" en el prólogo quita sorpresa?
- Decisiones: D-tony-111.

### T-164 Deudas: guardado y CambioEscenaPruebas
- Verificado automáticamente (por el implementador, sin verificador aparte): EditMode 824/824; PlayMode Progresion 109/109 cuatro veces seguidas; CambioEscenaPruebas 4/4 en 8 corridas aisladas. 3 pruebas nuevas de GuardadoClavesPendientesPruebas.
- Ojo: la causa de CambioEscenaPruebas (tope de 600 frames ≈ 1 s en batch) está demostrada por la medición del margen, no por un fallo reproducido. Si vuelve a fallar, reabrir.
- Para probar jugando: nada.

### T-165 Cambiar los datos de un mecha en juego
- Verificado automáticamente (por el implementador, sin verificador aparte): EditMode 829/829; PlayMode Combate 40/40, IA 35/35, Mecha 113/116 (3 omitidas como antes); suite PlayMode 776/789 (9 fallos conocidos de Alan en Arena). 5 pruebas nuevas de ReglaCambioDatos y 6 de CambiarDatosMechaPruebas con el prefab real.
- Para probar jugando: nada todavía (las piezas que cambian los datos llegan en T-167).
- Decisiones: D-tony-127.

### T-163 Valores del mecha en tiempo de ejecución (Core)
- Verificado automáticamente (por el implementador, sin verificador aparte): compilar OK; EditMode 834/834 (5 nuevas de ValoresMechaPruebas; ReglasEnsambladosPruebas en verde); PlayMode Combate 40/40, IA 35/35, Pelea 66/66, Mecha 113/116 (3 omitidas). Ningún DatosMecha_*.asset cambió.
- Se hizo sin aprobación explícita de Tony por su orden de no parar (D-tony-128); revertible deshaciendo su merge.
- Para probar jugando: nada todavía.

#### Para Alan
- Existe `ValoresMecha` y `DatosMecha.CrearEnEjecucion(ValoresMecha)` en Core/Datos, por si algún día creas mechas del mundo por código. No hace falta hacer nada; viaja con tony/fase-7.

### T-166 Piezas como datos
- Verificado automáticamente (por el implementador, sin verificador aparte): compilar OK; EditMode 849/849 (15 nuevas: ReglaEstadisticasPiezasPruebas y PiezasAssetsPruebas); PlayMode por área 776/789 (8 fallos conocidos de Arena, 4 omitidas). Creadores idempotentes en los dos órdenes (md5 igual de Mecha/Datos/Piezas, Economia/Datos y ServiciosEconomia.prefab).
- Para ti (sensación): la tabla de bonos y precios de las 85 piezas (fuego: golpe más seguido; militar: golpe más pesado). Se ajusta en CreadorPiezas.cs; las pruebas avisan si rompes una regla.
- Decisiones: D-tony-129.

### T-167 EquipoMecha: las piezas puestas cambian al mecha y se guardan
- Verificado automáticamente (por el implementador, sin verificador aparte): EditMode 849/849; PlayMode Mecha 123/126 (3 omitidas de captura), Combate 40/40, Economia 19/19, Progresion 109/109, UI 101/102 (1 omitida). 10 pruebas nuevas de EquipoMechaPruebas. Creador idempotente (md5 igual de los dos prefabs).
- Para probar jugando: nada todavía (la pantalla llega en T-169 y lo visible en T-170).
- Decisiones: D-tony-130.

### T-168 Comprar y cambiar piezas en el banco del taller (lógica)
- Verificado automáticamente (por el implementador, sin verificador aparte): compilar OK; EditMode 859/859 (9 nuevas); PlayMode Mecha 133/136, Economia 19/19, Personaje 111/111, UI 101/102, Progresion 109/109; 11 nuevas de TallerPiezasPruebas. Creador idempotente. Pruebas viejas de BancoTaller, ReparacionParcial y RecorridoEconomia ajustadas por la opción nueva (una línea de RecorridoEconomiaPruebas fuera de carpetas permitidas, sin cambiar lo que comprueba).
- Para probar jugando: nada todavía (la pantalla es T-169).
- Decisiones: D-tony-131.

### T-169 Pantalla "PIEZAS DEL MECHA" en el taller
- Verificado automáticamente (por el implementador, sin verificador aparte): verificar.sh todo: EditMode 859/859; PlayMode 807/820 (9 fallos conocidos de Alan en Arena); UI 113 sin fallos propios; 11 nuevas de PantallaPiezasPruebas; MedidorFrames 0 lentos. Creador idempotente. Capturas T-169-1 y T-169-2 revisadas.
- Para ti (sensación): Pruebas_Economia, E en el mecha estacionado, PIEZAS DEL MECHA. ¿Se entiende qué cambia cada pieza (VIDA +2 · DAÑO −1) y por qué una está gris? Límite: con 10 o más filas la lista roza la línea de estadísticas (hoy el máximo es 9).
- Decisiones: D-tony-132.

### T-170 Las piezas se ven al subirte al mecha
- Verificado automáticamente: PlayMode Mecha 139/142 (3 omitidas de captura), UI sin fallos (115/117 con captura); 6 pruebas de AspectoPiezasPruebas y 4 de PantallaPiezasCabinaPruebas. Creadores idempotentes. Capturas T-170-1 y T-170-2 (la pantalla no tapa el centro).
- Para ti (sensación): Pruebas_Economia, compra un brazo de fuego y otro militar y súbete. ¿Se nota la mejora al primer vistazo? ¿Magenta, azul acero, turquesa y naranja se leen como ramas y no como alarma? ¿Las franjas se ven? ¿La lista de 5 piezas durante el encendido ayuda o estorba?
- Ojo: la captura del mecha desde fuera es una cápsula turquesa (greybox).

### T-171 Piezas de rama en juego en las ligas
- Verificado automáticamente (por el implementador): EditMode 862/862; PlayMode Progresion 112/112, Economia 19/19, Pelea 66/66. Creador idempotente. Sin capturas.
- Para ti (sensación): Pruebas_Economia, apuesta $250 contra TORNILLO. ¿La pieza en juego te tienta? ¿Ganarla y ponértela en el taller se siente premio? La revancha de la cartelera ya no da pieza.
- Decisiones: D-tony-121.

### T-172 Balance de las piezas contra las ligas
- Verificado automáticamente (por el implementador): EditMode 869/869 (7 nuevas de BalancePiezasLigasPruebas). Las 6 reglas pasan sin cambiar ningún rival ni bono; la más justa es fuego IV contra el jefe de la liga 4 (0.82; el mínimo es 0.8). Tabla de razones en la bitácora.
- Para ti (sensación): Pruebas_Economia con LigasDePrueba hasta la liga 3. ¿El jefe se siente ganable con las piezas de tu liga y duro sin ellas?
