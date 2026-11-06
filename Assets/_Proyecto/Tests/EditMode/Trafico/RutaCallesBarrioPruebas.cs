using MechaCyberpunk.Core;
using MechaCyberpunk.Trafico.Persecucion;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Trafico
{
    /// <summary>T-632 (movidas aquí en T-669): la ruta GPS por las calles del barrio de principio a fin, ahora detrás del contrato IRutaCalles de Core.</summary>
    public class RutaCallesBarrioPruebas
    {
        private readonly RutaCallesBarrio barrio = RutaCallesBarrio.DelBarrio();

        [SetUp]
        public void Preparar() => Registros.LimpiarTodos();

        [Test]
        public void Ruta_DeUnaCalleAOtra_VaPorLasCallesSinCortarManzanas()
        {
            Vector3 origen = new Vector3(-47f, 0f, -60f);
            Vector3 destino = new Vector3(47f, 0f, 60f);
            var ruta = barrio.Calcular(origen, destino);
            Assert.GreaterOrEqual(ruta.Count, 4, "Hay curvas por las esquinas");
            Assert.AreEqual(origen, ruta[0]);
            Assert.AreEqual(destino, ruta[ruta.Count - 1]);
            foreach (Vector3 p in ruta)
            {
                Assert.LessOrEqual(barrio.Calles.DistanciaALaCalle(p), 0.01f, "Todo punto está sobre una calle: " + p);
            }
            for (int i = 1; i < ruta.Count; i++)
            {
                Vector3 d = ruta[i] - ruta[i - 1];
                Assert.IsTrue(Mathf.Abs(d.x) < 0.01f || Mathf.Abs(d.z) < 0.01f, "Cada tramo sigue una calle recta: " + ruta[i - 1] + " a " + ruta[i]);
            }
            Assert.GreaterOrEqual(CalculoRutaCalles.Largo(ruta), Vector3.Distance(origen, destino));
            Assert.LessOrEqual(CalculoRutaCalles.Largo(ruta), 94f + 120f + 60f + 60f + 1f, "Es la ruta corta: 60 m al sur-norte, 94 m al este y 60 m al norte");
        }

        [Test]
        public void Ruta_DentroDeLaMismaCalle_EsUnaLinea()
        {
            var ruta = barrio.Calcular(new Vector3(-47f, 0f, -30f), new Vector3(-47f, 0f, 30f));
            Assert.AreEqual(60f, CalculoRutaCalles.Largo(ruta), 0.5f);
        }

        [Test]
        public void Ruta_OrigenFueraDeLaCalle_EmpiezaConUnTramoALaCalle()
        {
            Vector3 origen = new Vector3(0f, 0f, 20f); // en el centro de una manzana
            var ruta = barrio.Calcular(origen, new Vector3(47f, 0f, 60f));
            Assert.AreEqual(origen, ruta[0]);
            Assert.LessOrEqual(barrio.Calles.DistanciaALaCalle(ruta[1]), 0.01f, "El segundo punto ya está en la calle");
        }
        [Test]
        public void AlArrancar_SeRegistraSola()
        {
            typeof(RutaCallesBarrio).GetMethod("Registrar", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).Invoke(null, null);
            Assert.IsInstanceOf<RutaCallesBarrio>(Registro<IRutaCalles>.Actual, "Trafico registra el trazador de rutas al arrancar (T-669)");
            Registros.LimpiarTodos();
        }

        [Test]
        public void ElBarrio_EsUnaIRutaCalles_ParaQueUILaUseSinVerTrafico()
        {
            IRutaCalles contrato = barrio;
            Registro<IRutaCalles>.Agregar(contrato);
            Assert.AreSame(contrato, Registro<IRutaCalles>.Actual);
            Assert.GreaterOrEqual(Registro<IRutaCalles>.Actual.Calcular(new Vector3(-47f, 0f, -60f), new Vector3(47f, 0f, 60f)).Count, 4);
            Registros.LimpiarTodos();
        }
    }
}
