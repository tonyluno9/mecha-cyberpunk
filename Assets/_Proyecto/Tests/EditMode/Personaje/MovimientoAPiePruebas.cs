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

        /// <summary>
        /// T-1550 (decisión de Tony del 20-nov): el impulso del salto sale 0.1 s (3 cuadros) después de pedirlo, así que las pruebas viejas, que esperaban el impulso en el mismo cuadro,
        /// siguen pidiendo el salto y esperan a que salga. Devuelve cuánto subió en el cuadro del despegue (los cuadros de la agachada se quedan en el piso).
        /// </summary>
        private static float Despegar(MovimientoAPie m, out int cuadros)
        {
            float altura = m.ActualizarVertical(true, true, Dt, AlturaSalto, Gravedad);
            cuadros = 1;
            while (!m.DespegoEsteFrame && cuadros < 100)
            {
                altura = m.ActualizarVertical(true, false, Dt, AlturaSalto, Gravedad);
                cuadros++;
            }
            return altura;
        }

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
            altura += Despegar(m, out _); // T-1550: el impulso sale 0.1 s después de pedir el salto
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
            Despegar(m, out _); // T-1550: el impulso sale 0.1 s después de pedir el salto
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

        // ---------- T-1550: el impulso del salto sale 3 cuadros (0.1 s) después de pedirlo ----------

        [Test]
        public void Salto_ElImpulsoSaleTresCuadrosDespuesDePedirlo_YMientrasTantoSigueEnElPiso()
        {
            Assert.AreEqual(0.1f, MovimientoAPie.SegundosRetardoSalto, 1e-6f, "3 cuadros a 30 fps");
            MovimientoAPie m = new MovimientoAPie();
            float desplazamiento = m.ActualizarVertical(true, true, 0.03f, AlturaSalto, Gravedad);
            Assert.IsTrue(m.PreparandoSalto, "Al pedirlo empieza la agachada");
            Assert.IsFalse(m.DespegoEsteFrame);
            Assert.AreEqual(MovimientoAPie.VelocidadVerticalEnPiso * 0.03f, desplazamiento, 1e-5f, "Sigue pegado al piso");
            m.ActualizarVertical(true, false, 0.03f, AlturaSalto, Gravedad);
            m.ActualizarVertical(true, false, 0.03f, AlturaSalto, Gravedad);
            Assert.IsTrue(m.PreparandoSalto, "A los 0.09 s todavía no sale");
            Assert.IsFalse(m.DespegoEsteFrame);
            float sube = m.ActualizarVertical(true, false, 0.03f, AlturaSalto, Gravedad);
            Assert.IsTrue(m.DespegoEsteFrame, "A los 0.12 s (pasados 0.1) sale el impulso");
            Assert.IsFalse(m.PreparandoSalto);
            Assert.Greater(sube, 0f);
            m.ActualizarVertical(false, false, 0.03f, AlturaSalto, Gravedad);
            Assert.IsFalse(m.DespegoEsteFrame, "Solo cuenta en el cuadro del impulso");
        }

        [Test]
        public void Salto_SinRetardo_SaleEnElMismoCuadro()
        {
            MovimientoAPie m = new MovimientoAPie { RetardoSalto = 0f };
            float sube = m.ActualizarVertical(true, true, Dt, AlturaSalto, Gravedad);
            Assert.IsTrue(m.DespegoEsteFrame);
            Assert.Greater(sube, 0f);
        }

        [Test]
        public void Salto_SiDejaElPisoAntesDelImpulso_SeCancela()
        {
            MovimientoAPie m = new MovimientoAPie();
            m.ActualizarVertical(true, true, Dt, AlturaSalto, Gravedad);
            Assert.IsTrue(m.PreparandoSalto);
            m.ActualizarVertical(false, false, Dt, AlturaSalto, Gravedad);
            Assert.IsFalse(m.PreparandoSalto, "Si se cae del borde en la agachada, no salta en el aire");
            for (int i = 0; i < 20; i++)
            {
                m.ActualizarVertical(true, false, Dt, AlturaSalto, Gravedad);
                Assert.IsFalse(m.DespegoEsteFrame);
            }
        }

        [Test]
        public void Salto_PedirloDeNuevoEnLaAgachada_NoReiniciaLaEspera()
        {
            MovimientoAPie m = new MovimientoAPie();
            Despegar(m, out int cuadrosSolo);
            MovimientoAPie otro = new MovimientoAPie();
            otro.ActualizarVertical(true, true, Dt, AlturaSalto, Gravedad);
            int cuadros = 1;
            while (!otro.DespegoEsteFrame && cuadros < 100)
            {
                otro.ActualizarVertical(true, true, Dt, AlturaSalto, Gravedad); // sigue pulsando espacio
                cuadros++;
            }
            Assert.AreEqual(cuadrosSolo, cuadros, "Pulsar de nuevo durante la agachada no la alarga");
            Assert.AreEqual(Mathf.CeilToInt(MovimientoAPie.SegundosRetardoSalto / Dt), cuadrosSolo, 1, "0.1 s a 50 cuadros por segundo");
        }

        [Test]
        public void Salto_ConRetardo_LlegaALaMismaAltura()
        {
            MovimientoAPie m = new MovimientoAPie();
            float altura = Despegar(m, out _);
            float maxima = altura;
            while (altura > 0f)
            {
                altura += m.ActualizarVertical(false, false, Dt, AlturaSalto, Gravedad);
                maxima = Mathf.Max(maxima, altura);
            }
            Assert.AreEqual(AlturaSalto, maxima, AlturaSalto * 0.05f, "El retardo no cambia la altura");
        }

        [Test]
        public void Reiniciar_DespuesDeCorrerYSaltar_DejaTodoEnCero()
        {
            MovimientoAPie m = new MovimientoAPie();
            Horizontal(m, Vector2.up, true, true, 1f);
            Despegar(m, out _); // T-1550: el impulso sale 0.1 s después de pedir el salto
            m.Reiniciar();
            Assert.AreEqual(Vector2.zero, m.VelocidadHorizontal);
            Assert.AreEqual(0f, m.VelocidadVertical);
        }
    }
}
