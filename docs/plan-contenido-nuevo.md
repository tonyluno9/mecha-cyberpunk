# Plan: contenido nuevo (enemigos, armas y prótesis de ataque)

Solo plan. No hay código ni arte hecho. **Todos los números son PROPUESTAS de Claude; Tony decide y se afinan jugando.**
Códigos de arte según `docs/pedido-arte-14nov.md`. Este plan reemplaza a los enemigos de `docs/plan-enemigos-nuevos.md`
(Cuervos, Guardia blindado, Dron, Verdugo) y reutiliza sus ideas: el guardia blindado pasa a ser E12, el dron a E13 y el Verdugo
se vuelve el jefe final E14; los Cuervos se descartan porque ahora el tercer bando son los SNAKES con más poder.

## 0. Datos actuales que usé como base (leídos del repo)

| Qué | Valor actual | Dónde |
| --- | --- | --- |
| SKULL / SNAKE (pandillero) | 60 de vida, pistola 8 por disparo, golpe 10, velocidad 4.5, alcance de tiro 12 m, Ƀ40, grupos de 3 | `IA/Datos/DatosPandilleroAPie_Calaveras/Viboras.asset` |
| Guardia NOVA / Policía | 80 de vida, pistola 8, Ƀ60 | `DatosPandilleroAPie_Nova/Policia.asset` |
| JEFE SKULL | 250 de vida, 8 perdigones de 6, Ƀ300 | `DatosPandilleroAPie_JefeCalaveras.asset` |
| LA CULEBRA (jefe SNAKES) | 300 de vida, 10 perdigones de 6, Ƀ400 | `DatosPandilleroAPie_JefeViboras.asset` |
| CAPITÁN NOVA (jefe de Nova) | 400 de vida, escopeta 8 de 8, golpe en área y carga, Ƀ600 | `DatosPandilleroAPie_JefeNova.asset` (`ReglaJefeNova`) |
| Dron de policía | 40 de vida, 4 de daño cada 1.5 s, vuela a 9 m/s a 7 m de altura, dispara desde calor 2 | `Trafico/Datos/ConfigPersecucion` (`DronMundo`) |
| Misil de habilidad (mecha) | 20 de daño, alcance 25 | `Combate/Datos/Habilidades/DatosHabilidad_Misil.asset` |

Armas a pie actuales (nivel 0 entre paréntesis nivel III). Daño por bala; DPS teórico = daño x perdigones / tiempo entre disparos:

| Arma | Precio | Daño | Cadencia (s) | Cargador | Recarga (s) | Alcance (m) | Auto | DPS teórico |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Pistola (W01) | Ƀ150 (500) | 25 (44) | 0.25 | 8 (15) | 1.4 (0.8) | 60 | no | 100 (176) |
| Escopeta (W02) | Ƀ350 (700) | 9 x 8 (16 x 8) | 0.9 | 4 (8) | 2.4 (1.4) | 20 | no | 80 (142) |
| Fusil (W03) | Ƀ700 (850) | 15 (25) | 0.11 | 30 (40) | 2.0 (1.3) | 70 | si | 136 (227) |
| Francotirador (W04) | Ƀ900 | 70 (120) | 1.2 | 4 (7) | 2.8 (1.9) | 150 | no | 58 (100) |

Munición: escopeta paquete de 8 (máx 40), fusil 60 (máx 240, Ƀ120 el paquete), sniper 10 (máx 40). Los precios de mejora (I a III) están en
`Economia/Datos/DatosOferta_Arma_*`. Un SKULL (60 de vida) cae con 3 balas de pistola o 4 de fusil.

**Historia que existe hoy** (`docs/diseno/pandillas.md`, `docs/guia-para-jugar.md`): Suburbios (casa, Doña Lupe, CHATO) -> pistola con EL INVENTOR (TINO) ->
El Tianguis y el jefe SKULL -> EL HACKER (SERGIO) en Neón Alto revela que NOVA les paga y les da armas a los SNAKES -> CONTINUARÁ.
En juego libre Neón Alto se conquista con la CULEBRA. El hilo mayor (`concepto-historia.md`): el misterio de tus papás, ligado a una corporación (NOVA).
No hay capítulos escritos con nombre; abajo uso **Capítulo 2, 3 y 4 como etiquetas propuestas** para las fases después de la demo:

| Etiqueta | Contenido | Estado |
| --- | --- | --- |
| Cap. 1 | Suburbios, Tianguis, jefe SKULL, HACKER (la demo) | existe |
| Cap. 2 (propuesto) | Guerra con los SNAKES: Neón Alto, VIVACOIN, jefe SNAKES | nuevo |
| Cap. 3 (propuesto) | NOVA sale a la luz: calles con guardias, torre de Nova | nuevo |
| Cap. 4 (propuesto) | La sede de Nova y el director de seguridad (jefe final) | nuevo |

