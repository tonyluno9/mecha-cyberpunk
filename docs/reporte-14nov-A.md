# Reporte 14-nov, Sesión A

Pedido de Tony (13-nov noche): integrar T-976/T-978, clips de PJ01, patrullas con física, maletín del asalto, recorrido de la demo y builds.
Opus dirigió y revisó (diagnóstico, capturas y verificaciones); Sonnet implementó. Todo en main con `--no-ff` y push.
Los números de tarea T-985 a T-988 se eligieron saltando T-980 a T-984 para no chocar con la Sesión B (`.sesiones/s2`).

## 1. T-976, T-978 y T-979 → main (`ad2221a`)
- Revisado el código: bajar de la moto sin tirones (T-976), calidad BAJO/MEDIO/ALTO (T-978) y video de la demo (T-979).
- Pruebas: EditMode 2366/2366. PlayMode de moto, carro y opciones: 17/17.
- Se quitó la copia `mecha-sonido` (`git worktree remove --force` + `prune`).
- Arreglo que salió después (en T-988): T-978 ponía `Camera.layerCullSpherical`, que URP no soporta. Llenaba el log con 94 avisos por corrida. Se quitó; la distancia de dibujo por capa sigue igual.

## 2. T-985: los 80 clips de PJ01 en el jugador (`22470db`)
- Auditoría automática nueva (`ClipsPJ01JugadorT985Pruebas`, EditMode): recorre todo el ControladorBase con Override_Jugador.
  - Antes: 3 clips de Mixamo (Burla, Presumir, Pitar) y 3 clips PJ01 sin estado (Pistola_ApuntarCaminar_Derecha e Izquierda, Punos_Guardia).
  - Ahora: 0 clips de Mixamo, 80/80 clips usados.
- Conectado:
  - **GirarApuntarDer/Izq**: girar en el sitio apuntando con pistola.
  - **PunosGuardia**: la guardia 2.5 s después de un golpe a puños.
  - **Pitar**: usa PJ01_Carro_Manejar.
  - **Burla/Presumir del jugador**: usan PJ01_Punos_Guardia. El jugador nunca los lanza; los otros papeles conservan sus clips.
  - **VerTelefono**: estaba huérfano en el jugador. Ahora sale al sacar el teléfono.
- Los demás grupos se revisaron por código y sí se disparan: locomoción con y sin implante, salto, agachado, rodar, pistola, rifle, golpes, muertes, levantarse, carro, moto, cobertura e interacciones.
- Capturas de los 80 clips en su estado del Animator: `docs/capturas/t985/` y la hoja `docs/capturas/t985/_hoja.png`. Las revisó Opus: ninguna en T-pose ni vacía.
- Pruebas: EditMode 2367/2367. PlayMode de animación: 40/40.

## 3. T-986: patrullas con la física del carro del jugador (`68dab03`)
- Antes, la patrulla se movía por transform con el Rigidbody cinemático: sin inercia, derrape ni frenado real.
- Ahora es un `CarroViejo` no cinemático, con la misma física y config que el carro del jugador.
  - Un conductor IA puro (`ConductorCarroIA`) solo da acelerador, freno, volante y freno de mano. Sigue un punto de mira sobre la ruta (persecución pura) y frena antes de las curvas.
  - Si se atora, da reversa con el volante contrario. No hay teletransportes: solo la colocación inicial, fuera de la vista.
  - Al robarla, la IA se suelta.
- `BloqueosDeCalles` cierra en el grafo solo los tramos tapados de lado a lado. En `Ciudad_Prologo` cierra 0 de 13 tramos.
- Persecución medida en Ciudad_Prologo (80 s, calor 3, 6 patrullas):
  - 0 cuadros cinemáticos, 0 saltos de posición, 0 giros sobre su eje.
  - Frenado máximo 18–20 m/s², derrape lateral de hasta 1.85 m/s, 0 volcadas.
  - Alcanzan al jugador a los 13 s.
