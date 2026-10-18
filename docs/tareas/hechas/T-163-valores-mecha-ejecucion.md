# T-163 Valores del mecha en tiempo de ejecución (Core, necesita aprobación)
Dueño: Tony (tarea de Core, D-032; **necesita aprobación de Tony antes de empezar**, D-tony-114) | Rama: tony/T-163-valores-mecha | Modelo: Sonnet 5.5, medium
Depende de: nada

## Objetivo
`DatosMecha` (Core) puede leerse como un juego de valores y crearse en tiempo de ejecución con valores calculados, sin tocar ningún
asset. Es lo único de Core que necesita el bloque de piezas: el mecha del jugador arma sus estadísticas sumando sus 5 piezas (T-166,
T-167) y las entrega a la vida, el golpe, la guardia y el movimiento, que ya leen un `DatosMecha`. Hoy `DatosMecha` solo tiene campos
privados serializados y no se puede crear uno con otros números sin escribir un asset.

## Contexto
- Diseño: mecha-taller.md ("El mecha se arma por 5 piezas"; "Un mecha de cuerpo a cuerpo corre más y aguanta más que uno militar, y el
  militar pega más fuerte").
- Decisiones: D-026, D-027, D-032. Propuestas nuevas: D-tony-114 (este cambio y la alternativa descartada) y D-tony-118 (dónde viven las piezas).
- Archivo actual: `Core/Datos/DatosMecha.cs` (13 valores con `[Min]` y `[Range]`). Lo leen `VidaMecha`, `GolpeMecha`, `GuardiaMecha`
  (Combate), `ControladorMecha` (Mecha), `RivalBasico` (IA) y `AtacanteDePrueba`. Ninguno cambia en esta tarea.
- Estilo de contratos: `Core/Pelea/InfoPelea.cs` (struct de solo lectura con constructor), `Core/Ligas/IProgresoLigas.cs`. Los `///` son el contrato.
- Alan no lo necesita: no hace falta llevarlo a main antes que el resto del bloque; viaja con tony/fase-7. Avisa en docs/revision-tony.md
  ("Para Alan") que existe, por si algún día crea mechas del mundo por código.

## Qué hacer
1. `Core/Datos/ValoresMecha.cs`: `[Serializable] public struct ValoresMecha` con un campo público por cada valor de `DatosMecha`, mismos
   nombres y unidades en `///` (nombre, vidaMaxima, velocidadMaxima, aceleracion, desaceleracion, velocidadGiroMaxima, danioGolpe,
   alcanceGolpe, preparacionGolpe, recargaGolpe, tiempoSubirGuardia, reduccionBloqueo, ventanaBloqueoPerfecto). Sin lógica de piezas: la
   suma de piezas es de Tony (Mecha/), no de Core.
2. `DatosMecha.Valores` (propiedad de solo lectura que devuelve un `ValoresMecha` con todos los valores actuales).
3. `public static DatosMecha DatosMecha.CrearEnEjecucion(ValoresMecha valores)`: `ScriptableObject.CreateInstance`, copia los valores
   aplicando los mismos límites del inspector (`[Min]`, `[Range]`: un valor fuera de rango queda en el límite, NaN usa el valor por
   defecto), `name` = nombre + " (ejecución)" y `hideFlags = HideFlags.DontSave`. `///`: nunca se guarda como asset; quien lo crea lo
   destruye cuando ya no lo usa (`Object.Destroy`); no reemplaza a los assets de los rivales.
4. Ningún campo serializado cambia de nombre ni de tipo: los 28 assets `DatosMecha_*.asset` no cambian al abrir el proyecto ni al correr
   las pruebas.
5. EditMode `Tests/EditMode/Core/ValoresMechaPruebas.cs`: ida y vuelta (`CrearEnEjecucion(asset.Valores).Valores` igual al asset
   `DatosMecha_Jugador`), límites (vida 0 queda en 1; reducción 1.5 queda en 1; NaN usa el valor por defecto), `hideFlags` DontSave y
   que el objeto creado no tiene ruta en `AssetDatabase`.

## Carpetas permitidas
Assets/_Proyecto/Core/Datos/ (DatosMecha.cs y ValoresMecha.cs nuevo, con su .meta), Assets/_Proyecto/Tests/EditMode/Core/
No tocar: el resto de Core/, Combate/, Mecha/, IA/ (la aplicación es de T-165 y T-167), assets, ProjectSettings/, Packages/, todo lo de Alan.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa (salvo los 9 fallos conocidos de Alan en `JaulaClandestinoPruebas`, listados por nombre)
- [ ] ValoresMechaPruebas pasa (EditMode) y ReglasEnsambladosPruebas sigue en verde
- [ ] Tras la suite, `git status` no muestra ningún `DatosMecha_*.asset` modificado
- [ ] Grep: `ValoresMecha.cs` no referencia nada fuera de Core ni de UnityEngine; ningún archivo de Alan cambió; todo lo que ya leía
      `DatosMecha` compila sin cambios

## Criterios de sensación (para un humano)
- Tony: aprobar el cambio de Core antes de que empiece (D-tony-114). Leer los `///` de `ValoresMecha` y `CrearEnEjecucion`: ¿queda claro
  quién lo crea, quién lo destruye y que nunca se guarda?

## Pruebas automáticas
- EditMode: ValoresMechaPruebas.

## Fuera de alcance
- Sumar piezas (T-166), cambiar los datos de un mecha en juego (T-165, T-167). Cualquier cambio en los rivales.