---

## 1. Enemigos nuevos

### Resumen de balance (propuesta)

| Código | Nombre | Vida | Daño | Velocidad (m/s) | Alcance | Recompensa | Comparado con |
| --- | --- | --- | --- | --- | --- | --- | --- |
| (base) | SNAKE actual | 60 | 8 por bala | 4.5 | 12 m | Ƀ40 | referencia |
| E10 | Pandillero SNAKES | 90 | ráfaga 3 x 6 (18); machete 14 | 5.5 | 14 m / 2.0 m | Ƀ75 | 1.5x la vida de un SKULL |
| E11 | Jefe SNAKES (mini jefe) | 450 | cuchilla 25; embestida 30; granada 20 | 5.0 (embestida 11) | 2.5 m / 15 m | Ƀ900 | LA CULEBRA 300; Capitán Nova 400 |
| E12 | Guardia pesado de Nova | 80 + 120 de armadura (200) | ráfaga 4 x 8; escudazo 20 | 3.5 | 12 m / 2.0 m | Ƀ150 | Guardia Nova 80 |
| E13 | Dron de asalto | 70 | ráfaga 3 x 5 cada 2.0 s; explosión 15 | 8 | 20 m | Ƀ50 | dron de policía 40 de vida, 4 de daño |
| E14 | Jefe final de Nova | 1200 en 3 fases (escudo de energía de 400 aparte) | cañón 10 por bala; misil 25; pisotón 40 | 3.0 | 25 m / 4 m | Ƀ3000 | Capitán Nova 400 |

Derivación: E10 = 1.5 x 60 (SKULL). E11 sube sobre LA CULEBRA (300) para ser el pico del Cap. 2 sin pasar al de Nova. E12 = 2.5 x 80 (guardia Nova)
dividido en armadura y vida. E13 = 70 contra 40 del dron policía (casi el doble), pero 5 de daño (4 del dron) con ráfagas más raras. E14 ~3 x Capitán Nova (400).
Para que valga la pena pelear: con fusil (136 DPS) E10 cae en ~1 s de fuego continuo; E12 en ~2 s tras romperle el escudo; E11 en ~4 s de DPS sostenido; E14 pide ~10 s sostenidos
más romper el escudo. Con pistola (100 DPS) multiplica por 1.4.

### E10 — Pandillero SNAKES

- **Quiénes son**: la gente de la CULEBRA, armada por NOVA. Chamarra con escamas, tatuajes, máscara de tela, verde y negro. Reemplaza al SNAKE actual como "soldado raso 2.0" en Neón Alto (el SNAKE actual queda como variante débil del Cap. 1).
- **Cómo pelea**: usa subfusil (ráfaga de 3 balas de 6, mismo modelo de `PandilleroAPie` con `perdigones` y `danioDisparo`) y se mueve mucho: sale de cobertura, rueda hacia un lado y vuelve. Con el jugador a menos de 4 m saca un machete (golpe 14).
  Turnos de grupo con `maxAtacando` 2 de 4 (grupos de 4).
- **Punto débil**: recarga lenta (2.5 s, se queda quieto detrás de la cobertura); la máscara de tela no cubre la cabeza (si hay impacto por cabeza, x1.5; confirmar si el sistema de golpes distingue zona).
- **Cómo se le gana**: no quedarse parado; castigarlo cuando recarga; las granadas y la escopeta lo limpian de cerca.
- **Dónde aparece**: Cap. 2, Neón Alto (calles y azoteas), VIVACOIN (afuera), puestos de la CULEBRA; refuerzo de E11. Grupos de 3 a 4. Desde Cap. 3 también como aliados armados por Nova en emboscadas al jugador.
- **Soltar**: Ƀ75 y 40% de soltar un **subfusil W05** (al inicio, para que el jugador lo consiga por botín).

| Reutiliza | Falta |
| --- | --- |
| Modelo base: `SkullHombre`/`Mixamo` (se hace nuevo con Tripo E10). Clips `Skull_Idle`, `Skull_Caminar`, `Skull_Burla`; `Rifle_*` (apuntar, correr, disparar caminando, recargar); `Cobertura_Entrar/Disparo/Disparo2`; `Dano_Frente/Espalda/Fuerte`; `Muerte_*`; `Loco_Rodar`; `Loco_Correr`; `Punos_GolpeDer` (el machete mientras no haya clip propio) | Modelo E10 (Tripo + rig Mixamo). Clips propuestos: `Snake_Machete_Tajo` (un golpe diagonal), `Snake_Esquivar_Lateral_Der` y `_Izq` (o `Loco_Rodar` mientras), `Snake_Gritar` (aviso). Material verde `#73FF1A` y máscara. Sonido: grito, ráfaga corta |

