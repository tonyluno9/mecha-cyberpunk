# T-1317 Ajustes al mundo (playtest 17-nov, punto 17)

Script: Assets/Arte/Editor/ColocarT1317Ajustes.cs (Aplicar = escena; Fotos = capturas sin guardar). Idempotente.

- a) BarrilExplosivo: una explosion dentro del radio revienta el barril siempre, tras BarrilExplosivo.RetrasoCadena (0.15 s). En la escena: 60 barriles/tambos de ciudad, 14 recibieron collider, todos llevan BarrilExplosivo. Los dos tambos visuales T507 pasan a hijos de Barril1/2 para desaparecer con ellos.
- b) Postes: IntensidadPoste 40 -> 54 (102 luces, solo intensidad). Ningun otro script la pisa (T507/T509 no tocan postes; KitS2c 340 solo en el kit y T1102 usa la constante).
- c) Quitados S2t_Casa/Tablero_C15a y Tablero_Texto; ColocarS2tCasa ya no los crea. El TableroEncargos invisible (interaccion) se queda.
- d) Quitados Letrero_NOVA (con mastiles) y Haz_Nova; ColocarT1103Torres ya no llama LetreroYHaz. T-1103 solo cambio materiales (nitidos) y agrego avisos rojos: se quedan.

Pruebas: EditMode Mundo+Vehiculos 566/566; PlayMode BarrilesCadenaT1317 + EfectosCombateT745 + DanioCarro 14/14.
Pruebas viejas actualizadas (T-1317): CasaS2t, CiudadTorresT1103, CallesLlenasT508, SinGreyboxT506, EfectosCombateT745 (espera de cadena).
Capturas: docs/capturas/t-1317/ (casa, torre Nova, calle antes/despues; la mejora de luz es sutil: charco naranja bajo el poste).

Decisiones: los tambos decorativos de T506/T508 ahora tienen collider (solidos) para poder reventar. CreadorCiudadPandillasT426 (Progresion) sigue creando un TextMesh ENCARGOS que ColocarS2.Limpieza quita; no se toco.
- Revisión (Opus): con 54 casi no se notaba; postes a intensidad 70, alcance 12 m (antes 9) y cono 80° (el máximo de PostesS2tPruebas; antes 75). EditMode Mundo+Vehiculos 566/566.
