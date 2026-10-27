# T-195 Pandilleros a pie: la calle de noche pega, y los ojos y los brazos se notan a pie
Dueño: Tony | Rama: tony/T-195-pandilleros-a-pie | Modelo: Sonnet 5.5, medium
Depende de: T-192, T-193, T-194

## Objetivo
De noche, en la calle, una pandilla a pie te cae si te acercas: te persiguen, avisan su golpe y te pegan. Con los puños los tumbas en 4
golpes, con BRAZOS I en 3 y con BRAZOS II en 2; con la pistola en 3 tiros; con PIERNAS les sacas ventaja corriendo. Con OJOS, el HUD a pie
te muestra la vida del que miras y te avisa su golpe. Es lo que le da sentido a los implantes a pie y la forma de "caer en la calle" (T-196).

## Contexto
- Diseño: ciudad.md ("De noche ... aparecen las pandillas"), personaje-progresion.md (Brazos, Piernas, Ojos; "Si mueres en la calle ...");
  hoja-de-ruta.md T-F3 ("combate a pie básico (golpes simples y pistola)").
- Decisiones: D-tony-15 (el jugador a pie no es IPeleador; su vida es `VidaAPie` y publica el impacto con objetivo null), D-012 (aviso del
  golpe en la preparación). Propuestas: D-tony-165, D-tony-168 (lectura del rival), D-tony-169 (pandilleros a pie: valores y por qué en este
  bloque), D-tony-171 (balance: golpes para tumbar a uno), D-tony-173 (lo de Alan).
- Contratos: `IRecibeDanio`, `IVida`, `ModeloVida`, `InfoDanio`, `IJugador` (`Modo`), `IRelojMundo` (`EsDeNoche`, `FranjaCambio`), `ICartera`,
  `IImplantes`.
- Código: `Combate/Lectura/ILecturaRival.cs` (T-194), `Personaje/Pruebas/MunecoAPie.cs` (vida a pie greybox con parpadeo y KO),
  `Personaje/Vida/VidaAPie.cs`, `Personaje/Combate/GolpeAPie.cs` y `PistolaAPie.cs` (a qué le pegan), `IA/Rival/RivalBasico.cs` (aviso del golpe
  con el brazo en naranja), `UI/HudAPie/HudAPie.cs`, `IA/Editor/CreadorRivalPandilla.cs` (modelo de creador de prefab de IA).

## Qué hacer
1. `IA/APie/PandilleroAPie.cs` con `IA/Datos/DatosPandilleroAPie` (asset `DatosPandilleroAPie_Base` con los valores de D-tony-169) y una regla
   pura `IA/APie/ReglaPandilleroAPie.cs` (esperar, perseguir, atacar, volver): cápsula greybox con `CharacterController`; `IRecibeDanio` con
   `ModeloVida` (parpadeo al recibir, cae en KO y desaparece unos segundos después); implementa `ILecturaRival` con `EsMecha` = false.
   Persigue al jugador a pie que entra a su radio; golpe con aviso visible (brazo en naranja durante la preparación) que daña la `VidaAPie`
   con `InfoDanio`; recarga; se rinde y vuelve si el jugador se aleja de su punto más que la correa. No ataca a un jugador en KO, en carro ni
   en mecha (si el jugador sube al mecha, se dispersan y vuelven a su punto). No usa armas.
2. `IA/APie/PandillaAPie.cs`: un grupo de 3 alrededor de su punto, solo de noche (aparecen al anochecer y se retiran al amanecer si nadie
   pelea); como máximo 2 atacan a la vez; los vencidos vuelven la noche siguiente. Cada pandillero vencido suelta su dinero (`ICartera.Abonar`,
   `MotivoDinero.Otro`; el HUD lo anuncia). Prefabs `IA/Prefabs/PandilleroAPie.prefab` y `PandillaAPie.prefab` con un creador idempotente en
   `IA/Editor/`. Alan puede colocar `PandillaAPie.prefab` en sus calles (D-tony-173).
3. HUD a pie con OJOS (`UI/HudAPie/`): con `verVidaRival`, una barra delgada turquesa con la vida del pandillero en la mira (a 15 m o menos;
   nivel 2 con números); con `anticipacionAtaque`, "¡GOLPE!" junto a la mira cuando el que miras está preparando o le falta menos que la
   anticipación. Sin OJOS no se muestra nada. Textos en el `Config` del HUD.
