# T-910 B7: piezas nuevas de calle y casino

Rama: `tony/T-910-b7-piezas-calle`. Modelos de imageassets/modelos3d/ (calle/ y O01) importados con `Assets/Arte/Editor/ImportarT910.cs`
(flujo de T-250: prefab con hijo Visual, URP Lit, pivote en la base). Colocación en `Assets/Arte/Editor/ColocarT910.cs`.

| Pieza | Dónde | Qué se hizo |
| --- | --- | --- |
| ES01 semáforo (4.5 m) | `Arte/Calle/Prefabs/ES01_Semaforo.prefab`; `ColocarS2cKit.Semaforo` | Sustituye al poste N08 con micas. Tres discos sobre las lentes, encendidos de uno en uno por `SemaforoCiclo` (MaterialPropertyBlock) sobre `CalculoSemaforo` (lógica pura) y `ConfigSemaforo_Mundo.asset` (verde 8 s, ámbar 2 s, rojo 10 s). Esquinas con giro distinto en 90° (calle cruzada) van a medio ciclo; las diagonales coinciden. Collider solo en el poste. No toca la IA del tráfico. |
| BS01a-e basura (25 cm) | `ColocarS2cCalle` -> `ColocarT910.Basura` | Reemplaza las copias de bolsas del callejón: 16 piezas sueltas en las banquetas de la calle de prueba, pieza y giro al azar (semilla 9101), sin collider. |
| PS01 pasarela (20 x 8 x 12.5 m) | `ColocarT910.Pasarelas`, raíz `T910_Pasarelas` | Una por cada N02 apagado de Neón Alto, con la cubierta en z 60 y 73.5 (en z 38 y 72, donde estaban los N02, camina la gente de T-510 y las escaleras estorbaban), de pared a pared, con la escalera en cada banqueta. Collider = malla del modelo (peldaños y cubierta). Claro bajo la cubierta mayor de 4.5 m. Los N02 siguen apagados. |
| PC01 puerta (2.2 m) | `ColocarS2oCasino.Puerta` -> `ColocarT910.PuertaCristal` | El FBX es una sola malla: se cortó por triángulos en marco fijo y dos hojas (`PC01_Hojas.asset`, prefab `PC01_PuertaCorrediza`). `HojasCorredizas` desliza cada hoja 0.3 x ancho al lado al abrir. La `HojaPuerta` (E, abrir sola, cerrar, collider) sigue igual pero ya no se dibuja. |
| O01 lata (12 cm) | `CY06_Expendedora.prefab`, hijo `Lata_O01` | En la bandeja de entrega de la máquina. Sin lógica. Las expendedoras de la ciudad (`ColocadorExpendedoras`, UI/) la heredan. |

Escena tocada: `Ciudad_Prologo.unity` (vía `ColocarT910.TodoEnEscena`: Suburbios + calle de prueba, pasarelas, casino). `Ciudad.unity` no se tocó.
Pruebas: EditMode `CalculoSemaforoPruebas`, `PiezasCalleT910Pruebas`; PlayMode `PiezasCalleT910Pruebas`
(capturas con `T910_CAPTURAS=1` en `docs/capturas/t910-*.png`).
