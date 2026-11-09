using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MechaCyberpunk.Core;
using MechaCyberpunk.Trafico.Persecucion;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.Trafico
{
    /// <summary>
    /// T-746: una patrulla que persigue gira como un carro: su giro depende de lo que avanza (nunca da vueltas sobre su eje con el jugador cerca)
    /// y siempre va sobre el suelo. Mismo montaje que T-707 (Barrio.prefab con dobles de calor y jugador).
    /// </summary>
    public class PatrullaGiroT746Pruebas
    {
        private const string RutaBarrio = "Assets/_Proyecto/Mundo/Prefabs/Barrio.prefab";
        private const string RutaDron = "Assets/_Proyecto/Trafico/Prefabs/Dron.prefab";
        private const string RutaPatrulla = "Assets/_Proyecto/Trafico/Prefabs/Patrulla.prefab";
        private const float LadoManzana = 80f;
        private const float AlturaCalles = 0.2f;

        private GameObject barrio;
        private GameObject jugadorObjeto;
        private PatrullaFisicaT707Pruebas.CalorDoble calor;
        private GestorPersecucion gestor;
        private GrafoCalles calles;
        private float escalaAnterior;

        [SetUp]
        public void Preparar()
        {
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
            escalaAnterior = Time.timeScale;
            barrio = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaBarrio));
            foreach (GestorPersecucion propio in barrio.GetComponentsInChildren<GestorPersecucion>()) propio.enabled = false;
            List<Vector2> centros = barrio.transform.Cast<Transform>().Where(t => t.name.StartsWith("Manzana_"))
                .Select(t => new Vector2(t.position.x, t.position.z)).ToList();
            calles = GrafoCalles.DeManzanas(centros, LadoManzana, AlturaCalles);
            Vector3 cruce = calles.Nodo(calles.MasCercano(barrio.transform.position));
            jugadorObjeto = new GameObject("JugadorDePrueba");
            jugadorObjeto.transform.position = cruce;
            Registro<IJugador>.Agregar(new PatrullaFisicaT707Pruebas.JugadorDoble { Raiz = jugadorObjeto.transform });
            calor = new PatrullaFisicaT707Pruebas.CalorDoble();
            Registro<ICalor>.Agregar(calor);
            // El Barrio solo no trae piso con collider: se pone uno a la altura de las calles.
            GameObject piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.name = "PisoPrueba";
            piso.GetComponent<MeshRenderer>().enabled = false; // solo collider: no tapa la calle en las capturas
            piso.transform.SetParent(barrio.transform, true);
            piso.transform.position = new Vector3(barrio.transform.position.x, AlturaCalles - 0.5f, barrio.transform.position.z);
            piso.transform.localScale = new Vector3(600f, 1f, 600f);
            GameObject objeto = new GameObject("Persecucion");
            objeto.SetActive(false);
            objeto.transform.SetParent(barrio.transform, false);
            gestor = objeto.AddComponent<GestorPersecucion>();
            gestor.Configurar(ConfigPersecucionPrueba.SinArresto(), AssetDatabase.LoadAssetAtPath<GameObject>(RutaDron).GetComponent<DronMundo>(),
                AssetDatabase.LoadAssetAtPath<GameObject>(RutaPatrulla).GetComponent<PatrullaMundo>());
            objeto.SetActive(true);
        }

        [TearDown]
        public void Limpiar()
        {
            Time.timeScale = escalaAnterior;
            Object.DestroyImmediate(jugadorObjeto);
            Object.DestroyImmediate(barrio);
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
        }

        [UnityTest]
        public IEnumerator PatrullaQuePersigue_NoGiraSobreSuEje_YVaSobreElSuelo()
        {
            calor.Poner(1);
            yield return null;
            yield return null;
            PatrullaMundo patrulla = gestor.GetComponentsInChildren<PatrullaMundo>().FirstOrDefault(p => !p.Robada && !p.EnRonda);
            Assert.IsNotNull(patrulla);
            Time.timeScale = 3f;
            Vector3 posAnt = patrulla.transform.position;
            float yawAnt = patrulla.transform.eulerAngles.y;
            float maxGiro = 0f;
            float maxGiroDetenida = 0f;
            float giroAcumuladoDetenida = 0f;
            int detenidos = 0;
            float maxAltura = float.MinValue;
            float minAltura = float.MaxValue;
            bool fotografiada = false;
            float fin = Time.time + 60f;
            float cuandoFoto = Time.time + 12f;
            while (Time.time < fin)
            {
                yield return null;
                float dt = Time.deltaTime;
                Vector3 pos = patrulla.transform.position;
                float yaw = patrulla.transform.eulerAngles.y;
                float cambio = Mathf.Abs(Mathf.DeltaAngle(yawAnt, yaw));
                float giro = cambio / dt;
                float metros = Vector3.ProjectOnPlane(pos - posAnt, Vector3.up).magnitude;
                maxGiro = Mathf.Max(maxGiro, giro);
                if (metros < 0.002f)
                {
                    detenidos++;
                    giroAcumuladoDetenida += cambio;
                    maxGiroDetenida = Mathf.Max(maxGiroDetenida, giro);
                }
                Physics.SyncTransforms();
                RaycastHit[] bajo = Physics.RaycastAll(pos + Vector3.up * 3f, Vector3.down, 8f, ~0, QueryTriggerInteraction.Ignore).Where(x => !x.collider.transform.IsChildOf(patrulla.transform) && x.point.y <= pos.y + 0.3f).OrderBy(x => x.distance).ToArray();
                if (bajo.Length > 0)
                {
                    float altura = pos.y - bajo[0].point.y;
                    maxAltura = Mathf.Max(maxAltura, altura);
                    minAltura = Mathf.Min(minAltura, altura);
                }
                // Con la patrulla cerca, el jugador da vueltas alrededor de ella para provocar que gire en el lugar.
                Vector3 jug = jugadorObjeto.transform.position;
                if (Vector3.ProjectOnPlane(jug - pos, Vector3.up).magnitude < 30f)
                {
                    float ang = Time.time * 2.5f;
                    jugadorObjeto.transform.position = new Vector3(pos.x, jug.y, pos.z) + new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * 9f;
                }
                if (!fotografiada && Time.time > cuandoFoto)
                {
                    fotografiada = true;
                    Camera cam = new GameObject("CamaraCaptura").AddComponent<Camera>();
                    cam.farClipPlane = 300f;
                    Bounds caja = new Bounds(pos, Vector3.zero);
                    foreach (Renderer rr in patrulla.GetComponentsInChildren<Renderer>()) caja.Encapsulate(rr.bounds);
                    Debug.Log("[T-746] caja de la patrulla " + caja.size + " centro " + caja.center);
                    cam.transform.position = caja.center + patrulla.transform.forward * 9f + patrulla.transform.right * 3f + Vector3.up * 2.5f;
                    cam.transform.LookAt(caja.center);
                    CapturaPrueba.Guardar(cam, "s1e-T-746-patrulla-persiguiendo.png");
                    Object.Destroy(cam.gameObject);
                }
                posAnt = pos;
                yawAnt = yaw;
            }
            Time.timeScale = 1f;
            Debug.Log("[T-746] giro max " + maxGiro + " °/s; detenida: max " + maxGiroDetenida + " °/s, acumulado " + giroAcumuladoDetenida + " ° en " + detenidos + " cuadros; altura " + minAltura + " a " + maxAltura);
            Assert.AreEqual(0f, giroAcumuladoDetenida, 1.5f, "Detenida no gira sobre su eje (grados acumulados con la patrulla parada)");
            Assert.Less(maxGiro, 300f, "El giro está limitado como el de un carro (°/s)");
            Assert.That(minAltura, Is.GreaterThan(-0.05f), "No se hunde en el piso");
            Assert.That(maxAltura, Is.LessThan(0.7f), "No flota sobre el piso: va sobre sus ruedas");
        }
    }
}
