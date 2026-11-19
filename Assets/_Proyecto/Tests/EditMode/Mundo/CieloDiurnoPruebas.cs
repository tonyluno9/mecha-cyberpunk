using System.IO;
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
            // 19-nov, a propósito: T-1399 (pedido de Tony) subió la niebla de noche a densidad 0.009 y color (0.34, 0.42, 0.57); T-1392 aclaró el ambiente a (0.46, 0.52, 0.66).
            // Antes: densidad 0.006 (T-920), niebla (0.2, _, 0.32) y ambiente r 0.4 (T-880).
            Assert.AreEqual(0.009f, config.DensidadNiebla(HoraNoche), Tolerancia, "T-1399 (19-nov): niebla de noche más densa y de color (antes 0.006, T-920)");
            // T-1461 (19-nov, sesión A): niebla azul-gris (0.30, 0.40, 0.50).
            Assert.AreEqual(0.30f, config.ColorNiebla(HoraNoche).r, Tolerancia, "T-1461: niebla azul-gris (antes 0.34, T-1399)");
            Assert.AreEqual(0.50f, config.ColorNiebla(HoraNoche).b, Tolerancia, "T-1461 (antes 0.57, T-1399)");
            Assert.AreEqual(0.46f, config.ColorAmbiente(HoraNoche).r, Tolerancia, "T-1392 (19-nov): ambiente de noche más claro (antes 0.4, T-880)");
            Assert.AreEqual(0f, config.DiscoSol(HoraNoche), Tolerancia, "La luna no se dibuja como disco");
        }

        [Test]
        public void CieloDeNoche_AzulMarinoMasOscuroQueLaNiebla()
        {
            // T-880: el cielo de K01 es azul marino liso y la niebla azul grisácea más clara; el horizonte del cielo sí es la niebla.
            ConfigCicloDia config = Config();
            Color cielo = config.ColorCielo(HoraNoche);
            Color niebla = config.ColorNiebla(HoraNoche);
            Assert.Less(cielo.grayscale, niebla.grayscale, "el cénit más oscuro que la niebla");
            Assert.Greater(cielo.b, cielo.r * 2f, "azul marino");
        }

        [Test]
        public void MaterialCielo_UsaElCieloGradiente()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(RutaMaterialCielo);
            Assert.IsNotNull(material, "Falta Cielo_Procedural.mat: corre Mecha Cyberpunk/Crear T-079");
            Assert.AreEqual("MechaCyberpunk/CieloGradiente", material.shader.name, "T-806 cambió el Skybox/Procedural (morado a mediodía)");
        }

        [Test]
        public void CieloDeDia_CenitMasAzulQueElHorizonte_YSinBrilloDeCiudad()
        {
            ConfigCicloDia config = Config();
            Color cenit = config.ColorCielo(HoraMediodia);
            Color horizonte = config.ColorNiebla(HoraMediodia);
            Assert.Greater(cenit.b - cenit.r, horizonte.b - horizonte.r, "El cénit es más azul que la bruma del horizonte");
            Assert.Greater(horizonte.grayscale, cenit.grayscale, "La bruma del horizonte es más clara que el cénit");
            Assert.Greater(cenit.g, cenit.r, "Azul verdoso, no morado (T-806)");
            Assert.AreEqual(0f, config.BrilloCiudad(HoraMediodia).a, Tolerancia, "De día no hay brillo de la ciudad en el cielo");
            Assert.Greater(config.BrilloCiudad(HoraNoche).a, 0.1f, "De noche la ciudad ilumina el horizonte");
        }

        [Test]
        public void CapasDelCielo_SeProyectanSobreUnPlano_YVanEnElPlanoLejano()
        {
            string capas = File.ReadAllText("Assets/_Proyecto/Mundo/Resources/Shaders/CieloTextura.shader");
            string domo = File.ReadAllText("Assets/_Proyecto/Mundo/Ambiente/Shaders/NubesNoche.shader");
            StringAssert.Contains("d.xz / (arriba", capas, "T-806: sin las uv de la esfera, que juntaban las nubes en el cénit");
            StringAssert.Contains("UNITY_RAW_FAR_CLIP_VALUE", capas, "Las nubes nunca quedan delante de un edificio lejano");
            StringAssert.Contains("UNITY_RAW_FAR_CLIP_VALUE", domo, "El domo de noche tampoco");
        }
    }
}
