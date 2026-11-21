using System.Collections;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.Personaje.Visual;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests
{
    /// <summary>
    /// T-1506: la caída de Nova: Caida_Aire (en bucle) mientras el agente baja, Caida_Aterrizaje al tocar el piso y Caida_Levantarse; después el controlador vuelve.
    /// Un soldado que baja de la nave por la cuerda juega Caida_Aire y al llegar aterriza.
    /// </summary>
    public class CaidaNovaT1506Pruebas
    {
        private const string RutaGuardia = "Assets/_Proyecto/IA/Prefabs/GuardiaNova_Armado.prefab";
        private GameObject objeto;
        private GameObject ancla;

        [SetUp]
        public void Preparar()
        {
            var config = ScriptableObject.CreateInstance<ConfigClipsNovaNpc>();
            // T-1553: los E18_* viven en la carpeta Clips de su personaje; los NPC_* siguen en PJ01/Clips.
            AnimationClip Clip(string n)
            {
                string carpeta = n.StartsWith("E18_") ? "Assets/Arte/Personajes/E18_EliteNova/Clips/" : "Assets/Arte/Personajes/PJ01/Clips/";
                return AssetDatabase.LoadAllAssetsAtPath(carpeta + n + ".fbx") is Object[] todos
                    ? System.Array.Find(todos, o => o is AnimationClip c && c.name == n) as AnimationClip : null;
            }
            config.Configurar(Clip("E18_Caida_Aire"), Clip("E18_Caida_Aterrizaje"), Clip("E18_Caida_Levantarse"), Clip("NPC_Asustarse"));
            Assert.IsNotNull(config.CaidaAire, "falta E18_Caida_Aire importado");
            ConfigClipsNovaNpc.Forzar(config);
        }

        [TearDown]
        public void Limpiar()
        {
            if (objeto != null) { Object.Destroy(objeto); }
            if (ancla != null) { Object.Destroy(ancla); }
            ConfigClipsNovaNpc.Forzar(null);
        }

        private static IEnumerator Hasta(System.Func<bool> condicion, string que, float segundos)
        {
            float fin = Time.time + segundos;
            while (Time.time < fin && !condicion())
            {
                yield return null;
            }
            Assert.IsTrue(condicion(), "No llegó: " + que);
        }

        private GameObject CrearGuardia(Vector3 posicion)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaGuardia);
            Assert.IsNotNull(prefab, RutaGuardia);
            objeto = Object.Instantiate(prefab, posicion, Quaternion.identity);
            return objeto;
        }

        [UnityTest]
        public IEnumerator Caida_AireEnBucle_Aterrizaje_Levantarse_YVuelveElControlador()
        {
            CrearGuardia(new Vector3(0f, 0f, 60f));
            yield return null;
            CaidaNova caida = CaidaNova.Iniciar(objeto, false);
            Assert.IsNotNull(caida);
            Assert.AreEqual(CaidaNova.Fase.Aire, caida.Actual);
            Assert.AreEqual("E18_Caida_Aire", caida.ClipActual.name);
            yield return new WaitForSeconds(caida.ClipActual.length + 0.4f);
            Assert.AreEqual(CaidaNova.Fase.Aire, caida.Actual, "el aire se repite hasta que toca el piso");
            caida.Aterrizar();
            Assert.AreEqual(CaidaNova.Fase.Aterrizaje, caida.Actual);
            Assert.AreEqual("E18_Caida_Aterrizaje", caida.ClipActual.name);
            yield return Hasta(() => caida == null || caida.Actual == CaidaNova.Fase.Levantarse, "Levantarse", 3f);
            Assert.IsTrue(caida != null && caida.ClipActual.name == "E18_Caida_Levantarse");
            yield return Hasta(() => caida == null, "termina y devuelve el controlador", 4f);
            Assert.IsNull(objeto.GetComponent<CaidaNova>());
        }

        [UnityTest]
        public IEnumerator Caida_SiLoMatan_SeCorta()
        {
            CrearGuardia(new Vector3(0f, 0f, 60f));
            yield return null;
            CaidaNova caida = CaidaNova.Iniciar(objeto, false);
            Assert.IsNotNull(caida);
            objeto.GetComponentInChildren<VisualPersonaje>().Caido = true;
            yield return Hasta(() => caida == null, "se corta al morir", 1f);
        }

        [UnityTest]
        public IEnumerator SoldadoDeLaNave_JuegaCaidaAireMientrasBaja_YAterrizaAlTocarElPiso()
        {
            CrearGuardia(new Vector3(0f, 0f, 60f));
            var p = objeto.GetComponent<PandilleroAPie>();
            Assert.IsNotNull(p);
            ancla = new GameObject("AnclaNave");
            ancla.transform.position = new Vector3(0f, 8f, 60f);
            DescensoNova d = DescensoNova.Iniciar(p, ancla.transform, Vector3.zero, new Vector3(0f, 0f, 60f), 4f, null);
            Assert.IsNotNull(d.Caida, "hay secuencia de caída");
            Assert.AreEqual("E18_Caida_Aire", d.Caida.ClipActual.name, "baja con Caida_Aire");
            yield return Hasta(() => d.Llego, "toca el piso", 6f);
            Assert.IsTrue(d.Caida == null || d.Caida.Actual != CaidaNova.Fase.Aire, "al llegar ya no está en el aire");
        }
    }
}
