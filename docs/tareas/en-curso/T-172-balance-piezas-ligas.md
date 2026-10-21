# T-172 Balance de las piezas contra las ligas y reajuste de las ligas 3 y 4
Dueño: Tony | Rama: tony/T-172-balance-piezas | Modelo: Sonnet 5.5, medium
Depende de: T-166, T-171

## Objetivo
Una prueba vigila que las piezas y las ligas encajen: con las piezas de tu liga se le puede ganar al jefe, con las de la liga anterior no
(hay que mejorar), lo que cuestan las piezas se junta peleando en la liga anterior, y ninguna rama queda inútil. Con eso se cierra el
pendiente de D-tony-97 ("las ligas 3 y 4 están pensadas para un mecha con piezas y se reajustan entonces"): los rivales de esas ligas se
ajustan solo si alguna regla falla.

## Contexto
- Diseño: personaje-progresion.md ("Cada una paga más y tiene rivales más duros"), mecha-taller.md (4 niveles desbloqueados con las 4 ligas).
- Decisiones: D-tony-97 (los 20 rivales y sus reglas), D-tony-98 (premios por liga), D-tony-109 y D-tony-110 (los creadores no pisan assets
  existentes), D-tony-120 (bonos y precios de las piezas). Propuesta nueva: D-tony-125 (reglas de balance de abajo).
- Datos: rivales en `IA/Datos/Ligas/DatosMecha_Liga<L>_<n>.asset` (tabla en docs/revision-tony.md, T-157), calendario
  `Pelea/Datos/CalendarioLigas_Base.asset`, piezas `Mecha/Datos/Piezas/` y `ReglaEstadisticasPiezas` (T-166).
- Ejemplos: `Tests/EditMode/Pelea/BalanceLigasPruebas.cs` (reglas con letra y mensaje que explica), `Tests/EditMode/Economia/BalanceEconomiaPruebas.cs`.
- Cuenta de referencia (hecha al planear, con la tabla de T-166 y los rivales de hoy): todas las reglas de abajo ya pasan; la más justa es
  fuego IV contra EL CACIQUE (0.82). Si al implementar algo falla, se ajusta el rival (o el bono), no la regla.

## Qué hacer
1. `Tests/EditMode/Mecha/BalancePiezasLigasPruebas.cs` (lee los assets; los números nunca escritos a mano). Definiciones:
   - "mecha de nivel N de una rama": las 5 piezas de esa rama de nivel N (nivel 0 = las de serie), compuesto con `ReglaEstadisticasPiezas`;
   - "razón" contra un rival = golpes que el rival necesita para tumbarte (`ceil(tu vida / su daño)`) ÷ golpes que tú necesitas para tumbarlo
     (`ceil(su vida / tu daño)`). Mayor que 1: vas con ventaja (sin contar bloqueo, ritmo ni furia).
   Reglas, para cada rama y cada liga L de 1 a 4 (el mensaje dice rama, liga, valores y regla):
   a) nivel L−1 contra el rival 1 de la liga L: razón ≥ 1.0 (puedes empezar la liga sin mejorar);
   b) nivel L−1 contra el jefe de la liga L: razón < 1.0 (para el jefe hay que mejorar);
   c) nivel L contra el jefe de la liga L: razón ≥ 0.8 (con las piezas de tu liga el jefe se puede ganar);
   d) precio de una pieza de nivel I ≤ premio de ganar del rival 1 de la liga 1;
   e) para L de 2 a 4: 5 × precio de nivel L ≤ 0.8 × suma de los premios de ganar de los 5 carteles de la liga L−1;
   f) con el mismo nivel, ninguna rama es peor que otra en todo a la vez (menos vida, menos daño, menos velocidad y recarga igual o más
      lenta): ninguna queda inútil. Fuego tiene menos vida y daño que militar, pero corre más y golpea más seguido.
2. Corre la prueba. Si alguna regla falla, ajusta los valores de los rivales de las ligas 3 y 4 (o un bono de la tabla de T-166 si el
   problema es de una rama) con un paso explícito del creador (`IA/Editor/CreadorRivalesLiga.cs` o `Mecha/Editor/CreadorPiezas.cs`) que
   escribe solo los valores cambiados, y vuelve a correr también BalanceLigasPruebas y las reglas de los rivales de T-157 (deben seguir en verde).
3. Imprime en la bitácora la tabla de razones (rama × liga × a/b/c) y anota cada valor que cambiaste (antes → después) y por qué.

## Carpetas permitidas
Assets/_Proyecto/Tests/EditMode/Mecha/, Assets/_Proyecto/IA/Editor/ y IA/Datos/Ligas/ (solo con el creador, solo ligas 3 y 4, solo si una
regla falla), Assets/_Proyecto/Mecha/Editor/ y Mecha/Datos/Piezas/ (solo con el creador, solo si una regla falla)
No tocar: Core/, código del juego, premios y momios (T-158; si una regla de dinero falla, se anota y se pide decisión a Tony), escenas,
todo lo de Alan.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa (salvo los 9 fallos conocidos de Alan en Arena)
- [ ] BalancePiezasLigasPruebas pasa con sus 6 reglas; BalanceLigasPruebas, las reglas de rivales de T-157 y BalanceEconomiaPruebas siguen en verde
- [ ] La bitácora tiene la tabla de razones y la lista de valores cambiados (o "ninguno")
- [ ] Si se cambió un asset, solo cambiaron los valores anotados (`git diff` de los `.asset`) y el creador corrido otra vez no cambia nada

## Criterios de sensación (para un humano)
- Pruebas_Economia con `LigasDePrueba` (L) hasta la liga 3: con mecha de nivel II, ¿VOLTIO se le gana y EL ALCALDE se siente casi imposible
  hasta mejorar? Con nivel III, ¿EL ALCALDE se le gana peleando bien?

## Pruebas automáticas
- EditMode: BalancePiezasLigasPruebas.

## Fuera de alcance
- Simular peleas completas para balancear (el ritmo, el bloqueo y la furia quedan para la prueba humana). Cambiar premios, momios o renta.
