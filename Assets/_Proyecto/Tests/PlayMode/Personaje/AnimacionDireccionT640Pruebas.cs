using System.Collections;
using System.IO;
using MechaCyberpunk.Personaje.Visual;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>
    /// T-640: el personaje camina mirando hacia donde se mueve (no de espaldas), Caminar y Correr son animaciones distintas
    /// y ningún clip gira el cuerpo. Capturas con CAPTURAR_T640=1 en docs/capturas/anim-fix-*.png.
    /// </summary>
    public class AnimacionDireccionT640Pruebas : BaseJugadorAPie
    {
        private const string CarpetaPrefabs = "Assets/_Proyecto/Personaje/Visual/Prefabs/Visual_";
        private const string VariableCaptura = "CAPTURAR_T640";

        // Velocidad horizontal en el mundo (el controlador la expone en local).
        private Vector3 Mundo()
        {
            Vector3 v = jugador.GetComponent<CharacterController>().velocity;
            v.y = 0f;
            return v;
        }

        private VisualPersonaje Visual() => jugador.GetComponentInChildren<VisualPersonaje>(true);

        // Frente del cuerpo según los huesos (de la pierna izquierda a la derecha, girado a la izquierda): no depende de nodos del prefab.
        private static Vector3 FrenteHuesos(Animator a)
        {
            Vector3 der = a.GetBoneTransform(HumanBodyBones.RightUpperLeg).position - a.GetBoneTransform(HumanBodyBones.LeftUpperLeg).position;
            der.y = 0f;
            return Vector3.Cross(der.normalized, Vector3.up);
        }

        private static string Estado(Animator a)
        {
            AnimatorStateInfo actual = a.GetCurrentAnimatorStateInfo(0);
            AnimatorStateInfo siguiente = a.GetNextAnimatorStateInfo(0);
            foreach (string n in ReglasAnimacion.EstadosBase)
            {
                if (a.IsInTransition(0) ? siguiente.IsName(n) : actual.IsName(n))
                {
                    return n;
                }
            }
            return "?";
        }

        private static string ClipDominante(Animator a)
        {
            string mejor = "";
            float peso = -1f;
            foreach (AnimatorClipInfo i in a.GetCurrentAnimatorClipInfo(0))
            {
                if (i.weight > peso)
                {
                    peso = i.weight;
                    mejor = i.clip.name;
                }
            }
            return mejor;
        }

        private static float YawEntre(Vector3 a, Vector3 b) => Mathf.Abs(Vector3.SignedAngle(a, b, Vector3.up));

        private IEnumerator Foto(Transform objetivo, Vector3 desde, Vector3 hacia, string archivo)
        {
            if (System.Environment.GetEnvironmentVariable(VariableCaptura) != "1")
            {
                yield break;
            }
            Directory.CreateDirectory("docs/capturas");
            GameObject cg = new GameObject("CamaraT640");
            Camera cam = cg.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.25f, 0.3f, 0.4f);
            cam.fieldOfView = 40f;
            Vector3 baseP = objetivo.position;
            cam.transform.position = baseP + desde;
            cam.transform.LookAt(baseP + hacia);
            GameObject lg = new GameObject("LuzT640");
            Light luz = lg.AddComponent<Light>();
            luz.type = LightType.Directional;
            luz.intensity = 1.5f;
            lg.transform.rotation = Quaternion.LookRotation((hacia - desde).normalized + Vector3.down * 0.5f);
            yield return null;
            baseP = objetivo.position;
            cam.transform.position = baseP + desde;
            cam.transform.LookAt(baseP + hacia);
            var rt = new RenderTexture(900, 700, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(900, 700, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 900, 700), 0, 0);
            tex.Apply();
            File.WriteAllBytes("docs/capturas/" + archivo, tex.EncodeToPNG());
            RenderTexture.active = null;
            Object.Destroy(tex);
            rt.Release();
            Object.Destroy(rt);
            Object.Destroy(cg);
            Object.Destroy(lg);
        }

        [UnityTest]
        public IEnumerator Caminar_ConW_MiraHaciaDondeSeMueveYUsaCaminarNoCorrer()
        {
            yield return Calentar();
            VisualPersonaje visual = Visual();
            Animator a = visual.Animador;
            a.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            yield return Esperar(0.8f);
            Press(teclado.wKey);
            yield return Esperar(1.0f);
            Vector3 mov = Mundo();
            mov.y = 0f;
            Assert.Greater(mov.magnitude, 3f, "Se mueve");
            Assert.Greater(Vector3.Dot(FrenteHuesos(a), mov.normalized), 0.7f, "El cuerpo (huesos) mira hacia donde camina");
            Assert.Greater(Vector3.Dot(visual.transform.forward, mov.normalized), 0.7f, "El visual mira hacia donde camina");
            Assert.AreEqual("Caminar", Estado(a), "Caminar normal usa Caminar");
            Assert.AreEqual("Loco_Caminar_SinImplante", ClipDominante(a));

            // El clip no gira el cuerpo: el frente se queda estable mientras camina recto.
            Vector3 f0 = FrenteHuesos(a);
            float maximo = 0f;
            for (int i = 0; i < 20; i++)
            {
                yield return null;
                maximo = Mathf.Max(maximo, YawEntre(f0, FrenteHuesos(a)));
            }
            Assert.Less(maximo, 15f, "El cuerpo no gira durante el ciclo de caminar (grados)");

            yield return Foto(jugador.transform, Vector3.forward * 2.6f + Vector3.up * 1.5f, Vector3.up * 1.2f, "anim-fix-jugador-frente.png");
            yield return Foto(jugador.transform, Vector3.right * 2.8f + Vector3.up * 1.3f, Vector3.up * 1.0f, "anim-fix-jugador-lado.png");

            Press(teclado.leftShiftKey);
            yield return Esperar(1.2f);
            Assert.AreEqual("Correr", Estado(a), "Con Shift usa Correr");
            Assert.AreEqual("Loco_Correr_SinImplante", ClipDominante(a));
            Assert.Greater(Vector3.Dot(FrenteHuesos(a), Mundo().normalized), 0.7f, "Corriendo también mira al frente");
            Release(teclado.leftShiftKey);
            yield return Esperar(1.2f);
            Assert.AreEqual("Caminar", Estado(a), "Al soltar Shift vuelve a Caminar");
            Release(teclado.wKey);
        }

        [UnityTest]
        public IEnumerator Caminar_AtrasYLados_UsaLosClipsDeCadaDireccion()
        {
            yield return Calentar();
            Animator a = Visual().Animador;
            a.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            yield return Esperar(0.8f);
            Vector3 frente = FrenteHuesos(a);

            // Sin apuntar el cuerpo gira hacia donde camina: ir "atrás" es darse la vuelta y caminar de frente.
            Press(teclado.sKey);
            yield return Esperar(1.0f);
            Assert.Greater(Vector3.Dot(FrenteHuesos(a), Mundo().normalized), 0.7f, "Con S el cuerpo se voltea y mira hacia donde camina");
            Assert.AreEqual("Loco_Caminar_SinImplante", ClipDominante(a));
            Release(teclado.sKey);
            yield return Esperar(0.5f);
            Press(raton.rightButton);
            yield return Esperar(0.6f);
            frente = FrenteHuesos(a);

            Press(teclado.sKey);
            yield return Esperar(1.0f);
            Assert.AreEqual("Loco_CaminarAtras", ClipDominante(a), "S usa Caminar hacia atrás");
            Assert.Less(Vector3.Dot(frente, Mundo().normalized), -0.7f, "Va hacia atrás: el cuerpo sigue mirando al frente");
            Assert.Less(YawEntre(frente, FrenteHuesos(a)), 20f, "El cuerpo no se voltea al ir de reversa");
            Release(teclado.sKey);
            yield return Esperar(0.8f);

            Press(teclado.dKey);
            yield return Esperar(1.0f);
            Assert.AreEqual("Loco_CaminarDer", ClipDominante(a), "D usa Caminar a la derecha");
            Vector3 derecha = Vector3.Cross(Vector3.up, frente);
            Assert.Greater(Vector3.Dot(derecha, Mundo().normalized), 0.7f, "D va a la derecha del cuerpo");
            Release(teclado.dKey);
            yield return Esperar(0.8f);

            Press(teclado.aKey);
            yield return Esperar(1.0f);
            Assert.AreEqual("Loco_CaminarIzq", ClipDominante(a), "A usa Caminar a la izquierda");
            Assert.Less(Vector3.Dot(derecha, Mundo().normalized), -0.7f, "A va a la izquierda del cuerpo");
            Assert.Less(YawEntre(frente, FrenteHuesos(a)), 30f, "El cuerpo no gira al desplazarse de lado");
            Release(teclado.aKey);
            Release(raton.rightButton);
        }

        [UnityTest]
        public IEnumerator Saltar_ConEspacio_PasaPorUnEstadoDeSaltoYNoGiraElCuerpo()
        {
            yield return Calentar();
            Animator a = Visual().Animador;
            a.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            yield return Esperar(0.8f);
            Vector3 frente;
            Press(teclado.wKey);
            yield return Esperar(0.8f);
            frente = FrenteHuesos(a);
            Press(teclado.spaceKey);
            bool saltoVisto = false;
            float maximo = 0f;
            float fin = Time.time + 1.2f;
            while (Time.time < fin)
            {
                yield return null;
                string e = Estado(a);
                saltoVisto |= e.StartsWith("Salto") || e == "Caida";
                float giroFrame = YawEntre(frente, FrenteHuesos(a));
                maximo = Mathf.Max(maximo, giroFrame);
            }
            Release(teclado.spaceKey);
            Release(teclado.wKey);
            Assert.IsTrue(saltoVisto, "Al saltar el Animator entra a un estado de salto");
            Assert.Less(maximo, 40f, "El torso del clip de salto se tuerce poco (grados de pose)");
            Assert.Less(YawEntre(Vector3.forward, Visual().transform.forward), 10f, "El salto no gira el visual del personaje");
        }

        [UnityTest]
        public IEnumerator VisualesDeNPCSkullsJefeYGuardias_CaminanDeFrenteYCorrenSoloRapido()
        {
            yield return Calentar();
            string[] nombres = { "SkullHombre", "GuardiaNova", "woman2", "Doctor", "Hacker", "Inventor", "RobotHombre", "man" };
            var raices = new GameObject[nombres.Length];
            for (int i = 0; i < nombres.Length; i++)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CarpetaPrefabs + nombres[i] + ".prefab");
                Assert.IsNotNull(prefab, nombres[i]);
                raices[i] = new GameObject("Npc_" + nombres[i]);
                raices[i].transform.position = new Vector3(30f + i * 3f, 0.05f, 0f);
                GameObject v = Object.Instantiate(prefab, raices[i].transform);
                v.GetComponentInChildren<Animator>().cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }
            yield return Esperar(0.5f);
            foreach (float rapidez in new[] { 1.6f, 7.5f })
            {
                float fin = Time.time + 1.4f;
                while (Time.time < fin)
                {
                    foreach (GameObject r in raices)
                    {
                        r.transform.position += Vector3.forward * rapidez * Time.deltaTime;
                    }
                    yield return null;
                }
                for (int i = 0; i < raices.Length; i++)
                {
                    VisualPersonaje v = raices[i].GetComponentInChildren<VisualPersonaje>();
                    Animator a = v.Animador;
                    Assert.Greater(Vector3.Dot(FrenteHuesos(a), Vector3.forward), 0.7f, nombres[i] + " mira hacia donde camina a " + rapidez);
                    Assert.AreEqual(rapidez < 4f ? "Caminar" : "Correr", Estado(a), nombres[i] + " a " + rapidez + " m/s");
                }
                if (rapidez < 4f)
                {
                    GameObject r = raices[0];
                    yield return Foto(r.transform, Vector3.forward * 2.6f + Vector3.up * 1.4f, Vector3.up * 1.1f, "anim-fix-skull-frente.png");
                    yield return Foto(r.transform, Vector3.right * 2.8f + Vector3.up * 1.3f, Vector3.up * 1.0f, "anim-fix-skull-lado.png");
                }
            }
            foreach (GameObject r in raices)
            {
                Object.Destroy(r);
            }
        }
    }
}
