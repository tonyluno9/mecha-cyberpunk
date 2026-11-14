# Reporte 15-nov, Sesión A

Pedido de Tony (14-nov noche): avisos del Doc y de la silla, rueda de armas y objetos, habilidades de prótesis, holograma del banco de Tino y builds.
- Opus diseñó la rueda y las habilidades (`docs/diseno/rueda-y-habilidades.md`) y revisó todas las capturas. Sonnet implementó.
- Todo entró a main con `--no-ff` y push.
- Los números T-990 a T-993 no chocan con la Sesión B, que va en T-995 a T-999.

## 1. T-990: [E] IMPLANTES con el Doc y [E] CURARME en la silla (`d30bf7d`)
- **Causa, medida en Ciudad_Prologo:**
  - De noche la clínica I+D+I cierra por horario. El Doc, con la vida llena, decía "ESTÁS BIEN, MIJO", un aviso que no se puede usar, y el HUD no da ayuda de botón para eso.
  - El Doc está a 0.96 m de la silla y ganaba como lo más cercano, así que también tapaba la silla.
  - Alcance (2.5 m) y ángulo (60°) no eran el problema.
- **Decisión de Opus:** el Doc está ahí, así que **siempre** ofrece "IMPLANTES", de día o de noche y con cualquier vida. La E abre la clínica aunque el mostrador esté cerrado por horario (`MostradorClinica.AbrirDesdeDoc`). Curar queda a cargo de la silla.
- **La silla** dice "CURARME · $costo" si estás herido y "CURARME" si estás sano. Se cambió el asset `ConfigSillaClinica_Base` por SerializedObject.
- **Pruebas:**
  - `AvisosDocSillaT990Pruebas` (de noche): "[E] IMPLANTES" y la E abre la clínica; "[E] CURARME" y la E te sienta.
  - EditMode 2383/2383.
- **Capturas** revisadas: `docs/capturas/t990/doc.png` y `silla.png`.

## 2. T-991: rueda de armas y objetos con Tab (`d1b2efc`)
- Al mantener Tab se abre una rueda al centro con 10 casillas.
  - Arriba, las armas: puños, pistola, escopeta, fusil y francotirador.
  - Abajo, los objetos: bebida, papitas, venda, café y batería, con cuántos llevas.
  - Lo que no tienes sale apagado.
- **Cómo se elige:** con el mouse; al soltar Tab se equipa el arma o se usa el objeto.
- **Mientras está abierta:**
  - El juego va a `timeScale` 0.25 y el fondo se oscurece.
  - No se gira la cámara ni se dispara.
  - Se apagan el tutorial, los mensajes, las ayudas y los subtítulos.
  - Si se abre la pausa, la rueda no pisa su escala de tiempo.
- **Teclas que cambian:**
  - 1–5 ya no cambian de arma. 6–0, F y Tab ya no usan ni eligen objetos.
  - La rueda del mouse sigue cambiando de arma.
  - Tutorial, controles del inicio, consejos de carga y Opciones dicen "MANTÉN [Tab]".
- Estilo H10 provisional hasta el mockup M30.
- **Pruebas:** EditMode 2393/2393 en la rama. `RuedaArmasT991Pruebas`: 9/9 (abrir, pistola, venda, arma que no tienes, teclas viejas sin efecto, cámara quieta, pausa y muerte). Se reescribieron unas 20 pruebas que usaban las teclas viejas.
- **Capturas** revisadas: `docs/capturas/t991/`. En la primera versión el fondo casi no se oscurecía y se encimaban textos; se corrigió.

## 3. T-992: habilidades de prótesis [1] PIERNAS, [2] BRAZOS, [3] CABEZA (`fcae36d`)

| Tecla | Implante | Efecto | Duración I / II | Recarga |
| --- | --- | --- | --- | --- |
| 1 | Exo de piernas | Correr ×1.5 y súper salto ×2.2 | 6 s / 10 s | 20 s |
| 2 | Exo de brazos | Golpe ×2.5 y recarga del arma ×0.4 | 6 s / 10 s | 25 s |
| 3 | Visor | Ver enemigos a través de paredes | 8 s / 14 s | 30 s |

- Cada casilla tiene un medidor de energía:
  - **Lleno:** lista.
  - **Se vacía:** activa.
  - **Se llena:** en recarga.
