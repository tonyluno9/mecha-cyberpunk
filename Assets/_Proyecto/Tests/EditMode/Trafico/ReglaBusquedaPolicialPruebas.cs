using MechaCyberpunk.Trafico.Persecucion;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Trafico
{
    /// <summary>La búsqueda policial (T-665); desde T-729 dejas de ser buscado si nadie te ve durante segundosParaEscapar, estés dentro del círculo o no.</summary>
    public class ReglaBusquedaPolicialPruebas
    {
        private static readonly Vector3 Centro = new Vector3(10f, 0f, 20f);

        [Test]
        public void SinIniciar_NoEscapaNiCuenta()
        {
            ReglaBusquedaPolicial regla = new ReglaBusquedaPolicial(50f, 20f);
            Assert.IsFalse(regla.Activa);
            Assert.IsFalse(regla.Avanzar(Centro + Vector3.right * 500f, false, 100f));
        }

        [Test]
        public void MientrasTeVen_ElCirculoTeSigue_YNoEscapas()
        {
            ReglaBusquedaPolicial regla = new ReglaBusquedaPolicial(50f, 20f);
            regla.Iniciar(Centro);
            Vector3 lejos = Centro + new Vector3(300f, 0f, 0f);
            for (int i = 0; i < 100; i++)
            {
                Assert.IsFalse(regla.Avanzar(lejos, true, 1f), "Mientras te ven no escapas, ni en 100 s");
            }
            Assert.AreEqual(lejos, regla.Centro, "El círculo se centra donde te vieron");
            Assert.IsTrue(regla.Visto);
            Assert.IsFalse(regla.PorPerderte);
        }

        [Test]
        public void SinQueTeVeanNadie_DejasDeSerBuscado_AunDentroDelCirculo()
        {
            ReglaBusquedaPolicial regla = new ReglaBusquedaPolicial(50f, 20f);
            regla.Iniciar(Centro);
            Vector3 dentro = Centro + new Vector3(30f, 0f, 0f);
            Assert.IsFalse(regla.Avanzar(dentro, false, 19f));
            Assert.IsTrue(regla.Activa);
            Assert.IsTrue(regla.Avanzar(dentro, false, 1f), "A los 20 s sin que te vean ya no te buscan, aunque sigas dentro del círculo");
            Assert.IsFalse(regla.Activa, "La búsqueda termina");
        }

        [Test]
        public void SinQueTeVeanNadie_DejasDeSerBuscado_FueraDelCirculo()
        {
            ReglaBusquedaPolicial regla = new ReglaBusquedaPolicial(50f, 20f);
            regla.Iniciar(Centro);
            Vector3 fuera = Centro + new Vector3(80f, 0f, 0f);
            Assert.IsFalse(regla.Avanzar(fuera, false, 19.9f));
            Assert.IsTrue(regla.Avanzar(fuera, false, 0.2f));
        }

        [Test]
        public void SiTeVen_ElContadorSeReinicia()
        {
            ReglaBusquedaPolicial regla = new ReglaBusquedaPolicial(50f, 20f);
            regla.Iniciar(Centro);
            Vector3 fuera = Centro + new Vector3(80f, 0f, 0f);
            regla.Avanzar(fuera, false, 15f);
            Assert.AreEqual(15f, regla.SegundosFuera, 0.0001f);
            regla.Avanzar(fuera, true, 0.1f);
            Assert.AreEqual(0f, regla.SegundosFuera, 0.0001f, "Si te ven se reinicia");
            Assert.IsTrue(regla.Activa);
        }

        [Test]
        public void PorPerderte_SoloEnLosUltimosSegundosSinQueTeVean()
        {
            ReglaBusquedaPolicial regla = new ReglaBusquedaPolicial(50f, 20f);
            regla.Iniciar(Centro);
            regla.Avanzar(Centro, false, 11.9f);
            Assert.IsFalse(regla.PorPerderte, "Aún falta mucho: sin parpadeo");
            regla.Avanzar(Centro, false, 0.2f);
            Assert.IsTrue(regla.PorPerderte, "Faltan 8 s: te están por perder");
            regla.Avanzar(Centro, true, 0.1f);
            Assert.IsFalse(regla.PorPerderte, "Te vuelven a ver: se acabó el aviso");
        }

        [Test]
        public void Recentrar_MueveElCirculoYReiniciaElContador()
        {
            ReglaBusquedaPolicial regla = new ReglaBusquedaPolicial(50f, 20f);
            regla.Iniciar(Centro);
            regla.Avanzar(Centro + new Vector3(80f, 0f, 0f), false, 13f);
            regla.Recentrar(new Vector3(-5f, 0f, 0f));
            Assert.AreEqual(new Vector3(-5f, 0f, 0f), regla.Centro);
            Assert.AreEqual(0f, regla.SegundosFuera, 0.0001f);
            Assert.IsFalse(regla.PorPerderte);
        }
    }
}
