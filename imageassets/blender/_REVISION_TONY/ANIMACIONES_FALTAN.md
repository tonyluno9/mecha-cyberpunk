# ANIMACIONES QUE FALTAN — inventario (21-nov, madrugada)

Fuentes revisadas: `Assets/Arte/Personajes/**/Clips`, `Animaciones/*`, los 16 `Overrides/*.overrideController`, `ControladorBase.controller` (71 estados) y `imageassets/blender/{pj01_clips,npc_clips,nova_clips}`. El volcado crudo de los overrides está en `_overrides_dump.txt`.

## Hallazgos del proyecto de Unity (lo más importante)
1. **El `ControladorBase` no tiene estados** para: muerte por explosión, por atropello, por caída de altura; derribado por explosión; ser sacado del carro (NPC); robar carro; tirarse del carro en movimiento; caer de la moto; disparar desde vehículo; granada; recargas por arma; implantes; arresto; rapel; caída del cielo de Nova (solo `Caida`/`SaltoAterrizaje` reutilizados); bloquear con escudo; tambaleo. Aunque se hagan los clips, **hay que agregar estados y parámetros** en el controlador.
2. **Clips prestados en los overrides:**
   - `Dano_Frente / Dano_Espalda / Dano_Fuerte` usan el clip **genérico Mixamo** (no el de PJ01) en Jugador, Aliados, Guardias, Jefe, NPC, Profes, Robots, SKULLS y SNAKES — aunque `PJ01_Dano_Frente/Espalda` ya existen.
   - `Skull_Burla` y `Nova_Flexing` → `PJ01_Punos_Guardia` en **todos** (no hay burla real).
   - `Carro_Pitar` → `PJ01_Manejar_Carro` (no hay pitar).
   - E15/E16/E18/Guardias: `Muerte_Atras` → `NPC_Muerte_Espaldas` (clip de civil); `Muerte_Desmayo` → PJ01; levantarse, carro, cobertura → PJ01.
   - E16: `Loco_Correr` → `E16_Caminar` (no corre; correcto si no debe correr).
   - E18: `Loco_Caminar` → `Guardia_Caminar`, `Loco_Correr` → `PJ01_Correr_SinImplante` (**ya hay `E18_Caminar/Correr` nuevos, falta conectarlos**).
   - NPC*: `Dano_Izquierda/Derecha` → PJ01; no existe NPC izquierda/derecha.
   - SKULLS/SNAKES/Jefe/Robots/Profes: locomoción, muertes y daño **todo prestado de PJ01**.
3. `PJ01_Carro_SacarConductor` y `PJ01_Conductor_SerSacado` **existen en Clips pero ningún override/estado los usa**.
4. `E17_*` clips existen pero son del rig anterior (el pedido confirma que no calzan).

