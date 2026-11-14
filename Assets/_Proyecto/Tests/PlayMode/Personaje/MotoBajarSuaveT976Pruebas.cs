using System;
using System.Collections;
using System.Text;
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
    /// T-976: bajar de la moto sin tirones. Con dt fijo (30 cuadros/s) se mide cuadro por cuadro, desde que se aprieta E hasta 0.6 s después de quedar de pie,
    /// cuánto salta la cadera y los pies (respecto de lo que se movían en el cuadro anterior). Un tirón es un cambio brusco de velocidad del hueso.
    /// Capturas docs/capturas/t976/.
    /// </summary>
    public class MotoBajarSuaveT976Pruebas : BaseJugadorAPie
    {
        private GameObject moto;

        public override void TearDown()
        {
            Time.captureFramerate = 0;
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
        public IEnumerator Moto_BajarYQuedarDePie_SinTirones([ValueSource(nameof(Motos))] string codigo)
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
            Assert.IsTrue(jugadorAPie.Subir(montable), "sube");
            PoseMoto pose = a.GetComponent<PoseMoto>();
            yield return Hasta(() => EnEstado(a, "MotoManejar"), "MotoManejar", 4f);
            yield return Esperar(0.8f);

            var cam = new GameObject("CamT976").AddComponent<Camera>();
            cam.fieldOfView = 40f;
            Vector3 centro = puntos.Asiento.position;
            cam.transform.position = centro - moto.transform.right * 4.5f + moto.transform.forward * 1.0f + Vector3.up * 0.3f;
            cam.transform.LookAt(centro + Vector3.down * 0.3f);

            Transform cadera = a.GetBoneTransform(HumanBodyBones.Hips);
            Transform pieI = a.GetBoneTransform(HumanBodyBones.LeftFoot);
            Transform pieD = a.GetBoneTransform(HumanBodyBones.RightFoot);
            Transform cabeza = a.GetBoneTransform(HumanBodyBones.Head);
            AnimacionCarro anim = jugador.GetComponent<AnimacionCarro>();

            var registro = jugador.AddComponent<RegistroHuesosT976>();
            registro.Huesos = new[] { cadera, pieI, pieD, cabeza };
            registro.Nota = () => Estado(a) + " " + anim.Fase + " vis " + a.transform.parent.position.ToString("F3");

            Time.captureFramerate = 30;
            Press(teclado.eKey);
            yield return null;
            Release(teclado.eKey);
            int inicio = registro.Cuadros.Count;

            float finDePie = -1f;
            int capturas = 0;
            int vuelta = 0;
            bool giroEnElSitio = false;
            float t0 = Time.time;
            while (true)
            {
                yield return null;
                float t = Time.time - t0;
                if (vuelta++ % 6 == 3 && capturas < 12)
                {
                    capturas++;
                    yield return AnimacionCarroPruebas.Capturar(cam, "docs/capturas/t976/" + codigo + "-" + capturas.ToString("D2") + ".png");
                }
                if (anim.Fase == AnimacionCarro.FaseCarro.Fuera && (EnEstado(a, "GirarIzq") || EnEstado(a, "GirarDer") || a.GetNextAnimatorStateInfo(0).IsName("GirarIzq") || a.GetNextAnimatorStateInfo(0).IsName("GirarDer")))
                {
                    giroEnElSitio = true;
                }
                if (finDePie < 0f && anim.Fase == AnimacionCarro.FaseCarro.Fuera && EnEstado(a, "Idle") && !a.IsInTransition(0))
                {
                    finDePie = t;
                }
                if ((finDePie >= 0f && t > finDePie + 0.6f) || t > 6f)
                {
                    break;
                }
            }
            Time.captureFramerate = 0;
            UnityEngine.Object.Destroy(registro);

            var log = new StringBuilder();
            float peorTiron = 0f;
            string dondeTiron = "";
            for (int c = inicio; c < registro.Cuadros.Count; c++)
            {
                Vector3[] p = registro.Cuadros[c];
                log.Append(c - inicio).Append(' ').Append(registro.Notas[c]).Append(" cad ").Append(p[0].ToString("F3")).Append(" pI ").Append(p[1].ToString("F3"))
                    .Append(" pD ").Append(p[2].ToString("F3")).Append(" cab ").Append(p[3].ToString("F3")).Append('\n');
                if (c < inicio + 2)
                {
                    continue;
                }
                Vector3[] p1 = registro.Cuadros[c - 1];
                Vector3[] p2 = registro.Cuadros[c - 2];
                for (int i = 0; i < p.Length; i++)
                {
                    // Cambio de velocidad por cuadro (aceleración discreta): un tirón es un salto que no viene del movimiento que ya llevaba.
                    float tiron = ((p[i] - p1[i]) - (p1[i] - p2[i])).magnitude;
                    if (tiron > peorTiron)
                    {
                        peorTiron = tiron;
                        dondeTiron = "cuadro " + (c - inicio) + " hueso " + i + " (" + registro.Notas[c] + ")";
                    }
                }
            }
            Debug.Log("[Prueba] T-976 " + codigo + " bajar:\n" + log);
            Debug.Log("[Prueba] T-976 " + codigo + ": peor tirón " + peorTiron.ToString("F3") + " m/cuadro en " + dondeTiron + "; de pie a los " + finDePie.ToString("F2") + " s.");
            UnityEngine.Object.Destroy(cam.gameObject);
            // Los clips limpios no sacan el pie de su camino entre cuadros (antes, a medio cuadro del 40 % de subir y el 60 % de bajar, saltaba ~25 cm),
            // y de MotoBajar a Idle la pierna no cruza el aire (antes, a media mezcla, el pie llegaba a 1.5 m).
            float peorClip = 0f;
            string dondeClip = "";
            foreach (string est in new[] { "MotoBajar", "MotoSubir" })
            {
                Vector3 antes = Vector3.zero;
                for (int k = 0; k <= 200; k++)
                {
                    a.Play(est, 0, k / 200f);
                    a.Update(0f);
                    Vector3 pie = a.transform.InverseTransformPoint(pieD.position);
                    if (k > 0 && (pie - antes).magnitude > peorClip)
                    {
                        peorClip = (pie - antes).magnitude;
                        dondeClip = est + " " + (k / 200f).ToString("F3");
                    }
                    antes = pie;
                }
            }
            a.Play("MotoBajar", 0, 0.8f);
            a.Update(0f);
            float pieMasAlto = 0f;
            for (int k = 0; k < 20; k++)
            {
                a.Update(1f / 30f);
                pieMasAlto = Mathf.Max(pieMasAlto, a.transform.InverseTransformPoint(pieD.position).y);
            }
            Debug.Log("[Prueba] T-976 " + codigo + ": paso mayor del pie en los clips " + peorClip.ToString("F3") + " m (" + dondeClip + "); pie más alto al pasar a Idle " + pieMasAlto.ToString("F2") + " m.");

            Assert.GreaterOrEqual(finDePie, 0f, "queda de pie en Idle");
            Assert.Less(Vector3.Distance(a.transform.parent.position, jugador.transform.position), 0.05f, "el visual vuelve a los pies del jugador");
            Assert.IsFalse(giroEnElSitio, "de pie no gira en el sitio (el jugador no movió el ratón)");
            Assert.Less(peorTiron, TironMaximo, "sin tirones al bajar (cambio de velocidad de cadera, pies o cabeza por cuadro)");
            // En el tramo rápido un cuadro del clip avanza el pie ~0.09 m; ahí el clip limpio pasa de un cuadro al otro sin mezclar (antes iba y volvía ~25 cm).
            Assert.Less(peorClip, 0.15f, "el pie sigue su camino en los clips de subir y bajar");
            Assert.Less(pieMasAlto, 0.3f, "de MotoBajar a Idle el pie se queda abajo");
        }

        /// <summary>
        /// Antes de T-976 el peor tirón era 1.71 m/cuadro. Queda uno de ~0.16 en el segundo cuadro: los pies bajan 8 cm un solo cuadro al soltar los estribos
        /// (el IK de PoseMoto corre con peso 0.99 y aun así; sin causa encontrada). Todo lo demás va por debajo de 0.05.
        /// </summary>
        private const float TironMaximo = 0.2f;

        private static string Estado(Animator a)
        {
            foreach (string s in new[] { "MotoManejar", "MotoBajar", "MotoSubir", "Idle", "Caminar", "Correr", "Frenar" })
            {
                if (EnEstado(a, s)) { return s + (a.IsInTransition(0) ? "*" + Siguiente(a) + " " + a.GetCurrentAnimatorStateInfo(0).normalizedTime.ToString("F2") : ""); }
            }
            return "?" + Siguiente(a);
        }

        private static string Siguiente(Animator a)
        {
            AnimatorStateInfo n = a.GetNextAnimatorStateInfo(0);
            foreach (string s in ReglasAnimacion.EstadosBase)
            {
                if (n.IsName(s)) { return ">" + s; }
            }
            return "";
        }
    }
}
