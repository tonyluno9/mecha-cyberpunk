using System.Collections;
using System.Linq;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Combate;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>
    /// T-545 (plan de la noche 3-nov, paso 3): la pistola W01 de Tony mide unos 20 cm, está en la mano del jugador y de los SKULLS y es la que se
    /// tira en el piso; al disparar la corredera va atrás y regresa, y al recargar sale el cargador y entra otro en lo que dura la recarga.
    /// </summary>
    public class PistolaW01Pruebas
    {
        private const string RutaPrefab = "Assets/Arte/Armas/W01_Pistola/W01_Pistola.prefab";

        [Test]
        public void Prefab_DeVeinteCentimetros_EnLaManoDelJugadorYDeLosSkulls()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab);
            Assert.IsNotNull(prefab);
            Bounds b = prefab.GetComponentsInChildren<Renderer>().Select(r => r.bounds).Aggregate((a, c) => { a.Encapsulate(c); return a; });
            Assert.That(Mathf.Max(b.size.x, b.size.z), Is.InRange(0.17f, 0.24f), "Unos 20 cm de largo");
            Assert.IsNotNull(prefab.GetComponent<AnimacionPistola>().Corredera, "Corredera animada");
            GameObject jugador = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab");
            Transform pistola = jugador.GetComponentsInChildren<Transform>(true).First(t => t.name == "Pistola" && t.Find("Canon") != null);
            Assert.IsNotNull(pistola.Find("W01"), "El jugador usa la W01");
            Assert.IsFalse(pistola.Find("Canon").GetComponent<Renderer>().enabled, "Sin la pistola de cubos");
            foreach (string ruta in new[] { "Assets/_Proyecto/IA/Prefabs/PandilleroArmado_Skulls.prefab", "Assets/_Proyecto/IA/Prefabs/PandilleroArmado_JefeSkulls.prefab" })
            {
                GameObject skull = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
                Assert.IsTrue(skull.GetComponentsInChildren<Transform>(true).Any(t => t.name == "W01" && t.parent.name == "ManoDer"), ruta);
            }
        }

        [UnityTest]
        public IEnumerator Disparo_LaCorrederaVaAtrasYRegresa_Recarga_CambiaElCargador()
        {
            GameObject w = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab));
            try
            {
                AnimacionPistola anim = w.GetComponent<AnimacionPistola>();
                yield return null;
                Vector3 reposo = anim.Corredera.localPosition;
                anim.Disparar();
                yield return new WaitForSeconds(0.08f);
                Assert.Greater(Vector3.Distance(anim.Corredera.localPosition, reposo), 0.01f, "La corredera fue para atrás");
                yield return new WaitForSeconds(0.5f);
                Assert.IsFalse(anim.Disparando);
                Assert.Less(Vector3.Distance(anim.Corredera.localPosition, reposo), 0.0005f, "Regresó");

                Transform cargador = w.GetComponentsInChildren<Transform>(true).First(t => t.name == "Bateria");
                anim.Recargar(1f);
                yield return new WaitForSeconds(0.7f);
                Assert.IsFalse(cargador.gameObject.activeSelf, "El cargador viejo salió");
                Assert.IsTrue(w.GetComponentsInChildren<Transform>(true).First(t => t.name == "Bateria_Nueva").gameObject.activeSelf, "Entra uno nuevo");
                yield return new WaitForSeconds(0.5f);
                Assert.IsFalse(anim.Recargando, "La animación dura lo que la recarga");
                Assert.IsTrue(cargador.gameObject.activeSelf);
            }
            finally
            {
                Object.Destroy(w);
            }
        }

        [UnityTest]
        public IEnumerator PistolaDeSkull_SeAnimaConElDisparoQueSuenaEnSuBoca()
        {
            EventosRuido.LimpiarSuscriptores();
            GameObject w = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab), new Vector3(5f, 1f, 5f), Quaternion.identity);
            try
            {
                AnimacionPistola anim = w.GetComponent<AnimacionPistola>();
                yield return null;
                EventosRuido.PublicarDisparo(new Vector3(30f, 1f, 5f));
                Assert.IsFalse(anim.Disparando, "Un disparo lejano no es de ella");
                EventosRuido.PublicarDisparo(w.transform.position + Vector3.forward * 0.3f);
                Assert.IsTrue(anim.Disparando);
            }
            finally
            {
                Object.Destroy(w);
                EventosRuido.LimpiarSuscriptores();
            }
        }

        [Test]
        public void CurvaCorredera_ComoElBlend()
        {
            Assert.AreEqual(0f, AnimacionPistola.CurvaCorredera(1f, 1f), 0.001f);
            Assert.AreEqual(1f, AnimacionPistola.CurvaCorredera(2f, 1f), 0.001f);
            Assert.AreEqual(1f, AnimacionPistola.CurvaCorredera(3f, 1f), 0.001f);
            Assert.AreEqual(0f, AnimacionPistola.CurvaCorredera(6f, 1f), 0.001f);
        }
    }
}
