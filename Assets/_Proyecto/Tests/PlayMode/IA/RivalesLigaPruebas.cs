using System.Collections;
using System.Collections.Generic;
using System.IO;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.Rival;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.IA
{
    /// <summary>Pruebas jugables de T-157: los 20 rivales de liga nacen en una arena de prueba con su alias y su vida.</summary>
    public class RivalesLigaPruebas
    {
        private const int Ligas = 4;
        private const int RivalesPorLiga = 5;
        private const float Separacion = 6f;
        private const float MargenVida = 1e-3f;
        private const string RutaCaptura = "docs/capturas/T-157-1.png";
        private const string VariableCaptura = "CAPTURAR_T157";
        private const int LigaCaptura = 1;
        private const int AnchoCaptura = 1280;
        private const int AltoCaptura = 400;
        private const int ProfundidadCaptura = 24;
        private const int SaltoDePixeles = 97;
        private const float VariacionMinima = 0.1f;
        private const float SeparacionCaptura = 3.2f;
        private const float DistanciaCamara = 11f;
        private const float AlturaCamara = 2.2f;
        private const float AlturaMirada = 1.4f;
        private const float CampoVision = 40f;

        private readonly List<GameObject> creados = new List<GameObject>();
        private readonly List<string> errores = new List<string>();

        [SetUp]
        public void Preparar()
        {
            EventosPelea.LimpiarSuscriptores();
            errores.Clear();
            Application.logMessageReceived += Registrar;
            GameObject piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(200f, 1f, 200f);
            creados.Add(piso);
        }

        [TearDown]
        public void Limpiar()
        {
            Application.logMessageReceived -= Registrar;
            foreach (GameObject objeto in creados)
            {
                if (objeto != null)
                {
                    Object.Destroy(objeto);
                }
            }
            creados.Clear();
            EventosPelea.LimpiarSuscriptores();
        }

        private void Registrar(string mensaje, string pila, LogType tipo)
        {
            if (tipo == LogType.Error || tipo == LogType.Exception || tipo == LogType.Assert)
            {
                errores.Add(mensaje);
            }
        }

        [UnityTest]
        public IEnumerator Rivales_LosVeinte_NacenConSuAliasSuVidaYSinErrores()
        {
            List<GameObject> rivales = new List<GameObject>();
            for (int l = 1; l <= Ligas; l++)
            {
                for (int n = 1; n <= RivalesPorLiga; n++)
                {
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                        "Assets/_Proyecto/IA/Prefabs/Ligas/RivalLiga" + l + "_" + n + ".prefab");
                    Assert.IsNotNull(prefab, "Falta RivalLiga" + l + "_" + n);
                    GameObject instancia = Object.Instantiate(prefab, new Vector3((n - 1) * Separacion, 1f, (l - 1) * Separacion), Quaternion.identity);
                    creados.Add(instancia);
                    rivales.Add(instancia);
                }
            }
            yield return null;
            yield return null;

            Assert.AreEqual(Ligas * RivalesPorLiga, rivales.Count);
            foreach (GameObject rival in rivales)
            {
                IPeleador peleador = rival.GetComponent<IPeleador>();
                RivalBasico cerebro = rival.GetComponent<RivalBasico>();
                DatosRival datos = new SerializedObject(cerebro).FindProperty("datos").objectReferenceValue as DatosRival;
                Assert.AreEqual(datos.Alias, peleador.Nombre, rival.name);
                Assert.AreEqual(datos.Mecha.VidaMaxima, peleador.Vida.Actual, MargenVida, rival.name);
                Assert.AreEqual(datos.Mecha.VidaMaxima, peleador.Vida.Maxima, MargenVida, rival.name);
            }
            CollectionAssert.IsEmpty(errores, string.Join("\n", errores));
        }

        [UnityTest]
        public IEnumerator Captura_CincoRivalesDeLaLiga1_SeVenLadoALado()
        {
            for (int n = 1; n <= RivalesPorLiga; n++)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/_Proyecto/IA/Prefabs/Ligas/RivalLiga" + LigaCaptura + "_" + n + ".prefab");
                float x = ((RivalesPorLiga - 1) * 0.5f - (n - 1)) * SeparacionCaptura;
                creados.Add(Object.Instantiate(prefab, new Vector3(x, 0.1f, 0f), Quaternion.Euler(0f, 180f, 0f)));
            }
            GameObject luz = new GameObject("LuzCaptura");
            luz.AddComponent<Light>().type = LightType.Directional;
            luz.transform.rotation = Quaternion.Euler(40f, 20f, 0f);
            creados.Add(luz);
            GameObject objetoCamara = new GameObject("CamaraCaptura");
            Camera camara = objetoCamara.AddComponent<Camera>();
            camara.fieldOfView = CampoVision;
            camara.transform.position = new Vector3(0f, AlturaCamara, DistanciaCamara);
            camara.transform.LookAt(new Vector3(0f, AlturaMirada, 0f));
            creados.Add(objetoCamara);
            yield return null;
            yield return null;

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
            for (int i = 0; i < pixeles.Length; i += SaltoDePixeles)
            {
                float brillo = pixeles[i].g / 255f;
                minimo = Mathf.Min(minimo, brillo);
                maximo = Mathf.Max(maximo, brillo);
            }
            if (System.Environment.GetEnvironmentVariable(VariableCaptura) == "1")
            {
                string ruta = Path.GetFullPath(Path.Combine(Application.dataPath, "..", RutaCaptura));
                Directory.CreateDirectory(Path.GetDirectoryName(ruta));
                File.WriteAllBytes(ruta, imagen.EncodeToPNG());
            }
            Object.Destroy(rt);
            Object.Destroy(imagen);

            Assert.Greater(maximo - minimo, VariacionMinima, "La imagen es plana: no se ve nada");
            CollectionAssert.IsEmpty(errores, string.Join("\n", errores));
        }
    }
}
