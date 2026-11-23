# Progreso 23-nov noche 2 (T80-T86)
Paso 0: sin cambios nuevos del chat de Blender (los .blend >100 MB de _REVISION_TONY siguen fuera). main al día (3fba87a).
Orden pedido: T81 → T80 → T82 → T84 → T83 → T86 → T85.

| Tarea | Rama | Estado |
|---|---|---|
| T81 | tony/T-1730-camara-lenta | en curso (subagente) |
| T80 | tony/T-1731-aviso-tab | en curso (Opus) |
| T82 | tony/T-1732-npcs-velocidad | en curso (subagente) |
| T84 | tony/T-1733-tino-guiado | en curso (subagente) |
| T83 | tony/T-1734-carros-al-doc | en curso (Opus) |
| T86 | tony/T-1735-sin-numeros-danio | en curso (subagente, junto con T85) |
| T85 | tony/T-1736-campo-holo | en curso (subagente) |

Modo: subagentes en paralelo en la carpeta principal SIN git y SIN Unity, archivos que no se cruzan; Opus compila, prueba en Play
y hace un commit por tarea en su rama (git checkout -b con el árbol sucio, add por ruta, merge --no-ff).
Antes de Play: respaldar partidas de Tony (%USERPROFILE%/AppData/LocalLow/DefaultCompany/CYBERCITY) y restaurarlas al final.
