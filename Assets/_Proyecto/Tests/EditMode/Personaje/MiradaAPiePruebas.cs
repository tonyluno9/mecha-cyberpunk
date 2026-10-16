using MechaCyberpunk.Personaje.Movimiento;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Personaje
{
    public class MiradaAPiePruebas
    {
        private const float Sensibilidad = 0.1f;
        private const float Limite = 85f;

        [Test]
        public void Actualizar_DeltaEnX_DevuelveElGiroDelCuerpo()
        {
            MiradaAPie m = new MiradaAPie();
            float giro = m.Actualizar(new Vector2(50f, 0f), Sensibilidad, Limite);
            Assert.AreEqual(5f, giro, 0.0001f);
            Assert.AreEqual(0f, m.Inclinacion);
        }

        [Test]
        public void Actualizar_DeltaEnYPositivo_MiraArriba()
        {
            MiradaAPie m = new MiradaAPie();
            m.Actualizar(new Vector2(0f, 100f), Sensibilidad, Limite);
            Assert.AreEqual(-10f, m.Inclinacion, 0.0001f);
        }

        [Test]
        public void Actualizar_DeltaEnYNegativo_MiraAbajo()
        {
            MiradaAPie m = new MiradaAPie();
            m.Actualizar(new Vector2(0f, -100f), Sensibilidad, Limite);
            Assert.AreEqual(10f, m.Inclinacion, 0.0001f);
        }

        [Test]
        public void Actualizar_DeltaEnormeArribaYAbajo_NuncaPasaDelLimite()
        {
            MiradaAPie m = new MiradaAPie();
            m.Actualizar(new Vector2(0f, 1e9f), Sensibilidad, Limite);
            Assert.AreEqual(-Limite, m.Inclinacion, 0.0001f);
            m.Actualizar(new Vector2(0f, 1e9f), Sensibilidad, Limite);
            Assert.AreEqual(-Limite, m.Inclinacion, 0.0001f);
            m.Actualizar(new Vector2(0f, -1e9f), Sensibilidad, Limite);
            Assert.AreEqual(Limite, m.Inclinacion, 0.0001f);
        }

        [Test]
        public void Actualizar_MuchosDeltasPequenos_AcumulaYSeDetieneEnElLimite()
        {
            MiradaAPie m = new MiradaAPie();
            for (int i = 0; i < 1000; i++)
            {
                m.Actualizar(new Vector2(0f, -10f), Sensibilidad, Limite);
                Assert.LessOrEqual(m.Inclinacion, Limite);
            }
            Assert.AreEqual(Limite, m.Inclinacion, 0.0001f);
        }

        [Test]
        public void Reiniciar_DespuesDeInclinar_VuelveAlHorizonte()
        {
            MiradaAPie m = new MiradaAPie();
            m.Actualizar(new Vector2(0f, -100f), Sensibilidad, Limite);
            m.Reiniciar();
            Assert.AreEqual(0f, m.Inclinacion);
        }
    }
}
