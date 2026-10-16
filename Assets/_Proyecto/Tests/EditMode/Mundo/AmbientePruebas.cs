using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-079: perfiles de posproceso, cámaras con posproceso prendido y materiales de neón con emisión HDR.</summary>
    public class AmbientePruebas
    {
        private const string RutaPerfilNoche = "Assets/_Proyecto/Mundo/Datos/PerfilNoche.asset";
        private const string RutaPerfilDia = "Assets/_Proyecto/Mundo/Datos/PerfilDia.asset";
        private const string RutaCamaraLibre = "Assets/_Proyecto/Mundo/Prefabs/CamaraLibre.prefab";
        private const string RutaCarro = "Assets/_Proyecto/Vehiculos/Prefabs/CarroViejo.prefab";
        private const string CarpetaMateriales = "Assets/_Proyecto/Mundo/Materiales";
        private const float UmbralMinimo = 0.8f;
        private const float UmbralMaximo = 1.2f;
        private const float EmisionMinimaHdr = 2f;
        private const float SuavidadMinimaAsfalto = 0.8f;
        private const float BrilloMaximoAsfalto = 0.06f;
        private const float VinetaMaxima = 0.5f;
        private const int MaterialesEsperados = 6;

        [Test]
        public void PerfilNoche_TieneBloomConUmbralCercanoA1YTonemappingAces()
        {
            VolumeProfile perfil = AssetDatabase.LoadAssetAtPath<VolumeProfile>(RutaPerfilNoche);
            Assert.IsNotNull(perfil, "Falta PerfilNoche.asset");
            Assert.IsTrue(perfil.TryGet(out Bloom bloom), "PerfilNoche necesita Bloom");
            Assert.IsTrue(bloom.active);
            Assert.IsTrue(bloom.threshold.overrideState, "El umbral del bloom debe estar activado");
            Assert.That(bloom.threshold.value, Is.InRange(UmbralMinimo, UmbralMaximo), "Solo el HDR debe brillar");
            Assert.Greater(bloom.intensity.value, 0f);
            Assert.IsTrue(perfil.TryGet(out Tonemapping tonemapping), "PerfilNoche necesita Tonemapping");
            Assert.IsTrue(tonemapping.active);
            Assert.AreEqual(TonemappingMode.ACES, tonemapping.mode.value);
        }

        [Test]
        public void Perfiles_NocheYDia_TienenColorYVineta()
        {
            foreach (string ruta in new[] { RutaPerfilNoche, RutaPerfilDia })
            {
                VolumeProfile perfil = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ruta);
                Assert.IsNotNull(perfil, "Falta " + ruta);
                Assert.IsTrue(perfil.TryGet(out ColorAdjustments color), ruta + " necesita ColorAdjustments");
                Assert.Greater(color.contrast.value, 0f, ruta + ": el contraste sube");
                Assert.Greater(color.saturation.value, 0f, ruta + ": la saturación sube");
                Assert.IsTrue(perfil.TryGet(out Vignette vineta), ruta + " necesita Vignette");
                Assert.Greater(vineta.intensity.value, 0f);
                Assert.Less(vineta.intensity.value, VinetaMaxima, ruta + ": la viñeta es ligera");
            }
        }

        [Test]
        public void PerfilNoche_TieneTinteFrioYMasExposicionQueElDeDia()
        {
            VolumeProfile noche = AssetDatabase.LoadAssetAtPath<VolumeProfile>(RutaPerfilNoche);
            VolumeProfile dia = AssetDatabase.LoadAssetAtPath<VolumeProfile>(RutaPerfilDia);
            Assert.IsTrue(noche.TryGet(out ColorAdjustments colorNoche));
            Assert.IsTrue(dia.TryGet(out ColorAdjustments colorDia));
            Assert.Greater(colorNoche.colorFilter.value.b, colorNoche.colorFilter.value.r, "De noche el tinte es frío");
            Assert.Greater(colorNoche.postExposure.value, colorDia.postExposure.value, "ACES oscurece: la noche compensa con exposición");
        }

        [TestCase(RutaCamaraLibre)]
        [TestCase(RutaCarro)]
        public void Camaras_DeAlan_TienenElPosprocesoPrendido(string ruta)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
            Assert.IsNotNull(prefab, "Falta " + ruta);
            Camera[] camaras = prefab.GetComponentsInChildren<Camera>(true);
            Assert.IsNotEmpty(camaras, ruta + " debe tener una cámara");
            foreach (Camera camara in camaras)
            {
                UniversalAdditionalCameraData datos = camara.GetComponent<UniversalAdditionalCameraData>();
                Assert.IsNotNull(datos, camara.name + " necesita UniversalAdditionalCameraData");
                Assert.IsTrue(datos.renderPostProcessing, camara.name + " debe tener renderPostProcessing prendido");
            }
        }

        [Test]
        public void Materiales_LetrerosYNeonDelBar_TienenEmisionHdr()
        {
            string[] guias = AssetDatabase.FindAssets("t:Material", new[] { CarpetaMateriales });
            int revisados = 0;
            foreach (string guia in guias)
            {
                string ruta = AssetDatabase.GUIDToAssetPath(guia);
                string nombre = Path.GetFileNameWithoutExtension(ruta);
                if (!nombre.StartsWith("Barrio_Letrero_") && !nombre.StartsWith("Bar_Neon"))
                {
                    continue;
                }
                Material material = AssetDatabase.LoadAssetAtPath<Material>(ruta);
                Assert.IsTrue(material.IsKeywordEnabled("_EMISSION"), nombre + " debe tener _EMISSION");
                Color emision = material.GetColor("_EmissionColor");
                Assert.GreaterOrEqual(emision.maxColorComponent, EmisionMinimaHdr, nombre + " debe tener emisión HDR (algún canal >= 2)");
                revisados++;
            }
            Assert.GreaterOrEqual(revisados, MaterialesEsperados, "Deben existir 3 letreros del barrio y 3 neones del bar");
        }

        [Test]
        public void Asfalto_EsOscuroYLiso()
        {
            Material asfalto = AssetDatabase.LoadAssetAtPath<Material>(CarpetaMateriales + "/Barrio_Asfalto.mat");
            Assert.GreaterOrEqual(asfalto.GetFloat("_Smoothness"), SuavidadMinimaAsfalto, "El asfalto mojado es liso");
            Assert.Less(asfalto.GetColor("_BaseColor").grayscale, BrilloMaximoAsfalto, "El asfalto mojado es oscuro");
        }
    }
}
