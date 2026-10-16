# T-083 El bar con los props de Blender y mejor luz
Dueño: Alan | Rama: alan/T-083-bar-props-luz | Modelo: Sonnet 5.5, medium
Depende de: T-076 (prefabs de la barra, la mesa, el DJ y el letrero), T-079 (bloom para el neón).

## Objetivo
El interior del bar deja de ser una caja oscura de cubos: barra larga con neón, mesas con sus sillas, cabina de DJ con bocinas y
pantalla, letrero de neón en el muro, y una luz que deja ver el lugar sin perder el ambiente de cantina cyberpunk. Se sigue
entrando y saliendo sin carga y caminando entre mesas sin atorarse.

## Contexto
- Lo que hay (Mundo/Editor/CreadorBar.cs, T-043): interior de unos 20 × 14 m y 4.5 m de alto dentro de Barrio.prefab: barra de
  16 m de cubos, mesas de 1.2 m con dos bancos, cabina de DJ en la esquina derecha, luces cálidas y de neón que `ZonaInterior`
  apaga cuando no hay nadie. Revisión de la fase 1: "el interior sale algo oscuro (muros y techo)"; los props de Blender "aún no
  colocados".
- Prefabs de T-076: Prop_Barra (3 × 0.85 × 1.16 m, frente con neón cian y borde magenta), Prop_Mesa (mesa redonda con 4 sillas
  con neón en el respaldo), Prop_DJ (cabina de 2 m con platos, bocinas a los lados y pantalla), Prop_Letrero (2 × 0.8 m, en postes).
  Renders en `Assets/Arte/Fuentes~/bar/`.
- Las pruebas de T-043 (BarInteriorPruebas) deben seguir pasando: 36 rayos sin huecos, entrar en menos de 10 s, luces por
  presencia, entrada y salida una vez.

## Qué hacer
1. CreadorBar: la barra pasa a 5 Prop_Barra en fila (unos 15 m) en el mismo lugar, con los bancos de antes o mejores; la cabina
   de DJ pasa a Prop_DJ sobre el escenario, mirando al salón; las mesas pasan a 6 u 8 Prop_Mesa en una cuadrícula que deje
   pasillos de 1.2 m o más; un Prop_Letrero en el muro del fondo y otro junto a la puerta de los baños. Los colliders de cada prop
   (de T-076) se conservan.
2. Estante de botellas detrás de la barra (cubos de colores con emisión ligera) y tiras de neón en el techo.
3. Luz: muros y techo menos negros (material un poco más claro, luz ambiente del interior por `ZonaInterior` o luces de relleno),
   lámparas colgantes cálidas sobre la barra y las mesas, y el neón del DJ que pinta la pista. Máximo 12 luces en el interior, todas
   bajo el apagado por presencia de `ZonaInterior`.
4. Regenera con `CreadorTodo.Crear` y al final rehornea los reflejos (`CreadorAmbiente.HornearReflejos()`, T-079).

## Carpetas permitidas
Assets/_Proyecto/Mundo/, Assets/_Proyecto/Escenas/Ciudad.unity, Assets/_Proyecto/Escenas/Pruebas_Alan.unity y
Assets/_Proyecto/Tests/PlayMode/Mundo/
No tocar: Core/, Assets/Arte/ (las piezas son de T-076), Vehiculos/, Clandestino.unity, las carpetas de Tony.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa, con BarInteriorPruebas sin cambiar sus criterios
- [ ] PlayMode: dentro del bar hay 5 o más Prop_Barra, 1 Prop_DJ, entre 6 y 8 Prop_Mesa y 2 Prop_Letrero, y ningún cubo greybox de
      barra, mesa o cabina
- [ ] PlayMode con entrada simulada a las 22:00: la cámara libre entra, camina de la puerta a la barra, de la barra al DJ y de ahí a
      la mesa más lejana siguiendo puntos (W y giro con el ratón), sin atorarse (avanza más de 0.5 m en cada segundo) y en menos de
      30 s
- [ ] PlayMode: hay 12 luces o menos en el interior y todas se apagan con nadie dentro y se prenden al entrar
- [ ] Medido en el editor vivo (eval, cámara a una RenderTexture de 640 × 360): desde la entrada mirando a la barra a las 22:00, la
      luminancia media es al menos 40 % mayor que la línea base (medida igual antes de la tarea, en la bitácora) y menos del 3 % de
      píxeles saturados
- [ ] Capturas desde la entrada mirando a la barra (docs/capturas/T-083-1.png), desde la barra mirando a la pista y al DJ
      (T-083-2.png) y entre las mesas (T-083-3.png)
- [ ] Consola sin errores en 3 minutos entrando y saliendo

## Criterios de sensación (para un humano)
- Al entrar dan ganas de quedarse: se siente cantina cyberpunk, con el neón de la barra y del DJ como protagonistas.
- Se camina entre mesas a pie sin atorarse y nada se ve fuera de escala.

## Pruebas automáticas
- PlayMode (Tests/PlayMode/Mundo/): BarPropsPruebas (conteos, recorrido, luces); BarInteriorPruebas sin cambios.

## Fuera de alcance
- Personajes, barman, música y audio del bar (A-F3 y A-F4); misiones del bar (Tony)
- Interiores de la casa y del clandestino (A-F3)
