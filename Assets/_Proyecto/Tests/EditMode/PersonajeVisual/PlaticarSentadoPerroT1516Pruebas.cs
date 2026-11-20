using System.Collections.Generic;
using System.Linq;
using MechaCyberpunk.Personaje.Visual;
using MechaCyberpunk.Personaje.Visual.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace MechaCyberpunk.Tests.PersonajeVisual
{
    /// <summary>
    /// T-1516: los clips de platicar, sentado y agachado están en la config; el perro usa el modelo de escala real (1.2 m) y sus clips E17_* no tienen
    /// curvas sin hueso. Necesita haber corrido ClipsNpcNovaT1506.Importar, ClipsNpcPlaticarT1516.AplicarConfig y PerroEscalaRealT1516.Importar y Aplicar.
    /// </summary>
    public class PlaticarSentadoPerroT1516Pruebas
    {
        private static float AlturaRenderers(GameObject prefab)
        {
            GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            try
            {
                Bounds? total = null;
                foreach (Renderer r in inst.GetComponentsInChildren<Renderer>())
                {
                    if (total.HasValue) { Bounds b = total.Value; b.Encapsulate(r.bounds); total = b; }
                    else { total = r.bounds; }
                }
                return total.HasValue ? total.Value.size.y : 0f;
            }
            finally
            {
                Object.DestroyImmediate(inst);
            }
        }

        [TestCase("NPC_Platicar_1")]
        [TestCase("NPC_Platicar_2")]
        [TestCase("NPC_Sentado_Banca")]
        [TestCase("NPC_Agachado_CubriendoCabeza")]
        public void ConfigClips_TieneElClipDeCadaPapel(string nombre)
        {
            var config = Resources.Load<ConfigClipsNovaNpc>(ConfigClipsNovaNpc.NombreRecurso);
            Assert.IsNotNull(config, "falta Resources/ConfigClipsNovaNpc");
            AnimationClip clip = new[] { config.Platicar1, config.Platicar2, config.SentadoBanca, config.Agachado }.FirstOrDefault(c => c != null && c.name == nombre);
            Assert.IsNotNull(clip, nombre + " no está en la config (corre ClipsNpcPlaticarT1516.AplicarConfig)");
            Assert.IsTrue(clip.isLooping, nombre + " va en bucle");
        }

        [Test]
        public void SentadoEnBanca_AlineaAlAsiento_ConElFrenteDeLaBanca()
        {
            var banca = new GameObject("Banca").transform;
            banca.SetPositionAndRotation(new Vector3(5f, 0.2f, -3f), Quaternion.Euler(0f, 90f, 0f));
            var npc = new GameObject("Npc").transform;
            try
            {
                NpcSentadoBanca.Alinear(npc, banca, 0.4f);
                Assert.AreEqual(0.2f + NpcSentadoBanca.AlturaAsientoBanca - NpcSentadoBanca.AlturaAsientoClip, npc.position.y, 1e-4f, "el asiento del clip coincide con el de la banca");
                Assert.Less(Vector3.Distance(new Vector3(npc.position.x, 0f, npc.position.z), new Vector3(5f, 0f, -3f) + banca.right * 0.4f), 1e-4f, "corrido a lo largo de la banca");
                Assert.Greater(Vector3.Dot(npc.forward, -banca.forward), 0.999f, "mira al frente de la banca (-Z)");
            }
            finally
            {
                Object.DestroyImmediate(banca.gameObject);
                Object.DestroyImmediate(npc.gameObject);
            }
        }

        [Test]
        public void Perro_ModeloDeEscalaReal_EsGenericYMideUnMetroVeinte()
        {
            var importador = AssetImporter.GetAtPath(PerroEscalaRealT1516.RutaModelo) as ModelImporter;
            Assert.IsNotNull(importador, "falta " + PerroEscalaRealT1516.RutaModelo + " (corre PerroEscalaRealT1516.Importar)");
            Assert.AreEqual(ModelImporterAnimationType.Generic, importador.animationType);
            Assert.IsTrue(importador.userData.StartsWith("escala="), "la escala va en userData: " + importador.userData);
            GameObject visual = AssetDatabase.LoadAssetAtPath<GameObject>(CreadorVisualPersonaje.RutaPrefab(PerroEscalaRealT1516.Variante));
            Assert.IsNotNull(visual, "falta el visual (corre PerroEscalaRealT1516.Aplicar)");
            Assert.AreEqual(EscalasNova.AlturaPerroE17, AlturaRenderers(visual), EscalasNova.AlturaPerroE17 * 0.10f);
        }

        [Test]
        public void Perro_ClipsE17_UsanLosHuesosDelModeloNuevo_SinCurvasSinHueso()
        {
            var modelo = AssetDatabase.LoadAssetAtPath<GameObject>(PerroEscalaRealT1516.RutaModelo);
            Assert.IsNotNull(modelo, "falta " + PerroEscalaRealT1516.RutaModelo);
            var rutas = new HashSet<string>();
            void Recorrer(Transform t, string prefijo)
            {
                foreach (Transform hijo in t)
                {
                    string ruta = prefijo.Length == 0 ? hijo.name : prefijo + "/" + hijo.name;
                    rutas.Add(ruta);
                    Recorrer(hijo, ruta);
                }
            }
            Recorrer(modelo.transform, "");
            foreach (string nombre in ClipsNpcNovaT1506.ClipsPerro)
            {
                AnimationClip clip = ClipsNpcNovaT1506.CargarClip(nombre);
                Assert.IsNotNull(clip, nombre);
                var sinHueso = AnimationUtility.GetCurveBindings(clip).Where(b => b.path.Length > 0 && !rutas.Contains(b.path)).Select(b => b.path).Distinct().ToList();
                Assert.AreEqual(0, sinHueso.Count, nombre + ": curvas sin hueso en el modelo nuevo: " + string.Join(", ", sinHueso));
            }
        }

        [Test]
        public void Perro_Controlador_UsaLosClipsE17()
        {
            var controlador = AssetDatabase.LoadAssetAtPath<AnimatorController>(CreadorVisualPersonaje.RutaControlador(PerroEscalaRealT1516.Variante));
            Assert.IsNotNull(controlador, "falta el controlador del perro nuevo");
            var nombres = controlador.animationClips.Select(c => c.name).ToList();
            foreach (string esperado in new[] { "E17_Caminar", "E17_Correr", "E17_Morder", "E17_Muerte" })
            {
                CollectionAssert.Contains(nombres, esperado);
            }
        }

        [Test]
        public void Perro_Enemigo_UsaElVisualNuevo_ConsuCapsulaYComportamiento()
        {
            GameObject enemigo = AssetDatabase.LoadAssetAtPath<GameObject>(PerroEscalaRealT1516.RutaEnemigo);
            Assert.IsNotNull(enemigo);
            Transform modelo = enemigo.transform.Find("Modelo");
            Assert.IsNotNull(modelo, "el enemigo conserva su hijo Modelo");
            string origen = AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(modelo.gameObject));
            Assert.AreEqual(CreadorVisualPersonaje.RutaPrefab(PerroEscalaRealT1516.Variante), origen, "el modelo viene del visual de escala real");
            Assert.IsTrue(enemigo.GetComponents<MonoBehaviour>().Any(m => m != null && m.GetType().Name == "PandilleroAPie"), "sigue siendo el PandilleroAPie del perro");
        }
    }
}
