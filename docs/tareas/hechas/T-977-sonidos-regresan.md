# T-977: regresan los sonidos sugeridos y mezcla final

Pedido de Tony (13-nov): regresar los sonidos de docs/sonidos-sugerencias.md en su orden, con volumen razonable y sin repetirse en exceso;
los marcados como molestos siguen apagados. Mezcla final de música, ambiente, efectos y voces, y que en ninguna zona se encimen.

## Qué regresó (en el orden de la lista)
`ReglaMezclaSonidos.IdsRegresados`, prendidos en el CatalogoSonidos con `RegresarSonidosT977.Aplicar` (editor, no YAML a mano):

| # | ID | Quién lo pide | Volumen | Voces | Mínimo entre repeticiones |
| --- | --- | --- | --- | --- | --- |
| 1 | AMB_CALLE_NOCHE | GestorSonidos (calle de noche) | nivel 1.0 | 1 ambiente | cruce de 1 s |
| 2 | VEH_PUERTA_ABRIR / CERRAR | CarroEstacionado, patrullas, refuerzos | 0.6 | 1 | 0.5 s |
| 3 | ARM_BALA_IMPACTO_CONCRETO | PistolaAPie: el tiro que pega en una superficie (no persona), **uno por tiro** | 0.5 | 2 | 0.12 s |
| 4 | PAN_RIVAL_ALERTA | PandilleroAPie | 0.6 | 1 (un solo grito aunque te vean varios) | 2.5 s |
| 5 | PAN_JEFE_ENTRADA | JefePandilla | 0.9 | 1 | 10 s |
| 6 | UI_OBJETIVO_NUEVO / CUMPLIDO | MarcadorObjetivo | 0.6 | 1 (comparten grupo) | 1 s |
| 7 | VEH_FRENO_DERRAPE | CarroViejo (solo el que maneja el jugador), **bucle** que se corta al terminar el derrape (el archivo dura 11.6 s) | 0.5 | 1 | descanso 1.2 s |
| 8 | JUG_PASO_ASFALTO | ControladorAPie (solo el jugador; prueba que nadie más pide pasos) | 0.45 | 1 | 0.15 s |
| 9 | CAL_SUBE_NIVEL | ServicioCalor | 0.65 | 1 | 3 s |
| 10 | ECO_DINERO_RECOGER | DineroEnPiso (antes sonaba ECO_COMPRA) | 0.55 | 1 | 0.25 s |
| 11 | MUN_PUERTA_CASA_ABRIR / CERRAR | HojaPuerta con bisagra (al abrir y cerrar) y PuertaCuarto (al cruzar) | 0.6 | 1 | 0.5 s |
| 12 | AMB_CASINO / AMB_CLINICA | GestorSonidos (al entrar a VivaCoin / I+D+I) | nivel 0.55 / 0.13 | 1 ambiente | cruce de 1 s |

Nuevos ganchos: impacto en concreto (PistolaAPie), derrape (CarroViejo + ReglaDerrapeSonido, con histéresis), dinero (DineroEnPiso),
puertas de casa (HojaPuerta, PuertaCuarto). Los demás ya tenían quien los pidiera.

Siguen apagados (`ReglaMezclaSonidos.IdsMolestos`): voces de NPC (PAN_VECINO_AUXILIO, PAN_SILBIDO_LLAMAR, PAN_RIVAL_PROVOCA, PAN_ALIADO_SIGUE/ESPERA),
jadeos (JUG_CORRER_RESPIRA), claxon, hologramas, neón (MUN_NEON_ZUMBIDO, AMB_NEON_ALTO), perros, aire acondicionado, disparos lejanos y UI_TEXTO.
Tampoco se prendieron los que la lista no pide (AMB_TIANGUIS, AMB_INTERIOR_CASA, AMB_MERCADO, AMB_BAR, pasos de interior, etc.).

## Mezcla final
Medí el RMS de cada archivo: los ambientes van de -46.5 dB (calle) a -27.1 dB (clínica), así que cada ambiente lleva su nivel
(`ReglaMezclaSonidos.VolumenAmbiente`) y todos quedan entre -45 y -48 dB, ~20 dB debajo de disparos y voces.

| Sección | Antes | Ahora |
| --- | --- | --- |
| Efectos | 1 | 1 |
| Voces | 1 | 0.85 (los gritos están grabados a -11 dB) |
| Ambiente | 0.3 | 1 (con el nivel por ambiente) |
| Música | 0 | 0.5 (hoy nada pide MUS_ y la opción arranca en 0: sigue en silencio) |
| Interfaz | 0.7 | 0.6 |

Los bucles (motor, sirena, derrape) ahora también llevan el volumen relativo de su ID.

## Que no se encimen por zona
- Un solo ambiente a la vez (cruce de 1 s). Las capas viejas de AmbienteSonoro (lluvia, neón, tráfico, música) siguen apagadas.
- Lugares anidados: al salir de la clínica sigue el del mercado (antes volvía el de la calle estando dentro del mercado).
- Límite de 12 voces, una por ID, y cada ID con su tiempo mínimo.

## Pruebas
- EditMode (Sonido, Vehículos, Progresión, Personaje): 741/741. Nuevas: SonidosRegresanT977Pruebas (6), ReglaDerrapeSonidoT977Pruebas (4).
- PlayMode Sonido: 19/19. Nuevas: SonidosRegresanT977Pruebas (zonas, ráfaga de 6 s pidiendo cada ID 5 veces por cuadro, derrape en bucle).
  Ráfaga: PAN_RIVAL_ALERTA sonó 3 veces en 6 s, CAL_SUBE_NIVEL 2, jefe 1, pasos 40.
- PlayMode ManejoCarroPruebas 15/15 con entrada simulada: frenazo a 16 m/s rechina y se calla al parar; acelerar derecho no rechina.
- DisparoLimpioT676 actualizada: en una persona no suena impacto; en la pared, uno solo. SonidoArmasT593 alineada con T-676 (el tono del disparo no varía; ya fallaba antes).
- Fallaban antes y siguen igual sin relación con el sonido (comprobado con los cambios guardados en stash): CuartosPruebas (2: el cuarto mide 5 m, la E no ofrece la puerta) y CuerposBotinT746 (el enemigo no cae con la pistola).

## Para Tony (a oído)
Nada de esto se oyó con audífonos: los volúmenes salen de medir los archivos. Si algo queda alto o bajo, se cambia el número en
`ReglaMezclaSonidos` (por ID) o en el CatalogoSonidos (por sección).
