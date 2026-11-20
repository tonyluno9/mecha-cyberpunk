using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.Personaje.Visual;
using MechaCyberpunk.Trafico.Datos;
using MechaCyberpunk.Trafico.Peatones;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.IA
{
    /// <summary>T-1516: un civil de la ciudad (peatón con su visual) se agacha cubriéndose al oír tiros y juega el clip; otro platica con su clip.</summary>
    public class CivilAgachadoPlaticaT1516Pruebas
    {
        private const string RutaVisual = "Assets/_Proyecto/Personaje/Visual/Prefabs/Visual_NP07_Pescador_a.prefab";
        private static readonly Vector3[] Ruta = { Vector3.zero, new Vector3(200f, 0f, 0f) };

        private GameObject raiz;
        private ConfigPeatones config;

        [SetUp]
        public void Preparar()
        {
            EventosRuido.LimpiarSuscriptores();
            config = ScriptableObject.CreateInstance<ConfigPeatones>();
            raiz = new GameObject("Peatones");
        }

        [TearDown]
        public void Limpiar()
        {
            EventosRuido.LimpiarSuscriptores();
            Object.Destroy(raiz);
            Object.Destroy(config);
        }

        private PeatonMundo CrearCivil(float metros)
        {
            GameObject objeto = new GameObject("Civil");
            objeto.transform.SetParent(raiz.transform, false);
            objeto.SetActive(false);
            objeto.AddComponent<AnimacionCivilMundo>();
            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RutaVisual), objeto.transform);
            PeatonMundo peaton = objeto.AddComponent<PeatonMundo>();
            peaton.Preparar(Ruta, config, new System.Random(3), () => (Vector3?)null, metros / config.Velocidad);
            objeto.SetActive(true);
            return peaton;
        }

        [UnityTest]
        public IEnumerator Civil_AlOirTiros_SeAgachaJugandoElClipDeAgachado_YSeLevanta()
        {
            ConfigClipsNovaNpc clips = ConfigClipsNovaNpc.Actual;
            Assert.IsNotNull(clips != null ? clips.Agachado : null, "falta el clip de agachado en la config (ClipsNpcPlaticarT1516.AplicarConfig)");
            config.ConfigurarReaccion(0, 1f, 1.2f);
            PeatonMundo civil = CrearCivil(50f);
            yield return null;
            EventosRuido.PublicarDisparo(new Vector3(40f, 0f, 0f));
            yield return null;
            yield return null;
            ReproductorClipUnico reproductor = civil.GetComponentInChildren<ReproductorClipUnico>();
            Assert.IsNotNull(reproductor, "el puente de animación puso su reproductor");
            Assert.AreEqual(clips.Agachado, reproductor.ClipActual, "juega el clip de agachado");
            Assert.IsTrue(civil.Agachado);
            yield return new WaitForSeconds(1.6f);
            Assert.IsFalse(civil.Agachado, "se levantó");
            Assert.AreNotEqual(clips.Agachado, reproductor.ClipActual, "dejó el clip de agachado (juega el de susto o ninguno)");
        }

        [UnityTest]
        public IEnumerator Civil_QuePlatica_JuegaElClipDePlatica_YAlOirTirosLoSuelta()
        {
            ConfigClipsNovaNpc clips = ConfigClipsNovaNpc.Actual;
            Assert.IsNotNull(clips != null ? clips.Platicar1 : null, "falta el clip de platicar en la config");
            config.ConfigurarReaccion(8, 0f, 1.2f);
            PeatonMundo civil = CrearCivil(50f);
            civil.Platicar(0, 2);
            yield return new WaitForSeconds(0.3f);
            ReproductorClipUnico reproductor = civil.GetComponentInChildren<ReproductorClipUnico>();
            Assert.IsNotNull(reproductor);
            Assert.AreEqual(clips.Platicar1, reproductor.ClipActual, "juega NPC_Platicar_1");
            EventosRuido.PublicarDisparo(new Vector3(40f, 0f, 0f));
            yield return null;
            Assert.AreNotEqual(clips.Platicar1, reproductor.ClipActual, "el disparo corta la plática");
        }
    }
}
