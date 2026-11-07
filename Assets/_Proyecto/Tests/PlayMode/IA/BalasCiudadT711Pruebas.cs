using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.IA.Datos;
using MechaCyberpunk.Progresion.Guardado;
using MechaCyberpunk.Trafico.Datos;
using MechaCyberpunk.Trafico.Persecucion;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.IA
{
    /// <summary>
    /// T-711: en la escena Ciudad real (Ciudad_Prologo), las balas de los pandilleros SKULLS, los policías y el dron no atraviesan paredes, las casas
    /// nuevas del kit, los carros ni cajas: sin línea de vista no hay disparo ni daño; con línea libre sí. Las posiciones salen de la propia escena
    /// (los colliders reales, el volumen del callejón), no de coordenadas fijas.
    /// </summary>
    public class BalasCiudadT711Pruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string RutaPandillero = "Assets/_Proyecto/IA/Prefabs/PandilleroArmado.prefab";
        private const string RutaPolicia = "Assets/_Proyecto/IA/Prefabs/PoliciaAPie.prefab";
        private const string RutaDatosSkulls = "Assets/_Proyecto/IA/Datos/DatosPandilleroAPie_Calaveras.asset";
        private const string RutaConfig = "Assets/_Proyecto/Trafico/Datos/ConfigPersecucion_Trafico.asset";
        private const string RutaDron = "Assets/_Proyecto/Trafico/Prefabs/Dron.prefab";
        private const float AlturaPecho = 1.2f;

        private sealed class BlancoDoble : MonoBehaviour, IRecibeDanio
        {
            public int Impactos;
            public ResultadoDanio RecibirDanio(InfoDanio info)
            {
                Impactos++;
                return new ResultadoDanio(info.Cantidad, TipoBloqueo.Ninguno, false);
            }
        }


        public override void Setup()
        {
            base.Setup();
            InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
        }

        private readonly List<GameObject> extras = new List<GameObject>();
        private readonly List<Object> extrasConfig = new List<Object>();
        private string carpetaGuardado;
        private Func<float> azarPistola;
        private Func<float> azarDron;

        // Lo que la ciudad deja vivo entre escenas (HuecosBala, VisorRayosX, ModoVideo): se guarda lo que había y se borra lo nuevo al terminar,
        // para que las pruebas que siguen en la misma corrida no hereden objetos de la ciudad.
        private readonly HashSet<GameObject> persistentesAntes = new HashSet<GameObject>();

        private void AnotarPersistentes()
        {
            persistentesAntes.Clear();
            foreach (GameObject g in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (g.scene.name == "DontDestroyOnLoad" && g.transform.parent == null)
                {
                    persistentesAntes.Add(g);
                }
            }
        }

        private void BorrarPersistentesNuevos()
        {
            foreach (GameObject g in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (g.scene.name == "DontDestroyOnLoad" && g.transform.parent == null && !persistentesAntes.Contains(g))
                {
                    Object.DestroyImmediate(g);
                }
            }
        }

        public IEnumerator CargarCiudad()
        {
            AnotarPersistentes();
            // El autoguardado de la ciudad va a una carpeta temporal: ni pisa la partida real ni le deja su estado a las pruebas que siguen.
            carpetaGuardado = Path.Combine(Application.temporaryCachePath, "balas_ciudad_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(carpetaGuardado);
            ServicioGuardado.RutaForzada = Path.Combine(carpetaGuardado, "partida.json");
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            float fin = Time.realtimeSinceStartup + 40f;
            while (Registro<IJugador>.Actual == null || Registro<IJugador>.Actual.Raiz == null)
            {
                Assert.Less(Time.realtimeSinceStartup, fin, "El jugador de la ciudad no apareció");
                yield return null;
            }
            yield return new WaitForSecondsRealtime(0.5f);
            azarPistola = PistolaPandillero.Azar;
            azarDron = DronMundo.Azar;
            PistolaPandillero.Azar = () => 0f; // si hay línea libre, acierta
            DronMundo.Azar = () => 0f;
        }

        [UnityTearDown]
        public IEnumerator Descargar()
        {
            PistolaPandillero.Azar = azarPistola;
            DronMundo.Azar = azarDron;
            Time.timeScale = 1f;
            foreach (GameObject extra in extras)
            {
                if (extra != null)
                {
                    Object.DestroyImmediate(extra);
                }
            }
            extras.Clear();
            foreach (Object o in extrasConfig) { Object.Destroy(o); }
            extrasConfig.Clear();
            foreach (GameObject raiz in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                Object.DestroyImmediate(raiz);
            }
            Registros.LimpiarTodos();
            BorrarPersistentesNuevos();
            ServicioGuardado.RutaForzada = null;
            if (carpetaGuardado != null && Directory.Exists(carpetaGuardado))
            {
                Directory.Delete(carpetaGuardado, true);
            }
            yield return null;
        }

        private static IEnumerator Esperar(float segundos)
        {
            float fin = Time.time + segundos;
            while (Time.time < fin)
            {
                yield return null;
            }
        }

        private static Bounds CajaDeColliders(Transform raiz)
        {
            bool hay = false;
            Bounds caja = new Bounds();
            foreach (Collider c in raiz.GetComponentsInChildren<Collider>())
            {
                if (!c.enabled || c.isTrigger)
                {
                    continue;
                }
                if (!hay)
                {
                    caja = c.bounds;
                    hay = true;
                }
                else
                {
                    caja.Encapsulate(c.bounds);
                }
            }
            return hay ? caja : new Bounds(raiz.position, Vector3.zero);
        }

        private static Transform Buscar(string ruta)
        {
            GameObject go = GameObject.Find(ruta);
            return go != null ? go.transform : null;
        }

        private static IEnumerable<Transform> Hijos(string ruta, int maximo)
        {
            Transform padre = Buscar(ruta);
            if (padre == null)
            {
                yield break;
            }
            List<Transform> lista = new List<Transform>();
            foreach (Transform hijo in padre)
            {
                if (hijo.gameObject.activeInHierarchy)
                {
                    lista.Add(hijo);
                }
            }
            int paso = Mathf.Max(1, lista.Count / maximo);
            for (int i = 0; i < lista.Count && maximo > 0; i += paso, maximo--)
            {
                yield return lista[i];
            }
        }

        private static GameObject CrearBlanco(Vector3 pecho)
        {
            GameObject blanco = new GameObject("BlancoPrueba");
            blanco.transform.position = pecho - Vector3.up * AlturaPecho;
            CapsuleCollider capsula = blanco.AddComponent<CapsuleCollider>();
            capsula.height = 1.8f;
            capsula.radius = 0.35f;
            capsula.center = new Vector3(0f, 0.9f, 0f);
            blanco.AddComponent<BlancoDoble>();
            return blanco;
        }

        // El punto sobre el suelo real de la escena.
        private static Vector3 ConSuelo(Vector3 p)
        {
            return Physics.Raycast(p + Vector3.up * 3f, Vector3.down, out RaycastHit h, 8f, ~0, QueryTriggerInteraction.Ignore) ? h.point : p;
        }

        private static string Cuando(Transform t) => t.parent != null ? t.parent.name + "/" + t.name : t.name;

        // Muestras reales de la ciudad: casas del kit (callejón y suburbios), edificios, carros del tráfico y robables, y cosas del callejón.
        private static List<(Transform raiz, string tipo)> Muestras()
        {
            List<(Transform, string)> lista = new List<(Transform, string)>();
            foreach (Transform t in Hijos("S2c_CalleDorada/Casas", 6)) { lista.Add((t, "casa")); }
            foreach (Transform t in Hijos("S2c_Suburbios/Casas", 4)) { lista.Add((t, "casa")); }
            foreach (Transform t in Hijos("Barrio/Manzana_4/Edificios", 4)) { lista.Add((t, "edificio")); }
            foreach (Transform t in Hijos("Barrio/Manzana_2/Edificios", 3)) { lista.Add((t, "edificio")); }
            foreach (Transform t in Hijos("GTA_T511", 5)) { if (t.name.StartsWith("CarroRobable")) { lista.Add((t, "carro")); } }
            foreach (Transform t in Hijos("GTA_T451", 3)) { if (t.name.StartsWith("CarroTrafico")) { lista.Add((t, "carro")); } }
            Transform estacionado = Buscar("S2c_CalleDorada/Cosas/CarroEstacionado");
            if (estacionado != null) { lista.Add((estacionado, "carro")); }
            Transform contenedor = Buscar("Barrio/Manzana_4/Callejon/Contenedor");
            if (contenedor != null) { lista.Add((contenedor, "caja")); }
            return lista;
        }

        private static IEnumerable<Collider> Solidos(Transform raiz) => raiz.GetComponentsInChildren<Collider>().Where(c => c.enabled && !c.isTrigger);

        [UnityTest]
        public IEnumerator LasBalas_NoAtraviesanNadaDeLaCiudadReal_ParedesCasasCarrosYCajas()
        {
            yield return CargarCiudad();
            Physics.SyncTransforms();
            GameObject tirador = new GameObject("TiradorPrueba");
            extras.Add(tirador);
            PistolaPandillero pistola = tirador.AddComponent<PistolaPandillero>();
            List<string> fallos = new List<string>();
            Dictionary<string, int> validas = new Dictionary<string, int>();
            foreach ((Transform raiz, string tipo) in Muestras())
            {
                Bounds caja = CajaDeColliders(raiz);
                if (caja.size.sqrMagnitude < 0.01f || caja.max.y < 1.5f)
                {
                    continue;
                }
                float y = Mathf.Clamp(AlturaPecho + caja.min.y, caja.min.y + 0.3f, caja.max.y - 0.2f);
                foreach (Vector3 eje in new[] { Vector3.right, Vector3.forward })
                {
                    float medio = Mathf.Abs(eje.x) * caja.extents.x + Mathf.Abs(eje.z) * caja.extents.z;
                    Vector3 centro = new Vector3(caja.center.x, y, caja.center.z);
                    Vector3 a = centro - eje * (medio + 2.5f);
                    Vector3 b = centro + eje * (medio + 2.5f);
                    GameObject blanco = CrearBlanco(b);
                    BlancoDoble doble = blanco.GetComponent<BlancoDoble>();
                    tirador.transform.position = a;
                    Physics.SyncTransforms();
                    Vector3 hacia = b - a;
                    float distancia = hacia.magnitude;

                    // Control: sin los colliders de la muestra la línea queda libre (si no, esta posición no sirve y se salta).
                    Collider[] solidos = Solidos(raiz).ToArray();
                    foreach (Collider c in solidos) { c.enabled = false; }
                    Physics.SyncTransforms();
                    bool libre = pistola.HayLinea(a, hacia.normalized, distancia, doble, out _);
                    foreach (Collider c in solidos) { c.enabled = true; }
                    Physics.SyncTransforms();
                    if (!libre)
                    {
                        Object.Destroy(blanco);
                        continue;
                    }
                    validas[tipo] = (validas.TryGetValue(tipo, out int n) ? n : 0) + 1;
                    bool llega = pistola.HayLinea(a, hacia.normalized, distancia, doble, out Vector3 punto);
                    bool acierta = pistola.Disparar(a, b, doble, 8f, 1f);
                    if (llega || acierta || doble.Impactos > 0)
                    {
                        fallos.Add(tipo + " " + Cuando(raiz) + " eje " + eje + " (llega=" + llega + " acierta=" + acierta + " impactos=" + doble.Impactos + ")");
                    }
                    Object.Destroy(blanco);
                    yield return null;
                }
            }
            string resumen = string.Join(", ", validas.Select(p => p.Key + "=" + p.Value));
            Debug.Log("[T-711] Muestras válidas por tipo: " + resumen);
            Assert.IsEmpty(fallos, "Balas que atraviesan: " + string.Join(" | ", fallos));
            Assert.GreaterOrEqual(validas.GetValueOrDefault("casa"), 4, "Casas del kit probadas (" + resumen + ")");
            Assert.GreaterOrEqual(validas.GetValueOrDefault("carro"), 4, "Carros probados (" + resumen + ")");
            Assert.GreaterOrEqual(validas.GetValueOrDefault("edificio"), 2, "Edificios probados (" + resumen + ")");
        }

        // ---- Tiradores reales (SKULLS, policía y dron) con un obstáculo real en medio ----

        // Vida más escudo: el escudo (T-684) absorbe primero, así que el daño se ve en la suma.
        private static float Reserva(IVida vida) => vida.Actual + (Registro<IEscudo>.Actual?.Actual ?? 0f);

        private IVida VidaJugador => Registro<IJugador>.Actual.Raiz.GetComponentInChildren<IVida>();

        private static IEnumerator PonerJugador(Vector3 punto, Vector3 mirandoA)
        {
            if (Physics.Raycast(punto + Vector3.up * 4f, Vector3.down, out RaycastHit piso, 8f, ~0, QueryTriggerInteraction.Ignore))
            {
                punto = piso.point;
            }
            punto.y += 0.1f;
            Vector3 mira = mirandoA - punto;
            mira.y = 0f;
            Registro<IJugador>.Actual.Colocar(new Pose(punto, Quaternion.LookRotation(mira.sqrMagnitude > 0.01f ? mira.normalized : Vector3.forward)));
            // Los triggers de lugar (la casa, un local) avisan que entró al llegar: ya adentro de la física se avisa que está en la calle.
            yield return null;
            yield return null;
            EventosLugares.PublicarSalio(IdLugar.Calle);
        }

        private GameObject CrearSkull(Vector3 punto, Vector3 mirandoA)
        {
            GameObject grupo = new GameObject("GrupoSkullsPrueba");
            grupo.SetActive(false);
            grupo.transform.position = punto;
            DatosPandilleroAPie datos = AssetDatabase.LoadAssetAtPath<DatosPandilleroAPie>(RutaDatosSkulls);
            GameObject p = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPandillero), grupo.transform);
            p.transform.SetPositionAndRotation(punto, Quaternion.LookRotation(Vector3.ProjectOnPlane(mirandoA - punto, Vector3.up)));
            SerializedObject soP = new SerializedObject(p.GetComponent<PandilleroAPie>());
            soP.FindProperty("datos").objectReferenceValue = datos;
            soP.ApplyModifiedPropertiesWithoutUndo();
            PandillaAPie pandilla = grupo.AddComponent<PandillaAPie>();
            SerializedObject so = new SerializedObject(pandilla);
            so.FindProperty("datos").objectReferenceValue = datos;
            so.FindProperty("idPandilla").enumValueIndex = (int)IdPandilla.Calaveras;
            so.ApplyModifiedPropertiesWithoutUndo();
            grupo.SetActive(true);
            extras.Add(grupo);
            return grupo;
        }

        private GameObject CrearPolicia(Vector3 punto, Vector3 mirandoA)
        {
            GameObject p = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPolicia), punto,
                Quaternion.LookRotation(Vector3.ProjectOnPlane(mirandoA - punto, Vector3.up)));
            extras.Add(p);
            return p;
        }

        private DronMundo CrearDron(Vector3 punto)
        {
            // Una copia de la configuración para que dispare con la búsqueda en cero: así la ciudad real no manda patrullas ni drones propios.
            ConfigPersecucion config = Object.Instantiate(AssetDatabase.LoadAssetAtPath<ConfigPersecucion>(RutaConfig));
            typeof(ConfigPersecucion).GetField("nivelDisparoDron", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(config, 0);
            extrasConfig.Add(config);
            GameObject dron = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaDron), punto, Quaternion.identity);
            DronMundo mundo = dron.GetComponent<DronMundo>();
            // Se queda donde está: su "jugador" queda justo debajo de la altura de vuelo.
            mundo.Preparar(config.Crear(), config, () => dron.transform.position - Vector3.up * config.AlturaDron);
            extras.Add(dron);
            return mundo;
        }

        private static int Disparos(GameObject raiz) => raiz.GetComponentsInChildren<PistolaPandillero>(true).Sum(p => p.Disparos);

        private readonly List<string> violaciones = new List<string>();
        private readonly RaycastHit[] golpes = new RaycastHit[16];

        // Independiente de la IA: cada vez que un tirador dispara, entre su boca y el pecho del jugador no debe haber nada sólido.
        private IEnumerator Vigilar(float segundos, params GameObject[] tiradores)
        {
            Dictionary<PistolaPandillero, int> cuenta = new Dictionary<PistolaPandillero, int>();
            float fin = Time.time + segundos;
            while (Time.time < fin)
            {
                Transform jugador = Registro<IJugador>.Actual.Raiz;
                Vector3 pecho = jugador.position + Vector3.up * AlturaPecho;
                foreach (GameObject t in tiradores)
                {
                    if (t == null)
                    {
                        continue;
                    }
                    foreach (PistolaPandillero p in t.GetComponentsInChildren<PistolaPandillero>())
                    {
                        cuenta.TryGetValue(p, out int antes);
                        cuenta[p] = p.Disparos;
                        if (p.Disparos <= antes)
                        {
                            continue;
                        }
                        Vector3 boca = p.transform.position + Vector3.up * 1.4f + p.transform.forward * 0.4f;
                        Vector3 hacia = pecho - boca;
                        int n = Physics.RaycastNonAlloc(boca, hacia.normalized, golpes, hacia.magnitude, ~0, QueryTriggerInteraction.Ignore);
                        for (int i = 0; i < n; i++)
                        {
                            Transform c = golpes[i].collider.transform;
                            if (c.IsChildOf(jugador) || c.IsChildOf(p.transform.root) || c.GetComponentInParent<PandilleroAPie>() != null)
                            {
                                continue;
                            }
                            violaciones.Add(p.transform.root.name + " disparó con " + golpes[i].collider.name + " en medio (a " + golpes[i].distance.ToString("F1") + " m)");
                            break;
                        }
                    }
                }
                yield return null;
            }
        }

        // ¿Hay algo sólido entre la boca (a 1.4 m) del tirador y el pecho del jugador ahora?
        private static bool HayAlgoEnMedio(Vector3 tirador, Vector3 jugador)
        {
            Vector3 boca = tirador + Vector3.up * 1.4f;
            Vector3 pecho = jugador + Vector3.up * AlturaPecho;
            RaycastHit[] hits = Physics.RaycastAll(boca, (pecho - boca).normalized, (pecho - boca).magnitude, ~0, QueryTriggerInteraction.Ignore);
            Transform raizJugador = Registro<IJugador>.Actual.Raiz;
            return hits.Any(h => !h.collider.transform.IsChildOf(raizJugador) && h.collider.GetComponentInParent<PandilleroAPie>() == null);
        }

        // Un punto a 5-6 m del tirador, sobre su mismo suelo, sin nada sólido encima ni entre los dos.
        private static Vector3 ElegirLibre(Vector3 tirador)
        {
            for (float distancia = 6f; distancia >= 4f; distancia -= 1f)
            {
                for (int grados = 0; grados < 360; grados += 30)
                {
                    Vector3 punto = ConSuelo(tirador + Quaternion.Euler(0f, grados, 0f) * Vector3.forward * distancia);
                    if (Mathf.Abs(punto.y - tirador.y) > 0.3f || Physics.CheckCapsule(punto + Vector3.up * 0.7f, punto + Vector3.up * 1.4f, 0.35f, ~0, QueryTriggerInteraction.Ignore))
                    {
                        continue;
                    }
                    if (!HayAlgoEnMedio(tirador, punto))
                    {
                        return punto;
                    }
                }
            }
            return tirador + Vector3.forward * 5f;
        }

        // Prueba a los tiradores: con el obstáculo en medio no disparan ni dañan (el dron, quieto, ni avisa); ya con la línea libre sí dañan.
        private IEnumerator ProbarTiradores(Vector3 tirador, Vector3 jugadorTapado, Vector3 jugadorLibre, string que, string captura, Transform obstaculo = null)
        {
            Physics.SyncTransforms();
            IVida vida = VidaJugador;
            if (jugadorLibre == default) { jugadorLibre = ElegirLibre(tirador); }
            Assert.IsNotNull(vida, "El jugador real tiene vida");
            violaciones.Clear();

            // 1) SKULL
            yield return PonerJugador(jugadorTapado, tirador);
            yield return null;
            Physics.SyncTransforms();
            Debug.Log("[T-711] jugador en " + Registro<IJugador>.Actual.Raiz.position + " tirador " + tirador);
            Assert.IsTrue(HayAlgoEnMedio(tirador, Registro<IJugador>.Actual.Raiz.position), que + ": el obstáculo real está entre el tirador y el jugador");
            float maxima = Reserva(vida);
            GameObject grupo = CrearSkull(tirador, jugadorTapado);
            yield return Esperar(0.4f);
            if (!string.IsNullOrEmpty(captura) && Environment.GetEnvironmentVariable("CAPTURAR_S1") == "1")
            {
                yield return Capturar(tirador, jugadorTapado, captura);
            }
            yield return Vigilar(2.5f, grupo);
            Assert.IsEmpty(violaciones, que + ": el SKULL disparó a través del obstáculo: " + string.Join(" | ", violaciones));
            if (Disparos(grupo) == 0) { Assert.GreaterOrEqual(Reserva(vida), maxima - 0.001f, que + ": el SKULL no daña a través del obstáculo"); } // si rodeó el obstáculo y disparó, Vigilar ya comprobó su línea
            // Para que sea una prueba firme (el SKULL se mueve y se cubre), en el tramo de línea libre el obstáculo se quita: nada le estorba.
            Collider[] quitados = obstaculo != null ? Solidos(obstaculo).ToArray() : new Collider[0];
            foreach (Collider c in quitados) { c.enabled = false; }
            yield return PonerJugador(jugadorLibre, tirador);
            float fin = Time.time + 10f;
            while (Time.time < fin && Reserva(vida) >= maxima)
            {
                yield return null;
            }
            Assert.Greater(Disparos(grupo), 0, que + ": con línea libre el SKULL sí dispara");
            Assert.Less(Reserva(vida), maxima, que + ": con línea libre el SKULL sí daña");
            foreach (Collider c in quitados) { c.enabled = true; }
            Object.Destroy(grupo);
            yield return null;

            // 2) Policía
            yield return PonerJugador(jugadorTapado, tirador);
            GameObject policia = CrearPolicia(tirador, jugadorTapado);
            float vidaTapado = Reserva(vida);
            yield return Vigilar(2.5f, policia);
            Assert.IsEmpty(violaciones, que + ": el policía disparó a través del obstáculo: " + string.Join(" | ", violaciones));
            if (Disparos(policia) == 0) { Assert.GreaterOrEqual(Reserva(vida), vidaTapado - 0.001f, que + ": el policía no daña a través del obstáculo"); }
            Object.Destroy(policia);
            yield return null;

            // 3) Dron (quieto)
            yield return PonerJugador(jugadorTapado, tirador);
            Physics.SyncTransforms();
            DronMundo dron = CrearDron(tirador + Vector3.up * 0.2f);
            float vidaDron = Reserva(vida);
            yield return Esperar(3f);
            Assert.AreEqual(0, dron.Disparos, que + ": el dron no dispara a través del obstáculo");
            Assert.GreaterOrEqual(Reserva(vida), vidaDron - 0.001f, que + ": el dron no daña a través del obstáculo");
            yield return PonerJugador(jugadorLibre, tirador);
            fin = Time.time + 6f;
            while (Time.time < fin && dron.Aciertos == 0)
            {
                yield return null;
            }
            Assert.Greater(dron.Disparos, 0, que + ": con línea libre el dron sí dispara");
        }

        private IEnumerator Capturar(Vector3 tirador, Vector3 jugador, string archivo)
        {
            GameObject objeto = new GameObject("CamaraCaptura");
            extras.Add(objeto);
            Camera camara = objeto.AddComponent<Camera>();
            camara.farClipPlane = 300f;
            Vector3 medio = (tirador + jugador) * 0.5f;
            Vector3 eje = (jugador - tirador).normalized;
            Vector3 lado = Vector3.Cross(Vector3.up, eje);
            // La primera posición de cámara que no quede dentro de un edificio: al costado, al otro costado, detrás del tirador o delante.
            Vector3[] opciones = { medio + lado * 11f + Vector3.up * 5f, medio - lado * 11f + Vector3.up * 5f, medio - eje * 9f + Vector3.up * 9f, medio + eje * 9f + Vector3.up * 9f };
            objeto.transform.position = opciones.FirstOrDefault(o => !Physics.CheckSphere(o, 0.5f, ~0, QueryTriggerInteraction.Ignore));
            objeto.transform.LookAt(medio + Vector3.up * 1.5f);
            yield return null;
            RenderTexture rt = new RenderTexture(1280, 720, 24);
            camara.targetTexture = rt;
            camara.Render();
            RenderTexture.active = rt;
            Texture2D foto = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            foto.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            foto.Apply();
            camara.targetTexture = null;
            RenderTexture.active = null;
            File.WriteAllBytes(Path.Combine(Application.dataPath, "..", "docs", "capturas", archivo), foto.EncodeToPNG());
            Object.Destroy(foto);
            Object.Destroy(rt);
        }

        [UnityTest]
        public IEnumerator EnElCallejonDeLosSkulls_LaParedDeCasasNuevasParaLasBalas()
        {
            yield return CargarCiudad();
            Transform volumen = Buscar("T502_Callejon/VolumenCallejon");
            Assert.IsNotNull(volumen, "El volumen del callejón existe en la escena");
            Bounds callejon = volumen.GetComponent<Collider>().bounds;
            // La pared norte del callejón: el primer sólido que se encuentra yendo hacia el norte desde su centro.
            Vector3 desde = new Vector3(callejon.center.x, AlturaPecho, callejon.center.z);
            Physics.SyncTransforms();
            Assert.IsTrue(Physics.Raycast(desde, Vector3.forward, out RaycastHit pared, 30f, ~0, QueryTriggerInteraction.Ignore), "Hay pared al norte del callejón");
            Vector3 tirador = new Vector3(desde.x, 0.2f, pared.point.z - 3f);
            // El jugador al otro lado de las casas: del frente de las casas hacia la calle.
            Assert.IsTrue(Physics.Raycast(new Vector3(desde.x, AlturaPecho, pared.point.z + 9f), Vector3.back, out RaycastHit frente, 20f, ~0, QueryTriggerInteraction.Ignore),
                "La pared tiene frente a la calle");
            Assert.Greater(frente.point.z - pared.point.z, 2f, "Las casas tienen cuerpo: la pared tiene grosor");
            Vector3 jugadorTapado = new Vector3(desde.x, 0.2f, frente.point.z + 1.2f);
            Vector3 jugadorLibre = new Vector3(desde.x + 7f, 0.2f, tirador.z);
            Debug.Log("[T-711] Callejón: tirador " + tirador + ", pared norte z=" + pared.point.z + ", frente z=" + frente.point.z + ", jugador tapado " + jugadorTapado + ", libre " + jugadorLibre);
            yield return ProbarTiradores(tirador, jugadorTapado, jugadorLibre, "Callejón con casas nuevas", "s1c-T-711-callejon.png");
        }

        [UnityTest]
        public IEnumerator ConUnCarroDeLaCiudad_EnMedio_LasBalasNoPasan()
        {
            yield return CargarCiudad();
            Physics.SyncTransforms();
            // El carro más grande de los que hay sueltos en la ciudad (los hay del tamaño de una moto).
            Transform carro = Hijos("GTA_T511", 100).Concat(Hijos("GTA_T451", 100)).Where(t => t.name.StartsWith("Carro")).OrderByDescending(t => CajaDeColliders(t).size.sqrMagnitude).FirstOrDefault();
            Assert.IsNotNull(carro, "Hay un carro de la ciudad");
            Bounds caja = CajaDeColliders(carro);
            Assert.Greater(caja.size.magnitude, 2f);
            bool largoEnX = caja.size.x >= caja.size.z;
            // Por el largo del carro, con el tirador y el jugador sobre el mismo suelo: la línea cruza la cabina y la carrocería.
            Vector3 eje = largoEnX ? Vector3.right : Vector3.forward;
            Vector3 lado = largoEnX ? Vector3.forward : Vector3.right;
            float medio = largoEnX ? caja.extents.x : caja.extents.z;
            Vector3 centro = new Vector3(caja.center.x, 0f, caja.center.z);
            Vector3 tirador = ConSuelo(centro - eje * (medio + 2.5f));
            Vector3 jugadorTapado = ConSuelo(centro + eje * (medio + 2f));
            Vector3 jugadorLibre = default; // se elige un punto despejado a su alrededor
            Debug.Log("[T-711] Carro " + carro.name + " en " + caja.center + " largo " + (largoEnX ? "X" : "Z") + ": tirador " + tirador + ", jugador " + jugadorTapado);
            yield return ProbarTiradores(tirador, jugadorTapado, jugadorLibre, "Carro " + carro.name, "s1c-T-711-carro.png", carro);
        }
    }
}
