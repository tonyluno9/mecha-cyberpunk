# CYBERFIX — Listas de trabajo para el equipo
29-oct-2026 · Juego de pandillas tipo GTA en tercera persona (ya sin mechas). Corte: 12-nov. Entrega: 18-nov.

---

## LISTA 1 — SONIDOS

### Cómo entregarlos (importante: así entran solos al juego)
- **Nombre del archivo = el ID de la tabla** (ej. `ARM_PIS_DISPARO_01.wav`). Si hay variantes: `_01`, `_02`, `_03`; el juego elige una al azar.
- Carpeta compartida con subcarpetas por sección (JUG, ARM, VEH, PAN…). Claude los mete en `Assets/Arte/Audio/<Sección>/`.
- **Formato:** WAV, 44.1 o 48 kHz, 16 bits. **Mono** para efectos que suenan en un punto del mundo; **estéreo** para música,
  ambientes y UI. Sin silencio al inicio. Los que dicen **LOOP** deben repetirse sin corte.
- **Dónde buscar (gratis):** freesound.org (filtrar licencia CC0), Sonniss "GDC Game Audio Bundle", Kenney.nl (audio),
  Pixabay (sounds), ZapSplat, OpenGameArt. Anotar en una hoja: ID, archivo, sitio, autor y licencia (para los créditos).
- **Prioridad:** P1 = lo necesita la demo de 15 min. P2 = mundo libre. P3 = si sobra tiempo.

### JUG — Jugador
| ID | Sonido | Var. | P |
|---|---|---|---|
| JUG_PASO_ASFALTO | Pisada en asfalto seco | 6 | P1 |
| JUG_PASO_ASFALTO_MOJADO | Pisada en asfalto mojado / charco | 6 | P1 |
| JUG_PASO_INTERIOR | Pisada en piso de casa/concreto | 6 | P1 |
| JUG_PASO_METAL | Pisada en lámina/escalera metálica | 4 | P2 |
| JUG_CORRER_RESPIRA | Respiración al correr (LOOP corto) | 1 | P3 |
| JUG_SALTO | Impulso de salto (ropa + esfuerzo) | 3 | P1 |
| JUG_CAER | Aterrizar | 3 | P1 |
| JUG_DANIO | Quejido al recibir golpe/bala | 4 | P1 |
| JUG_CAE_DERROTADO | Caer al suelo derrotado | 2 | P1 |
| JUG_VIDA_BAJA | Latido / pulso con vida baja (LOOP) | 1 | P2 |
| JUG_REGENERA | Respiro de alivio al regenerar | 1 | P3 |
| JUG_ROPA_MOVER | Roce de chamarra al girar/apuntar | 3 | P3 |

### PUÑ — Combate a mano
| ID | Sonido | Var. | P |
|---|---|---|---|
| PUN_GOLPE_AIRE | Puñetazo al aire (whoosh) | 4 | P1 |
| PUN_GOLPE_CARNE | Puñetazo que conecta | 5 | P1 |
| PUN_GOLPE_FUERTE | Golpe fuerte / remate | 3 | P1 |
| PUN_BLOQUEO | Bloquear un golpe | 3 | P2 |
| PUN_PATADA | Patada que conecta | 3 | P3 |

### ARM — Armas
| ID | Sonido | Var. | P |
|---|---|---|---|
| ARM_PIS_DISPARO | Pistola: disparo | 4 | P1 |
| ARM_PIS_RECARGA | Pistola: recarga completa | 1 | P1 |
| ARM_PIS_SACAR | Pistola: sacar/guardar | 1 | P2 |
| ARM_ESC_DISPARO | Escopeta: disparo | 3 | P1 |
| ARM_ESC_BOMBEO | Escopeta: bombeo (corredera) | 2 | P1 |
| ARM_ESC_RECARGA_CARTUCHO | Escopeta: meter un cartucho | 3 | P1 |
| ARM_SUB_DISPARO | Subfusil: disparo individual (para ráfaga) | 4 | P1 |
| ARM_SUB_RAFAGA_COLA | Subfusil: eco al soltar ráfaga | 2 | P2 |
| ARM_SUB_RECARGA | Subfusil: recarga | 1 | P1 |
| ARM_VACIO | Clic sin balas | 2 | P1 |
| ARM_CAMBIAR | Cambiar de arma (genérico) | 2 | P1 |
| ARM_CASQUILLO | Casquillo cayendo al piso | 4 | P3 |
| ARM_BALA_IMPACTO_CONCRETO | Bala pega en pared | 4 | P1 |
| ARM_BALA_IMPACTO_METAL | Bala pega en metal/carro | 4 | P1 |
| ARM_BALA_IMPACTO_CARNE | Bala pega en persona | 4 | P1 |
| ARM_BALA_ZUMBIDO | Bala pasando cerca (whiz) | 3 | P2 |
| ARM_LASER_AVISO | Pitido del láser rojo antes del tiro rival | 1 | P1 |
| ARM_DISPARO_LEJANO | Disparos lejanos en la ciudad (ambiente) | 4 | P3 |
| ARM_MEJORA | Arma mejorada (metal + clic satisfactorio) | 1 | P2 |