Código: `IA/APie/` (variante de `PandilleroAPie`: ráfaga de 3 y esquive), `IA/Datos/DatosPandilleroAPie_SnakesE10.asset`. Core: **no hace falta** (usa `IdPandilla.Viboras`).

### E11 — Jefe SNAKES (mini jefe)

- **Quiénes son**: lugarteniente de LA CULEBRA, grande, abrigo largo y brazo cibernético con cuchilla (regalo de NOVA). Es el tope del Cap. 2: se pelea al final de la conquista de Neón Alto y antes de que el HACKER revele lo de Nova ("mira, su brazo dice NOVA"). LA CULEBRA queda como jefe del Cap. 1 y 2; E11 la **reemplaza en la conquista de Neón Alto** (decisión abierta abajo).
- **Cómo pelea** (patrón copiado de `ReglaJefeNova`: aviso, golpe, recuperación):
  - Fase 1 (100% a 50%): camina hacia ti con la cuchilla; **corte horizontal** con aviso (el brazo brilla verde 0.7 s, 25 de daño, 2.5 m de arco, recupera 1.2 s); **embestida** en línea recta de 6 m (aviso con chispas en el piso, 30 de daño, queda aturdido 1.5 s si choca con pared o cobertura). Cada 20 s llama a 2 E10.
  - Fase 2 (menos de 50%): más rápido (+20%), añade **granada de humo y fragmentos** (20 de daño en 2.5 m, la lanza a tus pies) y la embestida se encadena 2 veces.
- **Punto débil**: el brazo cibernético se sobrecalienta tras 3 cortes seguidos y se abre (2 s). Daño x1.5 mientras brilla. Ahí entra la escopeta o el revólver W06.
- **Cómo se le gana**: leer los avisos, esquivar el corte lateral, hacerlo chocar con una cobertura, disparar al brazo y limpiar los E10 de refuerzo.
- **Balance**: 450 de vida, Ƀ900, 40% de soltar el **lanzagranadas W08** la primera vez (garantizado la primera; luego solo se compra).
- **Dónde**: un encuentro fijo, Cap. 2, final de la misión "Termina con los SNAKES" en Neón Alto (zona de VIVACOIN / azotea del club). Rematch opcional en juego libre.

| Reutiliza | Falta |
| --- | --- |
| Clips de jefe `Jefe_Idle`, `Jefe_Caminar`, `Jefe_Ataque`, `Jefe_Burla`, `Jefe_Muerte`; `Dano_Fuerte`; `Nova_Barrido` (movimiento amplio) | Modelo E11 (Tripo). Clips: `JefeSnakes_Corte` (tajo horizontal), `JefeSnakes_Embestida_Inicio`/`_Bucle`, `JefeSnakes_Aturdido`, `JefeSnakes_Granada` (lanzar), `JefeSnakes_Llamar` (puede reusar `Jefe_Burla`). Efecto: brillo del brazo, sobrecalentamiento. Sonido: cuchilla, motor del brazo |

Código: `IA/APie/ReglaJefeSnakes` (hermana de `ReglaJefeNova`), `Pelea/` para la barra de vida y el cierre del encuentro, `Progresion/Misiones/` para la misión. Core: no.

### E12 — Guardia pesado de Nova

- **Quiénes son**: el escalón que sigue al `GuardiaNova` de ambiente. Armadura completa blanca y azul, casco cerrado, escudo antidisturbios. Aparece cuando NOVA te marca. Muestra que la corporación manda sobre la policía.
- **Cómo pelea**: avanza en línea con el escudo al frente (3.5 m/s), dispara ráfagas de 4 balas de 8 con un fusil, y de cerca da **escudazo** (20 de daño y empuje). Los escudos protegen en **cono frontal**: reducen 80% del daño de bala que viene de frente (`InfoDanio.Direccion` ya lo permite; **no hace falta Core**).
  Grupos de 2, o 1 junto a 2 guardias normales. Llama a un E13 (dron) una vez por pelea.
- **Armadura**: 120 puntos que se agotan antes de la vida. La armadura es nueva (campo extra en `DatosPandilleroAPie`) y se acaba con daño explosivo y plasma al doble.
- **Puntos débiles**: espalda y piernas (x1.5 por la espalda). Después de 12 disparos el fusil se sobrecalienta (para 2 s). Explosiones ignoran el escudo.
- **Cómo se le gana**: flanquear (rodear), usar granadas W08 o el plasma W07 (W07 ignora la mitad del escudo, ver sección 2), romper el escudo con el revólver W06 (cada disparo de 55 pega fuerte aunque reduzca), o usar el golpe fuerte para aturdirlo (`Punos_Fuerte`).
- **Dónde**: Cap. 3: calles de Neón Alto cuando el calor es 3 o tras la misión de Nova; entradas de edificios de NOVA; lobby de la torre. Sustituye a los guardias de ambiente cuando suben la alerta (`GuardiaNova` pasa a combate).
- **Soltar**: Ƀ150 y 35% de soltar un **rifle de plasma W07** (se puede comprar después).