## PROTAGONISTA PJ01
| Animación | Estado | Clip actual / nota |
|---|---|---|
| Muerte por balazo de frente | YA EXISTE Y APROBADA | PJ01_Muerte_Frente |
| Muerte por balazo de espalda | YA EXISTE Y APROBADA | PJ01_Muerte_Espaldas |
| Muerte desmayo / variante | YA EXISTE Y APROBADA | PJ01_Muerte_Desmayo, PJ01_Muerte_Adelante/Atras |
| Muerte por explosión (sale volando) | FALTA HACERLA | — |
| Muerte por atropello | FALTA HACERLA | — |
| Muerte por caída de altura | FALTA HACERLA | — |
| Daño balazo frente / espalda / izq / der | EXISTE PERO FALTA PERFECCIONAR | PJ01_Dano_* existen; el override del Jugador usa Dano_Frente/Espalda genéricos → cambiar a PJ01 |
| Golpe recibido | EXISTE PERO FALTA PERFECCIONAR | Dano_Fuerte genérico |
| Empujón de maleante | FALTA HACERLA | — |
| Explosión cercana (tambalea / cae) | FALTA HACERLA | — |
| Atropellado y levantarse | FALTA HACERLA | levantarse existe (PJ01_Dano_Levantarse_*), falta el derribo |
| Caer por explosión y levantarse | FALTA HACERLA | idem |
| Levantarse del piso al iniciar | EXISTE PERO FALTA PERFECCIONAR | puede usar PJ01_Dano_Levantarse_BocaArriba |
| Abrir puerta y subir al carro | YA EXISTE Y APROBADA | PJ01_Carro_Entrar |
| Bajar del carro | YA EXISTE Y APROBADA | PJ01_Carro_Salir |
| Robar carro (sacar conductor y subir) | EXISTE PERO FALTA PERFECCIONAR | PJ01_Carro_SacarConductor existe, sin estado en Unity |
| Tirarse del carro en movimiento | FALTA HACERLA | — |
| Subir / bajar moto | YA EXISTE Y APROBADA | PJ01_Moto_Subir / Bajar |
| Caer de la moto | FALTA HACERLA | — |
| Disparar desde carro / moto | FALTA HACERLA | — |
| Recargar pistola | YA EXISTE Y APROBADA | PJ01_Pistola_Recargar |
| Recargar fusil | YA EXISTE Y APROBADA | PJ01_Rifle_Recargar |
| Recargar revólver, escopeta, subfusil, francotirador, lanzagranadas, plasma | FALTA HACERLA | — |
| Cambiar de arma | EXISTE PERO FALTA PERFECCIONAR | PJ01_Pistola_Sacar/Guardar |
| Lanzar granada | FALTA HACERLA | — |
| Apuntar / disparar agachado | YA EXISTE Y APROBADA | PJ01_Pistola_AgachadoIdle/Caminar |
| Trepar borde / saltar barda / rodar caída | YA EXISTE Y APROBADA | PJ01_Parkour_* |
| Colgarse | FALTA HACERLA | — |
| Nadar / caer al agua | FALTA HACERLA | (si aplica) |
| Abrir puerta, botón, recoger | YA EXISTE Y APROBADA | PJ01_Inter_* |
| Beber bebida, comer papitas, venda | FALTA HACERLA | — |
| Sentarse en silla del Doc | FALTA HACERLA | — |
| Ser arrestado (manos arriba y esposado) | FALTA HACERLA | — |
| Implantes: escudo, visor, salto potenciado, brazo láser, misiles | FALTA HACERLA | hay PJ01_Implantes_Ataque.fbx sin revisar |
| Idle con pies girados, saltos, aterrizaje 3 puntos, caminar apuntando der | YA EXISTE Y APROBADA | aprobados 20-nov |

## NPCs CIVILES
| Animación | Estado | Clip actual / nota |
|---|---|---|
| Balazo frente / espalda | YA EXISTE Y APROBADA | NPC_Balazo_Frente/Espalda |
| Balazo izquierda / derecha | USA CLIP PRESTADO | usa PJ01_Dano_Izquierda/Derecha |
| Herido cojeando | FALTA HACERLA | — |
| Muerte balazo (3 variantes) | EXISTE PERO FALTA PERFECCIONAR | solo 2: NPC_Muerte_Frente/Espaldas (+Desmayo de PJ01) |
| Muerte por explosión / atropello / cayendo de lado | FALTA HACERLA | — |
| Sacado del carro | EXISTE PERO FALTA PERFECCIONAR | PJ01_Conductor_SerSacado existe, sin estado |
| Manejando (manos al volante) | USA CLIP PRESTADO | PJ01_Carro_Manejar |
| Asustarse, huir, agacharse cubriéndose | YA EXISTE Y APROBADA | NPC_Asustarse, NPC_Huir_Corriendo, NPC_Agachado_CubriendoCabeza |
| Tirarse al piso, levantar las manos | FALTA HACERLA | — |
| Celular, platicar, sentarse en banca, esperar | YA EXISTE Y APROBADA | NPC_* |
| Grabar con celular, discutir, señalar, comer, vender, barrer | FALTA HACERLA | — |

