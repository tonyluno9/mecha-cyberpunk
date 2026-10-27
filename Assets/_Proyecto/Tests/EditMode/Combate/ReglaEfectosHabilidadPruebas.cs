using MechaCyberpunk.Combate.Habilidades;
using MechaCyberpunk.Combate.Peleador;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Combate
{
    public class ReglaEfectosHabilidadPruebas
    {
        [Test]
        public void AngulosSalva_Uno_SaleRecto()
        {
            CollectionAssert.AreEqual(new[] { 0f }, ReglaEfectosHabilidad.AngulosSalva(1, 6f));
        }

        [Test]
        public void AngulosSalva_Tres_SeReparteDeMenosAMasDispersion()
        {
            CollectionAssert.AreEqual(new[] { -6f, 0f, 6f }, ReglaEfectosHabilidad.AngulosSalva(3, 6f));
        }

        [Test]
        public void AngulosSalva_CantidadCero_DaUnoRecto()
        {
            Assert.AreEqual(1, ReglaEfectosHabilidad.AngulosSalva(0, 6f).Length);
        }

        [Test]
        public void EnCono_AlFrenteDentroDelAlcance_Si()
        {
            Assert.IsTrue(ReglaEfectosHabilidad.EnCono(Vector3.zero, Vector3.forward, new Vector3(0f, 1f, 3f), 4f, 60f));
        }

        [Test]
        public void EnCono_ANoventaGrados_No()
        {
            Assert.IsFalse(ReglaEfectosHabilidad.EnCono(Vector3.zero, Vector3.forward, new Vector3(3f, 0f, 0f), 4f, 60f));
        }

        [Test]
        public void EnCono_MasLejosQueElAlcance_No()
        {
            Assert.IsFalse(ReglaEfectosHabilidad.EnCono(Vector3.zero, Vector3.forward, new Vector3(0f, 0f, 4.1f), 4f, 60f));
        }

        [Test]
        public void EnCono_EnElBordeDelAngulo_Si()
        {
            Vector3 punto = Quaternion.Euler(0f, 30f, 0f) * Vector3.forward * 2f;
            Assert.IsTrue(ReglaEfectosHabilidad.EnCono(Vector3.zero, Vector3.forward, punto, 4f, 60f));
        }

        [Test]
        public void EnCono_360_CuentaAtras()
        {
            Assert.IsTrue(ReglaEfectosHabilidad.EnCono(Vector3.zero, Vector3.forward, new Vector3(0f, 0f, -2f), 3f, 360f));
        }

        [Test]
        public void AvanceProyectil_NoPasaDelAlcance()
        {
            Assert.AreEqual(1f, ReglaEfectosHabilidad.AvanceProyectil(20f, 0.1f, 24f, 25f), 1e-5f);
            Assert.AreEqual(0f, ReglaEfectosHabilidad.AvanceProyectil(20f, 0.1f, 25f, 25f), 1e-5f);
            Assert.AreEqual(2f, ReglaEfectosHabilidad.AvanceProyectil(20f, 0.1f, 0f, 25f), 1e-5f);
        }

        [Test]
        public void PasoEmbestida_SumaLaDistanciaEnLaDuracion()
        {
            float recorrido = 0f;
            for (int i = 0; i < 3; i++)
            {
                recorrido += ReglaEfectosHabilidad.PasoEmbestida(5f, 0.3f, recorrido, 0.1f);
            }
            Assert.AreEqual(5f, recorrido, 1e-4f);
        }

        [Test]
        public void PasoEmbestida_NoPasaDeLaDistancia()
        {
            Assert.AreEqual(0.5f, ReglaEfectosHabilidad.PasoEmbestida(5f, 0.3f, 4.5f, 0.1f), 1e-5f);
        }

        [Test]
        public void NuevoFin_UnAturdimientoNuevoNoSeSuma()
        {
            float fin = ReglaAturdimiento.NuevoFin(0f, 10f, 1f);
            Assert.AreEqual(11f, fin, 1e-5f);
            Assert.AreEqual(11f, ReglaAturdimiento.NuevoFin(fin, 10.2f, 0.5f), 1e-5f);
            Assert.AreEqual(12f, ReglaAturdimiento.NuevoFin(fin, 10.2f, 1.8f), 1e-5f);
        }
    }
}
