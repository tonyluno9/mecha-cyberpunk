# Prueba de Tony 6-nov 00:10 (build de prueba de la noche del 5-nov) — tareas

Corte 12-nov. Todo es obligatorio salvo lo marcado "después". Cada sesión además juega su parte y arregla por su cuenta lo que
se vea roto, encimado, flotando, fuera de escala o hecho con primitivos. Reparto en 2 sesiones; cada una en su rama, merge a main
después de cada tarea. Assets nuevos (los genera Tony): ver sección "Assets" al final; si aún no están, deja el hueco listo y sigue.

Lo que a Tony le gustó y NO se toca: caminar, correr, salto, giro, subir/bajar del carro, manejo y reversa del carro, aceleración
y giro de la moto, puestos del tianguis, ciudad de Nova, sombras, el día, cómo conviven los NPC, el nombre del lugar arriba,
robar un carro sin patrulla cerca sin que te persigan, la caída al morir, la pistola en la mano.

---
## SESIÓN 1 — Jugador, combate, vehículos, policía, enemigos (y termina el menú que ya lleva)

### Jugador y combate
- **Arma equipada sin apuntar = caminar y correr NORMAL** (la pistola baja en la mano). Solo al apuntar se usan las poses de arma.
  Esto deshace lo de T-651 (capa Piernas con Pistola_/Rifle_Caminar al no apuntar).
- **Cámara al apuntar: personaje centrado horizontalmente** en pantalla, cámara detrás y un poco más alta para que la mira quede
  arriba del hombro/cabeza y el personaje no la tape. Que se vea bien al disparar.
- **Agacharse**: Ctrl agacha (alterna). Agachado se camina lento; agachado + Shift camina agachado más rápido. Agachado + Espacio =
  rodar. Quitar la rodada en cualquier otro caso (sin agacharse no rueda). Agachado sirve para cubrirse.
- **Golpes**: no se puede dar el siguiente golpe hasta que acabe la animación del anterior (nada de spam). La reacción al recibir
  un golpe debe ser corta y poder cortarse con un golpe o un esquive, para poder devolver o esquivar.
- **Destello del disparo**: el actual es horrible, quitarlo. Uno pequeño y muy breve (usar F01 cuando esté).
- **Sonido del disparo**: siempre el mismo sonido de disparo por arma. Quitar los sonidos de impacto en suelo, paredes y carros que
  hacen que cada disparo suene distinto. Quitar también el sonido feo de cuando le haces daño a un enemigo.
- **Balas sin rayo láser**: quitar los rayos de las balas del jugador, SKULLS y policía. Como mucho un trazo finísimo y cortísimo.
  Nadie dispara a través de barricadas/muros: si no hay línea de vista, no hay disparo.
- **Daño por zona**: la cabeza hace mucho más daño que el cuerpo (headshot).
- **Muerte**: al morir, pantalla de "HAS MUERTO" con estilo del juego antes de reaparecer en la clínica.
- Revisar el daño progresivo misterioso cerca del robot de Nova / quemándose: encontrar de dónde sale y arreglarlo. El robot de Nova
  debe poder hacer daño.

### Vehículos
- Moto: 20 % más chica. Manos justo en el manubrio y pies en los estribos. Quitar el tambaleo feo al ir montado. Frenar mucho más
  rápido (casi instantáneo). Humo de daño saliendo del motor/centro de la moto, no del aire.
- Chocar hace menos daño que un disparo. Chocar a alta velocidad en moto avienta al jugador (cae y se levanta).
- **Nada sale volando por los cielos**: revisar los impulsos de choque entre vehículos (moto y carro salieron volando y explotaron).
  Los carros no explotan sin razón; solo por daño acumulado real, y con aviso claro.
- Carros: frenar más rápido, aceleración más viva como la moto. Algunos carros del tráfico van demasiado lento.
- Al robar un carro con conductor, el conductor sale (lo sacas), cae al suelo y se levanta asustado; es testigo y puede llamar.
- Aliados se suben al carro con animación por la puerta (no volando).
- Carro un poco grande comparado con el jugador: revisar escala de los carros (junto con la sesión 2 si hace falta, avisando).

### Policía
- Las patrullas usan la misma física de carro que el jugador con un conductor IA (no giran sobre su eje, no se deslizan raro).
  Tienen collider sólido: no se pueden atravesar. Se pueden robar como cualquier carro.
- Robar una patrulla = te buscan siempre (la patrulla tiene GPS), llegan desde donde estén.
- Al alcanzarte, los policías se bajan del carro y te disparan, se cubren con la patrulla. Necesita modelo de policía (C01).
- El dron de la policía también dispara cuando te busca.
- Dispararle a la policía o a su carro debe tener efecto (daño, reacción, más búsqueda).

### SKULLS (pelea del callejón / tutorial)
- Bug: solo aparecen 3 y vuelven a aparecer al irte y regresar; el último no aparece y la misión se traba. Que aparezcan todos de una
  vez, que no se repitan, y que la misión termine al matarlos a todos.
