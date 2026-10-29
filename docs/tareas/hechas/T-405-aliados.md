# T-405 Tu pandilla: reclutar hasta 3 aliados que te siguen, pelean y suben de nivel
Dueño: Tony | Rama: tony/T-405-aliados | Modelo: Sonnet 5.5, medium | MODO RÁPIDO
Depende de: T-404 (respeto de LA FOSA, `ITerritorio`, mapa) y T-402 (pandilleros armados).

## Objetivo
En LA FOSA esperan tres chavos del barrio. Con E y el respeto suficiente se unen a ti: te siguen a pie, pelean contra los pandilleros
rivales con pistola y suben de nivel. F les dice que esperen o que te sigan.

## Contexto
- Reutiliza el cerebro y el arma de T-402 (`IA/APie/PandilleroAPie.cs`, `PistolaPandillero`, turnos) y el muñeco `ModeloHumano.prefab`.
- Interacción con E: `Core/Jugador/IInteractuable.cs` (ya la usan mostradores y puertas).
- Decisión: D-tony-221 (números, reclutables, contrato y guardado).
- **Contrato nuevo de Core (Tony lo autorizó en esta tarea)**: `Core/Territorio/IPandillaJugador.cs`, para que UI y la historia vean a
  tus aliados sin ver IA: `int Cantidad`, `int Maximo` (3), `IReadOnlyList<Transform> Aliados`, `int Nivel(int indice)`,
  `string Nombre(int indice)`, `bool Siguiendo`, `event Action Cambio`, `event Action<int> SubioDeNivel`. `Registro<IPandillaJugador>`.
  Agrega la clave `aliados` a "Claves en uso" de `Core/Guardado/IGuardable.cs`.

## Carpetas permitidas
Core/Territorio/IPandillaJugador.cs, Core/Guardado/IGuardable.cs (solo el comentario), IA/, UI/HudAPie/, UI/Mapa/, UI/Editor/,
Personaje/Entrada (acción Ordenar F, con su creador), Progresion/Prologo/Editor/CreadorCiudadPandillas.cs,
Escenas/Ciudad_Prologo.unity (solo con código), Tests/EditMode/IA/, Tests/PlayMode/IA/, docs de Tony.

## Qué hacer
1. `IA/Aliados/ReglaAliado.cs` (pura): experiencia y nivel (nivel 2 con 3 vencidos, nivel 3 con 8); vida 80/110/140 y daño por tiro
   6/8/10 por nivel; a quién atacar (el pandillero rival más cercano a menos de 20 m de ti, primero el que te está disparando).
2. `IA/Aliados/AliadoAPie.cs`: muñeco naranja con pistola. Siguiendo: a 3 a 5 m detrás de ti, corre si te alejas más de 8 m; si
   te subes a un carro, te espera donde bajaste y te alcanza al bajar (sin subir al carro). Esperando (F): se queda donde está.
   Pelea con el aviso y las reglas de tiro de T-402. Si cae: desaparece y a los 45 s vuelve a 15 m detrás de ti. Un vencido suyo
   también cuenta para tu respeto (`ITerritorio.RegistrarVencido`) y para su experiencia.
3. Los pandilleros rivales también disparan a los aliados (el más cercano de jugador o aliados), con los mismos turnos.
4. `IA/Aliados/PandillaJugador.cs` (`IPandillaJugador`, `IGuardable` "aliados": quiénes, nivel y experiencia). Al cargar, los aliados
   aparecen junto a ti.
5. `IA/Aliados/Reclutable.cs` (`IInteractuable`): EL CHATO (gratis, respeto de LA FOSA ≥ 10), LA GÜERA ($200, ≥ 40), EL TOPO ($350, ≥ 70).
   Sin respeto o sin dinero, el texto de E dice qué falta ("TE FALTA RESPETO EN LA FOSA: 40"). Un campo `soloHistoria` para que la
   historia de T-406 controle al CHATO.
6. HUD: fila de aliados abajo a la izquierda ("CHATO II ▮▮▮▯") y "¡CHATO SUBIÓ A NIVEL II!" 3 s. En el mapa, un punto naranja por aliado.
7. `CreadorCiudadPandillas.T405()`: los 3 reclutables en LA FOSA bajo `GTA_T405` (frente a la casa, en la esquina y junto al taller),
   y `PandillaJugador` en la escena o en ServiciosProgresion.

## Pruebas (solo estas 2 nuevas)
- EditMode `ReglaAliadoPruebas.NivelesYObjetivo`: 3 vencidos → nivel 2; 8 → nivel 3; con dos rivales a 10 y 15 m, ataca al de 10,
  pero si el de 15 te disparó, a ese.
- PlayMode `AliadosPruebas.ReclutarSeguirYPelear`: respeto de LA FOSA 10 (con `ITerritorio`); E junto al CHATO → `Cantidad` 1; caminar
  W 4 s → el aliado queda a menos de 8 m; un pandillero a 12 m → el aliado le dispara y le baja la vida en menos de 6 s.

## Al terminar
- Build: `-executeMethod MechaCyberpunk.Progresion.Prologo.Editor.ConstruirDemo.ConstruirJuego` (ver T-400) → Builds/Juego/MechaCyberpunk.exe.
- docs/guia-para-jugar.md, sección "Juego de pandillas": 3 líneas de qué probar.
- Bitácora de una línea: docs/bitacora/<fecha>-tony-T-405.md. Fila de docs/estado-tony.md en "hecha". Merge a tony/fase-g.

## Sensación (Tony)
- ¿Los aliados ayudan o estorban? ¿Se siente tu pandilla? ¿Subir de nivel se nota? ¿Te gustaría darles armas tú?

## Fuera de alcance
- Que suban a tu carro, darles armas, que defiendan zonas solos, que mueran para siempre.