| Reutiliza | Falta |
| --- | --- |
| `GuardiaNova.fbx` y su controlador como base; `Nova_Idle`, `Nova_Caminar`, `Nova_Correr`, `Nova_Punch`, `Nova_Barrido`, `Nova_Muerte`, `Nova_Flexing`; `Rifle_*`, `Cobertura_*`, `Dano_*`, `Muerte_*` | Modelo E12 (Tripo). Clips: `GuardiaPesado_AvanzarEscudo_Bucle` (caminar con escudo arriba), `GuardiaPesado_Escudazo` (puede reusar `Nova_Punch` provisional), `GuardiaPesado_Aturdido`, `GuardiaPesado_EscudoRoto`. Efecto: chispas en la armadura. Sonido: pasos pesados, el megáfono (ya hay) |

Código: `IA/APie/` (`PandilleroAPie` con armadura y escudo frontal), `Personaje/Visual/GuardiaNova.cs` (la alerta lo vuelve de combate). Core: no.

### E13 — Dron de asalto de Nova

- **Quiénes son**: dron de Nova (4 rotores, ametralladora abajo, luz roja). No es de la policía, no se va al bajar el calor.
- **Cómo pelea**: vuela en círculo a 6 m, se fija en el jugador, muestra un **láser rojo de aviso (0.5 s)** y dispara una ráfaga de 3 balas de 5 cada 2.0 s, luego se reposiciona. Con 2 o más se reparten alturas (`CalculoSeparacionDrones`). Sin ametralladora cuando lo derriban: cae girando y explota (15 de daño en 3 m, dañando a quien esté cerca, incluso otros enemigos).
- **Punto débil**: la luz roja de abajo (el centro). Recarga 2.5 s tras 3 ráfagas y se queda quieto 1.5 s. Los rotores se pueden disparar (cae con 70 de vida).
- **Cómo se le gana**: apuntar con precisión (arma automática o rifle), usar techos y cobertura, no correr en línea recta; el plasma W07 lo derriba en 4 disparos.
- **Dónde**: Cap. 3, como refuerzo de E12 y en techos y patios de NOVA. En interiores de Nova siempre. Con calor 3 llegan junto con los policías. En Cap. 4 patrullan la sede.
- **Soltar**: Ƀ50 y chatarra.

| Reutiliza | Falta |
| --- | --- |
| `DronMundo` (vuela, orbita, dispara, se derriba, explota, `CalculoTiroDron`), `CY07b_Dron` (modelo y colores), `CY07a_PlataformaDron` | Modelo E13 (Tripo, vista 3/4 con hélices separadas). **No necesita clips de Mixamo**: las hélices giran por script, la caída ya la hace `DronMundo`. Falta: variante "Nova" con bando y "no se retira por el calor" (Trafico/ es de Alan, ver sección 4); láser de aviso y sonidos |

### E14 — Jefe final de Nova (director de seguridad en exotraje)

- **Quiénes son**: el director de seguridad de Nova, en un exotraje de 3 m con cañones en los brazos y escudo de energía en el pecho. Guarda la pista final de tus papás. Reemplaza al "Verdugo" del plan anterior.
- **Cómo pelea** (3 fases; 1200 de vida más un escudo de energía de 400):
  - Fase 1 (100% a 66%): **escudo de energía** activo (400 de daño) con aviso azul; dispara cañones de los brazos en ráfagas de 10 balas de 10 con aviso láser; **pisotón** de área (40 de daño en 4 m, 1 s de aviso). Se queda quieto 1.5 s al recargar. El escudo se rompe, se apaga 10 s, se vuelve a cargar.
  - Fase 2 (66% a 33%): llama 2 E12 y 2 E13 y lanza **misiles** (3, 25 de daño, 2 m) en arco, avisados por marcas rojas en el piso; escudo ya no regresa solo.
  - Fase 3 (menos de 33%): modo furia, más rápido (+25%), sin escudo, cañones a doble cadencia; un **sobrecalentamiento** de la espalda (el generador) se expone 3 s tras cada pisotón fallado: x3 de daño.
