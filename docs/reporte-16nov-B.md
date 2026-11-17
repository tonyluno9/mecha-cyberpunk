# Reporte de la Sesión B del 16-nov

La Sesión B hizo la parte de ciudad, techos, casino y enemigos nuevos de `docs/playtest-16nov.md`: tareas T-1101 a T-1113, cada una en su rama e
integradas en `s2-integ`. T-1109 y T-1110 las integró a main la Sesión A el 17-nov. T-1111 no existió: no hay archivo de tarea ni commits con ese número.

## Tareas

| Tarea | Qué quedó |
| --- | --- |
| T-1101 Escaleras a techos | 13 escaleras de mano nuevas en casas (Suburbios 5, CalleDorada 3, Mercado 5, Barrio 0). Las 30 escaleras (17 viejas y 13 nuevas) suben al techo en 8 s o menos. Se corrigieron las paredes invisibles de las casas: 245 mallas, 1960 triángulos con la cara al revés. |
| T-1102 Calle del Doc con cuadras | La calle norte-sur junto al Doc se rehízo con el kit: 13 fachadas de 1 a 3 pisos, tiendita, taquería, vulcanizadora, lavandería, lote baldío con mural y remate con neón. 6 postes nuevos con luz de sodio y 8 charcos. Se apagaron 12 casas viejas y 4 postes viejos; las casas 12, 13, 18 y 19 se quedan porque sostienen accesos de techo y rutas de parkour. |
| T-1103 Torres nítidas | Torres de Nova y Neón Alto con shader propio (niebla al 30% de distancia, tope de 45%) y ventanas que brillan de noche. Letrero NOVA cian en dos caras, haz de luz de 100 m y luces rojas de aviso (4 por torre, 14 torres). |
| T-1104 Quitar pasarelas | Se quitaron las pasarelas PS01 de Neón Alto y sus escaleras. Se reencendieron 10 props y el carro robable 7 se movió al borde de la banqueta. |
| T-1105 Arreglos del casino | Cajera de frente al salón; las 12 sillas miran a su mesa; Sergio (el Hacker) sentado en la esquina noroeste; panel de muro sobre el arco de la entrada. |
| T-1106 Enemigos nuevos (visuales) | 4 modelos Humanoid importados con prefabs y ganchos de conducta y daño: E10 SNAKES (1.8 m), E11 Jefe SNAKES (1.95 m), E12 Guardia pesado (2.05 m) y E14 Jefe final de Nova (2.3 m). |
| T-1107 Riña SNAKES contra SKULLS | Dos riñas de 6 pandilleros en Neón Alto, en (75, -16) y (101, -12), a más de 60 m de la ruta de la demo. Pelean al 35% del daño y nadie baja de 25% de vida; si el jugador se acerca a 6 m o les pega, los seis van contra él. |
| T-1108 Jefe SNAKES (E11) | Mini jefe con cuchilla: 3 tajos de 10 y una estocada de 22 que se esquiva apartándose. Vida 260; al 50% llama refuerzos una vez. Su puesto está en (113, -16) con 2 escoltas. |
| T-1109 Guardia pesado de Nova (E12) | Dos guardias en la puerta de la torre de Nova con escudo frontal: de frente reduce 85% el daño, aguanta 150, se rompe y vuelve a los 5 s. Empujón de 18 y mazazo de 28 cada 3 ataques. Vida 220. Chispas al bloquear. |
| T-1110 Jefe final de Nova (E14) | 3 fases (cambia al 66% y al 33% de vida), vida 600. Puñetazo de 25, cañonazo de 30, onda de 20 y escudo de energía de 140 que al romperse lo aturde 3 s. Pelea en un recinto con barrera al final de la calle de Nova; se reinicia si te alejas a más de 40 m o mueres. Al vencerlo se abre la barrera y se apagan los guardias. |
| T-1112 Arreglos de la calle del Doc | Los 6 postes nuevos alumbran 9 m, como los de la ciudad. La demo se atoraba en el tramo 09 porque la clínica del Doc quitaba el control; la prueba ahora la cierra con Esc. |
| T-1113 Demo intermitente | La prueba de la demo pulsa Esc hasta que la clínica cierre y vuelva el control (máximo 10 s). 4 corridas seguidas pasaron 3 de 3. |

