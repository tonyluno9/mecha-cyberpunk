using System.Collections.Generic;
using System.Linq;
using MechaCyberpunk.Arte.Editor;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests
{
    /// <summary>T-1357: acomodo puro del Puerto (bloques de contenedores y calles).</summary>
    public class PuertoT1357Pruebas
    {
        private static Rect Caja(ColocarT1357Puerto.Bloque b)
        {
            return new Rect(b.Centro - b.Tam * 0.5f, b.Tam);
        }

        [Test]
        public void Bloques_Todos_QuedanDentroDelRectanguloDelPuerto()
        {
            foreach (ColocarT1357Puerto.Bloque b in ColocarT1357Puerto.Bloques())
            {
                Rect r = Caja(b);
                Assert.GreaterOrEqual(r.xMin, ColocarT1357Puerto.X0);
                Assert.LessOrEqual(r.xMax, ColocarT1357Puerto.X1);
                Assert.GreaterOrEqual(r.yMin, ColocarT1357Puerto.ZSur);
                Assert.LessOrEqual(r.yMax, ColocarT1357Puerto.ZNorte);
            }
        }

        [Test]
        public void Bloques_ParejasVecinas_TienenPasilloDeAlMenos3m()
        {
            List<ColocarT1357Puerto.Bloque> lista = ColocarT1357Puerto.Bloques();
            Assert.Greater(lista.Count, 10);
            for (int i = 0; i < lista.Count; i++)
            {
                for (int j = i + 1; j < lista.Count; j++)
                {
                    Rect a = Caja(lista[i]), b = Caja(lista[j]);
                    float dx = Mathf.Max(0f, Mathf.Max(a.xMin - b.xMax, b.xMin - a.xMax));
                    float dy = Mathf.Max(0f, Mathf.Max(a.yMin - b.yMax, b.yMin - a.yMax));
                    Assert.GreaterOrEqual(Mathf.Sqrt(dx * dx + dy * dy), 3f, "bloques " + i + " y " + j);
                }
            }
        }

        [Test]
        public void Bloques_ConCalles_NoInvadenLaCalzada()
        {
            foreach (ColocarT1357Puerto.Bloque b in ColocarT1357Puerto.Bloques())
            {
                Rect r = Caja(b);
                Vector3[] puntos =
                {
                    new Vector3(r.xMin, 0f, r.yMin), new Vector3(r.xMax, 0f, r.yMax), new Vector3(r.xMin, 0f, r.yMax),
                    new Vector3(r.xMax, 0f, r.yMin), new Vector3(b.Centro.x, 0f, b.Centro.y),
                };
                foreach (Vector3[] calle in ColocarT1357Puerto.Calles())
                {
                    foreach (Vector3 p in puntos)
                    {
                        Assert.GreaterOrEqual(ColocarT1357Puerto.DistanciaACalle(p, calle), ColocarT1357Puerto.AnchoCalle * 0.5f);
                    }
                }
            }
        }

        [Test]
        public void Bloques_Alturas_EscalonanDeUnoEnUnoHastaCuatro()
        {
            foreach (ColocarT1357Puerto.Bloque b in ColocarT1357Puerto.Bloques())
            {
                Assert.AreEqual(1, b.AlturaFila[0]);
                for (int i = 1; i < b.AlturaFila.Length; i++)
                {
                    Assert.That(b.AlturaFila[i] - b.AlturaFila[i - 1], Is.InRange(0, 1));
                    Assert.LessOrEqual(b.AlturaFila[i], 4);
                }
            }
        }

        [Test]
        public void Calles_PasanExactoPorU5YU8()
        {
            List<Vector3[]> calles = ColocarT1357Puerto.Calles();
            Assert.IsTrue(calles.Any(c => ColocarT1357Puerto.DistanciaACalle(ColocarT1357Puerto.U5, c) < 0.001f));
            Assert.IsTrue(calles.Any(c => ColocarT1357Puerto.DistanciaACalle(ColocarT1357Puerto.U8, c) < 0.001f));
        }

        [Test]
        public void Calles_Puntas_UnenConOtraCalleOSonPuerta()
        {
            List<Vector3[]> calles = ColocarT1357Puerto.Calles();
            foreach (Vector3[] c in calles)
            {
                foreach (Vector3 punta in new[] { c[0], c[c.Length - 1] })
                {
                    bool puerta = Vector3.Distance(punta, ColocarT1357Puerto.U5) < 0.01f || Vector3.Distance(punta, ColocarT1357Puerto.U8) < 0.01f;
                    bool union = calles.Any(o => o != c && ColocarT1357Puerto.DistanciaACalle(punta, o) < 0.01f);
                    Assert.IsTrue(puerta || union, "punta sin salida " + punta);
                }
            }
        }

        [Test]
        public void Calles_Puntos_EstanDentroDelRectanguloYAlNivelDelPiso()
        {
            foreach (Vector3 p in ColocarT1357Puerto.Calles().SelectMany(c => c))
            {
                Assert.That(p.x, Is.InRange(ColocarT1357Puerto.X0, ColocarT1357Puerto.X1));
                Assert.That(p.z, Is.InRange(ColocarT1357Puerto.ZSur, ColocarT1357Puerto.ZNorte));
                Assert.AreEqual(0f, p.y);
            }
        }
    }
}
