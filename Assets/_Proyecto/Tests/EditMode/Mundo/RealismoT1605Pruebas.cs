using MechaCyberpunk.Mundo.Ambiente;
using MechaCyberpunk.Mundo.Datos;
using MechaCyberpunk.Mundo.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T14 (T-1605): el posproceso tiene lo pedido (hay que correr RealismoT1605.Aplicar antes), el selector de calidad aplica los tres niveles y el tinte del cielo sigue al barrio.</summary>
    public class RealismoT1605Pruebas
    {
        private const string NombreShaderHalo = "MechaCyberpunk/Mundo/HaloLuz";
        private GameObject raiz;
        private int prefsOriginal;
        private bool habiaPrefs;

        [SetUp]
        public void PrepararPrefs()
        {
            habiaPrefs = PlayerPrefs.HasKey(CalculoCalidadRealismo.LlaveNivel);
            prefsOriginal = PlayerPrefs.GetInt(CalculoCalidadRealismo.LlaveNivel, 1);
        }

        [TearDown]
        public void Limpiar()
        {
            if (raiz != null)
            {
                Object.DestroyImmediate(raiz);
            }
            if (habiaPrefs)
            {
                PlayerPrefs.SetInt(CalculoCalidadRealismo.LlaveNivel, prefsOriginal);
            }
            else
            {
                PlayerPrefs.DeleteKey(CalculoCalidadRealismo.LlaveNivel);
            }
        }

        [Test]
        public void Perfiles_NocheYDia_TienenLosOverridesDelLook()
        {
            foreach (string ruta in new[] { RealismoT1605.RutaPerfilNoche, RealismoT1605.RutaPerfilDia })
            {
                VolumeProfile perfil = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ruta);
                Assert.IsNotNull(perfil, "Falta " + ruta);
                Assert.IsTrue(perfil.TryGet(out Bloom bloom) && bloom.active && bloom.threshold.overrideState, ruta + ": bloom");
                Assert.IsTrue(perfil.TryGet(out Tonemapping tono) && tono.active && tono.mode.value == TonemappingMode.ACES, ruta + ": ACES");
                Assert.IsTrue(perfil.TryGet(out Vignette vineta) && vineta.active && vineta.intensity.value > 0f && vineta.intensity.value < 0.5f, ruta + ": viñeta suave");
                Assert.IsTrue(perfil.TryGet(out ChromaticAberration aberracion) && aberracion.active && aberracion.intensity.value > 0f && aberracion.intensity.value <= 0.1f, ruta + ": aberración leve");
                Assert.IsTrue(perfil.TryGet(out SplitToning partido) && partido.active, ruta + ": tono dividido");
                Assert.Greater(partido.shadows.value.b, partido.shadows.value.r, ruta + ": sombras azules");
                Assert.Greater(partido.highlights.value.r, partido.highlights.value.b, ruta + ": luces cálidas");
            }
        }

        [Test]
        public void Calidad_BajoMedioAlto_ApagaYPrendeHalosYParticulas()
        {
            Shader shaderHalo = Shader.Find(NombreShaderHalo);
            Assert.IsNotNull(shaderHalo, "Falta el shader " + NombreShaderHalo);
            raiz = new GameObject("PruebaT1605");
            raiz.SetActive(false);
            CalidadRealismo calidad = raiz.AddComponent<CalidadRealismo>();
            ConfigRealismo config = ScriptableObject.CreateInstance<ConfigRealismo>();
            SerializedObject so = new SerializedObject(calidad);
            so.FindProperty("config").objectReferenceValue = config;
            so.ApplyModifiedPropertiesWithoutUndo();

            GameObject halo = GameObject.CreatePrimitive(PrimitiveType.Quad);
            halo.transform.SetParent(raiz.transform, false);
            halo.GetComponent<MeshRenderer>().sharedMaterial = new Material(shaderHalo);
            GameObject polvo = new GameObject("Vapor_prueba");
            polvo.transform.SetParent(raiz.transform, false);
            ParticleSystem sistema = polvo.AddComponent<ParticleSystem>();
            raiz.SetActive(true);

            calidad.Aplicar(NivelCalidadRealismo.Bajo);
            Assert.IsFalse(halo.GetComponent<MeshRenderer>().enabled, "Bajo apaga los halos");
            Assert.IsFalse(sistema.emission.enabled, "Bajo apaga las partículas");
            Assert.AreEqual(NivelCalidadRealismo.Bajo, calidad.Nivel);

            calidad.Aplicar(NivelCalidadRealismo.Medio);
            Assert.IsTrue(halo.GetComponent<MeshRenderer>().enabled, "Medio prende los halos");
            Assert.IsTrue(sistema.emission.enabled, "Medio prende las partículas");

            calidad.Aplicar(NivelCalidadRealismo.Alto);
            Assert.IsTrue(halo.GetComponent<MeshRenderer>().enabled, "Alto prende los halos");
            Assert.IsTrue(sistema.emission.enabled, "Alto prende las partículas");
            Assert.AreEqual(NivelCalidadRealismo.Alto, calidad.Nivel);

            Assert.IsFalse(CalculoCalidadRealismo.Ajustes(NivelCalidadRealismo.Bajo).OclusionAmbiental);
            Assert.IsFalse(CalculoCalidadRealismo.Ajustes(NivelCalidadRealismo.Medio).OclusionAmbiental);
            Assert.IsTrue(CalculoCalidadRealismo.Ajustes(NivelCalidadRealismo.Alto).OclusionAmbiental);
            Assert.IsFalse(CalculoCalidadRealismo.Ajustes(NivelCalidadRealismo.Medio).SombrasLucesAdicionales);
            Assert.IsTrue(CalculoCalidadRealismo.Ajustes(NivelCalidadRealismo.Alto).SombrasLucesAdicionales);
            Object.DestroyImmediate(config);
        }

        [Test]
        public void Calidad_LeeElNivelGuardadoEnPlayerPrefs()
        {
            Assert.AreEqual(NivelCalidadRealismo.Bajo, CalculoCalidadRealismo.Nivel(0));
            Assert.AreEqual(NivelCalidadRealismo.Medio, CalculoCalidadRealismo.Nivel(1));
            Assert.AreEqual(NivelCalidadRealismo.Alto, CalculoCalidadRealismo.Nivel(2));
            Assert.AreEqual(NivelCalidadRealismo.Medio, CalculoCalidadRealismo.Nivel(99));
        }

        [Test]
        public void Cielo_BarrioPorPosicion_ConLosLimitesMedidos()
        {
            Assert.AreEqual(BarrioCielo.Suburbios, CalculoCieloRealista.BarrioEn(-100f, 0f, -40f, 40f, -10f));
            Assert.AreEqual(BarrioCielo.Mercado, CalculoCieloRealista.BarrioEn(0f, 30f, -40f, 40f, -10f));
            Assert.AreEqual(BarrioCielo.NeonAlto, CalculoCieloRealista.BarrioEn(80f, 45f, -40f, 40f, -10f));
            Assert.AreEqual(BarrioCielo.Nova, CalculoCieloRealista.BarrioEn(80f, -45f, -40f, 40f, -10f));
        }
    }
}
