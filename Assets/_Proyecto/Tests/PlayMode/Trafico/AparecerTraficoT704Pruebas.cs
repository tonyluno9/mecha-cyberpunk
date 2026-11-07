using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using MechaCyberpunk.Core;
using MechaCyberpunk.Trafico.Calle;
using MechaCyberpunk.Trafico.Datos;
using MechaCyberpunk.Trafico.Persecucion;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.Trafico
{
    /// <summary>
    /// T-704: con la cámara fija, ningún carro del tráfico nace ni se borra (ni se teletransporta) dentro del frustum; el ciclo es un pool
    /// (no se crea ni se destruye nada al correr); y todos los modelos quedan sobre el suelo al nacer, sin flotar ni hundirse.
    /// </summary>
    public class AparecerTraficoT704Pruebas
    {
        private const string RutaConfig = "Assets/_Proyecto/Trafico/Datos/ConfigTraficoCalle_Trafico.asset";
        private static readonly string[] Prefabs =
        {
            "CarroTrafico", "CarroTrafico_V01a", "CarroTrafico_V01b", "CarroTrafico_V02", "CarroTrafico_V03", "CarroTrafico_V04",
            "CarroTrafico_V05", "CarroTrafico_V06", "CarroTrafico_V08", "CarroTrafico_V09", "CarroTrafico_V10",
        };

        private sealed class RelojDoble : IRelojMundo
        {
            public bool Noche;
            public float Hora => Noche ? 22f : 12f;
            public int Dia => 1;
            public FranjaDia Franja => Noche ? FranjaDia.Noche : FranjaDia.Dia;
            public bool EsDeNoche => Noche;
            public float HoraAmanecer => 6f;
            public float HoraAnochecer => 20f;
            public bool Pausado => true;
            public event Action<int> HoraEnteraCambio { add { } remove { } }
            public event Action<FranjaDia> FranjaCambio { add { } remove { } }
            public event Action<int> DiaCambio { add { } remove { } }
            public void Pausar(bool pausado) { }
            public void AvanzarHasta(float hora) { }
        }

        private sealed class JugadorDoble : IJugador
        {
            public Transform Raiz { get; set; }
            public ModoJugador Modo => ModoJugador.APie;
            public Camera Camara => null;
            public event Action<ModoJugador> ModoCambio { add { } remove { } }
            public void Colocar(Pose pose) { }
        }

        private GameObject barrio;
        private GameObject piso;
        private RelojDoble reloj;
        private ConfigTraficoCalle config;
        private readonly List<GameObject> extras = new List<GameObject>();
        private float escalaTiempo;

        [SetUp]
        public void Preparar()
        {
            Registros.LimpiarTodos();
            EventosRuido.LimpiarSuscriptores();
            CarroTrafico.Activos.Clear();
            escalaTiempo = Time.timeScale;
            reloj = new RelojDoble();
            Registro<IRelojMundo>.Agregar(reloj);
            config = AssetDatabase.LoadAssetAtPath<ConfigTraficoCalle>(RutaConfig);
            barrio = new GameObject("BarrioDePrueba");
            foreach (Vector2 c in new[] { new Vector2(0f, 0f), new Vector2(80f, 0f), new Vector2(0f, 80f), new Vector2(80f, 80f) })
            {
                GameObject m = new GameObject("Manzana_" + c.x + "_" + c.y);
                m.transform.SetParent(barrio.transform, false);
                m.transform.position = new Vector3(c.x, 0f, c.y);
            }
            piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(40f, -0.5f, 40f);
            piso.transform.localScale = new Vector3(600f, 1f, 600f);
            Physics.SyncTransforms();
        }

        [TearDown]
        public void Limpiar()
        {
            Time.timeScale = escalaTiempo;
            foreach (GameObject extra in extras)
            {
                if (extra != null)
                {
                    Object.DestroyImmediate(extra);
                }
            }
            extras.Clear();
            Object.DestroyImmediate(barrio);
            Object.DestroyImmediate(piso);
            EventosRuido.LimpiarSuscriptores();
            Registros.LimpiarTodos();
            CarroTrafico.Activos.Clear();
        }

        private static float MinY(GameObject carro)
        {
            float menor = float.MaxValue;
            foreach (Renderer r in carro.GetComponentsInChildren<Renderer>())
            {
                if ((r is MeshRenderer || r is SkinnedMeshRenderer) && r.enabled)
                {
                    menor = Mathf.Min(menor, r.bounds.min.y);
                }
            }
            return menor;
        }

        private static Bounds Limites(GameObject carro)
        {
            Bounds b = new Bounds(carro.transform.position, Vector3.zero);
            bool primero = true;
            foreach (Renderer r in carro.GetComponentsInChildren<Renderer>())
            {
                if (!(r is MeshRenderer || r is SkinnedMeshRenderer) || !r.enabled)
                {
                    continue;
                }
                if (primero)
                {
                    b = r.bounds;
                    primero = false;
                }
                else
                {
                    b.Encapsulate(r.bounds);
                }
            }
            return b;
        }

        private Camera CrearCamara(Vector3 posicion, Vector3 mira)
        {
            GameObject c = new GameObject("CamaraDePrueba");
            extras.Add(c);
            c.tag = "MainCamera";
            Camera camara = c.AddComponent<Camera>();
            camara.farClipPlane = 400f;
            camara.fieldOfView = 60f;
            c.transform.position = posicion;
            c.transform.LookAt(mira);
            return camara;
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
        [Timeout(120000)]
        public IEnumerator ConLaCamaraFija_NingunCarroNaceNiSeBorraDentroDelFrustum()
        {
            CarroTrafico prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Proyecto/Trafico/Prefabs/CarroTrafico_V09.prefab").GetComponent<CarroTrafico>();
            Camera camara = CrearCamara(new Vector3(40f, 8f, -50f), new Vector3(40f, 0f, 40f));
            JugadorDoble jugador = new JugadorDoble { Raiz = camara.transform };
            Registro<IJugador>.Agregar(jugador);
            GameObject raiz = new GameObject("TraficoDePrueba");
            extras.Add(raiz);
            TraficoCalle trafico = raiz.AddComponent<TraficoCalle>();
            trafico.Configurar(config, prefab, barrio.transform);
            yield return null;
            yield return null;
            int total = trafico.Carros.Count;
            Assert.AreEqual(config.Maximo, total, "El pool arma todos los carros de una vez");

            Plane[] planos = new Plane[6];
            int n = trafico.Carros.Count;
            bool[] activo = new bool[n];
            Bounds[] limites = new Bounds[n];
            Vector3[] posicion = new Vector3[n];
            for (int i = 0; i < n; i++)
            {
                activo[i] = trafico.Carros[i].gameObject.activeSelf;
                limites[i] = Limites(trafico.Carros[i].gameObject);
                posicion[i] = trafico.Carros[i].transform.position;
            }
            int nacieron = 0;
            int seBorraron = 0;
            int saltos = 0;
            StringBuilder fallos = new StringBuilder();
            Time.timeScale = 4f;
            float inicio = Time.realtimeSinceStartup;
            int vueltas = 0;
            float proximoCambio = Time.realtimeSinceStartup + 2.5f;
            while (Time.realtimeSinceStartup - inicio < 28f)
            {
                yield return null;
                if (Time.realtimeSinceStartup >= proximoCambio)
                {
                    reloj.Noche = !reloj.Noche;
                    proximoCambio += 2.5f;
                    vueltas++;
                }
                GeometryUtility.CalculateFrustumPlanes(camara, planos);
                for (int i = 0; i < n; i++)
                {
                    CarroTrafico carro = trafico.Carros[i];
                    Assert.IsTrue(carro != null, "El pool no destruye carros");
                    bool ahora = carro.gameObject.activeSelf;
                    if (ahora)
                    {
                        Bounds b = Limites(carro.gameObject);
                        bool vistoAhora = GeometryUtility.TestPlanesAABB(planos, b);
                        if (!activo[i])
                        {
                            nacieron++;
                            if (vistoAhora)
                            {
                                fallos.AppendLine(carro.name + " nació dentro del frustum en " + carro.transform.position);
                            }
                        }
                        else if ((carro.transform.position - posicion[i]).magnitude > 10f)
                        {
                            saltos++;
                            if (vistoAhora || GeometryUtility.TestPlanesAABB(planos, limites[i]))
                            {
                                fallos.AppendLine(carro.name + " se teletransportó a la vista: de " + posicion[i] + " a " + carro.transform.position);
                            }
                        }
                        limites[i] = b;
                        posicion[i] = carro.transform.position;
                    }
                    else if (activo[i])
                    {
                        seBorraron++;
                        if (GeometryUtility.TestPlanesAABB(planos, limites[i]))
                        {
                            fallos.AppendLine(carro.name + " se borró dentro del frustum en " + limites[i].center);
                        }
                    }
                    activo[i] = ahora;
                }
            }
            Debug.Log("[T-704] nacieron " + nacieron + ", se borraron " + seBorraron + ", saltos " + saltos + ", cambios de día/noche " + vueltas);
            Assert.GreaterOrEqual(nacieron, 3, "La prueba vio carros nacer");
            Assert.GreaterOrEqual(seBorraron, 3, "La prueba vio carros borrarse");
            Assert.AreEqual(string.Empty, fallos.ToString(), "Carros que aparecieron o desaparecieron a la vista");
        }

        [UnityTest]
        public IEnumerator SinCamara_ElCicloDiaNocheSigueFuncionando()
        {
            CarroTrafico prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Proyecto/Trafico/Prefabs/CarroTrafico_V09.prefab").GetComponent<CarroTrafico>();
            GameObject raiz = new GameObject("TraficoDePrueba");
            extras.Add(raiz);
            TraficoCalle trafico = raiz.AddComponent<TraficoCalle>();
            trafico.Configurar(config, prefab, barrio.transform);
            yield return null;
            yield return null;
            Assert.AreEqual(config.CarrosDeDia, trafico.Vivos);
            reloj.Noche = true;
            yield return null;
            Assert.AreEqual(config.CarrosDeNoche, trafico.Vivos);
            reloj.Noche = false;
            yield return null;
            yield return null;
            Assert.AreEqual(config.CarrosDeDia, trafico.Vivos, "Los carros vuelven a salir (pool)");
        }

        [UnityTest]
        public IEnumerator TodosLosModelos_AlNacerQuedanSobreElSuelo_ElevadoOHundido()
        {
            // El suelo a 0.35 m (como una banqueta o un arroyo elevado): las ruedas deben tocarlo, sin flotar ni hundirse.
            piso.transform.position = new Vector3(40f, -0.15f, 40f);
            Physics.SyncTransforms();
            List<Vector2> centros = new List<Vector2> { new Vector2(0f, 0f), new Vector2(80f, 0f), new Vector2(0f, 80f), new Vector2(80f, 80f) };
            GrafoCalles calles = GrafoCalles.DeManzanas(centros, 80f, 0.2f);
            StringBuilder fallos = new StringBuilder();
            foreach (string nombre in Prefabs)
            {
                CarroTrafico molde = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Proyecto/Trafico/Prefabs/" + nombre + ".prefab").GetComponent<CarroTrafico>();
                CarroTrafico carro = Object.Instantiate(molde);
                extras.Add(carro.gameObject);
                carro.Preparar(config, calles, calles.MasCercano(new Vector3(40f, 0f, -40f)), new System.Random(11), () => null);
                Physics.SyncTransforms();
                yield return null;
                float bajo = MinY(carro.gameObject);
                float topeDelPiso = piso.transform.position.y + 0.5f;
                if (Mathf.Abs(bajo - topeDelPiso) > 0.08f)
                {
                    fallos.AppendLine(nombre + ": las ruedas quedan a y=" + bajo.ToString("F2") + " y el suelo está a " + topeDelPiso.ToString("F2"));
                }
                Object.DestroyImmediate(carro.gameObject);
            }
            Assert.AreEqual(string.Empty, fallos.ToString());
        }

        [UnityTest]
        [Timeout(90000)]
        public IEnumerator UnCarroAtoradoQueSeVe_DaLaVueltaEnVezDeDesaparecer()
        {
            List<Vector2> centros = new List<Vector2> { new Vector2(0f, 0f), new Vector2(80f, 0f), new Vector2(0f, 80f), new Vector2(80f, 80f) };
            GrafoCalles calles = GrafoCalles.DeManzanas(centros, 80f, 0.2f);
            CarroTrafico molde = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Proyecto/Trafico/Prefabs/CarroTrafico_V02.prefab").GetComponent<CarroTrafico>();
            CarroTrafico carro = Object.Instantiate(molde);
            extras.Add(carro.gameObject);
            carro.Preparar(config, calles, calles.MasCercano(new Vector3(40f, 0f, 40f)), new System.Random(5), () => null);
            yield return null;
            // Una pared fija en su carril, y la cámara viéndolo: no puede desaparecer ni reubicarse, así que debe dar la vuelta.
            GameObject pared = GameObject.CreatePrimitive(PrimitiveType.Cube);
            extras.Add(pared);
            pared.transform.SetPositionAndRotation(carro.transform.position + carro.transform.forward * 9f + Vector3.up, carro.transform.rotation);
            pared.transform.localScale = new Vector3(8f, 2.5f, 1f);
            Physics.SyncTransforms();
            Camera camara = CrearCamara(carro.transform.position - carro.transform.forward * 14f + Vector3.up * 8f, carro.transform.position + carro.transform.forward * 4f);
            Time.timeScale = 4f;
            Vector3 anterior = carro.transform.position;
            float maximoSalto = 0f;
            float inicio = Time.realtimeSinceStartup;
            while (carro.Vueltas == 0 && Time.realtimeSinceStartup - inicio < 40f)
            {
                yield return null;
                maximoSalto = Mathf.Max(maximoSalto, (carro.transform.position - anterior).magnitude);
                anterior = carro.transform.position;
                Assert.IsTrue(carro.EnVista(camara), "El carro sigue a la vista de la cámara");
            }
            Assert.GreaterOrEqual(carro.Vueltas, 1, "Dio la vuelta para salir del atorón");
            Assert.Less(maximoSalto, 3f, "No se teletransportó (salto máximo en un cuadro " + maximoSalto.ToString("F1") + " m)");
            yield return Esperar(4f);
            Assert.Greater(carro.Velocidad, 2f, "Después de dar la vuelta, anda (causa " + carro.Causa + ", destino " + carro.NodoDestino + ", pos " + carro.transform.position + ", rumbo " + carro.transform.forward + ", chocado " + carro.Chocado + ", bloqueado " + carro.Bloqueado + ")");
        }

        [UnityTest]
        public IEnumerator UnCarroQueSeReubica_SeVuelveAPegarAlSueloNuevo()
        {
            CarroTrafico molde = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Proyecto/Trafico/Prefabs/CarroTrafico_V10.prefab").GetComponent<CarroTrafico>();
            List<Vector2> centros = new List<Vector2> { new Vector2(0f, 0f), new Vector2(80f, 0f), new Vector2(0f, 80f), new Vector2(80f, 80f) };
            GrafoCalles calles = GrafoCalles.DeManzanas(centros, 80f, 0.2f);
            CarroTrafico carro = Object.Instantiate(molde);
            extras.Add(carro.gameObject);
            carro.Preparar(config, calles, calles.MasCercano(new Vector3(40f, 0f, -40f)), new System.Random(2), () => null);
            yield return null;
            Assert.AreEqual(0f, MinY(carro.gameObject), 0.08f, "En el suelo plano, las ruedas tocan y=0");
            // Una plataforma de 0.4 m que empieza unos 10 m adelante: el rayo la toma como suelo.
            GameObject plataforma = GameObject.CreatePrimitive(PrimitiveType.Cube);
            extras.Add(plataforma);
            plataforma.transform.localScale = new Vector3(200f, 1f, 200f);
            Vector3 centro = carro.transform.position + carro.transform.forward * 110f;
            plataforma.transform.position = new Vector3(centro.x, -0.1f, centro.z);
            Physics.SyncTransforms();
            yield return Esperar(3f);
            Assert.Greater(carro.AlturaSuelo, 0.3f, "Al subir a la plataforma, el suelo que ve el rayo sube (" + carro.AlturaSuelo.ToString("F2") + ")");
            Assert.AreEqual(carro.AlturaSuelo, MinY(carro.gameObject), 0.1f, "Y las ruedas siguen sobre el suelo");
            GameObject luz = new GameObject("LuzDePrueba");
            extras.Add(luz);
            luz.AddComponent<Light>().type = LightType.Directional;
            luz.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            Camera foto = CrearCamara(carro.transform.position + carro.transform.right * 6f + Vector3.up * 1.2f, carro.transform.position + Vector3.up * 0.4f);
            foto.clearFlags = CameraClearFlags.SolidColor;
            foto.backgroundColor = new Color(0.45f, 0.55f, 0.7f);
            CapturaPrueba.Guardar(foto, "s1c-T-704-carro-sobre-el-suelo.png");
        }
    }
}