- Capturas en `docs/capturas/t986/`, revisadas por Opus: `ciudad-1..6` y `persecucion-*`. Las patrullas van sobre la calle y rodean al jugador en el cruce.
- Pruebas: EditMode 2383/2383. PlayMode de patrullas, IA y asalto en una sola corrida: 67/70.
  - Las 3 que fallan son de `AsaltoDineroT728Pruebas` y fallan **igual en main sin T-986** con la misma combinación (ahí son 4 fallos). Solas pasan 6/6. Ver Pendientes.
- El derrape existe pero es moderado: no se cambió el agarre del carro del jugador, que es el mismo.

## 4. T-987: el maletín del asalto (`4c7f922`, ajuste en `5014b5e`)
- Al detenerse la camioneta cae un maletín (con brillo y luz pequeña), con el objetivo "RECOGE EL MALETÍN" ("BÁJATE Y…" si vas en carro) y el marcador encima.
- Se recoge con E. Da **$10,000** una sola vez, con los avisos "MALETÍN RECUPERADO · +$10,000" y "+$10,000 · PREMIO", y la historia sigue.
- El asaltante baja y pelea, pero ya no hace falta vencerlo.
- Ajuste de Opus: el maletín cae detrás de la camioneta (4.2 m). Junto a la puerta, la E a veces tomaba el carro.
- Capturas: `docs/capturas/t987/`.

## 5. T-988: RecorridoDemoPruebas (`5014b5e`)
- Fallaba `NavMesh_LugaresDeLaHistoria_TienenCamino` porque el punto de DOC en la prueba era viejo. Ahora es el del marcador real: PathComplete a 0 m.
- El recorrido completo a veces no recogía el maletín por el problema de la E; quedó resuelto con el ajuste de T-987.
- Resultado: DemoDePrincipioAFin pasa (233 s, 0 `[BUG]`), NavMesh pasa y VideoDemoT979 pasa. Con el maletín nuevo, 8/8 junto con las del asalto, y se recoge al primer intento.

## Builds (desde main `68dab03`, uno por uno, sin ningún Unity de la Sesión B corriendo)
- **Demo**: `C:\Users\Angel Perez\Desktop\videojuego\mecha-cyberpunk\Builds\Demo\CyberCity.exe`, hecho con ConstruirDemoPandillas (lleva DEMO_FINAL).
  - Terminó a las **19:12** del 13-nov, hora de México. Logs/build-demo-14nov.log dice "Build Finished, Result: Success". Pesa 1.9 GB.
- **Juego**: `C:\Users\Angel Perez\Desktop\videojuego\mecha-cyberpunk\Builds\Juego\CyberCity.exe`, hecho con ConstruirJuego.
  - Terminó a las **19:13**. Logs/build-juego-14nov.log dice "Build Finished, Result: Success". Pesa 1.8 GB.
- Unity quedó cerrado al terminar.

## Pendientes y notas
- `AsaltoDineroT728Pruebas`: 3 pruebas fallan cuando corren después de las clases de patrullas e IA, en main y en la rama.
  - El log dice "E no recogió: candidato Recoger maletín, distancia 1.4". El jugador sí elige el maletín, pero la E simulada no llega.
  - Parece estado de entrada que dejan las pruebas anteriores. Solas pasan 6/6.
  - Es de las pruebas, no del juego. No se arregló.
- `EfectosCombateT745Pruebas` (2 pruebas): fallan en corridas largas combinadas y pasan solas 7/7. Ya se sabía que Personaje depende del orden.
- `PatrullaFisicaT707Pruebas.Patrulla_EsUnCarroSolido_ConCuerpoCinematicoYColliders` fallaba en main antes de T-986. T-986 reescribió la prueba (ahora no cinemática) y pasa.
- No se corrió PlayMode completo (por memoria, con el Unity de la Sesión B): solo clases filtradas por área.
- Archivo suelto `C:\ANGELP~1\x` (1 byte) creado por un agente: borrado. La carpeta `C:\ANGELP~1` ya existía desde octubre y no se tocó.
