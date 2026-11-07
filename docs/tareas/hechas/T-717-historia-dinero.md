# T-717 La historia del dinero: asalto, persecución y llanta ponchada
Dueño: Tony (S1, autorizado en Trafico e IA esta noche) | Rama: tony/T-717-historia-dinero | Modelo: Sonnet 5.5
Depende de: T-704, T-714, T-716

## Objetivo
Al salir de casa por primera vez hay una cinemática corta: un asaltante te quita todo el dinero y huye en un carro del tráfico. Lo persigues con objetivo y marcador; le ponchas una llanta (disparo a las ruedas o choque fuerte), el carro se detiene, el asaltante baja y pelea; al vencerlo el dinero vuelve a tu cartera.

## Qué había
Nada de esto: la historia (DirectorPandillas) pasaba de Despierta a PrimeraPelea al cruzar la puerta. Se agregó el paso `Asalto` entre los dos (y el aviso `DineroRecuperado`); los guardados viejos siguen valiendo (el paso se guarda por nombre).

## Qué se hizo
- Progresion/Prologo/Pandillas: `ReglaAsalto` (pura: fases, cuánto te roban, cuánto vuelve) y `AsaltoDinero` (la cinemática con cámara propia y barras, el jugador congelado con ControladorAPie.ActivarControl, Enter salta con el Input System, el asaltante camina, te roba por ICartera, corre al carro; luego el objetivo sigue al carro, y al detenerse el asaltante baja en alerta). DirectorPandillas agrega el componente, cancela el asalto si el paso cambia (el dinero vuelve y el control también) y guarda `dineroRobado` solo si hay un asalto a medias.
- Trafico: `AsaltoMundo` (lo agrega GestorPersecucion) manda un carro del pool con `TraficoCalle.EnviarRefuerzo` (T-704/T-714), lo espera junto al jugador y lo hace huir por las calles a un nodo a 120-240 m a 10 m/s. `CarroTrafico`: `Ponchar`, `IniciarHuida`, `EnHuida`, `Ponchado` (frena fuerte y se queda quieto; un choque fuerte también lo ponchaba; no reacciona a disparos). `LlantaPonchable` (IRecibeDanio + IObjetoInerte) es una franja baja de 0.5 m en el carro: un disparo ahí le poncha la llanta.
- Contrato sin tocar Core: Progresion no ve Trafico, así que se hablan por puntos de historia de Core (IPuntoHistoria): `Historia_Asalto` (Activar(true) manda el carro, Activar(false) lo hace huir, su Pose es la del carro), `Historia_Asalto_Listo` (el carro espera) e `Historia_Asalto_Detenido` (ya no se va a mover).
- Nunca se traba: sin tráfico o sin carro en 20 s se cancela el asalto y la historia sigue; el carro se detiene por llanta, por llegar al final de la ruta o por atorón (6 s quieto, 60 s máximos).
- ServicioObjetivos/SeguimientoObjetivo: un objetivo que se mueve con el mismo texto (el carro) no borra la distancia (`Reubicar`): el HUD no parpadea.

## Criterios
- [x] Al cruzar la puerta empieza el asalto: barras, jugador congelado, el asaltante camina hacia ti, objetivo PERSIGUE AL ASALTANTE
- [x] Enter (tecla real) salta la cinemática; el dinero sale de la cartera y el carro huye
- [x] El objetivo y el marcador siguen al carro; el carro se detiene al final de su ruta o con un disparo a las ruedas
- [x] El asaltante baja y pelea (RECUPERA TU DINERO); al vencerlo el dinero vuelve y la historia sigue a la primera pelea
- [x] Cambiar de paso (F6, un guardado) cancela el asalto: el dinero y el control vuelven

## Pruebas
- EditMode Progresion: ReglaAsaltoT717Pruebas (7), MaquinaHistoriaPandillasPruebas (12 pasos, 11 avisos)
- PlayMode Progresion: AsaltoDineroT717Pruebas (3: flujo completo hasta el final de la ruta; llanta con el rayo del disparo; cancelar). Capturas docs/capturas/s1c-T-717-*.png
- Se actualizaron HistoriaPandillasPruebas, MarcadorObjetivoPruebas y RecorridoDemoPruebas (tramo 03b) al paso nuevo.

## Notas
- Si HistoriaPandillasPruebas corre justo antes de MarcadorObjetivoPruebas en una misma corrida mezclada, esta última falla (el jugador no avanza); por área (ensamblados separados) y solas pasan. Orden entre pruebas, no del asalto.
- El choque con el carro del jugador (ponchadura) se probó por regla (Nivel Fuerte), no con un choque físico real.