### VEH — Vehículos
| ID | Sonido | Var. | P |
|---|---|---|---|
| VEH_VIEJO_MOTOR_RALENTI | Carro viejo: motor en marcha mínima (LOOP) | 1 | P1 |
| VEH_VIEJO_MOTOR_ACELERA | Carro viejo: motor a medias/alto (LOOP) | 1 | P1 |
| VEH_VIEJO_ARRANQUE | Carro viejo: encender (batalla un poco) | 1 | P1 |
| VEH_PRO_MOTOR_RALENTI | Carro cyberpunk: motor eléctrico/zumbido (LOOP) | 1 | P2 |
| VEH_PRO_MOTOR_ACELERA | Carro cyberpunk: acelerando (LOOP) | 1 | P2 |
| VEH_PUERTA_ABRIR | Abrir puerta | 2 | P1 |
| VEH_PUERTA_CERRAR | Cerrar puerta | 2 | P1 |
| VEH_FRENO_DERRAPE | Llantas derrapando (LOOP) | 1 | P1 |
| VEH_CHOQUE_LEVE | Choque ligero | 3 | P1 |
| VEH_CHOQUE_FUERTE | Choque fuerte | 2 | P2 |
| VEH_CLAXON_VIEJO | Claxon del carro viejo | 1 | P2 |
| VEH_CLAXON_TRAFICO | Claxons de otros carros | 3 | P2 |
| VEH_ALARMA_ROBO | Alarma de carro (LOOP 3 s) | 1 | P1 |
| VEH_FORZAR_PUERTA | Forzar cerradura al robar | 1 | P1 |
| VEH_PASA_CERCA | Carro de tráfico pasando (whoosh) | 3 | P2 |
| VEH_LLANTAS_MOJADO | Llantas sobre piso mojado (LOOP) | 1 | P2 |

### PAN — Pandillas y aliados (voces sin palabras claras o frases cortas en español)
| ID | Sonido | Var. | P |
|---|---|---|---|
| PAN_RIVAL_ALERTA | Rival te descubre ("¡Ey!", silbido) | 4 | P1 |
| PAN_RIVAL_PROVOCA | Provocación / burla | 4 | P2 |
| PAN_RIVAL_DANIO | Quejido de rival | 4 | P1 |
| PAN_RIVAL_CAE | Rival derrotado | 3 | P1 |
| PAN_JEFE_ENTRADA | Jefe aparece (grito + golpe musical corto) | 1 | P1 |
| PAN_JEFE_CAE | Jefe derrotado | 1 | P1 |
| PAN_ALIADO_RECLUTA | Aliado se une ("¡Va!", "Jalo") | 3 | P1 |
| PAN_ALIADO_SIGUE | Aliado: "Te sigo" | 2 | P2 |
| PAN_ALIADO_ESPERA | Aliado: "Aquí espero" | 2 | P2 |
| PAN_ALIADO_SUBE_NIVEL | Aliado sube de nivel | 1 | P2 |
| PAN_ALIADO_CAE | Aliado cae | 2 | P2 |
| PAN_VECINO_AUXILIO | Vecino pidiendo ayuda (inicio de la demo) | 2 | P1 |
| PAN_SILBIDO_LLAMAR | Silbido para llamar a tu gente | 1 | P3 |

### CAL — Calor, policía, drones
| ID | Sonido | Var. | P |
|---|---|---|---|
| CAL_SUBE_NIVEL | Aviso de calor sube | 1 | P1 |
| CAL_BAJA_NIVEL | Calor se pierde | 1 | P2 |
| CAL_DRON_ZUMBIDO | Dron volando (LOOP) | 1 | P1 |
| CAL_DRON_DISPARO | Disparo de dron (láser/eléctrico) | 3 | P1 |
| CAL_DRON_ESCANEO | Dron escaneando | 1 | P2 |
| CAL_PATRULLA_SIRENA | Sirena de patrulla (LOOP) | 1 | P1 |
| CAL_PATRULLA_MEGAFONO | Megáfono: "¡Alto!" (voz corta) | 2 | P3 |

