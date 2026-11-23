# Reglas para los subagentes de la sesión B (noche del 23-nov)
- Trabajas SOLO en `C:/Users/Angel Perez/Desktop/videojuego/mecha-cyberpunk/.sesiones/s2` (worktree de git, Unity 6000.3, URP, Input System).
  No uses git (ni add, ni commit, ni checkout, ni stash). Otros subagentes editan otros archivos del mismo árbol a la vez: toca solo lo de tu tarea.
- NO abras ni corras Unity (ni batchmode). El orquestador compila y prueba. Escribe C# que compile a la primera: revisa namespaces, asmdef
  (`MechaCyberpunk.<Área>`; áreas de dueños distintos no se referencian; usa los contratos de Core/) y firmas reales leyendo el código.
- NO edites escenas de la ciudad (Ciudad*.unity, Clandestino, zonas) ni YAML de escenas/prefabs/controllers/assets a mano. Si hace falta cambiar
  un prefab, un AnimatorController, un override o un ScriptableObject, escribe un método estático de editor
  `public static void Aplicar()` (en una carpeta Editor/ del área, con [MenuItem] opcional) que lo haga por código; el orquestador lo corre con -executeMethod.
  Lo que haya que mover en el MAPA no lo haces: descríbelo en tu informe (qué, dónde, por qué) para docs/para-sesion-A.md.
- No edites FBX ni clips de Blender (solo importación por código, controladores, overrides y código). No toques PROMPTS_ASSETS.md ni registro.csv. No corras CreadorTodo.
- Convenciones de C#: .claude/rules/unity-csharp.md. Código y comentarios en español como el resto.
- Pruebas: solo las necesarias, una clase EditMode o PlayMode pequeña por tarea si aporta (en Tests/EditMode/<Área>/ o PlayMode/<Área>/).
- Sonidos: los IDs nuevos se agregan al catálogo y a `ReglaMezclaSonidos` si aplica (búscalos con grep).
- Herramientas de captura en vivo que existen: Assets/_Proyecto/Progresion/Prologo/Editor/Capturas20nB.cs.
- Al terminar, tu informe final DEBE traer, por tarea: (1) lista EXACTA de archivos creados/cambiados (rutas desde la raíz del repo),
  (2) métodos Aplicar() a correr y en qué orden, (3) clases de prueba nuevas o a correr, (4) qué comprobar en Play y cómo (posición/escena,
  métodos de Capturas20nB o de un script de captura que dejes), (5) lo que debe hacer la sesión A en el mapa, (6) dudas o lo que no quedó.
