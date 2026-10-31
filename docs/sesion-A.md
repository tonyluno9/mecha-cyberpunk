# Sesión A — Jugabilidad (carpeta principal: mecha-cyberpunk)
31-oct-2026. Corre al mismo tiempo que la Sesión B (carpeta ../cyberfix-b, rama tony/sesion-b). Lee también docs/sesion-B.md para saber qué NO tocar.

## Tus carpetas (solo estas)
Assets/_Proyecto/: Personaje/, Combate/, IA/, Pelea/, Progresion/, Economia/, Vehiculos/, Trafico/, Core/, UI/ (menos UI/Creditos/),
sus Tests/, y las escenas y prefabs del juego (Ciudad_Prologo, Barrio.prefab, etc.).
NO toques: Assets/Arte/Audio/, la carpeta del GestorSonidos y sus configs de volumen, UI/Creditos/, docs/sesion-B.md, docs/bugs.md (solo léelo).

## Prioridades (en orden)
1. Termina T-430 implantes a pie (rama tony/T-430-implantes-a-pie, nota en docs/estado-tony.md).
2. Tutorial ligero: aviso la primera vez de cada acción (moverse, apuntar, disparar, cubrirse Q, rodar CTRL, recargar, subir al carro, robar, mapa M, misiones). Se puede apagar en Opciones.
3. Entre cada tarea, lee docs/bugs.md (lo escribe la Sesión B jugando la demo) y arregla los que caigan en tus carpetas; márcalos como resueltos.
4. Ganchos de sonido que falten: lee docs/sonidos-faltan-enganchar.md (lo escribe la Sesión B) y agrega la llamada GestorSonidos.Tocar("<ID>") en el lugar del código que diga; no cambies el GestorSonidos.
5. Peatones que huyen al oír disparos; tráfico que frena y pita.
6. Carros que se dañan (humo, fuego, explotan) y disparar desde el carro (pistola y fusil).
7. Pandilleros que patrullan su zona a pie.
8. Día y noche conectado: más peatones de día, más pandilleros de noche.
9. Guarida que sube de nivel (armería, garaje, cuarto de la pandilla).
10. Teléfono: mensajes de aliados y profes, contratos.

## Reglas para no chocar con la Sesión B
- Rama de integración: tony/fase-g; una rama por tarea; IDs de tarea T-440 a T-469.
- Antes de cada merge a main: git fetch, mezcla origin/main en tu rama, compila. Si hay conflicto en un archivo de la Sesión B, quédate con la versión de origin/main. Nunca corras CreadorTodo; si regeneras Barrio.prefab, corre ColocarT250.Colocar y la colocación de T-419.
- Merge a main con push al terminar cada tarea (yo, Tony, lo autorizo). Si el push es rechazado: git pull con merge y reintenta. Nunca force push.
- Sonnet para todo; Opus solo si algo falla 2 veces. Pruebas solo de lo que tocas; sin suite completa. Commit y push cada vez que termines algo.
- Después de cada tarea: regenera Builds/Juego y 3 líneas de qué probar en docs/guia-para-jugar.md. (Builds/Demo lo regenera la Sesión B.)
- Si se acaba el límite, espera y continúa. Desde el 12-nov solo bugs.
