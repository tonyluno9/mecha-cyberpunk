# Plan: enemigos nuevos

Solo plan. No hay código ni arte hecho. Los nombres y números son propuesta de Claude; Tony decide.

## Lo que ya hay (para no repetir)
- **Pandilleros a pie** (`IA/APie/`): `PandilleroAPie`, `PandillaAPie` (turnos: `maxAtacando`), `PuestosDeCubierta`, `PistolaPandillero`.
  Los datos viven en `DatosPandilleroAPie_*.asset`. Hay Calaveras (SKULLS), Víboras (SNAKES), Policía y jefes.
- **SKULL base**: 60 de vida, golpe de 10, pistola de 8 por disparo, alcance de golpe 1.8 m, 40 Ƀ al vencerlo. Los Policía tienen 80 de vida.
- **Jefe de Nova** (`ReglaJefeNova`): 400 de vida, golpe en área, carga en línea recta, se queda recuperándose (se le castiga ahí).
- **Guardias de Nova** (`Personaje/Visual/GuardiaNova.cs`): hoy son ambiente en Neón Alto. Patrullan, vigilan, gritan y suben el calor. No pelean.
- **Drones de policía** (`Trafico/Persecucion/DronMundo.cs`, `CalculoTiroDron.cs`): llegan con el calor; desde nivel 2 disparan.
- **Historia** (`docs/diseno/pandillas.md`): NOVA arma a los SNAKES. Los SKULLS dominan El Tianguis, los SNAKES Neón Alto. La corporación está ligada a tus papás.
- **Fase**: según `docs/estado.md`, Tony va en T-F5. Estos enemigos son para después de la demo (T-F7) salvo que se diga otra cosa.

Los cuatro enemigos nuevos suben el nivel poco a poco: primero un tercer bando de calle, luego la corporación sale a la luz.

---

## 1. LOS CUERVOS (tercera pandilla, nombre provisional)

### Quiénes son
Pandilla de las orillas, sin zona fija. Venden vigilancia y cobran "peaje" a los que cruzan sus calles. Los SKULLS y los SNAKES los odian.
Dan al jugador algo que ahora falta: una pelea donde nadie es amigo. Sirven para mover el mapa en el juego libre.

### Cómo pelean
- **Armas**: escopeta de cerca y bate eléctrico. Sin pistola.
- **Comportamiento**: patrullan en pares. Al verte gritan, se separan y te rodean por los lados (un SKULL se queda en su cobertura; un Cuervo no).
  Si les quitas a uno, el otro huye a pedir ayuda y vuelve con tres más.
- **Vida**: 45 (menos que un SKULL). Golpean más fuerte: escopeta 5 perdigones de 6.
- **Punto débil**: la escopeta pierde fuerza de lejos. Recargan lento (2.5 s): ese es el momento de salir y disparar.
- **Táctica que piden**: mantener distancia y moverte. No sirve quedarse detrás de una cobertura fija; te rodean.

### Dónde salen
- Orillas del mapa y calles entre zonas. De noche en grupos de 3 a 4; de día solo en pares.
- Evento "peaje": bloquean una calle y cobran Ƀ50 o pelean.
- Fase: T-F7 (primera tanda de contenido libre).

### Arte
| Ya existe | Falta (pedido de arte) |
| --- | --- |
| Modelos `SkullHombre`, `MunecoHumano`, `man` (se recolorea el `SkullHombre`) | Un modelo propio o una variante con chamarra y máscara de cuervo (se puede hacer como textura nueva sobre `SkullHombre`) |
| Escopeta `W02_Escopeta` | Bate eléctrico (arma cuerpo a cuerpo con luz) |
| Animaciones: `Loco_*`, `Rifle_*`, `Punos_*`, `Dano_*`, `Muerte_*`, `Cobertura_*` | Animación de "gritar y señalar" y de "huir corriendo" (se puede usar `Loco_Correr` al inicio) |
| | Efecto: chispas azules del bate. Sonido: grito de pandilla, escopeta recortada |

### Código
- `IA/APie/`: una regla nueva de "rodear" (flanqueo) junto a `ReglaTacticaPandillero`, y la orden de huir a pedir refuerzos.
- `IA/Datos/`: `DatosPandilleroAPie_Cuervos.asset`, con campos de escopeta (ya existe `perdigones`).
- `Personaje/Combate/`: el bate puede salir como golpe con más daño; nada nuevo si se usa `danioGolpe`.
- Core: necesita una pandilla nueva en `IdPandilla` y respeto propio. **Sí necesita tarea de Core** (la hace Tony).

---

## 2. GUARDIA BLINDADO DE NOVA