- **Punto débil**: el generador de la espalda (se ilumina tras el pisotón) y el escudo cae con plasma W07 y misiles IMP05 (x2).
- **Cómo se le gana**: romper el escudo con armas pesadas, esquivar los avisos, usar las coberturas del hangar, limpiar los refuerzos, y castigar la espalda en la fase 3. Está pensado para que el jugador use algo de lo nuevo.
- **Dónde**: Cap. 4, un solo encuentro fijo en la sede de NOVA, final de la historia ("CONTINUARÁ" o cierre según se decida). No hay rematch hasta el juego libre.
- **Balance**: Ƀ3000, soltar una pieza única (decisión abierta).

| Reutiliza | Falta |
| --- | --- |
| Clips de jefe `Jefe_Idle/Caminar/Ataque/Burla/Muerte`, `Nova_Muerte`, `Nova_Flexing`; patrón `ReglaJefeNova` (golpe, carga, recuperación); barra de jefe en `Pelea/` | Modelo E14 (Tripo, 3 m: capsulas de colisión propias). Clips: `Final_Disparar_Brazos` (apuntar con dos cañones), `Final_Pisoton`, `Final_Escudo_Activar`, `Final_Misiles` (hombros abiertos), `Final_Aturdido`, `Final_Furia` (rugido) y muerte con explosión (`Nova_Muerte` provisional). Efecto: escudo de energía (shader), marcas en el suelo, explosión final. Sonido: motor del exotraje, voz del director |

Código: `IA/APie/ReglaJefeFinalNova` (hermana de `ReglaJefeNova`, con fases y refuerzos), `Pelea/` (encuentro), `Progresion/Misiones/` y `Historia/` (cierre). Core: si el escudo debe ser un tipo de daño distinto, **tarea de Core** para `TipoDanio.Energia` (ver sección 4).

---

## 2. Armas nuevas

Mismas reglas de datos que hoy: un `DatosArma_<arma>_<nivel>.asset` por nivel (0 a III) en `Personaje/Datos/Armas/`, un nuevo valor en `ArmaAPie`
(hoy Punos, Pistola, Escopeta, Fusil, Sniper), y precio y munición por `Economia/Datos`. **Números = propuesta.**

### Tabla comparativa (nivel 0; entre paréntesis la propuesta de nivel III)

| Arma | Precio | Daño | Cadencia (s) | Cargador | Recarga (s) | Alcance (m) | Auto | DPS | Notas |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Pistola (W01) | Ƀ150 | 25 | 0.25 | 8 | 1.4 | 60 | no | 100 | actual |
| Escopeta (W02) | Ƀ350 | 9 x 8 | 0.9 | 4 | 2.4 | 20 | no | 80 | actual |
| Fusil (W03) | Ƀ700 | 15 | 0.11 | 30 | 2.0 | 70 | si | 136 | actual |
| Francotirador (W04) | Ƀ900 | 70 | 1.2 | 4 | 2.8 | 150 | no | 58 | actual |
| **Subfusil (W05)** | Ƀ500 (850) | 10 (16) | 0.08 | 32 (45) | 1.6 (1.1) | 35 | si | 125 (200) | ráfaga rápida, poco alcance |
| **Revólver (W06)** | Ƀ600 (1000) | 55 (85) | 0.55 | 6 | 3.2 (2.0) | 80 | no | 100 (155) | 2 tiros matan a un SKULL; empuja |
| **Rifle de plasma (W07)** | Ƀ1100 (1500) | 22 (34) | 0.18 | 24 (36) | 2.2 (1.6) | 55 | si | 122 (189) | ignora 50% del escudo; área chica |
| **Lanzagranadas (W08)** | Ƀ1400 (1900) | 90 directo + 70 en área (3.5 m) | 1.0 | 6 | 4.0 (2.8) | 40 (arco) | no | 90 + área | explosión, hace daño propio |

Derivación: W05 queda debajo del fusil en DPS (125 vs 136) para no jubilarlo, pero con menos alcance (35 vs 70) y recarga más rápida; cuesta Ƀ500, entre escopeta y fusil.
W06 llena el hueco entre la pistola (25) y el sniper (70): daño por bala 55 con cadencia lenta, el mismo DPS que la pistola pero con "una decisión por bala"; el precio sube junto con el daño. W07 es el arma del Cap. 3, más cara que el fusil: DPS parecido (122) pero
con efecto especial. W08 es el tope, daño explosivo, mismo `TipoDanio.Explosion` que el carro reventado (no lo frena una cubierta).
Los niveles I a III suben daño, cargador y recarga igual que hoy (aprox. +20% de daño por nivel; precios de mejora tomados de la escala de la escopeta: 250/450/700).

### Cómo funcionan y dónde se consiguen

