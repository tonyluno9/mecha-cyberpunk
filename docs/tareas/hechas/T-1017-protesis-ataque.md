# T-1017: prótesis de ataque del Doc (playtest 16-nov, punto 17)

- **Brazo láser** ([4], Ƀ2,500): mientras dura (4 s, recarga 16 s) el brazo derecho dispara un rayo rojo hacia la mira que daña 80/s a todo
  lo que toca; atraviesa a la gente y para en las paredes.
- **Hombrera de misiles** ([5], Ƀ3,500): lanza 4 misiles teledirigidos (uno cada 0.15 s, recarga 22 s) a los enemigos más cercanos al
  frente (o a la mira); cada uno explota en área (90 de daño, 3.5 m) con `ExplosionAPie` de T-1013.
- Se compran con el Doc: en la clínica, **[TAB] PRÓTESIS DE ATAQUE** abre la pantalla de tienda con las dos; Enter compra y Esc vuelve a
  la clínica. Son objetos del inventario (tipo `Protesis`, máximo 1, en CatalogoObjetos_Base: se guardan con la partida).
- Se usan desde la barra M31 (T-1011): sus casillas salen solo cuando las tienes.
- Valores en `ConfigHabilidadesProtesis`; datos con `ProtesisAtaqueT1017.Crear`; teclas 4 y 5 en ControlesAPie.inputactions.
- Pruebas: ProtesisAtaqueT1017Pruebas (3), ClinicaAtaqueT1017Pruebas, ProtesisAtaqueT1017EditPruebas (2), y T992, Clinica, ImplantesDoc
  siguen pasando.
- Capturas revisadas: `docs/capturas/t1017/`.
- Falta: los modelos IMP04/IMP05 puestos en el cuerpo (hoy se ve el rayo y los misiles, no la prótesis en el brazo ni en el hombro).
