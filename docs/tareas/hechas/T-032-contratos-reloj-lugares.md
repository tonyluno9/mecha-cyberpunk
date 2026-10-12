# T-032 Contratos de reloj, lugares y puertas
Dueño: Tony (tarea de Core, D-032) | Rama: tony/T-032-contratos-reloj-lugares | Modelo: Sonnet 5.5, medium
Depende de: T-031 (usa `Registro<T>`)

## Objetivo
En Core/ existen los contratos del tiempo y los lugares: el reloj del mundo (`IRelojMundo`), los lugares reales de la ciudad
(`IdLugar`, `ILugar`), sus horarios (`HorarioLugar`), sus puertas (`IPuerta`) y los avisos de entrar, salir, abrir y cerrar
(`EventosLugares`). Alan los implementa en T-041, T-042 y T-043; Tony los usa después para renta, misiones, objetivos y el HUD.

## Contexto
- Diseño: docs/diseno/ciudad.md (seis lugares reales; día de 20 minutos, 12 de noche y 8 de día; de día abre el mercado
  negro y de noche el clandestino, el bar y el casino; un lugar cerrado tiene la puerta cerrada y un letrero con el horario),
  docs/diseno/demo.md (casino y mercado negro cerrados en la demo), docs/diseno/personaje-progresion.md (clínica dentro
  del mercado negro), docs/diseno/economia-misiones.md (renta cada 7 días: Tony necesita contar días).
- Decisiones: D-027. Propuestas: D-tony-4 (IdLugar como enum), D-tony-5 (los horarios son datos de Alan; la regla vive en Core).
- Quién implementa qué: `IRelojMundo`, `ILugar` e `IPuerta` los implementa Alan (Mundo/). `HorarioLugar` y
  `EventosLugares` son de Core y no se implementan. Las horas de amanecer y anochecer y los horarios de cada lugar son
  datos de Alan, no de Core.
- Es una tarea de Core: se integra a main en cuanto el verificador la apruebe.

## Qué hacer
Todo en `Core/Mundo/`, un tipo por archivo, con comentarios `///` que digan quién implementa y qué garantiza.
1. `IdLugar`: `enum IdLugar { Ninguno, Calle, Casa, Taller, Bar, Clandestino, Casino, MercadoNegro, Clinica }`.
2. `FranjaDia`: `enum FranjaDia { Dia, Noche }`.
3. `HorarioLugar`: struct `[Serializable]` con campos privados serializados `abre` y `cierra` (horas de 0 a 24, con
   `[Range(0, 24)]`) y `siempreAbierto`; propiedades de solo lectura, constructor y `static HorarioLugar Siempre`.
   - `bool EstaAbierto(float hora)`: siempre abierto da true; `abre == cierra` da false (nunca abre); si `abre < cierra`,
     abierto en `[abre, cierra)`; si no, cruza la medianoche y está abierto si `hora >= abre` o `hora < cierra`.
     La hora se normaliza a [0, 24) antes (24 es 0, 25 es 1, -1 es 23).
   - `float HorasHastaAbrir(float hora)`: 0 si está abierto; las horas que faltan, dando la vuelta a la medianoche; infinito
     si nunca abre.
   - `string Texto()`: "20:00 a 06:00", "24 horas" o "Cerrado". `static string TextoHora(float hora)`: 21.5 da "21:30",
     24 da "00:00", 7.999 da "07:59" (trunca los minutos, nunca da "08:60").
4. `IRelojMundo` (Alan lo implementa una vez por escena y lo agrega a `Registro<IRelojMundo>`):
   - `float Hora` (hora del día en [0, 24) con decimales), `int Dia` (empieza en 1), `FranjaDia Franja`, `bool EsDeNoche`,
     `float HoraAmanecer`, `float HoraAnochecer`, `bool Pausado`.
   - `event Action<int> HoraEnteraCambio` (cada hora entera que pasa, en orden, aunque un frame largo pase varias),
     `event Action<FranjaDia> FranjaCambio`, `event Action<int> DiaCambio` (al pasar las 00:00).
   - `void Pausar(bool pausado)`.
   - `void AvanzarHasta(float hora)`: salta hacia adelante hasta la próxima vez que el reloj marque esa hora (si ya pasó
     hoy, al día siguiente; si es la hora actual, no cambia nada). Avisa una vez `HoraEnteraCambio` con la hora final, una vez
     `DiaCambio` por cada medianoche cruzada y `FranjaCambio` solo si la franja final es distinta de la inicial. Sirve para
     dormir (T-F5) y para las herramientas de prueba.
