using MechaCyberpunk.Trafico.Persecucion;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Trafico
{
    public class ReglaBusquedaPolicialPruebas
    {
        private static readonly Vector3 Centro = new Vector3(10f, 0f, 20f);

        [Test]
        public void SinIniciar_NoEscapaNiCuenta()
        {
            ReglaBusquedaPolicial regla = new ReglaBusquedaPolicial(50f, 5f);
            Assert.IsFalse(regla.Activa);
            Assert.IsFalse(regla.Avanzar(Centro + Vector3.right * 500f, false, 100f));
        }

        [Test]
        public void MientrasTeVen_ElCirculoTeSigue_YNoEscapas()
        {
            ReglaBusquedaPolicial regla = new ReglaBusquedaPolicial(50f, 5f);
            regla.Iniciar(Centro);
            Vector3 lejos = Centro + new Vector3(300f, 0f, 0f);
            Assert.IsFalse(regla.Avanzar(lejos, true, 10f), "Aunque estés fuera, si te ven no escapas");
            Assert.AreEqual(lejos, regla.Centro, "El círculo se centra donde te vieron");
            Assert.IsTrue(regla.Visto);
        }

        [Test]
        public void SinVerteYDentroDelCirculo_NuncaEscapas()
        {
            ReglaBusquedaPolicial regla = new ReglaBusquedaPolicial(50f, 5f);
            regla.Iniciar(Centro);
            for (int i = 0; i < 100; i++)
            {
                Assert.IsFalse(regla.Avanzar(Centro + new Vector3(30f, 0f, 0f), false, 1f));
            }
            Assert.IsTrue(regla.Activa, "Los 100 s dentro del círculo no bastan");
            Assert.AreEqual(0f, regla.SegundosFuera, 0.0001f);
        }

        [Test]
        public void FueraDeLaVistaYDelCirculo_EscapasTrasLosSegundos()
        {
            ReglaBusquedaPolicial regla = new ReglaBusquedaPolicial(50f, 5f);
            regla.Iniciar(Centro);
            Vector3 fuera = Centro + new Vector3(80f, 0f, 0f);
            Assert.IsFalse(regla.Avanzar(fuera, false, 4f));
            Assert.IsTrue(regla.Avanzar(fuera, false, 1f), "A los 5 s escapas");
            Assert.IsFalse(regla.Activa, "La búsqueda termina");
        }

        [Test]
        public void SiVuelvesAlCirculoOTeVen_ElContadorSeReinicia()
        {
            ReglaBusquedaPolicial regla = new ReglaBusquedaPolicial(50f, 5f);
            regla.Iniciar(Centro);
            Vector3 fuera = Centro + new Vector3(80f, 0f, 0f);
            regla.Avanzar(fuera, false, 4f);
            regla.Avanzar(Centro, false, 0.1f);
            Assert.AreEqual(0f, regla.SegundosFuera, 0.0001f, "Dentro del círculo se reinicia");
            regla.Avanzar(fuera, false, 4f);
            regla.Avanzar(fuera, true, 0.1f);
            Assert.AreEqual(0f, regla.SegundosFuera, 0.0001f, "Si te ven se reinicia");
            Assert.IsTrue(regla.Activa);
        }

        [Test]
        public void Recentrar_MueveElCirculoYReiniciaElContador()
        {
            ReglaBusquedaPolicial regla = new ReglaBusquedaPolicial(50f, 5f);
            regla.Iniciar(Centro);
            regla.Avanzar(Centro + new Vector3(80f, 0f, 0f), false, 3f);
            regla.Recentrar(new Vector3(-5f, 0f, 0f));
            Assert.AreEqual(new Vector3(-5f, 0f, 0f), regla.Centro);
            Assert.AreEqual(0f, regla.SegundosFuera, 0.0001f);
        }
    }
}
