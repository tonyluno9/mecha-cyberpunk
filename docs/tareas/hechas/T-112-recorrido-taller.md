# T-112 Recorrido de daño, taller y guardado (cierre de T-F5 de Tony)
Dueño: Tony | Rama: tony/T-112-recorrido-taller | Modelo: Sonnet 5.5, medium
Depende de: T-101 a T-111

## Objetivo
La prueba de cierre de T-F5 del lado de Tony (H5 sin el taller de Alan), jugada con entrada simulada en Pruebas_Calle: pelear y
salir dañado, que la revancha empiece dañada, regresar el mecha al taller, repararlo pagando, quedar bloqueado por la renta y
pagarla, y guardar y cargar sin perder nada. No agrega sistemas: solo une lo de T-101 a T-111 y lo mide.

## Contexto
- Hoja de ruta, T-F5: "pelea que deja el mecha dañado, siguiente pelea empieza dañada, reparación descuenta dinero, guardar y
  cargar conserva todo (H5)". H5 completo necesita el taller de la casa de Alan (A-F3): esa integración será otra tarea cuando
  A-F3 esté en main (D-tony-52).
- Decisiones: D-068 (Pruebas_Calle la genera `CreadorEscenaCalle`), D-042 (el director busca al jugador como el `IPeleador` del
  lado Jugador y la arena más cercana), D-061 (una pelea de arena deja el mecha encendido), D-062 (M devuelve el mecha vacío al
  taller). Propuestas de T-F5: D-tony-42 a D-tony-51.
- Ejemplo de recorrido largo con entrada simulada: Tests/PlayMode/Pelea/RecorridoEmboscadaPruebas.cs (T-072) y
  Tests/PlayMode/Personaje/RecorridoAPiePruebas.cs (T-059).
- Herramientas de prueba en Pruebas_Calle: `DineroDePrueba` (7 y 8), `RelojDePrueba` (J), `GuardadoDePrueba` (F5 y F9),
  `CalorDePrueba` (9 y 0), `TallerDePrueba` (banco 3 m a un lado).

## Qué hacer
1. `CreadorEscenaCalle`: agrega a Pruebas_Calle una arena de prueba (`Pelea/Pruebas/ArenaDePrueba.prefab`) en un terreno abierto
   junto a la calle, a 30 m o menos en línea recta y sin obstáculos del banco del taller, y fuera del radio de activación de la
   emboscada (12 m) y de su callejón; un `FlujoPelea.prefab` (casera: `RivalBasico`, premio 600) con `empezarAlArrancar` en false y
   un `IniciadorPeleaDePrueba` (P). Revisa que el director tome como jugador al mecha estacionado y no a otro peleador. Regenera
   la escena con su creador.
2. PlayMode `Tests/PlayMode/Mecha/RecorridoTallerPruebas.cs` (Pruebas_Calle, entrada simulada para caminar, mirar, E, M, encendido,
   clics, P, R, 7, 8, J, F5 y F9; está permitido ayudar al rival a caer con `RecibirDanio` una vez que el jugador ya recibió daño
   real, para que la prueba dure poco y sea estable):
   a) a pie al mecha, E, encendido (F, Espacio mantenido, Q), caminar a la arena y P: la pelea empieza con el jugador a vida
      completa; el jugador recibe golpes reales (vida < máxima) y gana: dinero +600;
   b) R: al empezar el conteo la vida del jugador es la del final de la pelea a) (±0.01) y la del rival la máxima; gana otra vez:
      dinero +600;
   c) bajar con E, M: el mecha vuelve al taller con la misma vida; caminar al banco: el aviso dice "E · Reparar mecha · $X" con
      X = costo de `ReglaReparacion`; E: el dinero baja exactamente X y la vida queda al máximo;
   d) dejar el dinero por debajo de 500 (tecla 8) y saltar al día 8 con J: "RENTA VENCIDA · $500" en el HUD y el banco dice
      "TALLER BLOQUEADO"; 7 para tener dinero, E en el banco paga la renta y se desbloquea;
   e) dañar el mecha (o dejarlo como quedó), F5; cambiar dinero, vida, perfil y avanzar el reloj; F9: dinero, vida del mecha, renta,
      perfil, día y hora vuelven a lo guardado;
   f) en todo el recorrido la consola no tiene errores ni excepciones, y dura menos de 120 s de juego.
3. Capturas: docs/capturas/T-112-1.png (revancha empezando con la cabina dañada: pantalla de vida, alarma o grietas según la vida),
   T-112-2.png (banco del taller con el costo de reparar) y T-112-3.png (HUD tras reparar, con el aviso del cobro).

## Carpetas permitidas
Assets/_Proyecto/Mecha/Editor/CreadorEscenaCalle.cs (y su asmdef si falta la referencia a Pelea),
Assets/_Proyecto/Escenas/Tony/Pruebas_Calle.unity (con su creador), Assets/_Proyecto/Tests/PlayMode/Mecha/, docs/capturas/
No tocar: el código de juego de ninguna área (si algo falla, se anota con la evidencia en el resumen y se vuelve una corrección de la
tarea que lo hizo), Core/, Pruebas_Tony.unity, las carpetas y escenas de Alan.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa (todas las pruebas de Tony, no solo las nuevas)
- [ ] RecorridoTallerPruebas (a-f) pasa en 3 corridas seguidas
- [ ] Revancha con el mismo daño, reparación con el costo exacto, bloqueo y pago de renta, y guardar/cargar con todo igual: medidos
      en la prueba
- [ ] Capturas T-112-1, T-112-2 y T-112-3 revisadas
- [ ] Las pruebas de llamado, emboscada y calor de Pruebas_Calle siguen pasando con la arena agregada

## Criterios de sensación (para un humano, prueba humana de H5 del lado de Tony)
- Pruebas_Calle, Play, sin ayuda: sube al mecha (E), enciéndelo (F, Espacio, Q), ve a la arena, P para pelear, gana con daño y
  pulsa R. ¿Se nota que empiezas dañado (vida, alarma, grietas, más lento)? ¿Ganar se siente bien con "+$600"?
- Baja (E), M para mandarlo al taller, camina al banco y repara: ¿el costo se entiende y quedar en negativo se siente como decisión?
- J hasta el día 8 sin dinero: ¿el bloqueo del taller y "RENTA VENCIDA" se entienden sin explicación?
- F5, cambia cosas, F9: ¿todo vuelve como estaba?

## Pruebas automáticas
- PlayMode: RecorridoTallerPruebas.

## Fuera de alcance
- El taller de la casa de Alan en Ciudad.unity (integración cuando A-F3 esté en main); dormir, menú de cargar y la pelea casera de la
  demo (T-F6)
