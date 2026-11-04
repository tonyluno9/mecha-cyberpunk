using MechaCyberpunk.Trafico.Persecucion;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Trafico
{
    /// <summary>T-587: las reglas puras de la persecución realista (corte de paso, jugador perdido, vista de cámara).</summary>
    public class ReglaVistaPoliciaPruebas
    {
        [Test]
        public void PuntoDeCorte_ConVelocidad_SeAdelantaPorLosSegundos()
        {
            Vector3 punto = ReglaVistaPolicia.PuntoDeCorte(new Vector3(10f, 0.2f, 5f), new Vector3(6f, 3f, 0f), 3f, 3f);
            Assert.AreEqual(28f, punto.x, 0.001f);
            Assert.AreEqual(5f, punto.z, 0.001f);
        }

        [Test]
        public void PuntoDeCorte_MasLentoQueElMinimo_SeQuedaDondeEsta()
        {
            Vector3 origen = new Vector3(10f, 0f, 5f);
            Assert.AreEqual(origen, ReglaVistaPolicia.PuntoDeCorte(origen, new Vector3(1f, 0f, 1f), 3f, 3f));
        }

        [Test]
        public void Enfriamiento_AntesDelUmbral_EsCero_YDespuesBajaPorSegundo()
        {
            Assert.AreEqual(0f, ReglaVistaPolicia.Enfriamiento(5f, 12f, 6f, 0.5f), 0.0001f);
            Assert.AreEqual(3f, ReglaVistaPolicia.Enfriamiento(12f, 12f, 6f, 0.5f), 0.0001f);
            Assert.AreEqual(0f, ReglaVistaPolicia.Enfriamiento(20f, 12f, 6f, 0f), 0.0001f);
        }

        [Test]
        public void Perdido_SoloAlLlegarAlUmbral()
        {
            Assert.IsFalse(ReglaVistaPolicia.Perdido(11.9f, 12f));
            Assert.IsTrue(ReglaVistaPolicia.Perdido(12f, 12f));
        }

        [Test]
        public void LoVe_SinCamara_EsFalso()
        {
            Assert.IsFalse(ReglaVistaPolicia.LoVe(null, Vector3.zero, 100f));
        }

        [Test]
        public void LoVe_DelanteSi_DetrasNo_YMasAllaDelAlcanceNo()
        {
            GameObject objeto = new GameObject("CamaraDePrueba");
            try
            {
                Camera camara = objeto.AddComponent<Camera>();
                camara.fieldOfView = 60f;
                objeto.transform.SetPositionAndRotation(Vector3.zero, Quaternion.LookRotation(Vector3.forward, Vector3.up));
                Assert.IsTrue(ReglaVistaPolicia.LoVe(camara, new Vector3(0f, 0f, 30f), 100f), "Enfrente se ve");
                Assert.IsFalse(ReglaVistaPolicia.LoVe(camara, new Vector3(0f, 0f, -30f), 100f), "Detrás no");
                Assert.IsFalse(ReglaVistaPolicia.LoVe(camara, new Vector3(90f, 0f, 10f), 100f), "A un lado no");
                Assert.IsFalse(ReglaVistaPolicia.LoVe(camara, new Vector3(0f, 0f, 300f), 100f), "Más allá del alcance no");
            }
            finally
            {
                Object.DestroyImmediate(objeto);
            }
        }
    }
}
