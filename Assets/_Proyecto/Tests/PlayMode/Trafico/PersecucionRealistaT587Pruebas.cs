using System;
using System.Collections;
using System.Collections.Generic;
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
    /// T-587: persecución realista sobre un barrio de prueba de 2x3 manzanas. Las patrullas salen fuera de la vista de la cámara, algunas se adelantan
    /// al cruce hacia donde corres, te pierden si una pared se interpone (y el calor baja de más) y al bajar el calor se van por la calle hasta salir de la vista.
    /// </summary>
    public class PersecucionRealistaT587Pruebas
    {
        private const string RutaConfig = "Assets/_Proyecto/Trafico/Datos/ConfigPersecucion_Trafico.asset";
        private const string RutaDron = "Assets/_Proyecto/Trafico/Prefabs/Dron.prefab";
        private const string RutaPatrulla = "Assets/_Proyecto/Trafico/Prefabs/Patrulla.prefab";
        private const float Lado = 80f;
        private const float Altura = 0.2f;
        private const float Paso = 94f;
        private const float RadioVistaGrande = 400f;
        private const float EscalaDeTiempo = 5f;

        private sealed class CalorDoble : ICalor
        {
            public float Valor { get; private set; }
            public float Maximo => 100f;
            public int Nivel { get; private set; }
            public float Enfriado { get; private set; }
            public event Action<float> ValorCambio;
            public event Action<int> NivelCambio;

            public void Poner(int nivel)
            {
                if (nivel == Nivel)
                {
                    return;
                }
                Nivel = nivel;
                Valor = nivel * 30f;
                ValorCambio?.Invoke(Valor);
                NivelCambio?.Invoke(Nivel);
            }

            public void Sumar(float cantidad, MotivoCalor motivo) { }
            public void Enfriar(float cantidad) => Enfriado += cantidad;
            public void Borrar() => Poner(0);
        }

        private sealed class JugadorDoble : IJugador
        {
            public Transform Raiz { get; set; }
            public ModoJugador Modo => ModoJugador.APie;
            public Camera Camara => null;
            public event Action<ModoJugador> ModoCambio { add { } remove { } }
            public void Colocar(Pose pose) { }
        }

        private readonly List<GameObject> extras = new List<GameObject>();
        private GameObject barrio;
        private GameObject jugador;
        private CalorDoble calor;
        private GestorPersecucion gestor;
        private ConfigPersecucion config;
        private float escalaAnterior;

        private static readonly Vector2[] Centros =
        {
            new Vector2(-Paso, Paso / 2f), new Vector2(0f, Paso / 2f), new Vector2(Paso, Paso / 2f),
            new Vector2(-Paso, -Paso / 2f), new Vector2(0f, -Paso / 2f), new Vector2(Paso, -Paso / 2f)
        };

        [SetUp]
        public void Preparar()
        {
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
            escalaAnterior = Time.timeScale;
            GameObject piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(900f, 1f, 900f);
            extras.Add(piso);
            config = Object.Instantiate(AssetDatabase.LoadAssetAtPath<ConfigPersecucion>(RutaConfig));
            Poner("segundosArresto", 9999f); // T-665: estas pruebas miran la persecución, no el arresto
            Poner("radioVistaDron", 1f);     // y los drones y las cámaras no deben ver a través de las paredes de prueba
            Poner("camarasPorCruce", 0f);
        }

        [TearDown]
        public void Limpiar()
        {
            Time.timeScale = escalaAnterior;
            foreach (GameObject extra in extras)
            {
                if (extra != null)
                {
                    Object.DestroyImmediate(extra);
                }
            }
            extras.Clear();
            if (barrio != null)
            {
                Object.DestroyImmediate(barrio);
            }
            if (jugador != null)
            {
                Object.DestroyImmediate(jugador);
            }
            Object.DestroyImmediate(config);
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
        }

        private void Poner(string campo, float valor)
        {
            SerializedObject so = new SerializedObject(config);
            SerializedProperty propiedad = so.FindProperty(campo);
            if (propiedad.propertyType == SerializedPropertyType.Integer)
            {
                propiedad.intValue = (int)valor;
            }
            else
            {
                propiedad.floatValue = valor;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private GrafoCalles GrafoDePrueba()
        {
            GrafoCalles grafo = GrafoCalles.DeManzanas(Centros, Lado, Altura);
            TraficoCalle.AlargarHaciaLasAfueras(grafo);
            return grafo;
        }

        // Barrio con las manzanas, jugador (registrado) en <posicion> y el gestor con el doble de calor.
        private void ArmarGestor(Vector3 posicion)
        {
            barrio = new GameObject("BarrioPrueba");
            for (int i = 0; i < Centros.Length; i++)
            {
                GameObject m = new GameObject("Manzana_" + (i + 1));
                m.transform.SetParent(barrio.transform, false);
                m.transform.position = new Vector3(Centros[i].x, 0f, Centros[i].y);
            }
            jugador = new GameObject("JugadorDePrueba");
            jugador.transform.position = posicion;
            Registro<IJugador>.Agregar(new JugadorDoble { Raiz = jugador.transform });
            calor = new CalorDoble();
            Registro<ICalor>.Agregar(calor);
            GameObject objeto = new GameObject("Persecucion");
            objeto.SetActive(false);
            objeto.transform.SetParent(barrio.transform, false);
            gestor = objeto.AddComponent<GestorPersecucion>();
            gestor.Configurar(config, AssetDatabase.LoadAssetAtPath<GameObject>(RutaDron).GetComponent<DronMundo>(),
                AssetDatabase.LoadAssetAtPath<GameObject>(RutaPatrulla).GetComponent<PatrullaMundo>());
            objeto.SetActive(true);
        }

        private Camera PonerCamara(Vector3 desde, Vector3 haciaDonde)
        {
            GameObject objeto = new GameObject("CamaraPrueba");
            objeto.tag = "MainCamera";
            Camera camara = objeto.AddComponent<Camera>();
            camara.fieldOfView = 60f;
            camara.farClipPlane = 600f;
            Vector3 mira = haciaDonde - desde;
            mira.y = 0f;
            objeto.transform.SetPositionAndRotation(desde + Vector3.up * 1.7f, Quaternion.LookRotation(mira, Vector3.up));
            extras.Add(objeto);
            return camara;
        }

        [UnityTest]
        public IEnumerator Patrullas_AparecenFueraDeLaVista_YLejosDelJugador()
        {
            GrafoCalles grafo = GrafoDePrueba();
            Vector3 posicion = grafo.Nodo(0) + new Vector3(10f, 0f, 0f);
            // La cámara mira justo hacia el nodo que, sin el filtro, sería la salida más natural (el más cercano a 80 m).
            int natural = 0;
            float error = float.MaxValue;
            for (int i = 0; i < grafo.Cantidad; i++)
            {
                Vector3 d = grafo.Nodo(i) - posicion;
                d.y = 0f;
                float e = Mathf.Abs(d.magnitude - GestorPersecucion.DistanciaSalidaPatrulla);
                if (e < error)
                {
                    error = e;
                    natural = i;
                }
            }
            Camera camara = PonerCamara(posicion, grafo.Nodo(natural));
            Assert.IsTrue(ReglaVistaPolicia.LoVe(camara, grafo.Nodo(natural), 400f), "Control: la salida natural sí estaría a la vista");
            ArmarGestor(posicion);
            calor.Poner(3);
            yield return null;
            Assert.AreEqual(3, gestor.Patrullas);
            PatrullaMundo[] patrullas = gestor.GetComponentsInChildren<PatrullaMundo>();
            Assert.AreEqual(3, patrullas.Length);
            foreach (PatrullaMundo p in patrullas)
            {
                Vector3 d = p.transform.position - posicion;
                d.y = 0f;
                Assert.IsFalse(ReglaVistaPolicia.LoVe(camara, p.transform.position, 400f), "Una patrulla salió a la vista en " + p.transform.position);
                Assert.GreaterOrEqual(d.magnitude, config.DistanciaMinimaSalida, "Salió demasiado cerca: " + d.magnitude + " m");
            }
        }

        [UnityTest]
        public IEnumerator Patrulla_QueCortaElPaso_VaAlCruceDeAdelante_YLaQueSigue_AlDeAtras()
        {
            Poner("radioVista", RadioVistaGrande);
            Poner("segundosAdelantoCorte", 5f);
            GrafoCalles grafo = GrafoDePrueba();
            int n0 = 0;
            int n1 = grafo.Vecinos(n0)[0];
            Vector3 calle = (grafo.Nodo(n1) - grafo.Nodo(n0)).normalized;
            GameObject cuerpo = new GameObject("JugadorQueCorre");
            extras.Add(cuerpo);
            cuerpo.transform.position = grafo.Nodo(n0) + calle * 10f;
            Vector3 inicio = cuerpo.transform.position;
            const float velocidad = 12f;
            Func<Vector3?> donde = () => cuerpo.transform.position;
            PatrullaMundo seguidora = CrearPatrulla(grafo, n1, donde, false);
            PatrullaMundo cortadora = CrearPatrulla(grafo, n1, donde, true);
            float limite = Time.time + 1.5f;
            while (Time.time < limite)
            {
                cuerpo.transform.position += calle * velocidad * Time.deltaTime;
                yield return null;
            }
            Assert.IsTrue(cortadora.VeAlJugador && seguidora.VeAlJugador, "Las dos lo ven");
            Vector3 jugadorAhora = cuerpo.transform.position;
            float avanzado = Vector3.Dot(jugadorAhora - inicio, calle);
            Assert.Greater(avanzado, 15f, "El jugador corrió");
            float cortaEn = Vector3.Dot(grafo.Nodo(cortadora.NodoMeta) - inicio, calle);
            float sigueEn = Vector3.Dot(grafo.Nodo(seguidora.NodoMeta) - inicio, calle);
            Assert.Greater(cortaEn, sigueEn + 20f, "La que corta el paso va a un cruce más adelante (" + cortaEn + " contra " + sigueEn + ")");
            Assert.Greater(cortaEn, avanzado, "El cruce de la que corta el paso queda delante del jugador");
        }

        private PatrullaMundo CrearPatrulla(GrafoCalles grafo, int nodo, Func<Vector3?> donde, bool corta)
        {
            PatrullaMundo patrulla = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPatrulla).GetComponent<PatrullaMundo>());
            extras.Add(patrulla.gameObject);
            patrulla.Preparar(config.Crear(), config, grafo, nodo, donde);
            patrulla.Intercepta = corta;
            Physics.SyncTransforms();
            return patrulla;
        }

        [UnityTest]
        public IEnumerator SinVerAlJugador_SeDaPorPerdido_PeroElCalorNoBajaDeMas()
        {
            Poner("segundosParaPerder", 4f);
            Poner("enfriamientoSinVista", 10f);
            Poner("radioVista", RadioVistaGrande);
            GrafoCalles grafo = GrafoDePrueba();
            Vector3 posicion = grafo.Nodo(0) + new Vector3(0f, 0f, 12f);
            ArmarGestor(posicion);
            calor.Poner(1);
            Time.timeScale = EscalaDeTiempo;
            // Primero lo ven: se acercan y hay contacto. Mientras lo ven no hay enfriamiento extra.
            float limite = Time.time + 80f;
            PatrullaMundo patrulla = null;
            while (Time.time < limite)
            {
                yield return null;
                patrulla = gestor.GetComponentInChildren<PatrullaMundo>();
                if (patrulla != null && patrulla.VeAlJugador)
                {
                    break;
                }
            }
            Assert.IsNotNull(patrulla);
            Assert.IsTrue(patrulla.VeAlJugador, "La patrulla debía verlo al acercarse");
            float despuesDeVer = Time.time + 6f;
            while (Time.time < despuesDeVer)
            {
                yield return null;
            }
            Assert.AreEqual(0f, calor.Enfriado, 0.0001f, "Mientras lo ven no baja de más");
            Assert.IsFalse(gestor.Perdido);

            // Se mete en un cuarto cerrado: las patrullas ya no lo ven.
            Encerrar(jugador.transform.position);
            Physics.SyncTransforms();
            float inicio = Time.time;
            limite = Time.time + 20f;
            while (Time.time < limite && !gestor.Perdido)
            {
                yield return null;
            }
            Assert.IsTrue(gestor.Perdido, "Tras unos segundos sin verlo lo dan por perdido");
            Assert.IsFalse(patrulla.VeAlJugador, "Con la pared en medio no lo ve");
            float perdidoEn = Time.time - inicio;
            Assert.GreaterOrEqual(perdidoEn, 4f - 0.5f, "No lo pierden antes de tiempo");
            float antes = calor.Enfriado;
            float mas = Time.time + 4f;
            while (Time.time < mas)
            {
                yield return null;
            }
            Assert.AreEqual(0f, calor.Enfriado - antes, 0.0001f, "T-665: ya no baja de más; la policía no se va sola, solo escapando de su vista y del círculo");
        }

        private void Encerrar(Vector3 centro)
        {
            const float Medio = 3f;
            Vector3[] lados = { Vector3.right, Vector3.left, Vector3.forward, Vector3.back };
            foreach (Vector3 lado in lados)
            {
                GameObject pared = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bool enX = Mathf.Abs(lado.x) > 0.5f;
                pared.transform.position = centro + lado * Medio + Vector3.up * 5f;
                pared.transform.localScale = enX ? new Vector3(0.5f, 12f, 2f * Medio + 1f) : new Vector3(2f * Medio + 1f, 12f, 0.5f);
                extras.Add(pared);
            }
        }

        [UnityTest]
        public IEnumerator AlBajarElCalor_LasPatrullasSeVanPorLaCalle_YSoloDesaparecenFueraDeLaVista()
        {
            GrafoCalles grafo = GrafoDePrueba();
            Vector3 posicion = grafo.Nodo(0) + new Vector3(0f, 0f, 12f);
            Camera camara = PonerCamara(posicion, grafo.Nodo(grafo.Vecinos(0)[0]));
            ArmarGestor(posicion);
            calor.Poner(3);
            Time.timeScale = EscalaDeTiempo;
            // Que lleguen cerca.
            float limite = Time.time + 80f;
            PatrullaMundo[] patrullas = gestor.GetComponentsInChildren<PatrullaMundo>();
            Assert.AreEqual(3, patrullas.Length);
            while (Time.time < limite)
            {
                yield return null;
                bool todasCerca = true;
                foreach (PatrullaMundo p in patrullas)
                {
                    Vector3 d = p.transform.position - posicion;
                    d.y = 0f;
                    todasCerca &= d.magnitude < 30f;
                }
                if (todasCerca)
                {
                    break;
                }
            }
            calor.Poner(0);
            yield return null;
            Assert.AreEqual(0, gestor.Patrullas);
            Assert.AreEqual(3, CuentaRetirando(patrullas), "Las tres se están yendo");
            Dictionary<PatrullaMundo, Vector3> ultima = new Dictionary<PatrullaMundo, Vector3>();
            limite = Time.time + 90f;
            int desaparecieronALaVista = 0;
            while (Time.time < limite && gestor.Retirandose > 0)
            {
                foreach (PatrullaMundo p in patrullas)
                {
                    if (p != null)
                    {
                        ultima[p] = p.transform.position;
                    }
                }
                yield return null;
                foreach (PatrullaMundo p in patrullas)
                {
                    if (p == null && ultima.TryGetValue(p, out Vector3 donde))
                    {
                        Vector3 d = donde - posicion;
                        d.y = 0f;
                        if (ReglaVistaPolicia.LoVe(camara, donde, 400f) && d.magnitude < config.DistanciaRetiro)
                        {
                            desaparecieronALaVista++;
                        }
                        ultima.Remove(p);
                    }
                }
            }
            Assert.AreEqual(0, gestor.Retirandose, "Se fueron todas");
            Assert.AreEqual(0, desaparecieronALaVista, "Ninguna desapareció a la vista y de cerca");
        }

        // Genera docs/capturas/T-587-persecucion.png: vista aérea de dos patrullas que llegan por calles distintas hacia el jugador.
        [UnityTest]
        public IEnumerator Captura_PatrullasLlegandoPorLasCalles()
        {
            GrafoCalles grafo = GrafoDePrueba();
            foreach (Vector2 c in Centros)
            {
                GameObject bloque = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bloque.transform.position = new Vector3(c.x, 8f, c.y);
                bloque.transform.localScale = new Vector3(Lado, 16f, Lado);
                extras.Add(bloque);
            }
            Vector3 posicion = grafo.Nodo(0) + new Vector3(0f, 0f, 12f);
            ArmarGestor(posicion);
            calor.Poner(3);
            Time.timeScale = EscalaDeTiempo;
            float limite = Time.time + 8f;
            while (Time.time < limite)
            {
                yield return null;
            }
            Time.timeScale = 0f;
            GameObject camObjeto = new GameObject("CamaraCaptura");
            extras.Add(camObjeto);
            Camera cam = camObjeto.AddComponent<Camera>();
            cam.fieldOfView = 60f;
            cam.farClipPlane = 800f;
            camObjeto.transform.position = posicion + new Vector3(0f, 190f, -120f);
            camObjeto.transform.LookAt(posicion + Vector3.forward * 20f);
            yield return null;
            RenderTexture rt = new RenderTexture(1280, 720, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            Texture2D foto = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            foto.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            foto.Apply();
            cam.targetTexture = null;
            RenderTexture.active = null;
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(Application.dataPath, "..", "docs", "capturas", "T-587-persecucion.png"), foto.EncodeToPNG());
            Object.DestroyImmediate(foto);
            Object.DestroyImmediate(rt);
            Assert.AreEqual(3, gestor.Patrullas);
        }

        private static int CuentaRetirando(PatrullaMundo[] patrullas)
        {
            int cuenta = 0;
            foreach (PatrullaMundo p in patrullas)
            {
                cuenta += p != null && p.Retirando ? 1 : 0;
            }
            return cuenta;
        }

        [UnityTest]
        public IEnumerator MasCalor_MasPatrullas_YLasSalidasSonDistintas()
        {
            GrafoCalles grafo = GrafoDePrueba();
            ArmarGestor(grafo.Nodo(0) + new Vector3(0f, 0f, 12f));
            calor.Poner(1);
            yield return null;
            int nivel1 = gestor.Patrullas;
            calor.Poner(2);
            yield return null;
            int nivel2 = gestor.Patrullas;
            calor.Poner(3);
            yield return null;
            int nivel3 = gestor.Patrullas;
            Assert.Less(nivel1, nivel2);
            Assert.Less(nivel2, nivel3);
            PatrullaMundo[] patrullas = gestor.GetComponentsInChildren<PatrullaMundo>();
            for (int i = 0; i < patrullas.Length; i++)
            {
                for (int j = i + 1; j < patrullas.Length; j++)
                {
                    Assert.Greater(Vector3.Distance(patrullas[i].transform.position, patrullas[j].transform.position), 5f, "No salen encimadas");
                }
            }
        }
    }
}
