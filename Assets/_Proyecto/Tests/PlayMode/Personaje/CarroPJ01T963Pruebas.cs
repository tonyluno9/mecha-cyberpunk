using System;
using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Visual;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>
    /// T-963 (A1): el jugador sube, maneja y baja del V01a con los clips de Blender (PJ01_Carro_*). Sentado, la cadera queda en el asiento, ningún punto del cuerpo
    /// baja del piso del carro ni la cabeza pasa el techo; las capturas (docs/capturas/pj01-carro-*.png) se comparan con 02_vista_carro_*.png.
    /// </summary>
    public class CarroPJ01T963Pruebas : BaseJugadorAPie
    {
        private const string RutaCarro = "Assets/_Proyecto/Vehiculos/Prefabs/Carro_V01a.prefab";
        private GameObject carro;

        public override void TearDown()
        {
            if (carro != null) { UnityEngine.Object.Destroy(carro); }
            base.TearDown();
        }

        private static bool EnEstado(Animator a, string nombre) => a.GetCurrentAnimatorStateInfo(0).IsName(nombre);

        private static IEnumerator Hasta(Func<bool> condicion, string que, float limite = 6f)
        {
            float fin = Time.time + limite;
            while (!condicion() && Time.time < fin) { yield return null; }
            Assert.IsTrue(condicion(), "No llegó: " + que);
        }

        private static float TechoDelCarro(GameObject c)
        {
            float techo = float.NegativeInfinity;
            foreach (BoxCollider caja in c.GetComponentsInChildren<BoxCollider>(true))
            {
                if (!caja.isTrigger) { techo = Mathf.Max(techo, caja.bounds.max.y); }
            }
            return techo;
        }

        private void MostrarCarroceria(bool ver)
        {
            foreach (Renderer r in carro.GetComponentsInChildren<Renderer>(true))
            {
                string n = r.name.ToLower();
                if (!n.Contains("rueda") && !n.Contains("wheel") && !n.Contains("llanta")) { r.enabled = ver; }
            }
        }

        [UnityTest]
        public IEnumerator Carro_SubeManejaYBaja_ConLosClipsDeBlender()
        {
            yield return Calentar();
            carro = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaCarro), new Vector3(0f, 0f, 4f), Quaternion.identity);
            yield return Esperar(0.5f);
            IMontable real = null;
            foreach (IMontable m in Registro<IMontable>.Todos) { if (m.Asiento != null && m.Asiento.IsChildOf(carro.transform)) { real = m; } }
            Assert.IsNotNull(real, "El V01a se registra como montable");
            jugador.transform.position = real.PuntoSubida + (real.PuntoSubida - real.Asiento.position).normalized * 0.5f;
            yield return null;
            VisualPersonaje visual = jugador.GetComponentInChildren<VisualPersonaje>(true);
            Animator a = visual.Animador;
            AnimacionCarro anim = jugador.GetComponent<AnimacionCarro>();
            Assert.IsTrue(jugadorAPie.Subir(real));

            Vector3 lado = Vector3.ProjectOnPlane(real.PuntoSubida - real.Asiento.position, Vector3.up).normalized;
            var cam = new GameObject("CamT963").AddComponent<Camera>();
            cam.fieldOfView = 40f;
            Vector3 centro = real.Asiento.position;
            cam.transform.position = centro + lado * 4.2f + Vector3.up * 0.5f;
            cam.transform.LookAt(centro + Vector3.up * 0.1f);

            yield return Hasta(() => EnEstado(a, "CarroEntrar"), "CarroEntrar");
            AnimatorClipInfo[] clips = a.GetCurrentAnimatorClipInfo(0);
            Assert.AreEqual("PJ01_Carro_Entrar", clips[clips.Length - 1].clip.name);
            yield return Esperar(0.6f);
            yield return AnimacionCarroPruebas.Capturar(cam, "docs/capturas/pj01-carro-entrando.png");
            yield return Hasta(() => anim.Fase == AnimacionCarro.FaseCarro.Dentro, "dentro", 8f);
            yield return Hasta(() => EnEstado(a, "CarroManejar"), "CarroManejar");
            yield return Esperar(0.5f);
            clips = a.GetCurrentAnimatorClipInfo(0);
            Assert.AreEqual("PJ01_Carro_Manejar", clips[clips.Length - 1].clip.name);
            yield return AnimacionCarroPruebas.Capturar(cam, "docs/capturas/pj01-carro-lado.png");
            MostrarCarroceria(false);
            yield return AnimacionCarroPruebas.Capturar(cam, "docs/capturas/pj01-carro-lado-sin-carroceria.png");
            cam.transform.position = centro - carro.transform.forward * 4.5f + Vector3.up * 0.9f;
            cam.transform.LookAt(centro + Vector3.up * 0.2f);
            MostrarCarroceria(true);
            yield return AnimacionCarroPruebas.Capturar(cam, "docs/capturas/pj01-carro-atras.png");

            Vector3 cadera = a.GetBoneTransform(HumanBodyBones.Hips).position;
            Vector3 cabeza = a.GetBoneTransform(HumanBodyBones.Head).position;
            float bajo = float.MaxValue;
            var malla = new Mesh();
            foreach (SkinnedMeshRenderer r in a.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if (!r.gameObject.activeInHierarchy) { continue; }
                r.BakeMesh(malla, true);
                Matrix4x4 m = Matrix4x4.TRS(r.transform.position, r.transform.rotation, Vector3.one);
                foreach (Vector3 v in malla.vertices) { bajo = Mathf.Min(bajo, m.MultiplyPoint3x4(v).y); }
            }
            UnityEngine.Object.Destroy(malla);
            float techo = TechoDelCarro(carro);
            Debug.Log("[Prueba] T-963 sentado: cadera a " + Vector3.Distance(cadera, real.Asiento.position).ToString("F2") + " m del asiento (dy " + (cadera.y - real.Asiento.position.y).ToString("F2")
                + "), punto más bajo " + (bajo - carro.transform.position.y).ToString("F2") + " m sobre el piso, cabeza " + (techo - cabeza.y).ToString("F2") + " m bajo el techo.");
            Assert.Less(Vector3.Distance(cadera, real.Asiento.position), 0.6f, "la cadera queda en el asiento");
            Assert.Greater(bajo, carro.transform.position.y + 0.05f, "los pies no atraviesan el piso del carro");
            Assert.Less(cabeza.y, techo, "la cabeza no pasa el techo");

            Assert.IsTrue(jugadorAPie.Bajar());
            cam.transform.position = centro + lado * 4.2f + Vector3.up * 0.5f;
            cam.transform.LookAt(centro + Vector3.up * 0.1f);
            yield return Hasta(() => EnEstado(a, "CarroSalir"), "CarroSalir");
            clips = a.GetCurrentAnimatorClipInfo(0);
            Assert.AreEqual("PJ01_Carro_Salir", clips[clips.Length - 1].clip.name);
            yield return Esperar(0.7f);
            yield return AnimacionCarroPruebas.Capturar(cam, "docs/capturas/pj01-carro-saliendo.png");
            yield return Hasta(() => anim.Fase == AnimacionCarro.FaseCarro.Fuera, "fuera", 8f);
            yield return Hasta(() => EnEstado(a, "Idle"), "Idle al bajar");
            UnityEngine.Object.Destroy(cam.gameObject);
        }
    }
}