### Quiénes son
La seguridad privada de NOVA, la que hoy solo patrulla en Neón Alto. Cuando el calor sube o descubres cosas de Nova, dejan de gritar y pelean.
Muestran que la corporación manda sobre la policía. Ligan con el misterio de tus papás: cuidan los lugares donde están las pistas.

### Cómo pelean
- **Armas**: fusil (`W03_Fusil`) y escudo de barrido (`Nova_Barrido`, `Nova_Punch` ya existen como golpes).
- **Comportamiento**: patrullan, entran en alerta con el megáfono (ya existe), avanzan en línea y se cubren detrás de su escudo. Llaman a un dron.
- **Vida**: 120 de armadura más 60 de vida (el doble que un SKULL). La armadura se rompe antes que la vida.
- **Punto débil**: la espalda y las piernas. De frente reducen el daño a la mitad. Se sobrecalientan el fusil tras 12 disparos y paran 2 s.
- **Táctica que piden**: flanquear. Ir de frente gasta munición. Disparar a la espalda o usar un golpe fuerte para romperles la guardia.

### Dónde salen
- Neón Alto (ya están de ambiente), entradas de edificios de Nova, el casino VIVACOIN a calor 3.
- Siempre de día y de noche. Aparecen solo si Nova te marcó (misión de Nova o calor 3).
- Fase: T-F7, después de que el HACKER revela que NOVA arma a los SNAKES.

### Arte
| Ya existe | Falta (pedido de arte) |
| --- | --- |
| Modelo `GuardiaNova.fbx` con su `.controller` | Armadura y casco con visera (variante del modelo) |
| Animaciones `Nova_Idle`, `Nova_Caminar`, `Nova_Correr`, `Nova_Punch`, `Nova_Barrido`, `Nova_Muerte`, `Nova_Flexing` | Escudo de mano (modelo) |
| Fusil `W03_Fusil`, `Rifle_*` (apuntar, correr, recargar) y `Cobertura_*` | Animación de "avanzar con escudo" y de "guardia rota" (aturdido) |
| `Dano_*`, `Muerte_*` | Efecto: chispas al pegar en la armadura. Sonido: pasos pesados, megáfono (ya hay) |

### Código
- `IA/APie/`: nuevo pandillero con **armadura** (un segundo medidor antes de la vida) y daño reducido por frente.
- `Combate/` o `Personaje/Combate/`: que el daño sepa de qué lado llega. Hoy `IRecibeDanio` quizá no lo dice. **Probablemente tarea de Core** para añadir la dirección del golpe al contrato de daño.
- `Personaje/Visual/GuardiaNova.cs`: el guardia de ambiente pasa a combate al subir la alerta (hoy solo llama a la policía).

---

## 3. DRON DE ATAQUE DE NOVA

### Quiénes son
Máquinas de Nova que patrullan sobre sus edificios y te persiguen si entras sin permiso. No son de la policía: no se van al bajar el calor.
Hacen sentir que la corporación te vigila desde arriba.

### Cómo pelean
- **Armas**: ráfaga corta de 3 disparos y un láser de aviso (se ve la línea 0.6 s antes del tiro).
- **Comportamiento**: vuela en círculo, se fija en ti, dispara, se aleja a reposicionarse. Con varios, se reparten alturas (ya hay `CalculoSeparacionDrones`).
- **Vida**: 30. Muere fácil pero es difícil de golpear porque se mueve. Daño por tiro: 6.
- **Punto débil**: el centro rojo. Cuando recarga se queda quieto 1.5 s. Explota y hiere cerca.
- **Táctica que piden**: apuntar al aire con precisión y usar la cobertura techada. No sirve correr en línea recta.

### Dónde salen
- Techos y patios de Nova, y como refuerzo de guardias blindados. Solo de noche en las calles abiertas; en interiores de Nova siempre.
- Eventos: llegan en grupos de 2 al entrar a una zona restringida. Con calor 3 llegan junto con los policías.
- Fase: T-F7. Es el más barato de hacer, por eso va primero.

### Arte
| Ya existe | Falta (pedido de arte) |
| --- | --- |
| `Arte/Calle/CY07b_Dron` (modelo) y `CY07a_PlataformaDron` | Variante con color de Nova y luz roja |
| `DronMundo` ya vuela, orbita y dispara | Animación de giro de hélices y de caída |
| | Efecto: láser de aviso, explosión pequeña, chispas. Sonido: zumbido, disparo, explosión |

### Código
- `Trafico/` ya tiene el dron de policía (`DronMundo`, `CalculoTiroDron`). **Reutilizar** y añadirle "bando" (Nova o policía) y "nunca se retira por el calor". Esto es del área de Alan (Trafico/), así que la tarea se la pasa a él o se pide por contrato en Core.
- Contrato de Core sugerido: una forma de decir "quién me manda" (policía, Nova) en el evento de calor, sin que IA y Trafico se referencien entre sí.

