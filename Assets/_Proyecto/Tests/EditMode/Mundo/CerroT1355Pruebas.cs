using System.Collections.Generic;
using System.Linq;
using MechaCyberpunk.Arte.Editor;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-1355: el trazado puro de la Colonia del Cerro: pendiente, radio, uniones U4 y U6 y escalones de azoteas.</summary>
    public class CerroT1355Pruebas
    {
        [Test]
        public void Zigzag_Pendiente_NoPasaDe9PorCiento()
        {
            float peor = CerroT1355Trazado.PendienteMaximaDe(CerroT1355Trazado.Zigzag());
            Assert.LessOrEqual(peor, CerroT1355Trazado.PendienteMaxima, "pendiente máxima " + peor);
            Assert.Greater(peor, 0.05f, "el zigzag debe subir de verdad");
        }

        [Test]
        public void Zigzag_Radio_NoBajaDe25m()
        {
            float menor = CerroT1355Trazado.RadioMinimoDe(CerroT1355Trazado.Zigzag());
            Assert.GreaterOrEqual(menor, CerroT1355Trazado.RadioMinimo, "radio mínimo " + menor);
            Assert.Less(menor, 100f, "debe tener curvas");
        }

        [Test]
        public void Zigzag_PasaPorLaSalidaDelTunel_U6()
        {
            float difY;
            float d = CerroT1355Trazado.DistanciaAPunto(CerroT1355Trazado.Zigzag(), CerroT1355Trazado.PuntoU6Calle, out difY);
            Assert.Less(d, 0.05f);
            Assert.AreEqual(0f, difY, 0.01f);
            d = CerroT1355Trazado.DistanciaAPunto(CerroT1355Trazado.Zigzag(), CerroT1355Trazado.PuntoU6Salida, out difY);
            Assert.Less(d, 0.05f);
            Assert.AreEqual(0f, difY, 0.01f);
        }

        [Test]
        public void Zigzag_SaleDeLaCalleBase_YTerminaEnLaPlazaArriba()
        {
            List<Vector3> zigzag = CerroT1355Trazado.Zigzag();
            Assert.AreEqual(CerroT1355Trazado.XBase, zigzag[0].x, 0.01f);
            Assert.AreEqual(0f, zigzag[0].y, 0.01f);
            float difY;
            Assert.Less(CerroT1355Trazado.DistanciaAPunto(CerroT1355Trazado.CalleBase(), zigzag[0], out difY), 0.01f);
            Vector3 fin = zigzag[zigzag.Count - 1];
            Assert.That(fin.y, Is.InRange(24f, 34f), "la plaza queda arriba (~28 m)");
            Assert.AreEqual(CerroT1355Trazado.AlturaPlaza(), fin.y, 0.1f);
        }

        [Test]
        public void ConexionU4_UneElCorredorConLaCalleBase()
        {
            List<Vector3> u4 = CerroT1355Trazado.ConexionU4();
            float difY;
            Assert.Less(CerroT1355Trazado.DistanciaAPunto(u4, CerroT1355Trazado.PuntoU4Corredor, out difY), 0.01f);
            Assert.AreEqual(0f, difY, 0.01f);
            Assert.Less(CerroT1355Trazado.DistanciaAPunto(u4, CerroT1355Trazado.PuntoU4Base, out difY), 0.01f);
            Assert.AreEqual(0f, difY, 0.01f);
            Assert.Less(CerroT1355Trazado.DistanciaAPunto(CerroT1355Trazado.CalleBase(), CerroT1355Trazado.PuntoU4Base, out difY), 0.01f, "la calle base pasa por (-305, 0, 0)");
            Assert.AreEqual(0f, CerroT1355Trazado.PendienteMaximaDe(u4), 0.0001f);
        }

        [Test]
        public void CalleBase_EsPlana_YCubreDeMenos120A150()
        {
            List<Vector3> b = CerroT1355Trazado.CalleBase();
            Assert.AreEqual(-120f, b.First().z, 0.01f);
            Assert.AreEqual(150f, b.Last().z, 0.01f);
            Assert.IsTrue(b.All(p => Mathf.Approximately(p.y, 0f) && Mathf.Approximately(p.x, CerroT1355Trazado.XBase)));
        }

        [Test]
        public void PlanAzoteas_Escalones_EstanEntre05Y23()
        {
            for (int semilla = 1; semilla <= 40; semilla++)
            {
                System.Random rnd = new System.Random(semilla);
                int n = 30;
                float[] yCalle = new float[n];
                float[] alto = new float[n];
                for (int i = 0; i < n; i++)
                {
                    yCalle[i] = 10f + 0.072f * i * 5.5f;
                    alto[i] = rnd.NextDouble() < 0.5 ? 5.4f : 6.3f;
                }
                float[] e = CerroT1355Trazado.PlanAzoteas(yCalle, alto, semilla, -0.3f, 2.8f);
                float[] saltos = CerroT1355Trazado.Escalones(yCalle, alto, e);
                Assert.AreEqual(n - 1, saltos.Length);
                foreach (float s in saltos)
                {
                    Assert.That(Mathf.Abs(s), Is.InRange(CerroT1355Trazado.EscalonMinimo, CerroT1355Trazado.EscalonMaximo), "semilla " + semilla + " escalón " + s);
                }
                Assert.IsTrue(e.All(v => v >= -0.3f - 0.001f && v <= 2.8f + 0.001f), "e fuera de rango");
            }
        }

        [Test]
        public void PlanAzoteas_CalleBajando_TambienCumple()
        {
            float[] yCalle = Enumerable.Range(0, 20).Select(i => 30f - 0.4f * i).ToArray();
            float[] alto = Enumerable.Repeat(5.6f, 20).ToArray();
            float[] e = CerroT1355Trazado.PlanAzoteas(yCalle, alto, 7, -5f, -1.8f);
            foreach (float s in CerroT1355Trazado.Escalones(yCalle, alto, e))
            {
                Assert.That(Mathf.Abs(s), Is.InRange(CerroT1355Trazado.EscalonMinimo, CerroT1355Trazado.EscalonMaximo));
            }
        }

        [Test]
        public void Terreno_BaseSubeAlOeste_YEsPlanaEnLaCalleBase()
        {
            Assert.AreEqual(0f, CerroT1355Terreno.Base(-305f, 0f), 0.01f);
            Assert.AreEqual(0f, CerroT1355Terreno.Base(-300f, 50f), 0.01f);
            float arriba = CerroT1355Terreno.Base(-450f, 40f);
            Assert.That(arriba, Is.InRange(24f, 34f));
            Assert.Greater(CerroT1355Terreno.Base(-475f, 40f), arriba, "la roca de atrás sigue subiendo");
            Assert.AreEqual(0f, CerroT1355Terreno.Base(-330f, -150f), 0.01f, "plano en la unión U3");
            Assert.Greater(CerroT1355Terreno.Base(-345f, 200f), 15f, "el terreno queda por encima del túnel");
        }

        [Test]
        public void Terreno_UvDeMundo_CuatroMetrosPorRepeticionEnLosTresPlanos()
        {
            Vector3 p = new Vector3(8f, 12f, 20f);
            Assert.AreEqual(new Vector2(2f, 5f), CerroT1355Terreno.UvMundo(p, 1));
            Assert.AreEqual(new Vector2(5f, 3f), CerroT1355Terreno.UvMundo(p, 0));
            Assert.AreEqual(new Vector2(2f, 3f), CerroT1355Terreno.UvMundo(p, 2));
        }

        [Test]
        public void Terreno_Altura_CalleQuedaBajoElAsfalto()
        {
            Vector3[] pts = CerroT1355Trazado.Zigzag().Where(p => Mathf.Abs(p.x - CerroT1355Trazado.XPierna2) < 0.01f && p.z < 100f && p.z > 0f).ToArray();
            List<CerroT1355Terreno.Fuente> fuentes = new List<CerroT1355Terreno.Fuente> { new CerroT1355Terreno.Fuente { Puntos = pts, Semiancho = 7f, Baja = 0.12f } };
            Vector3 p0 = pts[pts.Length / 2];
            float h = CerroT1355Terreno.Altura(p0.x, p0.z, fuentes, new CerroT1355Terreno.Plataforma[0]);
            Assert.AreEqual(p0.y - 0.12f, h, 0.001f);
        }
    }
}
