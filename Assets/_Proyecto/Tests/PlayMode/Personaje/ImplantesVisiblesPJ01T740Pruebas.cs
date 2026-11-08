using System;
using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Visual;
using MechaCyberpunk.UI.Hud;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>
    /// T-740 (A9): las piezas de PJ01_Implantes.fbx se ven según el nivel de cada implante en IImplantes (IMP01 piernas, IMP02 brazos, IMP03 visor/ojos; sistema nervioso no tiene pieza), van en
    /// sincronía con los iconos del HUD y la suela de IMP01 nivel 2 queda a ras del piso. Capturas pj01-implantes-*.png (quieto, caminando, corriendo y apuntando; de frente y de lado) con CAPTURAR_S1=1.
    /// </summary>
    public class ImplantesVisiblesPJ01T740Pruebas : BaseJugadorAPie
    {
        private sealed class ImplantesFalsos : IImplantes
        {
            private readonly int[] niveles = new int[4];
            public int Nivel(EspacioImplante espacio) => niveles[(int)espacio];
            public string Nombre(EspacioImplante espacio) => niveles[(int)espacio] > 0 ? "X" : "";
            public int Cantidad { get { int n = 0; foreach (int v in niveles) { if (v > 0) { n++; } } return n; } }
            public EfectosImplantes Efectos => EfectosImplantes.Ninguno;
            public event Action Cambio;
            public event Action<EspacioImplante, int> Instalado;
            public void Poner(EspacioImplante espacio, int nivel)
            {
                niveles[(int)espacio] = nivel;
                Cambio?.Invoke();
                if (nivel > 0) { Instalado?.Invoke(espacio, nivel); }
            }
        }

        private Animator anim;
        private ImplantesFalsos implantes;
        private ImplantesVisiblesPJ01 visibles;

        private IEnumerator Preparar()
        {
            yield return Calentar();
            VisualPersonaje visual = jugador.GetComponentInChildren<VisualPersonaje>(true);
            anim = visual.Animador;
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            visibles = anim.GetComponentInChildren<ImplantesVisiblesPJ01>(true);
            Assert.IsNotNull(visibles, "El jugador lleva ImplantesVisiblesPJ01");
            foreach (SkinnedMeshRenderer r in visibles.GetComponentsInChildren<SkinnedMeshRenderer>(true)) { r.updateWhenOffscreen = true; }
            implantes = new ImplantesFalsos();
            Registro<IImplantes>.Agregar(implantes);
            yield return Asentar();
        }

        private static IEnumerator Asentar()
        {
            for (int i = 0; i < 6; i++) { yield return null; }
            yield return Esperar(0.2f);
        }

        private string Activas()
        {
            var nombres = new List<string>();
            foreach (ImplantesVisiblesPJ01.Pieza p in visibles.Piezas) { if (p.objeto.activeSelf) { nombres.Add(p.objeto.name); } }
            nombres.Sort();
            return string.Join(",", nombres);
        }

        [UnityTest]
        public IEnumerator SinImplantes_NoSeVeNinguna_ConImplantes_SeVeLaPiezaDeSuNivel_YSistemaNerviosoNoTienePieza()
        {
            yield return Preparar();
            Assert.AreEqual("", Activas(), "Sin implantes no se ve ninguna pieza");
            Assert.AreEqual(6, visibles.Piezas.Length);

            implantes.Poner(EspacioImplante.Piernas, 1);
            yield return Asentar();
            Assert.AreEqual("IMP01_LVL1", Activas());
            implantes.Poner(EspacioImplante.Piernas, 2);
            yield return Asentar();
            Assert.AreEqual("IMP01_LVL2", Activas(), "El nivel 2 reemplaza al 1");
            implantes.Poner(EspacioImplante.Brazos, 1);
            implantes.Poner(EspacioImplante.Ojos, 2);
            yield return Asentar();
            Assert.AreEqual("IMP01_LVL2,IMP02_LVL1,IMP03_LVL2", Activas());
            implantes.Poner(EspacioImplante.Brazos, 2);
            implantes.Poner(EspacioImplante.Ojos, 1);
            implantes.Poner(EspacioImplante.Piernas, 0);
            yield return Asentar();
            Assert.AreEqual("IMP02_LVL2,IMP03_LVL1", Activas());
            implantes.Poner(EspacioImplante.SistemaNervioso, 2);
            yield return Asentar();
            Assert.AreEqual("IMP02_LVL2,IMP03_LVL1", Activas(), "Sistema nervioso no tiene pieza");
            implantes.Poner(EspacioImplante.Brazos, 0);
            implantes.Poner(EspacioImplante.Ojos, 0);
            yield return Asentar();
            Assert.AreEqual("", Activas(), "Al quitarlos se ocultan");
        }

        [UnityTest]
        public IEnumerator ElHud_MarcaLosMismosImplantesQueSeVenEnElCuerpo()
        {
            yield return Preparar();
            var hud = new GameObject("HudImplantesT740").AddComponent<HudImplantes>();
            try
            {
                implantes.Poner(EspacioImplante.Piernas, 2);
                implantes.Poner(EspacioImplante.Brazos, 1);
                yield return Asentar();
                Assert.AreEqual("P2", hud.TextoIcono(EspacioImplante.Piernas));
                Assert.AreEqual("B1", hud.TextoIcono(EspacioImplante.Brazos));
                Assert.AreEqual("", hud.TextoIcono(EspacioImplante.Ojos));
                Assert.AreEqual(2, hud.Visibles);
                Assert.AreEqual("IMP01_LVL2,IMP02_LVL1", Activas(), "El cuerpo muestra lo mismo que el HUD");
                implantes.Poner(EspacioImplante.Ojos, 1);
                implantes.Poner(EspacioImplante.Piernas, 0);
                yield return Asentar();
                Assert.AreEqual("O1", hud.TextoIcono(EspacioImplante.Ojos));
                Assert.AreEqual("", hud.TextoIcono(EspacioImplante.Piernas));
                Assert.AreEqual("IMP02_LVL1,IMP03_LVL1", Activas());
            }
            finally { UnityEngine.Object.Destroy(hud.gameObject); }
        }

        [UnityTest]
        public IEnumerator SuelaDeIMP01Nivel2_QuedaAlRasDelPiso_YLasPiezasSiguenAlCuerpo()
        {
            yield return Preparar();
            implantes.Poner(EspacioImplante.Piernas, 2);
            yield return Asentar();
            SkinnedMeshRenderer exo = visibles.transform.Find("IMP01_LVL2").GetComponent<SkinnedMeshRenderer>();
            SkinnedMeshRenderer cuerpo = null;
            foreach (SkinnedMeshRenderer r in anim.GetComponentsInChildren<SkinnedMeshRenderer>(true)) { if (!r.transform.IsChildOf(visibles.transform) && r.gameObject.activeInHierarchy) { cuerpo = r; } }
            Assert.IsNotNull(cuerpo);
            float suela = MinY(exo);
            float pies = MinY(cuerpo);
            Debug.Log("T740 suela IMP01 LVL2 y=" + suela.ToString("F3") + " pies del cuerpo y=" + pies.ToString("F3") + " piso y=0");
            Assert.AreEqual(pies, suela, 0.03f, "La suela del exoesqueleto queda a ras de los zapatos");
            Assert.AreEqual(0f, suela, 0.06f, "Y a ras del piso (no flota ni se hunde)");
            // Siguen al cuerpo: al caminar la pieza se mueve con las piernas (su caja cambia con la pose).
            Press(teclado.wKey);
            yield return Esperar(0.6f);
            Bounds antes = exo.bounds;
            yield return Esperar(0.35f);
            Assert.AreNotEqual(antes.center, exo.bounds.center, "La pieza va con las piernas al caminar");
            Release(teclado.wKey);
        }

        private static float MinY(SkinnedMeshRenderer r)
        {
            var malla = new Mesh();
            r.BakeMesh(malla);
            Vector3[] v = malla.vertices;
            float min = float.MaxValue;
            Matrix4x4 m = r.transform.localToWorldMatrix;
            foreach (Vector3 p in v) { min = Mathf.Min(min, m.MultiplyPoint3x4(p).y); }
            UnityEngine.Object.Destroy(malla);
            return min;
        }

        private void Fotos(string nombre)
        {
            CapturaS1.Foto("pj01-implantes-" + nombre + "-frente", jugador.transform, new Vector3(0.3f, 1.2f, 3.4f), new Vector3(0f, 0.95f, 0f), 40f, 700, 700);
            CapturaS1.Foto("pj01-implantes-" + nombre + "-lado", jugador.transform, new Vector3(3.4f, 1.1f, 0.2f), new Vector3(0f, 0.95f, 0f), 40f, 700, 700);
        }

        private IEnumerator Estados(string pieza)
        {
            yield return Asentar();
            Fotos(pieza + "-quieto");
            Press(teclado.wKey);
            yield return Esperar(1.0f);
            Fotos(pieza + "-caminando");
            yield return null;
            Press(teclado.leftShiftKey);
            yield return Esperar(1.4f);
            Fotos(pieza + "-corriendo");
            Release(teclado.leftShiftKey);
            Release(teclado.wKey);
            yield return Esperar(0.8f);
            Press(raton.rightButton);
            yield return Esperar(2.0f);
            Fotos(pieza + "-apuntando");
            Release(raton.rightButton);
            yield return Esperar(1.2f);
        }

        [UnityTest]
        public IEnumerator Capturas_CadaPiezaYTodasJuntas_QuietoCaminandoCorriendoYApuntando()
        {
            if (!CapturaS1.Activa) { Assert.Ignore("Solo con CAPTURAR_S1=1"); }
            yield return Preparar();
            Assert.IsTrue(jugador.GetComponent<ArmasAPie>().Equipar(ArmaAPie.Pistola));
            (string nombre, EspacioImplante espacio, int nivel)[] casos =
            {
                ("sin", EspacioImplante.Ojos, 0),
                ("imp01-lvl1", EspacioImplante.Piernas, 1), ("imp01-lvl2", EspacioImplante.Piernas, 2),
                ("imp02-lvl1", EspacioImplante.Brazos, 1), ("imp02-lvl2", EspacioImplante.Brazos, 2),
                ("imp03-lvl1", EspacioImplante.Ojos, 1), ("imp03-lvl2", EspacioImplante.Ojos, 2),
            };
            foreach ((string nombre, EspacioImplante espacio, int nivel) in casos)
            {
                foreach (EspacioImplante e in new[] { EspacioImplante.Ojos, EspacioImplante.Brazos, EspacioImplante.Piernas }) { implantes.Poner(e, 0); }
                implantes.Poner(espacio, nivel);
                yield return Estados(nombre);
            }
            implantes.Poner(EspacioImplante.Piernas, 2);
            implantes.Poner(EspacioImplante.Brazos, 2);
            implantes.Poner(EspacioImplante.Ojos, 2);
            yield return Estados("todos-lvl2");
            implantes.Poner(EspacioImplante.Piernas, 1);
            implantes.Poner(EspacioImplante.Brazos, 1);
            implantes.Poner(EspacioImplante.Ojos, 1);
            yield return Estados("todos-lvl1");
        }
    }
}
