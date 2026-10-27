# T-188 Recorrido de las misiones y la reputación (cierre del bloque 5 de T-F7)
Dueño: Tony | Rama: tony/T-188-recorrido-misiones | Modelo: Sonnet 5.5, medium
Depende de: T-181 a T-187

## Objetivo
Una prueba de PlayMode recorre, con entrada simulada y en Pruebas_Misiones, un día y una noche de trabajos: ayudar a Doña Lupe y ganar un
favor, aceptar un contrato corporativo por teléfono y ver cómo baja el barrio, robar la bodega de noche, esconderse con un favor, vender la
chatarra, usar el descuento, defender al taquero en el mecha, y guardar y cargar. Si pasa, el bloque "misiones y reputación" está cerrado del
lado de Tony y la reputación se ve en la ciudad (con el doble de Tony; la de Alan es un pedido).

## Contexto
- Diseño: economia-misiones.md (criterios "Una pelea paga más que una misión de barrio"; "Una misión de barrio sube la reputación del barrio y
  da un favor usable"), concepto-historia.md ("Aceptar un contrato corporativo baja la reputación del barrio de forma visible"; propuesta de la
  ciudad que reacciona), hoja-de-ruta.md (H7: "reputación visible en la ciudad").
- Decisiones: D-tony-143 a D-tony-152. Tareas: T-181 a T-187.
- Ejemplos: `Tests/PlayMode/Progresion/RecorridoHabilidadesPruebas.cs` (T-180) y `RecorridoPiezasPruebas.cs` (T-173): estructura,
  `MedidorFrames`, `ServicioGuardado.RutaForzada`, atajos permitidos, registro de errores, helper de mapas de acciones.

## Qué hacer
1. `Tests/PlayMode/Progresion/Misiones/RecorridoMisionesPruebas.cs` en Pruebas_Misiones, en `Libre`, con entrada simulada (caminar, E, T,
   flechas, Enter, Esc, J, M, F/Espacio/Q, ratón). Atajos permitidos, anotados en la bitácora: `Colocar` cerca de un punto; dinero inicial por
   código; `AvanzarHasta(21)` por código para ir a la noche (J solo lleva a las 8:00); `RecibirDanio` a los rivales solo después de daño real del jugador; `ServicioLigas.Poner` solo para abrir la liga 2 en el paso h.
   Números calculados desde los assets. Pasos:
   a) inicio de noche: reputación 0/0, sin favores, sin chatarra, dinero fijado; el letrero de `ReaccionReputacionDePrueba` vacío;
   b) E en Doña Lupe, aceptar, recoger, entregar: +$50, BARRIO +8 (el HUD lo dice), 1 DESCUENTO; ese pago es menor que el premio del rival 1 de
      la liga 1 (se imprime la comparación: el criterio "una pelea paga más");
   c) J (día). T → CONTRATOS → COBRO DE ADEUDO; cumplirlo: +$500, el HUD dice "BARRIO −12 · CORPORACIONES +10"; REPUTACIÓN muestra barrio −4
      (8 − 12); el letrero sigue vacío;
   d) `AvanzarHasta(21)` (noche): contacto del bar → BODEGA DE REFACCIONES; recoger (calor 50, nivel 2); con el ESCONDITE dado por código
      (atajo anotado; el favor real se gana en el paso f) esconderse en la azotea: calor 0, +6 chatarra, corporaciones 0 (10 − 10);
   e) mercado negro: VENDER la chatarra (+$300); COMPRAR 1 MISIL con el DESCUENTO del paso b ($56);
   f) la misma noche: LOS DEL CALLEJÓN en el mecha (llamar, subir, encender, vencer a 2, bajar, volver con el taquero): +$100, barrio 11,
      1 ESCONDITE real;
   g) J (día siguiente): Doña Lupe otra vez (ya no dice "YA LO HICISTE HOY"): barrio 19, nivel CONOCIDO (el HUD dice "AHORA ERES CONOCIDO EN EL
      BARRIO"), el letrero dice "GRAFITI: <ALIAS>" y LUZ PARA LA VECINDAD ya se ofrece en su encargo;
   h) `ServicioLigas.Poner` en la liga 2: en el teléfono, LIMPIEZA DE ZONA sigue en gris con su motivo (corporaciones en 0, SIN FICHA; pide
      CONTRATISTA);
   i) F5; cambiar reputación, favores, chatarra y dinero por código; F9: todo vuelve; la misión en curso (si hay una aceptada antes de F5) queda
      cancelada sin castigo;
   j) consola sin errores ni excepciones; `MedidorFrames` sin tirones fuera de su tolerancia; la partida real del humano no se toca.
