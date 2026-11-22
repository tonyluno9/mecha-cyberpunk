# Tareas Unity — alturas de personajes (revisión con Tony, 20-nov)

Decisión final de Tony: PJ01 no se baja (1.84 m). Regla: todos los humanos miden mínimo 1.84 m; la abuela 1.64; robots humanoides 1.94; soldado azul 2.14; robots grandes 3.14.
Vista en `imageassets/blender/_REVISION_TONY/revision_personajes.blend`.

Factor = nueva / hoy (multiplica la escala actual del FBX o del `Visual_*`).

| Personaje | Hoy (m) | Nuevo (m) | Factor |
|---|---|---|---|
| NP12 Abuela | 1.42 | 1.64 | ×1.155 |
| PJ01 | 1.84 | 1.84 | sin cambio |
| woman | 1.80 | 1.84 | ×1.022 |
| woman2 | 1.73 | 1.84 | ×1.064 |
| R01 Mesera robot | 1.65 | 1.84 | ×1.115 |
| NP09 Corredora a / b | 1.61 / 1.63 | 1.84 / 1.85 | ×1.143 / ×1.135 |
| NP14 Oficinista a / b | 1.59 / 1.59 | 1.84 / 1.86 | ×1.157 / ×1.170 |
| NP10 Elotero | 1.51 | 1.84 | ×1.219 |
| Tino (Inventor) | 1.96 | 1.84 | ×0.939 |
| El Canepa (Mecánico) | 1.80 | 1.85 | ×1.028 |
| AL10 / AL11 | 1.79 | 1.86 | ×1.039 |
| Policía (C01) | 1.86 | 1.86 | sin cambio |
| NP13 Repartidor a/b | 1.51 | 1.86 | ×1.232 |
| NP08 Turista a / b | 1.56 / 1.59 | 1.86 | ×1.192 / ×1.170 |
| NP07 Pescador a / b | 1.56 / 1.55 | 1.87 | ×1.199 / ×1.206 |
| SkullHombre | 1.73 | 1.87 | ×1.081 |
| Doc | 1.96 | 1.88 | ×0.959 |
| man (Pandillero armado) | 1.76 | 1.88 | ×1.068 |
| E19 Pandillero Skulls a_2 / b | 1.60 / 1.58 | 1.89 | ×1.181 / ×1.196 |
| E10 Pandillero Snakes | 1.62 | 1.89 | ×1.167 |
| Hacker | 1.80 | 1.90 | ×1.056 |
| NP11 Estibador a / b | 1.61 / 1.66 | 1.90 | ×1.180 / ×1.145 |
| E15 Centinela a / b | 2.37 | 1.94 | ×0.819 |
| GuardiaNova | 2.46 | 1.94 | ×0.789 |
| RobotHombre | 1.80 | 1.94 | ×1.078 |
| E20 Matón Skulls a / b | 1.70 / 1.67 | 1.96 | ×1.153 / ×1.174 |
| E11 Jefe Snakes | 1.78 | 1.98 | ×1.112 |
| E12 Guardia pesado | 1.77 | 2.04 | ×1.153 |
| E18 Élite (azul) | 1.93 | 2.14 | ×1.109 |
| E14 Jefe final | 1.89 | 3.14 | ×1.661 |
| E16 Robot pesado a / b | 2.92 / 3.02 | 3.14 | ×1.075 / ×1.040 |

Sin cambio: perro E17.

## Cómo aplicarlo en Unity
1. Cambiar la escala (globalScale del FBX o escala del `Visual_*`) por el factor de la tabla. Para los Nova, actualizar también `EscalasNova.cs` (hoy E15 2.6, E16 3.5, E18 2.2) y `EscalasHumanos.cs`, y sus pruebas.
2. Tino en `Ciudad_Prologo` tiene escala 1.67 en la instancia: ponerla en 1 y escalar solo el prefab.
3. Ajustar cápsulas y alturas de agentes de IA (`RespuestaNova.AlturaAgente`, NavMesh) para E14 y E16 a 3.14 m.
4. PJ01 no cambia.
