# T-992: habilidades de prótesis [1] PIERNAS, [2] BRAZOS, [3] CABEZA

Diseño: `docs/diseno/rueda-y-habilidades.md`.

## Qué se hizo
- `Personaje/Habilidades/ReglaHabilidadProtesis.cs` (lógica pura: Lista, Activa, Recargando, medidor 0..1, duración I/II, Cortar) y `HabilidadesProtesis.cs` (componente que agrega `JugadorAPie.Awake`; entrada por `performed` de las acciones nuevas; solo a pie, con control y vivo; en el carro los tiempos corren; al morir se cortan).
- `Personaje/Datos/ConfigHabilidadesProtesis.cs` y asset `Datos/Resources/ConfigHabilidadesProtesis.asset` (creado con `Personaje/Editor/CreadorHabilidadesProtesis.cs`, enlazado a `ControlesAPie.inputactions`). Se carga con Resources: el prefab `JugadorAPie` no se tocó.
- Valores: PIERNAS correr x1.5, caminar x1.3, salto x2.2, 6/10 s, recarga 20 s; BRAZOS golpe x2.5, empuje 6 m/s, tiempo de recarga del arma x0.4, 6/10 s, recarga 25 s; CABEZA 8/14 s, recarga 30 s.
- Entrada: acciones `HabilidadPiernas/Brazos/Cabeza` (teclas 1, 2, 3) en `ControlesAPie.inputactions` (GUIDs viejos intactos) y en `CreadorJugadorAPie`.
- Los multiplicadores se suman encima de los pasivos en `ControladorAPie` (correr, caminar, salto), `GolpeAPie` (daño y empuje) y `PistolaAPie` (recarga).
- `VisorRayosX` marca solo con CABEZA activa y implante de ojos (`HabilidadesProtesis.CabezaActiva` y evento `CabezaCambio`; Apoyo ya referenciaba Personaje).
- HUD: `UI/HudH10/BarraHabilidades.cs` (3 casillas abajo al centro, y=96 sobre el aviso del tutorial; tecla real con `TeclasHud`; "—" sin implante; avisos "PIERNAS ACTIVAS" y "NECESITAS EXO DE PIERNAS/EXO DE BRAZOS/EL VISOR" por `ColaAvisos`). La agrega `HudAPie.Awake`; la rueda y los menús la apagan porque su canvas es del HUD.
- Tutorial: `AccionTutorial.Habilidades` (solo con algún implante; texto con las que se tienen). Controles de Opciones/inicio: cuarta fila de habilidades. `ConsejosCarga`: un consejo.

## Decisiones
- Sonido: no hay sonido de poder/activar en Assets/Arte/Audio; ninguno.
- "Más empuje": no existía empuje en el golpe; se aplica un impulso a un Rigidbody del objetivo si lo tiene (los pandilleros no lo tienen, sin efecto visible).
- Las pruebas de capturas están en Tests/PlayMode/UI (necesitan el director del prólogo); las jugables en Tests/PlayMode/Personaje. Se agregó `Progresion.Apoyo` a los asmdef de pruebas Personaje y UI.

## Pruebas
- EditMode completo: 2443 de 2444 pasan; falla solo `NeonAltoT505Pruebas.EntradaDelCasino_TieneFocosTubosCargadoresYMoto` (ya fallaba en main).
- PlayMode `HabilidadesProtesisT992Pruebas`: 11, primera corrida 10 pasan y 1 fallaba (recarga: faltaba el pasivo x0.55 de BRAZOS II en lo esperado; corregido en la prueba, medido 0.31 s contra 0.56 s esperados). Medidos: correr 20.258 m (esperado 20.258), salto 1.287 m (1.287), golpe 75 (75), visor 1 renderer marcado.
- Pendiente de correr (el sistema cortó el proceso por poca memoria): la corrida filtrada de PlayMode (Visor, PandillaAPie, ControladorAPie, GolpeAPie, PistolaAPie, HudH10, Tutorial, RuedaArmasT991), la prueba de recarga corregida y las capturas (`CAPTURAR_T992=1`, `docs/capturas/t992/`).
