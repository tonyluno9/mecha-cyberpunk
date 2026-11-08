using System.Collections;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.Personaje.Visual;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>
    /// T-749: con los clips de Blender ya aplicados a PJ01 y el agarre de la pistola sin compensación (AgarrePistolaPJ01 apagado), en el juego: giro y cabeceo del cañón contra la mira,
    /// ladeo de la corredera y mano izquierda en la empuñadura. Medidas en Logs/t749-medidas.txt.
    /// </summary>
    public class ApuntarClipsPJ01T749Pruebas : BaseJugadorAPie
    {
        private VisualPersonaje visual;
        private Animator anim;
        private Transform pistola;
        private AgarrePistolaPJ01 agarre;

        private IEnumerator Preparar()
        {
            yield return Calentar();
            visual = jugador.GetComponentInChildren<VisualPersonaje>(true);
            anim = visual.Animador;
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            Assert.IsTrue(jugador.GetComponent<ArmasAPie>().Equipar(ArmaAPie.Pistola));
            pistola = visual.GetComponentInChildren<ApuntadoIK>(true).SocketArma.Find("Pistola");
            agarre = anim.GetComponent<AgarrePistolaPJ01>();
            if (agarre != null) { agarre.Usar = false; agarre.RetrocesoGrados = 0f; }
            yield return Esperar(0.8f);
        }

        private Camera Camara() => jugador.GetComponentInChildren<CamaraHombro>().GetComponent<Camera>();

        private string Medir(string titulo)
        {
            Camera c = Camara();
            Vector3 punto = c.transform.position + c.transform.forward * 30f;
            Transform boca = pistola.Find("Boca");
            Vector3 origen = boca != null ? boca.position : pistola.position;
            Vector3 haciaMira = punto - origen;
            Vector3 f = visual.transform.InverseTransformDirection(pistola.forward);
            Vector3 m = visual.transform.InverseTransformDirection(haciaMira.normalized);
            float giro = Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg, cab = Mathf.Asin(Mathf.Clamp(f.y, -1f, 1f)) * Mathf.Rad2Deg;
            float giroM = Mathf.Atan2(m.x, m.z) * Mathf.Rad2Deg, cabM = Mathf.Asin(Mathf.Clamp(m.y, -1f, 1f)) * Mathf.Rad2Deg;
            float ladeo = Mathf.Asin(Mathf.Clamp(Vector3.Dot(pistola.right, Vector3.up), -1f, 1f)) * Mathf.Rad2Deg;
            Transform hi = anim.GetBoneTransform(HumanBodyBones.LeftHand), hd = anim.GetBoneTransform(HumanBodyBones.RightHand), dedo = anim.GetBoneTransform(HumanBodyBones.LeftMiddleProximal);
            float izq = Vector3.Distance(Vector3.Lerp(hi.position, dedo.position, 0.5f), pistola.position) * 100f;
            float manos = Vector3.Distance(hi.position, hd.position) * 100f;
            string s = titulo + ": canon giro " + giro.ToString("F1") + " cabeceo " + cab.ToString("F1") + " | mira giro " + giroM.ToString("F1") + " cabeceo " + cabM.ToString("F1") +
                " | canon a la mira " + Vector3.Angle(pistola.forward, haciaMira).ToString("F1") + " | ladeo " + ladeo.ToString("F1") + " | mano izq a empunadura " + izq.ToString("F1") + " cm, entre manos " + manos.ToString("F1") + " cm";
            // Giro local de la pistola (respecto a su socket) con el que el canon miraria al frente del cuerpo con la corredera arriba: el agarre que haria falta en este cuadro.
            Quaternion necesario = Quaternion.Inverse(pistola.parent.rotation) * visual.transform.rotation;
            s += " | agarre necesario euler " + necesario.eulerAngles.ToString("F1") + " q(" + necesario.x.ToString("F5") + ", " + necesario.y.ToString("F5") + ", " + necesario.z.ToString("F5") + ", " + necesario.w.ToString("F5") + ")" +
                " | socket local euler " + pistola.parent.localEulerAngles.ToString("F1") + " pistola local euler " + pistola.localEulerAngles.ToString("F1") + " pos " + pistola.localPosition.ToString("F4");
            for (int capa = 0; capa < 2; capa++)
            {
                string cs = "";
                foreach (AnimatorClipInfo ci in anim.GetCurrentAnimatorClipInfo(capa)) { cs += ci.clip.name + ":" + ci.weight.ToString("F2") + " "; }
                s += " | capa" + capa + " " + cs + (anim.IsInTransition(capa) ? "(transicion)" : "");
            }
            MedidaPoseClipsPJ01.Anotar(s);
            return s;
        }

        [UnityTest]
        public IEnumerator Medida_Apuntar_Disparo_Laterales()
        {
            yield return Preparar();
            Press(raton.rightButton);
            yield return Esperar(2.0f);
            Medir("quieto");
            Press(raton.leftButton);
            yield return null;
            Release(raton.leftButton);
            yield return null;
            Medir("disparo");
            yield return Esperar(1.5f);
            foreach ((string n, UnityEngine.InputSystem.Controls.KeyControl k) in new[] { ("lateral-derecha", teclado.dKey), ("lateral-izquierda", teclado.aKey), ("atras", teclado.sKey), ("adelante", teclado.wKey) })
            {
                Press(k);
                yield return Esperar(1.2f);
                Medir(n);
                Release(k);
                yield return Esperar(0.6f);
            }
            Release(raton.rightButton);
        }
    }
}