### ZON — Zonas y respeto
| ID | Sonido | Var. | P |
|---|---|---|---|
| ZON_ENTRAR_RIVAL | Entras a zona rival (tensión corta) | 1 | P1 |
| ZON_ENTRAR_PROPIA | Entras a tu zona (alivio corto) | 1 | P2 |
| ZON_RESPETO_SUBE | Respeto sube | 1 | P1 |
| ZON_CONQUISTADA | Zona conquistada (fanfarria corta 3–5 s) | 1 | P1 |

### ECO — Dinero, tienda, mercado negro, clínica
| ID | Sonido | Var. | P |
|---|---|---|---|
| ECO_DINERO_RECOGER | Recoger dinero | 2 | P1 |
| ECO_COMPRA | Compra hecha (caja) | 1 | P1 |
| ECO_SIN_DINERO | No alcanza el dinero | 1 | P1 |
| ECO_ARMA_RECOGER | Recoger arma del piso | 1 | P1 |
| ECO_CLINICA_DESPERTAR | Despertar en la clínica (monitor + respiración) | 1 | P1 |
| ECO_CASINO_TRAGAMONEDAS | Tragamonedas girando / premio | 2 | P3 |

### UI — Interfaz
| ID | Sonido | Var. | P |
|---|---|---|---|
| UI_MOVER | Mover entre opciones | 1 | P1 |
| UI_ACEPTAR | Aceptar | 1 | P1 |
| UI_CANCELAR | Cancelar / cerrar | 1 | P1 |
| UI_ERROR | Acción no permitida | 1 | P1 |
| UI_OBJETIVO_NUEVO | Objetivo nuevo | 1 | P1 |
| UI_OBJETIVO_CUMPLIDO | Objetivo cumplido | 1 | P1 |
| UI_MAPA_ABRIR | Abrir mapa (M) | 1 | P2 |
| UI_MAPA_CERRAR | Cerrar mapa | 1 | P2 |
| UI_GUARDADO | Partida guardada | 1 | P2 |
| UI_TELEFONO | Notificación de teléfono / mensaje | 1 | P2 |
| UI_TITULO_INICIO | Golpe de sonido al mostrar el título | 1 | P1 |
| UI_CONTINUARA | Golpe final de "CONTINUARÁ" | 1 | P1 |

### MUN — Mundo y objetos
| ID | Sonido | Var. | P |
|---|---|---|---|
| MUN_PUERTA_CASA_ABRIR / CERRAR | Puerta de madera | 2 | P1 |
| MUN_CORTINA_METALICA | Cortina metálica (subir/bajar) | 1 | P2 |
| MUN_REJA | Reja/portón metálico | 1 | P2 |
| MUN_CAMA | Acostarse / dormir | 1 | P1 |
| MUN_BASURERO_GOLPE | Pegarle a basurero/tambo | 2 | P3 |
| MUN_VIDRIO_ROTO | Vidrio roto | 2 | P2 |
| MUN_CHISPAS_ELECTRICAS | Chispas de transformador/cable | 2 | P3 |
| MUN_NEON_ZUMBIDO | Zumbido de letrero de neón (LOOP, 3D) | 1 | P2 |
| MUN_AIRE_ACONDICIONADO | AC en azotea (LOOP, 3D) | 1 | P3 |
| MUN_GENERADOR | Generador del taller (LOOP, 3D) | 1 | P3 |
| MUN_PERRO_LADRA | Perro lejano | 3 | P3 |

### AMB — Ambientes (LOOP, estéreo, 1–2 min)
| ID | Sonido | P |
|---|---|---|
| AMB_CALLE_NOCHE | Calle de barrio de noche: tráfico lejano, gente, perros | P1 |
| AMB_CALLE_DIA | Calle de día: más tráfico, vendedores, voces | P2 |
| AMB_LLUVIA_SUAVE | Lluvia ligera (para cuando se vuelva a activar) | P2 |
| AMB_CALLEJON | Callejón: goteras, eco, ventilas | P1 |
| AMB_INTERIOR_CASA | Casa: refri, reloj, silencio con ciudad lejos | P1 |
| AMB_MERCADO_NEGRO | Mercado negro: murmullo, radio lejana, regateo | P1 |
| AMB_CLINICA | Clínica: monitor, zumbido de lámpara | P2 |
| AMB_BAR | Bar/cantina: murmullo, vasos | P2 |
| AMB_CASINO | Casino: máquinas, campanitas | P3 |
| AMB_CLANDESTINO | Sótano con público gritando | P2 |
| AMB_TIANGUIS | Tianguis: puestos, pregones, multitud | P1 |

