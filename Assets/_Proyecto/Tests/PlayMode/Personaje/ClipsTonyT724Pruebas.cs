using System.Collections;
using System.Text;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Visual;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>
    /// T-724: al caminar y correr el jugador reproduce EXACTAMENTE Loco_Caminar y Loco_Correr de Tony (los de Movimiento_base), a su rapidez original,
    /// sin capas ni IK procedurales encima. Mide en el Animator vivo: estado y clip activos, rapidez del Animator, peso de las otras capas e IK.
    /// Con CAPTURAR_S1=1 deja docs/capturas/s1d-T-724-*.png.
    /// </summary>
    public class ClipsTonyT724Pruebas : BaseJugadorAPie
    {
        private VisualPersonaje Visual() => jugador.GetComponentInChildren<VisualPersonaje>(true);

        private static string Estado(Animator a)
        {
            AnimatorStateInfo actual = a.GetCurrentAnimatorStateInfo(0);
            foreach (string n in ReglasAnimacion.EstadosBase)
            {
                if (actual.IsName(n)) return n;
            }
            return "?";
        }

        private static string ClipsActivos(Animator a, int capa)
        {
            var sb = new StringBuilder();
            foreach (AnimatorClipInfo i in a.GetCurrentAnimatorClipInfo(capa))
            {
                if (i.weight > 0.01f) sb.Append(i.clip.name).Append('(').Append(i.weight.ToString("F2")).Append(") ");
            }
            return sb.ToString().Trim();
        }

        private static string Volcado(Animator a)
        {
            var sb = new StringBuilder();
            sb.Append("estado ").Append(Estado(a)).Append(" velocidadAnimator ").Append(a.speed.ToString("F3"));
            sb.Append(" rootMotion ").Append(a.applyRootMotion).Append(" controlador ").Append(a.runtimeAnimatorController.name);
            for (int capa = 0; capa < a.layerCount; capa++)
            {
                sb.Append("\n   capa ").Append(capa).Append(' ').Append(a.GetLayerName(capa)).Append(" peso ").Append(capa == 0 ? 1f : a.GetLayerWeight(capa))
                    .Append(" clips [").Append(ClipsActivos(a, capa)).Append(']');
            }
            return sb.ToString();
        }

        /// <summary>Todos los clips que suenan en alguna capa con peso de capa mayor que cero deben ser de locomoción de Tony.</summary>
        private static void AfirmarSoloLocomocionDeTony(Animator a, string esperado)
        {
            Assert.AreEqual(1f, a.speed, 0.001f, "El Animator reproduce el clip a su rapidez original (sin acelerar ni frenar)");
            Assert.IsFalse(a.applyRootMotion, "Sin movimiento de raíz");
            string base0 = ClipsActivos(a, 0);
            Assert.AreEqual(esperado + "(1.00)", base0, "La capa base reproduce solo " + esperado);
            for (int capa = 1; capa < a.layerCount; capa++)
            {
                if (a.GetLayerWeight(capa) < 0.001f) continue;
                string clips = ClipsActivos(a, capa);
                Assert.IsEmpty(clips, "La capa " + a.GetLayerName(capa) + " (peso " + a.GetLayerWeight(capa) + ") no pone clips encima: " + clips);
            }
            AnimationClip clip = a.GetCurrentAnimatorClipInfo(0)[0].clip;
            Assert.AreEqual("Assets/Arte/Personajes/PJ01/Clips/" + esperado + ".fbx", AssetDatabase.GetAssetPath(clip), "El clip es el de Blender aplicado a PJ01 (T-749)");
        }

        private void AfirmarSinProcedurales(VisualPersonaje visual)
        {
            IKPiesYMirada ik = visual.Animador.GetComponent<IKPiesYMirada>();
            if (ik != null)
            {
                Assert.AreEqual(0f, ik.PesoPies, 0.001f, "Sin IK de pies encima del clip");
                Assert.AreEqual(0f, ik.PesoMirada, 0.001f, "Sin mirada procedural encima del clip");
            }
            ApuntadoIK apuntado = visual.Animador.GetComponent<ApuntadoIK>();
            if (apuntado != null)
            {
                Assert.AreEqual(0f, apuntado.Peso, 0.001f, "Sin IK de apuntado al caminar");
            }
            Assert.AreEqual(Quaternion.identity, visual.Animador.transform.localRotation, "El modelo no se inclina ni se gira por encima del clip");
        }

        private void Capturar(string nombre)
        {
            if (!CapturaS1.Activa) return;
            GameObject camObj = new GameObject("CamT724");
            Camera cam = camObj.AddComponent<Camera>();
            cam.nearClipPlane = 0.05f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.25f, 0.28f, 0.35f);
            Vector3 centro = jugador.transform.position + Vector3.up * 0.9f;
            camObj.transform.position = centro + jugador.transform.right * 3.2f + Vector3.up * 0.2f;
            camObj.transform.LookAt(centro);
            CapturaS1.FotoCamara(nombre, cam);
            Object.Destroy(camObj);
        }

        [UnityTest]
        public IEnumerator Caminar_ConW_ReproduceExactamenteLocoCaminar_SinCapasNiIKEncima()
        {
            yield return Calentar();
            VisualPersonaje visual = Visual();
            Animator a = visual.Animador;
            a.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            yield return Esperar(0.6f);
            Press(teclado.wKey);
            yield return Esperar(1.3f);
            Debug.Log("[T724] caminando: " + Volcado(a));
            Assert.AreEqual("Caminar", Estado(a));
            AfirmarSoloLocomocionDeTony(a, "PJ01_Caminar_SinImplante");
            AfirmarSinProcedurales(visual);
            Capturar("s1d-T-724-caminar");
            Release(teclado.wKey);
        }

        [UnityTest]
        public IEnumerator Correr_ConShift_ReproduceExactamenteLocoCorrer_SinCapasNiIKEncima()
        {
            yield return Calentar();
            VisualPersonaje visual = Visual();
            Animator a = visual.Animador;
            a.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            yield return Esperar(0.6f);
            Press(teclado.wKey);
            Press(teclado.leftShiftKey);
            yield return Esperar(1.6f);
            Debug.Log("[T724] corriendo: " + Volcado(a));
            Assert.AreEqual("Correr", Estado(a));
            AfirmarSoloLocomocionDeTony(a, "PJ01_Correr_SinImplante");
            AfirmarSinProcedurales(visual);
            Capturar("s1d-T-724-correr");
            Release(teclado.leftShiftKey);
            Release(teclado.wKey);
        }

        [UnityTest]
        public IEnumerator Caminar_ConPistolaSinApuntar_SigueSiendoLocoCaminar()
        {
            yield return Calentar();
            VisualPersonaje visual = Visual();
            Animator a = visual.Animador;
            a.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            jugador.GetComponent<ArmasAPie>().Equipar(ArmaAPie.Pistola);
            yield return Esperar(0.6f);
            Press(teclado.wKey);
            yield return Esperar(1.3f);
            Debug.Log("[T724] caminando con pistola: " + Volcado(a));
            AfirmarSoloLocomocionDeTony(a, "PJ01_Caminar_SinImplante");
            AfirmarSinProcedurales(visual);
            Release(teclado.wKey);
        }
    }
}
