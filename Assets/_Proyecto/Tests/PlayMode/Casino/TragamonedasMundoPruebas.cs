using System.Collections;
using System.IO;
using System.Linq;
using MechaCyberpunk.Casino.Datos;
using MechaCyberpunk.Casino.Fichas;
using MechaCyberpunk.Casino.Tragamonedas;
using MechaCyberpunk.Core;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace MechaCyberpunk.Tests.Casino
{
    /// <summary>La tragamonedas jugable (T-301): cobro, premio, giro, texto y uso con E. Usa un doble de fichas y azar con semilla.</summary>
    public class TragamonedasMundoPruebas : InputTestFixture
    {
        private const string RutaPrefab = "Assets/_Proyecto/Casino/Prefabs/Tragamonedas.prefab";
        private const string RutaDatos = "Assets/_Proyecto/Casino/Datos/DatosTragamonedas_Base.asset";
        private const string RutaCaptura = "docs/capturas/T-301-1.png";
        private const float SegundosMaximosGiro = 6f;
        private const float AlcanceUso = 2.5f;
        private const int SemillasMaximas = 10000;
        private const int Anchura = 1280;
        private const int Altura = 720;
        private const int BitsProfundidad = 24;
        private const int FichasSuficientes = 1000;
        private const float FOVCaptura = 38f;

        private sealed class FichasDoble : IFichas
        {
            public int Saldo { get; set; }
            public int Cobros;
            public int Abonos;
            public int TotalAbonado;

            public bool PuedePagar(int cantidad) => cantidad > 0 && Saldo >= cantidad;

            public bool IntentarPagar(int cantidad)
            {
                if (!PuedePagar(cantidad))
                {
                    return false;
                }
                Saldo -= cantidad;
                Cobros++;
                return true;
            }

            public void Abonar(int cantidad)
            {
                if (cantidad <= 0)
                {
                    return;
                }
                Saldo += cantidad;
                Abonos++;
                TotalAbonado += cantidad;
            }
        }


        /// <summary>Jugador de prueba: con E usa el interactuable más cercano a 2.5 m, como lo hace el jugador a pie de Tony.</summary>
        private sealed class JugadorDePrueba : MonoBehaviour
        {
            private InputAction interactuar;

            private void OnEnable()
            {
                interactuar = new InputAction("Interactuar", InputActionType.Button, "<Keyboard>/e");
                interactuar.Enable();
            }

            private void OnDisable()
            {
                interactuar.Disable();
                interactuar.Dispose();
            }

            private void Update()
            {
                if (!interactuar.WasPressedThisFrame())
                {
                    return;
                }
                IInteractuable mejor = null;
                float mejorDistancia = AlcanceUso;
                foreach (IInteractuable candidato in Registro<IInteractuable>.Todos)
                {
                    float distancia = Vector3.Distance(transform.position, candidato.Posicion);
                    if (distancia <= mejorDistancia)
                    {
                        mejor = candidato;
                        mejorDistancia = distancia;
                    }
                }
                if (mejor != null && mejor.PuedeUsar)
                {
                    mejor.Usar();
                }
            }
        }

        private Keyboard teclado;
        private DatosTragamonedas datos;
        private TragamonedasMundo maquina;
        private FichasDoble fichas;
        private GameObject jugador;
        private GameObject escenario;

        public override void Setup()
        {
            base.Setup();
            teclado = InputSystem.AddDevice<Keyboard>();
            datos = AssetDatabase.LoadAssetAtPath<DatosTragamonedas>(RutaDatos);
            Assert.IsNotNull(datos, "Falta " + RutaDatos);
            fichas = new FichasDoble();
        }

        public override void TearDown()
        {
            if (maquina != null)
            {
                Object.DestroyImmediate(maquina.gameObject);
            }
            if (jugador != null)
            {
                Object.DestroyImmediate(jugador);
            }
            if (escenario != null)
            {
                Object.DestroyImmediate(escenario);
            }
            base.TearDown();
        }

        private TragamonedasMundo CrearMaquina(int semilla)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab);
            Assert.IsNotNull(prefab, "Falta " + RutaPrefab + "; corre CreadorTragamonedas.Crear.");
            GameObject instancia = Object.Instantiate(prefab);
            instancia.name = "Tragamonedas_Prueba";
            TragamonedasMundo nueva = instancia.GetComponent<TragamonedasMundo>();
            nueva.Inyectar(fichas, new System.Random(semilla));
            maquina = nueva;
            return nueva;
        }

        // Busca una semilla cuyo primer giro paga (o no paga); el giro usa el azar antes que la animación, así que coincide.
        private int BuscarSemilla(bool paga, out int[] simbolos, out int premio)
        {
            CalculoTragamonedas calculo = datos.Crear();
            for (int semilla = 0; semilla < SemillasMaximas; semilla++)
            {
                int[] giro = calculo.Girar(new System.Random(semilla));
                int pago = calculo.Pago(giro, datos.Apuesta);
                if ((pago > 0) == paga)
                {
                    simbolos = giro;
                    premio = pago;
                    return semilla;
                }
            }
            Assert.Fail("No hay semilla " + (paga ? "ganadora" : "perdedora") + " en " + SemillasMaximas);
            simbolos = null;
            premio = 0;
            return -1;
        }

        private IEnumerator EsperarFinGiro(TragamonedasMundo m)
        {
            float limite = Time.realtimeSinceStartup + SegundosMaximosGiro;
            while (m.Girando && Time.realtimeSinceStartup < limite)
            {
                yield return null;
            }
            Assert.IsFalse(m.Girando, "El giro no terminó en " + SegundosMaximosGiro + " s.");
        }

        [Test]
        public void Prefab_TieneMaquinaConTresCarretesYLuz()
        {
            TragamonedasMundo m = CrearMaquina(1);
            Assert.AreEqual(3, m.GetComponentsInChildren<Text>().Length - 1, "Tres carretes más el marcador.");
            Assert.IsNotNull(m.GetComponentInChildren<Light>(), "Falta la luz de neón.");
            Assert.AreEqual(datos.Apuesta, m.Apuesta);
            Assert.IsTrue(Registro<IInteractuable>.Todos.Contains(m), "Debe registrarse en OnEnable.");
        }

        [Test]
        public void Desactivar_QuitaLaMaquinaDelRegistro()
        {
            TragamonedasMundo m = CrearMaquina(1);
            m.gameObject.SetActive(false);
            Assert.IsFalse(Registro<IInteractuable>.Todos.Contains(m));
            m.gameObject.SetActive(true);
            Assert.IsTrue(Registro<IInteractuable>.Todos.Contains(m));
        }

        [Test]
        public void SinFichas_NoGiraNiCobraYAvisa()
        {
            TragamonedasMundo m = CrearMaquina(1);
            fichas.Saldo = datos.Apuesta - 1;
            Assert.IsFalse(m.PuedeUsar);
            StringAssert.StartsWith("Sin fichas", m.Texto);
            m.Usar();
            Assert.IsFalse(m.Girando);
            Assert.AreEqual(0, fichas.Cobros);
            Assert.AreEqual(0, fichas.Abonos);
            Assert.AreEqual(datos.Apuesta - 1, fichas.Saldo);
        }

        [UnityTest]
        public IEnumerator ConFichas_CobraUnaVezYAbonaElPremioUnaVez()
        {
            int semilla = BuscarSemilla(true, out int[] esperado, out int premio);
            TragamonedasMundo m = CrearMaquina(semilla);
            fichas.Saldo = FichasSuficientes;
            StringAssert.StartsWith("Jugar", m.Texto);
            Assert.IsTrue(m.PuedeUsar);

            m.Usar();
            m.Usar();
            Assert.IsTrue(m.Girando);
            Assert.IsFalse(m.PuedeUsar, "Mientras gira no se puede usar.");
            Assert.AreEqual(1, fichas.Cobros, "Un solo cobro aunque se use dos veces.");
            Assert.AreEqual(FichasSuficientes - datos.Apuesta, fichas.Saldo);
            Assert.AreEqual(0, fichas.Abonos, "El premio se abona al parar, no antes.");

            yield return EsperarFinGiro(m);

            Assert.AreEqual(1, fichas.Cobros);
            Assert.AreEqual(1, fichas.Abonos, "El premio se abona una sola vez.");
            Assert.AreEqual(premio, fichas.TotalAbonado);
            Assert.AreEqual(FichasSuficientes - datos.Apuesta + premio, fichas.Saldo);
            CollectionAssert.AreEqual(esperado, m.UltimoResultado);
            Text[] carretes = m.GetComponentsInChildren<Text>();
            for (int i = 0; i < esperado.Length; i++)
            {
                Assert.AreEqual(datos.Simbolos[esperado[i]].nombre, carretes[i].text, "El carrete " + i + " muestra su símbolo.");
            }
            StringAssert.StartsWith("Jugar", m.Texto);
        }

        [UnityTest]
        public IEnumerator GiroPerdedor_CobraYNoAbonaNada()
        {
            int semilla = BuscarSemilla(false, out _, out _);
            TragamonedasMundo m = CrearMaquina(semilla);
            fichas.Saldo = FichasSuficientes;
            m.Usar();
            yield return EsperarFinGiro(m);
            Assert.AreEqual(1, fichas.Cobros);
            Assert.AreEqual(0, fichas.Abonos);
            Assert.AreEqual(0, m.UltimoPremio);
            Assert.AreEqual(FichasSuficientes - datos.Apuesta, fichas.Saldo);
        }

        [UnityTest]
        public IEnumerator ApagarAMediaVuelta_TerminaElGiroYPagaElPremio()
        {
            int semilla = BuscarSemilla(true, out _, out int premio);
            TragamonedasMundo m = CrearMaquina(semilla);
            fichas.Saldo = FichasSuficientes;
            m.Usar();
            yield return null;
            Assert.IsTrue(m.Girando);
            m.gameObject.SetActive(false);
            Assert.IsFalse(m.Girando);
            Assert.AreEqual(1, fichas.Abonos);
            Assert.AreEqual(premio, fichas.TotalAbonado);
        }

        [UnityTest]
        public IEnumerator ConE_ElJugadorCercaGiraYElTextoVuelveAJugar()
        {
            int semilla = BuscarSemilla(true, out _, out int premio);
            TragamonedasMundo m = CrearMaquina(semilla);
            fichas.Saldo = FichasSuficientes;
            jugador = new GameObject("JugadorDePrueba");
            jugador.transform.position = m.Posicion + Vector3.forward * 0.5f;
            jugador.AddComponent<JugadorDePrueba>();
            yield return null;

            Press(teclado.eKey);
            yield return null;
            Release(teclado.eKey);
            yield return null;

            Assert.IsTrue(m.Girando, "La E con el jugador cerca empieza el giro.");
            Assert.AreEqual("Girando…", m.Texto);
            Assert.AreEqual(1, fichas.Cobros);

            yield return EsperarFinGiro(m);

            StringAssert.StartsWith("Jugar", m.Texto);
            Assert.AreEqual(1, fichas.Abonos);
            Assert.AreEqual(premio, fichas.TotalAbonado);
        }

        [UnityTest]
        public IEnumerator ConE_ElJugadorLejosNoGira()
        {
            TragamonedasMundo m = CrearMaquina(1);
            fichas.Saldo = FichasSuficientes;
            jugador = new GameObject("JugadorDePrueba");
            jugador.transform.position = m.Posicion + Vector3.forward * (AlcanceUso + 2f);
            jugador.AddComponent<JugadorDePrueba>();
            yield return null;

            Press(teclado.eKey);
            yield return null;
            Release(teclado.eKey);
            yield return null;

            Assert.IsFalse(m.Girando);
            Assert.AreEqual(0, fichas.Cobros);
        }

        [UnityTest]
        public IEnumerator Captura_DeLaMaquinaConUnPremio()
        {
            int semilla = BuscarSemilla(true, out _, out _);
            TragamonedasMundo m = CrearMaquina(semilla);
            fichas.Saldo = FichasSuficientes;
            escenario = new GameObject("EscenarioCaptura");
            GameObject piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.SetParent(escenario.transform, false);
            piso.transform.localScale = new Vector3(6f, 0.1f, 6f);
            piso.transform.position = new Vector3(0f, -0.05f, 0f);
            piso.GetComponent<MeshRenderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Proyecto/Casino/Materiales/Tragamonedas_Cuerpo.mat");
            GameObject luzObjeto = new GameObject("LuzAmbiente");
            luzObjeto.transform.SetParent(escenario.transform, false);
            luzObjeto.transform.rotation = Quaternion.Euler(50f, 160f, 0f);
            Light luz = luzObjeto.AddComponent<Light>();
            luz.type = LightType.Directional;
            luz.intensity = 0.8f;
            luz.color = new Color(0.6f, 0.65f, 1f);
            GameObject camaraObjeto = new GameObject("CamaraCaptura");
            camaraObjeto.transform.SetParent(escenario.transform, false);
            Camera camara = camaraObjeto.AddComponent<Camera>();
            camara.clearFlags = CameraClearFlags.SolidColor;
            camara.backgroundColor = new Color(0.03f, 0.02f, 0.07f);
            camara.fieldOfView = FOVCaptura;
            camaraObjeto.transform.position = m.transform.position + new Vector3(0.9f, 1.3f, 4.2f);
            camaraObjeto.transform.LookAt(m.transform.position + new Vector3(0f, 0.95f, 0f));

            m.Usar();
            yield return EsperarFinGiro(m);
            yield return null;

            RenderTexture rt = new RenderTexture(Anchura, Altura, BitsProfundidad);
            camara.targetTexture = rt;
            camara.Render();
            RenderTexture.active = rt;
            Texture2D imagen = new Texture2D(Anchura, Altura, TextureFormat.RGB24, false);
            imagen.ReadPixels(new Rect(0f, 0f, Anchura, Altura), 0, 0);
            imagen.Apply();
            RenderTexture.active = null;
            camara.targetTexture = null;
            string completa = Path.GetFullPath(Path.Combine(Application.dataPath, "..", RutaCaptura));
            Directory.CreateDirectory(Path.GetDirectoryName(completa));
            File.WriteAllBytes(completa, imagen.EncodeToPNG());
            Color[] pixeles = imagen.GetPixels();
            double suma = 0.0;
            foreach (Color p in pixeles)
            {
                suma += 0.2126f * p.r + 0.7152f * p.g + 0.0722f * p.b;
            }
            Object.DestroyImmediate(imagen);
            Object.DestroyImmediate(rt);
            float media = (float)(suma / pixeles.Length);
            Debug.Log("[Casino] Captura " + RutaCaptura + ": luminancia media " + media.ToString("0.000"));
            Assert.Greater(media, 0.02f, "La captura no debe salir negra.");
            Assert.Less(media, 0.9f, "La captura no debe salir quemada.");
        }
    }
}
