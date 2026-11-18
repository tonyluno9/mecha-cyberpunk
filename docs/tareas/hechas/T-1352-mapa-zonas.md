# T-1352 Mapa grande y minimapa con las zonas nuevas
Dueño: Tony | Rama: tony/T-1352-mapa-zonas | Modelo: Sonnet 5.5, medium
Depende de: T-1350, T-1351, T-1354 a T-1358 (para regenerar el PNG)

## Objetivo
El mapa grande y el minimapa cubren Malecón, Colonia del Cerro, Autopista y Puerto, con sus nombres, y el HUD dice la zona nueva al entrar.

## Contexto
docs/plan-zonas-18nov.md (rectángulos y límites del mundo), imageassets/referencias/look/M12_MapaExpandido.png.
Código: UI/Mapa/MapaImagen.cs, BarriosMapa.cs, Minimapa.cs, PantallaMapa.cs, UI/Editor/CapturadorMapaCiudad.cs y EstiloM10.cs, UI/HudH10/HudH10.cs.

## Qué hacer
1. MapaImagen cubre x -480..345 y z -330..290 a 4 px/m (3300 x 2480 px). Mapa grande: 0.9 px/m de vista con el cuadro centrado, zoom hasta 6x. Minimapa: la imagen se coloca por el centro del cuadro.
2. BarriosMapa: MALECÓN, COLONIA DEL CERRO, PUERTO, AUTOPISTA con rectángulo, color y punto del nombre; los 4 barrios viejos conservan su cuadro.
3. Minimapa dibuja las CalleZona.Activas como cintas; el mar sale azul oscuro en la imagen (EstiloM10).
4. HudH10: el título de zona usa la zona nueva en vez del barrio de territorio (BarriosMapa.ZonaNuevaEn).
5. CapturadorMapaCiudad usa los límites nuevos y acepta suelos planos grandes; no guarda la escena.

## Carpetas permitidas
Assets/_Proyecto/UI/, Tests/EditMode/UI/, Tests/PlayMode/UI/ y docs/tareas/
No tocar: Core/, escenas, Assets/Arte/, ProjectSettings/, Packages/

## Criterios de aceptación (se comprueban jugando)
- [ ] Con el PNG regenerado, M muestra las zonas nuevas con su nombre y el mar en azul oscuro
- [ ] El minimapa alinea la imagen y las calles de zona con el jugador en el Malecón y en el cerro
- [ ] Al entrar al Malecón el título dice MEGALOPOLIS · MALECÓN

## Pruebas automáticas
- EditMode (MapaCiudadT747Pruebas): BarriosMapa.De en las zonas nuevas y los 4 viejos, ZonaNuevaEn, conversión mundo-píxel ida y vuelta, límites, TramosDeLinea
- La prueba de la imagen generada exige regenerar el PNG (CapturadorMapaCiudad.Generar) tras colocar las zonas

## Fuera de alcance
- Regenerar el PNG (lo corre Tony después de colocar las zonas), rutas GPS por las calles de zona
