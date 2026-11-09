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

        private float ultimoCanon, ultimoLadeo, ultimasManos;

        private void Comprobar(string titulo)
        {
            Medir(titulo);
            Assert.Less(ultimoCanon, CanonMaximo, titulo + ": canon apuntando a la mira");
            Assert.Less(Mathf.Abs(ultimoLadeo), LadeoMaximo, titulo + ": corredera arriba");
            Assert.Less(ultimasManos, ManosMaximo, titulo + ": las dos manos juntas");
        }

        private const float CanonMaximo = 4f, LadeoMaximo = 4f, ManosMaximo = 13f, DiferenciaPiesMaxima = 0.06f;

        private float AlturaPies()
        {
            float y = Mathf.Min(anim.GetBoneTransform(HumanBodyBones.LeftFoot).position.y, anim.GetBoneTransform(HumanBodyBones.RightFoot).position.y);
            return y - visual.transform.position.y;
        }

        private void Foto(string nombre, Vector3 desplazamiento)
        {
            if (System.Environment.GetEnvironmentVariable("T749_CAPTURAS") != "1") { return; }
            var go = new GameObject("CamaraFoto");
            var cam = go.AddComponent<Camera>();
            Vector3 centro = visual.transform.position + Vector3.up * 1.2f;
            go.transform.position = centro + visual.transform.rotation * desplazamiento;
            go.transform.LookAt(centro);
            var rt = new RenderTexture(1280, 720, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            System.IO.File.WriteAllBytes("docs/capturas/pj01-t749-" + nombre + ".png", tex.EncodeToPNG());
            cam.targetTexture = null;
            Object.Destroy(tex); Object.Destroy(rt); Object.Destroy(go);
        }

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
            ultimoCanon = Vector3.Angle(pistola.forward, haciaMira); ultimoLadeo = ladeo; ultimasManos = manos;
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
            float piesSinApuntar = AlturaPies();
            Foto("sin-apuntar", new Vector3(2.2f, 0.3f, 2.2f));
            Press(raton.rightButton);
            yield return Esperar(2.0f);
            Comprobar("quieto");
            Foto("apuntando-lado", new Vector3(3f, 0.2f, 0.4f));
            Foto("apuntando-frente", new Vector3(0.7f, 0.2f, 3f));
            Foto("apuntando-atras", new Vector3(-0.8f, 0.5f, -2.6f));
            Assert.Less(Mathf.Abs(AlturaPies() - piesSinApuntar), DiferenciaPiesMaxima, "Los pies no se despegan al apuntar quieto");
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
                Comprobar(n);
                Foto(n, new Vector3(2.5f, 0.3f, 1.5f));
                Release(k);
                yield return Esperar(0.6f);
            }
            Release(raton.rightButton);
        }
    }
}
