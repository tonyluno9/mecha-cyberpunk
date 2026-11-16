# T-1019: el recorrido de la demo cierra la clínica del Doc

- Desde T-1005 el Doc primero dice "Elige tu prótesis..." y la clínica abre unos segundos después. El bot pulsaba Esc antes de que abriera,
  la clínica se quedaba abierta y todo lo demás fallaba en cadena (7 bugs desde EL INVENTOR).
- Ahora el bot espera hasta 15 s a que la clínica abra y la cierra con Esc; si en una traba junto al Doc la E vuelve a abrirla, también la cierra.
- RecorridoDemoPruebas: 3/3 (llega al juego libre). Quedan 4 bugs que no lo detienen: el bot tarda en llegar al Doc, a EL INVENTOR y a
  EL HACKER, y el marcador de EL HACKER queda a 165 m del personaje (Neón Alto).
