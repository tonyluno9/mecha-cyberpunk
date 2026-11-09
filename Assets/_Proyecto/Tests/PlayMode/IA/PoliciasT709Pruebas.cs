using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.Trafico.Datos;
using MechaCyberpunk.Trafico.Persecucion;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests
{
    /// <summary>
    /// T-709: al llegar, los policías se bajan de la patrulla, se cubren detrás de ella, asoman y disparan (con la línea de vista de T-676: las paredes
    /// paran las balas); dispararles tiene efecto: reaccionan, mueren y suben la búsqueda. Patrulla real sobre las calles de prueba y el jugador real.
    /// </summary>
    public class PoliciasT709Pruebas
    {
        private const string RutaConfig = "Assets/_Proyecto/Trafico/Datos/ConfigPersecucion_Trafico.asset";
        private const string RutaDron = "Assets/_Proyecto/Trafico/Prefabs/Dron.prefab";
        private const string RutaPatrulla = "Assets/_Proyecto/Trafico/Prefabs/Patrulla.prefab";
        private const string RutaJugador = "Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab";
        private const string RutaPolicia = "Assets/_Proyecto/IA/Prefabs/PoliciaAPie.prefab";
        private const float Paso = 94f;
        private const float EscalaDeTiempo = 3f;
        private static readonly Vector2[] Centros =
        {
            new Vector2(-Paso, Paso / 2f), new Vector2(0f, Paso / 2f), new Vector2(Paso, Paso / 2f),
            new Vector2(-Paso, -Paso / 2f), new Vector2(0f, -Paso / 2f), new Vector2(Paso, -Paso / 2f)
        };

        private sealed class CalorDoble : ICalor
        {
            public float Valor { get; private set; }
            public float Maximo => 100f;
            public int Nivel => Valor >= 70f ? 3 : (Valor >= 40f ? 2 : (Valor >= 10f ? 1 : 0));
            public event Action<float> ValorCambio;
            public event Action<int> NivelCambio;
            private void Poner(float valor)
            {
                int antes = Nivel;
                Valor = Mathf.Clamp(valor, 0f, Maximo);
                ValorCambio?.Invoke(Valor);
                if (Nivel != antes) NivelCambio?.Invoke(Nivel);
            }
            public void Sumar(float cantidad, MotivoCalor motivo) => Poner(Valor + cantidad);
            public void Enfriar(float cantidad) => Poner(Valor - cantidad);
            public void Borrar() => Poner(0f);
        }

        private readonly List<GameObject> extras = new List<GameObject>();
        private GameObject barrio;
        private GameObject jugador;
        private ConfigPersecucion config;
        private GestorPersecucion gestor;
        private CalorDoble calor;
        private GrafoCalles grafo;
        private Func<float> azarAnterior;
        private float escalaAnterior;

        [SetUp]
        public void Preparar()
        {
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
            EventosPolicia.LimpiarSuscriptores();
            EventosPelea.LimpiarSuscriptores();
            escalaAnterior = Time.timeScale;
            azarAnterior = PistolaPandillero.Azar;
            PistolaPandillero.Azar = () => 1f; // por defecto no acierta; las pruebas que miden daño lo cambian
            config = Object.Instantiate(AssetDatabase.LoadAssetAtPath<ConfigPersecucion>(RutaConfig));
            typeof(ConfigPersecucion).GetField("segundosArresto", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(config, 9999f);
            GameObject piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(900f, 1f, 900f);
            extras.Add(piso);
            barrio = new GameObject("BarrioPrueba");
            for (int i = 0; i < Centros.Length; i++)
            {
                GameObject m = new GameObject("Manzana_" + (i + 1));
                m.transform.SetParent(barrio.transform, false);
                m.transform.position = new Vector3(Centros[i].x, 0f, Centros[i].y);
            }
            grafo = GrafoCalles.DeManzanas(Centros, 80f, 0.2f);
            Vector3 punto = grafo.Nodo(grafo.MasCercano(new Vector3(0f, 0f, 0f)));
            jugador = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaJugador), new Vector3(punto.x, 0.05f, punto.z), Quaternion.identity);
            calor = new CalorDoble();
            Registro<ICalor>.Agregar(calor); // después del jugador: el último registrado es el que usa todo
            GameObject camObjeto = new GameObject("CamaraPrueba");
            camObjeto.tag = "MainCamera";
            camObjeto.AddComponent<Camera>();
            camObjeto.transform.position = new Vector3(0f, 600f, 0f);
            camObjeto.transform.rotation = Quaternion.Euler(-90f, 0f, 0f); // mira al cielo: nada de lo que nace se ve
            extras.Add(camObjeto);
            GameObject objeto = new GameObject("Persecucion");
            objeto.SetActive(false);
            objeto.transform.SetParent(barrio.transform, false);
            gestor = objeto.AddComponent<GestorPersecucion>();
            gestor.Configurar(config, AssetDatabase.LoadAssetAtPath<GameObject>(RutaDron).GetComponent<DronMundo>(),
                AssetDatabase.LoadAssetAtPath<GameObject>(RutaPatrulla).GetComponent<PatrullaMundo>());
            objeto.SetActive(true);
        }

        [TearDown]
        public void Limpiar()
        {
            Time.timeScale = escalaAnterior;
            PistolaPandillero.Azar = azarAnterior;
            foreach (GameObject extra in extras)
            {
                if (extra != null)
                {
                    Object.DestroyImmediate(extra);
                }
            }
            extras.Clear();
            foreach (PandilleroAPie p in Object.FindObjectsByType<PandilleroAPie>(FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(p.gameObject);
            }
            Object.DestroyImmediate(barrio);
            Object.DestroyImmediate(jugador);
            Object.DestroyImmediate(config);
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
            EventosPolicia.LimpiarSuscriptores();
            EventosPelea.LimpiarSuscriptores();
        }

        private static IEnumerator Esperar(float segundos)
        {
            float fin = Time.time + segundos;
            while (Time.time < fin)
            {
                yield return null;
            }
        }

        private List<PandilleroAPie> Policias() => Object.FindObjectsByType<PandilleroAPie>(FindObjectsSortMode.None).Where(p => p.Datos.EsPolicia).ToList();

        // Sube el calor y deja que la patrulla llegue y baje a sus policías.
        private IEnumerator EsperarPolicias(Action<PatrullaMundo> listo)
        {
            calor.Sumar(15f, MotivoCalor.Mision);
            Time.timeScale = EscalaDeTiempo;
            float limite = Time.time + 120f;
            PatrullaMundo patrulla = null;
            while (Time.time < limite)
            {
                patrulla = gestor.GetComponentsInChildren<PatrullaMundo>().FirstOrDefault(p => p.PoliciasBajaron);
                if (patrulla != null)
                {
                    break;
                }
                yield return null;
            }
            Assert.IsNotNull(patrulla, "La patrulla llega y baja a sus policías");
            listo(patrulla);
        }

        private sealed class TestigoQueSiempreVe : MechaCyberpunk.Trafico.Testigos.ITestigoPolicia
        {
            public bool VeElPunto(Vector3 punto) => true;
        }

        [UnityTest]
        public IEnumerator AlLlegar_LosPoliciasSeBajan_SeCubrenTrasLaPatrulla_AsomanYDisparan()
        {
            PatrullaMundo patrulla = null;
            yield return EsperarPolicias(p => patrulla = p);
            List<PandilleroAPie> policias = Policias();
            Assert.AreEqual(config.PoliciasPorPatrulla, policias.Count, "Se bajan los policías de la patrulla");
            foreach (PandilleroAPie p in policias)
            {
                Assert.LessOrEqual(Vector3.Distance(p.transform.position, patrulla.transform.position), 4f, "Salen por las puertas de la patrulla");
                Assert.IsTrue(p.Alerta, "El policía está en alerta contra el jugador");
                Assert.AreEqual("POLICÍA", p.Datos.Nombre);
            }

            bool seCubrio = false;
            bool asomo = false;
            float limite = Time.time + 60f;
            PistolaPandillero[] pistolas = policias.Select(p => p.GetComponent<PistolaPandillero>()).ToArray();
            while (Time.time < limite && !(seCubrio && asomo && pistolas.Sum(x => x.Disparos) > 0))
            {
                foreach (PandilleroAPie p in policias)
                {
                    seCubrio |= p.ModoTactico == ReglaTacticaPandillero.Modo.Cubrirse && p.TieneCubierta;
                    asomo |= p.ModoTactico == ReglaTacticaPandillero.Modo.Asomado;
                }
                yield return null;
            }
            Time.timeScale = 1f;
            Assert.IsTrue(seCubrio, "Se cubren detrás de la patrulla");
            Assert.IsTrue(asomo, "Asoman");
            Assert.Greater(pistolas.Sum(x => x.Disparos), 0, "Y disparan");

            // La cubierta que eligieron es la patrulla: su punto queda junto a ella y del lado contrario al jugador.
            PandilleroAPie cubierto = policias.First(p => p.TieneCubierta);
            Vector3 jug = jugador.transform.position;
            Assert.Less(Vector3.Distance(cubierto.PuntoCubierta, patrulla.transform.position), 6f, "El punto de cubierta está junto a la patrulla");
            Assert.Greater(Vector3.Distance(cubierto.PuntoCubierta, jug), Vector3.Distance(patrulla.transform.position, jug) - 0.5f,
                "Se esconde del lado contrario al jugador");
            // T-711: cada policía va a una esquina distinta de la patrulla (no se encimen).
            Time.timeScale = EscalaDeTiempo;
            float limiteEsquinas = Time.time + 30f;
            while (Time.time < limiteEsquinas && !policias.All(p => p.TieneCubierta && Vector3.Distance(p.transform.position, p.PuntoCubierta) < 0.8f))
            {
                yield return null;
            }
            Time.timeScale = 1f;
            Debug.Log("[T-711] patrulla " + patrulla.transform.position + " fwd " + patrulla.transform.forward + " jugador " + jugador.transform.position + " puestos " + policias[0].PuntoCubierta + " / " + policias[1].PuntoCubierta + " cuerpos " + string.Join(";", patrulla.GetComponentsInChildren<Collider>().Select(c => c.bounds.ToString())));
            Assert.IsTrue(policias.All(p => p.TieneCubierta), "Los dos tienen su puesto de cubierta");
            Assert.GreaterOrEqual(Vector3.Distance(policias[0].PuntoCubierta, policias[1].PuntoCubierta), 1.8f, "Cada policía tiene su puesto: no es el mismo punto");
            Assert.GreaterOrEqual(Vector3.Distance(policias[0].transform.position, policias[1].transform.position), 1.5f, "Y ya parados no están encimados");
            Vector3 ejePatrulla = patrulla.transform.forward;
            float a = Vector3.Dot(policias[0].PuntoCubierta - patrulla.transform.position, ejePatrulla);
            float b = Vector3.Dot(policias[1].PuntoCubierta - patrulla.transform.position, ejePatrulla);
            Debug.Log("[T-711] Puestos de los policías sobre el largo de la patrulla: " + a.ToString("F2") + " y " + b.ToString("F2"));

            if (Environment.GetEnvironmentVariable("CAPTURAR_S1") == "1")
            {
                yield return Captura(patrulla, "s1c-T-711-policias.png");
            }
        }

        private IEnumerator Captura(PatrullaMundo patrulla, string archivo)
        {
            GameObject objetoCamara = new GameObject("CamaraCaptura");
            Camera camara = objetoCamara.AddComponent<Camera>();
            camara.farClipPlane = 300f;
            // Desde detrás de los policías (del lado contrario al jugador), mirando la patrulla y al jugador al fondo.
            Vector3 haciaJugador = (jugador.transform.position - patrulla.transform.position).normalized;
            Vector3 lado = Vector3.Cross(Vector3.up, haciaJugador);
            objetoCamara.transform.position = patrulla.transform.position - haciaJugador * 6f + lado * 3f + Vector3.up * 2.6f;
            objetoCamara.transform.LookAt(patrulla.transform.position + haciaJugador * 2f + Vector3.up * 0.8f);
            yield return null;
            RenderTexture rt = new RenderTexture(1280, 720, 24);
            camara.targetTexture = rt;
            camara.Render();
            RenderTexture.active = rt;
            Texture2D foto = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            foto.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            foto.Apply();
            camara.targetTexture = null;
            RenderTexture.active = null;
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(Application.dataPath, "..", "docs", "capturas", archivo), foto.EncodeToPNG());
            Object.DestroyImmediate(foto);
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(objetoCamara);
        }

        [UnityTest]
        public IEnumerator UnaPared_ParaLasBalasDelPolicia_YSinPared_LeDanAlJugador()
        {
            PistolaPandillero.Azar = () => 0f; // siempre acierta si hay línea de vista
            PatrullaMundo patrulla = null;
            yield return EsperarPolicias(p => patrulla = p);
            Time.timeScale = 1f;
            IVida vida = jugador.GetComponentInChildren<IVida>();
            Assert.IsNotNull(vida);

            // Una pared sólida entre la patrulla y el jugador: todos los tiros chocan con ella.
            Vector3 centro = Vector3.Lerp(patrulla.transform.position, jugador.transform.position, 0.5f);
            Vector3 eje = (jugador.transform.position - patrulla.transform.position).normalized;
            GameObject pared = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pared.transform.position = new Vector3(centro.x, 3f, centro.z);
            pared.transform.rotation = Quaternion.LookRotation(eje);
            pared.transform.localScale = new Vector3(40f, 6f, 0.5f);
            extras.Add(pared);
            yield return null;
            float antes = vida.Actual;
            PistolaPandillero[] pistolas = Policias().Select(p => p.GetComponent<PistolaPandillero>()).ToArray();
            int disparosAntes = pistolas.Sum(x => x.Disparos);
            yield return Esperar(6f);
            Assert.AreEqual(antes, vida.Actual, 0.001f, "Con la pared en medio, las balas de la policía no llegan al jugador");
            Assert.AreEqual(disparosAntes, pistolas.Sum(x => x.Disparos), "Sin línea de vista no disparan");

            Object.DestroyImmediate(pared);
            yield return null;
            float limite = Time.time + 40f;
            while (Time.time < limite && vida.Actual >= antes)
            {
                yield return null;
            }
            Assert.Less(vida.Actual, antes, "Sin la pared, las balas sí le pegan al jugador");
        }

        [UnityTest]
        public IEnumerator DispararleAUnPolicia_LoHace_Reaccionar_SubeLaBusqueda_YPuedeMorir()
        {
            GameObject p = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPolicia), jugador.transform.position + new Vector3(0f, 0f, 15f), Quaternion.identity);
            PandilleroAPie policia = p.GetComponent<PandilleroAPie>();
            yield return null;
            Assert.IsTrue(policia.Datos.EsPolicia);
            // T-933: el calor solo sube si lo ve la policía; este policía suelto no cuenta, así que otro testigo de prueba ve.
            Registro<MechaCyberpunk.Trafico.Testigos.ITestigoPolicia>.Agregar(new TestigoQueSiempreVe());
            float antes = calor.Valor;
            float vidaAntes = policia.Actual;
            policia.RecibirDanio(new InfoDanio(5f, TipoDanio.Disparo, p.transform.position + Vector3.up, Vector3.back, null));
            Assert.Less(policia.Actual, vidaAntes, "Recibe el daño");
            Assert.AreEqual(antes + policia.Datos.CalorAlGolpear, calor.Valor, 0.01f, "Dispararle sube la búsqueda");
            Assert.IsFalse(policia.EstaKO);

            antes = calor.Valor;
            policia.RecibirDanio(new InfoDanio(policia.Maxima * 2f, TipoDanio.Disparo, p.transform.position + Vector3.up, Vector3.back, null));
            Assert.IsTrue(policia.EstaKO, "Puede morir");
            Assert.AreEqual(antes + policia.Datos.CalorAlMatar, calor.Valor, 0.01f, "Matarlo sube la búsqueda más");
            yield return Esperar(0.3f);
            Assert.IsFalse(p.GetComponentInChildren<Animator>().GetBool("KO") && false);
        }
    }
}