- Solo funciona a pie y con el implante. Sin él, la casilla sale con "—" y el aviso dice "NECESITAS EXO DE PIERNAS" (o el implante que falte).
- La barra tiene 3 casillas, abajo al centro, con fondo oscuro (estilo H10 provisional hasta el M31).
- **Cambio de diseño:** el visor ya no ve a través de paredes todo el tiempo, solo mientras CABEZA está activa.
- Los efectos pasivos de los implantes no cambian. La habilidad se suma encima.
- **Medido contra lo esperado:**

  | Medida | Medido | Esperado |
  | --- | --- | --- |
  | Correr con PIERNAS I | 20.258 m | 20.258 m |
  | Salto con PIERNAS I | 1.287 m | 1.287 m |
  | Golpe con BRAZOS I | 75 | 75 |
  | Recarga con BRAZOS II (1.4 × 0.55 × 0.4) | 0.3086 s | 0.308 s |

  - Con CABEZA activa, el visor marca al enemigo; al terminar, se desmarca.
- **Pruebas:** `HabilidadesProtesisT992Pruebas` 12/12 (con capturas). EditMode 2443/2444 (la falla es `NeonAltoT505`; ver Pendientes).
- **Capturas** revisadas: `docs/capturas/t992/barra-lista.png`, `barra-activa.png` y `visor.png`.
- **No se hizo:**
  - No hay sonido de activación, porque el proyecto no tiene uno adecuado.
  - El "más empuje" del golpe solo empuja objetivos con Rigidbody; los pandilleros no tienen.

## 4. T-993: armas más grandes en el holograma del banco de Tino (`f4eef16`)
- **Causa:** la cámara del holograma estaba a 3.5 m fijos, con 28° de campo de visión, sin importar el arma. Ocupaban ~36 % del ancho del panel.
- **Ahora:** el holograma encuadra cada arma con sus límites reales, para que ocupe 80 % del ancho o 75 % del alto del panel, lo que limite primero. La rueda del mouse acerca y aleja desde ahí, de 0.6× a 1.5×.

  | Arma | Antes (ancho / alto) | Después (ancho / alto) |
  | --- | --- | --- |
  | Pistola | 0.39 / 0.63 | 0.47 / 0.78 (la limita el alto) |
  | Escopeta | 0.35 / 0.25 | 0.76 / 0.57 |
  | Fusil | 0.36 / 0.34 | 0.76 / 0.75 |
  | Francotirador | 0.36 / 0.23 | 0.77 / 0.51 |

- **Pruebas:** `HologramaArmaT993Pruebas`, MenuArmas y BancoTodasArmasT942: 11/11. EditMode 2443/2444.
- **Capturas** revisadas: `docs/capturas/t993/`. Incluye `antes-pistola.png` para comparar.

## Builds
BUILDS_AQUI

## Pendientes y notas
- **`NeonAltoT505Pruebas.EntradaDelCasino_TieneFocosTubosCargadoresYMoto`** (EditMode, Mundo/casino) falla en main desde que entraron cambios de la Sesión B.
  - Ninguna rama de esta sesión tocó Mundo. Es área de la otra sesión y no se tocó.
- **Fallos de PlayMode que ya estaban en main** y siguen igual:
  - `HudH10Pruebas.Estrellas_SinCalorSonContorno...`
  - 8 de `PandillaAPiePruebas` (`Dinero_`, `Fuga_`, `Ojos_`, `Pelea_` y `Tumbar_` ×4)
  - `HeadshotPandilleroT677`
  - `ArmasPruebas.ComprarYUsarEscopeta`
  - `CuerposBotinT746`
  - `ControladorAPiePruebas.APie_EspacioEnElPiso_SubeLosPiesEntre09Y13Metros`: mide 0.45 m porque `alturaSalto` vale 0.45 en los datos.
- **`PandillaAPiePruebas.Mecha_SubirAlMechaASuLado...`** falló en una corrida larga y pasa sola: depende del orden.
- **`RecorridoRonda2Pruebas`**, si corre antes en la misma sesión, deja la entrada simulada sin teclas para las pruebas siguientes. No se corrieron completos ni `RecorridoRonda2Pruebas` ni `RecorridoDemoPruebas` después de la rueda.
- **Con la rueda abierta** siguen visibles el minimapa y la calavera de abajo a la derecha. Es menor; se arregla con el mockup M30.
- **Se quitaron las acciones de entrada `ArmaPunos` a `ArmaSniper`.** `JugadorAPie.prefab` conserva campos viejos sin uso, que Unity ignora.
- **No se corrió PlayMode completo**, por memoria (el Unity de la Sesión B llegó a usar 6 GB). Se corrieron las clases de cada área.
