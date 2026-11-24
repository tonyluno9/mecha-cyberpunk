# Reporte 24-nov mañana (sesión A, main)

Tareas de docs/tareas/24nov-manana/, en orden: T97, T96, T94, T90, T93, T92. Una rama por tarea, todas con merge en main y push.
El merge de vr/quest3 (novena tanda, VR9) ya estaba en origin/main antes de integrar; se hizo fast-forward y no hubo choques.
Respaldo de las partidas: Desktop/videojuego/respaldos-partida/24nov-manana/.

## Qué quedó

| Tarea | Estado | En corto |
|---|---|---|
| T97 cinemáticas con frames | HECHA | C01 (campo y despertar) sigue en video. C02–C05 ahora son 29 imágenes fijas con zoom/paneo lento: cada una dura su voz + 1 s, o 3 s si no tiene voz, con fundidos de 0.4 s. En VR salen en la pantalla de cine. OMITIR igual. |
| T96 carga infinita al menú | HECHA | Causa: la pantalla de carga solo se cerraba cuando aparecía un jugador, y el menú no tiene. Ahora también se cierra al cargar el menú (desde muerte, pausa y REINTENTAR). |
| T94 fase 0 SKULLS | PARCIAL | Mínimo 2 min de SKULLS (luego hasta 10 muertos, tope 4 min). Llegan grupos de 2–3 cada 12 s, con 20 % armados, y no aparece policía en esa fase. Al terminar llegan patrullas y cambia la música. |
| T90 menú de Tino en su cinemática | HECHA | Al empezar cualquier cinemática se cierran el banco de Tino, las tiendas, el Doc, las expendedoras, las ruedas, el mapa, el inventario y los avisos. El HUD vuelve al terminar. |
| T93 HAS MUERTO estilo nuevo | HECHA | Usa el estilo de la pausa (H10): panel de estadísticas, REINTENTAR y MENÚ PRINCIPAL con resaltado, y se navega con flechas y Enter. En VR, en el panel del láser. |
| T92 doble de balas | HECHA | Cargador, reserva, compras y drops ×2 en todas las armas (pistola 16, fusil 60, escopeta 8…). La granada y los puños siguen con 1 en la mano. |

## Qué no quedó

- T94: los SKULLS armados siempre llevan **pistola**. La escopeta pide cambiar la IA de pandilleros (fuera del área de la tarea) y en el proyecto no hay bate, así que los demás pelean a puños.
- T93: no se revisó en captura; falta verlo en pantalla (PC 1920x1080 y panel VR).
- Fallas de prueba que ya estaban antes (salen igual en corridas del 23-nov, no son de hoy): HudH10Pruebas (estrellas 3/5 y ayuda de recargar) y el error de la jaula del Clandestino en ArmasMercadoPruebas / MercadoArmasT720Pruebas.

## Pruebas que corrieron

- Compilación limpia.
- EditMode de las clases nuevas y tocadas: 86 de 86 pasan.
- PlayMode UI/cinemáticas: 32 de 34. Las 2 fallas son las viejas del HUD. C02 completa en 73.5 s, con 0 cuadros sin imagen. CierreMenusCinematicaT90 pasa.
- PlayMode VR (recarga, cinemáticas VR36, inicio VR): 9 de 9.

## Builds

Uno tras otro, sin otro Unity corriendo (el último build de wtVR fue build-vr9 a las 08:45):
- ConstruirDemo.ConstruirDemoPandillas → Builds/Demo: Succeeded, 0 errores, 3112 MB (09:04).
- ConstruirDemo.ConstruirJuego → Builds/Juego: Succeeded, 0 errores, 3112 MB (09:06).
Logs: Logs/build-demo-24nov-manana.log y Logs/build-juego-24nov-manana.log.

## Qué probar jugando

1. Partida nueva: el campo y el despertar en video, el resto de las cinemáticas con imágenes fijas, cada una con su voz y sin cortes negros. OMITIR funciona. Revisar en VR que la imagen que se mueve no maree.
2. Comprar y mejorar con Tino: en su cinemática ya no se ve el menú, y al terminar recuperas el control y el cursor.
3. Salir de Tino al modo libre: unos 2 min de SKULLS que te buscan (pocos con pistola). Luego llegan patrullas y cambia la música.
4. Morir: HAS MUERTO con el estilo nuevo. Probar MENÚ PRINCIPAL (que no se quede la carga encima) y REINTENTAR. También pausa → salir al menú. En PC y VR.
5. Armas: cada una con el doble de balas en el cargador y la reserva. La compra de munición da el doble. El tambor del revólver gira bien.
