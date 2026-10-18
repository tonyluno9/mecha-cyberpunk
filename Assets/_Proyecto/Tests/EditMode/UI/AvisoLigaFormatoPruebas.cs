using System;
using MechaCyberpunk.Core;
using MechaCyberpunk.UI;
using MechaCyberpunk.UI.Hud;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>Formato del aviso de liga (T-161): lógica pura de <see cref="FormatoHud.AvisoLiga"/>.</summary>
    public class AvisoLigaFormatoPruebas
    {
        private static readonly string[] Nombres = { "SÓTANO", "CALLE", "DISTRITO", "CAMPEONATO" };
        private ConfigHudAPie config;

        private sealed class LigasFalsas : IProgresoLigas
        {
            public int CantidadLigas => Nombres.Length;
            public int LigaActual => 1;
            public int CampeonesVencidos => 0;
            public string NombreLiga(int liga) => liga >= 1 && liga <= Nombres.Length ? Nombres[liga - 1] : string.Empty;
#pragma warning disable CS0067
            public event Action Cambio;
            public event Action<int> CampeonVencido;
#pragma warning restore CS0067
        }

        [SetUp]
        public void Setup() => config = ScriptableObject.CreateInstance<ConfigHudAPie>();

        [TearDown]
        public void TearDown() => UnityEngine.Object.DestroyImmediate(config);

        [TestCase(1, "¡CAMPEÓN DE LA LIGA SÓTANO!", "SE ABRE LA LIGA CALLE")]
        [TestCase(2, "¡CAMPEÓN DE LA LIGA CALLE!", "SE ABRE LA LIGA DISTRITO")]
        [TestCase(3, "¡CAMPEÓN DE LA LIGA DISTRITO!", "SE ABRE LA LIGA CAMPEONATO")]
        public void AvisoLiga_LigaIntermedia_DiceCampeonYLaQueSeAbre(int liga, string primera, string segunda)
        {
            (string linea1, string linea2) = FormatoHud.AvisoLiga(liga, new LigasFalsas(), config);
            Assert.AreEqual(primera, linea1);
            Assert.AreEqual(segunda, linea2);
        }

        [Test]
        public void AvisoLiga_UltimaLiga_DiceCampeonDelClandestinoSinSegundaLinea()
        {
            (string linea1, string linea2) = FormatoHud.AvisoLiga(4, new LigasFalsas(), config);
            Assert.AreEqual("¡CAMPEÓN DEL CLANDESTINO!", linea1);
            Assert.AreEqual(string.Empty, linea2);
        }
    }
}
