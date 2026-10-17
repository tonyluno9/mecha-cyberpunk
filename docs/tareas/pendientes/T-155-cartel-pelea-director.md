# T-155 El flujo de pelea acepta un cartel por pelea (rival, premio, apuesta, presentador) y anuncia la presentación
Dueño: Tony | Rama: tony/T-155-cartel-pelea | Modelo: Sonnet 5.5, medium
Depende de: T-152

## Objetivo
Un mismo `DirectorPelea` (el del mostrador de peleas) puede pelear cualquiera de los 20 rivales de las ligas: antes de cada pelea recibe un
"cartel" con el rival, el premio, la apuesta, el presentador, la liga y si es pelea de jefe, y al terminar vuelve a su pelea de siempre (la
casera). Cada pelea publica `EventosPelea.PeleaPresentada` para que el mundo acompañe, y el presentador puede decir el nombre de la liga.

## Contexto
- Diseño: personaje-progresion.md (ligas, campeón con pelea de jefe), estetica-hud.md (en el mecha todo es diegético).
- Contratos: `Core/Pelea/InfoPelea.cs`, `Core/Pelea/EventosPelea.cs` (`PeleaPresentada`) e `IProgresoLigas` (`NombreLiga`) de T-152.
- Decisiones: D-tony-48 (el premio lo paga `DirectorPelea`, una vez por pelea), D-tony-65 (`EntradaArena` reinicia el director al terminar),
  D-tony-80 (apuesta anunciada con `AnunciarApuesta`). Propuestas nuevas: D-tony-94 (el cartel vive en Pelea/Datos) y D-tony-96 (pelea de jefe).
- Código: `Pelea/Flujo/DirectorPelea.cs` (la máquina se arma en `Awake` con las duraciones del presentador; `ResolverRival` crea el rival desde
  `prefabRival`; `AlCambiarLinea` cambia `{alias}` y `{rival}`), `Pelea/Datos/ConfigPresentador.cs`, `Pelea/Editor/CreadorFlujoPeleaCasera.cs`,
  `Pelea/Editor/CreadorFlujoPelea.cs` (el tutorial). Pruebas de ejemplo: `Tests/PlayMode/Pelea/PremioPeleaPruebas.cs` y `ApuestaPeleaPruebas.cs`.
- Pelea no referencia IA (decisión de T-022): el alias sale de `IPeleador.Nombre` del rival creado.

## Qué hacer
1. `Pelea/Datos/CartelPelea.cs`: clase `[Serializable]` (no ScriptableObject; vivirá dentro del calendario de T-158) con `prefabRival`
   (GameObject), `premioGanar` y `premioPerder` (enteros ≥ 0), `apuesta` (`DatosApuestaPelea`, opcional), `presentador` (`ConfigPresentador`,
   opcional), `liga` (0..4) y `esJefe`. Con un `Crear(...)` estático para pruebas.
2. `DirectorPelea`:
   - `bool PrepararCartel(CartelPelea cartel, float fraccionPremio)`: solo en Preparando, sin apuesta anunciada y sin `rivalEnEscena`; si ya
     creó un rival de otro cartel, lo destruye. Usa del cartel el prefab del rival, el premio (× `fraccionPremio`, redondeado al entero), la
     apuesta y el presentador (rearma la máquina si cambian las líneas). Si no se puede, devuelve false y no cambia nada.
   - `bool QuitarCartel()`: vuelve a lo serializado en el prefab (rival, premio, apuesta, presentador, liga 0 y jefe según sus campos). Solo en
     Preparando. `Reiniciar` NO quita el cartel (lo quita quien lo puso).
   - Campos serializados nuevos `liga` (0) y `esJefe` (false) para las peleas sin cartel. Propiedad `Info` (`InfoPelea` de la pelea en curso o
     preparada) y `CartelActual` (null sin cartel).
   - Publica `EventosPelea.PublicarPresentacion(Info)` una vez por pelea y por revancha, justo antes de la presentación o del conteo.
   - En las líneas del presentador, `{liga}` se cambia por `IProgresoLigas.NombreLiga(liga)` (por `Registro<IProgresoLigas>`); sin servicio o
     con liga 0, por vacío. `{alias}` y `{rival}` siguen igual.
   - El premio que paga y muestra la pantalla es el del cartel si hay; sin cartel, el de `DatosPremioPelea` como hoy.
3. `FlujoPeleaTutorial.prefab` (con su creador, idempotente): `esJefe` true y `liga` 4 (la noche del prólogo es tu primera pelea de campeonato,
   personaje-progresion.md). `FlujoPelea.prefab` (el de Alan) y `FlujoPeleaCasera.prefab` no cambian de valores.
4. PlayMode `Tests/PlayMode/Pelea/CartelPeleaPruebas.cs` (como PremioPeleaPruebas: `ArenaDePrueba`, cartera instanciada, jugador real y daño
   real; `RecibirDanio` solo para rematar) y EditMode para el reemplazo de `{liga}` si lo sacas a una función pura.

## Carpetas permitidas
Assets/_Proyecto/Pelea/ (Datos/, Flujo/, Editor/, Prefabs/ solo con creadores), Assets/_Proyecto/Tests/EditMode/Pelea/,
Assets/_Proyecto/Tests/PlayMode/Pelea/
No tocar: Core/, Progresion/, IA/, Economia/, UI/, `FlujoPelea.prefab`, escenas, todo lo de Alan.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa (salvo los 9 fallos conocidos de Alan en Arena)
- [ ] CartelPeleaPruebas: con `FlujoPeleaCasera` y un cartel (prefab `RivalBasico`, premio 900, liga 1), al ganar se abonan exactamente $900 y la
      pantalla muestra "+$900"; con `fraccionPremio` 0.5 se abonan $450; tras `Reiniciar` y `QuitarCartel`, la siguiente pelea es contra EL
      CHATARRAS y paga $600 (el premio de la casera)
- [ ] CartelPeleaPruebas: `PeleaPresentada` se publica una vez por pelea con el alias real del rival, la liga y `EsJefe` del cartel; en el
      tutorial se publica con liga 4 y `EsJefe` true
- [ ] CartelPeleaPruebas: con un doble de `IProgresoLigas` registrado, una línea "LIGA {liga}: {rival}" se ve en la pantalla de la arena como
      "LIGA SÓTANO: <ALIAS>"; sin el doble, como "LIGA : <ALIAS>" sin errores
- [ ] `PrepararCartel` durante el conteo, en el resultado o con una apuesta anunciada devuelve false y no cambia el rival ni el premio
- [ ] PremioPeleaPruebas, ApuestaPeleaPruebas, FlujoPeleaPruebas y las de la emboscada y el tutorial siguen en verde sin cambios de valores

## Criterios de sensación (para un humano)
- Ninguno todavía (la cartelera llega en T-159 y T-160).

## Pruebas automáticas
- PlayMode: CartelPeleaPruebas. EditMode: lo que salga puro (reemplazo de líneas).

## Fuera de alcance
- Los carteles reales de las 20 peleas (T-158). Elegir la pelea en el mostrador (T-159). Textos "PELEA DE JEFE" y "CAMPEÓN DE LA LIGA" (T-161).
  Luces de la jaula (Alan, opcional).
