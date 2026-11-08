# T-747 Mapa grande con las calles reales, nombres sin encimar, tienda del CIC y dron derribable
Dueño: Tony | Rama: tony/T-747-mapa-tienda | Integrada en tony/s1-09nov

## Qué cambió
1. **Mapa grande y minimapa con una captura cenital de la ciudad real** (playtest 07-nov). `CapturadorMapaCiudad` (UI/Editor, se corre con `-executeMethod MechaCyberpunk.UI.Editor.CapturadorMapaCiudad.Generar`) abre Ciudad_Prologo SIN guardarla, cambia en memoria los materiales por un shader que pinta la altura (`AlturaMapaT747.shader`), la fotografía con una cámara ortográfica desde arriba (norte arriba, 400 x 280 m, 6 px/m, 2400 x 1680) y con esas alturas dibuja el estilo M10 (`EstiloM10`): calles oscuras, banquetas, edificios del color de su barrio y más claros cuanto más altos, contorno oscuro, borde claro en techos y sombra al sureste. Sale en `UI/Resources/Mapa/MapaCiudad.png`. La escena no cambia (git no la ve modificada).
   - `MapaImagen`: cuadro del mundo (x -200..200, z -140..140, centro = origen) y cuentas mundo a píxel/uv/dibujo. `BarriosMapa`: Suburbios naranja, El Mercado ámbar, Neón Alto magenta, Nova azul (como las zonas del territorio) con su nombre grande.
   - `PantallaMapa`: la imagen reemplaza las calles planas; 2.0 px/m para que la imagen llene la vista de 800x560 con zoom 1 (ya no es serializado, el valor viejo 2.6 seguía en la escena); las manzanas muestran el color del dueño solo en el borde (siguen siendo `Manzanas[i].color`); nombres de los cuatro barrios.
   - `Minimapa`: la misma imagen debajo de las líneas de calle (bajadas a 0.4 de alfa), con el marco H20 que ya tenía.
2. **T-721 nombres encimados** (MECÁNICO / CASA / CIC): `CalculoEtiquetasMapa` (anti-encimado general): a cada nombre se le busca sitio (abajo, arriba, derecha, izquierda, más lejos) sin tapar otro nombre, iconos, marcas de lugar, objetivo, jugador, nombres de barrio ni etiquetas de manzana. Las etiquetas de manzana ya no crecen con el zoom.
3. **Tienda del CIC**: causa medida: no era un fallo del juego. Desde T-814 Tino ya no abre un mostrador (MostradorCIC apagado a propósito, "HABLAR CON TINO"): las mejoras de armas se compran en su banco de trabajo (`BancoArmas`, "BANCO DE TINO"). RecorridoDemoPruebas seguía esperando la `PantallaTienda` vieja y reportaba los BUG 1 a 3. Se probó el banco real en Ciudad_Prologo con entrada simulada (E abre, Enter mejora y cobra, Esc cierra y devuelve el control) y se actualizó el tramo 09 del recorrido al banco.
4. **Dron (regresión T-745)**: la esfera sólida de T-745 hace falta (las balas ignoran triggers). El dron se mueve por transform, sin Rigidbody, así que no estorba. Se corrigió la prueba: ahora exige una sola esfera chica y sin Rigidbody en vez de "cero colliders". El derribo a tiros lo cubre EfectosCombateT745Pruebas.

## Pruebas
- EditMode: 2237/2237 (nuevas: MapaCiudadT747Pruebas, 9: cuentas mundo-imagen, barrios, anti-encimado, la imagen está alineada con la ciudad: avenidas oscuras y techos claros en sus coordenadas).
- PlayMode filtradas: MapaPlayT747Pruebas (M abre con la imagen y M/Esc cierran; flecha e iconos caen sobre su lugar de la imagen en el mapa y el minimapa; nombres sin traslape con zoom 1 y 3 sobre el mecánico) y TiendaCicT747Pruebas: 4/4. Regresión del mapa (Minimapa, DestinosMapa, RecorridoNombres, TuPandilla, TituloCreacion, MarcadorObjetivo, CirculoBusqueda, PersecucionBarrio.Dron): 15/15. Progresion (ConquistaZonas, Territorio, Negocios): 3/3.
- Nota para pruebas: una clase PlayMode con el mismo nombre que otra de EditMode en el mismo namespace pierde el teclado simulado; por eso la de PlayMode se llama MapaPlayT747Pruebas.
- RecorridoDemoPruebas no se corrió (tarda mucho): solo se cambió el tramo 09 al banco.

## Capturas (revisadas)
docs/capturas/s1e-T-747-mapa.png, -minimapa.png, -mecanico.png (zoom 3 sobre el mecánico) y -tienda.png (banco de Tino).

## Decisiones
- Estilo M10 hecho con las alturas de la ciudad, no con arte pintado: da color por barrio y contornos fieles a las calles reales; el look "dibujado" de M10 lo daría un artista (se puede cambiar `EstiloM10` o reemplazar el PNG).
- El banco de Tino es la tienda del CIC; no se reactivó el mostrador viejo.
