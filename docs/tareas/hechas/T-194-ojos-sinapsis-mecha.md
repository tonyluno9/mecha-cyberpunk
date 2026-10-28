# T-194 Ojos y sistema nervioso en el mecha: ves al rival y reaccionas más rápido
Dueño: Tony | Rama: tony/T-194-ojos-sinapsis | Modelo: Sonnet 5.5, medium
Depende de: T-191, T-193

## Objetivo
Con OJOS instalados, una pantalla de la cabina muestra la vida del rival que tienes enfrente y te avisa su próximo ataque (golpe o
habilidad) antes de que lo veas venir. Con SISTEMA NERVIOSO, tu golpe sale antes, tu guardia sube antes y el bloqueo perfecto es más
generoso. Son los implantes "de la jaula": no suben vida ni daño (eso es de las piezas), te hacen mejor peleador.

## Contexto
- Diseño: personaje-progresion.md (Ojos: "ves la vida y el próximo ataque del rival"; Sistema nervioso: "reaccionas más rápido en el
  mecha"), estetica-hud.md ("en el mecha todo es diegético").
- Decisiones: D-011 (la vida del rival no se mostraba porque se ligaba al implante de ojos: esta tarea la muestra), D-012 (golpe en tres
  tiempos; la preparación del rival es su aviso visible), D-tony-114 (`ValoresMecha`), D-tony-127 (cambiar datos a media pelea),
  D-tony-139 (habilidad del jefe con 0.6 s de aviso). Propuestas: D-tony-165 (efectos), D-tony-168 (lectura del rival en Combate, sin Core),
  D-tony-171 (pisos de la reacción).
- Contratos: `IImplantes`, `EfectosImplantes` (T-190); `IVida`.
- Código: `IA/Rival/RivalBasico.cs` (`Fase`, `EnAviso`), `IA/Rival/CerebroRival.cs` (`EsperaRestante`), `IA/Rival/HabilidadRival.cs`
  (`AvisoIniciado`, `ProgresoEnfriamiento`), `IA/Rival/TurnosAtaque.cs`; `Mecha/Piezas/EquipoMecha.cs` y `ReglaEstadisticasPiezas.cs` (los
  valores del mecha de la historia), `Combate/Vida/VidaMecha.cs` (`CambiarDatos` conserva el daño), `Combate/Golpe/CicloGolpe.cs`,
  `Combate/Bloqueo/GuardiaMecha.cs`; `UI/Cabina/PantallaHabilidadesCabina.cs` y `UI/Editor/CreadorPantallaPiezasCabina.cs` (modelo de pantalla
  de cabina que se agrega a `MechaJugador.prefab` con un creador); `Mecha/Editor/CreadorCabina.cs` (tablero y LEDs).
- Pruebas modelo: `Tests/PlayMode/Progresion/LigasMostradorPruebas.cs` y `RecorridoHabilidadesPruebas.cs` (pelea del flujo con entrada
  simulada, atajos permitidos), `Tests/PlayMode/IA/HabilidadRivalPruebas.cs`.

## Qué hacer
1. `Combate/Lectura/ILecturaRival.cs` (interfaz de Tony, no de Core) y `TipoAtaqueLeido` (`Golpe`, `Habilidad`): `Nombre`, `Raiz`, `Vida`,
   `EsMecha`, `Preparando` (golpe o habilidad ya en aviso), `SegundosParaAtacar` (estimado, ≥ 0; −1 si no se sabe, por ejemplo fuera de
   alcance o sin turno) y `Proximo`. Quien la implementa se agrega a `Registro<ILecturaRival>` en `OnEnable` y se quita en `OnDisable`.
2. IA: `RivalBasico` la implementa (con su `HabilidadRival` si la tiene: `Proximo` = `Habilidad` cuando a la habilidad le falta menos que al
   golpe). Si hace falta, `CerebroRival` y `HabilidadRival` exponen lo que les quede en segundos; no cambia cómo deciden ni sus tiempos (las
   pruebas de IA siguen en verde).
3. `UI/Cabina/PantallaOjosCabina.cs` en `MechaJugador.prefab` (con creador idempotente en `UI/Editor/`; textos en `UI/Datos/ConfigPantallaOjos`):
   apagada sin OJOS. Con OJOS: elige entre `Registro<ILecturaRival>.Todos` el rival mecha vivo a 40 m o menos más centrado en la vista de la
   cabina (máximo 60°); muestra su nombre y su vida en una barra turquesa (nivel 2: también "142 / 200"); "PRÓXIMO: GOLPE" o "PRÓXIMO: HABILIDAD"
   se prende cuando `Preparando` o cuando `0 ≤ SegundosParaAtacar ≤ anticipacionAtaque`; "SIN OBJETIVO" si no hay rival a la vista. Funciona en
   la arena, la emboscada y las peleas de misión. Sin crear objetos por cuadro.
4. Mecha: `EquipoMecha` compone el sistema nervioso encima de serie + piezas con una regla pura (`Mecha/Piezas/ReglaReaccionImplantes.cs`):
   preparación del golpe × m y tiempo de subir guardia × m (con los pisos de D-tony-171), ventana del bloqueo perfecto ÷ m (con su tope).
   Vida, daño, alcance, velocidad, giro, recarga y bloqueo no cambian. Reacciona a `IImplantes.Cambio` y a `Registro<IImplantes>.Agregado`
   (conserva el daño, como al cambiar piezas). Solo el mecha de la historia (`EquipoMecha`); el `Directo` de Pruebas_Tony no cambia.
5. Cabina: un LED "SINAPSIS" en el tablero (turquesa; dos LEDs con el nivel 2) prendido con sistema nervioso; con su creador en `Mecha/Editor/`.
6. Pruebas:
   - EditMode `Tests/EditMode/Mecha/ReglaReaccionImplantesPruebas.cs`: multiplicadores, pisos y tope; con cualquier implante y cualquier
     pieza, vida, daño y velocidad del mecha iguales a sin implantes.
   - PlayMode `Tests/PlayMode/Progresion/Implantes/OjosSinapsisPruebas.cs` en Pruebas_Implantes, pelea del flujo desde el mostrador con
     entrada simulada (clic izquierdo y derecho, WASD; atajos permitidos y anotados: implantes con `ImplantesDePrueba` o `Instalar` por código,
     `ServicioLigas.Poner` para llegar al jefe de la liga 1, `RecibirDanio` al rival solo después de daño real del jugador):
     a) sin OJOS la pantalla está apagada;
     b) OJOS I contra el rival 1 de la liga 1: la barra coincide con `Vida.Actual / Vida.Maxima` (±1 %) tras golpes reales; para cada golpe del
        rival en 30 s, el aviso se prendió al menos `anticipacion` − 1 cuadro antes de que el rival entrara en preparación (los golpes donde el
        rival llegó a su alcance con menos tiempo que eso se cuentan aparte y se imprimen; al menos el 80 % del total cumple);
     c) OJOS II: lo mismo con su anticipación, y el texto de la vida en números es correcto;
     d) contra DON FIERRO (jefe con EMBESTIDA): "PRÓXIMO: HABILIDAD" se prende al menos `anticipacion` − 1 cuadro antes de `AvisoIniciado`;
     e) SINAPSIS I y II: se mide del clic izquierdo al impacto del golpe del jugador y del clic derecho a la guardia arriba, y se compara con
        los valores esperados (±1 cuadro); vida máxima, daño y velocidad iguales a sin implante; el LED está prendido;
     f) consola sin errores; `MedidorFrames` sin tirones fuera de su tolerancia.
   - Captura docs/capturas/T-194-1.png: la cabina en pelea con OJOS II (nombre, barra, números y "PRÓXIMO: GOLPE") y el LED SINAPSIS, revisada a ojo.

