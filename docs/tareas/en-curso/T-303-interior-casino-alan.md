# T-303 Interior del casino, abierto de noche (A-F5)
Dueño: Alan | Rama: alan/T-303-interior-casino | Modelo: Sonnet 5.5, medium
Depende de: T-301, T-302, T-094 (exterior), T-043 (patrón de interiores).

## Objetivo
El casino se puede entrar de noche: sala con 6 tragamonedas, cajera, luz cálida y neón, y cierra de día.

## Qué hacer
1. `Mundo/Editor/CreadorCasinoInterior.cs`: interior bajo `Lugar_Casino` (como `CreadorBar`), 6 `Tragamonedas.prefab`, la cajera, columnas, alfombra y luces. Se agrega a `CreadorTodo` en orden y se puede repetir.
2. El `DatosLugar` del casino deja de estar "cerrado en la demo"; el horario real es de noche. Actualizar `CerradosPruebas` y el letrero.
3. Sonda de luz y audio como en el bar (música dentro, sin lluvia).

## Carpetas permitidas
Assets/_Proyecto/Mundo/, Assets/_Proyecto/Casino/, Tests de Mundo y Casino, docs/capturas/. No tocar Core/ ni carpetas de Tony.

## Criterios de aceptación
- [ ] A las 22:00 la puerta abre y a las 12:00 no; adentro hay 6 máquinas y la cajera registradas como `IInteractuable`
- [ ] Con entrada simulada se entra, se cambian fichas y se juega una vez sin errores ni atravesar paredes
- [ ] Capturas de noche, fuera y dentro: docs/capturas/T-303-1..3.png
- [ ] `bash herramientas/verificar.sh todo` pasa

## Fuera de alcance
Apuestas a peleas, más juegos.

## Estado de la preparación (sin Unity)
Escrito y revisado solo de forma estática. **No compilado, sin pruebas corridas, sin capturas, sin .meta nuevos** (los genera Unity en la verificación).

### Hecho en código
- `Mundo/Editor/CreadorCasinoInterior.cs` (nuevo): interior de 28 × 22 m bajo `Lugar_Casino` (como `CreadorBar`): muros con collider, 6 `Tragamonedas.prefab` en el muro del fondo, `Cajera.prefab` en el muro derecho con mostrador sólido, `ServicioFichas.prefab`, 4 columnas, 2 mesas de cartas, 2 mesas del kit, 2 sillones, alfombra con filetes, neón, 6 lámparas cálidas y luces de relleno. Los colliders de máquinas y mostrador se agregan en la instancia (los prefabs de Casino no se editan). Las luces de las máquinas entran a `ZonaInterior.luces` (se apagan con el casino vacío). Entra a `CreadorTodo` justo después de `CreadorBar`.
- `CreadorLugares`: el casino pasa a `cerradoEnDemo = false` (también corrige el asset existente, porque `CrearDatos` no pisa); mercado negro sigue cerrado.
- `VestirCerrados`: el casino sin postes ni cadena, con neón a pleno y tres focos bajo la marquesina (el grupo sigue llamándose `Cerrado`). `VestirLetreros`: letrero del casino a pleno (`cerrado = false`). `CreadorAmbiente.AsegurarEmision` y `AmbientePruebas` cubren `CasinoInt_Neon*`.
- Audio: la mezcla de `AmbienteSonoro` ya usa cualquier `ZonaInterior`, así que el casino da música dentro y baja la lluvia sin código nuevo (hay prueba).
- Pruebas (Tests/PlayMode/Mundo): `CasinoInteriorPruebas` (nuevo), `EscenaLugares.ZonaDe(IdLugar)` (nuevo; `BarInteriorPruebas` y `BarPropsPruebas` ya no usan `FindFirstObjectByType<ZonaInterior>`, que con dos zonas era ambiguo). Pruebas de T-042/T-082/T-094 actualizadas al nuevo contrato: `LugaresPruebas`, `PuertasPruebas` (casino A22 abre, A12 cerrada por horario), `LetrerosPruebas`, `CerradosPruebas` (solo el mercado sigue cerrado; las capturas T-094 quedan numeradas 3 y 4 para no pisar la evidencia del casino cerrado 1 y 2).
- Límite conocido: la E del jugador a pie (Tony) no se referencia desde Mundo; la prueba usa la cámara libre para caminar y abrir la puerta con W/E, y llama `Usar()` del `IInteractuable` desde su punto de uso con cartera doble.

### Pendiente (necesita Unity; esperar `Logs/t303-unity-allowed.flag`)
1. `bash herramientas/verificar.sh todo` (compilar, EditMode, PlayMode); revisar errores de compilación de lo nuevo.
2. Correr `CreadorTodo.Crear` (regenera Barrio, Ciudad, Ciudad_Prologo y el asset del casino); commit de assets, materiales `CasinoInt_*`, `.meta` y escenas.
3. Capturas `docs/capturas/T-303-1..3.png`: revisarlas a ojo (máquinas visibles, cajera, sin huecos).
4. Revisar a ojo que 18 luces puntuales en el interior no bajan los fotogramas (el `MedidorFotogramas` del bar sirve de referencia).

### Contrato de demo que cambia y qué hacer con T-311
T-311 (`alan/T-311-fachadas-neon`, verificada en otro worktree) asume el casino cerrado: `EntradasNeonPruebas.Entradas_NoCambianElAcceso` afirma `Casino.EstaAbierto == false` y `VestirEntradas` le pone al casino neón tenue (intensidad 0.6, sin luz). **No se toca desde esta rama.** Al integrar T-303 sobre `alan/fase-5` con T-311 ya dentro:
- `EntradasNeonPruebas.Entradas_NoCambianElAcceso`: casino abierto a las 22:00 (`IsTrue`), mercado negro sigue cerrado; renombrar a algo como `Entradas_CasinoAbreDeNocheYMercadoSigueCerrado`.
- `VestirEntradas`: estilo del casino de cerrado a abierto (neón a pleno, `conLuz = true`, como casa y clandestino) y volver a correr sus pruebas de materiales/luces máximos.
- Conflictos de texto esperados solo en `CreadorTodo.cs` (T-311 añade `VestirEntradas.Crear()` tras `VestirCerrados`; T-303 añade `CreadorCasinoInterior.Crear()` tras `CreadorBar`): líneas distintas, merge limpio.
- Rehacer capturas `T-311-Casino-*` después del cambio.
