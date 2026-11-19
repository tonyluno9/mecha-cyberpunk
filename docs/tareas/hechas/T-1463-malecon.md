# T-1463: el Malecón igual a K11 y A11, con vida

Rama `tony/T-1463-malecon` (sale de `integ/look-19nov-A`). Script `Assets/Arte/Editor/ZonaMaleconT1463.cs`, raíz `T1463_Malecon` dentro de `T1354_Malecon.prefab`.
Runtime nuevo: `Assets/_Proyecto/Mundo/Ambiente/PoseQuieta.cs` (estado Idle del Animator con fase al azar, AlwaysAnimate) con pruebas en `Tests/EditMode/Mundo/PoseQuietaPruebas.cs`.

## Criterios
1. Marcas `Vista19b_K11` (x = -135, a 9.6 m del eje, 1.9 m de alto, campo 60, mira al oeste donde la avenida ya curva a la derecha) y `Vista19b_A11` (150, 40, -300 hacia 90, 0, -275, campo 70).
2. Fila del paseo rehecha: farol ML01 cada 15 m y palmera AR03 a medio camino (a 12.4 m del eje); las viejas del paseo se apagan (se reencienden al rehacer). Escaleras (7), escollera ML05, puestos ML03 y faro con haz ya existen (T-1354/T-1363/T-1413): se validan.
3. Letrero de neón emisivo (sin Light) en cada edificio del frente en cuadro de K11 o A11 que no tenga.
4. Vida: ~40 personas NP07..NP14 en Idle (PoseQuieta) en el paseo y la banqueta; basura, huacales, tambos y botes en el paseo. `PoblarFoto(Camera)` y `QuitarFoto()` para la foto (carros con luces en los carriles y gente caminando, no se guardan).
5. Cupo de luces +40: solo 28 spots de farol (sin sombras); halos y letreros sin Light.
6. `Validar` escribe `Logs/t1463-validar.txt` (falla con Debug.LogError). `ValidarJuego` revisa en la escena que haya ZonaTrafico Malecon y CalleZona con banquetas.

## Qué se hizo
Todo escrito sin abrir Unity (no compilado ni corrido por el implementador): hay que correr Aplicar y Validar y ajustar con las fotos.

## Hallazgos
- El peatón de zona del juego sale de `GrupoPeatones.CrearPeatonesDeZonas` (una ruta por CalleZona con banquetas, tope 16 en todas las zonas) y el visual de `ConfigPeatonesZona_Base` (zona Malecon, 10 NP). Que salgan en el Malecón depende de que haya CalleZona con banquetas ahí: lo revisan `Validar` y `ValidarJuego`.
- Las cámaras dependen de lo que ya hay: la curva fuerte de la avenida está al oeste (x de -110 a -300, K11); al este (x de 60 a 160) es muy suave. El faro lejano (islote en -60, -335) y el muelle (x = 92) quedan a 150 m uno del otro; A11 se fijó para verlos a los dos.
- NP01..NP06 no existen: solo NP07..NP14.

## Assets que faltan
- Clips de "recargado en el barandal" y "hablando" para los NP (solo hay Idle, Caminar, Recargar de arma).
- Cables cruzando del poste a las fachadas con más hilos (existen los de T-1393; revisar en la foto).
- Textura de arena de playa (la playa usa un color liso T1463_Arena).
