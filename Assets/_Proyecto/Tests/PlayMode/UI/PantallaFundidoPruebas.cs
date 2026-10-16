using System.Collections;
using System.Collections.Generic;
using System.IO;
using MechaCyberpunk.UI;
using MechaCyberpunk.UI.Historia;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>El fundido a negro de T-125: tiempos, cortes sin saltos, clics y captura.</summary>
    public class PantallaFundidoPruebas
    {
        private const string RutaPrefab = "Assets/_Proyecto/UI/Prefabs/PantallasHistoria.prefab";
        private const string RutaCaptura = "docs/capturas/T-125-1.png";
        private const float MinimoSegundos = 0.55f;
        private const float MaximoSegundos = 0.75f;
        private const float MargenFrames = 3f;
        private const int AnchoCaptura = 1280;
        private const int AltoCaptura = 720;

        private readonly List<GameObject> creados = new List<GameObject>();
        private PantallaFundido fundido;

        [SetUp]
        public void Preparar()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab);
            Assert.IsNotNull(prefab, "Existe el prefab PantallasHistoria");
            GameObject instancia = Object.Instantiate(prefab);
            creados.Add(instancia);
            fundido = instancia.GetComponentInChildren<PantallaFundido>();
            Assert.IsNotNull(fundido);
        }

        [TearDown]
        public void Limpiar()
        {
            foreach (GameObject objeto in creados)
            {
                if (objeto != null)
                {
                    Object.Destroy(objeto);
                }
            }
            creados.Clear();
        }

        [UnityTest]
        public IEnumerator Prefab_Arranca_ConFundidoTransparenteYActual()
        {
            yield return null;
            Assert.AreEqual(0f, fundido.Alfa, 0.0001f);
            Assert.IsFalse(fundido.Opaco);
            Assert.AreSame(fundido, PantallaFundido.Actual);
            Assert.AreEqual(PantallaFundido.OrdenDibujo, fundido.GetComponent<Canvas>().sortingOrder);
            Assert.AreEqual(RenderMode.ScreenSpaceOverlay, fundido.GetComponent<Canvas>().renderMode);
            Assert.AreEqual(0.6f, fundido.Config.SegundosSalida, 0.0001f);
            Assert.AreEqual(0.6f, fundido.Config.SegundosEntrada, 0.0001f);
        }

        [UnityTest]
        public IEnumerator Fundir_AUno_TardaLoPedidoYAvisaUnaVez()
        {
            int avisos = 0;
            fundido.Terminado += () => avisos++;
            float inicio = Time.unscaledTime;
            yield return fundido.Fundir(1f, 0.6f);
            float duracion = Time.unscaledTime - inicio;
            Assert.GreaterOrEqual(duracion, MinimoSegundos);
            Assert.LessOrEqual(duracion, MaximoSegundos);
            Assert.AreEqual(1f, fundido.Alfa, 0.0001f);
            Assert.IsTrue(fundido.Opaco);
            yield return null;
            yield return null;
            Assert.AreEqual(1, avisos, "Terminado se avisa una sola vez");
        }

        [UnityTest]
        public IEnumerator Fundir_CortadoAMitad_BajaDesdeElAlfaActualSinSaltar()
        {
            int avisos = 0;
            fundido.Terminado += () => avisos++;
            Coroutine subida = fundido.StartCoroutine(fundido.Fundir(1f, 0.6f));
            float inicio = Time.unscaledTime;
            while (Time.unscaledTime - inicio < 0.3f)
            {
                yield return null;
            }
            float alfaCorte = fundido.Alfa;
            Assert.Greater(alfaCorte, 0.1f);
            Assert.Less(alfaCorte, 0.9f);

            Coroutine bajada = fundido.StartCoroutine(fundido.Fundir(0f, 0.6f));
            yield return null;
            Assert.LessOrEqual(fundido.Alfa, alfaCorte, "No salta a 1: baja desde donde estaba");
            Assert.Less(fundido.Alfa, 0.95f);
            float anterior = fundido.Alfa;
            yield return null;
            Assert.LessOrEqual(fundido.Alfa, anterior);

            yield return bajada;
            Assert.AreEqual(0f, fundido.Alfa, 0.0001f);
            Assert.AreEqual(1, avisos, "Solo el fundido que terminó avisa");
            Assert.IsNotNull(subida);
        }

        [UnityTest]
        public IEnumerator Fijar_CortaFundidoEnCurso()
        {
            fundido.StartCoroutine(fundido.Fundir(1f, 0.6f));
            yield return null;
            fundido.Fijar(0.25f);
            yield return null;
            yield return null;
            Assert.AreEqual(0.25f, fundido.Alfa, 0.0001f);
        }

        [UnityTest]
        public IEnumerator Transparente_NoBloqueaClicSobreBotonDebajo()
        {
            GameObject sistema = new GameObject("EventSystem", typeof(EventSystem));
            creados.Add(sistema);
            GameObject lienzo = new GameObject("LienzoBoton", typeof(Canvas), typeof(GraphicRaycaster));
            creados.Add(lienzo);
            lienzo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            GameObject boton = new GameObject("Boton", typeof(RectTransform), typeof(Image), typeof(Button));
            boton.transform.SetParent(lienzo.transform, false);
            RectTransform rect = (RectTransform)boton.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            yield return null;

            Assert.AreSame(boton, PrimerImpacto(), "Transparente el clic llega al botón");
            fundido.Fijar(1f);
            yield return null;
            GameObject impacto = PrimerImpacto();
            Assert.IsNotNull(impacto);
            Assert.AreNotSame(boton, impacto, "Opaco el fundido tapa el clic");
            fundido.Fijar(0f);
            yield return null;
            Assert.AreSame(boton, PrimerImpacto(), "Al volver a transparente el clic vuelve a llegar");
        }

        [UnityTest]
        public IEnumerator Captura_FundidoAMedias_GuardaPng()
        {
            GameObject camObjeto = new GameObject("CamCaptura", typeof(Camera));
            creados.Add(camObjeto);
            Camera camara = camObjeto.GetComponent<Camera>();
            camara.clearFlags = CameraClearFlags.SolidColor;
            camara.backgroundColor = new Color(0.1f, 0.5f, 0.7f);
            GameObject piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            creados.Add(piso);
            piso.transform.position = new Vector3(0f, 0f, 4f);
            piso.transform.localScale = new Vector3(3f, 2f, 1f);
            piso.transform.rotation = Quaternion.Euler(20f, 30f, 0f);

            // La herramienta de capturas no dibuja Overlay: solo en memoria pasa a Screen Space Camera.
            Canvas canvas = fundido.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camara;
            canvas.planeDistance = 1f;
            fundido.Fijar(0.5f);
            yield return null;

            RenderTexture destino = RenderTexture.GetTemporary(AnchoCaptura, AltoCaptura, 24);
            camara.targetTexture = destino;
            camara.Render();
            RenderTexture previa = RenderTexture.active;
            RenderTexture.active = destino;
            Texture2D textura = new Texture2D(AnchoCaptura, AltoCaptura, TextureFormat.RGB24, false);
            textura.ReadPixels(new Rect(0, 0, AnchoCaptura, AltoCaptura), 0, 0);
            textura.Apply();
            RenderTexture.active = previa;
            camara.targetTexture = null;
            RenderTexture.ReleaseTemporary(destino);

            Color centro = textura.GetPixel(AnchoCaptura / 2, AltoCaptura / 2);
            Color esquina = textura.GetPixel(4, 4);
            Assert.Less(esquina.b, 0.7f * 0.75f, "El fondo azul se ve oscurecido a la mitad");
            Assert.Greater(esquina.b, 0.7f * 0.25f);
            Assert.IsNotNull(centro);
            File.WriteAllBytes(Path.Combine(Application.dataPath, "..", RutaCaptura), textura.EncodeToPNG());
            Object.Destroy(textura);
        }

        private static GameObject PrimerImpacto()
        {
            PointerEventData datos = new PointerEventData(EventSystem.current)
            {
                position = new Vector2(Screen.width / 2f, Screen.height / 2f)
            };
            List<RaycastResult> resultados = new List<RaycastResult>();
            EventSystem.current.RaycastAll(datos, resultados);
            return resultados.Count > 0 ? resultados[0].gameObject : null;
        }
    }
}
