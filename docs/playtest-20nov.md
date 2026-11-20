# Prueba de Tony 20-nov mañana (build de las 02:55)

IMPORTANTE: varias cosas se marcaron como hechas en reporte-20nov.md y NO lo están en el build (cuarto, primitivos, armería, muro).
Nada se da por hecho sin comprobarlo en el build con captura desde el punto de vista del jugador.

## Le gustó (NO se toca)
Que el muro cierre gran parte del mapa · la torre del muro · el efecto en el agua al pisarla · el menú de armas nuevo · poder elegir
con el mouse · que te ataquen enemigos con mejores armas.

## 1. Inicio y casa
- SIGUE apareciendo en el cuarto. El cuarto se elimina de verdad: la partida empieza con el personaje levantándose en medio de la calle.
- Al morir aparece en la clínica del Doc (eso está bien), pero el Doc ahora es más alto que el jugador: revisar escala.

## 2. Jugador y animaciones [Blender, uno por uno con Tony]
- Caminar y correr se ven rarísimos con los clips nuevos: revisarlos en Blender clip por clip.
- Subir escaleras, saltar corriendo y saltar en general: arreglar.
- Sonido de los pasos: arreglar.
- Botón para cubrirse (cobertura) que el jugador pueda usar.

## 3. Personajes y NPCs [Blender, uno por uno]
- Revisar TODOS los personajes en Blender: escalas y animaciones rotas.
- Civiles de tamaños muy distintos entre sí: misma escala.
- Una civil acostada en la calle, bugueada (no está muerta).
- Robots rotísimos: revisar.
- Maleantes demasiado agresivos: primero provocan y buscan pleito, después atacan; no matan de inmediato.

## 4. Armas
- Granada: no tiene modelo. Pedir al chat de arte y pasar por Tripo.
- Las balas no van donde apunta la mira: chocan antes; las hitbox de postes y objetos son demasiado grandes (a 100 m pega en un poste).
- Francotirador: hace demasiado daño; el zoom no funciona con la cruceta (lo toma como cambio de arma).
- Disparar a una patrulla de lejos no le hace nada.
- Demasiadas armas en la ruleta cuando compras todas: limitar (por ejemplo, una por tipo o por ranuras).
- Al abrir la rueda de armas suena un golpe: cambiar por el sonido de cámara lenta.

## 5. Enemigos, policía y drones
- Algunas patrullas no reaccionan: un modelo de patrulla está configurado como carro civil.
- Las estrellas suben demasiado rápido.
- Enemigos y drones aparecen todos de golpe: que lleguen poco a poco (mejora para después).
- El marcador de dónde están los drones debe ser el mismo indicador de daño; ver drones solo con el visor (las gafas).

## 6. Vehículos [Blender]
- Revisar todos los carros en Blender, uno por uno con Tony (y la policía).
- Los carros NO salen de la ciudad: ni se generan afuera ni se salen; solo ciclan dentro del muro.
- Sonido del carro al subirte: no le gusta, cambiarlo.
- Puertas que se abren: solo en los superdeportivos (revisar regla).

## 7. Implantes y consumibles
- Las prótesis también dan más vida.
- Consumibles: se compran en la tiendita de la esquina (ED02), no en máquinas expendedoras. Poner la tienda.
- No deja usar el segundo consumible (papitas) después de la bebida.
- Quitar los números y el texto repetido arriba de los consumibles; con el logo basta.

## 8. Ciudad y muro
- Quitar TODAS las formas primitivas y edificios hechos con cubos (siguen muchos).
- Meter los edificios nuevos (ED del mercado y demás) que siguen sin aparecer.
- Assets flotando en el cielo.
- Muro: terminar de cerrar todo el mapa, sin huecos por donde salir; las puertas gigantes del muro NO se pueden atravesar.
- Los sacos (costales CB02) se ven pintados: revisar material.
- Revisar las calles parte por parte.
- Entrada del mercado: no le gusta, rehacer (pedir referencia).
- Armería: el que estaba al revés era el mostrador de ENFRENTE del armero; el de la pared estaba bien y ahora quedó al revés.
  Regresar el de la pared, voltear el de enfrente y pegar al armero más hacia la pared.
- Agua: sin salpicadura, solo el efecto al pisar; y si se puede, ese efecto en todos los pisos mojados.

## Para Blender (sesiones uno por uno con Tony)
Todos los personajes (escala y animaciones), todos los carros (incluida la policía), locomoción del jugador (caminar, correr,
escaleras, saltos).

## Para el chat de arte
Granada (modelo). Entrada del mercado (referencia). Edificios que falten para quitar los primitivos.