4. `CreadorEscenaImplantes`: coloca `PandillaAPie.prefab` en "Sitio_Pandilla" (T-193).
5. Pruebas:
   - EditMode `Tests/EditMode/IA/ReglaPandilleroAPiePruebas.cs`: decisiones de la regla (radio, correa, recarga, no atacar en KO, en carro ni
     en mecha, máximo 2 atacando).
   - PlayMode `Tests/PlayMode/Progresion/Implantes/PandillaAPiePruebas.cs` en Pruebas_Implantes con entrada simulada (WASD, Shift, clic
     izquierdo, cambiar a pistola con su tecla, ratón para apuntar; atajos permitidos y anotados: `AvanzarHasta` para la noche y el día
     siguiente, `Colocar` cerca del sitio, implantes por código):
     a) de día no hay pandilla; a las 21:00 aparecen 3;
     b) caminar hacia ellos: persiguen; quedarse quieto: el daño recibido por golpe y el ritmo coinciden con los datos, y nunca pegan más de 2
        a la vez;
     c) golpes para tumbar a uno: 4 sin implante, 3 con BRAZOS I, 2 con BRAZOS II (calculados desde los assets); 3 tiros de pistola;
     d) correr (Shift) lejos más allá de la correa: vuelven a su punto; con PIERNAS I el tiempo para salir de su radio es menor (se imprime);
     e) con OJOS I el HUD muestra la barra del que miras (coincide con su vida ±1 %) y "¡GOLPE!" antes de su preparación; sin OJOS no muestra nada;
     f) vencer a los 3 paga 3 × su dinero; al día siguiente no están y la noche siguiente vuelven;
     g) subir al mecha a su lado: se dispersan y no pegan; consola sin errores; `MedidorFrames` sin tirones fuera de su tolerancia.
   - Captura docs/capturas/T-195-1.png: a pie frente a dos pandilleros, uno en aviso (brazo naranja), con la barra de OJOS y "¡GOLPE!", revisada a ojo.

## Carpetas permitidas
Assets/_Proyecto/IA/APie/ (nueva), Assets/_Proyecto/IA/Datos/ (solo DatosPandilleroAPie y su asset), Assets/_Proyecto/IA/Editor/,
Assets/_Proyecto/IA/Prefabs/ (solo los prefabs nuevos, con el creador), Assets/_Proyecto/UI/HudAPie/, Assets/_Proyecto/UI/Datos/ (solo el Config del HUD a pie),
Assets/_Proyecto/UI/Editor/ (si el HUD lo necesita), Assets/_Proyecto/Progresion/Prologo/Editor/CreadorEscenaImplantes.cs,
Assets/_Proyecto/Escenas/Tony/Pruebas_Implantes.unity (solo con el creador), Assets/_Proyecto/Tests/EditMode/IA/,
Assets/_Proyecto/Tests/PlayMode/Progresion/Implantes/, docs/capturas/
No tocar: Core/, Personaje/ (si `VidaAPie` o el golpe necesitan algo, se anota y se pide), los rivales mecha (`RivalBasico` y sus datos), las
otras escenas, todo lo de Alan.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa (salvo los fallos conocidos de Alan en Arena) y `playmode-por-area` no marca ningún área "LIMITE"
- [ ] ReglaPandilleroAPiePruebas y PandillaAPiePruebas pasan 3 corridas seguidas; los golpes y tiros para tumbar a uno y el dinero se calculan
      desde los assets y se imprimen
- [ ] Las pruebas viejas a pie, del HUD y de IA siguen en verde (GolpeAPiePruebas, PistolaAPiePruebas, HudAPiePruebas, PandillaPruebas,
      RivalBasicoPruebas)
- [ ] Correr el creador de la pandilla y `CreadorEscenaImplantes` dos veces no cambia los prefabs ni la escena (md5 igual)
- [ ] Captura T-195-1 revisada a ojo

## Criterios de sensación (para un humano)
- Pruebas_Implantes de noche, sin implantes y luego con BRAZOS y PIERNAS: ¿la pandilla da miedo sin implantes y ganas de volver con cromo?
  ¿El aviso del golpe se lee a tiempo para hacerte a un lado? ¿Correr con PIERNAS para escapar se siente como un poder?

## Pruebas automáticas
- EditMode: ReglaPandilleroAPiePruebas. PlayMode: PandillaAPiePruebas.

## Fuera de alcance
- Caer en KO y despertar en la clínica (T-196: aquí el KO a pie se queda como hoy), pandilleros con armas, calor por pelear a pie, reputación
  por pelear a pie, pandillas en Ciudad.unity (Alan, cuando quiera), animaciones y modelos finales. Hackeo.
