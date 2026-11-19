# Pendiente al final: iterar en vivo con Unity abierto (unity-mcp)

Decisión de Tony (18-nov): se deja para el final, para pulir detalles visuales en vivo (luz, materiales, acomodo de zonas).

- Herramienta: MCP for Unity (CoplayDev/unity-mcp). Soporta Unity 2021.3 a 6.x. Necesita Python 3.10+ con `uv`.
- Descargado (código del 17-nov, commit 19b66ec) en `C:\Users\Angel Perez\Downloads\unity-mcp.zip`. NO está instalado en el proyecto.
- Cuando toque:
  1. Que no haya ninguna sesión usando Unity en la carpeta principal.
  2. En Unity: Package Manager > Add from git URL > `https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity#main` (o desde el zip: Add package from disk con `MCPForUnity/package.json`).
  3. Window > MCP for Unity > Configure All Detected Clients (Claude Code).
  4. Claude cambia cosas en el Editor abierto, Tony las ve en Play, y al final se guarda escena/prefab y se sube a git.
- Opcional para código en vivo: Fast Script Reload (gratis) o Hot Reload for Unity (de paga).
- Cuidado: un solo Unity por carpeta; con la memoria de la laptop, el Editor de Tony + una sesión en el worktree es lo máximo.
