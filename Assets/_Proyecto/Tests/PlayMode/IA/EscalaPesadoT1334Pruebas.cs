using System;
using System.Collections;
using System.Linq;
using System.Text;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.IA.Datos;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests
{
    /// <summary>
    /// T-1334: mide (BakeMesh con el Animator corriendo) cuánto miden el guardia de Nova y el pesado con escudo, y afirma que el pesado queda a +-4 % del guardia.
    /// </summary>
    public class EscalaPesadoT1334Pruebas
    {
        private sealed class JugadorQuieto : IJugador
        {
            public Transform Raiz { get; set; }
            public ModoJugador Modo => ModoJugador.APie;
            public Camera Camara => null;
            public event Action<ModoJugador> ModoCambio { add { } remove { } }
            public void Colocar(Pose pose) => Raiz.SetPositionAndRotation(pose.position, pose.rotation);
        }

        private sealed class CalorDoble : ICalor
        {
            public float Valor { get; private set; }
            public float Maximo => 100f;
            public int Nivel => Valor >= 70f ? 3 : (Valor >= 40f ? 2 : (Valor >= 10f ? 1 : 0));
            public event Action<float> ValorCambio;
            public event Action<int> NivelCambio;
            public void Poner(float valor)
            {
                Valor = Mathf.Clamp(valor, 0f, Maximo);
                ValorCambio?.Invoke(Valor);
                NivelCambio?.Invoke(Nivel);
            }
            public void Sumar(float cantidad, MotivoCalor motivo) => Poner(Valor + cantidad);
            public void Enfriar(float cantidad) => Poner(Valor - cantidad);
            public void Borrar() => Poner(0f);
        }

        private sealed class ZonaDoble : IZonaBusqueda
        {
            public bool Activa => true;
            public Vector3 Centro => Vector3.zero;
            public float Radio => 50f;
            public bool Visto => true;
        }

        private GameObject piso;
        private GameObject jugador;
        private CalorDoble calor;
        private RespuestaNova respuesta;
        private ConfigRespuestaNova config;
        private bool habiaActual;

        [SetUp]
        public void Preparar()
        {
            Registros.LimpiarTodos();
            piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(200f, 1f, 200f);
            jugador = new GameObject("JugadorNova1334");
            jugador.AddComponent<CapsuleCollider>().center = Vector3.up;
            Registro<IJugador>.Agregar(new JugadorQuieto { Raiz = jugador.transform });
            calor = new CalorDoble();
            Registro<ICalor>.Agregar(calor);
            Registro<IZonaBusqueda>.Agregar(new ZonaDoble());
            habiaActual = RespuestaNova.Actual != null;
            if (habiaActual)
            {
                RespuestaNova.Actual.enabled = false;
            }
            config = Resources.Load<ConfigRespuestaNova>(ConfigRespuestaNova.Recurso);
            Assert.IsNotNull(config, "IA/Resources/ConfigRespuestaNova");
            respuesta = new GameObject("RespuestaNovaPrueba1334").AddComponent<RespuestaNova>();
            respuesta.Configurar(config);
        }

        [TearDown]
        public void Limpiar()
        {
            Time.timeScale = 1f;
            Object.DestroyImmediate(respuesta.gameObject);
            Object.DestroyImmediate(piso);
            Object.DestroyImmediate(jugador);
            foreach (PandilleroAPie p in Object.FindObjectsByType<PandilleroAPie>(FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(p.gameObject);
            }
            foreach (DestelloLlegadaNova d in Object.FindObjectsByType<DestelloLlegadaNova>(FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(d.gameObject);
            }
            if (habiaActual && RespuestaNova.Actual != null)
            {
                RespuestaNova.Actual.enabled = true;
            }
            Registros.LimpiarTodos();
        }

        private static IEnumerator Esperar(float segundos)
        {
            float fin = Time.time + segundos;
            while (Time.time < fin)
            {
                yield return null;
            }
        }

        /// <summary>Altura en mundo (max.y - min.y) de los vértices de los SkinnedMeshRenderer del cuerpo (sin escudo ni armas) tras BakeMesh.</summary>
        private static float Altura(GameObject agente, StringBuilder detalle)
        {
            float min = float.MaxValue;
            float max = float.MinValue;
            var malla = new Mesh();
            foreach (SkinnedMeshRenderer r in agente.GetComponentsInChildren<SkinnedMeshRenderer>(false))
            {
                string nombre = r.name.ToLowerInvariant();
                if (nombre.Contains("escudo") || nombre.Contains("shield") || nombre.Contains("arma") || nombre.Contains("gun"))
                {
                    detalle.Append(" [omitido ").Append(r.name).Append(']');
                    continue;
                }
                malla.Clear();
                r.BakeMesh(malla, true);
                // BakeMesh deja los vértices en el espacio local del renderer sin su escala: se pasan a mundo con posición, rotación y lossyScale.
                Matrix4x4 aMundo = Matrix4x4.TRS(r.transform.position, r.transform.rotation, r.transform.lossyScale);
                float rMin = float.MaxValue;
                float rMax = float.MinValue;
                foreach (Vector3 v in malla.vertices)
                {
                    float y = aMundo.MultiplyPoint3x4(v).y;
                    rMin = Mathf.Min(rMin, y);
                    rMax = Mathf.Max(rMax, y);
                }
                min = Mathf.Min(min, rMin);
                max = Mathf.Max(max, rMax);
                detalle.Append(" [").Append(r.name).Append(' ').Append((rMax - rMin).ToString("F2")).Append(" m, escala ").Append(r.transform.lossyScale.y.ToString("F2")).Append(']');
            }
            Object.Destroy(malla);
            return max > min ? max - min : 0f;
        }

        private static void ForzarAnimar(GameObject agente)
        {
            foreach (Animator a in agente.GetComponentsInChildren<Animator>())
            {
                a.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }
            foreach (SkinnedMeshRenderer r in agente.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                r.updateWhenOffscreen = true;
            }
        }

        [UnityTest]
        public IEnumerator PesadoConEscudo_MideComoElGuardia_MasMenos4Porciento()
        {
            calor.Poner(75f);
            yield return Esperar(0.6f);
            PandilleroAPie guardia = respuesta.Guardias.First(g => g != null);
            PandilleroAPie pesado = respuesta.Pesados.First(g => g != null);
            ForzarAnimar(guardia.gameObject);
            ForzarAnimar(pesado.gameObject);
            yield return Esperar(4.5f); // caen de 12 m, aterrizan y quedan en idle
            var dg = new StringBuilder();
            var dp = new StringBuilder();
            float hGuardia = Altura(guardia.gameObject, dg);
            float hPesado = Altura(pesado.gameObject, dp);
            float escalaActual = config.EscalaPesado;
            float sinEscala = hPesado / Mathf.Max(0.0001f, escalaActual);
            float sugerida = hGuardia / Mathf.Max(0.0001f, sinEscala);
            Debug.Log("[T-1334] guardia=" + hGuardia.ToString("F3") + " pesado=" + hPesado.ToString("F3") + " escalaActual=" + escalaActual.ToString("F3")
                + " escalaSugerida=" + sugerida.ToString("F3") + " | guardia:" + dg + " | pesado:" + dp);
            Assert.Greater(hGuardia, 0.5f, "se midió al guardia");
            Assert.Greater(hPesado, 0.5f, "se midió al pesado");
            Assert.AreEqual(hGuardia, hPesado, hGuardia * 0.04f, "el pesado mide lo mismo que el guardia (+-4 %)");
            if (Environment.GetEnvironmentVariable("CAPTURAR_T1334") == "1")
            {
                yield return CapturarJuntos(guardia.transform, pesado.transform, "docs/capturas/t-1334/pesado-junto-guardia.png");
            }
        }

        // Pone al guardia y al pesado lado a lado, de frente a la cámara, y guarda la foto.
        private static IEnumerator CapturarJuntos(Transform guardia, Transform pesado, string ruta)
        {
            Vector3 centro = guardia.position;
            centro.y = 0f;
            guardia.SetPositionAndRotation(centro + Vector3.left * 1.3f, Quaternion.Euler(0f, 180f, 0f));
            pesado.SetPositionAndRotation(centro + Vector3.right * 1.3f, Quaternion.Euler(0f, 180f, 0f));
            GameObject cam = new GameObject("CamaraT1334");
            Camera c = cam.AddComponent<Camera>();
            cam.transform.position = centro + new Vector3(0f, 1.4f, -6.5f);
            cam.transform.LookAt(centro + Vector3.up * 1.3f);
            c.fieldOfView = 45f;
            c.clearFlags = CameraClearFlags.SolidColor;
            c.backgroundColor = new Color(0.4f, 0.55f, 0.75f);
            GameObject luz = new GameObject("LuzT1334");
            luz.transform.rotation = Quaternion.Euler(40f, 20f, 0f);
            luz.AddComponent<Light>().type = LightType.Directional;
            RenderTexture rt = new RenderTexture(1200, 900, 24);
            c.targetTexture = rt;
            guardia.SetPositionAndRotation(centro + Vector3.left * 1.3f, Quaternion.Euler(0f, 180f, 0f));
            pesado.SetPositionAndRotation(centro + Vector3.right * 1.3f, Quaternion.Euler(0f, 180f, 0f));
            c.Render();
            RenderTexture.active = rt;
            Texture2D imagen = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            imagen.ReadPixels(new Rect(0f, 0f, rt.width, rt.height), 0, 0);
            imagen.Apply();
            RenderTexture.active = null;
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(ruta));
            System.IO.File.WriteAllBytes(ruta, imagen.EncodeToPNG());
            c.targetTexture = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(imagen);
            Object.DestroyImmediate(cam);
            Object.DestroyImmediate(luz);
            yield return null;
        }
    }
}
