using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MechaCyberpunk.Mundo.Reloj;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-080: la calle vestida con el kit urbano (postes, cables, transformadores, puestos, basureros y azoteas).</summary>
    public class KitCallePruebas
    {
        private const string RutaBarrio = "Assets/_Proyecto/Mundo/Prefabs/Barrio.prefab";
        private const string RutaCiudad = "Assets/_Proyecto/Escenas/Ciudad.unity";
        private const float HoraDia = 12f;
        private const float HoraNoche = 22f;
        private const float MitadManzana = 40f;
        private const float PasoManzana = 94f;
        private const float MitadBarrioX = 134f;
        private const float MitadBarrioZ = 87f;
        private const float AnchoCalle = 14f;
        private const float DistanciaFranja = 38f;
        private const float RadioPersona = 0.35f;
        private const float AlturaCentro = 1f;
        private const float MitadAlturaCapsula = 0.25f;
        private const float MitadRecorrido = 36f;
        private const int MinimoPostes = 30;
        private const float DistanciaMinimaEntrada = 6f;
        private const float AlturaMinimaCruce = 6f;
        private const float MargenBanqueta = 0.05f;
        private const float MaximaDistanciaPosteCable = 1f;
        private const int MinimoPuestos = 3;
        private const int MinimoAzoteas = 10;
        private static readonly string[] PrefijosKit = { "Poste_Luz", "Transformador_Poste", "Puesto_Tacos", "Basurero_" };

        private GameObject barrio;

        private static readonly Vector2[] Centros =
        {
            new Vector2(-PasoManzana, PasoManzana / 2f), new Vector2(0f, PasoManzana / 2f), new Vector2(PasoManzana, PasoManzana / 2f),
            new Vector2(-PasoManzana, -PasoManzana / 2f), new Vector2(0f, -PasoManzana / 2f), new Vector2(PasoManzana, -PasoManzana / 2f)
        };

        [SetUp]
        public void CrearBarrio()
        {
            barrio = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaBarrio));
            Physics.SyncTransforms();
        }

        [TearDown]
        public void Limpiar()
        {
            Object.DestroyImmediate(barrio);
        }

        private IEnumerable<Transform> Todos() => barrio.GetComponentsInChildren<Transform>(true);

        private List<Transform> PorPrefijo(string prefijo) => Todos().Where(t => t.name.StartsWith(prefijo)).ToList();

        private static bool EnBanqueta(Vector3 punto)
        {
            return Centros.Any(c => Mathf.Abs(punto.x - c.x) <= MitadManzana + MargenBanqueta && Mathf.Abs(punto.z - c.y) <= MitadManzana + MargenBanqueta);
        }

        private List<Collider> CollidersDelKit()
        {
            List<Collider> resultado = new List<Collider>();
            foreach (Transform t in Todos())
            {
                if (PrefijosKit.Any(p => t.name.StartsWith(p)) && !t.name.StartsWith("Farola_"))
                {
                    Collider[] propios = t.GetComponents<Collider>();
                    resultado.AddRange(propios);
                }
            }
            return resultado;
        }

        private static bool EsLadoConCalle(Vector2 centro, Vector2 normal)
        {
            Vector2 destino = centro + normal * (MitadManzana + AnchoCalle / 2f);
            return Mathf.Abs(destino.x) <= MitadBarrioX - 1f && Mathf.Abs(destino.y) <= MitadBarrioZ - 1f;
        }

        [Test]
        public void Postes_HayAlMenos30_YNingunaFarolaEsGreybox()
        {
            List<Transform> postes = Todos().Where(t => t.name == "Poste_Luz").ToList();
            Assert.GreaterOrEqual(postes.Count, MinimoPostes, "Faltan Poste_Luz del kit");
            List<Transform> farolas = PorPrefijo("Farola_");
            Assert.AreEqual(postes.Count, farolas.Count, "Cada Farola_N lleva un Poste_Luz");
            foreach (Transform farola in farolas)
            {
                Assert.IsNotNull(farola.Find("Poste_Luz"), farola.name + " no tiene su Poste_Luz");
                Assert.IsNull(farola.Find("Poste"), farola.name + " todavía tiene el poste greybox");
                Assert.IsNull(farola.Find("Brazo"), farola.name + " todavía tiene el brazo greybox");
                Assert.IsNull(farola.Find("Lampara"), farola.name + " todavía tiene la lámpara greybox");
                Light luz = farola.GetComponentInChildren<Light>(true);
                Assert.IsNotNull(luz, farola.name + " no tiene Light");
                Assert.IsTrue(luz.transform.position.y > 6f, farola.name + ": la luz debe ir en la luminaria");
            }
        }

        [UnityTest]
        public IEnumerator Farolas_EnCiudad_TienenLuzYLuzNocturna_ApagadaA12YPrendidaA22()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaCiudad, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
            RelojMundo reloj = Object.FindFirstObjectByType<RelojMundo>();
            Assert.IsNotNull(reloj, "Ciudad debe traer un RelojMundo");
            reloj.Pausar(true);
            List<Transform> farolas = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t => t.name.StartsWith("Farola_")).ToList();
            Assert.GreaterOrEqual(farolas.Count, MinimoPostes);
            foreach (Transform farola in farolas)
            {
                Assert.IsNotNull(farola.GetComponentInChildren<Light>(true), farola.name + " sin Light");
                Assert.IsNotNull(farola.GetComponent<LuzNocturna>(), farola.name + " sin LuzNocturna");
            }

            reloj.AvanzarHasta(HoraDia);
            yield return null;
            foreach (Transform farola in farolas)
            {
                Assert.IsFalse(farola.GetComponent<LuzNocturna>().EstaPrendida, farola.name + " debe estar apagada a las 12:00");
                Assert.IsFalse(farola.GetComponentInChildren<Light>(true).enabled, farola.name + ": su Light debe estar apagada a las 12:00");
            }
            reloj.AvanzarHasta(HoraNoche);
            yield return null;
            foreach (Transform farola in farolas)
            {
                Assert.IsTrue(farola.GetComponent<LuzNocturna>().EstaPrendida, farola.name + " debe estar prendida a las 22:00");
                Assert.IsTrue(farola.GetComponentInChildren<Light>(true).enabled, farola.name + ": su Light debe estar prendida a las 22:00");
            }
            foreach (GameObject raiz in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                Object.DestroyImmediate(raiz);
            }
        }

        [Test]
        public void Colliders_NingunoDelKitSobreElAsfalto_NiACercaDeUnaEntrada()
        {
            List<Collider> colliders = CollidersDelKit();
            Assert.Greater(colliders.Count, MinimoPostes, "No encontré los colliders del kit");
            List<Vector3> entradas = Todos().Where(t => t.name == "Entrada" && t.parent.name != "Lugar_Clinica").Select(t => t.position).ToList();
            Assert.IsNotEmpty(entradas);
            foreach (Collider collider in colliders)
            {
                Bounds b = collider.bounds;
                foreach (float x in new[] { b.min.x, b.max.x })
                {
                    foreach (float z in new[] { b.min.z, b.max.z })
                    {
                        Assert.IsTrue(EnBanqueta(new Vector3(x, 0f, z)), collider.name + " tiene una esquina sobre el asfalto: (" + x + ", " + z + ")");
                    }
                }
                foreach (Vector3 entrada in entradas)
                {
                    Vector3 plano = new Vector3(entrada.x, b.center.y, entrada.z);
                    Assert.GreaterOrEqual(Mathf.Sqrt(b.SqrDistance(plano)), DistanciaMinimaEntrada, collider.name + " está a menos de 6 m de una Entrada");
                }
            }
        }

        [Test]
        public void FranjaDeCaminar_EnCadaLadoConCalle_NoChocaConNada()
        {
            int lados = 0;
            foreach (Vector2 centro in Centros)
            {
                foreach (Vector2 normal in new[] { Vector2.up, Vector2.down, Vector2.right, Vector2.left })
                {
                    if (!EsLadoConCalle(centro, normal))
                    {
                        continue;
                    }
                    lados++;
                    Vector2 tangente = new Vector2(-normal.y, normal.x);
                    Vector2 inicio = centro + normal * DistanciaFranja - tangente * MitadRecorrido;
                    Vector3 desde = new Vector3(inicio.x, AlturaCentro, inicio.y);
                    Vector3 direccion = new Vector3(tangente.x, 0f, tangente.y);
                    RaycastHit[] golpes = Physics.CapsuleCastAll(desde + Vector3.down * MitadAlturaCapsula, desde + Vector3.up * MitadAlturaCapsula, RadioPersona, direccion, 2f * MitadRecorrido);
                    foreach (RaycastHit golpe in golpes)
                    {
                        if (golpe.collider.GetComponentsInParent<Transform>().Any(t => t.name == "Finales"))
                        {
                            continue; // el pórtico y las vallas de los finales no son del kit
                        }
                        Assert.AreEqual("PanelPuerta", golpe.collider.name, "La franja de caminar del lado " + normal + " de la manzana " + centro + " choca con '" + golpe.collider.name + "'");
                    }
                }
            }
            Assert.GreaterOrEqual(lados, 8, "Debe haber varios lados con calle");
        }

        [Test]
        public void Puestos_HayTresOMas_Transformadores_UnoPorCadaTresPostes_Y_NoQuedaGreyboxViejo()
        {
            Assert.GreaterOrEqual(PorPrefijo("Puesto_Tacos").Count, MinimoPuestos);
            int postes = Todos().Count(t => t.name == "Poste_Luz");
            int transformadores = PorPrefijo("Transformador_Poste").Count;
            Assert.AreEqual(postes / 3f, transformadores, 1f, "Un transformador por cada 3 postes (±1)");
            Assert.IsEmpty(Todos().Where(t => t.name == "Tanque").ToList(), "El tanque greybox debe ser Azotea_AC_Tinaco");
            Assert.IsEmpty(PorPrefijo("Bote_"), "Los botes greybox pasan a Basurero_Grafiti");
            Assert.GreaterOrEqual(PorPrefijo("Basurero_").Count, 1);
            Assert.GreaterOrEqual(Todos().Count(t => t.name == "Azotea_AC_Tinaco"), MinimoAzoteas);
            Assert.GreaterOrEqual(PorPrefijo("TinacoPobre").Count, 1, "Las azoteas pobres ganan tinacos");
        }

        [Test]
        public void PuestosDelBarYElClandestino_EstanEnSusBanquetas()
        {
            Transform bar = Todos().First(t => t.name == "Entrada" && t.parent.name == "Lugar_Bar");
            Transform clandestino = Todos().First(t => t.name == "Entrada" && t.parent.name == "Lugar_Clandestino");
            Assert.AreEqual(2, PorPrefijo("Puesto_Tacos_Bar").Count);
            foreach (Transform puesto in PorPrefijo("Puesto_Tacos_Bar"))
            {
                Assert.Less(Mathf.Abs(puesto.position.z - bar.position.z), 4f, "El puesto del bar debe estar en su banqueta");
                Assert.Greater(Vector2.Distance(new Vector2(puesto.position.x, puesto.position.z), new Vector2(bar.position.x, bar.position.z)), DistanciaMinimaEntrada);
            }
            Transform delClandestino = PorPrefijo("Puesto_Tacos_Clandestino").Single();
            Assert.Less(Mathf.Abs(delClandestino.position.z - clandestino.position.z), 4f);
            Assert.Greater(Vector2.Distance(new Vector2(delClandestino.position.x, delClandestino.position.z), new Vector2(clandestino.position.x, clandestino.position.z)), DistanciaMinimaEntrada);
            Assert.AreEqual(4, Todos().Count(t => t.parent != null && t.parent.name == "PuestosCallejon" && t.name.StartsWith("Puesto_Tacos_")), "El callejón del mercado negro lleva 4 puestos del kit");
        }

        [Test]
        public void Cables_VanEntrePostesVecinos_YNingunoCruzaLaCalleABajoDe6m()
        {
            LineRenderer[] cables = barrio.GetComponentsInChildren<LineRenderer>(true);
            Assert.GreaterOrEqual(cables.Length, MinimoPostes, "Debe haber cables entre los postes");
            List<Vector3> postes = Todos().Where(t => t.name == "Poste_Luz").Select(t => t.position).ToList();
            int cruces = 0;
            foreach (LineRenderer cable in cables)
            {
                Vector3[] puntos = new Vector3[cable.positionCount];
                cable.GetPositions(puntos);
                Vector3 primero = cable.transform.TransformPoint(puntos[0]);
                Vector3 ultimo = cable.transform.TransformPoint(puntos[puntos.Length - 1]);
                Assert.IsTrue(CercaDeUnPoste(postes, primero), cable.name + ": su inicio no está en un poste");
                Assert.IsTrue(CercaDeUnPoste(postes, ultimo), cable.name + ": su final no está en un poste");
                Assert.AreNotEqual(Plano(primero), Plano(ultimo), cable.name + " empieza y termina en el mismo poste");
                Assert.IsNull(cable.GetComponent<Collider>(), cable.name + " no debe tener collider");
                bool cruza = false;
                foreach (Vector3 local in puntos)
                {
                    Vector3 mundo = cable.transform.TransformPoint(local);
                    if (!EnBanqueta(mundo))
                    {
                        cruza = true;
                        Assert.GreaterOrEqual(mundo.y, AlturaMinimaCruce, cable.name + " cruza la calle a menos de 6 m de alto");
                    }
                }
                if (cruza)
                {
                    cruces++;
                }
            }
            Assert.Greater(cruces, 0, "Debe haber cables que crucen la calle");
        }

        private static Vector2 Plano(Vector3 punto) => new Vector2(Mathf.Round(punto.x * 10f), Mathf.Round(punto.z * 10f));

        private static bool CercaDeUnPoste(List<Vector3> postes, Vector3 punto)
        {
            return postes.Any(p => Vector2.Distance(new Vector2(p.x, p.z), new Vector2(punto.x, punto.z)) <= MaximaDistanciaPosteCable);
        }
    }
}
