using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Trafico.Datos;
using MechaCyberpunk.Trafico.Persecucion;
using MechaCyberpunk.Trafico.Testigos;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.Trafico
{
    /// <summary>
    /// T-662: cámaras de seguridad en postes, en las esquinas de los cruces del barrio. El prefab existe con su poste y su cámara; el gestor las pone
    /// en las esquinas; una cámara que ve al jugador hace testigo del delito y, lejos de toda cámara y sin otro testigo, no hay calor.
    /// </summary>
    public class CamarasT662Pruebas
    {
        private const string RutaConfig = "Assets/_Proyecto/Trafico/Datos/ConfigPersecucion_Trafico.asset";
        private const string RutaDron = "Assets/_Proyecto/Trafico/Prefabs/Dron.prefab";
        private const string RutaPatrulla = "Assets/_Proyecto/Trafico/Prefabs/Patrulla.prefab";
        private const float Paso = 94f;
        private static readonly Vector2[] Centros =
        {
            new Vector2(-Paso, Paso / 2f), new Vector2(0f, Paso / 2f), new Vector2(Paso, Paso / 2f),
            new Vector2(-Paso, -Paso / 2f), new Vector2(0f, -Paso / 2f), new Vector2(Paso, -Paso / 2f)
        };

        private GameObject piso;
        private GameObject barrio;
        private GameObject jugador;
        private ConfigPersecucion config;
        private GestorPersecucion gestor;
        private TestigosT661Pruebas.CalorReal calor;

        [SetUp]
        public void Preparar()
        {
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
            config = Object.Instantiate(AssetDatabase.LoadAssetAtPath<ConfigPersecucion>(RutaConfig));
            piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(900f, 1f, 900f);
            barrio = new GameObject("BarrioPrueba");
            for (int i = 0; i < Centros.Length; i++)
            {
                GameObject m = new GameObject("Manzana_" + (i + 1));
                m.transform.SetParent(barrio.transform, false);
                m.transform.position = new Vector3(Centros[i].x, 0f, Centros[i].y);
            }
            jugador = new GameObject("JugadorDePrueba");
            Registro<IJugador>.Agregar(new TestigosT661Pruebas.JugadorDoble { Raiz = jugador.transform });
            calor = new TestigosT661Pruebas.CalorReal();
            Registro<ICalor>.Agregar(calor);
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
            Object.DestroyImmediate(barrio);
            Object.DestroyImmediate(jugador);
            Object.DestroyImmediate(piso);
            Object.DestroyImmediate(config);
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
        }

        [Test]
        public void Prefab_TienePosteCamaraYFoquito()
        {
            GameObject prefab = Resources.Load<GameObject>("CamaraSeguridad");
            Assert.IsNotNull(prefab, "El prefab está en Resources");
            Assert.IsNotNull(prefab.GetComponent<CamaraSeguridad>());
            Assert.IsNotNull(prefab.transform.Find("Poste"), "Tiene poste");
            Assert.IsNotNull(prefab.transform.Find("Camara/Carcasa"), "Tiene la carcasa de la cámara");
            Assert.IsNotNull(prefab.transform.Find("Camara/Foquito"), "Tiene el foquito");
            Assert.Greater(prefab.GetComponentsInChildren<MeshRenderer>().Length, 5);
        }

        [UnityTest]
        public IEnumerator Gestor_PoneCamarasEnLasEsquinasDeLosCruces()
        {
            yield return null;
            Assert.AreEqual(4, gestor.Camaras, "2 cruces de cuatro calles x 2 cámaras por cruce");
            Assert.AreEqual(4, CamaraSeguridad.Activas.Count);
            GrafoCalles calles = GrafoCalles.DeManzanas(Centros, 80f, 0.2f);
            foreach (CamaraSeguridad c in CamaraSeguridad.Activas)
            {
                float mejor = float.MaxValue;
                for (int i = 0; i < calles.Cantidad; i++)
                {
                    if (calles.Vecinos(i).Count >= 3)
                    {
                        Vector3 poste = new Vector3(c.transform.position.x, 0.2f, c.transform.position.z);
                        mejor = Mathf.Min(mejor, Vector3.Distance(poste, calles.Nodo(i)));
                    }
                }
                Assert.AreEqual(config.DesfaseEsquinaCamara * Mathf.Sqrt(2f), mejor, 0.1f, "El poste está en la esquina de un cruce");
            }
        }

        [UnityTest]
        public IEnumerator CamaraDelPoste_VeAlJugadorEnElCruce_YNoAlQueEstaLejos()
        {
            yield return null;
            GrafoCalles calles = GrafoCalles.DeManzanas(Centros, 80f, 0.2f);
            int cruce = 0;
            for (int i = 0; i < calles.Cantidad; i++)
            {
                if (calles.Vecinos(i).Count >= 3)
                {
                    cruce = i;
                    break;
                }
            }
            jugador.transform.position = calles.Nodo(cruce) + new Vector3(3f, -0.2f, 2f);
            Physics.SyncTransforms();
            yield return null;
            bool alguna = false;
            foreach (CamaraSeguridad c in CamaraSeguridad.Activas)
            {
                alguna |= c.Ve(jugador.transform.position);
            }
            Assert.IsTrue(alguna, "Alguna cámara del cruce ve al jugador");
            Registro<ICalor>.Actual.Sumar(20f, MotivoCalor.Pelea);
            Assert.AreEqual(20f, calor.Valor, 0.001f, "Una cámara es testigo: sube el calor");

            Registro<ICalor>.Actual.Borrar();
            jugador.transform.position = new Vector3(420f, 0f, 420f);
            Physics.SyncTransforms();
            yield return null;
            Registro<ICalor>.Actual.Sumar(20f, MotivoCalor.Pelea);
            Assert.AreEqual(0f, calor.Valor, 0.001f, "Lejos de toda cámara y sin testigos: no hay calor");
        }

        // Genera docs/capturas/T-662-camara-poste.png: una cámara de seguridad en el poste de una esquina.
        [UnityTest]
        public IEnumerator Captura_CamaraEnSuPoste()
        {
            yield return null;
            CamaraSeguridad camara = CamaraSeguridad.Activas[0];
            GameObject calle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            calle.transform.position = new Vector3(camara.transform.position.x, -0.45f, camara.transform.position.z);
            calle.transform.localScale = new Vector3(30f, 1f, 30f);
            Renderer rc = calle.GetComponent<Renderer>();
            rc.material.color = new Color(0.25f, 0.27f, 0.3f);
            GameObject camObjeto = new GameObject("CamaraCaptura");
            Camera cam = camObjeto.AddComponent<Camera>();
            cam.fieldOfView = 50f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.1f, 0.18f);
            Vector3 alPoste = camara.PosicionOjo;
            Vector3 lado = Vector3.Cross(Vector3.up, camara.transform.forward).normalized;
            cam.fieldOfView = 40f;
            camObjeto.transform.position = alPoste + camara.transform.forward * 4.5f + lado * 2f - Vector3.up * 0.8f;
            camObjeto.transform.LookAt(alPoste - Vector3.up * 0.2f);
            camara.PonerViendo(true);
            yield return null;
            CapturaPrueba.Guardar(cam, "T-662-camara-poste.png");
            Object.DestroyImmediate(camObjeto);
            Object.DestroyImmediate(calle);
            Assert.IsTrue(System.IO.File.Exists(System.IO.Path.Combine(Application.dataPath, "..", "docs", "capturas", "T-662-camara-poste.png")));
        }
    }
}
