using MechaCyberpunk.Core;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Core
{
    /// <summary>V8–V10: las opciones de comodidad de VR se guardan en PlayerPrefs y se vuelven a leer igual (o por defecto si no hay nada).</summary>
    public class OpcionesVRPruebas
    {
        private static readonly string[] Claves = { OpcionesVR.ClaveGiro, OpcionesVR.ClaveVineta, OpcionesVR.ClaveCorrer };
        private readonly int?[] guardadas = new int?[3];

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
            Assert.AreEqual(ModoGiroVR.PorPasos, OpcionesVR.Giro);
            Assert.IsTrue(OpcionesVR.Vineta);
            Assert.AreEqual(ModoCorrerVR.Mantener, OpcionesVR.Correr);
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
                OpcionesVR.FijarGiro(ModoGiroVR.Suave);
                OpcionesVR.FijarGiro(ModoGiroVR.Suave);
                Assert.AreEqual(1, avisos, "Fijar el mismo valor no vuelve a avisar.");
                OpcionesVR.FijarVineta(false);
                Assert.AreEqual(2, avisos);
            }
            finally
            {
                OpcionesVR.Cambio -= Contar;
            }
        }
    }
}