## Pruebas

- T-1101: EscalerasTechosT1101 3 (+1 de captura), TechosS2 6/6, TreparAPie 5/5.
- T-1102: CalleDocT1102 EditMode y PlayMode 3/3, TechosS2 6/6, RecorridoCiudadT995 1/1.
- T-1103: CiudadTorresT1103 4/4, AssetsNuevosT540 5/5.
- T-1104: PasarelaNovaT996 3/3.
- T-1105: CasinoArreglosT1105 6/6, HackerEsquinaT1105 1/1 y las del casino (meseras, fichas, cajera, tragamonedas).
- T-1106: EditMode completo 2515/2515, GanchosConductaT1106 4/4, EnemigosAnim2 6/6.
- T-1107: ReglaRina 8/8, RinaPandillasT1107 5/5, RinasCiudadT1107 3/3, RecorridoDemo 3/3.
- T-1108: ReglaJefeSnakes 8/8, JefeSnakesT1108 7/7, JefeSnakesCiudadT1108 4/4.
- T-1109: EditMode completo 2552/2552, GuardiaPesadoT1109 9/9, GuardiasPesadosCiudadT1109 4/4.
- T-1110: EditMode completo 2577/2577, JefeFinalNovaT1110 11/11, JefeFinalCiudadT1110 4/4, RecorridoDemo 3/3.
- T-1112 y T-1113: RecorridoDemoPruebas 3/3 (4 corridas seguidas en T-1113).

## Capturas

En `docs/capturas/`: `t-1101` (escaleras), `t-1102` (calle del Doc), `t-1103` (torres), `t-1104` (calle sin pasarelas), `t-1105` (casino),
`t-1106` (los 4 enemigos de frente y perfil), `t-1107` (riñas), `t-1108` (jefe SNAKES), `t-1109` (guardias) y `t-1110` (jefe final).

## Pendientes y notas

- `JefeNovaT693Pruebas.CadaTresGolpes_CargaEnLinea` ya fallaba antes.
- T-1101: el barrio quedó sin escaleras nuevas porque sus muros tienen fachadas pegadas.
- T-1102: las casas 12, 13, 18 y 19 siguen con el estilo viejo. Los pretiles nuevos no tienen collider.
- T-1103: el letrero NOVA se lee mejor de cerca; desde el tianguis lo tapan edificios.
- T-1105: falta un clip de sentado de verdad para Sergio (hoy es una pose por huesos).
- T-1107: los SKULLS se ven casi negros de noche. Los 3 SNAKES viejos no pasan a E10 porque `PersonajesT510Pruebas` exige su modelo viejo.
- T-1108: la estocada se lanza de 5 a 7 m (el diseño decía 5 a 9). Vencer al jefe puede conquistar Neón Alto.
- T-1110: el recinto es el final de la calle de Nova porque la torre no deja un círculo de 14 m al pie. Las pausas entre ataques (0.8 s y 1 s) las fijó la tarea.

## Integración a main (Sesión A, 17-nov)

- `s2-integ` se mezcló a main con `--no-ff` (commit `0dc8442`) y se subió. Después se quitó `.sesiones/s2` con `git worktree remove` y `git worktree prune`.
- Antes del merge, en `s2-integ`: EditMode completo 2577/2577. PlayMode del área (guardia pesado, jefe final, jefe SNAKES, daño a guardias de Nova, tiro a la cabeza): 35 pasan y 3 fallan.
  - `JefeFinalNovaT1110.Canonazo_ElJugadorSeAparta…` y `JefeSnakesT1108.Estocada_SiElJugadorSeAparta…` fallan solo corriendo junto con otras clases (el jugador no se mueve con A/D simuladas). Solas pasan: 17/17.
  - `HeadshotPandilleroT677.Pandillero_TiroALaCabeza…` falla también en main sin la Sesión B: ya fallaba antes.
