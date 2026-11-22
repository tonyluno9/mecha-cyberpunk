# Reporte: tema del menú y control de volumen

Rama `integ/musica` → main. Resultados del build en `docs/capturas/_musica/build-demo/` (1920×1080) y `build-juego/` (1366×768): `resultados_fase1.txt`, `resultados_fase2.txt` y capturas de la pestaña AUDIO.

## 1. Tema del menú
| Punto | Quedó | Evidencia en el build (Demo y Juego) |
| --- | --- | --- |
| Archivo | Ver detalle abajo | `TemaDelMenu_ImportadoComoMusica_YEnElCatalogo` |
| Importado como música | Ver detalle abajo | pista estéreo y grupo Musica en los resultados |
| Bucle sin clic | Ver detalle abajo | "dio la vuelta: True" |
| Fundido de entrada | De 0 a 1 en 1.5 s | nivel 0.07 → 0.38 → 0.71 → 1.00 en 0 / 0.5 / 1 / 2 s |
| Sin reiniciarse entre pantallas del menú | Sí | al abrir opciones la pista siguió de 4.4 s a 6.0 s, no volvió a 0 |
| Nueva partida o Continuar | Ver detalle abajo | "se apaga ANTES" (apagada a los 0.09–0.13 s, juego a los 3.0–3.2 s) |
| Salir al menú desde el juego | Vuelve a sonar | "en el menú otra vez: nivel 1.00, sonando True" |
| Catálogo | `MUS_MENU_PRINCIPAL` en CatalogoSonidos, sección Música, encendido | prueba EditMode |

**Archivo**
- Quedó en `Assets/Arte/Audio/Musica/MUS_MENU_PRINCIPAL.wav`, por Git LFS. La copia de la raíz se borró.

**Importado como música**
- Estéreo, Streaming, Vorbis, sin precarga.
- La regla general de `ImportadorAudio` lo volvía mono y en memoria; ahora la carpeta `Musica` queda fuera de esa regla.

**Bucle sin clic**
- El archivo **no empalma limpio**: empieza fuerte (RMS 13 407) y termina suave (RMS 3 264).
- Por eso el bucle es un cruce de igual potencia de 2.5 s entre dos copias, en vez de loop.

**Nueva partida o Continuar**
- La pantalla de carga baja el tema con un fundido de 1.6 s, menos que los 2.2 s mínimos de la carga, así que se apaga antes del juego.
- En la prueba del build sale "0.09 s" porque la prueba acababa de recargar el menú y el tema iba entrando (nivel 0.04). En uso normal tarda 1.6 s.
- Cubre también cargar una partida o cargar desde la pausa: todo pasa por la pantalla de carga.

**Detalle técnico:**
- Los fundidos avanzan como mucho 0.05 s por cuadro. Al cargar escenas hay cuadros de varios segundos que antes se comían el fundido.
- El punto del bucle sale de la posición del audio.
- Si una copia se acaba sin cruce, vuelve a empezar, así que nunca queda en silencio.

## 2. Volumen
| Punto | Quedó | Evidencia |
| --- | --- | --- |
| AudioMixer | Ver detalle abajo | `Mezclador_TieneLosCuatroGrupos...` |
| Deslizadores | **Volumen general, Música, Efectos y Voces** en la pestaña AUDIO del menú y en las opciones de la pausa. Voces queda lista para cuando haya voces | `1_opciones_audio.png`, `2_opciones_audio_guardadas.png` |
| Escala en decibeles | `dB = 20·log10(valor)`; **0 = -80 dB (silencio)** | build: 0.30 → -10.5 dB, 0 → -80 dB, 1 → 0 dB, 0.55 → -5.2 dB |
| Al instante | Cada movimiento del deslizador cambia el mezclador en el mismo cuadro, y la música del menú sube y baja en vivo | ídem |
| Se guardan | En `opciones.json`. Al abrir otra vez el juego se leen y se aplican al mezclador | fase 2: GENERAL 0.90, MÚSICA 0.55 y EFECTOS 0.40, con VolGeneral -0.9 dB, VolMusica -5.2 dB y VolEfectos -8.0 dB |
| Todo pasa por su grupo | Ver detalle abajo | `SonidosRegresanT977Pruebas` (el bucle de un carro va por "Efectos") |
| Mouse y teclado | Los deslizadores (`FilaOpcion`) ya aceptaban clic, arrastre y ←/→ | PlayMode `InicioPruebas` 11/11, con flechas sobre el volumen |

