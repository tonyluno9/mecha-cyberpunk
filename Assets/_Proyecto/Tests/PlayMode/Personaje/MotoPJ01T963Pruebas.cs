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
    /// T-963 (A1): el jugador sube, maneja y baja de la moto C19 con los clips de Blender (MotoSubir, MotoManejar, MotoBajar). Sentado: cadera en el asiento, manos en el
    /// manubrio y pies en los estribos; al bajar el visual vuelve a los pies del jugador. Capturas docs/capturas/pj01-moto-*.png (se comparan con 02_vista_subir_moto.png).
    /// </summary>
    public class MotoPJ01T963Pruebas : BaseJugadorAPie
    {
        private GameObject moto;

        public override void TearDown()
        {
            if (moto != null) { UnityEngine.Object.Destroy(moto); }
            base.TearDown();
        }

        private static bool EnEstado(Animator a, string nombre) => a.GetCurrentAnimatorStateInfo(0).IsName(nombre);

        private static IEnumerator Hasta(Func<bool> condicion, string que, float limite = 6f)
        {
            float fin = Time.time + limite;
            while (!condicion() && Time.time < fin) { yield return null; }
            Assert.IsTrue(condicion(), "No llegó: " + que);
        }

        private static readonly string[] Motos = { "C19", "V07" };

        [UnityTest]
        public IEnumerator Moto_SubeManejaYBaja_ConLosClipsDeBlender([ValueSource(nameof(Motos))] string codigo)
        {
            yield return Calentar();
            Animator a = jugador.GetComponentInChildren<VisualPersonaje>(true).Animador;
            a.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            moto = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Proyecto/Vehiculos/Prefabs/Carro_" + codigo + ".prefab"), new Vector3(0f, 0.3f, 6f), Quaternion.identity);
            IMontable montable = null;
            foreach (IMontable m in Registro<IMontable>.Todos) { if (m is Component c && c.gameObject == moto) { montable = m; } }
            Assert.IsNotNull(montable, "La moto se registra");
            PuntosMoto puntos = moto.GetComponent<PuntosMoto>();
            yield return Esperar(1f);
            jugadorAPie.Colocar(new Pose(montable.PuntoSubida + new Vector3(-0.8f, 0.05f, 0f), Quaternion.LookRotation(Vector3.right)));
            yield return Esperar(0.3f);

            var cam = new GameObject("CamT963Moto").AddComponent<Camera>();
            cam.fieldOfView = 40f;
            Vector3 centro = puntos.Asiento.position;
            cam.transform.position = centro - moto.transform.right * 4f + moto.transform.forward * 1.2f + Vector3.up * 0.4f;
            cam.transform.LookAt(centro);

            Assert.IsTrue(jugadorAPie.Subir(montable), "sube");
            PoseMoto pose = a.GetComponent<PoseMoto>();
            Assert.IsNotNull(pose);
            Assert.IsTrue(pose.ConClips, "usa los clips de Blender");
            yield return Hasta(() => EnEstado(a, "MotoSubir"), "MotoSubir");
            yield return Esperar(pose.SegundosSubir * 0.45f);
            yield return AnimacionCarroPruebas.Capturar(cam, "docs/capturas/pj01-moto-" + codigo + "-subiendo.png");
            yield return Hasta(() => EnEstado(a, "MotoManejar"), "MotoManejar", 4f);
            yield return Esperar(0.5f);
            yield return AnimacionCarroPruebas.Capturar(cam, "docs/capturas/pj01-moto-" + codigo + "-manejando.png");
            cam.transform.position = centro - moto.transform.forward * 3.5f + moto.transform.right * 1.5f + Vector3.up * 0.8f;
            cam.transform.LookAt(centro);
            yield return AnimacionCarroPruebas.Capturar(cam, "docs/capturas/pj01-moto-" + codigo + "-atras.png");

            float cadera = Vector3.Distance(a.GetBoneTransform(HumanBodyBones.Hips).position, puntos.Asiento.position);
            float manoI = Vector3.Distance(a.GetBoneTransform(HumanBodyBones.LeftHand).position, puntos.ManubrioIzquierdo.position);
            float manoD = Vector3.Distance(a.GetBoneTransform(HumanBodyBones.RightHand).position, puntos.ManubrioDerecho.position);
            float pieI = Vector3.Distance(a.GetBoneTransform(HumanBodyBones.LeftFoot).position, puntos.EstriboIzquierdo.position);
            float pieD = Vector3.Distance(a.GetBoneTransform(HumanBodyBones.RightFoot).position, puntos.EstriboDerecho.position);
            Transform raizV = jugador.GetComponentInChildren<VisualPersonaje>(true).transform;
            Debug.Log("[Prueba] T-963 moto diag: cadera real en visual " + raizV.InverseTransformPoint(a.GetBoneTransform(HumanBodyBones.Hips).position).ToString("F3") + ", esperada " + pose.CaderaSentado.ToString("F3")
                + ", asiento en visual " + raizV.InverseTransformPoint(puntos.Asiento.position).ToString("F3") + ", animador local " + a.transform.localPosition.ToString("F3") + " " + a.transform.localEulerAngles.ToString("F1")
                + ", escala " + raizV.lossyScale.ToString("F3"));
            Debug.Log("[Prueba] T-963 moto: cadera " + cadera.ToString("F2") + ", manos " + manoI.ToString("F2") + "/" + manoD.ToString("F2") + ", pies " + pieI.ToString("F2") + "/" + pieD.ToString("F2") + " m.");
            Assert.Less(cadera, 0.15f, "cadera en el asiento");
            Assert.Less(Mathf.Max(manoI, manoD), 0.1f, "manos en el manubrio");
            Assert.Less(Mathf.Max(pieI, pieD), 0.1f, "pies en los estribos");
            Assert.Greater(a.GetBoneTransform(HumanBodyBones.Head).position.y, puntos.ManubrioIzquierdo.position.y, "la cabeza queda sobre el manubrio");

            Press(teclado.eKey);
            yield return null;
            Release(teclado.eKey);
            yield return Hasta(() => EnEstado(a, "MotoBajar"), "MotoBajar");
            cam.transform.position = centro - moto.transform.right * 4f + moto.transform.forward * 1.2f + Vector3.up * 0.4f;
            cam.transform.LookAt(centro);
            yield return Esperar(pose.SegundosBajar * 0.5f);
            yield return AnimacionCarroPruebas.Capturar(cam, "docs/capturas/pj01-moto-" + codigo + "-bajando.png");
            AnimacionCarro anim = jugador.GetComponent<AnimacionCarro>();
            yield return Hasta(() => anim.Fase == AnimacionCarro.FaseCarro.Fuera, "fuera", 4f);
            yield return Hasta(() => EnEstado(a, "Idle"), "Idle al bajar", 3f);
            Transform visual = jugador.GetComponentInChildren<VisualPersonaje>(true).transform;
            Assert.Less(Vector3.Distance(visual.position, jugador.transform.position), 0.05f, "el visual vuelve a los pies del jugador");
            yield return Esperar(0.3f);
            yield return AnimacionCarroPruebas.Capturar(cam, "docs/capturas/pj01-moto-" + codigo + "-abajo.png");
            UnityEngine.Object.Destroy(cam.gameObject);
        }
    }
}
