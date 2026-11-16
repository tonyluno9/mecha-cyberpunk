using System;
using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Personaje.Visual;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>T-1004: un peatón, un pandillero, un guardia de Nova y una policía caminan, y el clip que suena es de PJ01. Capturas con CAPTURAR_S1=1.</summary>
    public class NpcCaminanConClipsPJ01T1004Pruebas
    {
        private const float VelocidadCaminar = 1.3f;
        private const float SegundosParaEstabilizar = 0.8f;
        private const float LimiteSegundos = 4f;
        private const float DistanciaCamara = 3.4f;
        private const float AlturaCamara = 0.3f;
        private const float GradosLado = 90f;
        private const string ClipCaminar = "PJ01_Caminar_SinImplante";
        private readonly List<GameObject> creados = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject g in creados) { if (g != null) { Object.Destroy(g); } }
            creados.Clear();
        }

        private static List<string> ClipsQueSuenan(Animator a)
        {
            var nombres = new List<string>();
            foreach (AnimatorClipInfo c in a.GetCurrentAnimatorClipInfo(0))
            {
                if (c.clip != null && c.weight > 0f) { nombres.Add(c.clip.name); }
            }
            return nombres;
        }

        private IEnumerator CaminaConClipDePJ01(string ruta, string captura)
        {
            var suelo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            suelo.transform.position = new Vector3(0f, -0.5f, 0f);
            suelo.transform.localScale = new Vector3(40f, 1f, 40f);
            creados.Add(suelo);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
            Assert.IsNotNull(prefab, ruta);
            GameObject go = Object.Instantiate(prefab, Vector3.zero, Quaternion.LookRotation(Vector3.back));
            creados.Add(go);
            VisualPersonaje visual = go.GetComponentInChildren<VisualPersonaje>(true);
            Assert.IsNotNull(visual, "Tiene visual: " + ruta);
            visual.LeerDelPadre = false;
            visual.Velocidad = VelocidadCaminar;
            Animator a = visual.Animador;
            a.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            float fin = Time.time + LimiteSegundos;
            while (Time.time < fin && !ClipsQueSuenan(a).Contains(ClipCaminar)) { yield return null; }
            CollectionAssert.Contains(ClipsQueSuenan(a), ClipCaminar, "Suena el clip de caminar de PJ01 en " + ruta + " (controller " + (a.runtimeAnimatorController != null ? a.runtimeAnimatorController.name : "null") + ", avatar " + (a.avatar != null ? a.avatar.name : "null") + ", activo " + a.isActiveAndEnabled + ")");
            foreach (string clip in ClipsQueSuenan(a)) { StringAssert.StartsWith("PJ01_", clip, "Solo clips de PJ01 en " + ruta); }
            float hasta = Time.time + SegundosParaEstabilizar;
            while (Time.time < hasta) { yield return null; }
            if (Environment.GetEnvironmentVariable("CAPTURAR_S1") == "1")
            {
                yield return Capturar(go, captura + "-frente", 0f);
                yield return Capturar(go, captura + "-lado", GradosLado);
            }
        }

        private IEnumerator Capturar(GameObject objetivo, string nombre, float grados)
        {
            var cam = new GameObject("CamNpc").AddComponent<Camera>();
            creados.Add(cam.gameObject);
            Vector3 centro = objetivo.transform.position + Vector3.up;
            Vector3 direccion = Quaternion.Euler(0f, grados, 0f) * objetivo.transform.forward;
            cam.transform.position = centro + direccion * DistanciaCamara + Vector3.up * AlturaCamara;
            cam.transform.LookAt(centro);
            yield return AnimacionCarroPruebas.Capturar(cam, "docs/capturas/t1004/" + nombre + ".png");
        }

        [UnityTest]
        public IEnumerator Peaton_CaminaConClipDePJ01()
        {
            yield return CaminaConClipDePJ01("Assets/_Proyecto/Trafico/Prefabs/Peaton.prefab", "peaton");
        }

        [UnityTest]
        public IEnumerator Pandillero_CaminaConClipDePJ01()
        {
            yield return CaminaConClipDePJ01("Assets/_Proyecto/IA/Prefabs/PandilleroArmado_Skulls.prefab", "pandillero");
        }

        [UnityTest]
        public IEnumerator GuardiaNova_CaminaConClipDePJ01()
        {
            yield return CaminaConClipDePJ01("Assets/_Proyecto/Personaje/Visual/Prefabs/Visual_GuardiaNova.prefab", "guardia");
        }

        [UnityTest]
        public IEnumerator Policia_CaminaConClipDePJ01()
        {
            yield return CaminaConClipDePJ01("Assets/_Proyecto/IA/Prefabs/PoliciaAPie.prefab", "policia");
        }
    }
}