| Arma | Disparo | Recarga | Efecto especial | Dónde se consigue (Cap.) |
| --- | --- | --- | --- | --- |
| W05 Subfusil | Ráfaga continua con retroceso que sube; sin apuntar cae mucho (dispersión 4 grados, apuntando 2.5) | Cargador largo; recarga corta con la animación de rifle | Ninguno (arma de ritmo). Los SNAKES la usan, la puedes quitar al E10 | Cap. 2: botín de E10 (primera vez), luego se vende en El Mercado (EL MERCADO) |
| W06 Revólver | Semiautomático, retroceso fuerte, 6 tiros; tambor con luz turquesa que baja con las balas | Larga (3.2 s): se puede interrumpir bala por bala (speed loader) | **Empuje**: tira hacia atrás a los enemigos pequeños (E10, SKULL); contra E12 rompe 2x escudo | Cap. 2: recompensa de la misión del jefe SKULL o venta en El Mercado después de El Tianguis |
| W07 Rifle de plasma | Semiautomático de bobinas; mantener 0.6 s carga un disparo fuerte (x2 daño y área 2 m) | 2.2 s: cambia la celda (la celda brilla) | **Ignora 50% del escudo y la armadura** de E12/E14; 2x al dron; área chica | Cap. 3: botín de E12 (primera vez) y venta en El Mercado tras la misión de Nova |
| W08 Lanzagranadas | Arco, rebota una vez, explota al tocar enemigo o al 1.5 s; tambor rotativo de 6 | Larga (4 s); granada a granada | **Explosión de área** (3.5 m). Daño propio al jugador del 25% si estás a menos de 2 m | Cap. 2: botín garantizado de E11 (jefe SNAKES); venta en El Mercado después |

Munición: añadir tres "paquetes" nuevos (`DatosMunicionArma_subfusil/revolver/plasma/granadas.asset` y sus ofertas en `Economia/Datos`):
subfusil 96 (3 cargadores), revólver 36, celdas de plasma 72, granadas 18; precios por paquete tomando como base los de fusil (Ƀ120 por 60 balas):
subfusil Ƀ100, revólver Ƀ90, celdas Ƀ220, granadas Ƀ250 (propuesta). Ya hay `DatosObjeto_MunicionCeldas` y `MunicionCargas`; confirmar si son del mecha antes de reutilizar.

### Modelos, hologramas y animaciones

| Arma | Modelo | Holograma (tienda) | Animaciones: reutiliza | Animaciones: falta (nombres propuestos) |
| --- | --- | --- | --- | --- |
| W05 | `imageassets/armas/W05_Subfusil.png` -> Tripo | H05 | `Rifle_Idle/Caminar/Correr/Apuntar/ApuntarCaminar/DisparoCaminar/Recargar` | `Subfusil_Recargar` (opcional, sirve el de rifle), sonido de ráfaga |
| W06 | `W06_Revolver.png` -> Tripo | H06 | `Pistola_Sacar/Guardar/Idle/Caminar/Correr/Apuntar*/Disparo/Dano_*` | `Revolver_Disparo` (retroceso grande), `Revolver_Recargar_Bala` (una bala), `Revolver_Recargar_Todo` |
| W07 | `W07_RiflePlasma.png` -> Tripo | H07 | `Rifle_*` | `Plasma_Cargar` (mantener), `Plasma_Recargar` (celda). Efecto: rayo, brillo de celda |
| W08 | `W08_Lanzagranadas.png` -> Tripo | H08 | `Rifle_Idle/Caminar/Correr/Apuntar` | `Lanzagranadas_Disparo` (retroceso fuerte), `Lanzagranadas_Recargar` (tambor). Efecto: humo, explosión, onda |

El controlador (`ControladorBase`) tiene capa de torso por arma (puños, pistola, arma larga): las tres armas largas se enchufan en la capa de arma larga; el revólver en la de pistola. **No incluyo en este plan**
el enganche del Animator ni la rueda de armas; solo anoto que dependen de ellos.

---

## 3. Prótesis de ataque

Según M31 (`M31_HabilidadesProtesis.png`): 3 casillas con tecla [1] PIERNAS, [2] BRAZOS, [3] CABEZA, cada una con ícono y barra de energía/recarga. Los 4 espacios de implante
hoy son Ojos, Brazos, Piernas y Sistema nervioso (`personaje-progresion.md`); las prótesis de ataque se instalan en la clínica I+D+I (EL DOC) y el CIC (TINO). **Números = propuesta.**

