# Pedido al chat de arte — 6-nov-2026 tarde (CYBERFIX)

Del chat principal. Mismo flujo: prompts + script de PowerShell para WaveSpeed (Nano Banana 2.1, 1K, una imagen por prompt),
cada archivo con su nombre en su carpeta dentro de `C:\Users\Angel Perez\Desktop\videojuego\mecha-cyberpunk\`.
Estilo de siempre: cyberpunk de megaciudad mexicana, desgastado, acentos turquesa y naranja; sin texto salvo donde se pide.
Lo que es para Tripo: fondo blanco liso, vista 3/4, objeto completo, iluminación pareja.

## 1. Mockups de HUD → imageassets/ui/hud-opciones/
Tony quiere cambiar el HUD a pie (vida, escudo de energía, calor/búsqueda de policía, arma con su holograma y balas, minimapa,
objetivo de misión). Hoy se ve genérico. Pantalla 16:9 de juego en tercera persona de noche en la ciudad, con el HUD encima.
4 estilos distintos para elegir, cada uno una imagen:
- HUD_A_minimalista.png: líneas finas, mucho espacio, solo lo esencial.
- HUD_B_holografico.png: paneles translúcidos cian con escaneo y glitch sutil.
- HUD_C_tactico.png: estilo militar-tecnológico, segmentos, números grandes.
- HUD_D_neon_callejero.png: neón magenta/naranja, más callejero mexicano pero elegante.
Textos del HUD en español (VIDA, ESCUDO, $). Que se vean modernos y de juego comercial.

## 2. Modelos para Tripo → imagen en imageassets/modelos-nuevos/, el zip de Tripo a imageassets/modelos3d/
| Archivo | Qué es |
| --- | --- |
| P10_PuertaCuarto.png | Puerta interior de departamento pobre: madera pintada desgastada, perilla, calcomanías, de frente. |
| P11_EntradaCasa.png | Entrada a edificio de departamentos: marco, puerta de reja metálica, escalones, buzones, foco. De frente 3/4. |
| P12_FachadaConsultorio.png | Fachada del consultorio clandestino del Doc (I+D+I): planta baja con puerta metálica, ventana tapada, luz turquesa, cables. Sin texto. |
| L15_LetreroCanepa.png | Letrero de taller: arriba "EL CANEPA" grande y abajo "TALLER MECÁNICO", lámina pintada a mano con focos. (Este SÍ lleva texto.) |
| R01_MeseraRobot.png | Mesera robot del casino VivaCoin, femenina estilizada, carcasa blanca y dorada, charola, T-pose de frente cuerpo completo. Para Mixamo. |
| IMP01_ExoPiernas.png | Implante: exoesqueleto de piernas que va por fuera (cadera a tobillo), solo la pieza, de frente. |
| IMP02_ExoBrazos.png | Implante: exoesqueleto de brazos y hombros por fuera, solo la pieza. |
| IMP03_VisorCabeza.png | Implante: visor de cabeza que cubre ojos y sien, con lente turquesa. |
| O01_BebidaRayo.png | Lata de bebida energética (diseño propio, rayo), para la máquina expendedora. |
| O02_Papitas.png | Bolsa de papitas (diseño propio). |
| DR01_Dron.png | Dron de policía con las 4 hélices separadas del cuerpo y una pequeña torreta abajo. |

## 3. Textura → imageassets/calle/
- T01_Banqueta.png: textura de banqueta de concreto vista desde arriba, losas con juntas, manchas y grietas, que repita (seamless).

## Nota
- Las balas de los enemigos y del dron no necesitan modelo: el trazo y el sonido de bala rozando se hacen en Unity.
- Los carros por partes (ruedas, puertas, sin cristal) los saca Tony en Tripo de los modelos que ya tiene.
