# T-080 La calle vestida con el kit urbano
Dueño: Alan | Rama: alan/T-080-calle-kit-urbano | Modelo: Sonnet 5.5, medium
Depende de: T-076 (prefabs del kit), T-079 (ambiente y vistas de referencia).

## Objetivo
Las banquetas y azoteas del barrio dejan de ser cubos: postes de luz con cables colgando entre ellos, transformadores, puestos de
tacos, basureros con grafiti y tinacos con aire acondicionado en las azoteas, todo del kit de Blender. Las calles siguen libres
para el carro y las banquetas para caminar.

## Contexto
- Prefabs de T-076 en `Mundo/Prefabs/Kit/`: Poste_Luz (7.5 m, brazo de 1.55 m hacia el frente, con stubs de cable a ±4 m),
  Transformador_Poste, Puesto_Tacos, Basurero_Grafiti, Azotea_AC_Tinaco (mira los renders en `Assets/Arte/Fuentes~/kit-urbano/`).
- Lo que hay (Mundo/Editor/CreadorBarrio.cs): `CrearFarolas` pone 4 farolas por lado de manzana que da a una calle
  (desplazamientos -33, -12, 12 y 33 m, a 0.6 m del borde) con una `Light` hija que `LuzNocturna` prende de noche (CreadorReloj
  busca objetos que empiezan con "Farola_"); `CrearAmbientacion` pone botes y carros greybox junto a la banqueta, lejos de
  `puntosLibres` (entradas y boca del callejón); `EdificioPlano` pone un "Tanque" greybox en azoteas no pobres.
- Banquetas de 3 m: los edificios llegan a 37 m del centro de la manzana y el borde está a 40 m. Lo del kit va del lado del borde
  (39 a 40 m); la franja de 37.3 a 38.7 m queda libre para caminar.
- Ciudad.md: el bar y el clandestino frente a frente en el centro; mercado negro en un callejón (los puestos del callejón
  `CrearPuestosCallejon` pueden cambiar a puestos del kit).

## Qué hacer
1. Farolas: cada farola greybox se cambia por una instancia de Poste_Luz con el brazo hacia la calle, conservando el nombre
   "Farola_N" y su `Light` hija en la luminaria (para que CreadorReloj y `LuzNocturna` sigan funcionando igual).
2. Cables: entre postes vecinos del mismo lado de la calle, 2 o 3 cables con comba (LineRenderer o malla delgada sin collider,
   material Hule_Negro), y algunos cruzando la calle a más de 6 m de alto.
3. Un Transformador_Poste en uno de cada tres postes.
4. Puestos de tacos: 2 en la banqueta del bar, 1 en la del clandestino (los dos a más de 6 m de las entradas) y los puestos del
   callejón del mercado negro con el kit. Los botes greybox pasan a Basurero_Grafiti.
5. Azoteas: el "Tanque" greybox pasa a Azotea_AC_Tinaco, y las azoteas pobres ganan tinacos con varilla expuesta (castillos)
   en la mitad de los casos, con el azar fijo del creador.
6. Todo desde el creador (archivo nuevo `Mundo/Editor/VestirCalle.cs` llamado por CreadorBarrio antes de guardar el prefab), con
   la semilla fija; regenera con `CreadorTodo.Crear` y al final rehornea los reflejos (`CreadorAmbiente.HornearReflejos()`, T-079).

## Carpetas permitidas
Assets/_Proyecto/Mundo/, Assets/_Proyecto/Escenas/Ciudad.unity, Assets/_Proyecto/Escenas/Pruebas_Alan.unity,
Assets/_Proyecto/Tests/EditMode/Mundo/ y Assets/_Proyecto/Tests/PlayMode/Mundo/
No tocar: Core/, Assets/Arte/ (las piezas son de T-076), Vehiculos/, Clandestino.unity, las carpetas de Tony.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa (BarrioPruebas, CicloDiaNochePruebas, LugaresPruebas, FinalesCarroPruebas y
      RecorridoCarroPruebas incluidas)
- [ ] PlayMode: hay al menos 30 Poste_Luz y ninguna farola greybox; cada "Farola_N" tiene su `Light` y `LuzNocturna`, apagada a
      las 12:00 y prendida a las 22:00
- [ ] PlayMode: ningún collider del kit queda sobre el asfalto (fuera de las banquetas) ni a menos de 6 m de una `Entrada`
- [ ] PlayMode: en cada lado de manzana que da a una calle, un `CapsuleCast` de radio 0.35 m por la franja de caminar (a 38 m del
      centro de la manzana, de punta a punta, a 1 m de alto) no choca con nada fuera de las puertas de los locales
- [ ] PlayMode: hay al menos 3 puestos de tacos, 1 transformador por cada 3 postes (±1) y cables entre postes vecinos; ningún cable
      cruza la calle a menos de 6 m de alto
- [ ] Medido en el editor (eval): triángulos totales de Barrio.prefab y número de `Light` antes y después, en la bitácora;
      después ≤ 1 500 000 triángulos y sin luces nuevas fuera de las que ya tenían las farolas
- [ ] Capturas de las vistas V1, V2 y V3 a las 22:00 (docs/capturas/T-080-1.png a -3.png) y V1 a las 13:00 (T-080-4.png)
- [ ] Consola sin errores

## Criterios de sensación (para un humano)
- La calle se siente vivida y mexicana: postes chuecos de cables, puesto de tacos con su luz, basura con grafiti.
- Caminar por la banqueta no se atora y el carro no roza nada al ir por el centro de la calle.

## Pruebas automáticas
- PlayMode (Tests/PlayMode/Mundo/): KitCallePruebas (conteos, distancias, franja libre, luces de las farolas).

## Fuera de alcance
- Fachadas (T-081) y letreros de neón (T-082); el bar por dentro (T-083)
- Grafitis en muros, basura suelta, peatones (A-F4 y A-F5)
