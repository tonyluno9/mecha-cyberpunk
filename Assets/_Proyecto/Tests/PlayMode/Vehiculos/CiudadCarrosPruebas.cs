using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using MechaCyberpunk.Core;
using MechaCyberpunk.Vehiculos.Carro;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Vehiculos
{
    /// <summary>
    /// T-511 en Ciudad_Prologo: no queda ningún carro decorativo en el barrio, todos los estacionados son manejables y robables con los 8 modelos,
    /// sin ruedas de greybox visibles, y cada uno sale de su lugar sin atorarse en banquetas ni postes (E, W 2 s, entrada simulada).
    /// </summary>
    public class CiudadCarrosPruebas : InputTestFixture
    {
        private GameObject[] testigosCamara;
        private const string RutaCiudad = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string PatronAdorno = @"^Carro_\d+$";
        private const string PrefijoRobable = "CarroRobable_";
        private const int MinimoDeEstacionados = 16;
        private const float DistanciaAlSubir = 1.5f;
        private const float MetrosParaSalir = 4f;
        private const float SegundosDeManejo = 2f;
        private const float GradosPorPixelInicial = 0.1f;
        private const int IntentosDeGiro = 12;
        private const float ToleranciaDeGiro = 5f;
        private const int CuadrosParaAsentar = 3;
        private const int IntentosDeTecla = 5;
        private const float SegundosParaFrenar = 6f;
        private const float LimiteCarga = 30f;

        private Keyboard teclado;
        private Mouse raton;

        public override void Setup()
        {
            base.Setup();
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();
            MapasDePruebaVehiculos.Reiniciar(teclado, raton);
        }

        public override void TearDown()
        {
            Time.timeScale = 1f;
            MapasDePruebaVehiculos.AntesDelTearDown(teclado, raton);
            base.TearDown();
            MapasDePruebaVehiculos.DespuesDelTearDown();
        }

        // La ciudad no se queda cargada: las pruebas que siguen arman su carro en la escena activa.
        [UnityTearDown]
        public IEnumerator DescargarCiudad()
        {
            foreach (GameObject raiz in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                Object.Destroy(raiz);
            }
            Registros.LimpiarTodos();
            yield return null;
        }

        private static IEnumerator CargarCiudad()
        {
            Registros.LimpiarTodos();
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaCiudad, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            float limite = Time.realtimeSinceStartup + LimiteCarga;
            while (Registro<IJugador>.Actual == null)
            {
                Assert.Less(Time.realtimeSinceStartup, limite, "La ciudad trae al jugador");
                yield return null;
            }
            yield return new WaitForSecondsRealtime(1f);
        }

        private static List<CarroEstacionado> Estacionados()
        {
            return Object.FindObjectsByType<CarroEstacionado>(FindObjectsSortMode.None)
                .Where(c => c.name.StartsWith(PrefijoRobable)).OrderBy(c => c.name.Length).ThenBy(c => c.name).ToList();
        }

        private static string ModeloDe(GameObject carro)
        {
            // El modelo se reconoce por el material de su visual (V02, V03, V01a_CarroViejito...).
            Transform visual = carro.transform.Find("Visual_T511");
            Renderer r = visual != null ? visual.GetComponentInChildren<Renderer>(true) : null;
            return r != null ? r.sharedMaterial.name : carro.name;
        }

        // La cámara de hombro no gira con Colocar: se gira con el ratón, midiendo cuántos grados da cada píxel, hasta mirar al yaw pedido.
        private IEnumerator MirarHacia(IJugador jugador, Mouse raton, float yawObjetivo)
        {
            float gradosPorPixel = GradosPorPixelInicial;
            for (int i = 0; i < IntentosDeGiro; i++)
            {
                // La cámara se acomoda en LateUpdate: se mide ya asentada.
                for (int f = 0; f < CuadrosParaAsentar; f++)
                {
                    yield return null;
                }
                float antes = jugador.Camara.transform.eulerAngles.y;
                float error = Mathf.DeltaAngle(antes, yawObjetivo);
                if (Mathf.Abs(error) < ToleranciaDeGiro)
                {
                    yield break;
                }
                float pixeles = error / gradosPorPixel;
                Set(raton.delta, new Vector2(pixeles, 0f));
                yield return null;
                Set(raton.delta, Vector2.zero);
                for (int f = 0; f < CuadrosParaAsentar; f++)
                {
                    yield return null;
                }
                float cambio = Mathf.DeltaAngle(antes, jugador.Camara.transform.eulerAngles.y);
                if (Mathf.Abs(cambio) > 0.5f && Mathf.Abs(pixeles) > 0.5f)
                {
                    gradosPorPixel = Mathf.Max(0.001f, cambio / pixeles);
                }
            }
        }

        private static string Candidato(IJugador jugador)
        {
            foreach (MonoBehaviour m in ((Component)jugador).GetComponentsInChildren<MonoBehaviour>())
            {
                System.Reflection.PropertyInfo p = m.GetType().GetProperty("MontableCandidato");
                if (p != null)
                {
                    object c = p.GetValue(m);
                    return c is Component comp ? comp.name : (c == null ? "ninguno" : c.ToString());
                }
            }
            return "?";
        }

        private static IEnumerator Esperar(float segundos)
        {
            float fin = Time.time + segundos;
            while (Time.time < fin)
            {
                yield return null;
            }
        }

        // Suelta y vuelve a pulsar: una acción de botón solo ve el cambio de estado, no una tecla que ya estaba abajo.
        private IEnumerator Sostener(KeyControl tecla)
        {
            yield return Soltar(tecla);
            for (int i = 0; i < IntentosDeTecla && !tecla.isPressed; i++)
            {
                Press(tecla);
                yield return null;
            }
        }

        // Repite hasta que la tecla quede arriba de verdad (con varias cargas de carro seguidas un Release suelto a veces no se aplica).
        private IEnumerator Soltar(KeyControl tecla)
        {
            for (int i = 0; i < IntentosDeTecla && (i == 0 || tecla.isPressed); i++)
            {
                Release(tecla);
                yield return null;
            }
        }

        private IEnumerator Tecla(KeyControl tecla)
        {
            Press(tecla);
            yield return null;
            Release(tecla);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Ciudad_NoQuedaNingunCarroDecorativo_YTodosLosEstacionadosSonRobables()
        {
            yield return CargarCiudad();

            // Los Carro_N del barrio (menos los de la autopista elevada) están apagados, igual que su visual.
            List<Transform> adornos = new List<Transform>();
            foreach (GameObject raiz in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
                {
                    if (Regex.IsMatch(t.name, PatronAdorno) && !EstaBajo(t, "Autopista"))
                    {
                        adornos.Add(t);
                    }
                }
            }
            Assert.GreaterOrEqual(adornos.Count, MinimoDeEstacionados, "Los 17 lugares de adorno siguen en el prefab del barrio");
            foreach (Transform t in adornos)
            {
                Assert.IsFalse(t.gameObject.activeInHierarchy, "Sigue un carro decorativo: " + t.name);
                Transform visual = t.parent != null ? t.parent.Find(t.name + "_Visual_T250") : null;
                Assert.IsTrue(visual == null || !visual.gameObject.activeInHierarchy, "Sigue un visual decorativo: " + t.name);
            }

            // Y en su lugar hay carros manejables y robables: los 8 modelos.
            List<CarroEstacionado> estacionados = Estacionados();
            Assert.GreaterOrEqual(estacionados.Count, MinimoDeEstacionados, "Hay un carro robable por lugar");
            HashSet<string> modelos = new HashSet<string>();
            foreach (CarroEstacionado c in estacionados)
            {
                Assert.IsTrue(c.gameObject.activeInHierarchy, c.name + " activo");
                CarroViejo manejable = c.GetComponent<CarroViejo>();
                Assert.IsNotNull(manejable, c.name + " es manejable");
                Assert.IsTrue(manejable.PuedeSubir, c.name + " se puede subir");
                Assert.IsFalse(c.EsTuyo, c.name + " todavía no es tuyo (suma calor al robarlo)");
                modelos.Add(ModeloDe(c.gameObject));
                foreach (Renderer r in c.GetComponentsInChildren<Renderer>(true))
                {
                    if (r.transform.parent != null && r.transform.parent.name.StartsWith("Rueda_"))
                    {
                        Assert.IsFalse(r.enabled, c.name + ": la rueda de greybox se ve");
                    }
                }
                foreach (MeshFilter f in c.GetComponentsInChildren<MeshFilter>(false))
                {
                    MeshRenderer r = f.GetComponent<MeshRenderer>();
                    Assert.IsFalse(r != null && r.enabled && f.sharedMesh != null && f.sharedMesh.name == "Cylinder", c.name + ": hay un cilindro visible");
                }
            }
            Assert.AreEqual(8, modelos.Count, "Salen los 8 modelos: " + string.Join(", ", modelos));
        }

        [UnityTest]
        public IEnumerator Ciudad_CadaCarroEstacionado_SeRobaYSaleDeSuLugar_SinAtorarse()
        {
            yield return CargarCiudad();
            IJugador jugador = Registro<IJugador>.Actual;
            ICalor calor = Registro<ICalor>.Actual;
            List<CarroEstacionado> estacionados = Estacionados();
            Assert.GreaterOrEqual(estacionados.Count, MinimoDeEstacionados);
            List<string> atorados = new List<string>();
            Dictionary<CarroEstacionado, Pose> lugares = estacionados.ToDictionary(c => c, c => new Pose(c.transform.position, c.transform.rotation));
            foreach (CarroEstacionado estacionado in estacionados)
            {
                CarroViejo carro = estacionado.GetComponent<CarroViejo>();
                // Cada carro se prueba sin el calor del anterior (con calor alto la policía tumba al jugador y le quita el control).
                calor.Borrar();
                // Junto a la puerta del conductor, mirándola.
                Vector3 punto = carro.PuntoSubida;
                Vector3 posicion = punto - carro.transform.right * DistanciaAlSubir;
                posicion.y = punto.y + 0.1f;
                Vector3 mirada = punto - posicion;
                mirada.y = 0f;
                jugador.Colocar(new Pose(posicion, Quaternion.LookRotation(mirada.normalized, Vector3.up)));
                yield return MirarHacia(jugador, raton, Quaternion.LookRotation(mirada.normalized, Vector3.up).eulerAngles.y);
                yield return Esperar(0.4f);
                // T-661: robar un carro solo sube el calor si alguien lo ve; aquí lo ven tres cámaras de seguridad alrededor.
                if (testigosCamara == null)
                {
                    testigosCamara = new GameObject[3];
                    for (int k = 0; k < testigosCamara.Length; k++)
                    {
                        testigosCamara[k] = new GameObject("CamaraTestigo" + k);
                        testigosCamara[k].AddComponent<MechaCyberpunk.Trafico.Testigos.CamaraSeguridad>().Configurar(60f, 89f);
                    }
                }
                Vector3[] lados = { Vector3.up * 8f, Vector3.up * 5f + Vector3.right * 4f, Vector3.up * 5f + Vector3.left * 4f };
                for (int k = 0; k < testigosCamara.Length; k++)
                {
                    testigosCamara[k].transform.position = posicion + lados[k];
                    testigosCamara[k].transform.rotation = Quaternion.LookRotation(posicion + Vector3.up - testigosCamara[k].transform.position, Vector3.up);
                }
                float antes = calor.Valor;

                yield return Tecla(teclado.eKey);
                if (jugador.Modo != ModoJugador.Carro || !carro.Ocupado)
                {
                    atorados.Add(estacionado.name + " no dejó subir (modo " + jugador.Modo + ", ocupado " + carro.Ocupado + ", candidato " + Candidato(jugador) + ", puedeSubir " + carro.PuedeSubir + ", jugador " + ((Component)jugador).transform.position.ToString("F1") + " cam " + jugador.Camara.transform.eulerAngles.y.ToString("F0") + " punto " + carro.PuntoSubida.ToString("F1") + ")");
                    if (jugador.Modo != ModoJugador.APie)
                    {
                        yield return Tecla(teclado.eKey);
                    }
                    continue;
                }
                Assert.GreaterOrEqual(calor.Valor, antes + 25f, estacionado.name + ": robarlo sube el calor (antes " + antes + ", ahora " + calor.Valor + ")");
                yield return Esperar(0.4f);
                Vector3 inicio = carro.transform.position;
                yield return Sostener(teclado.wKey);
                yield return Esperar(SegundosDeManejo);
                string estado = " [ctrl " + carro.TieneControl + " acel " + carro.EntradaActual.Acelerar + " w " + teclado.wKey.isPressed + " ruedas " + carro.RuedasEnPiso + " dormido " + carro.EstaDormido + " kin " + carro.GetComponent<Rigidbody>().isKinematic + " v " + carro.Velocidad + "]";
                yield return Soltar(teclado.wKey);
                float recorrido = Vector3.Distance(inicio, carro.transform.position);
                if (recorrido < MetrosParaSalir)
                {
                    atorados.Add(estacionado.name + " (" + ModeloDe(estacionado.gameObject) + ") solo avanzó " + recorrido.ToString("F1") + " m en " + estacionado.transform.position.ToString("F0") + estado);
                }
                // Frena y baja (E) para seguir con el siguiente.
                yield return Sostener(teclado.sKey);
                float tope = Time.time + SegundosParaFrenar;
                while (!carro.PuedeBajar && Time.time < tope)
                {
                    yield return null;
                }
                yield return Soltar(teclado.sKey);
                yield return Tecla(teclado.eKey);
                // Todos de vuelta a su lugar: el carro manejado puede empujar a otro estacionado más adelante en la calle.
                if (jugador.Modo == ModoJugador.APie)
                {
                    foreach (KeyValuePair<CarroEstacionado, Pose> lugar in lugares)
                    {
                        Rigidbody cuerpo = lugar.Key.GetComponent<Rigidbody>();
                        cuerpo.linearVelocity = Vector3.zero;
                        cuerpo.angularVelocity = Vector3.zero;
                        lugar.Key.transform.SetPositionAndRotation(lugar.Value.position, lugar.Value.rotation);
                    }
                    Physics.SyncTransforms();
                }
                if (jugador.Modo != ModoJugador.APie)
                {
                    atorados.Add(estacionado.name + " no dejó bajar (acel " + carro.EntradaActual.Acelerar + " frena " + carro.EntradaActual.Frenar + " w " + teclado.wKey.isPressed + " s " + teclado.sKey.isPressed + " v " + carro.Velocidad.ToString("F2") + ", puedeBajar " + carro.PuedeBajar + " en " + carro.transform.position.ToString("F1") + ")");
                    carro.PermitirBajarYa();
                    yield return Tecla(teclado.eKey);
                }
            }
            Assert.IsEmpty(atorados, "Carros atorados:\n" + string.Join("\n", atorados));
        }

        private static bool EstaBajo(Transform t, string nombre)
        {
            for (Transform p = t; p != null; p = p.parent)
            {
                if (p.name == nombre)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
