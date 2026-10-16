# T-129 Primera pelea casera y mostrador del clandestino
Dueño: Tony | Rama: tony/T-129-pelea-casera | Modelo: Sonnet 5.5, medium
Depende de: T-128

## Objetivo
Existe la primera pelea casera de la demo, la que sí se puede perder: un rival sin piedad ("EL CHATARRAS"), su flujo de pelea sin
revancha con premio de $600 y un mostrador en el clandestino donde el jugador a pie pulsa E para pelear con su mecha de la historia.
Es el paso 6 de demo.md y responde "cómo es la primera pelea casera y contra quién" con una propuesta.

## Contexto
- Diseño: demo.md ("despiertas con el mecha fregado y juegas tu primera pelea casera, que sí puedes perder o ganar"; pregunta abierta
  "cómo es la primera pelea casera y contra quién"), personaje-progresion.md (perder no es game over: reparas y vuelves a retar), ciudad.md
  (el clandestino abre de noche).
- Decisiones: D-064 (el rival normal no lleva `PiedadRival`: la casera se puede perder), D-066 (presentador), D-tony-44 y D-tony-48 (premio
  casera 600, 0 al perder), D-tony-65 (`EntradaArena`, T-128). Propuesta nueva: D-tony-66.
- Ejemplos: `IA/Editor/CreadorRivalTutorial.cs` (rival con datos propios), `Pelea/Editor/CreadorFlujoPelea.cs` y `CreadorPresentador.cs`,
  `Mecha/Taller/BancoTaller.cs` (un `IInteractuable` de Tony), `Economia/Datos/DatosPremioPelea_Casera.asset`.

## Qué hacer
1. Rival casero (con su creador en IA/Editor/): `DatosRival_Casero` (alias "EL CHATARRAS") y `DatosMecha_Casero` (vida 90, daño 10; un poco
   más lento que el jugador) en IA/Datos/, y `IA/Prefabs/RivalCasero.prefab` (como `RivalBasico`, SIN `PiedadRival`, color de chatarra
   oxidada para distinguirlo). Valores propuesta (D-tony-66).
2. `Pelea/Prefabs/FlujoPeleaCasera.prefab` (con su creador): `DirectorPelea` con `RivalCasero`, premio `DatosPremioPelea_Casera`,
   `permitirRevancha` en false, `empezarAlArrancar` en false y un presentador corto `ConfigPresentador_Casera` (dos líneas:
   "PELEA CASERA EN EL SÓTANO" y "¡{alias} CONTRA {rival}!", la segunda de grito).
3. `Progresion/Prologo/MostradorPeleas.cs` + `Progresion/Prologo/Prefabs/MostradorPeleas.prefab` (con creador): un `IInteractuable` que lleva
   dentro su `EntradaArena` y su `FlujoPeleaCasera` (hijo). Texto y permiso, en este orden:
   - entrada en curso o jugador no a pie: no se ofrece (`PuedeUsar` false, texto vacío);
   - con `IHistoria` registrada y etapa distinta de `PeleaCasera` y `Libre`: "Hoy no hay peleas caseras" (no se usa);
   - con `IRelojMundo` registrado y de día: "Peleas caseras de noche · abre 20:00" (no se usa; la hora sale del reloj);
   - si no: "Pelea casera · premio $600" (el monto sale del premio del flujo con `FormatoDinero`); E → `EntradaArena.Empezar(flujo)`.
   Evento `PeleaTerminada(ResultadoPelea)` (reenvía el de su `EntradaArena`) para el director (T-133).
4. Pruebas_Prologo (en `CreadorEscenaPrologo.Crear`): `MostradorPeleas.prefab` en "Sitio_Mostrador".
5. PlayMode `Tests/PlayMode/Progresion/PeleaCaseraPruebas.cs` (Pruebas_Prologo, entrada simulada para caminar, E y pelear):
   a) de noche, a pie frente al mostrador: el HUD dice "E · Pelea casera · premio $600"; E: tras el fundido pelea contra "EL CHATARRAS";
      gana (se permite rematar con `RecibirDanio` tras daño real al jugador): dinero +600, termina a pie en "Clandestino_Salida";
   b) con el mecha de la historia al 20 % de vida antes de entrar, el rival lo deja en KO con golpes reales (sin `RecibirDanio` sobre el
      jugador): gana el Rival, el dinero no cambia y el mecha vuelve al taller;
   c) de día el mostrador dice "Peleas caseras de noche · abre 20:00" y E no hace nada; con una `IHistoria` en `PeleaTutorial` dice
      "Hoy no hay peleas caseras";
   d) consola sin errores y un solo rival en escena en todo momento.

## Carpetas permitidas
Assets/_Proyecto/IA/ (Datos/, Prefabs/, Editor/), Assets/_Proyecto/Pelea/ (Datos/, Prefabs/, Editor/), Assets/_Proyecto/Progresion/Prologo/
(con Prefabs/ y Editor/), Assets/_Proyecto/Escenas/Tony/Pruebas_Prologo.unity (con su creador), Assets/_Proyecto/Tests/PlayMode/Progresion/,
Tests/PlayMode/IA/, docs/capturas/
No tocar: Core/, todo lo de Alan, `FlujoPelea.prefab`, `RivalBasico.prefab`, `RivalTutorial.prefab`, otras escenas.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa
- [ ] PeleaCaseraPruebas a) a d) pasan
- [ ] `RivalCasero.prefab` no tiene `PiedadRival` y su `IPeleador.Nombre` es "EL CHATARRAS" (prueba)
- [ ] Captura docs/capturas/T-129-1.png: el aviso del mostrador en el HUD a pie frente al clandestino

## Criterios de sensación (para un humano)
- Pruebas_Prologo, Play, de noche: entra al clandestino, E en el mostrador y pelea. ¿EL CHATARRAS se siente como un rival de sótano que
  sí te puede ganar, distinto del campeón del tutorial? Con el mecha fregado (al 45 %), ¿ganar es posible pero pide cuidado?
- ¿El presentador corto da ambiente sin alargar?

## Pruebas automáticas
- PlayMode: PeleaCaseraPruebas.

## Fuera de alcance
- Apuestas, ligas y más rivales (T-F7); qué pasa después de la casera en la demo (T-133); el clandestino de Alan
