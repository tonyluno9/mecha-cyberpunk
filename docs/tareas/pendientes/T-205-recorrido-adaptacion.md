# T-205 Recorrido de la adaptación del rival (cierre del bloque 8 de T-F7)
Dueño: Tony | Rama: tony/T-205-recorrido-adaptacion | Modelo: Sonnet 5.5, medium
Depende de: T-198 a T-204

## Objetivo
Una prueba de PlayMode recorre, con entrada simulada, varias peleas seguidas contra los mismos rivales: TORNILLO te lee los puños y se cubre,
le ganas castigándolo y aprende a retirarse, te cubres y aprende a fintar, nunca tiene más de dos lecturas a la vez, un rival de la liga 3 llega a nivel
II, perder dos veces seguidas no lo vuelve más duro, la memoria se guarda y se carga, y en la escena de implantes, por el mostrador, SINAPSIS II
le gana a su guardia. Si pasa, el bloque "adaptación del rival" está cerrado del lado de Tony.

## Contexto
- Diseño: combate.md (caso límite "El rival se adapta a tu forma de pelear en peleas repetidas"; criterio "Puedo bloquear un golpe y
  contraatacar con buen timing").
- Decisiones: D-tony-190 a D-tony-201 (sobre todo D-tony-194 y D-tony-197 g). Tareas: T-198 a T-204.
- Ejemplos: `Tests/PlayMode/Progresion/Implantes/RecorridoImplantesPruebas.cs` (T-197) y `Tests/PlayMode/Progresion/RecorridoHabilidadesPruebas.cs`
  (T-180): estructura, `MedidorFrames`, `ServicioGuardado.RutaForzada`, atajos permitidos, registro de errores, mapas de acciones.

## Qué hacer
1. `Tests/PlayMode/Progresion/Adaptacion/RecorridoAdaptacionPruebas.cs`, con entrada simulada (clic izquierdo y derecho, ratón, WASD, R, F5, F9,
   F6 a F8, E, flechas, Enter y las teclas de `ImplantesDePrueba`). Atajos permitidos, anotados en la bitácora: `RecibirDanio` al rival solo
   después de daño real del jugador (para acortar peleas); `VidaMecha.FijarVida` del jugador para las derrotas del paso g; `FijarAzar` en los
   componentes de adaptación donde se pida una medida exacta; `MemoriaRivales` por código solo en el paso i; `ServicioLigas.Poner` si el
   mostrador lo pide. En cada pelea la prueba recalcula con `ReglaEstilo` y `ReglaAdaptacion` lo que debía aprender a partir del perfil medido
   y lo compara con la memoria. Pasos, en Pruebas_Adaptacion salvo el i:
   a) memoria limpia, TORNILLO: pelea a PUÑOS (clic seguido, guardia abajo): ningún golpe bloqueado por el rival; ganar; la cabina dice
      "APRENDIÓ: GUARDIA I"; evidencia {1, 0, 0};
   b) R: con GUARDIA I y azar natural, 20 golpes a PUÑOS: fracción bloqueada en `probabilidadGuardia` ± 0.2 (se imprime); luego solo
      CASTIGANDO (soltar el clic 0.15 a 0.3 s después de cada impacto del rival): ninguno de esos se bloquea; ganar sin habilidades. Se espera
      RETIRADA I (y para GUARDIA, lo que diga la regla; se imprime);
   c) R: con RETIRADA I y azar 0: tras su golpe se retira; el contragolpe a 0.3 s conecta y el de 0.9 s se queda corto en la mayoría
      (se imprimen); el resto de la pelea CUBRIÉNDOSE (guardia arriba, golpes sueltos); ganar. Se espera FINTAS I (la prueba manda lo que
      diga la regla con el perfil medido y lo imprime);
   d) R: con FINTAS I, los avisos medidos quedan entre la preparación de serie y + `extraAvisoMaximo`, y con azar 0 hay un amago sin daño;
      ganar a PUÑOS; en ningún momento del recorrido un rival tiene más de 2 adaptaciones activas;
   e) F6 hasta VOLTIO (_Avanzado): 3 peleas cortas a PUÑOS con R: GUARDIA I, I y II; en la cuarta, la cabina dice "GUARDIA II";
   f) F6 hasta EL CHATARRAS: no sabe nada de ti (memoria vacía, cabina oculta) aunque TORNILLO y VOLTIO sí;
   g) F6 hasta TORNILLO: dos derrotas seguidas cubriéndote (vida baja fijada): la primera enseña, la segunda no sube nada aunque se vio el
      hábito; la victoria siguiente vuelve a enseñar;
   h) F5; `OlvidarTodo` por código; F9: TORNILLO y VOLTIO recuerdan lo mismo que antes de F5;
   i) Pruebas_Implantes con la misma `RutaForzada`: TORNILLO por el mostrador (E) en el mecha de la historia, con GUARDIA I fijada y azar 0:
      los golpes de serie se bloquean 5 de 5; tras instalar SINAPSIS II con `ImplantesDePrueba` (tecla 4) y la revancha por el mostrador, 0 de 5;
      la pantalla de la cabina se ve en esa escena;
   j) consola sin errores ni excepciones; `MedidorFrames` sin tirones fuera de su tolerancia; la partida real del humano no se toca.
