# Reporte de la noche del 23-nov (T50–T76)

Tareas en docs/tareas/noche-23nov/ (cada documento tiene su sección "Estado"). Todo está en main con merge --no-ff y push.
Capturas en docs/capturas/_tareas/<tarea>/; las de la sesión A del final tienen HUD (herramienta Verif23).
Sesión A (Opus dirigiendo, subagentes Sonnet): T54, T66, T67, T69–T76 y lo de docs/para-sesion-A.md. Sesión B: el resto.
Builds: Demo (ConstruirDemoPandillas) y Juego (ConstruirJuego), uno tras otro, al final (ver abajo).

## Paso 0
- Había cambios sin guardar de T49 (roce de ropa) en la rama tony/T-1690-sin-roce: se commitearon y se mergearon (1ca4910).
- Los .meta del chat de Blender (remapeos de material) y las tareas de la noche quedaron en tony/blender-23nov (f564ed1).
- Los .blend de más de 100 MB de imageassets/blender/_REVISION_TONY siguen fuera de git, como antes.

## Tabla

| Tarea | Sesión | Estado | Rama / merge | Qué quedó |
| --- | --- | --- | --- | --- |
| T50 Créditos y Cargar con el menú nuevo | B | HECHA | T-1762 | Cargar/Guardar y Créditos con el estilo H10 y la música del menú. |
| T51 Sin ruta de misiones | B | HECHA | T-1755 | Ya no hay línea turquesa en el piso ni ruta punteada en el minimapa. La ruta del mapa grande (M) no se tocó. |
| T52 SKULLS del inicio | B (mapa: A) | HECHA | T-1754 + T-1715 | Esperan a media calle, se plantan de frente, empujan y se van corriendo. A movió la patrulla 10 m y unos huacales del callejón: en 3 de 3 corridas llegan sin atorarse. |
| T53 Humanos con las animaciones del jugador | B | HECHA (parcial) | T-1753 | 28 visuales humanos usan los clips del jugador. Los SKULLS sin arma y los guardias pesados de Nova quedan aparte (ver T53.md). |
| T54 Mitad de carros estacionados | A | HECHA | T-1705 + escena ffefaee | 84 → 51 estacionados activos. De los 59 que se podían tocar quedan 26; los otros 25 son de guion, robables de encargos o motos/policía. Junto a la casa quedan 2 (V01a y V13) más los 2 de guion. Tablas en T54/. |
| T55 Sin roce de ropa | B | HECHA | T-1750 | T49 confirmada; también se quitó la respiración al correr. |
| T56 Holograma de prótesis | B | HECHA | T-1759 | Se instala en ~3.5 s sobre la silueta; mismos clips con o sin prótesis. |
| T57 Menú Tab con el cuerpo | B | HECHA | T-1760 | Silueta holográfica con zonas; con clic o soltando Tab se activa. |
| T58 Humo al caer | B | HECHA | T-1761 | Bocanadas redondas y transparentes. |
| T59 Tino vende y mejora; fuera la armería | B (mapa: A) | HECHA | T-1755 + T-1715 | Banco de Tino con COMPRAR/MEJORAR. A apagó los letreros de la armería y cerró la entrada con la puerta y dos rejas PU03. MostradorMercadoNegro sigue activo. |
| T60 Guardar el arma | B | HECHA | T-1756 | El arma aparece y desaparece de la mano. |
| T62 Avisos en ventana central; expendedora con ratón | B | HECHA | T-1755 | Aviso con ENTENDIDO que pausa; la expendedora se usa con ratón. |
| T63 Consumibles corriendo | B | HECHA | T-1758 | Efecto al instante, con sonido y "+N VIDA". |
| T64 Sin sonido de kill | B | HECHA | T-1752 | |
| T65 Sin sonidos viejos | B | HECHA | T-1751 | 47 IDs viejos en silencio (lista en T65/desconectados.md). |
| T66 Libera el Tianguis | A | HECHA | T-1701 + escena ffefaee + T-1714 | Causa: 2 de los 4 grupos del Tianguis estaban vacíos en DirectorPandillas y la misión nunca podía cerrarse. Ahora hay 7 SKULLS en el centro de la calle. La misión cuenta solo vivos y activos, y el que se atore más de 20 s se reubica junto al jugador. En Play pasó a VENCE AL JEFE 3 de 3 veces. El "SKULL flotando" eran pandilleros de las riñas parados uno encima de otro: ahora se reparten en anillo y se separan. |
| T67 Enemigos en el minimapa | A | HECHA | T-1706 + T-1714 | Rojo para enemigos en pelea a ≤60 m y siempre para los de misión; azul para policías; violeta para Nova. El minimapa escondía todo icono a menos de 16 m del jugador; ya no esconde a los hostiles. Capturas con HUD en T67/. |
| T68 Siempre de noche | B | HECHA | T-1755 | Ajustes > Gráficos > MOMENTO DEL DÍA (NOCHE por defecto). |
| T69 Flotantes y primitivos | A | HECHA | T-1700 (23ce0f9) | 27 flotantes → 0 tinacos, aires, antenas o cajas: 19 se bajaron a su techo. Quedan 6 justificados: el elevador del taller y 5 candiles del casino. Primitivos 1615 → 1607: se apagaron 8 franjas de cubo sin textura. Los demás tienen textura o son neones, calcomanías de calle o el cielo (lista en T69/). |
| T70 Charcos blancos | A | HECHA | T-1704 | Cuando el reflejo cae fuera de cámara, el shader del charco usa un fondo oscuro y limita reflejo y fresnel. En Play de noche, el Tianguis desde 3 ángulos no tiene charcos blancos. |
| T71 Civil tirado | A | HECHA | T-1703 | Era el cuerpo de un pandillero. Ahora todo cuerpo (pandillero, policía a pie o civil) se apaga a los 45 s fuera de vista (máximo 120 s a la vista). En Play se apagaron entre 38 y 52 s. |
| T72 Refinería sin fin | A | HECHA | T-1702 | Nova con tope de 4 vivos, reposición a 60 s y solo con el jugador a más de 80 m. En Play, 71 s después de matarlos seguía sin aparecer ninguno. |
| T73 Un balazo en la cabeza | A | HECHA | T-1707 | Cabeza ×2 (antes ×3); francotirador ×4. Pistola: el pandillero muere con 2 tiros, el policía con 3 y Nova con 9. Tabla en T73/tabla.md. |
| T74 Zonas de glitch | A | HECHA | T-1709 + escena + T-1714 | Dos zonas: el elevador del taller (-29, 2, -73) y un candil del casino (70.5, 4.2, 50.5). Zumbido de transformador en loop 3D y un parpadeo leve cada 1-4 s. |
| T75 Atropellar | A | HECHA | T-1708 | Ya existía (T25, desde 14 km/h). Se comprobó con un civil y un policía a pie: salen volando y caen. |
| T76 Puertas de carros | A | NO QUEDÓ | T-1711 (nota) | Los carros básicos de Tripo son de una sola malla, sin puerta separada. Hay que separarla en Blender; PuertasCarro ya funciona en carros que traen bisagra. |

