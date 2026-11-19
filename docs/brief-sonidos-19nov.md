BRIEF COMÚN: sonidos nuevos del 19-nov (orquesta Opus; tareas T-1470..T-1476, una rama cada una)

- Proyecto Unity 6000.3 + URP 17.3 "mecha-cyberpunk" (juego CyberCity). Lee CLAUDE.md y .claude/rules/unity-csharp.md de tu worktree.
- Tu worktree NO tiene Library: NO abras Unity ni compiles. Escribe C# con cuidado (solo APIs reales de Unity 6, mismos `using` y namespaces
  que los archivos vecinos, respeta el asmdef del área). El orquestador compila, corre pruebas por clase y te devuelve los errores.
- Crea tu rama desde main: `git fetch origin && git checkout -b tony/T-147x-<nombre> origin/main`. Commit seguido (add explícito, nunca -a), sin push.
- Fuente de los sonidos (fuera de git, NO la subas): C:\Users\Angel Perez\Desktop\videojuego\mecha-cyberpunk\3d sonidos\
  Lee ahí LEEME_SONIDOS.md e INDICE_SONIDOS.csv. Nombres según docs/lista-sonidos-cybercity.txt.

## Cómo suena algo en el juego (ya existe)
- Contrato Core: MechaCyberpunk.Core.ISonidos por la fachada `Sonidos` (Reproducir, ReproducirEn, ReproducirBucle(id, transform), DetenerBucle, FijarTono).
- Progresion/Sonido: CatalogoSonidos (ID -> clips; asset Assets/_Proyecto/Progresion/Datos/CatalogoSonidos.asset, lo arma
  Progresion/Editor/CreadorCatalogoSonidos.Crear leyendo Assets/Arte/Audio/<SEC>/<ID>[_NN].wav), ReglaMezclaSonidos (política por ID:
  grupo, voces, volumen, variación de tono, tiempo mínimo, distancia), GestorSonidos (reproduce), interruptores por ID (un ID que no está
  encendido NO suena).
- El ID de un archivo es su nombre sin `.wav` y sin el sufijo de variante `_NN` (ARM_W01_DISPARO_01.wav -> ID ARM_W01_DISPARO;
  VEH_ACELERON_01/_02 -> ID VEH_ACELERON con 2 variantes).

## Dónde quedan los archivos nuevos (T-1470)
Assets/Arte/Audio/<carpeta de origen>/ : 01_UI, 02_Armas, 03_Impactos, 04_Explosiones, 05_Pasos, 06_Jugador, 07_Interaccion,
08_Consumibles, 09_Vehiculos, 10_Policia. Mismos nombres que traen, salvo los recortes:
- VEH_NORMAL_ARRANQUE_RALENTI_APAGAR_01/_02/_03 -> VEH_NORMAL_ARRANQUE_01.._03, VEH_NORMAL_RALENTI_01.._03 (loop), VEH_NORMAL_APAGAR_01.._03
- MOTO_ARRANQUE_01, MOTO_ARRANQUE_02, MOTO_ARRANQUE_RALENTI_APAGAR -> MOTO_ARRANQUE_01.._03, MOTO_RALENTI_01.._03 (loop), MOTO_APAGAR_01.._0N (los que lo traigan)
- VEH_PESADO_RALENTI -> VEH_PESADO_RALENTI_01 (loop) y VEH_PESADO_APAGAR_01 si lo trae; VEH_PESADO_ARRANQUE se queda igual
- PAS_TIERRA_SECUENCIA -> PAS_TIERRA_01.._NN; PAS_GRAVA_SECUENCIA -> PAS_GRAVA_01.._NN
- VEH_ALARMA y VEH_LLANTAS_GRAVA (9 y 12 MB) -> se recortan a un loop limpio de 10 s o menos, con el mismo nombre
Las grabaciones largas originales no se copian (solo los recortes).

