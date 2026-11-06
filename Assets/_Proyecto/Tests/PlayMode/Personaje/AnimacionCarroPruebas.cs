using System;
using System.Collections;
using System.IO;
using UnityEditor;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Visual;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>T-622: al subir a un carro el Animator pasa por Carro_Entrar y Carro_Manejar; con H pita; al bajar juega Carro_Salir.</summary>
    public class AnimacionCarroPruebas : BaseJugadorAPie
    {
        private class MontableFalso : IMontable
        {
            public Transform asiento;
            public Camera camara;
            public ModoJugador Modo => ModoJugador.Carro;
            public string Nombre => "Carro falso";
            public Vector3 PuntoSubida => asiento.position;
            public bool Ocupado { get; private set; }
            public bool PuedeSubir => !Ocupado;
            public bool PuedeBajar => Ocupado;
            public Transform Asiento => asiento;
            public Camera Camara => camara;
            public event Action<bool> OcupadoCambio;
            public bool IntentarSubir(IJugador jugador) { Ocupado = true; OcupadoCambio?.Invoke(true); return true; }
            public bool IntentarBajar(out Pose salida)
            {
                salida = new Pose(new Vector3(-2f, 0.05f, 0f), Quaternion.identity);
                Ocupado = false;
                OcupadoCambio?.Invoke(false);
                return true;
            }
        }

        private GameObject carro;
        private MontableFalso montable;

        private void CrearCarro()
        {
            carro = new GameObject("CarroFalso");
            carro.transform.position = new Vector3(0f, 0f, 3f);
            var asiento = new GameObject("Asiento").transform;
            asiento.SetParent(carro.transform, false);
            asiento.localPosition = new Vector3(-0.4f, 0.75f, 0f);
            var cam = new GameObject("Cam").AddComponent<Camera>();
            cam.transform.SetParent(asiento, false);
            cam.enabled = false;
            montable = new MontableFalso { asiento = asiento, camara = cam };
        }

        public override void TearDown()
        {
            if (carro != null) { UnityEngine.Object.Destroy(carro); }
            base.TearDown();
        }

        private static bool EnEstado(Animator a, int capa, string nombre)
        {
            AnimatorStateInfo actual = a.GetCurrentAnimatorStateInfo(capa);
            AnimatorStateInfo siguiente = a.GetNextAnimatorStateInfo(capa);
            return a.IsInTransition(capa) ? siguiente.IsName(nombre) || actual.IsName(nombre) : actual.IsName(nombre);
        }

        private static IEnumerator Hasta(Func<bool> condicion, string que, float limite = 4f)
        {
            float fin = Time.time + limite;
            while (!condicion() && Time.time < fin) { yield return null; }
            Assert.IsTrue(condicion(), "No llegó: " + que);
        }

        [UnityTest]
        public IEnumerator Subir_Entrar_Manejar_Pitar_Y_Bajar_CambianElAnimator()
        {
            yield return Calentar();
            CrearCarro();
            VisualPersonaje visual = jugador.GetComponentInChildren<VisualPersonaje>(true);
            Animator a = visual.Animador;
            AnimacionCarro anim = jugador.GetComponent<AnimacionCarro>();
            Assert.IsNotNull(anim, "JugadorAPie agrega AnimacionCarro");

            Assert.IsTrue(jugadorAPie.Subir(montable));
            yield return null;
            Assert.IsTrue(a.gameObject.activeInHierarchy, "El modelo se queda visible dentro del carro");
            Assert.AreEqual(AnimacionCarro.FaseCarro.Entrando, anim.Fase);
            yield return Hasta(() => EnEstado(a, 0, "CarroEntrar"), "Carro_Entrar al subir");
            yield return Hasta(() => anim.Fase == AnimacionCarro.FaseCarro.Dentro, "termina de entrar", 8f);
            yield return Hasta(() => EnEstado(a, 0, "CarroManejar"), "Carro_Manejar sentado");
            yield return Esperar(0.4f);
            Assert.AreEqual(0f, visual.PesoCapaArriba, 0.05f, "Sin torso de arma sentado");
            Vector3 cadera = a.GetBoneTransform(HumanBodyBones.Hips).position;
            Assert.Less(Vector3.Distance(cadera, montable.Asiento.position), 0.6f, "La cadera queda en el asiento");

            Press(teclado.hKey);
            yield return null;
            Release(teclado.hKey);
            yield return Hasta(() => EnEstado(a, 1, "Pitar"), "Honking Horn al pitar");
            Assert.IsTrue(EnEstado(a, 0, "CarroManejar"), "Sigue sentado al pitar");

            Assert.IsTrue(jugadorAPie.Bajar());
            yield return Hasta(() => EnEstado(a, 0, "CarroSalir"), "Carro_Salir al bajar");
            yield return Hasta(() => anim.Fase == AnimacionCarro.FaseCarro.Fuera, "termina de salir", 8f);
            yield return Hasta(() => EnEstado(a, 0, "Idle"), "vuelve a Idle", 5f);
        }

        [UnityTest]
        public IEnumerator Subir_Y_Bajar_T652_LaAnimacionDuraUnMomentoYNoLos5Segundos()
        {
            yield return Calentar();
            CrearCarro();
            AnimacionCarro anim = jugador.GetComponent<AnimacionCarro>();
            Assert.IsTrue(jugadorAPie.Subir(montable));
            float t0 = Time.time;
            yield return Hasta(() => anim.Fase == AnimacionCarro.FaseCarro.Dentro, "termina de entrar", 8f);
            Assert.Less(Time.time - t0, 2.6f, "Subir al carro toma menos de 2.6 s (acercarse a la puerta + el clip, que originalmente dura 5.5 s)");
            Assert.IsTrue(jugadorAPie.Bajar());
            t0 = Time.time;
            yield return Hasta(() => anim.Fase == AnimacionCarro.FaseCarro.Fuera, "termina de salir", 8f);
            Assert.Less(Time.time - t0, 2.2f, "Bajar del carro toma menos de 2.2 s (el clip original dura 5.8 s)");
        }
    
        public static IEnumerator Capturar(Camera camara, string ruta)
        {
            var luz = new GameObject("LuzPrueba");
            luz.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            luz.AddComponent<Light>().type = LightType.Directional;
            camara.clearFlags = CameraClearFlags.SolidColor;
            camara.backgroundColor = new Color(0.4f, 0.55f, 0.75f);
            var rt = new RenderTexture(1280, 720, 24);
            camara.targetTexture = rt;
            yield return null;
            camara.Render();
            RenderTexture.active = rt;
            var imagen = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            imagen.ReadPixels(new Rect(0f, 0f, rt.width, rt.height), 0, 0);
            imagen.Apply();
            RenderTexture.active = null;
            camara.targetTexture = null;
            string completa = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ruta));
            Directory.CreateDirectory(Path.GetDirectoryName(completa));
            File.WriteAllBytes(completa, imagen.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(imagen);
            UnityEngine.Object.DestroyImmediate(rt);
            UnityEngine.Object.Destroy(luz);
            Assert.IsTrue(File.Exists(completa));
        }

        [UnityTest]
        public IEnumerator Captura_EntrandoYManejandoEnElCarroViejo()
        {
            yield return Calentar();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Proyecto/Vehiculos/Prefabs/CarroViejo.prefab");
            carro = UnityEngine.Object.Instantiate(prefab, new Vector3(0f, 0f, 3f), Quaternion.identity);
            IMontable real = null;
            foreach (IMontable m in Registro<IMontable>.Todos) { real = m; }
            Assert.IsNotNull(real, "El carro viejo se registra");
            jugador.transform.position = real.PuntoSubida + new Vector3(-0.3f, 0f, 0f);
            jugador.transform.rotation = Quaternion.LookRotation(Vector3.right);
            yield return null;
            Animator a = jugador.GetComponentInChildren<VisualPersonaje>(true).Animador;
            AnimacionCarro anim = jugador.GetComponent<AnimacionCarro>();
            Assert.IsTrue(jugadorAPie.Subir(real));
            var cam = new GameObject("CamExterna").AddComponent<Camera>();
            cam.transform.position = carro.transform.position + new Vector3(-4.5f, 2.2f, -1.5f);
            cam.transform.LookAt(carro.transform.position + new Vector3(-0.3f, 0.9f, 0f));
            yield return Esperar(0.9f);
            yield return Capturar(cam, "docs/capturas/anim2-carro-entrar.png");
            yield return Hasta(() => anim.Fase == AnimacionCarro.FaseCarro.Dentro, "dentro", 8f);
            yield return Esperar(0.5f);
            // El vidrio del modelo es opaco: se apaga la carrocería (no las llantas) para ver la pose sentada.
            foreach (Renderer r in carro.GetComponentsInChildren<Renderer>())
            {
                if (!r.name.ToLower().Contains("rueda") && !r.name.ToLower().Contains("wheel") && !r.name.ToLower().Contains("llanta")) { r.enabled = false; }
            }
            cam.transform.position = carro.transform.position + new Vector3(-3.2f, 1.6f, 0.5f);
            cam.transform.LookAt(carro.transform.position + new Vector3(-0.4f, 1.0f, -0.1f));
            yield return Capturar(cam, "docs/capturas/anim2-carro-manejando.png");
            UnityEngine.Object.Destroy(cam.gameObject);
        }
    }
}