2. Correr la suite completa: `bash herramientas/verificar.sh todo` y `bash herramientas/verificar.sh playmode-por-area`; anotar el resultado por
   área (incluida "Progresion.Misiones") en docs/revision-tony.md.
3. docs/revision-tony.md: sección "Misiones y reputación (T-F7, bloque 5) cerradas: qué probar jugando" con la lista para un humano (los
   criterios de sensación de T-181 a T-187 juntos, en orden de juego), la tabla final de misiones (de T-183), los niveles de reputación y los
   favores, las preguntas abiertas para Tony (D-tony-152) y "Para Alan" (D-tony-151): ids de los puntos, `ContactoBar.prefab`,
   `PeleasDeMision.prefab` y qué puede leer de `IReputacion` e `IMisiones`; nada de eso lo bloquea.
4. docs/guia-para-jugar.md: "Teléfono: T (contratos de día, misión, reputación, favores)", dónde se aceptan las misiones y cómo se usan los
   favores; Pruebas_Misiones como la escena para probarlas.

## Carpetas permitidas
Assets/_Proyecto/Tests/PlayMode/Progresion/Misiones/, docs/capturas/, docs/revision-tony.md (sección de esta tarea), docs/guia-para-jugar.md
No tocar: código del juego (si el recorrido encuentra un error, se anota en la bitácora y se pide una tarea de corrección; no se arregla aquí),
Core/, escenas, todo lo de Alan.

## Criterios de aceptación (los comprueba el verificador)
- [ ] RecorridoMisionesPruebas pasa 3 corridas seguidas; la bitácora anota el tiempo de juego de cada paso (a a j)
- [ ] El dinero, la reputación, los favores, la chatarra y la munición finales coinciden con lo calculado desde los assets (la prueba los compara
      y los imprime)
- [ ] `bash herramientas/verificar.sh todo` pasa en una sola llamada (salvo los fallos conocidos de Alan en Arena) y `playmode-por-area` no
      marca ningún área "LIMITE"
- [ ] Capturas docs/capturas/T-188-1.png (HUD con "BARRIO −12 · CORPORACIONES +10" tras el cobro), T-188-2.png (teléfono en REPUTACIÓN con
      barrio CONOCIDO) y T-188-3.png (el letrero "GRAFITI: <ALIAS>" en la calle de prueba), revisadas a ojo
- [ ] docs/revision-tony.md tiene la sección de cierre con la lista para un humano, las tablas, las preguntas y lo de Alan; la guía tiene la T

## Criterios de sensación (para un humano)
- Pruebas_Misiones, Play, sin ayuda, unos 20 minutos de un día y una noche: ¿las misiones dan ganas de hacerlas entre peleas o se sienten como
  relleno? ¿Sentiste el jalón entre barrio y corporaciones al escoger? ¿Los favores te ayudaron en un momento justo? ¿Las peleas siguen siendo
  lo que más paga, como pide el diseño?

## Pruebas automáticas
- PlayMode: RecorridoMisionesPruebas.

## Fuera de alcance
- Las misiones en Ciudad.unity con los puntos y el bar de Alan (se escribe cuando Alan los coloque, como T-136). Prueba humana completa de H7
  (al final de T-F7). Hackeo.
