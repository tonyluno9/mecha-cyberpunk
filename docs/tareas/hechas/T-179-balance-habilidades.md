# T-179 Balance de las habilidades y la munición
Dueño: Tony | Rama: tony/T-179-balance-habilidades | Modelo: Sonnet 5.5, medium
Depende de: T-174, T-178 (y T-172, ya en main)

## Objetivo
Una prueba de EditMode vigila que las habilidades ayuden sin romper el juego: ningún uso tumba a un rival de golpe, la munición cuesta
pero alcanza con lo que paga una pelea, ninguna habilidad es mejor que otra en todo, tech de verdad enfría más rápido, y los jefes con
habilidad siguen siendo ganables con las piezas de su liga y duros sin ellas. Si una regla falla, se ajustan los assets (no las reglas).

## Contexto
- Diseño: economia-misiones.md ("Ganas una pelea pero el costo de reparar el mecha supera el premio: quedas en negativo. Es intencional"),
  mecha-taller.md (tech con cooldown más corto), combate.md (las peleas importantes son en mecha; los brazos son de precisión).
- Decisiones: D-tony-125 (razón = golpes que el rival necesita para tumbarte ÷ golpes que necesitas para tumbarlo; reglas a, b y c), D-tony-98
  (premios), D-tony-120 (bonos de piezas), D-tony-136, D-tony-137 y D-tony-139 (habilidades, munición y jefes). Propuesta nueva: D-tony-140.
- Código y pruebas: `Tests/EditMode/Mecha/BalancePiezasLigasPruebas.cs` (T-172: cómo lee rivales, premios y piezas desde los assets y calcula
  las razones), `Tests/EditMode/Pelea/BalanceLigasPruebas.cs`, `ReglaHabilidad`, `ReglaEstadisticasPiezas.ReduccionEnfriamiento`.

## Qué hacer
1. `Tests/EditMode/Mecha/BalanceHabilidadesPruebas.cs`, todo leído de los assets (nada de números copiados), con estas reglas (D-tony-140):
   a) un uso de cada habilidad que hace daño (MISIL, SALVA sumando sus proyectiles, LANZALLAMAS, MARTILLO, ONDA DE CALOR, EMBESTIDA) de
      nivel L quita como mucho el 35 % de la vida del rival 1 de la liga L y al menos 1.2 × el golpe del mecha de serie;
   b) la munición de una pelea típica (2 usos por ranura, la combinación de ramas más cara) cuesta como mucho el 60 % del premio de ganar del
      rival 1 de la liga 1;
   c) ninguna habilidad domina a otra de su misma ranura en daño, enfriamiento, precio de su munición y alcance a la vez (dominar = igual o
      mejor en las cuatro y mejor en al menos una);
   d) con las 5 piezas tech de nivel L, la habilidad tech de cada ranura (DESCARGA, ESCUDO) tiene el enfriamiento más corto de su ranura
      (comparando con las otras ramas sin piezas tech);
   e) la habilidad del jefe de la liga L quita como mucho el 25 % de la vida del jugador con las 5 piezas de fuego (la rama más frágil) de
      nivel L−1 (con L = 1, las de serie);
   f) razones de D-tony-125 con habilidades: al daño por golpe de cada lado se le suma lo que su habilidad aporta por golpe (daño de la
      habilidad × intervalo entre golpes ÷ enfriamiento; para el jugador, su mejor habilidad de brazo de esa rama y nivel; para el jefe, la
      suya con su factor de enfriamiento). Con el nivel L−1 la razón contra el jefe de la liga L sigue < 1.0, y con el nivel L sigue ≥ 0.7.
   La prueba imprime una tabla por regla (rama, liga, valor y mínimo o máximo) para la bitácora.
2. Si una regla falla: ajusta solo lo que la regla señala, con el creador correspondiente en un paso explícito (D-tony-109): el daño o el
   enfriamiento de la habilidad (Combate), el precio de la munición (Economia) o el `factorEnfriamiento` del jefe (IA), en pasos chicos, y anota
   cada cambio en la bitácora y en D-tony-140. No cambies piezas, rivales ni premios (los vigila T-172 y T-158). Las pruebas de T-174, T-175,
   T-176 y T-178 que leen esos valores se recalculan desde los assets o se actualizan sin bajar lo que comprueban.
3. Anota la tabla final de habilidades, munición y jefes (leída de los assets) en la bitácora de la tarea; T-180 la copia a la revisión.

## Carpetas permitidas
Assets/_Proyecto/Tests/EditMode/Mecha/, y solo si una regla falla: Assets/_Proyecto/Combate/Editor/ y Combate/Datos/Habilidades/ (con su
creador), Assets/_Proyecto/Economia/Editor/ y Economia/Datos/ (munición, con su creador), Assets/_Proyecto/IA/Editor/ e IA/Datos/Ligas/
(`DatosHabilidadRival_*`, con su creador), y las pruebas de Combate, Mecha e IA que lean esos valores.
No tocar: Core/, código del juego fuera de los creadores, piezas, rivales, premios, escenas, todo lo de Alan.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa (salvo los 9 fallos conocidos de Alan en Arena)
- [ ] BalanceHabilidadesPruebas pasa con las reglas a) a f); BalancePiezasLigasPruebas, BalanceLigasPruebas y BalanceEconomiaPruebas siguen en verde
- [ ] La prueba falla si se rompe una regla: el verificador cambia en memoria (no en disco) el daño del MISIL a 100 y la regla a) falla
      (o lo comprueba con un caso de la prueba que use un asset creado en la prueba)
- [ ] Si se cambiaron assets: los creadores corridos dos veces no cambian nada (md5 igual) y cada cambio está anotado con su razón
- [ ] La bitácora tiene las tablas de las reglas y la tabla final de habilidades, munición y jefes

## Criterios de sensación (para un humano)
- Tony: leer las tablas de la bitácora. ¿Te convence que la munición de una pelea típica cueste hasta la mitad del premio del primer rival
  (hay que escoger entre reparar, mejorar y cargar misiles)?

## Pruebas automáticas
- EditMode: BalanceHabilidadesPruebas.

## Fuera de alcance
- Simular peleas completas (las razones son cuentas, como en T-172). Balance de los rivales normales (no usan habilidades).
