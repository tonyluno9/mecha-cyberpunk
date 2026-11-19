# Prueba de Tony 17-nov madrugada + integrar Blender ronda 2 y arte del 16-nov — ÚLTIMA NOCHE ANTES DE ENTREGAR (18-nov)

Regla de esta noche: **estabilidad primero**. Lo que no quede bien, se apaga para la demo y se anota. Cada sesión juega su parte al terminar.
Le gustó (NO tocar): techos con tinacos y saltar entre ellos, escaleras, policía disparando desde abajo, drones, patrullas que
persiguen, carros que quedan quietos al explotar, gente que sale ardiendo de la patrulla, perder el dinero al morir, la rueda con
cámara lenta en cualquier momento, el sonido del revólver, las sombras.
Assets listos: Blender ronda 2 (ver `imageassets/blender/_PENDIENTE_BLENDER_2/_REVISION/BITACORA.md`, RESUMEN FINAL al final) y arte del
16-nov (`imageassets/ui/carga/L01-L06`, `referencias/menus/M33_Arrestado`, `M34_HasMuerto`, `ui/menus/H42`, `referencias/look/K08-K10`).
Clips de PJ01: Scale Factor 1.8367, Humanoid con el avatar de PJ01 (como siempre).

## SESIÓN A — Combate, policía, civiles, jugador, clips
1. **Quitar la intro del asalto** (la cinemática de cuando roban a la chica) por ahora: la partida empieza sin ella y la misión sigue clara.
2. **Barra de prótesis vertical en el lateral izquierdo** (como M31 pero en columna) con teclas **Q PIERNAS, C BRAZOS, X CABEZA**
   (láser y misiles en sus propias teclas, también en la columna si los tienes).
3. **Policía**: te buscan desde el primer daño a un policía o patrulla (golpe o bala), y atacan de inmediato, no al explotar.
   Si destruyes patrullas llegan refuerzos. No dejan de perseguir sin razón. Si estás en un techo, suben por las escaleras (clips
   PJ01_Escalera_*). Caminan y corren disparando con animación (nada de flotar ni ir demasiado rápido).
   **Arresto**: solo si un policía te alcanza y te toca (muy cerca), no por estar cerca un rato.
4. **Estrellas altas = Nova**: al llegar a 4–5 estrellas las estrellas se ponen azul intenso y viene Nova: robots/guardias, guardia pesado
   y, en 5, el jefe final. Los bots de Nova atacan si les disparas.
5. **Civiles**: reciben daño y mueren; al oír disparos cerca huyen asustados; si les pegas, algunos te pegan de vuelta.
6. **Muertes**: enemigos, policías y civiles caen TIRADOS al piso (clips PJ01_Muerte_* bien aplicados o ragdoll), nunca hincados.
   SKULLS que saltan del techo usan la animación de caída (PJ01_Loco_Caida) y aterrizaje.
7. **Daño dentro del carro**: las balas te pueden dar dentro del carro y la moto.
8. **Recarga siempre con su animación** (obligatoria), aunque el arma esté guardada.
9. **Sonidos**: uno distinto por arma (el del revólver se queda), chispeo al cargar el rifle de plasma, motor de los drones.
   **Rifle de plasma** más realista (rayo con textura/VFX mejor).
10. **Integrar clips de Blender ronda 2** (en `imageassets/blender/pj01_clips/`): caminar e idle nuevos (con y sin implante), parkour
    (PJ01_Parkour_SaltarBarda para muros bajos y bardas, TreparBorde para bordes altos, RodarCaida al caer de alto: que SÍ se pueda
    tirar desde el techo y saltar muros bajos), PJ01_Moto_Bajar, cobertura con rifle (PJ01_CoberturaRifle_*), robar carro
    (PJ01_Carro_SacarConductor + PJ01_Conductor_SerSacado sincronizados, alineados al hueco de la puerta).
11. **Prótesis de ataque en el cuerpo** con `imageassets/blender/implantes/PJ01_Implantes_Ataque.fbx` (brazo láser y hombrera, con sus
    vacíos de salida del rayo y los misiles).

## SESIÓN B — Mundo, tiendas, menús, pantallas, carros, armas
12. **Ruedas de carros**: V02, V03, V04, V06, V09, V10 y la moto C19 con `imageassets/blender/carros/*.fbx` (carrocería + ruedas
    separadas que giran, al tamaño de su hueco).
13. **Armas nuevas en piezas** `imageassets/blender/armas/W05-W08.fbx`: en el banco de Tino se desarman como la pistola y el fusil; el
    tambor del revólver y del lanzagranadas gira en cada disparo y en la rueda.
14. **Tienda de armas (El Mercado)**: cursor del mouse activo para navegar; en la tienda solo armas y balas (con nombres claros: "Celdas de
    plasma" son las balas del rifle de plasma). Nada de "Revólver 1", "Rifle de plasma 2" ni otras mejoras: las mejoras se hacen solo en
    el banco de Tino.
15. **Inventario (I)**: equipar/desequipar armas y objetos (qué va en la rueda), ver implantes; que no se sature. Mostrar la tecla para
    consumir la bebida/objeto equipado (en el HUD y en el tutorial). **Todas las máquinas expendedoras** dejan comprar siempre.
16. **Pantallas**: ARRESTADO y HAS MUERTO como M33 y M34. **Pantalla de carga tipo GTA** con L01–L06 (zoom y paneo lento, fundidos).
17. **Mundo**: armería con el mostrador al revés (las armas deben mirar al jugador) y armada según
    `docs/referencias-interiores/entrada-armas-mercado.png`; arreglar la calle de la armería; la calle junto a la casa (techos apilados sin
    fin) rehecha con cuadras con intención según K08 (cuadra), K09 (plaza) y K10 (calle comercial); quitar el letrero "CARGOS" de afuera de
    la casa; quitar el letrero grande NOVA que se hizo y regresar el edificio que había; **calles de noche más iluminadas** (subir un poco
    la intensidad de las lámparas de poste); **todos los barriles explotan** (también los que están juntos, en cadena); textura para las
    azoteas (como la de la calle, pero de techo); que los muros bajos de los techos se puedan saltar y no haya paredes invisibles al
    borde (para poder tirarse).

## PENDIENTE (después de la entrega)
- Intro del asalto bien hecha. Animaciones del jefe. Maletín y tragamonedas (MA01, TR01, TR02 → Tripo). Letreros LT01–LT04.
- Mira que no sube hasta arriba al apuntar (Tony lo deja así). Más parkour entre techos.
