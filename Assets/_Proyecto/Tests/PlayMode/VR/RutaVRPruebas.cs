using System.Collections.Generic;
using MechaCyberpunk.VR.Ruta;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.VR
{
    /// <summary>VR30: muestreo de la ruta (chevrones cada 2 m hasta 25 m), alfa por distancia, llegada y la malla del chevrón.</summary>
    public class RutaVRPruebas
    {
        [Test]
        public void Muestrear_RutaLarga_CadaDosMetrosHasta25()
        {
            var esquinas = new List<Vector3> { Vector3.zero, new Vector3(0f, 0f, 10f), new Vector3(30f, 0f, 10f) };
            var puntos = new List<PuntoRutaVR>();
            int n = ReglaRutaVR.Muestrear(esquinas, esquinas.Count, 0f, puntos);
            Assert.Greater(n, 5);
            Assert.LessOrEqual(n, ReglaRutaVR.MaximoPuntos);
            for (int i = 1; i < n; i++)
            {
                Assert.AreEqual(ReglaRutaVR.MetrosEntrePuntos, puntos[i].Metros - puntos[i - 1].Metros, 0.001f);
            }
            Assert.LessOrEqual(puntos[n - 1].Metros, ReglaRutaVR.MetrosVisibles);
            Assert.AreEqual(ReglaRutaVR.MetrosInicio, puntos[0].Metros, 0.001f);
            // El que cae después de la esquina va por el segundo tramo, apuntando a +X.
            PuntoRutaVR despues = puntos.Find(p => p.Metros > 11f);
            Assert.AreEqual(10f, despues.Posicion.z, 0.001f);
            Assert.AreEqual(1f, despues.Direccion.x, 0.001f);
        }

        [Test]
        public void Muestrear_RutaCorta_NoPasaDelFinal()
        {
            var esquinas = new List<Vector3> { Vector3.zero, new Vector3(0f, 0f, 6f) };
            var puntos = new List<PuntoRutaVR>();
            ReglaRutaVR.Muestrear(esquinas, esquinas.Count, 0f, puntos);
            Assert.AreEqual(3, puntos.Count); // 1.2, 3.2, 5.2
            Assert.LessOrEqual(puntos[puntos.Count - 1].Posicion.z, 6f);
        }

        [Test]
        public void Muestrear_SinRuta_Nada()
        {
            var puntos = new List<PuntoRutaVR>();
            Assert.AreEqual(0, ReglaRutaVR.Muestrear(new List<Vector3> { Vector3.zero }, 1, 0f, puntos));
            Assert.AreEqual(0, ReglaRutaVR.Muestrear(null, 0, 0f, puntos));
        }

        [Test]
        public void Desfase_AvanzaYVuelveAEmpezar()
        {
            Assert.AreEqual(0f, ReglaRutaVR.Desfase(0f), 0.001f);
            Assert.Greater(ReglaRutaVR.Desfase(1f), 0f);
            Assert.Less(ReglaRutaVR.Desfase(1000f), ReglaRutaVR.MetrosEntrePuntos);
        }

        [Test]
        public void Alfa_SeDesvaneceConLaDistancia()
        {
            Assert.AreEqual(0f, ReglaRutaVR.Alfa(0.5f), 0.001f, "debajo de los pies no");
            Assert.AreEqual(1f, ReglaRutaVR.Alfa(5f), 0.001f);
            Assert.Greater(ReglaRutaVR.Alfa(12f), ReglaRutaVR.Alfa(20f));
            Assert.AreEqual(0f, ReglaRutaVR.Alfa(ReglaRutaVR.MetrosVisibles), 0.001f);
            Assert.AreEqual(0f, ReglaRutaVR.Alfa(30f), 0.001f);
        }

        [Test]
        public void Llego_AMenosDeTresMetros()
        {
            Assert.IsTrue(ReglaRutaVR.Llego(Vector3.zero, new Vector3(2f, 5f, 0f)), "la altura no cuenta");
            Assert.IsFalse(ReglaRutaVR.Llego(Vector3.zero, new Vector3(4f, 0f, 0f)));
        }

        [Test]
        public void Chevron_MallaPlanaHechaPorCodigo()
        {
            Mesh m = RutaObjetivoVR.CrearChevron(0.4f, 0.3f, 0.08f);
            Assert.AreEqual(6, m.vertexCount);
            Assert.AreEqual(12, m.triangles.Length);
            Assert.AreEqual(0f, m.bounds.size.y, 0.0001f);
            Object.DestroyImmediate(m);
        }
    }
}
