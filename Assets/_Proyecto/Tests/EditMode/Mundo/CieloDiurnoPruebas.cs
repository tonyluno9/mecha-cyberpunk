using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using MechaCyberpunk.Mundo.Datos;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-310: el cielo y la niebla de las 12:00 tienen color (no blanco-gris) y la noche de las 22:00 no cambió.</summary>
    public class CieloDiurnoPruebas
    {
        private const string RutaConfig = "Assets/_Proyecto/Mundo/Datos/ConfigCicloDia_Mundo.asset";
        private const string RutaMaterialCielo = "Assets/_Proyecto/Mundo/Materiales/Cielo_Procedural.mat";
        private const float HoraMediodia = 12f;
        private const float HoraNoche = 22f;
        private const float SaturacionMinimaDia = 0.2f;
        private const float DensidadMinimaDia = 0.005f;
        private const float Tolerancia = 0.001f;

        private static float Saturacion(Color color)
        {
            Color.RGBToHSV(color, out _, out float s, out _);
            return s;
        }

        // Se prueba el asset real si existe; si no, los valores por defecto del código, que son los que el creador copia al asset.
        private static ConfigCicloDia Config()
        {
            ConfigCicloDia asset = AssetDatabase.LoadAssetAtPath<ConfigCicloDia>(RutaConfig);
            return asset != null ? asset : ScriptableObject.CreateInstance<ConfigCicloDia>();
        }

        [Test]
        public void NieblaYCielo_AMediodia_TienenColorAzulado()
        {
            ConfigCicloDia config = Config();
            Color niebla = config.ColorNiebla(HoraMediodia);
            Color cielo = config.ColorCielo(HoraMediodia);
            Assert.GreaterOrEqual(Saturacion(niebla), SaturacionMinimaDia, "La niebla de día no debe ser gris");
            Assert.GreaterOrEqual(Saturacion(cielo), SaturacionMinimaDia, "El cielo de día no debe ser blanco-gris");
            Assert.Greater(niebla.b, niebla.r, "La niebla de día es azul violácea");
            Assert.Greater(cielo.b, cielo.r);
        }

        [Test]
        public void Niebla_AMediodia_SeNota()
        {
            Assert.GreaterOrEqual(Config().DensidadNiebla(HoraMediodia), DensidadMinimaDia);
        }

        [Test]
        public void Noche_ConservaLosValoresDeAntes()
        {
            ConfigCicloDia config = Config();
            Assert.AreEqual(0.02f, config.DensidadNiebla(HoraNoche), Tolerancia);
            Assert.AreEqual(0.12f, config.ColorNiebla(HoraNoche).r, Tolerancia);
            Assert.AreEqual(0.26f, config.ColorNiebla(HoraNoche).b, Tolerancia);
            Assert.AreEqual(0.27f, config.ColorAmbiente(HoraNoche).r, Tolerancia);
            Assert.AreEqual(0f, config.DiscoSol(HoraNoche), Tolerancia, "La luna no se dibuja como disco");
        }

        [Test]
        public void CieloDeNoche_EsElColorDeLaNiebla()
        {
            ConfigCicloDia config = Config();
            Color cielo = config.ColorCielo(HoraNoche);
            Color niebla = config.ColorNiebla(HoraNoche);
            Assert.AreEqual(niebla.r, cielo.r, Tolerancia);
            Assert.AreEqual(niebla.g, cielo.g, Tolerancia);
            Assert.AreEqual(niebla.b, cielo.b, Tolerancia);
        }

        [Test]
        public void MaterialCielo_UsaElSkyboxProcedural()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(RutaMaterialCielo);
            Assert.IsNotNull(material, "Falta Cielo_Procedural.mat: corre Mecha Cyberpunk/Crear T-079");
            Assert.AreEqual("Skybox/Procedural", material.shader.name);
        }
    }
}