**AudioMixer**
- `Resources/MezclaCyberCity.mixer` con los grupos Master, Musica, Efectos y Voces.
- Volúmenes expuestos: VolGeneral, VolMusica, VolEfectos y VolVoces.
- Unity no tiene API pública para crear mezcladores: lo creó `CrearMezclaAudio` con la del editor.

**Todo pasa por su grupo**
- Todas las fuentes de `GestorSonidos` (efectos, pasos, armas, carros, interfaz, bucles) van a su grupo, y el ambiente del barrio también.
- `AmbienteSonoro`, de Mundo, pide su grupo por el contrato nuevo de Core `IMezclaAudio`.
- Ambiente e Interfaz van por **Efectos**. La música va por **Musica**.
- No hay otros AudioSource en código ni en prefabs del juego.

## Decisiones viejas que cambiaron (y pruebas ajustadas)
- **Música apagada por defecto:** la música y el ambiente arrancaban en 0 (T-407).
  - Ahora arrancan en 0.8, como lo demás.
  - Tu `opciones.json` tiene la música en 0, de ese defecto viejo. Se migra **una sola vez**: los archivos de antes (`versionVolumen` 0) suben la música a 0.8.
  - Si después la bajas a 0, se respeta.
  - Pruebas ajustadas: `CatalogoSonidosPruebas`, `ReglaOpcionesPruebas` y `RecorridoRonda2Pruebas`.
- **Los deslizadores ya no multiplican el volumen de cada fuente:** lo hace el mezclador.
  - `SonidosRegresanT977Pruebas` ahora comprueba el grupo del mezclador.
- **De 6 a 4 deslizadores:** se quitaron "Ambiente" e "Interfaz", que van por Efectos.
- **Lista de IDs encendidos:** se agregó `MUS_MENU_PRINCIPAL` y la cuenta subió de 24 a 25 en `SonidoMinimoT620Pruebas`.
- **`IdsSonidoPruebas`:** ahora busca también en `Assets/Arte/Audio/Musica`.

## Pruebas
- EditMode: `MusicaMenuYMezclaPruebas` 6/6, y 437/437 en las clases de sonido y opciones.
- PlayMode: `InicioPruebas` 11/11, `OpcionesPruebas` y `SonidosRegresanT977Pruebas` 3/3.
- Build: `UI/Pruebas/PruebaMusicaBuild`, que solo actúa con `-pruebaMusica <carpeta>` y luego `-fase2`, y usa su propio `opciones.json`, nunca el tuyo. Corrió en Demo y en Juego, como se muestra arriba.

## Builds
`Builds/Demo/CyberCity.exe` y luego `Builds/Juego/CyberCity.exe`, uno por uno, con "Build Finished, Result: Success".

## Qué probar (Tony), con audífonos
1. Abrir el juego: el tema entra suave y sigue al pasar a opciones o créditos. Espera unos 40 s y revisa que el bucle no tenga clic ni salto.
2. Opciones > AUDIO: mueve MÚSICA con el mouse y con ←/→. Debe bajar y subir en vivo, y en 0 no se debe oír nada.
3. Nueva partida o Continuar: la música baja durante la carga y ya no suena en el juego.
4. Pausa > salir al menú: vuelve a sonar.
5. Cierra y abre el juego: los volúmenes siguen igual.
6. Un detalle a decidir: el tema entra con su volumen de mezcla del catálogo (ganancia de Música 0.5) multiplicado por el deslizador. Si se oye bajo, se sube esa ganancia.