5. `ILugar` (Alan, uno por lugar real, en `Registro<ILugar>`): `IdLugar Id`, `string Nombre` (el del letrero),
   `HorarioLugar Horario`, `bool EstaAbierto` (ahora, con el reloj y cualquier cierre especial, como "cerrado en la demo"),
   `bool JugadorDentro`, `Vector3 PosicionEntrada` (frente a la puerta, del lado de la calle; el objetivo de Tony apunta aquí),
   `event Action<bool> EstadoCambio` (true al abrir, false al cerrar).
6. `ResultadoPuerta`: `enum ResultadoPuerta { Abierta, YaAbierta, CerradaPorHorario, Bloqueada }`.
7. `IPuerta` (Alan, en `Registro<IPuerta>`): `IdLugar Lugar`, `bool EstaAbierta` (la hoja está abierta y se puede pasar),
   `Vector3 Posicion`, `ResultadoPuerta IntentarAbrir()`, `void Cerrar()`, `event Action<bool> Cambio`.
   Contrato: quien interactúa (el jugador de Tony en T-F3, o la cámara libre de Alan) llama a `IntentarAbrir`; la puerta decide
   según el lugar; `Bloqueada` es un cierre que no depende de la hora (por ejemplo, cerrado en la demo).
8. `EventosLugares` (estático, como `EventosPelea`): `JugadorEntro(IdLugar)`, `JugadorSalio(IdLugar)`,
   `EstadoLugarCambio(IdLugar, bool abierto)`, sus `Publicar...`, `LimpiarSuscriptores()` y limpieza al entrar a Play.
   Contrato: los publica Alan (las zonas de cada lugar y cada `ILugar` al abrir o cerrar); los escucha quien quiera (Tony:
   calor que baja en casa, objetivos, misiones).
9. Pruebas en `Tests/EditMode/Core/`: `HorarioLugarPruebas` y `EventosLugaresPruebas`. Una `RelojFalso : IRelojMundo` en la
   prueba que demuestre que el contrato se puede implementar sin áreas (no hace falta que avance).

## Carpetas permitidas
Assets/_Proyecto/Core/Mundo/ y Assets/_Proyecto/Tests/EditMode/Core/
No tocar: el resto de Core/, las áreas de Tony y de Alan, escenas, prefabs, ProjectSettings/, Packages/ ni herramientas/.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa con las pruebas nuevas contadas
- [ ] HorarioLugarPruebas pasa: 20 a 6 está abierto a las 23, a las 2 y a las 20 exactas, cerrado a las 6 exactas y a las 12;
      9 a 18 está abierto a las 9 y cerrado a las 18 y a las 22; siempre abierto da true a cualquier hora; `abre == cierra`
      da false siempre e infinito en `HorasHastaAbrir`; `HorasHastaAbrir(12)` con 20 a 6 da 8 y `HorasHastaAbrir(19)` con
      9 a 18 da 14; 24, 25 y -1 se normalizan; `TextoHora` da "21:30", "00:00" y "07:59" en los casos del paso 3;
      `Texto()` da "20:00 a 06:00", "24 horas" y "Cerrado"
- [ ] EventosLugaresPruebas pasa: cada evento llega con su `IdLugar` y su valor; `LimpiarSuscriptores` los quita todos
- [ ] `HorarioLugar` se serializa en un ScriptableObject: una prueba crea con `CreateInstance` un SO de prueba con un campo
      `HorarioLugar`, le pone 20 a 6 con `SerializedObject` y lo lee igual
- [ ] Cada interfaz y miembro tiene comentario `///` con quién implementa y qué garantiza, incluidas las reglas de
      `AvanzarHasta` y de `HoraEnteraCambio` (lectura del diff)
- [ ] ReglasEnsamblados sigue pasando y `git diff main --stat` solo toca las carpetas permitidas

## Criterios de sensación (para un humano)
- Alan, o Tony leyendo como Alan: con estos comentarios puede escribir T-041 y T-042 sin preguntar nada.

## Pruebas automáticas
- EditMode (Tests/EditMode/Core/): HorarioLugarPruebas, EventosLugaresPruebas y la de serialización.

## Fuera de alcance
- Implementar el reloj, los lugares y las puertas (T-041 y T-042, Alan)
- Datos de cada lugar (`DatosLugar` y sus horarios son de Alan, en Mundo/Datos/)
- Renta, dormir, objetivos (T-F3 a T-F5)