## Qué probar (Tony)
1. **Libera el Tianguis (T66, T67):** jugar la guerra con los SKULLS. Los 7 deben estar en la calle a la vista y marcados en rojo en el minimapa. Al matarlos debe salir VENCE AL JEFE. Hacerlo 2 o 3 veces.
2. **Riñas entre pandillas:** que nadie se vea flotando encima de otro y que sigan peleando entre ellas.
3. **Minimapa:** puntos azules de policía con estrellas y violeta de Nova cerca de la refinería.
4. **Refinería (T72):** matar a los guardias y quedarse cerca: no deben reaparecer.
5. **Headshots (T73):** con pistola, un pandillero aguanta un tiro a la cabeza. Ojo: el revólver sí mata de un tiro a la cabeza.
6. **Charcos (T70):** calle del mercado, que no haya manchas blancas. Si los reflejos de neón se ven muy apagados, sube `TopeReflejo` (0.55 → 0.8) en Charco.shader.
7. **Cuerpos (T71):** tras una pelea, al darse la vuelta y volver ~1 min después, los cuerpos ya no están.
8. **Glitch (T74):** acercarse al elevador del taller y al candil del casino: zumbido y parpadeo. El volumen se ajusta con `AmplificacionZumbido` en ZonaGlitch.
9. **Carros (T54):** calle de la casa con solo 2 estacionados y el resto de la ciudad con menos carros.
10. **Inicio (T52):** los SKULLS salen del callejón sin atorarse.
11. **Armería (T59):** sin letrero y cerrada; comprar con Tino.
12. **Atropellar (T75):** civil y policía.
13. **De B, verlos en el build:** consumibles corriendo (T63), súper salto con humo (T58) y que no haya siseo, "¡hey!" ni sonido de kill (T55, T64, T65).

## Pendientes y notas
- El destino MERCADO del mapa (CatalogoDestinosMapa) sigue apuntando al local de la armería, que ahora está cerrado.
- En la captura de la armería (para-sesion-A-e/despues-armeria-frente.png) hay un robot blanco en pose T a la izquierda de la entrada: revisarlo.
- La leyenda del mapa grande no tiene filas para policía ni Nova.
- Al quitar las franjas de cubo (T69), la barrera C18 de T504 se quedó sin sus franjas turquesa.
- Pruebas PlayMode que ya fallaban en main por "[Arena] Faltan referencias en JaulaClandestino": SkullsTianguisT713, MapaPlayT747 (3), MenusCyberfix, GuardadoRanuras y PersonajesApoyo. También fallan PandillaAPiePruebas (9 de 10), Headshot del pandillero y HudH10 (2); los dos primeros son iguales en main, HudH10 no se comparó.
- Al probar en el editor se pudo escribir el autoguardado del juego (paso 12 o 3); la ranura 1 de Tony no se tocó.
