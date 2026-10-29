# T-198 Contratos del bloque 8: la clave `adaptacion` y la llave del rival (Core)
Dueño: Tony (tarea de Core, D-032) | Rama: tony/T-198-contratos-adaptacion | Modelo: Sonnet 5.5, medium
Necesita aprobación: Tony pidió el bloque 8 el 2026-10-28. Esta tarea solo cambia comentarios de Core (como T-163, D-tony-128). Si Tony
no la aprueba, T-199 a T-205 siguen igual: ningún código depende de estos comentarios.
Depende de: nada

## Objetivo
Dejar escrito en Core, antes de usarlo (D-027), que la memoria de los rivales se guarda con la clave `adaptacion` y que el nombre del
asset `DatosRival` es la llave con la que un rival recuerda tu forma de pelear. Es lo único del bloque 8 que toca Core: la adaptación vive en
IA, Combate y UI, todo de Tony (D-tony-191).

## Contexto
- Diseño: combate.md, caso límite "El rival se adapta a tu forma de pelear en peleas repetidas" (Propuesta). docs/hoja-de-ruta.md, T-F7:
  "... adaptación del rival".
- Decisiones: D-027, D-032, D-tony-128. Propuestas de este bloque: D-tony-190 a D-tony-201 (sobre todo D-tony-191, D-tony-194 y D-tony-199).
- Archivos: `Core/Guardado/IGuardable.cs` (lista "Claves en uso"), `Core/Datos/DatosRival.cs`.
- Por qué no hay contrato nuevo: Alan no hace rivales ni los lee, y la pantalla de la cabina (UI) leerá la adaptación por una interfaz en
  Combate/Lectura/ (como los OJOS, D-tony-168). No se agrega un campo `id` a `DatosRival`: la llave es `name` y renombrar un rival es raro.
- Es tarea de Core: se integra a main en cuanto el verificador la apruebe y se avisa en docs/revision-tony.md (sección "Para Alan").

## Qué hacer
1. `IGuardable`: en "Claves en uso" agrega `adaptacion` (Tony, T-200). Solo comentario.
2. `DatosRival`: en el `<summary>` de la clase, una frase: el nombre del asset (`name`) es la llave con la que el rival recuerda al jugador
   entre peleas (IA, `AdaptacionRival`, T-200); renombrar el asset de un rival que se adapta le borra lo aprendido en las partidas guardadas.
   Sin campos nuevos ni cambios de valores.
3. docs/revision-tony.md, sección "Para Alan (T-198)": no le toca nada. `RivalBasico.prefab` (el base) y `FlujoPelea.prefab`, los de su
   Clandestino, no se adaptan, así que sus pruebas no cambian. Si algún día quiere que el público o un cartel reaccione a una revancha contra
   un rival que "ya te conoce", se pide una tarea de Core (D-tony-200).

## Carpetas permitidas
Assets/_Proyecto/Core/Guardado/IGuardable.cs y Assets/_Proyecto/Core/Datos/DatosRival.cs (solo comentarios), docs/revision-tony.md (sección de esta tarea)
No tocar: el resto de Core/, ProjectSettings/, Packages/, IA/, cualquier asset o prefab, todo lo de Alan.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh compilar` y `bash herramientas/verificar.sh editmode` pasan; ReglasEnsambladosPruebas sigue en verde
- [ ] `git diff main -- Assets/_Proyecto/Core` solo muestra líneas de comentario (`///` o `//`) en esos dos archivos
- [ ] Ningún .asset, .prefab, .unity ni .meta cambió
- [ ] docs/revision-tony.md tiene "Para Alan (T-198)" con lo del punto 3

## Criterios de sensación (para un humano)
- Leer los dos comentarios con la mirada de Alan: ¿queda claro que no le toca nada y por qué?

## Pruebas automáticas
- Ninguna nueva (solo comentarios): compilar y EditMode completos.

## Fuera de alcance
- Todo lo demás del bloque (T-199 a T-205). Un `id` propio en `DatosRival`. Que el presentador anuncie la adaptación (necesitaría Core;
  D-tony-201 g).
