# T-988 Recorrido de la demo: pruebas en verde

## Qué fallaba
`RecorridoDemoPruebas.NavMesh_LugaresDeLaHistoria_TienenCamino`: "casa -> DOC llega a menos de 3 m del lugar" (muestreo 0.3/7.0, fin del camino a 3.7 m).

## Causa (la sospecha del filtro de T-979 era falsa)
- El filtro de mallas sin lectura de T-979 solo quita 2 fuentes (los "Visual" de la pasarela PS01, en (47, 58)), nada cerca de DOC.
- El punto de la prueba para DOC, (-32.8, 1.24, 28.9), era viejo: EL DOC real está en el marcador (-100.3, 0.24, -13.4) (log del recorrido: "EL DOC: marcador (-100.30, 0.24, -13.40)"). El punto viejo cae en un local donde, a y=1.24, el NavMesh más cercano era el techo (8.22, a 7.0 m).
- Aparte, el recorrido completo fallaba de forma intermitente en "El asalto no terminó | fase Maletin" (T-987): el bot se ponía a 1.4 m del maletín por un solo lado y E a veces tomaba el carro del asalto en vez del maletín.

## Arreglo (solo Tests/PlayMode/Demo/RecorridoDemoPruebas.cs)
- DOC en la prueba de NavMesh pasa a (-100.3, 0.24, -13.4).
- `RecogerMaletin`: el bot prueba desde 4 lados a 1.2 m y luego a 0.8 m hasta recoger el maletín (como una persona). Log "[T-988] maletín intento N".
- ServicioOpciones.cs: se quita `layerCullSpherical` (URP no lo soporta, llenaba el log de avisos). No queda otro uso ni prueba que lo espere.

## Resultados
- PlayMode RecorridoDemoPruebas + DemoDePrincipioAFin + VideoDemoT979: 3/3 pasan. Recorrido: 233 s (0 BUG en el log); NavMesh 5.5 s; video 152 s.
- Maletín: recogido en el intento 3 (intentos 1 y 2 tomaron otro candidato); el paso avanzó a PrimeraPelea. El monto ($10,000) lo cubren las pruebas de T-987; el recorrido no imprime la cartera.
- compilar OK, EditMode 2368/2368, OpcionesPruebas 1/1.
- Log sin excepciones ni LogError en el juego (solo avisos de shaders HDRP del ParticlePack, ajenos).
