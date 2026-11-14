using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MechaCyberpunk.Core.Trepar;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.Progresion.Pruebas;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>
    /// S2 techos B: en Ciudad_Prologo (escena real) cada acceso a las azoteas tiene suelo y espacio arriba y abajo, hay accesos suficientes
    /// por zona, y el jugador real, con entrada simulada, sube por una escalera de servicio y por una ruta de parkour (contenedor, barda,
    /// escalera) y camina 5 s por una azotea con pretil sin caer a la calle.
    /// </summary>
    public class TechosS2Pruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string RaizTechos = "S2_Techos";
        private static readonly string[] ZonasCasas = { "S2c_Suburbios", "S2c_CalleDorada", "S2c_Mercado" };
        private const int AccesosPorZona = 3;
        private const int AccesosTotal = 20;
        private const int RutasMinimas = 4;
        private const float TolSuelo = 0.3f;
        private const float RadioJugador = 0.3f;
        private const float AlturaJugador = 1.8f;
        private const float MaxSegundosSubida = 25f;
        private const float TolTecho = 0.2f;

        private Keyboard teclado;
        private GameObject jugador;
        private ControladorAPie controlador;
        private TreparAPie trepar;
        private CharacterController cc;

        private static Transform RaizDeTechos => GameObject.Find(RaizTechos)?.transform;

        private static List<PuntoTrepar> PuntosDeTechos()
        {
            Transform raiz = RaizDeTechos;
            Assert.IsNotNull(raiz, "Falta " + RaizTechos + " en Ciudad_Prologo");
            return raiz.GetComponentsInChildren<PuntoTrepar>(true).ToList();
        }

        private static string ZonaDe(PuntoTrepar p)
        {
            string nombre = p.transform.parent.name.StartsWith("Ruta_") ? p.transform.parent.name.Substring("Ruta_".Length) : p.name.Substring("Acceso_".Length);
            return ZonasCasas.Concat(new[] { "Barrio" }).FirstOrDefault(z => nombre.StartsWith(z)) ?? nombre;
        }

        // Otras clases dejan la Ciudad cargada: se descarta antes de cada prueba y después (para no contaminar a las siguientes).
        [UnitySetUp]
        public IEnumerator DescargarCiudadPrevia()
        {
            yield return DescargarCiudades("TechosS2_VaciaAntes");
        }

        [UnityTearDown]
        public IEnumerator DescargarCiudadAlTerminar()
        {
            yield return DescargarCiudades("TechosS2_VaciaDespues");
        }

        private static IEnumerator DescargarCiudades(string nombreVacia)
        {
            bool hayCiudad = false;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                hayCiudad |= SceneManager.GetSceneAt(i).path.Contains("Ciudad");
            }
            if (!hayCiudad)
            {
                yield break;
            }
            SceneManager.SetActiveScene(SceneManager.CreateScene(nombreVacia));
            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                Scene s = SceneManager.GetSceneAt(i);
                if (s.path.Contains("Ciudad"))
                {
                    yield return SceneManager.UnloadSceneAsync(s);
                }
            }
        }

        public override void TearDown()
        {
            MapasDeAccionesDePrueba.AntesDelTearDown(teclado != null ? new InputDevice[] { teclado } : new InputDevice[0]);
            base.TearDown();
            MapasDeAccionesDePrueba.DespuesDelTearDown();
        }

        private IEnumerator Cargar()
        {
            teclado = InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
            MapasDeAccionesDePrueba.Reiniciar(teclado);
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            trepar = Object.FindFirstObjectByType<TreparAPie>();
            Assert.IsNotNull(trepar, "Ciudad_Prologo debe traer al jugador a pie con TreparAPie");
            jugador = trepar.gameObject;
            controlador = jugador.GetComponent<ControladorAPie>();
            cc = jugador.GetComponent<CharacterController>();
            // Lo demás de la escena (director del prólogo, tráfico, peatones) no debe estorbar a la prueba.
            foreach (string nombre in new[] { "DirectorPrologo", "Emboscada", "MostradorPeleas" })
            {
                GameObject g = GameObject.Find(nombre);
                if (g != null)
                {
                    g.SetActive(false);
                }
            }
            int frames = 0;
            while (!controlador.ControlActivo && frames++ < 200)
            {
                yield return null;
            }
            Assert.IsTrue(controlador.ControlActivo, "El jugador debe poder moverse en la escena");
        }

        private void Poner(Vector3 posicion, Vector3 mirar)
        {
            cc.enabled = false;
            jugador.transform.position = posicion;
            Vector3 plano = new Vector3(mirar.x, 0f, mirar.z);
            jugador.transform.rotation = Quaternion.LookRotation(plano);
            cc.enabled = true;
            controlador.ReiniciarMovimiento();
        }

        private IEnumerator Esperar(float segundos)
        {
            float fin = Time.time + segundos;
            while (Time.time < fin)
            {
                yield return null;
            }
        }

        private IEnumerator Calentar()
        {
            int estables = 0;
            int intentos = 0;
            while (estables < 10 && intentos++ < 1000)
            {
                yield return null;
                estables = Time.deltaTime < 0.05f ? estables + 1 : 0;
            }
        }

        // ------------------------------------------------------------------ conteos y comprobaciones por punto

        [UnityTest]
        public IEnumerator Accesos_PorZonaYTotal_CumplenLosMinimos()
        {
            yield return Cargar();
            List<PuntoTrepar> puntos = PuntosDeTechos();
            Assert.GreaterOrEqual(puntos.Count, AccesosTotal, "Accesos en total");
            foreach (string zona in ZonasCasas)
            {
                Assert.GreaterOrEqual(puntos.Count(p => ZonaDe(p) == zona), AccesosPorZona, "Accesos en " + zona);
            }
            Assert.GreaterOrEqual(puntos.Count(p => p.Tipo == TipoTrepar.Tubo), ZonasCasas.Length, "Debe haber tubos de desagüe");
            Assert.GreaterOrEqual(puntos.Count(p => p.Tipo == TipoTrepar.Escalera), AccesosTotal / 2, "Debe haber escaleras de servicio");
            Transform parkour = RaizDeTechos.Find("Parkour");
            Assert.IsNotNull(parkour, "Falta el grupo Parkour");
            int rutas = parkour.Cast<Transform>().Count(t => t.name.StartsWith("Ruta_"));
            Assert.GreaterOrEqual(rutas, RutasMinimas, "Rutas de parkour");
            Assert.GreaterOrEqual(RaizDeTechos.Find("Tablones").childCount, 2, "Tablones");
        }

        [UnityTest]
        public IEnumerator Accesos_CadaPunto_TieneSueloYEspacioArribaYSueloAbajo()
        {
            yield return Cargar();
            yield return null;
            Physics.SyncTransforms();
            List<string> fallas = new List<string>();
            foreach (PuntoTrepar p in PuntosDeTechos())
            {
                Vector3 s = p.Salida;
                if (!Physics.Raycast(s + Vector3.up * TolSuelo, Vector3.down, out RaycastHit hit, TolSuelo * 2f, ~0, QueryTriggerInteraction.Ignore))
                {
                    fallas.Add(p.name + ": sin suelo en la salida");
                }
                else if (Mathf.Abs(hit.point.y - s.y) > TolSuelo)
                {
                    fallas.Add(p.name + ": el suelo de la salida está a " + (hit.point.y - s.y).ToString("F2") + " m");
                }
                if (Physics.CheckCapsule(s + Vector3.up * (RadioJugador + 0.1f), s + Vector3.up * (AlturaJugador - RadioJugador), RadioJugador, ~0, QueryTriggerInteraction.Ignore))
                {
                    fallas.Add(p.name + ": no cabe el jugador en la salida");
                }
                Vector3 pie = p.PieAbajo;
                if (!Physics.Raycast(pie + Vector3.up * 0.5f, Vector3.down, 1f, ~0, QueryTriggerInteraction.Ignore))
                {
                    fallas.Add(p.name + ": sin suelo al pie");
                }
            }
            Assert.IsEmpty(fallas, string.Join("\n", fallas));
        }

        // ------------------------------------------------------------------ subir de verdad

        private IEnumerator SubirEscalera(PuntoTrepar p)
        {
            Poner(p.PieAbajo - p.Frente * 1.5f + Vector3.up * 0.05f, p.Frente);
            yield return Esperar(0.5f);
            Press(teclado.wKey);
            bool trepo = false;
            float fin = Time.time + MaxSegundosSubida;
            while (Time.time < fin)
            {
                trepo |= trepar.Trepando;
                if (trepo && !trepar.Trepando && jugador.transform.position.y > p.Salida.y - TolTecho)
                {
                    break;
                }
                yield return null;
            }
            Release(teclado.wKey);
            yield return Esperar(0.5f);
            Assert.IsTrue(trepo, p.name + ": debe engancharse a la escalera");
            Assert.AreEqual(p.Salida.y, jugador.transform.position.y, TolTecho, p.name + ": debe quedar parado en la azotea");
            Assert.Less(Vector2.Distance(new Vector2(jugador.transform.position.x, jugador.transform.position.z), new Vector2(p.Salida.x, p.Salida.z)), 2f, p.name + ": debe quedar cerca de la salida");
        }

        [UnityTest]
        public IEnumerator Escalera_DeServicioEnCasas_LlevaAlTecho()
        {
            yield return Cargar();
            yield return Calentar();
            PuntoTrepar p = PuntosDeTechos().First(x => x.Tipo == TipoTrepar.Escalera && ZonaDe(x) == "S2c_Suburbios" && x.transform.parent.name == "Accesos");
            yield return SubirEscalera(p);
        }

        [UnityTest]
        public IEnumerator Tubo_DeDesague_LlevaAlTecho()
        {
            yield return Cargar();
            yield return Calentar();
            PuntoTrepar p = PuntosDeTechos().First(x => x.Tipo == TipoTrepar.Tubo && ZonaDe(x) == "S2c_Suburbios");
            yield return SubirEscalera(p);
        }

        private IEnumerator CorrerYSaltar(float segundos)
        {
            Press(teclado.wKey);
            yield return null;
            Press(teclado.leftShiftKey);
            yield return Esperar(segundos);
            Press(teclado.spaceKey);
            yield return null;
            Release(teclado.spaceKey);
        }

        [UnityTest]
        public IEnumerator Parkour_ContenedorBardaEscalera_LlevaAlTecho()
        {
            yield return Cargar();
            yield return Calentar();
            Transform ruta = RaizDeTechos.Find("Parkour").Cast<Transform>().First(t => t.name.StartsWith("Ruta_S2c_Suburbios"));
            PuntoTrepar escalera = ruta.GetComponentInChildren<PuntoTrepar>();
            Transform contenedor = ruta.Find("Contenedor");
            Transform barda = ruta.Find("Barda");
            Assert.IsNotNull(contenedor);
            Assert.IsNotNull(barda);
            Vector3 hacia = escalera.Frente;
            // Salto 1: del suelo al contenedor (1,6 m).
            Vector3 centroCont = contenedor.position;
            Poner(centroCont - hacia * 3f + Vector3.up * 0.05f, hacia);
            yield return Esperar(0.5f);
            Press(teclado.wKey);
            float fin = Time.time + 4f;
            bool salto = false;
            while (Time.time < fin && jugador.transform.position.y < 1.4f)
            {
                float d = Vector3.Dot(centroCont - jugador.transform.position, hacia);
                if (!salto && d < 1.6f)
                {
                    Press(teclado.spaceKey);
                    yield return null;
                    Release(teclado.spaceKey);
                    salto = true;
                }
                yield return null;
            }
            Release(teclado.wKey);
            yield return Esperar(1.5f);
            Assert.Greater(jugador.transform.position.y, 1.4f, "Debe quedar sobre el contenedor");
            Assert.Less(jugador.transform.position.y, 2.0f, "Sobre el contenedor, no más arriba");
            // Salto 2: del contenedor a la barda (3,5 m).
            float yBarda = barda.GetComponentInChildren<Collider>().bounds.max.y;
            Press(teclado.wKey);
            fin = Time.time + 4f;
            salto = false;
            while (Time.time < fin && jugador.transform.position.y < yBarda - 0.3f)
            {
                float d = Vector3.Dot(contenedor.position - jugador.transform.position, hacia);
                if (!salto && d < 0.35f)
                {
                    Press(teclado.spaceKey);
                    yield return null;
                    Release(teclado.spaceKey);
                    salto = true;
                }
                yield return null;
            }
            Release(teclado.wKey);
            yield return Esperar(1.5f);
            Assert.GreaterOrEqual(jugador.transform.position.y, yBarda - TolTecho, "Debe quedar parado sobre la barda");
            // Escalera de la barda hasta el techo.
            Press(teclado.wKey);
            bool trepo = false;
            fin = Time.time + MaxSegundosSubida;
            while (Time.time < fin)
            {
                trepo |= trepar.Trepando;
                if (trepo && !trepar.Trepando && jugador.transform.position.y > escalera.Salida.y - TolTecho)
                {
                    break;
                }
                yield return null;
            }
            Release(teclado.wKey);
            yield return Esperar(0.5f);
            Assert.IsTrue(trepo, "Debe engancharse a la escalera de la barda");
            Assert.AreEqual(escalera.Salida.y, jugador.transform.position.y, TolTecho, "Debe terminar parado en el techo");
        }

        [UnityTest]
        public IEnumerator Pretil_CaminarCincoSegundosHaciaElBorde_NoCae()
        {
            yield return Cargar();
            yield return Calentar();
            Transform pretiles = RaizDeTechos.Find("Pretiles");
            Assert.Greater(pretiles.childCount, 0, "Debe haber pretiles nuevos");
            // Un pretil de Suburbios con techo de pie a 2 m detrás de él.
            Transform pretil = null;
            Vector3 dentro = Vector3.zero;
            foreach (Transform t in pretiles)
            {
                Vector3 n = t.forward;
                Vector3 punto = t.position - n * 2f;
                if (Physics.Raycast(punto + Vector3.up * 1f, Vector3.down, out RaycastHit hit, 2f, ~0, QueryTriggerInteraction.Ignore) && Mathf.Abs(hit.point.y - t.position.y) < 0.1f
                    && !Physics.CheckCapsule(punto + Vector3.up * 0.4f, punto + Vector3.up * 1.5f, RadioJugador, ~0, QueryTriggerInteraction.Ignore))
                {
                    pretil = t;
                    dentro = punto;
                    break;
                }
            }
            Assert.IsNotNull(pretil, "Debe haber un pretil con techo libre detrás");
            Vector3 salida = pretil.forward;
            Poner(dentro + Vector3.up * 0.05f, salida);
            yield return Esperar(0.5f);
            float minimoY = jugador.transform.position.y;
            float topeY = pretil.position.y;
            Press(teclado.wKey);
            float fin = Time.time + 5f;
            while (Time.time < fin)
            {
                minimoY = Mathf.Min(minimoY, jugador.transform.position.y);
                yield return null;
            }
            Release(teclado.wKey);
            Assert.GreaterOrEqual(minimoY, topeY - TolTecho, "No debe caer a la calle");
            float pasoBorde = Vector3.Dot(jugador.transform.position - pretil.position, salida);
            Assert.Less(pasoBorde, 0.3f, "No debe haber cruzado el pretil");
        }
    }
}
