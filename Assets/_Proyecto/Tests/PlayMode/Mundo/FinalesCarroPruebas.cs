using System.Collections;
using MechaCyberpunk.Mundo.Ambiente;
using MechaCyberpunk.Mundo.Pruebas;
using MechaCyberpunk.Vehiculos.Carro;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>
    /// T-077: los 6 finales de calle del barrio son sólidos y visibles, el carro no sale del barrio aunque llegue a toda
    /// velocidad y las patrullas de los retenes tienen torretas que parpadean.
    /// </summary>
    public class FinalesCarroPruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Pruebas_Alan.unity";
        private const string RutaCarro = "Assets/_Proyecto/Vehiculos/Prefabs/CarroViejo.prefab";
        private const string PrefijoFinal = "Final_";
        private const float MitadBarrioX = 134f;
        private const float MitadBarrioZ = 87f;
        private const float DistanciaInicial = 40f;
        private const float SegundosConW = 8f;
        private const float AlturaInicial = 0.1f;
        private const float DistanciaRayos = 20f;
        private const float AlcanceRayos = 25f;
        private const float AlturaMuroMinima = 3.99f;
        private const float AlturaRayoMuro = 3.9f;
        private const float AvanceMinimo = 25f;
        private const float SegundosTorretas = 3f;
        private const int CambiosMinimos = 4;
        private const int PatrullasEsperadas = 3;
        private const int FramesEstables = 10;
        private const int MaxFramesCalentamiento = 1000;
        private const float DeltaTimeEstable = 0.05f;

        private static readonly float[] Laterales = { -6f, -3f, 0f, 3f, 6f };
        private static readonly float[] Alturas = { 0.5f, 1.5f, 2.5f };

        private struct Extremo
        {
            public readonly string Nombre;
            public readonly Vector3 Linea;
            public readonly Vector3 Hacia;

            public Extremo(string nombre, Vector3 linea, Vector3 hacia)
            {
                Nombre = nombre;
                Linea = linea;
                Hacia = hacia;
            }
        }

        // Línea del cierre: 2 m dentro del muro de fondo, en el centro de cada calle.
        private static readonly Extremo[] Extremos =
        {
            new Extremo("Este", new Vector3(MitadBarrioX - 2f, 0f, 0f), Vector3.right),
            new Extremo("Oeste", new Vector3(-MitadBarrioX + 2f, 0f, 0f), Vector3.left),
            new Extremo("NorteOeste", new Vector3(-47f, 0f, MitadBarrioZ - 2f), Vector3.forward),
            new Extremo("NorteEste", new Vector3(47f, 0f, MitadBarrioZ - 2f), Vector3.forward),
            new Extremo("SurOeste", new Vector3(-47f, 0f, -MitadBarrioZ + 2f), Vector3.back),
            new Extremo("SurEste", new Vector3(47f, 0f, -MitadBarrioZ + 2f), Vector3.back)
        };

        private Keyboard teclado;
        private GameObject carroObjeto;

        public override void Setup()
        {
            base.Setup();
            teclado = InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
        }

        public override void TearDown()
        {
            if (carroObjeto != null)
            {
                Object.DestroyImmediate(carroObjeto);
            }
            base.TearDown();
        }

        private static IEnumerator CargarEscena()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
            int estables = 0;
            int intentos = 0;
            while (estables < FramesEstables && intentos < MaxFramesCalentamiento)
            {
                yield return null;
                intentos++;
                estables = Time.deltaTime < DeltaTimeEstable ? estables + 1 : 0;
            }
            Physics.SyncTransforms();
        }

        private static bool EsDeUnFinal(Transform t)
        {
            for (Transform actual = t; actual != null; actual = actual.parent)
            {
                if (actual.name.StartsWith(PrefijoFinal))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool SeVe(Collider collider)
        {
            MeshRenderer visual = collider.GetComponent<MeshRenderer>();
            if (visual == null)
            {
                visual = collider.GetComponentInParent<MeshRenderer>();
            }
            return visual != null && visual.enabled && visual.gameObject.activeInHierarchy;
        }

        private IEnumerator ProbarCarro(int indice)
        {
            Extremo extremo = Extremos[indice];
            yield return CargarEscena();
            foreach (CamaraLibre camara in Object.FindObjectsByType<CamaraLibre>(FindObjectsSortMode.None))
            {
                camara.gameObject.SetActive(false);
            }

            Vector3 inicio = extremo.Linea - extremo.Hacia * DistanciaInicial + Vector3.up * AlturaInicial;
            Quaternion mirada = Quaternion.LookRotation(extremo.Hacia, Vector3.up);
            carroObjeto = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaCarro), inicio, mirada);
            CarroViejo carro = carroObjeto.GetComponent<CarroViejo>();
            Physics.SyncTransforms();
            yield return null;
            yield return null;
            carro.TomarControl(true);
            yield return new WaitForSeconds(0.3f);

            Press(teclado.wKey);
            float fin = Time.time + SegundosConW;
            float maximoAvance = float.MinValue;
            while (Time.time < fin)
            {
                yield return null;
                Vector3 p = carroObjeto.transform.position;
                Assert.IsFalse(float.IsNaN(p.x) || float.IsNaN(p.y) || float.IsNaN(p.z), extremo.Nombre + ": la posición del carro es NaN");
                Assert.LessOrEqual(Mathf.Abs(p.x), MitadBarrioX, extremo.Nombre + ": el carro salió del barrio en x (" + p + ")");
                Assert.LessOrEqual(Mathf.Abs(p.z), MitadBarrioZ, extremo.Nombre + ": el carro salió del barrio en z (" + p + ")");
                float avance = Vector3.Dot(p - extremo.Linea, extremo.Hacia);
                maximoAvance = Mathf.Max(maximoAvance, avance);
                Assert.Less(avance, 0f, extremo.Nombre + ": el carro cruzó la línea del cierre (" + p + ")");
            }
            Release(teclado.wKey);
            Assert.Greater(maximoAvance, -DistanciaInicial + AvanceMinimo,
                extremo.Nombre + ": el carro casi no avanzó hacia el cierre (avance máximo " + maximoAvance + " m desde la línea)");
        }

        [UnityTest]
        public IEnumerator Carro_HaciaElCierreEste_NoSaleDelBarrio()
        {
            yield return ProbarCarro(0);
        }

        [UnityTest]
        public IEnumerator Carro_HaciaElCierreOeste_NoSaleDelBarrio()
        {
            yield return ProbarCarro(1);
        }

        [UnityTest]
        public IEnumerator Carro_HaciaElCierreNorteOeste_NoSaleDelBarrio()
        {
            yield return ProbarCarro(2);
        }

        [UnityTest]
        public IEnumerator Carro_HaciaElCierreNorteEste_NoSaleDelBarrio()
        {
            yield return ProbarCarro(3);
        }

        [UnityTest]
        public IEnumerator Carro_HaciaElCierreSurOeste_NoSaleDelBarrio()
        {
            yield return ProbarCarro(4);
        }

        [UnityTest]
        public IEnumerator Carro_HaciaElCierreSurEste_NoSaleDelBarrio()
        {
            yield return ProbarCarro(5);
        }

        [UnityTest]
        public IEnumerator Rayos_AlAnchoDeCadaCalle_ChocanConUnCierreQueSeVe()
        {
            yield return CargarEscena();
            foreach (Extremo extremo in Extremos)
            {
                Vector3 lateral = Vector3.Cross(Vector3.up, extremo.Hacia);
                foreach (float desplazamiento in Laterales)
                {
                    foreach (float altura in Alturas)
                    {
                        Vector3 origen = extremo.Linea - extremo.Hacia * DistanciaRayos + lateral * desplazamiento + Vector3.up * altura;
                        string rayo = extremo.Nombre + " lateral " + desplazamiento + " altura " + altura;
                        // Los carros estacionados de la calle pueden estar antes: se cuenta el primer golpe que es del final.
                        RaycastHit[] golpes = Physics.RaycastAll(origen, extremo.Hacia, AlcanceRayos, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                        System.Array.Sort(golpes, (a, b) => a.distance.CompareTo(b.distance));
                        Collider delFinal = null;
                        foreach (RaycastHit golpe in golpes)
                        {
                            if (EsDeUnFinal(golpe.collider.transform))
                            {
                                delFinal = golpe.collider;
                                break;
                            }
                        }
                        Assert.IsNotNull(delFinal, rayo + ": el rayo no chocó con ningún cierre");
                        Assert.IsTrue(SeVe(delFinal), rayo + ": '" + delFinal.name + "' no tiene MeshRenderer prendido");
                    }
                }
            }
        }

        [UnityTest]
        public IEnumerator Muro_DetrasDeCadaCierre_TieneAlMenos4mYSeVe()
        {
            yield return CargarEscena();
            foreach (Extremo extremo in Extremos)
            {
                Vector3 lateral = Vector3.Cross(Vector3.up, extremo.Hacia);
                foreach (float desplazamiento in Laterales)
                {
                    Vector3 origen = extremo.Linea - extremo.Hacia * 0.5f + lateral * desplazamiento + Vector3.up * AlturaRayoMuro;
                    string rayo = extremo.Nombre + " lateral " + desplazamiento;
                    Assert.IsTrue(Physics.Raycast(origen, extremo.Hacia, out RaycastHit golpe, AlcanceRayos, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore),
                        rayo + ": no hay muro a 3.9 m de alto detrás del cierre");
                    Assert.IsTrue(SeVe(golpe.collider), rayo + ": '" + golpe.collider.name + "' no se ve");
                    Assert.GreaterOrEqual(golpe.collider.bounds.max.y, AlturaMuroMinima, rayo + ": '" + golpe.collider.name + "' mide menos de 4 m");
                    Assert.Less(golpe.distance, 3f, rayo + ": el muro está demasiado lejos del cierre");
                }
            }
        }

        [UnityTest]
        public IEnumerator Torretas_TresRetenes_CambianDeColorAlMenos4VecesEn3s()
        {
            yield return CargarEscena();
            LuzTorreta[] torretas = Object.FindObjectsByType<LuzTorreta>(FindObjectsSortMode.None);
            Assert.AreEqual(PatrullasEsperadas, torretas.Length, "Debe haber una patrulla con torreta en cada retén");

            int[] cambios = new int[torretas.Length];
            ColorTorreta[] anterior = new ColorTorreta[torretas.Length];
            bool huboDistintas = false;
            for (int i = 0; i < torretas.Length; i++)
            {
                anterior[i] = torretas[i].ColorActual;
            }
            float fin = Time.time + SegundosTorretas;
            while (Time.time < fin)
            {
                yield return null;
                for (int i = 0; i < torretas.Length; i++)
                {
                    ColorTorreta color = torretas[i].ColorActual;
                    if (color != anterior[i])
                    {
                        cambios[i]++;
                        anterior[i] = color;
                    }
                    Light[] luces = torretas[i].GetComponentsInChildren<Light>(true);
                    int prendidas = 0;
                    foreach (Light luz in luces)
                    {
                        prendidas += luz.enabled ? 1 : 0;
                    }
                    Assert.AreEqual(1, prendidas, torretas[i].name + ": debe haber una sola luz de la torreta prendida");
                }
                huboDistintas |= torretas[0].ColorActual != torretas[1].ColorActual || torretas[0].ColorActual != torretas[2].ColorActual;
            }
            for (int i = 0; i < torretas.Length; i++)
            {
                Assert.GreaterOrEqual(cambios[i], CambiosMinimos, torretas[i].transform.parent.name + ": la torreta casi no cambió de color");
            }
            Assert.IsTrue(huboDistintas, "Las torretas no deben parpadear todas igual");
            LogAssert.NoUnexpectedReceived();
        }
    }
}