## POLICÍA (C01)
| Animación | Estado | Clip actual / nota |
|---|---|---|
| Caminar apuntando 4 dir, correr con arma, disparar, recargar | USA CLIP PRESTADO | usa clips de PJ01 (por el controlador base) |
| Bajar de la patrulla | USA CLIP PRESTADO | PJ01_Carro_Salir |
| Cubrirse tras carro y asomarse | USA CLIP PRESTADO | PJ01_Cobertura_* |
| Arrestar al jugador, hablar por radio, rapel desde nave | FALTA HACERLA | — |
| Daño y muertes | USA CLIP PRESTADO | PJ01 / genéricos |

## PANDILLEROS (SKULLS, SNAKES, E19, E20)
| Animación | Estado | Clip actual / nota |
|---|---|---|
| Provocar: retar, gritar, empujar, burlarse, sacar arma amenazando | FALTA HACERLA | Skull_Burla está mapeado a PJ01_Punos_Guardia; existe Skull_Burla.fbx original sin usar |
| Golpes | USA CLIP PRESTADO | PJ01_Punos_* |
| Patada | FALTA HACERLA | — |
| Apuntar, disparar, recargar, cubrirse, huir | USA CLIP PRESTADO | PJ01 |
| Daño y muertes (3) | USA CLIP PRESTADO | PJ01 / genéricos |
| Herido arrastrándose | FALTA HACERLA | — |
| E20: golpe con brazo robótico | FALTA HACERLA | — |

## NOVA HUMANOIDES (GuardiaNova, E12, E18, E14)
| Animación | Estado | Clip actual / nota |
|---|---|---|
| GuardiaNova caída del cielo / aterrizaje / levantarse | YA EXISTE Y APROBADA | Guardia_Caida_* (propios) |
| E18 caída / aterrizaje / levantarse | YA EXISTE Y APROBADA | E18_Caida_* (propios) |
| E12 caída del cielo | USA CLIP PRESTADO | usa Guardia_Caida_* |
| E18 caminar / correr / golpe | YA EXISTE Y APROBADA | E18_Caminar/Correr/Golpe_Der (20-nov) — falta conectarlos en Override_E18 |
| Caminar apuntando 4 dir (Nova) | USA CLIP PRESTADO | PJ01_Loco_CaminarDer/Izq |
| Disparar arma Nova | YA EXISTE Y APROBADA | Guardia_Disparar, E18_Disparar |
| Recargar, cubrirse | USA CLIP PRESTADO | PJ01 |
| Daño, stagger | USA CLIP PRESTADO | PJ01 / genéricos |
| Muerte balazo | EXISTE PERO FALTA PERFECCIONAR | Guardia_Muerte, E18_Muerte; Muerte_Atras prestada de NPC |
| Muerte por explosión | FALTA HACERLA | — |
| E14 jefe: entrada, ataques especiales, golpeado, fase herida, muerte | USA CLIP PRESTADO | Override_Jefe es casi todo PJ01; existen Jefe_* genéricos (Ataque, Burla, Caminar, Idle, Muerte) |

## ROBOTS DE NOVA
| Animación | Estado | Clip actual / nota |
|---|---|---|
| E15 caminar, correr, disparar, impacto, muerte, cubrirse | YA EXISTE Y APROBADA | E15_* |
| E15 encenderse, caída del cielo, girar, golpe, tambaleo, cojear, muerte con chispas | FALTA HACERLA | caída prestada de Guardia |
| E16 caminar, caminar con escudo, girar, cañón, golpe escudo, impacto, muerte | YA EXISTE Y APROBADA | E16_* (CaminarEscudo aprobado 20-nov) |
| E16 caída del cielo con retumbe | FALTA HACERLA | prestada de Guardia |
| E16 bloquear con escudo, tambaleo, muerte de rodillas / explotando | FALTA HACERLA | — |
| E17 perro (rig del juego): patrullar, olfatear, correr, saltar, morder, ladrar, impacto, muerte, salir de cápsula | FALTA HACERLA | E17_* existentes son del rig anterior: rehacer todo |

