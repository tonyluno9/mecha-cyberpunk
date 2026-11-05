using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion.Sonido;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Sonido
{
    /// <summary>T-597: el clip (o el tono) de la voz depende del género; si hay clip propio del género se usa.</summary>
    public class VocesT597Pruebas
    {
        private GameObject objeto;

        [TearDown]
        public void Limpiar()
        {
            if (objeto != null) Object.Destroy(objeto);
            Registros.LimpiarTodos();
        }

        private GestorSonidos Crear(params string[] ids)
        {
            Registros.LimpiarTodos();
            CatalogoSonidos catalogo = ScriptableObject.CreateInstance<CatalogoSonidos>();
            List<CatalogoSonidos.Entrada> entradas = new List<CatalogoSonidos.Entrada>();
            foreach (string id in ids)
            {
                entradas.Add(new CatalogoSonidos.Entrada { id = id, finales = new[] { AudioClip.Create("clip_" + id, 8820, 1, 44100, false) } });
            }
            catalogo.Fijar(entradas);
            objeto = new GameObject("GestorSonidos");
            GestorSonidos g = objeto.AddComponent<GestorSonidos>();
            g.Catalogo = catalogo;
            return g;
        }

        [UnityTest]
        public IEnumerator SinVozPropia_LaFemeninaSuenaMasAgudaQueLaMasculina()
        {
            GestorSonidos g = Crear("JUG_DANIO");
            yield return null;
            g.Reproducir("JUG_DANIO@F");
            float tonoF = g.UltimoTono;
            Assert.AreEqual("JUG_DANIO", g.UltimoIdClip);
            yield return new WaitForSecondsRealtime(0.15f);
            g.Reproducir("JUG_DANIO@M");
            float tonoM = g.UltimoTono;
            Assert.Greater(tonoF, 1.1f);
            Assert.Less(tonoM, 1f);
            Assert.Greater(tonoF, tonoM);
            Assert.AreEqual(2, g.Pedidos("JUG_DANIO"));
        }

        [UnityTest]
        public IEnumerator ConVozPropiaDelGenero_SuenaEsaYSinCambiarElTono()
        {
            GestorSonidos g = Crear("JUG_DANIO", "JUG_DANIO_F");
            yield return null;
            g.Reproducir("JUG_DANIO@F");
            Assert.AreEqual("JUG_DANIO_F", g.UltimoIdClip);
            Assert.That(g.UltimoTono, Is.InRange(0.95f, 1.05f));
            yield return new WaitForSecondsRealtime(0.15f);
            g.Reproducir("JUG_DANIO@M");
            Assert.AreEqual("JUG_DANIO", g.UltimoIdClip, "No hay voz masculina propia: usa la normal.");
        }

        [UnityTest]
        public IEnumerator VariosPandillerosCayendo_NoPasanDeTresVoces()
        {
            GestorSonidos g = Crear("PAN_RIVAL_CAE");
            yield return null;
            for (int i = 0; i < 10; i++)
            {
                g.ReproducirEn(i % 2 == 0 ? "PAN_RIVAL_CAE@F" : "PAN_RIVAL_CAE@M", Vector3.zero);
                Assert.LessOrEqual(g.VocesActivas("voz-rival-cae"), 3);
            }
            Assert.AreEqual(10, g.Pedidos("PAN_RIVAL_CAE"));
        }
    }
}