---

## 4. MINI JEFE: EL SARGENTO "VERDUGO" DE NOVA

### Quiénes son
Jefe de seguridad de Nova, con exoesqueleto. Está a un paso del jefe grande de Nova (400 de vida). Guarda una pista de tus papás: una tarjeta o un registro.
Es la prueba de que Nova te conoce. Va al final de una misión, no en la calle.

### Cómo pelean
- **Armas**: martillo hidráulico (golpe en área) y granadas de aturdimiento.
- **Comportamiento**: dos fases.
  - Fase 1 (vida llena a la mitad): avanza, telegrafía el martillazo con luz roja (0.8 s), golpea, se queda recuperando 1.5 s.
  - Fase 2 (menos de la mitad): llama a 2 guardias blindados y 1 dron, usa granadas y su carga se vuelve más rápida.
- **Vida**: 250, con armadura de 150 que se rompe en la fase 1. Mucho menos que el jefe de Nova.
- **Punto débil**: el generador de la espalda (se ilumina tras cada golpe fallado). Un golpe aquí quita 3 veces.
- **Táctica que piden**: leer el patrón, esquivar, castigar el momento de recuperación y priorizar los refuerzos en fase 2.

### Dónde salen
- Un solo encuentro fijo: el lobby de una torre de Nova, a mitad de la historia de Nova. Solo de día (horario de oficina).
- Aparece tras la misión "Entra a la torre". Se puede repetir como reto en el juego libre cuando se desbloquea.
- Fase: T-F7, al final, cuando ya existan guardias blindados y drones.

### Arte
| Ya existe | Falta (pedido de arte) |
| --- | --- |
| Modelo `RobotHombre` o `GuardiaNova` como base | Modelo propio del Verdugo (más grande, con exoesqueleto) |
| Animaciones `Jefe_Idle`, `Jefe_Caminar`, `Jefe_Ataque`, `Jefe_Burla`, `Jefe_Muerte` y `Nova_*` | Martillazo (golpe de arriba hacia abajo), lanzar granada, aturdido y tomar impulso para cargar |
| Patrón de jefe `ReglaJefeNova` (golpe, carga, recuperación) | Martillo hidráulico (arma). Efecto: onda de suelo, chispas del generador. Sonido: martillo, motor del exo, voz |

### Código
- `IA/APie/`: una regla nueva `ReglaVerdugo` que copie el estilo de `ReglaJefeNova` y le añada la fase 2 (llamar refuerzos, granadas).
- `Pelea/` o `Progresion/`: el encuentro lo dispara una misión; la recompensa va por el servicio de objetivos que ya existe (T-055).
- Core: no hace falta si se reutilizan eventos de objetivo y de daño. Si se quiere una barra de jefe, el HUD (UI/) la lee del `IRecibeDanio`.

---

## Resumen

| Enemigo | Dificultad (1-5) | Zona | Fase | Arte nuevo |
| --- | --- | --- | --- | --- |
| Dron de ataque de Nova | 2 | Techos y patios de Nova, Neón Alto | T-F7 | Poco: variante de color, efectos y sonidos |
| Los Cuervos | 3 | Orillas del mapa, calles entre zonas | T-F7 | Medio: textura nueva, bate, 2 animaciones |
| Guardia blindado de Nova | 4 | Neón Alto, VIVACOIN, edificios de Nova | T-F7 | Medio: armadura, escudo, 2 animaciones |
| Verdugo (mini jefe) | 5 | Lobby de una torre de Nova | T-F7 (al final) | Alto: modelo, martillo, 4 animaciones |

## Orden recomendado

1. **Dron de ataque de Nova**. El código y el modelo ya existen; casi todo es un cambio de datos. Es la forma más barata de probar "enemigo con bando de Nova" y el contrato de bando en Core.
2. **Los Cuervos**. Casi no pide arte (recolor y bate) y prueba el flanqueo, que luego usa el guardia. Obliga a abrir una pandilla nueva en Core, mejor hacerlo temprano.
3. **Guardia blindado de Nova**. Necesita armadura y dirección del golpe (Core), que ya estarán probadas con los Cuervos. Usa el modelo y las animaciones de Nova que ya hay.
4. **El Verdugo**. Junta todo lo anterior (guardias, dron, armadura) y es el que más arte pide. Va al final para que el arte tenga tiempo.

## Decisiones que necesitan a Tony
- Nombre definitivo de la tercera pandilla (Cuervos es provisional).
- Si los drones de Nova los toma Alan (Trafico/) o se crea un contrato para que Tony pueda hacerlos desde IA/.
- Si el Verdugo va dentro de la demo o solo después.
