# T-197 Recorrido de los implantes (cierre del bloque 6 de T-F7)
Dueño: Tony | Rama: tony/T-197-recorrido-implantes | Modelo: Sonnet 5.5, medium
Depende de: T-190 a T-196

## Objetivo
Una prueba de PlayMode recorre, con entrada simulada y en Pruebas_Implantes, un día y una noche con cromo: probar la pista sin implantes,
instalar piernas y brazos en la clínica, verte en el espejo, subir la caja que antes no subías, tumbar a la pandilla con menos golpes, pelear
en la jaula viendo la vida y el próximo ataque del rival, mejorar a los implantes de nivel 2, caer en la calle y despertar con el doc, y
guardar y cargar. Si pasa, el bloque "implantes" está cerrado del lado de Tony.

## Contexto
- Diseño: personaje-progresion.md (criterios "Un implante instalado se ve en mi apariencia y cambia lo que dice que cambia" y "Al morir en la
  calle despierto en la clínica con menos dinero, y el mecha está dañado en el taller").
- Decisiones: D-tony-163 a D-tony-174. Tareas: T-190 a T-196.
- Ejemplos: `Tests/PlayMode/Progresion/Misiones/RecorridoMisionesPruebas.cs` (T-188) y `Tests/PlayMode/Progresion/RecorridoHabilidadesPruebas.cs`
  (T-180): estructura, `MedidorFrames`, `ServicioGuardado.RutaForzada`, atajos permitidos, registro de errores, helper de mapas de acciones.

## Qué hacer
1. `Tests/PlayMode/Progresion/Implantes/RecorridoImplantesPruebas.cs` en Pruebas_Implantes, con entrada simulada (WASD, Shift, Espacio, E,
   flechas, Enter, Esc, clic izquierdo y derecho, ratón, M, F/Espacio/Q, F5, F9). Atajos permitidos, anotados en la bitácora: `Colocar` cerca de
   un punto; dinero fijado por código al inicio, antes del paso g y antes del paso h; `AvanzarHasta` para cambiar de franja; `ServicioLigas.Poner` para la liga 3 en
   el paso g; `RecibirDanio` al rival mecha solo después de daño real del jugador. Números calculados desde los assets. Pasos:
   a) 10:00, sin implantes, dinero fijado: en la pista, la caja "PIERNAS I" no se sube (se mide la altura del salto) y se mide la recta de 30 m
      corriendo;
   b) clínica: instalar PIERNAS I y BRAZOS I con la pantalla (−$4,000 en total, +2 h cada una); el espejo muestra las dos piezas;
   c) pista: la caja "PIERNAS I" sí se sube y la "PIERNAS II" no; la recta corriendo tarda lo esperado (±5 %);
   d) `AvanzarHasta(21)`: la pandilla; tumbar a los 3 a puño con 3 golpes cada uno (nunca 2 atacando a la vez de más), cobrar su dinero;
   e) clínica cerrada de noche ("CERRADA"); `AvanzarHasta(10)` del día siguiente: instalar OJOS I y SINAPSIS I;
   f) arena: E en el mostrador, pelea contra el rival 1 de la liga 1 en el mecha de la historia (llamar no hace falta si la entrada a la arena lo
      pone): la pantalla de ojos muestra su vida y avisa sus golpes con la anticipación de OJOS I; el golpe del jugador sale con la preparación
      de SINAPSIS I; ganar y cobrar el premio;
   g) liga 3 por código y dinero fijado: mejorar los 4 a nivel 2 pagando la diferencia; el espejo los muestra con su franja; la caja "PIERNAS II"
      sí se sube;
   h) `AvanzarHasta(23)` con $300: dejarse tumbar por la pandilla (quieto): despierta en la camilla a las 05:00 con −$200, calor 0, objetivo
      "VUELVE A CASA"; caminar a `Casa` lo quita; los implantes siguen puestos;
   i) F5; quitar los implantes y cambiar el dinero por código; F9: todo vuelve (implantes, dinero, mecha);
   j) consola sin errores ni excepciones; `MedidorFrames` sin tirones fuera de su tolerancia; la partida real del humano no se toca.
2. Correr la suite completa: `bash herramientas/verificar.sh todo` y `bash herramientas/verificar.sh playmode-por-area`; anotar el resultado por
   área (incluida "Progresion.Implantes") en docs/revision-tony.md.
3. docs/revision-tony.md: sección "Implantes (T-F7, bloque 6) cerrados: qué probar jugando" con la lista para un humano (los criterios de
   sensación de T-190 a T-196 juntos, en orden de juego), la tabla final de implantes (de T-191), las reglas del doctor, las preguntas abiertas
   para Tony (D-tony-174) y "Para Alan" (D-tony-173): `MostradorClinica.prefab`, la camilla "Clinica_Camilla", `DoctorClinica.prefab`,
   `PandillaAPie.prefab`, la altura de bardas con PIERNAS II y qué puede leer de `IImplantes`; nada de eso lo bloquea.
4. docs/guia-para-jugar.md: la clínica (E en el mostrador, de día), qué hace cada implante, la pandilla de noche, qué pasa si caes en la calle,
   las teclas de `ImplantesDePrueba` y Pruebas_Implantes como la escena para probarlos.

## Carpetas permitidas
Assets/_Proyecto/Tests/PlayMode/Progresion/Implantes/, docs/capturas/, docs/revision-tony.md (sección de esta tarea), docs/guia-para-jugar.md
No tocar: código del juego (si el recorrido encuentra un error, se anota en la bitácora y se pide una tarea de corrección; no se arregla aquí),
Core/, escenas, todo lo de Alan.

## Criterios de aceptación (los comprueba el verificador)
- [ ] RecorridoImplantesPruebas pasa 3 corridas seguidas; la bitácora anota el tiempo de juego de cada paso (a a j)
- [ ] El dinero, los implantes, la hora, la vida del mecha y las medidas de salto, carrera, golpes y anticipación coinciden con lo calculado
      desde los assets (la prueba los compara y los imprime)
- [ ] `bash herramientas/verificar.sh todo` pasa en una sola llamada (salvo los fallos conocidos de Alan en Arena) y `playmode-por-area` no
      marca ningún área "LIMITE"
- [ ] Capturas docs/capturas/T-197-1.png (el espejo con los 4 implantes de nivel 2), T-197-2.png (la cabina con OJOS en pelea) y T-197-3.png
      (el despertar con el doc), revisadas a ojo
- [ ] docs/revision-tony.md tiene la sección de cierre con la lista para un humano, la tabla, las preguntas y lo de Alan; la guía tiene la clínica

## Criterios de sensación (para un humano)
- Pruebas_Implantes, Play, sin ayuda, unos 20 minutos: ¿cada implante se siente distinto y vale lo que cuesta? ¿Los de la jaula (ojos,
  sistema nervioso) te hacen ganar peleas que antes perdías sin sentirse trampa? ¿Caer en la calle y pagarle al doc te dio ganas de volver
  con más cromo?

## Pruebas automáticas
- PlayMode: RecorridoImplantesPruebas.

## Fuera de alcance
- La clínica, el doctor y la pandilla en Ciudad.unity de Alan (se escribe cuando Alan los coloque, como T-136). Prueba humana completa de H7
  (al final de T-F7). Hackeo.
