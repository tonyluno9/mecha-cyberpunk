using MechaCyberpunk.Combate.Lectura;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.Adaptacion;
using MechaCyberpunk.IA.Datos;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.IA
{
    /// <summary>T-199: cuándo se ve cada hábito del jugador (D-tony-193), con los valores de serie de DatosAdaptacionRival.</summary>
    public class ReglaEstiloPruebas
    {
        private DatosAdaptacionRival datos;

        [SetUp]
        public void SetUp()
        {
            datos = ScriptableObject.CreateInstance<DatosAdaptacionRival>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(datos);
        }

        private static PerfilEstilo Lanzados(int n)
        {
            PerfilEstilo p = new PerfilEstilo();
            for (int i = 0; i < n; i++)
            {
                p.RegistrarGolpeLanzado();
            }
            return p;
        }

        private static PerfilEstilo RivalConCubiertos(int total, int cubiertos)
        {
            PerfilEstilo p = new PerfilEstilo();
            for (int i = 0; i < total; i++)
            {
                p.RegistrarGolpeRival(i < cubiertos ? TipoBloqueo.Normal : TipoBloqueo.Ninguno);
            }
            return p;
        }

        private static PerfilEstilo Conectados(int total, int castigando)
        {
            PerfilEstilo p = new PerfilEstilo();
            for (int i = 0; i < total; i++)
            {
                p.RegistrarConectado(i < castigando);
            }
            return p;
        }

        [Test]
        public void Guardia_JustoEnElUmbralDeRitmoYDeGolpes_SeVio()
        {
            // 8 golpes en 20 s = 24 por minuto exacto, y 8 es el mínimo de golpes.
            Assert.IsTrue(ReglaEstilo.Vio(TipoAdaptacion.Guardia, Lanzados(8), 20f, datos));
        }

        [Test]
        public void Guardia_UnGolpeMenosQueElMinimo_NoSeVio()
        {
            Assert.IsFalse(ReglaEstilo.Vio(TipoAdaptacion.Guardia, Lanzados(7), 10f, datos));
        }

        [Test]
        public void Guardia_RitmoJustoDebajoDelUmbral_NoSeVio()
        {
            // 8 golpes en 20.5 s = 23.4 por minuto.
            Assert.IsFalse(ReglaEstilo.Vio(TipoAdaptacion.Guardia, Lanzados(8), 20.5f, datos));
        }

        [Test]
        public void Guardia_MuchosGolpesEnPocoTiempo_SeVio()
        {
            Assert.IsTrue(ReglaEstilo.Vio(TipoAdaptacion.Guardia, Lanzados(10), 6f, datos));
        }

        [Test]
        public void Fintas_MitadCubierta_SeVio()
        {
            Assert.IsTrue(ReglaEstilo.Vio(TipoAdaptacion.Fintas, RivalConCubiertos(4, 2), 60f, datos));
        }

        [Test]
        public void Fintas_JustoDebajoDeLaMitad_NoSeVio()
        {
            Assert.IsFalse(ReglaEstilo.Vio(TipoAdaptacion.Fintas, RivalConCubiertos(5, 2), 60f, datos));
        }

        [Test]
        public void Fintas_TodoCubiertoPeroPocosGolpesDelRival_NoSeVio()
        {
            Assert.IsFalse(ReglaEstilo.Vio(TipoAdaptacion.Fintas, RivalConCubiertos(3, 3), 60f, datos));
        }

        [Test]
        public void Fintas_BloqueoPerfectoCuentaComoCubierto()
        {
            PerfilEstilo p = new PerfilEstilo();
            for (int i = 0; i < 4; i++)
            {
                p.RegistrarGolpeRival(TipoBloqueo.Perfecto);
            }
            Assert.AreEqual(4, p.GolpesRivalCubiertos);
            Assert.IsTrue(ReglaEstilo.Vio(TipoAdaptacion.Fintas, p, 60f, datos));
        }

        [Test]
        public void Retirada_CuarentaPorCientoCastigando_SeVio()
        {
            Assert.IsTrue(ReglaEstilo.Vio(TipoAdaptacion.Retirada, Conectados(5, 2), 60f, datos));
        }

        [Test]
        public void Retirada_JustoDebajoDelCuarentaPorCiento_NoSeVio()
        {
            Assert.IsFalse(ReglaEstilo.Vio(TipoAdaptacion.Retirada, Conectados(6, 2), 60f, datos));
        }

        [Test]
        public void Retirada_PocosConectados_NoSeVio()
        {
            Assert.IsFalse(ReglaEstilo.Vio(TipoAdaptacion.Retirada, Conectados(3, 3), 60f, datos));
        }

        [Test]
        public void Vio_PeleaMasCortaQueElMinimo_NoSeVioNada()
        {
            PerfilEstilo p = new PerfilEstilo();
            for (int i = 0; i < 20; i++)
            {
                p.RegistrarGolpeLanzado();
                p.RegistrarConectado(true);
                p.RegistrarGolpeRival(TipoBloqueo.Normal);
            }
            float justoMenos = datos.SegundosMinimos - 0.01f;
            foreach (TipoAdaptacion tipo in System.Enum.GetValues(typeof(TipoAdaptacion)))
            {
                Assert.IsFalse(ReglaEstilo.Vio(tipo, p, justoMenos, datos), tipo.ToString());
                Assert.IsTrue(ReglaEstilo.Vio(tipo, p, datos.SegundosMinimos, datos), tipo + " en el mínimo");
            }
        }

        [Test]
        public void Vio_PeleaConDosHabitos_LosVeAmbos()
        {
            PerfilEstilo p = new PerfilEstilo();
            for (int i = 0; i < 12; i++)
            {
                p.RegistrarGolpeLanzado();
            }
            for (int i = 0; i < 5; i++)
            {
                p.RegistrarConectado(true);
            }
            Assert.IsTrue(ReglaEstilo.Vio(TipoAdaptacion.Guardia, p, 20f, datos));
            Assert.IsTrue(ReglaEstilo.Vio(TipoAdaptacion.Retirada, p, 20f, datos));
            Assert.IsFalse(ReglaEstilo.Vio(TipoAdaptacion.Fintas, p, 20f, datos));
        }

        [Test]
        public void Perfil_Reiniciar_DejaTodoEnCero()
        {
            PerfilEstilo p = Conectados(5, 5);
            p.RegistrarGolpeLanzado();
            p.RegistrarGolpeRival(TipoBloqueo.Normal);
            p.Reiniciar();
            Assert.AreEqual(0, p.GolpesLanzados + p.GolpesConectados + p.ConectadosCastigando + p.GolpesRival + p.GolpesRivalCubiertos);
        }
    }
}
