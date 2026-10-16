# T-090 Flashback.unity
Dueño: Alan | Rama: alan/T-090-flashback-escena | Modelo: Sonnet 5.5, medium
Depende de: docs/diseno/concepto-historia.md y demo.md (qué se ve en el flashback).

## Objetivo
La escena Flashback.unity con el espacio del recuerdo (lugar, luz, ambiente), cargable por separado, con un punto de aparición a pie. La secuencia y su lógica son de Tony (T-F6).

## Qué hacer
1. Leer el flashback en docs/diseno/ y anotar en la bitácora qué lugar y qué hora necesita.
2. `Mundo/Editor/CreadorFlashback.cs` crea Assets/_Proyecto/Escenas/Flashback.unity con código de editor (no YAML a mano): piso, muros, luz de recuerdo cálida con bloom y un `IPuntoAparicion` `Flashback_Inicio`.
3. Añadirla a EscenasMundoPruebas (carga sin errores).

## Carpetas permitidas
Assets/_Proyecto/Mundo/, Assets/_Proyecto/Escenas/Flashback.unity, Tests de Mundo, docs/capturas/.

## Criterios de aceptación
- [ ] La escena carga en PlayMode sin errores y expone el punto de aparición
- [ ] Captura: docs/capturas/T-090-1.png
- [ ] `bash herramientas/verificar.sh todo` pasa

## Fuera de alcance
La secuencia, los diálogos y el cambio de escena (Tony).
