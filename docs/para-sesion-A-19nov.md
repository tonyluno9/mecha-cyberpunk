# Para la sesión A (de la sesión B, 19-nov)

La sesión B solo toca los prefabs T1355_Cerro y T1357_Puerto. Esto es de la sesión A (luz global, postproceso, materiales
compartidos, escena) y hace falta para que el Cerro y el Puerto se parezcan a K13, A13, K15 y A15:

| Qué | Dónde se ve | Por qué |
| --- | --- | --- |
| La niebla es muy densa y azul desde arriba: en A13 y A15 tapa la ciudad iluminada y el agua (la referencia tiene niebla baja y ligera, con la ciudad brillante visible) | docs/capturas/_vista19/cerro-a13.jpg, puerto-a15.jpg | Niebla global / volumen |
| Todo sale oscuro y sin contraste cálido: las referencias tienen luz de sodio naranja fuerte en el piso y fachadas | cerro-k13.jpg, puerto-k15.jpg | Exposición, ambiente y postproceso |
| Charcos con reflejo en el Puerto: si el material de charco compartido no refleja en el patio, la sesión B necesita uno que sí (o que el piso mojado global cubra el Puerto) | puerto-k15.jpg | Material compartido |

| La niebla del juego (densidad 0.02) borra todo a más de ~100 m: en A13 y A15 la franja de la ciudad, el agua y el barco no se ven. Pedido: niebla menos densa en las vistas aéreas (por altura de cámara) o un volumen por zona para el Cerro y el Puerto | cerro-a13-niebla-t1440.png, puerto-a15-t1443.png en docs/capturas/_vista19b/ | Niebla global |

Se irá agregando lo que digan las tareas T-1440 a T-1444.
