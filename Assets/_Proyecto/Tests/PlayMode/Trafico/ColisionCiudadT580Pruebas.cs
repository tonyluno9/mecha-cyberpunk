using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MechaCyberpunk.Core;
using MechaCyberpunk.Vehiculos.Carro;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.Trafico
{
    /// <summary>
    /// T-580 en la ciudad real: cada carro (estacionado o de tráfico) tiene un collider sólido y el jugador a pie, caminando hacia un carro estacionado,
    /// se detiene en él en vez de atravesarlo.
    /// </summary>
    public class ColisionCiudadT580Pruebas : InputTestFixture
    {
        private const string RutaCiudad = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const float LimiteCarga = 40f;
        private const int CarrosAProbar = 8;
        private const float DistanciaInicial = 4f;

        private Keyboard teclado;

        public override void Setup()
        {
            base.Setup();
            Registros.LimpiarTodos();
            teclado = InputSystem.AddDevice<Keyboard>();
            Mouse raton = InputSystem.AddDevice<Mouse>();
            MapasDePruebaTrafico.Reiniciar(teclado, raton);
        }

        public override void TearDown()
        {
            Registros.LimpiarTodos();
            MapasDePruebaTrafico.AntesDelTearDown();
            base.TearDown();
            MapasDePruebaTrafico.DespuesDelTearDown();
        }

        // La ciudad no se queda cargada: las pruebas siguientes (otras clases) arman su propio escenario.
        [UnityTearDown]
        public IEnumerator DescargarCiudad()
        {
            Scene vacia = SceneManager.CreateScene("VaciaT580");
            Scene ciudad = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(vacia);
            if (ciudad.name != vacia.name && ciudad.isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(ciudad);
            }
            Registros.LimpiarTodos();
        }

        private static IEnumerator CargarCiudad()
        {
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

        private static Collider[] Solidos(CarroViejo carro)
        {
            return carro.GetComponentsInChildren<Collider>().Where(c => c.enabled && !c.isTrigger && c.gameObject.activeInHierarchy).ToArray();
        }

        private static IEnumerator Esperar(float segundos)
        {
            float fin = Time.time + segundos;
            while (Time.time < fin)
            {
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator Ciudad_CadaCarroTieneColliderSolido()
        {
            yield return CargarCiudad();
            CarroViejo[] carros = Object.FindObjectsByType<CarroViejo>(FindObjectsSortMode.None);
            Assert.Greater(carros.Length, 0, "La ciudad trae carros");
            List<string> sin = new List<string>();
            foreach (CarroViejo carro in carros)
            {
                Collider[] solidos = Solidos(carro);
                if (solidos.Length == 0 || solidos.All(c => c.bounds.size.magnitude < 1.5f))
                {
                    sin.Add(carro.name);
                }
            }
            Assert.IsEmpty(sin, "Carros sin collider sólido del tamaño de un carro: " + string.Join(", ", sin));
        }

        [UnityTest]
        public IEnumerator Ciudad_JugadorAPie_NoAtraviesaUnCarroEstacionado()
        {
            yield return CargarCiudad();
            IJugador jugador = Registro<IJugador>.Actual;
            CarroViejo[] estacionados = Object.FindObjectsByType<CarroViejo>(FindObjectsSortMode.None)
                .Where(c => c.GetComponent<MechaCyberpunk.Trafico.Calle.CarroTrafico>() == null && c.gameObject.activeInHierarchy && Solidos(c).Length > 0)
                .OrderBy(c => c.name).Take(CarrosAProbar).ToArray();
            Assert.Greater(estacionados.Length, 0, "La ciudad trae carros estacionados");
            List<string> atravesados = new List<string>();
            foreach (CarroViejo carro in estacionados)
            {
                Collider[] solidos = Solidos(carro);
                Bounds limites = solidos[0].bounds;
                foreach (Collider c in solidos)
                {
                    limites.Encapsulate(c.bounds);
                }
                Vector3 centro = limites.center;
                // Se elige el lado (de los cuatro) con el camino despejado hasta el carro.
                Vector3[] lados = { Vector3.forward, Vector3.back, Vector3.left, Vector3.right };
                bool puesto = false;
                foreach (Vector3 lado in lados)
                {
                    Vector3 inicio = new Vector3(centro.x, limites.min.y + 0.1f, centro.z) + lado * (Mathf.Max(limites.extents.x, limites.extents.z) + DistanciaInicial);
                    Vector3 hacia = new Vector3(centro.x, inicio.y, centro.z) - inicio;
                    bool libre = true;
                    foreach (RaycastHit h in Physics.RaycastAll(inicio + Vector3.up, hacia.normalized, hacia.magnitude, ~0, QueryTriggerInteraction.Ignore))
                    {
                        if (!h.collider.transform.IsChildOf(carro.transform))
                        {
                            libre = false;
                        }
                    }
                    if (libre && Physics.Raycast(inicio + Vector3.up * 0.5f, Vector3.down, 3f, ~0, QueryTriggerInteraction.Ignore))
                    {
                        jugador.Colocar(new Pose(inicio, Quaternion.LookRotation(hacia.normalized, Vector3.up)));
                        puesto = true;
                        break;
                    }
                }
                if (!puesto)
                {
                    continue;
                }
                yield return Esperar(0.4f);
                Physics.SyncTransforms();
                Press(teclado.wKey);
                yield return Esperar(3f);
                Release(teclado.wKey);
                yield return null;
                Vector3 p = jugador.Raiz.position + Vector3.up * 0.5f;
                if (limites.Contains(p))
                {
                    atravesados.Add(carro.name + " (jugador " + jugador.Raiz.position.ToString("F2") + " dentro de " + limites + ")");
                }
            }
            Assert.IsEmpty(atravesados, "El jugador atravesó: " + string.Join("; ", atravesados));
        }
    }
}
