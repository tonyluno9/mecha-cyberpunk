using System.Collections;
using System.IO;
using MechaCyberpunk.Combate.Vida;
using MechaCyberpunk.Core;
using MechaCyberpunk.Mecha.Cabina;
using MechaCyberpunk.Mecha.Estado;
using MechaCyberpunk.Mecha.Movimiento;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Mecha
{
    /// <summary>T-105: el cristal se cuartea en dos niveles con la vida y se limpia al reparar. Prefab real.</summary>
    public class CristalCuarteadoPruebas : InputTestFixture
    {
        private const int FramesEstables = 10;
        private const int MaxFramesCalentamiento = 1000;
        private const float DeltaTimeEstable = 0.05f;
        private const string RutaPrefab = "Assets/_Proyecto/Mecha/Prefabs/MechaJugador.prefab";
        private const string RutaRival = "Assets/_Proyecto/IA/Prefabs/RivalBasico.prefab";
        private const float VidaNivel1 = 45f;
        private const float VidaNivel2 = 20f;
        private const float DanioMortal = 100000f;
        private const float DistanciaRival = 7f;
        private const string VariableCaptura = "CAPTURAR_T105";
        private const string RutaCaptura1 = "docs/capturas/T-105-1.png";
        private const string RutaCaptura2 = "docs/capturas/T-105-2.png";
        private const float EsperaCaptura = 0.6f;
        private const int AnchoCaptura = 1280;
        private const int AltoCaptura = 720;
        private const int ProfundidadCaptura = 24;

        private GameObject piso;
        private GameObject mecha;
        private GameObject rival;
        private VidaMecha vida;
        private CristalCuarteado cristal;

        public override void Setup()
        {
            base.Setup();
            InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();

            piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(400f, 1f, 400f);

            mecha = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab), new Vector3(0f, 0.05f, 0f), Quaternion.identity);
            vida = mecha.GetComponent<VidaMecha>();
            cristal = mecha.GetComponentInChildren<CristalCuarteado>(true);
        }

        public override void TearDown()
        {
            Object.Destroy(mecha);
            Object.Destroy(piso);
            if (rival != null)
            {
                Object.Destroy(rival);
            }
            base.TearDown();
        }

        private static IEnumerator Calentar()
        {
            int estables = 0;
            int intentos = 0;
            while (estables < FramesEstables && intentos < MaxFramesCalentamiento)
            {
                yield return null;
                intentos++;
                estables = Time.deltaTime < DeltaTimeEstable ? estables + 1 : 0;
            }
        }

        private bool Grupo(string nombre)
        {
            return cristal.transform.Find(nombre).gameObject.activeSelf;
        }

        private void AfirmarGrupos(bool grupo1, bool grupo2, string contexto)
        {
            Assert.AreEqual(grupo1, Grupo("Grietas1"), contexto + ": Grietas1");
            Assert.AreEqual(grupo2, Grupo("Grietas2"), contexto + ": Grietas2");
        }

        [UnityTest]
        public IEnumerator ConVidaMaxima_LosDosGruposEstanApagados()
        {
            yield return Calentar();
            Assert.IsNotNull(cristal, "El prefab lleva CristalCuarteado");
            Assert.AreEqual(0, cristal.Nivel);
            AfirmarGrupos(false, false, "vida máxima");
        }

        [UnityTest]
        public IEnumerator ConVida45_SoloElGrupoUno()
        {
            yield return Calentar();
            vida.FijarVida(VidaNivel1);
            Assert.AreEqual(1, cristal.Nivel);
            AfirmarGrupos(true, false, "vida 45");
        }

        [UnityTest]
        public IEnumerator ConVida20_LosDosGrupos()
        {
            yield return Calentar();
            vida.FijarVida(VidaNivel2);
            Assert.AreEqual(2, cristal.Nivel);
            AfirmarGrupos(true, true, "vida 20");
        }

        [UnityTest]
        public IEnumerator EnKO_LosDosGrupos()
        {
            yield return Calentar();
            vida.RecibirDanio(new InfoDanio(DanioMortal, TipoDanio.Golpe, Vector3.zero, Vector3.forward, null));
            Assert.IsTrue(vida.EstaKO);
            Assert.AreEqual(2, cristal.Nivel);
            AfirmarGrupos(true, true, "KO");
        }

        [UnityTest]
        public IEnumerator AlReparar_NingunGrupo()
        {
            yield return Calentar();
            vida.FijarVida(VidaNivel2);
            AfirmarGrupos(true, true, "antes de reparar");
            mecha.GetComponent<MechaPersistente>().Reparar();
            Assert.AreEqual(0, cristal.Nivel);
            AfirmarGrupos(false, false, "reparado");
        }

        [UnityTest]
        public IEnumerator Captura_Nivel1YNivel2_SeVenDesdeLaCabinaConRivalAlCentro()
        {
            yield return Calentar();
            rival = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaRival),
                new Vector3(0f, 0.05f, DistanciaRival), Quaternion.Euler(0f, 180f, 0f));
            // El rival se queda quieto: solo es para ver que se distingue por el cristal.
            foreach (MonoBehaviour comportamiento in rival.GetComponents<MonoBehaviour>())
            {
                if (comportamiento.GetType() != typeof(VidaMecha))
                {
                    comportamiento.enabled = false;
                }
            }
            yield return null;

            vida.FijarVida(VidaNivel1);
            yield return new WaitForSeconds(EsperaCaptura);
            float variacion1 = Capturar(RutaCaptura1);
            vida.FijarVida(VidaNivel2);
            yield return new WaitForSeconds(EsperaCaptura);
            float variacion2 = Capturar(RutaCaptura2);

            Assert.Greater(variacion1, 0.1f, "La imagen del nivel 1 es plana");
            Assert.Greater(variacion2, 0.1f, "La imagen del nivel 2 es plana");
        }

        private float Capturar(string ruta)
        {
            Camera camara = mecha.GetComponent<ControladorMecha>().Camara.GetComponent<Camera>();
            RenderTexture rt = new RenderTexture(AnchoCaptura, AltoCaptura, ProfundidadCaptura);
            Texture2D imagen = new Texture2D(AnchoCaptura, AltoCaptura, TextureFormat.RGB24, false);
            RenderTexture anterior = RenderTexture.active;
            camara.targetTexture = rt;
            camara.Render();
            RenderTexture.active = rt;
            imagen.ReadPixels(new Rect(0f, 0f, AnchoCaptura, AltoCaptura), 0, 0);
            imagen.Apply();
            camara.targetTexture = null;
            RenderTexture.active = anterior;

            float minimo = 1f;
            float maximo = 0f;
            Color32[] pixeles = imagen.GetPixels32();
            for (int i = 0; i < pixeles.Length; i += 97)
            {
                float brillo = pixeles[i].g / 255f;
                minimo = Mathf.Min(minimo, brillo);
                maximo = Mathf.Max(maximo, brillo);
            }
            if (System.Environment.GetEnvironmentVariable(VariableCaptura) == "1")
            {
                string completa = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ruta));
                Directory.CreateDirectory(Path.GetDirectoryName(completa));
                File.WriteAllBytes(completa, imagen.EncodeToPNG());
            }
            Object.Destroy(rt);
            Object.Destroy(imagen);
            return maximo - minimo;
        }
    }
}
