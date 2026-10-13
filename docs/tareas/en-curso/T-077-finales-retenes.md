# T-077 Finales del barrio sólidos y retenes que cierran las calles
Dueño: Alan | Rama: alan/T-077-finales-retenes | Modelo: Sonnet 5.5, medium
Depende de: T-074 (el prefab del carro, para las pruebas de choque).

## Objetivo
Cada calle del barrio termina en algo creíble y sólido (retén, obra o la autopista elevada), nunca en un muro invisible, y el
carro no puede salir del barrio aunque llegue a toda velocidad. Los retenes de seguridad corporativa tienen patrulla y torretas
que parpadean. Es la parte "retenes que cierran las calles" de A-F2 y la condición "sin atravesar nada" de H3.

## Contexto
- Diseño: docs/diseno/ciudad.md (las calles terminan en algo creíble, como un retén, una obra o una autopista elevada; el calor lo
  persiguen drones y patrullas de seguridad corporativa: aquí solo la escenografía, sin persecución).
- Lo que hay (Mundo/Editor/CreadorBarrio.cs, `CrearFinales`): el barrio mide 268 × 174 m (`MitadBarrioX` 134, `MitadBarrioZ`
  87); hay tres calles de 14 m: la principal este-oeste en z = 0 y dos norte-sur en x = ±47. Sus 6 extremos: este (retén
  `Final_Reten`, x = 134), oeste (obra `Final_Obra`, x = -134), los dos del norte (muro bajo la autopista, z = 87) y los dos del
  sur (obra `Final_Obra_Sur`, z = -87).
- Propuesta D-alan-15 en docs/decisiones-pendientes-alan.md. Las luces nuevas se prenden de noche como las farolas
  (`LuzNocturna`, T-041) o parpadean siempre (torretas).

## Qué hacer
1. En CreadorBarrio (o en un archivo nuevo `Mundo/Editor/CreadorFinales.cs` que CreadorBarrio llame antes de guardar el
   prefab), los dos extremos del norte pasan a ser retenes bajo la autopista: barreras tipo jersey a lo ancho de la calle, caseta,
   una patrulla greybox de seguridad corporativa estacionada atravesada y un letrero "ALTO · CONTROL CORPORATIVO". El retén del
   este gana la misma patrulla. La obra del oeste y las del sur se quedan, cerrando todo el ancho de la calle.
2. `Mundo/Ambiente/LuzTorreta.cs`: alterna rojo y azul en dos luces o materiales de la torreta de cada patrulla; la cuenta en
   lógica pura (`CalculoTorreta`, con `dt` y fase inicial por patrulla para que no parpadeen todas igual). Valores en un
   `ConfigTorreta`.
3. Cada cierre es sólido y visible: sus colliders cubren todo el ancho de la calle y de las banquetas, de 0 a 3 m de alto como
   mínimo, y cada collider de cierre tiene un renderer visible que lo cubre (nada de colliders invisibles). Detrás de cada cierre
   hay un muro de 4 m o más que tampoco se puede cruzar.
4. Regenera con `CreadorTodo.Crear` (no con CreadorBarrio suelto) y deja Ciudad.unity y Pruebas_Alan.unity actualizadas.

## Carpetas permitidas
Assets/_Proyecto/Mundo/, Assets/_Proyecto/Escenas/Ciudad.unity, Assets/_Proyecto/Escenas/Pruebas_Alan.unity,
Assets/_Proyecto/Tests/EditMode/Mundo/ y Assets/_Proyecto/Tests/PlayMode/Mundo/
No tocar: Core/, Vehiculos/ (solo se instancia su prefab en las pruebas), Clandestino.unity, las carpetas de Tony.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa (BarrioPruebas, LugaresPruebas y CicloDiaNochePruebas siguen pasando)
- [ ] EditMode `CalculoTorretaPruebas`: alterna con el periodo del config; dos fases iniciales distintas dan colores distintos en
      el mismo instante
- [ ] PlayMode `FinalesCarroPruebas` en Pruebas_Alan: para cada uno de los 6 extremos, el carro colocado en el centro de la calle a
      40 m del cierre, mirándolo, con control y W sostenida 8 s: nunca sale del rectángulo del barrio (|x| ≤ 134, |z| ≤ 87),
      nunca cruza la línea del cierre y su posición nunca es NaN
- [ ] PlayMode: en cada extremo, 5 rayos horizontales a lo ancho de la calle a 0.5, 1.5 y 2.5 m de alto chocan con el cierre, y
      el objeto con que chocan (o su padre) tiene un `MeshRenderer` prendido
- [ ] PlayMode: hay 3 retenes con patrulla (este y los dos del norte) y sus torretas cambian de color al menos 4 veces en 3 s
- [ ] Capturas de noche desde la calle, a 25 m de un retén del norte (docs/capturas/T-077-1.png) y de la obra del sur (T-077-2.png)
- [ ] Consola sin errores en las pruebas

## Criterios de sensación (para un humano)
- Al llegar a un extremo se entiende de un vistazo que la calle está cerrada y por qué (retén o obra), sin sentir un muro de
  videojuego.
- Las torretas llaman la atención sin cegar.

## Pruebas automáticas
- EditMode (Tests/EditMode/Mundo/): CalculoTorretaPruebas.
- PlayMode (Tests/PlayMode/Mundo/): FinalesCarroPruebas, con el prefab CarroViejo (`TomarControl(true)` y entrada simulada).

## Fuera de alcance
- Patrullas o drones que se mueven o persiguen (calor, A-F5); retenes que se abren
- Arte final de patrullas y vallas (A-F4)