## Carpetas permitidas
Assets/_Proyecto/Combate/Lectura/ (nueva), Assets/_Proyecto/IA/Rival/ (lo mínimo para exponer la lectura), Assets/_Proyecto/UI/Cabina/,
Assets/_Proyecto/UI/Datos/ (solo ConfigPantallaOjos y su asset), Assets/_Proyecto/UI/Editor/, Assets/_Proyecto/Mecha/Piezas/, Assets/_Proyecto/Mecha/Cabina/,
Assets/_Proyecto/Mecha/Editor/, Assets/_Proyecto/Mecha/Prefabs/ (solo con creadores), Assets/_Proyecto/Tests/EditMode/Mecha/,
Assets/_Proyecto/Tests/PlayMode/Progresion/Implantes/, Assets/_Proyecto/Tests/PlayMode/IA/ (solo si una prueba vieja necesita ajuste anotado), docs/capturas/
No tocar: Core/, Progresion/Implantes/ (solo se usa), Personaje/, IA/Datos/ (los tiempos de los rivales no cambian), escenas, todo lo de Alan.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa (salvo los fallos conocidos de Alan en Arena) y `playmode-por-area` no marca ningún área "LIMITE"
- [ ] ReglaReaccionImplantesPruebas y OjosSinapsisPruebas pasan 3 corridas seguidas; la bitácora imprime las anticipaciones medidas por golpe
      y los tiempos de golpe y guardia contra los esperados
- [ ] Las pruebas de IA, de piezas y de habilidades siguen en verde (RivalBasicoPruebas, HabilidadRivalPruebas, FuriaJefePruebas,
      RecorridoPiezasPruebas, RecorridoHabilidadesPruebas) y las de pelea del prólogo también
- [ ] Correr los creadores dos veces no cambia `MechaJugador.prefab` ni `MechaEstacionado.prefab` (md5 igual)
- [ ] Captura T-194-1 revisada a ojo: la pantalla no tapa el centro de la vista y no usa rojo puro

## Criterios de sensación (para un humano)
- Pruebas_Implantes, una pelea con OJOS I y otra con OJOS II: ¿el aviso te da tiempo de subir la guardia, o se siente como hacer trampa?
  ¿Mirar la pantalla te distrae de la pelea?
- Con SINAPSIS II: ¿el mecha se siente más ágil al pegar y bloquear, sin dejar de sentirse pesado?

## Pruebas automáticas
- EditMode: ReglaReaccionImplantesPruebas. PlayMode: OjosSinapsisPruebas.

## Fuera de alcance
- Ojos a pie contra los pandilleros (T-195), rivales que reaccionan a tus implantes (bloque 8, adaptación), sonido de aviso. Hackeo.
