using MechaCyberpunk.Core;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Core
{
    /// <summary>V8–V10: las opciones de comodidad de VR se guardan en PlayerPrefs y se vuelven a leer igual (o por defecto si no hay nada).</summary>
    public class OpcionesVRPruebas
    {
        private static readonly string[] Claves = { OpcionesVR.ClaveGiro, OpcionesVR.ClaveVineta, OpcionesVR.ClaveCorrer, OpcionesVR.ClaveVelocidadGiro,
            OpcionesVR.ClaveNivelVineta, OpcionesVR.ClaveRuta };
        private readonly int?[] guardadas = new int?[6];

        [SetUp]
        public void Preparar()
        {
            for (int i = 0; i < Claves.Length; i++)
            {
                guardadas[i] = PlayerPrefs.HasKey(Claves[i]) ? PlayerPrefs.GetInt(Claves[i]) : (int?)null;
                PlayerPrefs.DeleteKey(Claves[i]);
            }
            OpcionesVR.Recargar();
        }

        [TearDown]
        public void Restaurar()
        {
            for (int i = 0; i < Claves.Length; i++)
            {
                if (guardadas[i].HasValue)
                {
                    PlayerPrefs.SetInt(Claves[i], guardadas[i].Value);
                }
                else
                {
                    PlayerPrefs.DeleteKey(Claves[i]);
                }
            }
            PlayerPrefs.Save();
            OpcionesVR.Recargar();
        }

        [Test]
        public void Recargar_SinNadaGuardado_DaLosPorDefecto()
        {
            Assert.AreEqual(ModoGiroVR.Suave, OpcionesVR.Giro, "VR18: el giro es SUAVE por defecto.");
            Assert.IsTrue(OpcionesVR.Vineta);
            Assert.AreEqual(NivelVinetaVR.Baja, OpcionesVR.NivelVineta, "VR26: VIÑETA BAJA por defecto.");
            Assert.IsTrue(OpcionesVR.Ruta, "RUTA: SÍ por defecto.");
            Assert.AreEqual(ModoCorrerVR.Mantener, OpcionesVR.Correr);
            Assert.AreEqual(120f, OpcionesVR.VelocidadGiro, 1e-3f, "VR18: 120°/s a fondo por defecto.");
        }

        [Test]
        public void VelocidadGiro_SeGuardaYSeLimitaAlRango()
        {
            OpcionesVR.FijarVelocidadGiro(75f);
            OpcionesVR.Recargar();
            Assert.AreEqual(75f, OpcionesVR.VelocidadGiro, 1e-3f);
            OpcionesVR.FijarVelocidadGiro(1000f);
            Assert.AreEqual(OpcionesVR.VelocidadGiroMaxima, OpcionesVR.VelocidadGiro, 1e-3f);
            OpcionesVR.FijarVelocidadGiro(-5f);
            Assert.AreEqual(OpcionesVR.VelocidadGiroMinima, OpcionesVR.VelocidadGiro, 1e-3f);
        }

        [Test]
        public void Fijar_LoGuardaYAlRecargarLeeLoMismo()
        {
            OpcionesVR.FijarGiro(ModoGiroVR.Suave);
            OpcionesVR.FijarVineta(false);
            OpcionesVR.FijarCorrer(ModoCorrerVR.Alternar);
            Assert.AreEqual((int)ModoGiroVR.Suave, PlayerPrefs.GetInt(OpcionesVR.ClaveGiro, -1));
            Assert.AreEqual(0, PlayerPrefs.GetInt(OpcionesVR.ClaveVineta, -1));
            Assert.AreEqual((int)ModoCorrerVR.Alternar, PlayerPrefs.GetInt(OpcionesVR.ClaveCorrer, -1));

            OpcionesVR.Recargar();
            Assert.AreEqual(ModoGiroVR.Suave, OpcionesVR.Giro);
            Assert.IsFalse(OpcionesVR.Vineta);
            Assert.AreEqual(ModoCorrerVR.Alternar, OpcionesVR.Correr);

            OpcionesVR.FijarGiro(ModoGiroVR.PorPasos);
            OpcionesVR.FijarVineta(true);
            OpcionesVR.FijarCorrer(ModoCorrerVR.Mantener);
            OpcionesVR.Recargar();
            Assert.AreEqual(ModoGiroVR.PorPasos, OpcionesVR.Giro);
            Assert.IsTrue(OpcionesVR.Vineta);
            Assert.AreEqual(ModoCorrerVR.Mantener, OpcionesVR.Correr);
        }

        [Test]
        public void Recargar_ValorGuardadoInvalido_DaElPorDefecto()
        {
            PlayerPrefs.SetInt(OpcionesVR.ClaveGiro, 7);
            PlayerPrefs.SetInt(OpcionesVR.ClaveCorrer, -3);
            OpcionesVR.Recargar();
            Assert.AreEqual(OpcionesVR.GiroPorDefecto, OpcionesVR.Giro);
            Assert.AreEqual(OpcionesVR.CorrerPorDefecto, OpcionesVR.Correr);
        }

        [Test]
        public void Fijar_ValorNuevo_AvisaCambioUnaSolaVez()
        {
            int avisos = 0;
            void Contar() => avisos++;
            OpcionesVR.Cambio += Contar;
            try
            {
                OpcionesVR.FijarGiro(ModoGiroVR.PorPasos);
                OpcionesVR.FijarGiro(ModoGiroVR.PorPasos);
                Assert.AreEqual(1, avisos, "Fijar el mismo valor no vuelve a avisar.");
                OpcionesVR.FijarVineta(false);
                Assert.AreEqual(2, avisos);
            }
            finally
            {
                OpcionesVR.Cambio -= Contar;
            }
        }

        [TestCase(0, NivelVinetaVR.No)]
        [TestCase(1, NivelVinetaVR.Baja)]
        public void Recargar_SoloClaveViejaDeVineta_PasaANivel(int vieja, NivelVinetaVR esperado)
        {
            PlayerPrefs.SetInt(OpcionesVR.ClaveVineta, vieja);
            OpcionesVR.Recargar();
            Assert.AreEqual(esperado, OpcionesVR.NivelVineta);
        }

        [Test]
        public void NivelVineta_SeGuardaYLaAperturaCierraPoco()
        {
            OpcionesVR.FijarNivelVineta(NivelVinetaVR.Media);
            OpcionesVR.Recargar();
            Assert.AreEqual(NivelVinetaVR.Media, OpcionesVR.NivelVineta);
            Assert.IsTrue(OpcionesVR.Vineta);
            OpcionesVR.FijarVineta(true);
            Assert.AreEqual(NivelVinetaVR.Media, OpcionesVR.NivelVineta, "FijarVineta(true) no baja MEDIA a BAJA.");
            OpcionesVR.FijarVineta(false);
            Assert.AreEqual(NivelVinetaVR.No, OpcionesVR.NivelVineta);
            Assert.AreEqual(1f, OpcionesVR.AperturaVineta(NivelVinetaVR.No), 1e-4f);
            // BAJA cierra ~20% de lo que cerraba antes (apertura 0.7 → cerraba 0.3).
            Assert.AreEqual(0.3f * 0.2f, 1f - OpcionesVR.AperturaVineta(NivelVinetaVR.Baja), 0.02f);
            Assert.Less(OpcionesVR.AperturaVineta(NivelVinetaVR.Media), OpcionesVR.AperturaVineta(NivelVinetaVR.Baja));
            Assert.LessOrEqual(OpcionesVR.SegundosVineta, 0.1f + 1e-4f);
        }

        [Test]
        public void Ruta_SeGuardaYAvisa()
        {
            int avisos = 0;
            void Contar() => avisos++;
            OpcionesVR.Cambio += Contar;
            try
            {
                OpcionesVR.FijarRuta(false);
                OpcionesVR.FijarRuta(false);
                Assert.AreEqual(1, avisos);
                OpcionesVR.Recargar();
                Assert.IsFalse(OpcionesVR.Ruta);
                Assert.AreEqual(0, PlayerPrefs.GetInt(OpcionesVR.ClaveRuta, -1));
            }
            finally
            {
                OpcionesVR.Cambio -= Contar;
            }
        }
    }
}