## PERSONAJES CON NOMBRE
| Animación | Estado | Clip actual / nota |
|---|---|---|
| Hacker tecleando | YA EXISTE Y APROBADA | NPC_Hacker_Tecleando |
| Hacker voltear a ver al jugador | FALTA HACERLA | — |
| Mesera robot caminar con charola | YA EXISTE Y APROBADA | NPC_Mesera_Charola |
| Mesera servir y esperar | FALTA HACERLA | — |
| Doc curar / instalar implante / hablar con gestos | FALTA HACERLA | — |
| Tino banco de armas / entregar arma | FALTA HACERLA | — |
| Canepa trabajar en el carro | FALTA HACERLA | — |
| Aliado subir/bajar carro, seguir, cubrirse y disparar | USA CLIP PRESTADO | PJ01 |

## PENDIENTES DE LA REVISIÓN CON TONY (siguen pendientes)
| Pendiente | Estado | Por qué |
|---|---|---|
| V01a / V01b / V05 versión por piezas desde el original | PENDIENTE DE LA REVISIÓN CON TONY | Están armados en `revision_vehiculos_arreglados.blend`, no exportados; Tony dijo “luego si sobra tiempo los arreglamos al final”. |
| V02, V04, V09, V23 huecos de puertas recortados | PENDIENTE DE LA REVISIÓN CON TONY | Falta que Tony diga si se quedan o se regresan. |
| Detalle fino de carros | PENDIENTE DE LA REVISIÓN CON TONY | Tony: al final si sobra tiempo. |
| E18: conectar E18_Caminar/Correr/Golpe_Der y rehacer Avatar | PENDIENTE DE LA REVISIÓN CON TONY | Tarea de Unity (ver `tareas-unity-robots-nova-20nov`). |
| E16 cañón en IA | PENDIENTE DE LA REVISIÓN CON TONY | Tarea de Unity. |
| Bisagra de cofre PA01/PA02/V22 | PENDIENTE DE LA REVISIÓN CON TONY | Tarea de Unity. |


---
## CLIPS NUEVOS DE ESTA NOCHE (borradores compuestos de captura existente — NO están en Assets hasta que Tony apruebe)
Método: se toma una captura existente de PJ01 (K_MuerteEspaldas, K_MuerteFrente, K_DanoFrente, K_DanoIzq), se reacomoda el tiempo y se agrega trayectoria de cadera (vuelo, rebote, giro). Exportados en `imageassets/blender/pj01_clips/`. Hojas de 10 cuadros de lado y de frente en `_REVISION_TONY/hojas_clave/<clip>/`. Fps de escena: 24 (igual que todos los clips anteriores; el pedido decía 30 — **confirmar con Tony**, cambiarlo afecta a todos).

| # | Clip | Base | Frames | Loop | Avance | Momento clave | Termina |
|---|---|---|---|---|---|---|---|
| 1 | PJ01_Muerte_Explosion | K_MuerteEspaldas | 56 | no | ~3.1 m hacia atrás | despega f1, toca piso f18, rebote f19–24 | en el piso |
| 2 | PJ01_Muerte_Atropello | K_MuerteEspaldas | 54 | no | ~4.0 m, gira ~150° | impacto f1, toca piso f15 | en el piso |
| 3 | PJ01_Muerte_CaidaAltura | K_MuerteFrente | 42 | no | 0 | impacto f1–5, cuerpo en piso f8 | en el piso |
| 4 | PJ01_Derribo_Explosion | K_MuerteEspaldas | 40 | no | ~1.8 m atrás | toca piso f14 | en el piso → encadenar `PJ01_Dano_Levantarse_BocaArriba` |
| 5 | PJ01_Dano_Empujon | K_DanoFrente | 26 | no | ~0.55 m atrás | empujón f2 | de pie |
| 6 | PJ01_Dano_ExplosionCercana | K_DanoIzq | 30 | no | ~0.25 m | sacudida f4–5 | de pie |

## ORDEN DE REVISIÓN PARA TONY
Blender: `revision_PJ01_animaciones.blend`, colección **REV_ANIM_PJ01** (objeto PJ01_REV + figura de 1.80 m). Todos los clips en fila en el NLA; marcadores `R:<clip>` en la línea de tiempo.