## IDs nuevos (los que puede pedir tu código)
UI_MOVER UI_SELECCIONAR UI_ATRAS UI_ERROR UI_PESTANA UI_MENU_ABRIR UI_MENU_CERRAR UI_LOGO_INTRO HUD_LOGRO HUD_NOTIFICACION HUD_VIDA_BAJA
TIE_COMPRAR TIE_EQUIPAR TIE_DESEQUIPAR
ARM_W01_DISPARO (pistola) ARM_W02_DISPARO ARM_W02_BOMBA (escopeta) ARM_W03_DISPARO ARM_W03_RECARGA_CARGADOR (fusil) ARM_W04_DISPARO ARM_W04_CERROJO
(francotirador) ARM_W06_DISPARO ARM_W06_SACAR (revólver) ARM_W07_DISPARO ARM_W07_CARGA ARM_W07_RAYO (plasma) ARM_NOVA_DISPARO ARM_NOVA_CARGA_DISPARO
ARM_GRANADA_SEGURO ARM_GRANADA_REBOTE ARM_COLA_CALLE IMP_LASER_DISPARO IMP_MISILES_FIJAR
IMP_BALA_CABEZA IMP_BALA_CARNE IMP_BALA_CONCRETO IMP_BALA_CRISTAL IMP_BALA_METAL IMP_BALA_ROBOT IMP_BALA_ZUMBIDO
EXP_GRANADA EXP_ONDA FUE_LOOP_CHICO
PAS_CONCRETO PAS_MADERA PAS_METAL PAS_MOJADO PAS_TIERRA PAS_GRAVA
JUG_ATERRIZAJE JUG_RODADA JUG_TREPAR MEL_GOLPE MEL_GOLPE_AIRE MEL_GOLPE_CARNE
INT_HACKEO_EXITO INT_HACKEO_FALLO INT_INTERRUPTOR_LUZ INT_MALETIN_CERRAR INT_PUERTA_CRISTAL INT_PUERTA_MADERA INT_PUERTA_METAL INT_PUERTA_REJA
INT_RECOGER_OBJETO INT_TECLEO_HACKEO
CON_BEBER CON_CURACION CON_LATA_ABRIR CON_PAPITAS_COMER CON_VENDA
VEH_NORMAL_ARRANQUE VEH_NORMAL_RALENTI VEH_NORMAL_APAGAR MOTO_ARRANQUE MOTO_RALENTI MOTO_APAGAR VEH_PESADO_ARRANQUE VEH_PESADO_RALENTI VEH_PESADO_APAGAR
VEH_ACELERON VEH_ALARMA VEH_CHOQUE VEH_CHOQUE_FUERTE VEH_CLAXON_CAMION VEH_CLAXON_NORMAL VEH_CRISTAL_ROMPER VEH_FRENO_FUERTE VEH_LLANTAS_GRAVA
VEH_PUENTE VEH_PUERTA_ABRIR VEH_PUERTA_CERRAR VEH_TOPE VEH_ELECTRICO_ACELERAR VEH_ELECTRICO_ARRANQUE VEH_ELECTRICO_MOVER VEH_ELECTRICO_PASAR
POL_NOVA_AVISO POL_RADIO_AVISO POL_RADIO_DESPACHO POL_SIRENA_CORTA POL_SIRENA_LOOP

## Reparto (cada quien SOLO sus archivos; si necesitas algo de otro, ponlo en tu respuesta final)
- T-1470 importar: copia y recorte (script Python en herramientas/sonidos/), Assets/Arte/Audio/<carpetas nuevas>/, importador de editor
  Assets/Arte/Editor/ImportarSonidos19nov.cs, .gitignore.
- T-1471 catálogo y mezcla: Assets/_Proyecto/Progresion/Sonido/*, Progresion/Editor/CreadorCatalogoSonidos.cs y sus pruebas. Lee las carpetas
  nuevas, hace que los IDs viejos que ya pide el código (ARM_PIS_DISPARO, UI_ACEPTAR, VEH_VIEJO_MOTOR_RALENTI, JUG_PASO_ASFALTO, ...) suenen
  con el archivo nuevo, enciende los IDs nuevos y pone la política de mezcla de TODOS los IDs nuevos (incluye tono base: el subfusil usa W01 alto).
  Las otras tareas NO tocan Progresion/Sonido: piden los IDs de la lista de arriba y ya.
- T-1472 armas, impactos, explosiones (código de Personaje/Combate/armas y explosiones de vehículos).
- T-1473 pasos por piso y sonidos del jugador (Personaje).
- T-1474 UI, HUD, tienda, interacción y consumibles.
- T-1475 vehículos (Vehiculos/ y tráfico de carros).
- T-1476 policía (Trafico/Persecucion, IA/Nova y sus Datos).

## Reglas
- No borres llamadas que ya funcionan: si el código pide un ID viejo genérico y T-1471 lo mapea, déjalo; cámbialo a un ID nuevo solo cuando
  haga falta distinguir (por arma, por material, por piso) o cuando hoy no suena nada.
- 3D (ReproducirEn o bucle con transform) para todo lo que pasa en el mundo; 2D (Reproducir) para UI, HUD y lo del propio jugador en primera persona.
- Sin spam: no pidas un sonido cada frame; los bucles se arrancan una vez y se detienen (DetenerBucle) al terminar o al destruirse el objeto.
- Pruebas: EditMode de tu área en Assets/_Proyecto/Tests/EditMode/<Área>/ con nombre que incluya T147x (y PlayMode por clase si hace falta, con
  entrada simulada). Usa un ISonidos falso para comprobar qué ID se pidió. No dependas del CatalogoSonidos.asset real en las pruebas.
- Datos (ScriptableObject .asset): si cambias un campo de un asset existente (por ejemplo ConfigPersecucion.asset), hazlo con un método de editor
  `public static void Aplicar()` en un script Editor de tu área (el orquestador lo corre), nunca editando el YAML a mano.
- docs/tareas/hechas/T-147x-<nombre>.md con qué sonido quedó en qué evento, y qué falta.
- Respuesta final: rama y último commit, métodos de editor a correr (nombre completo) y en qué orden, clases de prueba, qué IDs pide tu código
  y en qué evento, lo que falta (va a docs/sonidos-faltan.md) y lo "por revisar a oído".
