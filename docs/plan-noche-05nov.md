# Plan de la noche — 5-nov-2026 (computadora de Tony)
Reglas de siempre: Sonnet (Opus solo si algo falla 2 veces), rama tony/fase-g y una rama por tarea (IDs T-620 a T-659), pruebas de lo que tocas + RecorridoDemoPruebas al cerrar cada paso, merge a main con push al terminar cada paso (yo, Tony, lo autorizo), marca [x] en docs/pendientes-pruebas.md, nunca force push, no CreadorTodo, git restore a lo que Unity cambia solo. No preguntes: decide lo razonable y anótalo en docs/decisiones-pendientes-tony.md. Si se acaba el límite, espera y continúa.
Paso 0 (repítelo si vuelve a salir el candado): cierra Unity y Unity Hub si están abiertos, confirma que no queda Unity.exe, borra Temp/UnityLockfile si existe. git fetch, tony/fase-g, mezcla origin/main. Haz commit de este archivo y de docs/sonidos-sugerencias.md primero.

## Paso 1 — Sonido: dejar solo lo necesario
Hay demasiados sonidos y suenan como spam. NO borres archivos ni el GestorSonidos: apaga todo por configuración y deja encendidos SOLO estos IDs:
- Armas: ARM_PIS_DISPARO, ARM_ESC_DISPARO, ARM_FUS_DISPARO, ARM_SNI_DISPARO, ARM_PIS_RECARGA, ARM_ESC_RECARGA_CARTUCHO, ARM_FUS_RECARGA, ARM_SNI_RECARGA, ARM_VACIO, ARM_BALA_IMPACTO_CARNE.
- Golpes y daño: PUN_GOLPE_CARNE, JUG_DANIO, JUG_CAE_DERROTADO.
- Carros: VEH_VIEJO_MOTOR_RALENTI, VEH_VIEJO_MOTOR_ACELERA (el motor de todos los carros), VEH_CHOQUE_LEVE, VEH_ALARMA_ROBO, el estruendo de la explosión (T-591).
- Policía: CAL_PATRULLA_SIRENA.
- Interfaz: UI_MOVER, UI_ACEPTAR, UI_CANCELAR, ZON_CONQUISTADA, ECO_COMPRA.
Todo lo demás (ambientes, pasos, voces, jadeos, claxon, puertas, hologramas, perros, tráfico, UI_TEXTO, etc.) queda apagado. Además: límite de voces (máximo 1 a la vez por ID y 12 en total), sin repetir el mismo sonido en menos de 0.1 s, volúmenes parejos. Deja en el ScriptableObject de volumen un interruptor por ID para prenderlos uno por uno después. Prueba: un minuto de juego en la calle con tiroteo y carros sin que se amontonen sonidos.

## Paso 2 — Animaciones nuevas (imageassets/animaciones/)
Importa con el flujo de T-560 (Humanoid, In Place salvo entrar/salir del carro, bucle donde toca, nombres limpios) los dos zips nuevos y conéctalos:
- Carros e interaccion.zip: Carro_Entrar / Carro_Salir (al subir y bajar de cualquier carro, alineado a la puerta), Carro_Manejar (pose sentado al volante, visible por la ventana), Inter_AbrirPuerta (puertas de locales e interiores), Inter_Boton, Inter_Recoger (recoger la pistola y objetos), Inter_Telefono y Inter_VerTelefono (sacar el teléfono del HUD y NPC viendo el celular), Honking Horn (pitar desde el carro).
- Enemigos y jefe.zip: Skull_Idle, Skull_Caminar, Skull_Burla (pandilleros SKULLS: estar parados, patrullar y burlarse al verte); Jefe_Idle, Jefe_Caminar, Jefe_Ataque, Jefe_Burla, Jefe_Muerte (el jefe de los SKULLS: entrada con burla, ataque, muerte); Nova_Idle, Nova_Caminar, Nova_Correr, Nova_Punch, Nova_Barrido, Nova_Flexing, Nova_muerte (guardias robot de Nova: vigilar, perseguir, golpe y barrida, presumir, morir).
- Quita los trucos de IK que ya no hagan falta (pose de manejo si Carro_Manejar sirve; la moto sigue por IK).
- Actualiza docs/animaciones-faltan.md con lo que siga faltando (rodar, sentado programando del Hacker, platicar, recargado en pared, asustado, subir a moto…).
- Capturas en docs/capturas/anim2-*.png.

## Paso 3 — Pendientes
Bloques 6 a 8 de docs/noche-A-alan.md (NPC y aliados, mundo, interfaz) y los [C] sin marcar de docs/pendientes-pruebas.md. Lo de sonido de esos bloques NO se hace (quedó en el Paso 1). Cada arreglo comprobado con su prueba o con entrada simulada, captura si es visual.

Al final: Builds/Demo y Builds/Juego regenerados, reporte corto en docs/estado-tony.md y Unity cerrado.
