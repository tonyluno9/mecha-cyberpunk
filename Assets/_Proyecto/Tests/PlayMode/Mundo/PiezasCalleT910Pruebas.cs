using System.Collections;
using System.Linq;
using MechaCyberpunk.Core;
using MechaCyberpunk.Mundo.Ambiente;
using MechaCyberpunk.Mundo.Lugares;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>
    /// T-910 (B7): en la ciudad del juego el semáforo cambia de color y la puerta de cristal del casino desliza sus dos hojas. Con la variable de
    /// entorno T910_CAPTURAS=1 también escribe docs/capturas/t910-*.png (cámara con RenderTexture: en batchmode ScreenCapture no sirve).
    /// </summary>
    public class PiezasCalleT910Pruebas
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const float SegundosParaAbrir = 1.5f;
        private const float SegundosParaCerrar = 6f;
        private const float MetrosFrente = 2.5f;
        private const float MetrosLejos = 30f;

        [UnitySetUp]
        public IEnumerator Cargar()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            for (int i = 0; i < 10; i++)
            {
                yield return null;
            }
        }

        private static Color Emision(SemaforoCiclo s, int lente)
        {
            Renderer r = s.GetComponentsInChildren<Renderer>().Where(x => x.name.StartsWith("Lente_")).ElementAt(lente);
            MaterialPropertyBlock b = new MaterialPropertyBlock();
            r.GetPropertyBlock(b);
            return b.GetColor("_EmissionColor");
        }

        private static int Encendidas(SemaforoCiclo s)
        {
            return Enumerable.Range(0, 3).Count(i => Emision(s, i).maxColorComponent > 0.1f);
        }

        private static FaseSemaforo[] Fases(SemaforoCiclo[] s) => s.Select(x => x.Fase).ToArray();

        [UnityTest]
        public IEnumerator Semaforo_CambiaDeColorYLasEsquinasCruzadasVanEnContrafase()
        {
            SemaforoCiclo[] s = Object.FindObjectsByType<SemaforoCiclo>(FindObjectsSortMode.None).Where(x => x.gameObject.activeInHierarchy).ToArray();
            Assert.GreaterOrEqual(s.Length, 4, "los semáforos del cruce");
            SemaforoCiclo normal = s.First(x => !x.Contrafase);
            SemaforoCiclo cruzada = s.First(x => x.Contrafase);
            Assert.AreNotEqual(normal.Fase == FaseSemaforo.Rojo, cruzada.Fase == FaseSemaforo.Rojo, "una en rojo y la otra no");
            FaseSemaforo inicial = normal.Fase;
            Assert.AreEqual(1, Encendidas(normal), "una luz por vez");
            Color antes = Enumerable.Range(0, 3).Select(i => Emision(normal, i)).First(c => c.maxColorComponent > 0.1f);
            // El verde dura 8 s y el ámbar 2: en 11 s cambia de luz por lo menos una vez.
            float t = 0f;
            bool cambio = false;
            while (t < 11f && !cambio)
            {
                yield return new WaitForSeconds(0.25f);
                t += 0.25f;
                cambio = normal.Fase != inicial;
            }
            Assert.IsTrue(cambio, "cambió de fase");
            yield return null;
            Assert.AreEqual(1, Encendidas(normal), "una luz por vez");
            Color despues = Enumerable.Range(0, 3).Select(i => Emision(normal, i)).First(c => c.maxColorComponent > 0.1f);
            Assert.AreNotEqual(antes, despues, "cambió el color encendido");
            CapturarSemaforo(normal);
        }

        [UnityTest]
        public IEnumerator Puerta_PC01_LasDosHojasSeDeslizanAlLadoYVuelven()
        {
            IPuerta puerta = null;
            foreach (IPuerta p in Registro<IPuerta>.Todos)
            {
                if (p.Lugar == IdLugar.Casino)
                {
                    puerta = p;
                }
            }
            Assert.IsNotNull(puerta, "la puerta del casino");
            HojasCorredizas hojas = ((Component)puerta).GetComponentInChildren<HojasCorredizas>();
            Assert.IsNotNull(hojas, "con sus hojas de cristal PC01");
            Vector3 izqCerrada = hojas.Izquierda.position;
            Vector3 derCerrada = hojas.Derecha.position;
            Assert.IsTrue(hojas.Izquierda.GetComponent<MeshRenderer>().enabled && hojas.Derecha.GetComponent<MeshRenderer>().enabled);
            IJugador jugador = Registro<IJugador>.Actual;
            Assert.IsNotNull(jugador, "hay jugador");
            // Cerrada, vista desde la calle (la fachada da al poniente).
            Vector3 haciaCalle = Vector3.left;
            Vector3 centro = (izqCerrada + derCerrada) * 0.5f + Vector3.up * 1.2f;
            Foto(centro + haciaCalle * 4f, centro, "t910-puerta-casino-cerrada");
            Mover(jugador, puerta.Posicion + haciaCalle * MetrosFrente);
            yield return new WaitForSeconds(SegundosParaAbrir);
            Assert.IsTrue(puerta.EstaAbierta, "se abrió sola");
            Vector3 mizq = hojas.Izquierda.position - izqCerrada;
            Vector3 mder = hojas.Derecha.position - derCerrada;
            Assert.Greater(mizq.magnitude, 0.5f, "la hoja izquierda se deslizó");
            Assert.Greater(mder.magnitude, 0.5f, "la hoja derecha se deslizó");
            Assert.Less(Vector3.Dot(mizq.normalized, mder.normalized), -0.9f, "cada una a su lado");
            Assert.Less(Mathf.Abs(mizq.y) + Mathf.Abs(mder.y), 0.05f, "sin subir");
            Assert.Greater((hojas.Derecha.position - hojas.Izquierda.position).magnitude, (derCerrada - izqCerrada).magnitude + 1f, "se abren");
            Mover(jugador, puerta.Posicion + haciaCalle * (MetrosFrente + 6f));
            Foto(centro + haciaCalle * 4f + Vector3.up * 0.4f, centro, "t910-puerta-casino-abierta");
            Mover(jugador, puerta.Posicion + haciaCalle * MetrosLejos);
            yield return new WaitForSeconds(SegundosParaCerrar);
            Assert.IsFalse(puerta.EstaAbierta, "se cierra sola al alejarse");
            yield return null;
            Assert.Less((hojas.Izquierda.position - izqCerrada).magnitude, 0.05f, "las hojas volvieron");
            Assert.Less((hojas.Derecha.position - derCerrada).magnitude, 0.05f);
        }

        [UnityTest]
        public IEnumerator Capturas_BasuraPasarelaYLata()
        {
            if (System.Environment.GetEnvironmentVariable("T910_CAPTURAS") != "1")
            {
                Assert.Pass("sin T910_CAPTURAS=1 no se escriben capturas");
            }
            Transform basura = GameObject.Find("Basura_BS01").transform;
            // La pieza y el ángulo con la vista libre (los postes y los muros no tapan, no hay collider en la basura).
            Vector3 mira = Vector3.zero;
            Vector3 desde = Vector3.zero;
            bool libre = false;
            foreach (Transform pieza in basura.Cast<Transform>())
            {
                foreach (Vector3 d in new[] { new Vector3(0f, 1.7f, -3.2f), new Vector3(0f, 1.7f, 3.2f), new Vector3(3.2f, 1.7f, 0f), new Vector3(-3.2f, 1.7f, 0f) })
                {
                    if (!libre && !Physics.Linecast(pieza.position + d, pieza.position + Vector3.up * 0.1f) && !Physics.Linecast(pieza.position + d, pieza.position + Vector3.up * 1.0f))
                    {
                        libre = true;
                        mira = pieza.position + Vector3.up * 0.1f;
                        desde = pieza.position + d;
                    }
                }
            }
            if (!libre)
            {
                desde = basura.GetChild(0).position + new Vector3(0f, 1.2f, -1.2f);
                mira = basura.GetChild(0).position;
            }
            Foto(desde, mira, "t910-basura-banqueta");
            Transform pasarela = GameObject.Find("T910_Pasarelas").transform.GetChild(0);
            Vector3 p = pasarela.position;
            Foto(new Vector3(p.x, p.y + 1.7f, p.z - 22f), new Vector3(p.x, p.y + 4f, p.z), "t910-pasarela-calle");
            Transform lata = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).FirstOrDefault(t => t.name == "Lata_O01");
            Assert.IsNotNull(lata, "hay una expendedora con su lata");
            Vector3 frente = -lata.parent.forward;
            Foto(lata.position + frente * 1.1f + Vector3.up * 0.55f, lata.position + Vector3.up * 0.35f, "t910-lata-expendedora");
            yield return null;
        }

        private static void CapturarSemaforo(SemaforoCiclo s)
        {
            Transform poste = s.transform.Find("Poste");
            Vector3 cara = -poste.forward;
            Vector3 centro = poste.position + Vector3.up * 3.3f;
            Foto(centro + cara * 6f + Vector3.up * 0.3f, centro, "t910-semaforo-cruce");
        }

        private static void Mover(IJugador jugador, Vector3 a)
        {
            CharacterController cc = jugador.Raiz.GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.enabled = false;
            }
            jugador.Raiz.position = new Vector3(a.x, a.y + 0.1f, a.z);
            if (cc != null)
            {
                cc.enabled = true;
            }
        }

        private static void Foto(Vector3 desde, Vector3 hacia, string nombre)
        {
            if (System.Environment.GetEnvironmentVariable("T910_CAPTURAS") != "1")
            {
                return;
            }
            GameObject go = new GameObject("CamaraFoto");
            Camera cam = go.AddComponent<Camera>();
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 300f;
            cam.fieldOfView = 60f;
            go.transform.position = desde;
            go.transform.LookAt(hacia);
            RenderTexture rt = new RenderTexture(1280, 720, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            System.IO.File.WriteAllBytes("docs/capturas/" + nombre + ".png", tex.EncodeToPNG());
            cam.targetTexture = null;
            Object.Destroy(tex);
            Object.Destroy(rt);
            Object.Destroy(go);
        }
    }
}