| Prótesis | Slot | Cómo se usa | Energía y recarga | Daño y alcance | Precio |
| --- | --- | --- | --- | --- | --- |
| **IMP04 Brazo láser** | [2] BRAZOS (brazo derecho) | Tecla [2] la activa: el antebrazo se abre y sale el cañón; mantener el clic derecho (o clic) dispara un rayo continuo; soltar lo cierra | 100 de energía; el rayo gasta 25/s (4 s de rayo), se recarga 10/s después de 1.5 s sin usar; si se vacía, bloqueo de 3 s | 30 por segundo (rayo continuo que sigue la mira), 25 m; 60% de efecto contra armadura y escudos de E12 | Ƀ3500 |
| **IMP05 Hombrera de misiles** | [2] BRAZOS (mismo slot) o conflicto, ver decisión | Tecla [2] la activa: el lanzador sube; clic fija el objetivo (marca de mira) y lanza la salva de 4 misiles; los misiles buscan hasta 15 grados | 4 misiles por carga; recarga de 14 s al terminar la salva (o 1 misil cada 3.5 s, decide Tony) | 25 por misil, explosión de 2 m (`TipoDanio.Explosion`), alcance 40 m; golpe directo x1.5 | Ƀ4500 |

Derivación: el misil de habilidad del mecha hace 20 de daño con alcance 25, ya con cooldown y munición limitada (`combate.md`); el de la hombrera sube a 25 pero se lanza de 4 en 4, con recarga lenta.
Los precios quedan por encima de los implantes que ya tienen oferta (Brazo chatarra Ƀ1200, Ojos Ƀ5000 "de prueba"); IMP04 e IMP05 son de nivel Cap. 3 a 4.

**Dónde se consiguen**: IMP04 en el Cap. 3, como pieza de EL INVENTOR (TINO) tras vencer a E11 (el brazo cibernético de E11 y el IMP04 son del mismo diseño: "te lo quitas y lo reparas"); IMP05 en el Cap. 4, premio por derrotar a un E12
o venta en EL MERCADO con desbloqueo de la liga del campeón de misiones (decisión abierta). Ninguno depende de una pelea de mecha.

| Prótesis | Modelo | Animaciones: reutiliza | Animaciones: falta |
| --- | --- | --- | --- |
| IMP04 | `IMP04_BrazoLaser.png` -> Tripo (solo la pieza, se adapta como `PJ01_Implantes.fbx`) | `Loco_*`, `Rifle_Apuntar` para posar el brazo | `Protesis_BrazoLaser_Abrir`, `Protesis_BrazoLaser_Disparar_Bucle`, `Protesis_BrazoLaser_Cerrar`; efecto de rayo (línea turquesa con chispas), brillo, humo al sobrecalentar |
| IMP05 | `IMP05_HombreraMisiles.png` -> Tripo | `Rifle_Apuntar` | `Protesis_Misiles_Subir`, `Protesis_Misiles_Lanzar`, `Protesis_Misiles_Bajar` (o script en la hombrera); misiles (modelo pequeño), estela, explosión |

---

## 4. Orden recomendado, dependencias y áreas

Dueños según `CLAUDE.md`: **Tony** tiene IA, Personaje, Combate, Economía, Progresión, UI, Pelea. **Alan** tiene Trafico (dron), Mundo (zonas, azoteas, puntos de aparición) y Arte (por defecto).
Core solo en tareas de Core (Tony). Las áreas de dueños distintos no se referencian entre sí.

| Orden | Qué | Área / carpeta | Depende de | Notas |
| --- | --- | --- | --- | --- |
| 0 | Arte: Tripo de W05-W08, IMP04-05, E10-E14; hologramas H05-H08 después | `Assets/Arte/` (Alan por defecto), `imageassets/` | pedido-arte-14nov | Sin esto, los modelos son cajas provisionales |
| 1 | Subfusil W05 y revólver W06 (datos + `ArmaAPie` + munición + oferta) | `Personaje/Datos`, `Personaje/Combate`, `Economia/Datos` | nada de lo nuevo; **depende de la rueda de armas (otro trabajo)** para usarlas con slots | Son de datos; el flujo de disparo ya existe |
| 2 | Pandillero SNAKES E10 (ráfaga, esquive, machete) | `IA/APie`, `IA/Datos` | W05 (soltar el arma) | Sin Core |
| 3 | Dron de asalto E13 (bando Nova, no se retira) | `Trafico/Persecucion` (**Alan**) y un contrato en `Core/` si IA necesita hablarle | `DronMundo` | Tarea de Alan; Tony solo escribe el pedido. Alternativa: un dron propio en `IA/` (decisión abierta) |
| 4 | Jefe SNAKES E11 + misión "Termina con los SNAKES" + lanzagranadas W08 | `IA/APie`, `Pelea`, `Progresion/Misiones`, `Personaje/Datos` | E10, W05/W06, misil/explosión (`TipoDanio.Explosion` ya existe) | Primer jefe con fases |
| 5 | Guardia pesado E12 (armadura, escudo frontal) | `IA/APie`, `Personaje/Visual/GuardiaNova.cs` | `InfoDanio.Direccion` (ya existe) | Sin Core |
| 6 | Rifle de plasma W07 y ajuste de armadura | `Personaje/Combate`, `IA/APie` | E12 | Si se quiere "ignora armadura" sin heredar de `Disparo`, **tarea de Core** (`TipoDanio.Energia`) |
| 7 | Prótesis IMP04 y IMP05 | `Progresion/Implantes`, `Combate/` o `Personaje/Combate` | **depende del HUD de prótesis (M31), la tecla por slot y las habilidades (otro trabajo)** | Datos y reglas aquí; la UI no |
| 8 | Jefe final E14 y misión de cierre | `IA/APie`, `Pelea`, `Progresion/Historia`, `Mundo` (Alan: sede de NOVA) | E12, E13, W07, IMP05 recomendadas | Se hace al final |
| 9 | Tienda de armas (sección 5) | `UI/` y `Economia/` | hologramas H05-H08 | Pendiente |

