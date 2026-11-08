using System.Collections;
using System.Collections.Generic;
using System.Text;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Visual;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>
    /// T-737: al apuntar PJ01 se ve igual que el clip puro (el mismo clip sobre una copia del modelo, sin controlador ni scripts). Se mide el giro local de cada hueso
    /// del torso, brazos y cabeza contra esa copia y se saca una foto de las dos de frente y de lado (docs/capturas/pj01-apuntar-*.png con CAPTURAR_S1=1;
    /// T737_FASE=antes cambia el sufijo). Las medidas van a Logs/t737-medidas.txt.
    /// </summary>
    public class ApuntarPJ01T737Pruebas : BaseJugadorAPie
    {
        private const string RutaPJ01 = "Assets/Arte/Personajes/PJ01/PJ01.fbx";
        private const string Carpeta = "Assets/Arte/Personajes/Animaciones/Pistola/";
        // Huesos humanoides que mueve el clip de arriba y que se comparan.
        private static readonly HumanBodyBones[] HuesosArriba =
        {
            HumanBodyBones.Spine, HumanBodyBones.Chest, HumanBodyBones.UpperChest, HumanBodyBones.Head,
            HumanBodyBones.LeftShoulder, HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand,
            HumanBodyBones.RightShoulder, HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand,
        };

        private VisualPersonaje visual;
        private Animator anim;
        private readonly List<GameObject> copias = new List<GameObject>();
        private readonly List<PlayableGraph> grafos = new List<PlayableGraph>();
        private PlayableGraph copiaGrafo;
        private AnimationClipPlayable copiaPlayable;

        // El juego se congela, la copia se posa en el mismo momento del clip y se espera a que las poses lleguen a los huesos.
        private IEnumerator Posar(float tiempo)
        {
            yield return null;
            copiaPlayable.SetTime(tiempo);
            copiaGrafo.Evaluate(0f);
            yield return null;
        }

        private static string Fase => System.Environment.GetEnvironmentVariable("T737_FASE") ?? "despues";

        public override void TearDown()
        {
            foreach (PlayableGraph g in grafos) { if (g.IsValid()) { g.Destroy(); } }
            grafos.Clear();
            foreach (GameObject c in copias) { if (c != null) { Object.Destroy(c); } }
            copias.Clear();
            base.TearDown();
        }

        private IEnumerator Preparar()
        {
            yield return Calentar();
            visual = jugador.GetComponentInChildren<VisualPersonaje>(true);
            anim = visual.Animador;
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            Assert.IsTrue(jugador.GetComponent<ArmasAPie>().Equipar(ArmaAPie.Pistola));
            // T737_FASE=antes vuelve a encender las correcciones de ApuntadoIK para medir como se veia antes del arreglo.
            if (Fase == "antes") { anim.GetComponent<ApuntadoIK>().ClipPuro = false; }
            yield return Esperar(0.8f);
        }

        private static AnimationClip Clip(string nombre)
        {
            foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(Carpeta + nombre + ".fbx"))
            {
                if (o is AnimationClip c && !c.name.StartsWith("__preview__")) { return c; }
            }
            Assert.Fail("No encuentro el clip " + nombre);
            return null;
        }

        /// <summary>El clip tal como viene, sobre una copia limpia de PJ01 (sin controlador, sin scripts, sin IK), al lado del jugador y mirando igual.</summary>
        private Animator CopiaPura(AnimationClip clip, float tiempo, Vector3 desplazamiento)
        {
            GameObject copia = (GameObject)Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPJ01), anim.transform.position + desplazamiento, anim.transform.rotation);
            copia.transform.localScale = anim.transform.lossyScale;
            copias.Add(copia);
            Animator a = copia.GetComponent<Animator>();
            if (a == null) { a = copia.AddComponent<Animator>(); }
            foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(RutaPJ01)) { if (o is Avatar av) { a.avatar = av; } }
            a.applyRootMotion = false;
            a.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            a.runtimeAnimatorController = null;
            PlayableGraph g = PlayableGraph.Create("T737");
            g.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            AnimationPlayableOutput salida = AnimationPlayableOutput.Create(g, "salida", a);
            AnimationClipPlayable p = AnimationClipPlayable.Create(g, clip);
            p.SetApplyFootIK(false);
            p.SetTime(tiempo);
            salida.SetSourcePlayable(p);
            copiaGrafo = g; copiaPlayable = p;

            grafos.Add(g);
            return a;
        }

        // Giro local de cada hueso contra la copia pura: devuelve el texto y el maximo (grados) de los huesos comparados.
        private static string Comparar(Animator juego, Animator puro, out float maximo, string titulo)
        {
            var sb = new StringBuilder("[" + titulo + "]");
            maximo = 0f;
            foreach (HumanBodyBones h in HuesosArriba)
            {
                Transform a = juego.GetBoneTransform(h);
                Transform b = puro.GetBoneTransform(h);
                if (a == null || b == null) { continue; }
                float g = Quaternion.Angle(a.localRotation, b.localRotation);
                if (g > maximo) { maximo = g; }
                sb.Append(" " + h + "=" + g.ToString("F1"));
            }
            sb.Append(" | max=" + maximo.ToString("F1"));
            return sb.ToString();
        }

        private static void Anotar(string linea)
        {
            Debug.Log("T737 " + linea);
            System.IO.Directory.CreateDirectory("Logs");
            System.IO.File.AppendAllText("Logs/t737-medidas.txt", linea + "\n");
        }

        private void Fotos(string estado, Transform marcaJuego, Transform marcaClip)
        {
            string f = Fase;
            Vector3 frente = new Vector3(0f, 1.2f, 3.2f), lado = new Vector3(3.2f, 1.2f, 0.2f), mira = new Vector3(0f, 1.1f, 0.25f);
            CapturaS1.Foto("pj01-apuntar-" + estado + "-frente-" + f, marcaJuego, frente, mira, 38f, 700, 700);
            CapturaS1.Foto("pj01-apuntar-" + estado + "-lado-" + f, marcaJuego, lado, mira, 38f, 700, 700);
            CapturaS1.Foto("pj01-apuntar-" + estado + "-frente-clip", marcaClip, frente, mira, 38f, 700, 700);
            CapturaS1.Foto("pj01-apuntar-" + estado + "-lado-clip", marcaClip, lado, mira, 38f, 700, 700);
        }

        private Transform Marca(string nombre, Vector3 pos)
        {
            var go = new GameObject(nombre);
            go.transform.position = new Vector3(pos.x, jugador.transform.position.y, pos.z);
            go.transform.rotation = jugador.transform.rotation;
            copias.Add(go);
            return go.transform;
        }

        private IEnumerator MedirEstado(string estado, string clipNombre, bool exigir)
        {
            anim.speed = 0f;
            yield return null;
            AnimatorStateInfo info = anim.GetCurrentAnimatorStateInfo(1);
            AnimationClip clip = Clip(clipNombre);
            float t = (info.normalizedTime % 1f) * clip.length;
            Vector3 aparte = jugador.transform.right * 4f;
            Animator puro = CopiaPura(clip, t, aparte);
            yield return Posar(t);
            Transform marcaJuego = Marca("MarcaJuego", jugador.transform.position);
            Transform marcaClip = Marca("MarcaClip", jugador.transform.position + aparte);
            string texto = Comparar(anim, puro, out float max, estado + " fase=" + Fase + " t=" + t.ToString("F2"));
            Anotar(texto);
            Fotos(estado, marcaJuego, marcaClip);
            if (exigir)
            {
                Assert.Less(max, 6f, "El jugador se ve como el clip puro (" + clipNombre + "): " + texto);
            }
        }

        [UnityTest]
        public IEnumerator Apuntar_Quieto_IgualAlClipPuro()
        {
            yield return Preparar();
            Press(raton.rightButton);
            yield return Esperar(2.0f);
            yield return MedirEstado("quieto", "Pistola_Apuntar", true);
            Release(raton.rightButton);
        }

        [UnityTest]
        public IEnumerator Apuntar_Disparo_IgualAlClipPuro()
        {
            yield return Preparar();
            Press(raton.rightButton);
            yield return Esperar(2.0f);
            Press(raton.leftButton);
            yield return null;
            Release(raton.leftButton);
            yield return Esperar(0.12f);
            AnimatorClipInfo[] clips = anim.GetCurrentAnimatorClipInfo(1);
            Anotar("disparo: clips en capa 1 = " + (clips.Length > 0 ? clips[0].clip.name + " peso " + clips[0].weight : "ninguno") + " estado normalizedTime=" + anim.GetCurrentAnimatorStateInfo(1).normalizedTime.ToString("F2"));
            yield return MedirEstado("disparo", "Pistola_Disparo", true);
            Release(raton.rightButton);
        }

        [UnityTest]
        public IEnumerator Apuntar_CaminarDeLado_IgualAlClipPuro()
        {
            yield return Preparar();
            Press(raton.rightButton);
            yield return Esperar(1.6f);
            Press(teclado.dKey);
            yield return Esperar(1.2f);
            AnimatorClipInfo[] clips = anim.GetCurrentAnimatorClipInfo(1);
            string quien = "";
            foreach (AnimatorClipInfo c in clips) { quien += c.clip.name + ":" + c.weight.ToString("F2") + " "; }
            Anotar("lateral: clips en capa 1 = " + quien + " ApuntarX=" + anim.GetFloat("ApuntarX").ToString("F2") + " ApuntarZ=" + anim.GetFloat("ApuntarZ").ToString("F2"));
            yield return MedirEstado("lateral", "Pistola_ApuntarLateral_Derecha", true);
            Release(teclado.dKey);
            Release(raton.rightButton);
        }

        // Cañón y corredera de la pistola en el espacio del cuerpo.
        private string Canon(string titulo)
        {
            Transform pistola = visual.GetComponentInChildren<ApuntadoIK>(true).SocketArma.Find("Pistola");
            Vector3 f = visual.transform.InverseTransformDirection(pistola.forward);
            float yaw = Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg;
            float cabeceo = Mathf.Asin(Mathf.Clamp(f.y, -1f, 1f)) * Mathf.Rad2Deg;
            float ladeo = Mathf.Asin(Mathf.Clamp(Vector3.Dot(pistola.right, Vector3.up), -1f, 1f)) * Mathf.Rad2Deg;
            Vector3 hipsLocal = visual.transform.InverseTransformDirection(anim.GetBoneTransform(HumanBodyBones.Hips).forward);
            return "[" + titulo + "] canon: giro a un lado=" + yaw.ToString("F1") + " cabeceo=" + cabeceo.ToString("F1") + " ladeo de la corredera (eje X de la pistola sobre la horizontal)=" + ladeo.ToString("F1") + " | cadera mira a " + (Mathf.Atan2(hipsLocal.x, hipsLocal.z) * Mathf.Rad2Deg).ToString("F0");
        }

        [UnityTest]
        public IEnumerator Medida_CanonDeLaPistola_AlApuntar()
        {
            yield return Preparar();
            Press(raton.rightButton);
            yield return Esperar(2.0f);
            Anotar(Canon("quieto fase=" + Fase));
            Press(raton.leftButton);
            yield return null;
            Release(raton.leftButton);
            yield return Esperar(0.12f);
            Anotar(Canon("disparo fase=" + Fase));
            yield return Esperar(1.5f);
            Press(teclado.dKey);
            yield return Esperar(1.2f);
            Anotar(Canon("lateral derecha fase=" + Fase));
            Release(teclado.dKey);
            Release(raton.rightButton);
        }

        /// <summary>Quien mueve que: con cada componente apagado por turno se ve cuanto cambia cada hueso.</summary>
        [UnityTest]
        public IEnumerator Diagnostico_QueComponenteMueveQueHueso()
        {
            yield return Preparar();
            Press(raton.rightButton);
            yield return Esperar(2.0f);
            AnimationClip clip = Clip("Pistola_Apuntar");
            float t = (anim.GetCurrentAnimatorStateInfo(1).normalizedTime % 1f) * clip.length;
            anim.speed = 0f;
            yield return null;
            Animator puro = CopiaPura(clip, t, jugador.transform.right * 4f);
            yield return Posar(t);
            Anotar(Comparar(anim, puro, out float _, "todo encendido"));
            {
                var hj = new HumanPoseHandler(anim.avatar, anim.transform);
                var hp = new HumanPoseHandler(puro.avatar, puro.transform);
                var pj = new HumanPose();
                var pp = new HumanPose();
                hj.GetHumanPose(ref pj);
                hp.GetHumanPose(ref pp);
                var sbm = new StringBuilder("musculos que difieren >0.15 (juego/clip): ");
                for (int i = 0; i < pj.muscles.Length; i++)
                {
                    if (Mathf.Abs(pj.muscles[i] - pp.muscles[i]) > 0.15f) { sbm.Append(HumanTrait.MuscleName[i] + "=" + pj.muscles[i].ToString("F2") + "/" + pp.muscles[i].ToString("F2") + "; "); }
                }
                Anotar(sbm.ToString());
                Anotar("avatar juego=" + anim.avatar.name + " clip=" + puro.avatar.name + " mismo=" + (anim.avatar == puro.avatar) + " tiempoClip=" + t);
            }
            for (int i = 0; i < anim.layerCount; i++)
            {
                AnimatorClipInfo[] ci = anim.GetCurrentAnimatorClipInfo(i);
                string cs = "";
                foreach (AnimatorClipInfo c in ci) { cs += c.clip.name + ":" + c.weight.ToString("F2") + " "; }
                Anotar("capa " + i + " " + anim.GetLayerName(i) + " peso=" + anim.GetLayerWeight(i).ToString("F2") + " clips=" + cs + " enTransicion=" + anim.IsInTransition(i));
            }
            int capaDano = anim.GetLayerIndex("Dano");
            if (capaDano >= 0)
            {
                anim.SetLayerWeight(capaDano, 0f);
                yield return null; yield return null;
                Anotar(Comparar(anim, puro, out float _, "capa Dano a 0"));
            }
            var apuntado = anim.GetComponent<ApuntadoIK>();
            var pies = anim.GetComponent<IKPiesYMirada>();
            Anotar("ApuntadoIK presente=" + (apuntado != null) + " manosPorIK=" + (apuntado != null && apuntado.ManosPorIK) + " pesoManoIzq=" + (apuntado != null ? apuntado.PesoManoIzquierda : -1f));
            if (apuntado != null)
            {
                apuntado.enabled = false;
                yield return null; yield return null;
                Anotar(Comparar(anim, puro, out float _, "ApuntadoIK apagado"));
                apuntado.enabled = true;
            }
            if (pies != null)
            {
                pies.enabled = false;
                if (apuntado != null) { apuntado.enabled = false; }
                yield return null; yield return null;
                Anotar(Comparar(anim, puro, out float _, "ApuntadoIK e IKPiesYMirada apagados"));
            }
            Release(raton.rightButton);
        }
    }
}
