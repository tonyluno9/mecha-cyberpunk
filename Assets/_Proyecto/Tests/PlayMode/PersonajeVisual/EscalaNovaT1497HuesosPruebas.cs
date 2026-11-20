using System.Collections;
using MechaCyberpunk.Personaje.Visual;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests
{
    /// <summary>
    /// T-1497: con el Animator corriendo, la altura de cada agente de Nova medida con los huesos (cabeza menos el pie más bajo) cae donde debe según su altura real:
    /// el hueso de la cabeza queda en la nuca (~0.87 de la altura) y el tobillo a ~0.05, así que esa distancia vale entre 0.70 y 0.95 de la altura objetivo.
    /// </summary>
    public class EscalaNovaT1497HuesosPruebas
    {
        private const float FraccionMinima = 0.70f;
        private const float FraccionMaxima = 0.95f;

        public static readonly string[] Variantes = { "E15_CentinelaNova_a", "E16_RobotPesadoNova_a", "E18_EliteNova_a" };

        private GameObject piso;
        private GameObject agente;

        [SetUp]
        public void Preparar()
        {
            piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(50f, 1f, 50f);
        }

        [TearDown]
        public void Limpiar()
        {
            if (agente != null) { Object.DestroyImmediate(agente); }
            Object.DestroyImmediate(piso);
        }

        [UnityTest]
        public IEnumerator Agente_MideSuAlturaReal_PorHuesos([ValueSource(nameof(Variantes))] string variante)
        {
            GameObject prefab = BuscarVisual(variante);
            Assert.IsNotNull(prefab, "Visual_" + variante);
            agente = Object.Instantiate(prefab, Vector3.zero, Quaternion.identity);
            Animator animador = agente.GetComponentInChildren<Animator>(true);
            Assert.IsNotNull(animador);
            animador.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            Assert.IsTrue(animador.isHuman, variante + ": avatar humanoide");
            for (int i = 0; i < 20; i++) { yield return null; }
            Transform cabeza = animador.GetBoneTransform(HumanBodyBones.Head);
            Transform pieI = animador.GetBoneTransform(HumanBodyBones.LeftFoot);
            Transform pieD = animador.GetBoneTransform(HumanBodyBones.RightFoot);
            Assert.IsNotNull(cabeza, "hueso de la cabeza");
            Assert.IsNotNull(pieI, "pie izquierdo");
            Assert.IsNotNull(pieD, "pie derecho");
            float alto = cabeza.position.y - Mathf.Min(pieI.position.y, pieD.position.y);
            float objetivo = EscalasNova.Altura(variante);
            Debug.Log("[T-1497] " + variante + ": cabeza-pie " + alto.ToString("F2") + " m de " + objetivo + " m");
            Assert.GreaterOrEqual(alto, objetivo * FraccionMinima, variante + ": cabeza-pie " + alto);
            Assert.LessOrEqual(alto, objetivo * FraccionMaxima, variante + ": cabeza-pie " + alto);
        }

        private static GameObject BuscarVisual(string variante)
        {
            foreach (string guid in AssetDatabase.FindAssets("Visual_" + variante + " t:Prefab"))
            {
                string ruta = AssetDatabase.GUIDToAssetPath(guid);
                if (System.IO.Path.GetFileNameWithoutExtension(ruta) == "Visual_" + variante)
                {
                    return AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
                }
            }
            return null;
        }
    }
}