### MUS — Música (ahorita apagada; para después)
| ID | Qué | P |
|---|---|---|
| MUS_TITULO | Tema de la pantalla de título (1–2 min, LOOP) | P2 |
| MUS_COMBATE | Música de pelea que entra al pelear (LOOP) | P2 |
| MUS_CONQUISTA | Tema de victoria al conquistar zona (corto) | P2 |
| MUS_RADIO_CUMBIA / _OSCURA / _HIPHOP | 2–3 canciones por estación de radio (covers del equipo, solo escolar) | P3 |
| MUS_CREDITOS | Música de créditos | P3 |

**Total:** ~120 sonidos (contando variantes ~250 archivos). Solo P1: ~70.

---

## LISTA 2 — ASSETS 3D (para el chat de arte; reemplaza la de exos)

### Personajes (con esqueleto humanoide para animaciones de Mixamo)
| ID | Qué | P |
|---|---|---|
| P01 | Protagonista (personaje de la chamarra naranja y cabello turquesa) | P1 |
| P02 | Aliado / segundo personaje (el de la máscara) — o jefe de Los Calaveras | P1 |
| P03 | Pandillero Calaveras ×2 variantes (magenta) | P1 |
| P04 | Pandillero Víboras ×2 variantes (verde ácido) | P2 |
| P05 | Jefe Calaveras / Jefe Víboras | P1 / P2 |
| P06 | Aliados: El Chato, La Güera, El Topo | P1 (Chato) / P2 |
| P07 | Vecino (inicio de la demo), doctor de la clínica, vendedor del mercado negro | P1 |
| P08 | Peatones ×3–4 variantes | P2 |
| P09 | Policía/patrullero | P3 |
Reglas: pose T, sin cosas delgadas sueltas (medias de red, cadenas y correas pintadas en la textura), 15–25k caras.

### Animaciones (Mixamo, gratis; las baja el equipo con el personaje subido)
Idle, caminar, correr, saltar, caer, golpe ×2, recibir golpe, caer derrotado, apuntar pistola, disparar pistola, apuntar rifle (escopeta/subfusil),
disparar rifle, recargar ×2, agacharse/cubrirse, subir y bajar del carro (opcional), sentado (público), saludo/burla (rivales).

### Armas (en la mano y tiradas en el piso)
| ID | Qué | P |
|---|---|---|
| W01 | Pistola | P1 |
| W02 | Escopeta | P1 |
| W03 | Subfusil | P1 |
| W04 | Arma cuerpo a cuerpo (bate/tubo/machete) | P3 |
| W05 | Versiones mejoradas nivel II y III (mismo modelo con piezas extra o color) | P3 |
| W06 | Bolsa/billetes de dinero en el piso | P1 |

### Ciudad (lo que falta, del reporte del 28-oct)
- **Kit de pisos para edificios** C20a/b/c, C21, C22 (los edificios se ven cortados arriba). **P1**
- **Puestos del tianguis** (lona, mesa, cajas, frutas, ropa colgada gruesa) ×3 variantes — El Tianguis es zona clave y hoy no se reconoce. **P1**
- **Interior del mercado negro**: mostrador, estantes con armas y cajas, rejas, letrero liso — hoy está vacío. **P1**
- Marcas de territorio: grafiti/bandera/manta de Calaveras, Víboras y tu pandilla (planos con textura). **P1**
- Ruedas separadas del carro (V02a/b) y versiones ligeras de carros. **P2**
- Entrada del clandestino, público, jaula (si se sigue usando el clandestino). **P3**
- Clínica por dentro: camilla, monitor, lámpara. **P2**
- Barricadas de pandilla (llantas, tarimas, tambos) para marcar zonas. **P2**

---

## LISTA 3 — OTRAS COSAS QUE HAY QUE CONSEGUIR O DECIDIR (personas, no Claude)
1. **Nombre definitivo**: CYBERFIX (propuesto). Logo para la pantalla de título (imagen PNG transparente).
2. **Nombre de tu pandilla** y su color (hoy: naranja, sin nombre). Confirmar Los Calaveras y Las Víboras.
3. **Nombre del protagonista** (o se elige en la creación de personaje).
4. **Historia en 10 líneas**: por qué peleas por el barrio, qué quiere cada pandilla, qué pasa al final del "continuará".
5. **Frases cortas** para voces (para grabarlas el equipo): rivales, aliados, vecino, vendedor, doctor. 3–5 por personaje.
6. **Iconos de UI**: 4 armas, dinero, respeto, calor, aliados, marcas del mapa (PNG 128×128 transparentes).
7. **Fuente tipográfica** cyberpunk gratuita (Google Fonts o dafont con licencia libre).
8. **Música** de la radio (covers) — opcional.
9. **Probadores**: 2–3 personas que no conozcan el juego para jugar la demo (meta: terminarla sola en ~15 min).
10. **Material de entrega**: video de 2–3 min, capturas, presentación o documento según pida la materia.
