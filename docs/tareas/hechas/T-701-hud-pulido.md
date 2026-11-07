# T-701 HUD pulido como H10
Dueño: Tony | Rama: tony/T-701-hud-pulido | Modelo: Sonnet 5.5
Depende de: T-700

## Objetivo
Que el HUD a pie se vea igual de limpio que H10_HUD.png: sin el panel viejo de zona, sin hora, con el nombre de zona chico que se desvanece, aviso del Doc compacto, minimapa con calles de la escena, vida y etiquetas legibles, muñequito solo de contorno y marcador del objetivo discreto.

## Qué se hizo
1. Panel viejo de zona/respeto (HudTerritorio.PanelZona): ya no se dibuja (CanvasGroup alfa 0, sigue llevando datos). El nombre "EL TIANGUIS" estando en Suburbios sale de los rectángulos de zona del mapa viejo (Progresion/Datos/Territorio/DatosZona_*.asset: tianguis x -47..47, suburbios x < -47) contra la calle de la casa en la ciudad actual; es dato de zona, no del HUD (ver pendientes).
2. Nombre de zona: HudH10.MostrarTitulo, chico (30 px), Rajdhani, arriba al centro, 4 s y se desvanece 1.2 s. "MEGALOPOLIS · SUBURBIOS" del inicio ya no entra a la cola del centro (PantallaHistoriaPandillas lo manda al HUD nuevo). También sale al cruzar a otra zona.
3. Aviso del Doc (AvisoMensaje): recuadro de línea fina turquesa de 560 px, bajo el objetivo, encabezado chico turquesa y mensaje en blanco, se esconde a los 4.5 s.
4. Hora: se quita del HUD a pie (H10 no la lleva; la hora sigue en el teléfono).
5. Minimapa: mismo tamaño (260 px = 13.5 % del ancho, cuadrado). Calles de la escena viva: CatalogoCallesMapa toma las huellas de volúmenes altos (edificios) y CalculoCallesMapa saca los contornos de las manzanas; lo que queda entre ellas son las calles, líneas finas claras. No usa GeometriaBarrio ni nombres, así que sirve si la ciudad cambia. Íconos de lugar de 22 px y sin encimarse (entre sí ni con la flecha). Ruta punteada y cono como en T-700.
6. Vida: número de 34 px dentro del marco de la barra, a la derecha; ESCUDO y VIDA de 32 px.
7. Muñequito: solo contorno turquesa; el rojo solo por daño a la vida (un golpe absorbido por el escudo no lo pinta) y los implantes marcados.
8. Marcador del objetivo: rombo de 14 px, distancia de 18 px con la letra fina y 70 % de opacidad; flecha de 18 px.

## Criterios
- [x] Sin panel de zona ni hora en la pantalla normal
- [x] Nombre de zona chico, arriba al centro, que se desvanece
- [x] Aviso del Doc compacto, de línea fina, que se esconde solo
- [x] Minimapa del tamaño de H10 con calles de la escena
- [x] Vida y etiquetas de tamaño normal
- [x] Muñequito de contorno turquesa, rojo solo con daño
- [x] Marcador del objetivo discreto

## Capturas
docs/capturas/s1c-T-701-comparacion-*.png (H10 a la izquierda, juego a la derecha) y s1c-T-701-juego-*.png.

## Pruebas
EditMode HudH10PulidoReglasPruebas. PlayMode HudH10PulidoPruebas y MinimapaCallesT701Pruebas. HudH10CapturasT701 (solo con CAPTURAR_S1=1).
