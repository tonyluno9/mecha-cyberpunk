# T-1332: NPCs platican de frente, reciben daño, animaciones

Rama tony/T-1332-npcs. No existe docs/playtest-17nov-b.md en el repo; se siguió el encargo.

## Hecho
- GenteDeBanqueta (modo Platicar): cada uno busca a la pareja Platicar más cercana (2.5 m) y gira hacia ella (PlaticarCon la fija a mano). Sin escena que tocar.
- VidaGente (nuevo, Personaje/Visual): IRecibeDanio (IRecibeDanio de Core, sin referencias entre áreas), 40 de vida, collider de cápsula, muere cayendo hacia atrás y queda tirada. GenteDeBanqueta la agrega sola en Awake si no hay otro IRecibeDanio. Cubre balas, golpes y explosiones (todo llega por IRecibeDanio).
- Ya recibían daño y mueren tirados: PeatonMundo (Trafico, incluye atropello por su lógica) y CivilAcosado (huye). No se cambiaron.
- No se requiere método Aplicar(): no cambia escenas, prefabs ni animators.

## Clips de NPC encontrados (Assets/Arte/Personajes/Animaciones)
- Locomocion: Loco_Idle, Loco_Caminar, Loco_Correr (+ variantes). Interaccion: Inter_VerTelefono_Bucle, Inter_Telefono (ya usados por Celular). Muerte: Muerte_Adelante/Atras/Desmayo. Dano: Dano_Frente/Espalda/Fuerte. Nova_*, Jefe_*, Skull_* (específicos de personaje).
- De los modelos Tripo: AcuerdoCompleto y Enojo (CreadorVisualTripo.ClipAcuerdo/ClipEnojo), ya usados por Platicar.
- NO existen NPC_Platicar1/2, NPC_Aterrado, NPC_Huir, NPC_MirarAlrededor (pendientes en docs/animaciones-faltan.md; no hay carpeta NPC/ ni imageassets/animaciones).
- Usados: ninguno nuevo. La muerte es una caída procedural; Muerte_Desmayo podría engancharse cuando se quiera (hay que cargarlo desde editor). La variedad de NPCs espera assets.
