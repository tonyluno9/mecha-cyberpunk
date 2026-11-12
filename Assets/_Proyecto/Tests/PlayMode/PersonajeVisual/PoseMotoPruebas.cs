using System.Collections;
using MechaCyberpunk.Personaje.Visual;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests
{
    /// <summary>T-562: el conductor se sienta en la moto, las manos y pies siguen los puntos, baja y el caballete se esconde y vuelve.</summary>
    public class PoseMotoPruebas
    {
        private const string RutaPrefab = "Assets/_Proyecto/Personaje/Visual/Prefabs/Visual_woman.prefab";
        private GameObject raiz;
        private GameObject motoObjeto;
        private GameObject caballete;
        private PuntosMoto moto;
        private PoseMoto pose;
        private Animator animador;

        [SetUp]
        public void SetUp()
        {
            raiz = new GameObject("Jugador");
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab);
            VisualPersonaje visual = Object.Instantiate(prefab, raiz.transform).GetComponent<VisualPersonaje>();
            visual.LeerDelPadre = false;
            animador = visual.Animador;
            animador.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            pose = animador.gameObject.AddComponent<PoseMoto>();
            motoObjeto = new GameObject("Moto");
            motoObjeto.transform.position = new Vector3(5f, 0f, 3f);
            motoObjeto.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            caballete = new GameObject("Caballete");
            caballete.transform.SetParent(motoObjeto.transform, false);
            moto = motoObjeto.AddComponent<PuntosMoto>();
            moto.CrearPuntosPorDefecto();
            var so = new SerializedObject(moto);
            so.FindProperty("caballete").objectReferenceValue = caballete;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(raiz);
            Object.Destroy(motoObjeto);
        }

        [UnityTest]
        public IEnumerator Montar_SientaAlConductorEnElAsientoEscondeElCaballeteYBajarLoDevuelve()
        {
            yield return null;
            Transform visual = pose.GetComponentInParent<VisualPersonaje>().transform;
            Vector3 antes = visual.position;
            Assert.IsTrue(pose.Montar(moto));
            Assert.IsFalse(caballete.activeSelf, "el caballete se esconde al manejar");
            yield return new WaitForSeconds(pose.SegundosSubir + 0.3f);
            Assert.AreEqual(1f, pose.Avance, 0.01f);
            Transform cadera = animador.GetBoneTransform(HumanBodyBones.Hips);
            Assert.Less(Vector3.Distance(cadera.position, moto.Asiento.position), 0.15f, "cadera en el asiento");
            Assert.Less(Quaternion.Angle(visual.rotation, moto.Asiento.rotation), ReglasMoto.InclinacionAdelante + 1f, "mira al frente de la moto");
            Transform manoIzq = animador.GetBoneTransform(HumanBodyBones.LeftHand);
            Transform pieDer = animador.GetBoneTransform(HumanBodyBones.RightFoot);
            Assert.Less(Vector3.Distance(manoIzq.position, moto.ManubrioIzquierdo.position), 0.25f, "mano izquierda en el manubrio");
            Assert.Less(Vector3.Distance(pieDer.position, moto.EstriboDerecho.position), 0.25f, "pie derecho en el estribo");

            pose.Giro = 1f;
            pose.Velocidad = 20f;
            yield return new WaitForSeconds(0.8f);
            Assert.Greater(pose.Inclinacion, ReglasMoto.InclinacionMaxima * 0.8f, "se inclina a la derecha en la curva");

            pose.Bajar();
            yield return new WaitForSeconds(pose.SegundosBajar + 0.3f);
            Assert.IsFalse(pose.Montado);
            Assert.IsTrue(caballete.activeSelf, "el caballete vuelve");
            Assert.Less(Vector3.Distance(visual.position, antes), 0.01f, "el visual vuelve a su sitio de a pie");
        }

        [Test]
        public void Montar_ConUnaMotoSinPuntos_NoHaceNada()
        {
            var vacia = new GameObject("Vacia").AddComponent<PuntosMoto>();
            Assert.IsFalse(pose.Montar(vacia));
            Assert.IsFalse(pose.Montado);
            Object.Destroy(vacia.gameObject);
        }
    }
}
