# Revisión nocturna de Alan

Rama para probar: `alan/fase-1`. Actualizado: 2026-10-13.

## Qué probar primero
1. Escena Clandestino.unity, Play, mirar desde CamaraVistaArena.
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
- Decisiones del agente: LuzNocturna con MaterialPropertyBlock; CicloDiaNoche pinta el fondo de la cámara como la niebla; CreadorReloj restaura configs a valores del código.

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
- Decisiones del agente: sin LogAssert.NoUnexpectedReceived por el log "[Combate] KO" de Tony.

## Bloqueos y preguntas
- Nada todavía.
