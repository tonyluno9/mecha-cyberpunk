using System.Collections.Generic;
using MechaCyberpunk.UI;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.UI
{
    public class CalculoPiezasArmaPruebas
    {
        // Triángulo pequeño en (x, y, z).
        private static void Triangulo(List<Vector3> v, List<int> t, float x, float y, float z)
        {
            int i = v.Count;
            v.Add(new Vector3(x, y, z));
            v.Add(new Vector3(x + 0.01f, y, z));
            v.Add(new Vector3(x, y + 0.01f, z));
            t.Add(i);
            t.Add(i + 1);
            t.Add(i + 2);
        }

        // Largo 1 m, alto 0.3 m: culata (0), cuerpo (1), cargador colgando (2) y boca (3).
        private static ParteMalla[] ArmaDeEjemplo(float signo, bool largoEnX)
        {
            var v = new List<Vector3>();
            var t = new List<int>();
            float[] s = { 0.05f, 0.45f, 0.45f, 0.95f };
            float[] y = { 0.15f, 0.25f, 0.02f, 0.2f };
            for (int i = 0; i < s.Length; i++)
            {
                float largo = signo * (s[i] - 0.5f);
                Triangulo(v, t, largoEnX ? largo : 0f, y[i], largoEnX ? 0f : largo);
            }
            Vector3 boca = largoEnX ? new Vector3(signo * 0.5f, 0.2f, 0f) : new Vector3(0f, 0.2f, signo * 0.5f);
            return CalculoPiezasArma.Repartir(v.ToArray(), t.ToArray(), boca);
        }

        [TestCase(1f, true)]
        [TestCase(-1f, true)]
        [TestCase(1f, false)]
        [TestCase(-1f, false)]
        public void Repartir_ArmaEnCualquierOrientacion_SeparaCanonCargadorYResto(float signo, bool largoEnX)
        {
            ParteMalla[] partes = ArmaDeEjemplo(signo, largoEnX);
            Assert.AreEqual(ParteMalla.Resto, partes[0], "La culata queda en el resto");
            Assert.AreEqual(ParteMalla.Resto, partes[1], "El cuerpo central queda en el resto");
            Assert.AreEqual(ParteMalla.Cargador, partes[2], "Lo que cuelga en el medio es el cargador");
            Assert.AreEqual(ParteMalla.Canon, partes[3], "La boca es el cañón");
        }

        [Test]
        public void Repartir_SinTriangulos_DevuelveVacio()
        {
            Assert.AreEqual(0, CalculoPiezasArma.Repartir(new Vector3[0], new int[0], Vector3.zero).Length);
        }
    }
}
