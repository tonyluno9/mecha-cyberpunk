# T-204 Pruebas_Adaptacion: pelear una y otra vez contra el que elijas
Dueño: Tony | Rama: tony/T-204-escena-adaptacion | Modelo: Sonnet 5.5, medium
Depende de: T-200, T-201, T-202, T-203

## Objetivo
Una escena de Tony donde se pelea una y otra vez contra el rival que elijas, con revancha inmediata, guardado (F5 y F9) y una herramienta que
muestra lo que el rival está midiendo y lo que recuerda. Sirve para que Tony sienta la adaptación en 10 minutos sin subir de liga, y es la base
del recorrido de cierre (T-205).

## Contexto
- Decisiones: D-tony-172 (escena propia por bloque y herramienta en `Pruebas/`), D-tony-150 (ensamblado de pruebas propio). Propuestas:
  D-tony-195 (quién se adapta y con qué asset), D-tony-199 (dónde vive; teclas F6, F7 y F8).
- Código modelo: `Progresion/Prologo/Editor/CreadorEscenaImplantes.cs` (creador de escena idempotente), `Progresion/Pruebas/ImplantesDePrueba.cs`
  y `LigasDePrueba.cs` (herramientas con teclas), `Progresion/Pruebas/GuardadoDePrueba.cs` (F5/F9), `Pelea/Flujo/DirectorPelea.cs`
  (`PrepararCartel` solo en Preparando, `Reiniciar`, revancha con R), `Pelea/Datos/CartelPelea.cs` (`Crear`), `Pelea/Pruebas/ArenaDePrueba.prefab`,
  `Pelea/Prefabs/FlujoPelea.prefab` (lo usa Alan: se coloca, no se cambia), `IA/Adaptacion/AdaptacionRival.cs` y `MemoriaRivales.cs` (T-200).
- Pruebas modelo: `Tests/PlayMode/Progresion/Implantes/EscenaImplantesPruebas.cs` y su asmdef (ensamblado anidado; `verificar.sh` lo encuentra solo).

## Qué hacer
1. `Progresion/Prologo/Pruebas/AdaptacionDePrueba.cs` (solo en escenas de Tony; teclas en `InputAction` del inspector, no en el código):
   - lista de rivales: EL CHATARRAS (`RivalCasero`), TORNILLO (`RivalLiga1_1`), DON FIERRO (`RivalLiga1_5`, jefe con EMBESTIDA), VOLTIO
     (`RivalLiga3_1`, _Avanzado) y EL CACIQUE (`RivalLiga4_5`, jefe, _Avanzado);
   - F6: siguiente rival; solo entre peleas (en el resultado aplica el cartel y empieza la pelea siguiente; a media pelea no hace nada y lo dice
     en el panel);
   - F7: adapta al máximo al rival actual (evidencias al umbral de su nivel máximo, respetando `maximoActivas`: quedan GUARDIA y FINTAS) y llama a
     `Releer`; F8: lo olvida (`MemoriaRivales.Olvidar`) y `Releer`;
   - panel de depuración (uGUI en pantalla, solo para esta escena): rival, evidencias y niveles; en pelea, el perfil en vivo (golpes por minuto,
     % cubierto, % castigando) contra sus umbrales; los últimos 3 `Usada`; las teclas.
   - Antes de fijar F6, F7 y F8, grep en los `.inputactions`, prefabs y herramientas para confirmar que siguen libres.
2. `Progresion/Prologo/Editor/CreadorEscenaAdaptacion.cs` → `Escenas/Tony/Pruebas_Adaptacion.unity`: ArenaDePrueba, una instancia de
   `FlujoPelea.prefab` con revancha, `MechaJugador.prefab`, `ServiciosProgresion.prefab` (memoria y guardado), `GuardadoDePrueba`,
   `AdaptacionDePrueba` con su lista, luz y cámara de la cabina como en las otras escenas de prueba. Empieza contra TORNILLO. Idempotente.
3. `Tests/PlayMode/Progresion/Adaptacion/` con su asmdef nuevo `MechaCyberpunk.Tests.PlayMode.Progresion.Adaptacion` (referencias como el de
   Implantes) y `EscenaAdaptacionPruebas.cs` (entrada simulada; `ServicioGuardado.RutaForzada` para no tocar la partida del humano):
   - la escena carga sin errores y empieza la pelea contra TORNILLO;
   - F6 a media pelea no cambia nada; en el resultado cambia de rival y la siguiente pelea es contra él (`DirectorPelea.Rival.Nombre`);
   - F7 y F8 cambian la memoria y la pantalla de la cabina (T-203) lo refleja en la siguiente pelea;
   - el panel muestra lo mismo que `PerfilActual` y la memoria;
   - F5, F8, F9: la memoria vuelve igual;
   - consola sin errores.
4. docs/guia-para-jugar.md: Pruebas_Adaptacion, sus teclas (F6, F7, F8, R, F5, F9) y qué ver en el panel.
5. Captura docs/capturas/T-204-1.png: la escena en pelea con el panel y la pantalla de la cabina, revisada a ojo.

## Carpetas permitidas
Assets/_Proyecto/Progresion/Prologo/Pruebas/, Assets/_Proyecto/Progresion/Prologo/Editor/CreadorEscenaAdaptacion.cs (nuevo),
Assets/_Proyecto/Escenas/Tony/Pruebas_Adaptacion.unity (nueva, solo con el creador), Assets/_Proyecto/Tests/PlayMode/Progresion/Adaptacion/ (nueva),
docs/guia-para-jugar.md, docs/capturas/, docs/bitacora/
No tocar: IA/, Combate/, UI/, Pelea/ (sus prefabs solo se colocan), `MechaJugador.prefab`, `ServiciosProgresion.prefab`, otras escenas, Core/,
todo lo de Alan.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa (salvo los fallos conocidos de Alan en Arena) y `playmode-por-area` lista el área
      "Progresion.Adaptacion" sin "LIMITE"
- [ ] EscenaAdaptacionPruebas pasa 3 corridas seguidas
- [ ] Correr el creador dos veces no cambia `Pruebas_Adaptacion.unity` (md5 igual); `FlujoPelea.prefab`, `ArenaDePrueba.prefab` y
      `MechaJugador.prefab` sin cambios
- [ ] F6, F7 y F8 no chocan con ninguna tecla del proyecto (la bitácora trae el grep)
- [ ] Captura T-204-1 revisada a ojo; la guía tiene la escena y sus teclas

## Criterios de sensación (para un humano)
- Pruebas_Adaptacion, Play, unos 10 minutos: pelea contra TORNILLO como peleas tú, revancha con R, y otra vez. ¿Notas lo que aprendió sin leer el
  panel? ¿El panel te ayuda a entender por qué, o es demasiado? ¿Cambiar de estilo le gana?

## Pruebas automáticas
- PlayMode: EscenaAdaptacionPruebas.

## Fuera de alcance
- El recorrido completo (T-205). Llevar la herramienta a escenas de Alan. Cambiar la cartelera o los premios.
