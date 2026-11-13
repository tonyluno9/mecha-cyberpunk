using System.Collections;
using MechaCyberpunk.Personaje.Visual;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests
{
    /// <summary>T-985: estados nuevos del jugador (PJ01): giro apuntando con pistola, puños en guardia y claxon sin clips de Mixamo.</summary>
    public class ClipsPJ01JugadorT985Pruebas
    {
        private const string RutaPrefab = "Assets/_Proyecto/Personaje/Visual/Prefabs/Visual_PJ01.prefab";
        private GameObject suelo;
        private GameObject raiz;
        private VisualPersonaje visual;
        private Animator animador;

        [SetUp]
        public void SetUp()
        {
            suelo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            suelo.transform.position = new Vector3(0f, -0.5f, 0f);
            suelo.transform.localScale = new Vector3(50f, 1f, 50f);
            raiz = new GameObject("PadreVisual");
            GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab);
            visual = Object.Instantiate(prefab, raiz.transform).GetComponent<VisualPersonaje>();
            visual.LeerDelPadre = false;
            visual.GuardiaTrasGolpe = true;
            animador = visual.Animador;
            animador.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(suelo);
            Object.Destroy(raiz);
        }

        private string ClipDe(int capa)
        {
            AnimatorClipInfo[] clips = animador.GetCurrentAnimatorClipInfo(capa);
            return clips.Length > 0 ? clips[0].clip.name : "";
        }

        private bool EnEstado(int capa, string nombre) => animador.GetCurrentAnimatorStateInfo(capa).IsName(nombre);

        private IEnumerator Hasta(System.Func<bool> condicion, string que, float limite = 3f)
        {
            float fin = Time.time + limite;
            while (!condicion() && Time.time < fin)
            {
                yield return null;
            }
            Assert.IsTrue(condicion(), "No llegó: " + que);
        }

        [UnityTest]
        public IEnumerator ApuntarConPistolaYGirar_JuegaLosClipsPJ01DeGiroApuntando()
        {
            yield return new WaitForSeconds(0.3f);
            visual.TipoArma = TipoArmaVisual.Pistola;
            visual.Apuntando = true;
            yield return new WaitForSeconds(0.6f);
            animador.SetFloat(ReglasAnimacion.Giro, 150f);
            yield return Hasta(() => EnEstado(0, "GirarApuntarDer"), "GirarApuntarDer");
            yield return null;
            StringAssert.StartsWith("PJ01_Pistola_ApuntarCaminar_Derecha", ClipDe(0));
            animador.SetFloat(ReglasAnimacion.Giro, 0f);
            yield return Hasta(() => EnEstado(0, "Idle"), "Idle al dejar de girar", 4f);
            animador.SetFloat(ReglasAnimacion.Giro, -150f);
            yield return Hasta(() => EnEstado(0, "GirarApuntarIzq"), "GirarApuntarIzq");
            yield return null;
            StringAssert.StartsWith("PJ01_Pistola_ApuntarCaminar_Izquierda", ClipDe(0));
        }

        [UnityTest]
        public IEnumerator GirarSinApuntar_SigueSiendoElGiroNormal()
        {
            yield return new WaitForSeconds(0.3f);
            animador.SetFloat(ReglasAnimacion.Giro, 150f);
            yield return Hasta(() => EnEstado(0, "GirarDer"), "GirarDer");
            Assert.IsFalse(EnEstado(0, "GirarApuntarDer"));
        }

        [UnityTest]
        public IEnumerator GolpearConPunos_QuedaEnGuardiaYLuegoSuelta()
        {
            yield return new WaitForSeconds(0.3f);
            visual.Golpear(false);
            yield return Hasta(() => EnEstado(1, "GolpeIzq") || EnEstado(1, "GolpeDer"), "golpe");
            yield return Hasta(() => EnEstado(1, "PunosGuardia"), "PunosGuardia", 4f);
            Assert.AreEqual("PJ01_Punos_Guardia", ClipDe(1));
            yield return Hasta(() => EnEstado(1, "Vacio"), "Vacio tras " + ReglasAnimacion.SegundosGuardiaPunos + " s", ReglasAnimacion.SegundosGuardiaPunos + 3f);
        }

        [UnityTest]
        public IEnumerator Pitar_JuegaUnClipPJ01()
        {
            yield return new WaitForSeconds(0.3f);
            visual.Manejando = true;
            visual.Pitar();
            yield return Hasta(() => EnEstado(1, "Pitar"), "Pitar");
            yield return null;
            StringAssert.StartsWith("PJ01_", ClipDe(1));
        }
    }
}
