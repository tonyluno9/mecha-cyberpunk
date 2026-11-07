using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Trafico.Persecucion;
using MechaCyberpunk.Vehiculos.Carro;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Vehiculos
{
    /// <summary>
    /// T-692: la explosión de un carro daña todo lo de alrededor con caída por distancia (jugador, NPC, otros carros y patrullas), puede encadenar
    /// si el vecino ya venía dañado, empuja sin mandar nada por los aires, y nada explota sin daño acumulado real (con aviso de humo, fuego y sonido).
    /// Capturas con CAPTURAR_S1=1 en docs/capturas/s1t-T-692-*.png.
    /// </summary>
    public class ExplosionAreaT692Pruebas
    {
        private const string RutaCarro = "Assets/_Proyecto/Vehiculos/Prefabs/CarroViejo.prefab";
        private const string RutaPatrulla = "Assets/_Proyecto/Trafico/Prefabs/Patrulla.prefab";

        private sealed class Victima : MonoBehaviour, IRecibeDanio
        {
            public float Recibido;
            public TipoDanio UltimoTipo;
            public ResultadoDanio RecibirDanio(InfoDanio info)
            {
                Recibido += info.Cantidad;
                UltimoTipo = info.Tipo;
                return new ResultadoDanio(info.Cantidad, TipoBloqueo.Ninguno, false);
            }
        }

        private sealed class SonidosFalsos : ISonidos
        {
            public readonly List<string> Ids = new List<string>();
            public void Reproducir(string id) { Ids.Add(id); }
            public void ReproducirEn(string id, Vector3 posicion) { Ids.Add(id); }
            public int ReproducirBucle(string id, Transform sigue) { return Sonidos.SinBucle; }
            public void DetenerBucle(int bucle) { }
            public void FijarTono(int bucle, float tono) { }
            public float Volumen(SeccionSonido seccion) { return 1f; }
            public void FijarVolumen(SeccionSonido seccion, float volumen) { }
        }

        private GameObject piso;
        private SonidosFalsos sonidos;
        private readonly List<GameObject> extras = new List<GameObject>();

        [SetUp]
        public void Preparar()
        {
            EventosMontaje.LimpiarSuscriptores();
            sonidos = new SonidosFalsos();
            Registro<ISonidos>.Agregar(sonidos);
            piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(300f, 1f, 300f);
        }

        [TearDown]
        public void Limpiar()
        {
            Registro<ISonidos>.Quitar(sonidos);
            foreach (GameObject extra in extras)
            {
                if (extra != null) Object.DestroyImmediate(extra);
            }
            extras.Clear();
            foreach (ExplosionCarro e in Object.FindObjectsByType<ExplosionCarro>(FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(e.gameObject);
            }
            Object.DestroyImmediate(piso);
            EventosMontaje.LimpiarSuscriptores();
        }

        private static IEnumerator Esperar(float segundos)
        {
            float fin = Time.time + segundos;
            while (Time.time < fin) yield return null;
        }

        private DanioCarro Carro(Vector3 posicion)
        {
            GameObject go = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaCarro), posicion, Quaternion.identity);
            extras.Add(go);
            return go.GetComponent<DanioCarro>();
        }

        private DanioCarro Patrulla(Vector3 posicion)
        {
            GameObject go = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPatrulla), posicion, Quaternion.identity);
            go.GetComponent<PatrullaMundo>().enabled = false; // sin gestor no tiene a dónde ir
            extras.Add(go);
            return go.GetComponent<DanioCarro>();
        }

        private Victima Persona(Vector3 posicion)
        {
            GameObject go = new GameObject("Persona");
            go.transform.position = posicion;
            go.AddComponent<CapsuleCollider>().height = 1.8f;
            extras.Add(go);
            return go.AddComponent<Victima>();
        }

        private static void Reventar(DanioCarro carro)
        {
            carro.RecibirDanio(new InfoDanio(carro.VidaMaxima * 2f, TipoDanio.Disparo, carro.transform.position, Vector3.forward, null));
        }

        [UnityTest]
        public IEnumerator Explosion_DaniaJugadorYNpcConCaidaPorDistancia_YNoAlLejano()
        {
            DanioCarro carro = Carro(new Vector3(0f, 0.3f, 0f));
            Victima cerca = Persona(new Vector3(2.5f, 1f, 0f));
            Victima media = Persona(new Vector3(0f, 1f, 5f));
            Victima lejos = Persona(new Vector3(40f, 1f, 0f));
            yield return Esperar(0.3f);
            Reventar(carro);
            yield return null;
            Assert.Greater(cerca.Recibido, 0f, "el de cerca recibe daño");
            Assert.AreEqual(TipoDanio.Explosion, cerca.UltimoTipo);
            Assert.Greater(media.Recibido, 0f, "el del radio medio también");
            Assert.Greater(cerca.Recibido, media.Recibido, "el daño cae con la distancia");
            Assert.AreEqual(0f, lejos.Recibido, "el lejano ni se entera");
        }

        [UnityTest]
        public IEnumerator Explosion_UnCarroSanoAlLado_NoQuedaIntactoNiExplotaNiSaleVolando()
        {
            DanioCarro a = Carro(new Vector3(0f, 0.3f, 0f));
            DanioCarro b = Carro(new Vector3(4.5f, 0.3f, 0f));
            yield return Esperar(1.8f);
            Rigidbody rb = b.GetComponent<Rigidbody>();
            float vidaAntes = b.Vida;
            Reventar(a);
            float maxY = 0f;
            float maxRapidez = 0f;
            for (float t = 0f; t < 4f; t += Time.deltaTime)
            {
                yield return null;
                maxY = Mathf.Max(maxY, b.transform.position.y);
                maxRapidez = Mathf.Max(maxRapidez, rb.linearVelocity.magnitude);
            }
            Assert.Less(b.Vida, vidaAntes, "el carro de al lado recibe daño de la explosión");
            Assert.IsFalse(b.Explotado, "pero un carro sano no explota por el empujón");
            Assert.Less(maxY, 3f, "no sale volando (altura máxima " + maxY + ")");
            Assert.Less(maxRapidez, 14f, "ni a velocidades absurdas (máx " + maxRapidez + " m/s)");
            Assert.Less(Vector3.Distance(b.transform.position, new Vector3(4.5f, 0.3f, 0f)), 15f, "se movió poco");
        }

        [UnityTest]
        public IEnumerator Explosion_UnCarroYaDaniado_EncadenaYExplota()
        {
            DanioCarro a = Carro(new Vector3(0f, 0.3f, 0f));
            DanioCarro b = Carro(new Vector3(4f, 0.3f, 0f));
            yield return Esperar(0.4f);
            b.RecibirDanio(new InfoDanio(b.VidaMaxima - 30f, TipoDanio.Disparo, b.transform.position, Vector3.forward, null));
            Assert.AreEqual(EstadoCarro.Fuego, b.Estado, "el vecino ya venía en llamas");
            Assert.IsFalse(b.Explotado);
            Reventar(a);
            yield return null;
            Assert.IsTrue(b.Explotado, "la explosión del primero revienta al que ya venía dañado (reacción en cadena)");
        }

        [UnityTest]
        public IEnumerator Explosion_UnaPatrullaAlLado_RecibeDanio_YPuedeEncadenar()
        {
            DanioCarro a = Carro(new Vector3(0f, 0.3f, 0f));
            DanioCarro sana = Patrulla(new Vector3(4.5f, 0f, 0f));
            DanioCarro dañada = Patrulla(new Vector3(-4f, 0f, 0f));
            yield return Esperar(0.4f);
            Assert.IsNotNull(sana, "la patrulla trae DanioCarro");
            dañada.RecibirDanio(new InfoDanio(dañada.VidaMaxima - 30f, TipoDanio.Disparo, dañada.transform.position, Vector3.forward, null));
            float vida = sana.Vida;
            Reventar(a);
            yield return null;
            Assert.Less(sana.Vida, vida, "la patrulla de al lado no queda intacta");
            Assert.IsFalse(sana.Explotado, "una patrulla sana sobrevive a una explosión");
            Assert.IsTrue(dañada.Explotado, "la que ya venía dañada se encadena");
            Assert.IsTrue(dañada.GetComponent<PatrullaMundo>().Destruida, "la patrulla destruida deja de manejar");
        }

        [UnityTest]
        public IEnumerator Choque_ConRapidezImposible_NoExplotaNadaSinRazon()
        {
            DanioCarro carro = Carro(new Vector3(0f, 0.3f, 0f));
            yield return Esperar(1.8f);
            GameObject bala = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            extras.Add(bala);
            bala.transform.position = new Vector3(-6f, 1f, 0f);
            Rigidbody rb = bala.AddComponent<Rigidbody>();
            rb.mass = 50f;
            rb.useGravity = false;
            rb.linearVelocity = new Vector3(120f, 0f, 0f);
            yield return Esperar(1f);
            Assert.AreEqual(carro.VidaMaxima, carro.Vida, 0.01f, "un impacto de 120 m/s es un glitch: no cuenta como choque");
            Assert.IsFalse(carro.Explotado);
        }

        [UnityTest]
        public IEnumerator Fuego_AvisaConHumoFuegoYSonido_YSoloExplotaAlLlegarAVidaCero()
        {
            DanioCarro carro = Carro(new Vector3(0f, 0.3f, 0f));
            yield return Esperar(0.3f);
            carro.RecibirDanio(new InfoDanio(carro.VidaMaxima * 0.5f, TipoDanio.Disparo, carro.transform.position, Vector3.forward, null));
            Assert.AreEqual(EstadoCarro.Humo, carro.Estado, "primero humo");
            Assert.Greater(carro.Humo.emission.rateOverTime.constant, 0f, "sale humo");
            carro.RecibirDanio(new InfoDanio(carro.VidaMaxima * 0.3f, TipoDanio.Disparo, carro.transform.position, Vector3.forward, null));
            Assert.AreEqual(EstadoCarro.Fuego, carro.Estado, "luego fuego");
            Assert.Greater(carro.Fuego.emission.rateOverTime.constant, 0f, "sale fuego");
            Assert.Contains(DanioCarro.IdAvisoFuego, sonidos.Ids, "suena el aviso");
            Assert.IsFalse(carro.Explotado, "con fuego todavía no explota");
            yield return Esperar(1f);
            Assert.IsFalse(carro.Explotado, "sigue en llamas, avisando, sin explotar de golpe");
            float vida = carro.Vida;
            Assert.Greater(vida, 0f);
            // El fuego lo va consumiendo hasta que la vida llega a 0.
            float fin = Time.time + 20f;
            while (!carro.Explotado && Time.time < fin) yield return null;
            Assert.IsTrue(carro.Explotado, "al consumirse la vida explota");
            Assert.LessOrEqual(carro.Vida, 0.001f);
        }

        [UnityTest]
        public IEnumerator Carro_SanoQuieto_NoExplotaSolo()
        {
            DanioCarro carro = Carro(new Vector3(0f, 0.3f, 0f));
            yield return Esperar(6f);
            Assert.AreEqual(EstadoCarro.Sano, carro.Estado);
            Assert.AreEqual(carro.VidaMaxima, carro.Vida, 0.01f);
        }

        [UnityTest]
        public IEnumerator Captura_ExplosionConVecinos()
        {
            DanioCarro a = Carro(new Vector3(0f, 0.3f, 0f));
            Carro(new Vector3(4.5f, 0.3f, 0f));
            Patrulla(new Vector3(-4.5f, 0f, 0f));
            yield return Esperar(0.4f);
            RenderSettings.ambientLight = new Color(0.12f, 0.13f, 0.2f);
            GameObject camObj = new GameObject("CamCaptura");
            Camera cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.03f, 0.04f, 0.09f);
            cam.transform.position = new Vector3(2f, 4f, -13f);
            cam.transform.LookAt(new Vector3(0f, 1.2f, 0f));
            extras.Add(camObj);
            Reventar(a);
            yield return Esperar(0.35f);
            if (System.Environment.GetEnvironmentVariable("CAPTURAR_S1") == "1")
            {
                CapturaPruebasVehiculos.Tomar(cam, "docs/capturas/s1t-T-692-explosion-vecinos.png");
            }
            Assert.IsTrue(a.Explotado);
        }
    }
}
