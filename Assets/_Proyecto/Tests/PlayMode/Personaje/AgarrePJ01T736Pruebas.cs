using System.Collections;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Visual;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>
    /// T-736 (A6): la pistola W01 en R_Hand de PJ01 con el agarre de agarre.txt (caso A): R_Hand sin escala extra, la pistola mide unos 21 cm, apunta al frente
    /// y cae en la palma; las armas largas toman el mismo agarre. Capturas de cerca, de lado y apuntando en docs/capturas/pj01-agarre-*.png.
    /// </summary>
    public class AgarrePJ01T736Pruebas : BaseJugadorAPie
    {
        private VisualPersonaje visual;
        private Animator anim;

        private IEnumerator Preparar()
        {
            yield return Calentar();
            visual = jugador.GetComponentInChildren<VisualPersonaje>(true);
            anim = visual.Animador;
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            yield return Esperar(0.8f);
        }

        private Transform Arma(string nombre) => visual.GetComponentInChildren<ApuntadoIK>(true).SocketArma.Find(nombre);

        private static Vector3 Palma(Animator a) =>
            Vector3.Lerp(a.GetBoneTransform(HumanBodyBones.RightHand).position, a.GetBoneTransform(HumanBodyBones.RightMiddleProximal).position, 0.5f);

        // Tamano (m) en los ejes del arma de las mallas visibles (los cubos de ayuda no cuentan).
        private static Vector3 Medida(Transform arma)
        {
            Bounds b = new Bounds();
            bool primero = true;
            foreach (MeshRenderer mr in arma.GetComponentsInChildren<MeshRenderer>())
            {
                if (!mr.enabled || !mr.gameObject.activeInHierarchy || mr.name.StartsWith("Cuerpo") || mr.name == "Canon") { continue; }
                MeshFilter mf = mr.GetComponent<MeshFilter>();
                // Las mallas no son legibles (isReadable apagado): se usan las esquinas de su caja local.
                Bounds mb = mf.sharedMesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    Vector3 esquina = mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
                    Vector3 local = arma.InverseTransformPoint(mf.transform.TransformPoint(esquina));
                    if (primero) { b = new Bounds(local, Vector3.zero); primero = false; }
                    else { b.Encapsulate(local); }
                }
            }
            return b.size;
        }

        private void Fotos(string nombre, Transform arma)
        {
            Transform t = jugador.transform;
            Vector3 p = arma.position + arma.forward * 0.03f;
            CapturaS1.FotoMira("pj01-agarre-" + nombre + "-cerca", t, p, new Vector3(1f, 0.35f, 0.15f), 0.65f, 28f, 700);
            CapturaS1.FotoMira("pj01-agarre-" + nombre + "-lado", t, p, new Vector3(1f, 0.1f, -0.2f), 1.1f, 30f, 700);
            CapturaS1.Foto("pj01-agarre-" + nombre + "-cuerpo", t, new Vector3(2.8f, 1.1f, 0.6f), new Vector3(0f, 1.0f, 0.4f), 40f, 700, 700);
        }

        [UnityTest]
        public IEnumerator Pistola_CasoA_RHandSinEscala_Mide21cm_EnLaPalma_ApuntaAlFrente()
        {
            yield return Preparar();
            Transform mano = anim.GetBoneTransform(HumanBodyBones.RightHand);
            Assert.AreEqual("R_Hand", mano.name);
            Assert.AreEqual(1f, mano.lossyScale.x, 0.03f, "R_Hand con lossyScale 1: caso A de agarre.txt (con 1.84 seria el caso B)");
            Assert.IsTrue(jugador.GetComponent<ArmasAPie>().Equipar(ArmaAPie.Pistola));
            Transform pistola = Arma("Pistola");
            Assert.AreEqual(0f, Vector3.Distance(new Vector3(0.0129f, 0.1181f, 0.0080f), pistola.localPosition), 0.0005f);
            Assert.Less(Quaternion.Angle(Quaternion.Euler(AgarresArmas.GiroPJ01), pistola.localRotation), 0.2f, "El nodo Pistola lleva el agarre de agarre.txt compuesto con el giro del modelo");
            Assert.Less(Quaternion.Angle(AgarresArmas.CuaternionTony, pistola.localRotation * pistola.Find("W01/Modelo").localRotation * Quaternion.Inverse(AgarresArmas.GiroFbxDeTony)), 0.3f,
                "Pistola * Modelo * inverso del giro del FBX = el cuaternion de Tony");
            Assert.AreEqual(1f, pistola.lossyScale.x, 0.02f);
            Press(raton.rightButton);
            yield return Esperar(1.6f);
            Vector3 medida = Medida(pistola);
            Assert.AreEqual(0.21f, medida.z, 0.025f, "La pistola mide unos 21 cm de largo (" + medida.z.ToString("F3") + ")");
            Assert.Less(Vector3.Distance(pistola.position, Palma(anim)), 0.07f, "La empunadura esta en la palma");
            // T-738: AgarrePistolaPJ01 compensa en el agarre lo que Humanoid mueve la mano (el clip va puro, T-737).
            Assert.Greater(Vector3.Dot(Vector3.ProjectOnPlane(pistola.forward, Vector3.up).normalized, visual.transform.forward), 0.93f, "Apuntando el canon mira al frente");
            Assert.Less(Mathf.Abs(Vector3.Dot(pistola.right, Vector3.up)), 0.3f, "La corredera queda arriba, sin ladearse");
            Fotos("pistola-apuntando", pistola);
            Release(raton.rightButton);
        }

        [UnityTest]
        public IEnumerator Pistola_RecienDisparadaSinApuntar_SigueEnLaPalma()
        {
            yield return Preparar();
            Assert.IsTrue(jugador.GetComponent<ArmasAPie>().Equipar(ArmaAPie.Pistola));
            Transform pistola = Arma("Pistola");
            Press(raton.leftButton);
            yield return null;
            Release(raton.leftButton);
            yield return Esperar(0.5f);
            Assert.IsTrue(pistola.gameObject.activeInHierarchy);
            Assert.Less(Vector3.Distance(pistola.position, Palma(anim)), 0.07f, "Recien disparada sigue en la palma");
            Fotos("pistola-bajada", pistola);
        }

        [UnityTest]
        public IEnumerator ArmasLargas_ConElMismoAgarre_NoSeRompenConPJ01_ApuntanAlFrente()
        {
            yield return Preparar();
            ConIKDeApuntadoEncendido(); // T-737: mide las correcciones de ApuntadoIK, que el jugador ya no usa
            Transform socket = visual.GetComponentInChildren<ApuntadoIK>(true).SocketArma;
            socket.Find("Pistola").gameObject.SetActive(false);
            string fallos = "";
            foreach (string nombre in new[] { "Fusil", "Escopeta", "Sniper" })
            {
                foreach (Transform hijo in socket) { if (hijo.name != "Pistola") { hijo.gameObject.SetActive(hijo.name == nombre); } }
                Transform arma = socket.Find(nombre);
                visual.TipoArma = TipoArmaVisual.ArmaLarga;
                Press(raton.rightButton);
                yield return Esperar(1.3f);
                float alFrente = Vector3.Dot(Vector3.ProjectOnPlane(arma.forward, Vector3.up).normalized, visual.transform.forward);
                float distancia = Vector3.Distance(arma.position, Palma(anim));
                if (alFrente < 0.8f) { fallos += nombre + " frente=" + alFrente.ToString("F2") + "; "; }
                if (distancia > 0.1f) { fallos += nombre + " lejos de la palma=" + distancia.ToString("F2") + "; "; }
                Fotos(nombre.ToLower() + "-apuntando", arma);
                Release(raton.rightButton);
                yield return Esperar(0.4f);
            }
            Assert.AreEqual("", fallos);
        }
    }
}
