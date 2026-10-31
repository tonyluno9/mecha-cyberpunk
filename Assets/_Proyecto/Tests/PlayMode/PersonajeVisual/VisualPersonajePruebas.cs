using System.Collections;
using MechaCyberpunk.Personaje.Visual;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests
{
    public class VisualPersonajePruebas
    {
        private GameObject padre;
        private GameObject suelo;
        private int fpsAnterior;
        private int vsyncAnterior;

        [SetUp]
        public void SetUp()
        {
            // En batchmode los cuadros salen a miles por segundo: se fija 60 fps para que el movimiento por cuadro sea realista.
            fpsAnterior = Application.targetFrameRate;
            vsyncAnterior = QualitySettings.vSyncCount;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
        }

        [TearDown]
        public void TearDown()
        {
            Application.targetFrameRate = fpsAnterior;
            QualitySettings.vSyncCount = vsyncAnterior;
            if (suelo != null)
            {
                Object.Destroy(suelo);
            }
            if (padre != null)
            {
                Object.Destroy(padre);
            }
        }

        private static GameObject CargarPrefab()
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Proyecto/Personaje/Visual/Prefabs/Visual_Prueba.prefab");
#else
            return null;
#endif
        }

        private VisualPersonaje Crear()
        {
            GameObject prefab = CargarPrefab();
            Assert.IsNotNull(prefab, "Falta Visual_Prueba.prefab (menu Mecha Cyberpunk/Personajes/Crear visual de prueba)");
            padre = new GameObject("PadrePrueba");
            padre.AddComponent<CharacterController>();
            GameObject suelo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            suelo.transform.SetParent(padre.transform.parent);
            suelo.transform.position = new Vector3(0f, -1.5f, 0f);
            suelo.transform.localScale = new Vector3(100f, 1f, 100f);
            this.suelo = suelo;
            GameObject visual = Object.Instantiate(prefab, padre.transform);
            return visual.GetComponent<VisualPersonaje>();
        }

        [UnityTest]
        public IEnumerator Mover_ElPadre_SubeVelocidadYPasaACorrer()
        {
            VisualPersonaje v = Crear();
            CharacterController cc = padre.GetComponent<CharacterController>();
            yield return null;
            Assert.Less(v.Animador.GetFloat("Velocidad"), 0.1f);

            float fin = Time.time + 1.5f;
            while (Time.time < fin)
            {
                cc.Move((Vector3.forward * 6f + Vector3.down * 2f) * Time.deltaTime);
                yield return null;
            }

            Assert.Greater(v.Animador.GetFloat("Velocidad"), 4f, "Velocidad del Animator");
            Assert.IsTrue(v.Animador.GetCurrentAnimatorStateInfo(0).IsName("Correr") || v.Animador.GetNextAnimatorStateInfo(0).IsName("Correr"),
                "estado actual no es Correr");
        }

        [UnityTest]
        public IEnumerator Propiedades_Apuntar_Disparar_Cubrirse_LlegaAlAnimator()
        {
            VisualPersonaje v = Crear();
            v.LeerDelPadre = false;
            v.TipoArma = TipoArmaVisual.Pistola;
            v.Apuntando = true;
            yield return null;
            yield return null;
            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(1, v.Animador.GetInteger("TipoArma"));
            Assert.IsTrue(v.Animador.GetBool("Apuntando"));
            Assert.IsTrue(v.Animador.GetCurrentAnimatorStateInfo(0).IsName("ApuntarPistola"));

            v.Disparar();
            yield return null;
            yield return null;
            Assert.IsTrue(v.Animador.GetCurrentAnimatorStateInfo(0).IsName("DispararPistola") || v.Animador.GetNextAnimatorStateInfo(0).IsName("DispararPistola"));

            v.Apuntando = false;
            v.Cubierto = true;
            yield return new WaitForSeconds(0.8f);
            Assert.IsTrue(v.Animador.GetCurrentAnimatorStateInfo(0).IsName("Cubrirse"));
        }

        [UnityTest]
        public IEnumerator SinCharacterController_LeePosicionDelPadre()
        {
            VisualPersonaje v = Crear();
            Object.DestroyImmediate(padre.GetComponent<CharacterController>());
            // El visual cacheó el CharacterController en Awake: se recrea el visual para que lea sin él.
            Object.Destroy(v.gameObject);
            yield return null;
            GameObject prefab = CargarPrefab();
            VisualPersonaje nuevo = Object.Instantiate(prefab, padre.transform).GetComponent<VisualPersonaje>();
            yield return null;
            float fin = Time.time + 0.8f;
            while (Time.time < fin)
            {
                padre.transform.position += Vector3.right * 5f * Time.deltaTime;
                yield return null;
            }
            Assert.Greater(nuevo.Velocidad, 3f);
        }
    }
}
