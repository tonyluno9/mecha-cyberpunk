# Revisión nocturna de Alan

Rama para probar: `alan/fase-1`. Actualizado: 2026-10-13.

## Qué probar primero
1. Escena Clandestino.unity, Play (la cámara es la de la cabina del jugador).
2. Juzga: neón cyberpunk con el centro legible, escala de mecha (reja de 6 m), y si la pantalla de 8×4.5 se ve (hoy solo asoma arriba; subir o inclinar la cámara si molesta).

## Por tarea

### T-020 Arena greybox del clandestino
- Verificado automáticamente: todos los criterios (compilar, EditMode 181/181, PlayMode 113/113; capturas docs/capturas/T-020-1.png y -2.png, idénticas).
- Para ti: neón, niebla, escala y visibilidad de la pantalla (ver arriba).
- Decisiones del agente: ninguna relevante.

### T-040 Barrio greybox y cámara libre
- Verificado automáticamente: todos los criterios (EditMode 188/188, PlayMode 135/135; capturas docs/capturas/T-040-1..3.png).
- Para ti: Pruebas_Alan.unity, Play, WASD+ratón (Shift corre, F vuela) de la casa al clandestino: vueltas, calles no vacías, finales creíbles, escala de persona y de mecha.
- Decisiones del agente: casa en Manzana_4; muros en vez de calles perimetrales; jaula a 400 m de la cámara.

### T-041 Reloj y día y noche
- Verificado automáticamente: todos los criterios (EditMode 206/206, PlayMode 144/144; capturas T-041-1/2). La prueba x30 corre 45 s reales (>1 día de juego), no los 3 min literales.
- Para ti: Pruebas_Alan.unity, Play: N=22:00, M=12:00, T=x30; que amanecer y anochecer no sean saltos bruscos, que la noche se sienta fría con niebla y farolas (en la captura el cielo es casi negro).
- Decisiones del agente: LuzNocturna con MaterialPropertyBlock; CicloDiaNoche pinta el fondo de la cámara como la niebla; CreadorReloj ya no pisa los configs (T-045).

### T-042 Locales, puertas y horarios
- Verificado automáticamente: todos los criterios (EditMode 213/213, PlayMode 160/160; capturas T-042-1..3; consola x30 3 min sin errores).
- Para ti: Ciudad.unity, E y luego W frente al bar: que la puerta abra sin estorbar; de noche, que se vea de un vistazo qué está abierto; el estado del letrero del casino se lee pequeño a ~13 m.
- Decisiones del agente: ninguna relevante.

### T-043 Bar interior
- Verificado automáticamente: todos los criterios (EditMode 213/213, PlayMode 165; capturas T-043-1..3; 3 min con 18 ciclos entrar/salir sin errores).
- Para ti: Pruebas_Alan, 22:00, puerta del bar, E y W: sin tirón al cruzar; el interior sale algo oscuro (muros y techo); tamaño entre mesas.
- Props de Blender (barra, mesa, DJ, letrero) en Assets/Arte/Mundo/Bar/, aún no colocados en el bar.

### T-044 Pelea en Clandestino (cierre de H1)
- Verificado automáticamente: todos los criterios (EditMode 213/213, PlayMode 166/166; capturas T-044-1/2).
- Para ti: Clandestino.unity, Play, pelea tres veces con R: que la luz de la jaula deje leer al rival y su aviso naranja, que la pantalla se lea desde la cabina y que se sienta igual que en Pruebas_Tony.
- Decisiones del agente: colector propio de errores/excepciones en vez de LogAssert (el "[Combate] KO" de Tony es un Debug.Log y no cuenta como fallo).

### T-045 Correcciones de la revisión de la fase 1
- Creadores: nuevo `CreadorTodo.Crear` (Mundo/Editor) corre CreadorArena, CreadorBarrio, CreadorReloj, CreadorLugares, CreadorBar y CreadorPeleaClandestino en orden; los demás avisan en su encabezado. Correr alguno suelto borra lo posterior.
- CreadorReloj y CreadorLugares ya no pisan assets existentes. RelojMundo publica con cola (reentrada segura). ArenaPelea.Centro está en la superficie del piso (y = 0).
- Menores: PuertaLocal no cierra con alguien en el umbral; LugarMundo avisa si el reloj aparece tarde (LetreroLugar se actualiza); prueba de Canvas PantallaPelea a menos de 0.1 m. Sin prueba propia: el aviso tardío del reloj en LugarMundo.

### T-074 Carro: física y manejo
- Verificado automáticamente: 12 criterios (EditMode 234/234, PlayMode 182/182; captura T-074-1).
- Para ti: abre CarroViejo.prefab sobre un piso, TomarControl(true), Play: W S A D Espacio; acelera sin prisa, frena firme, no derrapa solo; escalón de 0.2 m a 5 m/s da un brinquito.

### T-076 Arte de Blender a Unity
- Verificado automáticamente: 13 FBX, 31 materiales, 13 prefabs en Mundo/Prefabs/Kit (EditMode 241/241, PlayMode 182/182; capturas T-076-1/2). La prueba de orientación usa vértices Neon_* porque la original (promedio de z de Concreto_Manchas) estaba mal definida.
- Para ti: CreadorArte.Vitrina() en una escena sin guardar: colores iguales a los renders de Blender, neón que brille (el bloom llega con T-079), nada negro, rosa ni volteado.

### T-075 Subir y bajar del carro, cámaras
- Verificado automáticamente: 9 criterios (EditMode 255/255, PlayMode 201/201; capturas T-075-1/2; 10 subidas y bajadas sin errores).
- Para ti: Pruebas_Alan con cámara libre: a <2.5 m de la puerta del conductor, E sube y E baja (inmediato, sin saltos); dentro, WASD; C alterna primera y tercera persona sin marear ni temblor.

### T-077 Finales del barrio y retenes
- Verificado automáticamente: todos los criterios medibles (EditMode 262/262, PlayMode 210/210; capturas T-077-1/2; carro a 40 m con W 8 s en los 6 extremos sin salir).
- Para ti: de noche, en el extremo norte y el este: que el retén se entienda como cierre y no como muro de videojuego; la torreta llama la atención sin cegar; el jersey y la reja salen oscuros (un poco más de luz sería un extra).

## Bloqueos y preguntas
- Nada todavía.