| # | Clip | Estado | Qué hice | Qué me preocupa | Qué sigue pendiente |
|---|---|---|---|---|---|
| 1 | PJ01_Muerte_Frente / Espaldas / Desmayo | YA EXISTE | nada | — | — |
| 2 | PJ01_Muerte_Explosion | NUEVO (borrador) | vuelo de 3 m con arco, inclinación y rebote | el cuerpo se pone horizontal muy pronto (parece “sentado en el aire”); brazos no reaccionan al vuelo | con captura real de Mixamo quedaría mejor |
| 3 | PJ01_Muerte_Atropello | NUEVO (borrador) | vuelo bajo de 4 m con giro y rodada | el giro es de todo el cuerpo rígido, no hay rodada real | idem |
| 4 | PJ01_Muerte_CaidaAltura | NUEVO (borrador) | muerte de frente acelerada al impacto + rebote | primer cuadro arranca ~6 cm sobre el piso (viene de la caída) | — |
| 5 | PJ01_Derribo_Explosion + Levantarse_BocaArriba | NUEVO (borrador) | vuelo corto, cae boca arriba y se levanta | posible salto de posición entre los dos clips (desplazamiento en cadera) | probar en Unity con root motion |
| 6 | PJ01_Dano frente/espalda/izq/der | YA EXISTE | nada | el override del Jugador usa los genéricos de Mixamo, no estos | cambiar en Override_Jugador |
| 7 | PJ01_Dano_Empujon | NUEVO (borrador) | daño de frente con retroceso de 0.55 m | pies pueden deslizar un poco al retroceder | — |
| 8 | PJ01_Dano_ExplosionCercana | NUEVO (borrador) | daño lateral con sacudida y giro | — | — |
| 9 | Todo lo marcado FALTA HACERLA arriba | PENDIENTE | — | sin captura fuente no se puede hacer a calidad comercial | lista de Mixamo abajo |

## LISTA PARA DESCARGAR DE MIXAMO (FBX, “Without Skin”, 30 fps, sin “In Place” salvo que se indique)
Ponerlas en `imageassets/blender/_mixamo/` y yo las paso a cada rig.
- **PJ01 muertes/daño:** Dying (Backwards), Falling Back Death, Flying Back Death, Hit Reaction (x4), Stunned, Getting Up, Knocked Out, Falling To Landing, Hard Landing.
- **Vehículos:** Entering Car, Exiting Car, Pulling Out Of Car / Getting Pulled Out (si no, “Push” + “Fall Flat”), Driving, Jump Off (rodar).
- **Armas:** Reloading (pistol, rifle, shotgun), Throw Grenade (Throwing), Crouch Shooting, Weapon Swap.
- **Interacción:** Drinking, Eating, Sitting (silla), Arrest / Surrender (Hands Up), Kneeling, Typing, Talking (x3), Pointing, Arguing, Sweeping, Phone (filming), Waving.
- **NPC miedo/vida:** Terrified, Cower, Hands Up, Laying Down (tirarse), Injured Walk, Crawling (arrastrarse herido), Sitting Idle.
- **Policía:** Rifle Aiming Walk (4 dir), Radio, Rappelling, Cover Idle/Peek.
- **Pandilleros:** Taunt, Yelling, Pushing, Mma Kick, Gun Threat (Pistol Aim Idle).
- **Nova/robots humanoides:** Stagger (Hit Reaction Big), Death From Explosion, Super Hero Landing (caída del cielo), Kneeling Death.
- **Jefe:** Boss Intro (Roaring / Flexing), Heavy Attack (x2), Injured Idle.
- **Perro E17 (cuadrúpedo):** Mixamo no tiene cuadrúpedos; hay que animarlo en su rig o conseguir paquete de animal.


## PENDIENTES AGREGADOS POR TONY (21-nov tarde)
| Pendiente | Estado | Nota |
|---|---|---|
| Arreglar las animaciones de pelea del jugador (PJ01: golpes, guardia, patada) | PENDIENTE | Pedido de Tony durante la revisión de Mixamo. |
| Pulir pasos de PJ01_Carro_SacarConductor (f30-98) | PENDIENTE | Tony: 'déjalo así, al final se corrige'. |
