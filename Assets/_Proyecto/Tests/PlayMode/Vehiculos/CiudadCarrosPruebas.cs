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
        private const string RutaCiudad = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string PatronAdorno = @"^Carro_\d+$";
        private const string PrefijoRobable = "CarroRobable_";
        private const int MinimoDeEstacionados = 17;
        private const float DistanciaAlSubir = 1.5f;
        private const float MetrosParaSalir = 4f;
        private const float SegundosDeManejo = 2f;
        private const float LimiteCarga = 30f;

        private Keyboard teclado;

        public override void Setup()
        {
            base.Setup();
            teclado = InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
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
            GameObject origen = PrefabUtility.GetCorrespondingObjectFromOriginalSource(carro);
            return origen != null ? origen.name : carro.name;
        }

        private static IEnumerator Esperar(float segundos)
        {
            float fin = Time.time + segundos;
            while (Time.time < fin)
            {
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
            foreach (CarroEstacionado estacionado in estacionados)
            {
                CarroViejo carro = estacionado.GetComponent<CarroViejo>();
                // Junto a la puerta del conductor, mirándola.
                Vector3 punto = carro.PuntoSubida;
                Vector3 posicion = punto - carro.transform.right * DistanciaAlSubir;
                posicion.y = punto.y + 0.1f;
                Vector3 mirada = punto - posicion;
                mirada.y = 0f;
                jugador.Colocar(new Pose(posicion, Quaternion.LookRotation(mirada.normalized, Vector3.up)));
                yield return Esperar(0.4f);
                float antes = calor.Valor;

                yield return Tecla(teclado.eKey);
                if (jugador.Modo != ModoJugador.Carro)
                {
                    atorados.Add(estacionado.name + " no dejó subir");
                    continue;
                }
                Assert.GreaterOrEqual(calor.Valor, antes + 25f, estacionado.name + ": robarlo sube el calor");
                yield return Esperar(0.4f);
                Vector3 inicio = carro.transform.position;
                Press(teclado.wKey);
                yield return Esperar(SegundosDeManejo);
                Release(teclado.wKey);
                float recorrido = Vector3.Distance(inicio, carro.transform.position);
                if (recorrido < MetrosParaSalir)
                {
                    atorados.Add(estacionado.name + " (" + ModeloDe(estacionado.gameObject) + ") solo avanzó " + recorrido.ToString("F1") + " m en " + estacionado.transform.position.ToString("F0"));
                }
                // Baja para seguir con el siguiente.
                carro.PermitirBajarYa();
                yield return Tecla(teclado.eKey);
                if (jugador.Modo != ModoJugador.APie)
                {
                    atorados.Add(estacionado.name + " no dejó bajar");
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