2. Correr la suite completa: `bash herramientas/verificar.sh todo` y `bash herramientas/verificar.sh playmode-por-area`; anotar el resultado por
   área (incluida "Progresion.Adaptacion") en docs/revision-tony.md.
3. docs/revision-tony.md: sección "Adaptación del rival (T-F7, bloque 8) cerrada: qué probar jugando" con la lista para un humano (los criterios
   de sensación de T-199 a T-204 juntos, en orden de juego), la tabla final de las tres adaptaciones con sus números (de los assets), las reglas
   de memoria (D-tony-194), las preguntas abiertas para Tony (D-tony-201) y "Para Alan" (D-tony-200: nada).
4. docs/guia-para-jugar.md: cómo funciona la adaptación (qué hábito lee cada respuesta y cómo ganarle) y dónde verla en la cabina.

## Carpetas permitidas
Assets/_Proyecto/Tests/PlayMode/Progresion/Adaptacion/, docs/capturas/, docs/revision-tony.md (sección de esta tarea), docs/guia-para-jugar.md, docs/bitacora/
No tocar: código del juego (si el recorrido encuentra un error, se anota en la bitácora y se pide una tarea de corrección; no se arregla aquí),
Core/, escenas, assets, todo lo de Alan.

## Criterios de aceptación (los comprueba el verificador)
- [ ] RecorridoAdaptacionPruebas pasa 3 corridas seguidas; la bitácora anota el tiempo de juego de cada paso (a a j)
- [ ] La memoria y los niveles al final de cada pelea coinciden con `ReglaAdaptacion` aplicada al perfil medido, y las medidas (fracción bloqueada,
      avisos, retirada, contragolpes, SINAPSIS II) coinciden con lo calculado desde los assets (la prueba las compara y las imprime)
- [ ] `bash herramientas/verificar.sh todo` pasa en una sola llamada (salvo los fallos conocidos de Alan en Arena) y `playmode-por-area` no
      marca ningún área "LIMITE"
- [ ] Capturas docs/capturas/T-205-1.png (cabina con "TE TIENE ESTUDIADO · GUARDIA II" contra VOLTIO), T-205-2.png (resultado con "APRENDIÓ"
      y la pista) y T-205-3.png (TORNILLO con la guardia arriba en Pruebas_Implantes), revisadas a ojo
- [ ] docs/revision-tony.md tiene la sección de cierre con la lista, la tabla, las reglas, las preguntas y lo de Alan; la guía explica la adaptación

## Criterios de sensación (para un humano)
- Pruebas_Adaptacion, Play, sin ayuda, unos 15 minutos, peleando como peleas tú contra TORNILLO y luego VOLTIO: ¿sientes que te está leyendo, o
  solo que se puso difícil? ¿Encuentras solo el estilo que le gana? ¿Volver a pelear contra el mismo rival se volvió más interesante? Después,
  en Pruebas_Implantes, ¿la revancha por el mostrador contra un rival que ya te conoce te da ganas de cambiar de implante o de pieza?

## Pruebas automáticas
- PlayMode: RecorridoAdaptacionPruebas.

## Fuera de alcance
- La adaptación en Ciudad.unity o el Clandestino de Alan (no se adaptan sus rivales, D-tony-200). Prueba humana completa de H7 (al final de
  T-F7). Esquivar habilidades, presentador que lo anuncia, hackeo.
