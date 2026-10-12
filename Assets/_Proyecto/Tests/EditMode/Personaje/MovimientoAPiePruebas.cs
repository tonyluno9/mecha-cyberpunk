using MechaCyberpunk.Personaje.Movimiento;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Personaje
{
    public class MovimientoAPiePruebas
    {
        private const float Caminar = 4.5f;
        private const float Correr = 7.5f;
        private const float Aceleracion = 30f;
        private const float Frenado = 40f;
        private const float ControlAire = 0.3f;
        private const float Gravedad = 20f;
        private const float AlturaSalto = 1.1f;
        private const float Dt = 0.02f;

        private static void Horizontal(MovimientoAPie m, Vector2 entrada, bool correr, bool enPiso, float segundos)
        {
            for (float t = 0f; t < segundos; t += Dt)
            {
                m.ActualizarHorizontal(entrada, correr, enPiso, Dt, Caminar, Correr, Aceleracion, Frenado, ControlAire);
            }
        }

        [Test]
        public void Horizontal_CaminarAdelante_LlegaALaVelocidadDeCaminarSinPasarse()
        {
            MovimientoAPie m = new MovimientoAPie();
            float maxima = 0f;
            for (float t = 0f; t < 2f; t += Dt)
            {
                Vector2 v = m.ActualizarHorizontal(Vector2.up, false, true, Dt, Caminar, Correr, Aceleracion, Frenado, ControlAire);
                maxima = Mathf.Max(maxima, v.magnitude);
            }
            Assert.AreEqual(Caminar, m.VelocidadHorizontal.y, 0.0001f);
            Assert.LessOrEqual(maxima, Caminar + 0.0001f);
        }

        [Test]
        public void Horizontal_Correr_LlegaALaVelocidadDeCorrerSinPasarse()
        {
            MovimientoAPie m = new MovimientoAPie();
            float maxima = 0f;
            for (float t = 0f; t < 2f; t += Dt)
            {
                Vector2 v = m.ActualizarHorizontal(Vector2.up, true, true, Dt, Caminar, Correr, Aceleracion, Frenado, ControlAire);
                maxima = Mathf.Max(maxima, v.magnitude);
            }
            Assert.AreEqual(Correr, m.VelocidadHorizontal.y, 0.0001f);
            Assert.LessOrEqual(maxima, Correr + 0.0001f);
        }

        [Test]
        public void Horizontal_Diagonal_NoVaMasRapidoQueRecto()
        {
            MovimientoAPie m = new MovimientoAPie();
            Horizontal(m, new Vector2(1f, 1f), false, true, 2f);
            Assert.AreEqual(Caminar, m.VelocidadHorizontal.magnitude, 0.0001f);
        }

        [Test]
        public void Horizontal_AlSoltar_FrenaACeroSinInvertir()
        {
            MovimientoAPie m = new MovimientoAPie();
            Horizontal(m, Vector2.up, true, true, 2f);
            float anterior = m.VelocidadHorizontal.y;
            for (float t = 0f; t < 1f; t += Dt)
            {
                m.ActualizarHorizontal(Vector2.zero, false, true, Dt, Caminar, Correr, Aceleracion, Frenado, ControlAire);
                Assert.GreaterOrEqual(m.VelocidadHorizontal.y, 0f);
                Assert.LessOrEqual(m.VelocidadHorizontal.y, anterior);
                anterior = m.VelocidadHorizontal.y;
            }
            Assert.AreEqual(0f, m.VelocidadHorizontal.magnitude, 0.0001f);
        }

        [Test]
        public void Horizontal_EnElAire_AceleraMasLento()
        {
            MovimientoAPie piso = new MovimientoAPie();
            MovimientoAPie aire = new MovimientoAPie();
            Horizontal(piso, Vector2.up, false, true, 0.1f);
            Horizontal(aire, Vector2.up, false, false, 0.1f);
            Assert.Less(aire.VelocidadHorizontal.y, piso.VelocidadHorizontal.y);
            Assert.Greater(aire.VelocidadHorizontal.y, 0f);
        }

        [Test]
        public void Vertical_EnElPisoSinSaltar_NoSaleDelPisoYBajaPoco()
        {
            MovimientoAPie m = new MovimientoAPie();
            float desplazamiento = m.ActualizarVertical(true, false, Dt, AlturaSalto, Gravedad);
            Assert.AreEqual(MovimientoAPie.VelocidadVerticalEnPiso * Dt, desplazamiento, 0.0001f);
            Assert.Less(desplazamiento, 0f);
        }

        [Test]
        public void Vertical_SaltarEnElPiso_AlcanzaLaAlturaDeLosDatos()
        {
            MovimientoAPie m = new MovimientoAPie();
            float altura = 0f;
            float maxima = 0f;
            altura += m.ActualizarVertical(true, true, Dt, AlturaSalto, Gravedad);
            maxima = Mathf.Max(maxima, altura);
            while (altura > 0f)
            {
                altura += m.ActualizarVertical(false, false, Dt, AlturaSalto, Gravedad);
                maxima = Mathf.Max(maxima, altura);
            }
            Assert.AreEqual(AlturaSalto, maxima, AlturaSalto * 0.05f);
        }

        [Test]
        public void Vertical_SaltarEnElAire_NoHaceNada()
        {
            MovimientoAPie m = new MovimientoAPie();
            m.ActualizarVertical(true, true, Dt, AlturaSalto, Gravedad);
            m.ActualizarVertical(false, false, Dt, AlturaSalto, Gravedad);
            float antes = m.VelocidadVertical;
            m.ActualizarVertical(false, true, Dt, AlturaSalto, Gravedad);
            Assert.AreEqual(antes - Gravedad * Dt, m.VelocidadVertical, 0.0001f);
        }

        [Test]
        public void Vertical_AlSaltarMasAlto_ElSaltoCalculadoSubeConLosDatos()
        {
            Assert.AreEqual(MovimientoAPie.VelocidadInicialSalto(1.1f, Gravedad) * 2f,
                MovimientoAPie.VelocidadInicialSalto(4.4f, Gravedad), 0.0001f);
        }

        [Test]
        public void Reiniciar_DespuesDeCorrerYSaltar_DejaTodoEnCero()
        {
            MovimientoAPie m = new MovimientoAPie();
            Horizontal(m, Vector2.up, true, true, 1f);
            m.ActualizarVertical(true, true, Dt, AlturaSalto, Gravedad);
            m.Reiniciar();
            Assert.AreEqual(Vector2.zero, m.VelocidadHorizontal);
            Assert.AreEqual(0f, m.VelocidadVertical);
        }
    }
}