- Llegan en un carro: se estacionan, se bajan con animación, se cubren detrás del carro, se agachan y disparan con lógica
  (asoman, disparan, vuelven a cubrirse). Su cobertura ahora se ve rara.

### Historia de la demo (nueva parte, con dinero)
- Matar enemigos suelta dinero. Además, en la demo: los SKULLS asaltan a un aliado, le quitan el dinero, el aliado muere (no se puede
  salvar) y huyen en carro. Los persigues, les disparas desde el carro, les ponchas una llanta (las llantas se pueden ponchar), se
  frenan, los matas y recuperas el dinero. Con ese dinero luego te curas, te pones implantes y mejoras el arma.
  Meterlo en la ruta de la demo y en RecorridoDemoPruebas.

---
## SESIÓN 2 — Mundo, interiores, menús de taller y clínica

### Casa y cuarto
- Quitar la bolsa de basura del centro del cuarto. Las mochilas están volteadas: ponerlas derechas, apoyadas.
- **Puertas reales**: la puerta del cuarto y la de la casa se abren con E (animación de la puerta, cruzas caminando). Nada de
  teletransporte al salir o entrar. Aplicarlo a todas las entradas de interiores que se puedan.

### Ciudad
- Quitar las imágenes de cruz / logo que están en las casas (como los letreros que se quitaron).
- La calle infinita junto a la casa: que llegue a algo (otra calle real con edificios y vuelta). Revisar todos los callejones sin
  límite y las partes rotas de la calle: cerrarlas o terminarlas.
- Taller del mecánico: Tony no lo encuentra. Que se vea desde la calle frente a la casa (letrero de asset, portón abierto, luz,
  carro en el elevador) y que esté en el mapa.
- Cielo raro: arreglar el skybox (de día y de noche).
- Líneas pintadas de la calle: amarillas al centro, blancas de carril, cruces peatonales (textura M01/M02 o decals).
- Huecos de bala: hoy sale un cuadrado al disparar a paredes/muebles. Usar decal de hueco de bala (D01 concreto, D02 metal).
- Mundo más grande y variado: puentes y pasarelas a los que se pueda subir (escaleras o rampas) para ver la ciudad desde arriba.
  El puente de Nova que está atravesado entre dos edificios sin llegar a nada: conectarlo con escaleras/rampas o quitarlo.
- Más variedad de carros en el tráfico con V09–V11 cuando estén.
- **Quitar el papel picado (banderitas) de TODOS lados.** Tony nunca lo pidió.

### Interiores
- CIC (Tino): Tino atraviesa un mueble; mesas de trabajo muy chicas; pizarra encimada en la pared; lámpara flotando en el escritorio.
  Revisar todo el CIC: se ve mal.
- I+D+I (El Doc / Joel): le gusta el área. El Doc un poco más grande. Barril encimado en otro modelo; frascos encimados.
- Barrido general: ningún modelo encimado con otro, flotando o atravesado en todo el juego.

### Clínica, taller de armas e implantes (menús)
- El Doc: en vez de "no te va a doler mucho, mijito", te dice que te sientes en la silla; E en la silla → el jugador se sienta
  (animación), cámara cercana, secuencia corta de curación (brazo robótico, luz, barra de vida subiendo), te levantas curado.
- Tino: te dice "acércate al banco de trabajo para mejorar tu arma". E en el banco → **menú de mejora de armas épico**: el modelo 3D
  del arma en estilo holograma (shader cian con líneas), se gira y observa con el mouse; eliges qué mejorar (cargador: más balas;
  cañón: daño; mira: precisión; etc.), y la pieza elegida se separa del arma (vista explotada) mostrando la mejora, con costo,
  estadísticas antes/después y efecto de sonido. Al salir, que se sienta que mejoraste. Usar las imágenes holográficas H01–H04 en la
  lista de armas e ícono del HUD.
- Implantes: el menú de implantes con la misma presentación (holograma del cuerpo, elegir implante).

---
## Después (no esta noche)
- Profundizar la vida de los NPC.
- Puertas del carro que se abren y ruedas.

---
## Assets que genera Tony (prompts en el chat) — dónde van
- imageassets/armas/: W02_Escopeta, W03_Fusil, W04_Francotirador (imagen para Tripo; los .zip de Tripo a imageassets/modelos3d/armas/).
- imageassets/ui/hologramas/: H01_Pistola, H02_Escopeta, H03_Fusil, H04_Francotirador.
- imageassets/vfx/: F01_Destello, D01_HuecoConcreto, D02_HuecoMetal.
- imageassets/calle/: M01_LineasAmarillas, M02_CrucePeatonal.
- imageassets/modelos3d/: C01_Policia.zip (personaje, va a Mixamo como los demás), V09_Taxi.zip, V10_Pickup.zip, V11_Van.zip.