Core posible (una sola tarea, antes del orden 5): `TipoDanio.Energia` si hace falta (hoy: Golpe, Disparo, Explosion) y contrato "quién manda al dron" (policía/Nova) para que Trafico e IA no se referencien.
Si no se quiere tocar Core: el plasma pega como `Disparo` con un multiplicador contra armadura dentro de `IA/`, y el dron de Nova se arma en `IA/`.

Pruebas por tarea (CLAUDE.md regla 4): EditMode de datos y cálculos (daño frontal, armadura, fases), PlayMode con entrada simulada para el disparo y la pelea de jefe.

---

## 5. Pendiente: tienda de armas con imagen grande (M32_TiendaArmas)

Anotado, **sin implementar**. Hoy hay `DatosTienda_Bar/CIC/Clinica` y ofertas (`DatosOferta_Arma_*`) pero la entrada de armas de EL MERCADO está en el
pendiente de `docs/interiores-faltan.md` ("Pendiente de T-550"). Para la tienda del mockup hace falta:

| Qué | Detalle |
| --- | --- |
| Pantalla | Lista de armas a la izquierda con precio; a la derecha la imagen grande del arma elegida (holograma), estadísticas (daño, cadencia, cargador, alcance) y botón COMPRAR. Sigue `guia-estilo-ui.md` y `H10_HUD.png`. Carpeta `UI/` |
| Datos de precios | Las ofertas ya existen por arma y nivel (`DatosOferta_Arma_*`); falta agregar las 4 armas nuevas y un campo de imagen grande (sprite) en `DatosArma`/oferta. Estadísticas se leen de `DatosArma` (ya tiene daño, cadencia, cargador, alcance) |
| Hologramas | H01-H04 de las armas actuales (existen como `W01..W04_*.png` en `Personaje/Datos`) y H05-H08 de las nuevas (pedido de arte "después"). Sin ellos la pantalla muestra el ícono chico |
| Interior de EL MERCADO | La entrada de armas del mercado y el mostrador (Alan, `Mundo/`) |
| Datos de desbloqueo | Qué armas aparecen según el capítulo y los botines ya conseguidos |
| Pruebas | EditMode de la lista y los precios; PlayMode de abrir, elegir y comprar con entrada simulada |

---

## 6. Decisiones abiertas para Tony

1. ¿Los capítulos 2, 3 y 4 de este plan son los que quieres, o la historia se ordena distinto después de la demo (T-F7)?
2. ¿E11 reemplaza a LA CULEBRA como jefe de Neón Alto, o es un jefe extra después de ella?
3. ¿Los números del cuadro de balance (vida, daño, precios) van como están o prefieres otra dureza? ¿El juego es difícil o relajado?
4. Los SNAKES de la demo (SNAKE actual, 60 de vida) ¿se quedan como están o se sustituyen por E10?
5. IMP04 e IMP05 ¿comparten el slot [2] BRAZOS (solo uno equipado) o la hombrera va a otro slot (por ejemplo [3] CABEZA como "hombros", o un cuarto slot en M31)?
6. ¿Aceptas una tarea de Core para `TipoDanio.Energia` y el contrato "quién manda al dron", o lo resolvemos en IA sin tocar Core?
7. ¿El dron E13 lo hace Alan en `Trafico/` (reusa `DronMundo`) o un dron propio en `IA/` de Tony?
8. Armadura y escudo (E12, E14): ¿se muestra una segunda barra de armadura en el HUD (pedido a UI) o solo un destello?
9. Botín: ¿las armas nuevas se consiguen primero por botín (lo propuesto) o solo comprándolas?
10. ¿Qué suelta E14 (pieza única, arma o cierre solo con historia)? ¿Qué pasa en Cap. 4 después: CONTINUARÁ o final?
11. Revólver: ¿recarga bala por bala interrumpible (más trabajo de animación) o recarga única?
12. Munición de plasma: ¿reutilizar `DatosObjeto_MunicionCeldas` (si es del mecha, conflictos) o crear una propia?
