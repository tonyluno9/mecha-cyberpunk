using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.IA.Datos;
using MechaCyberpunk.Personaje.Vida;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests
{
    /// <summary>
    /// T-1108: el jefe SNAKES (E11) en un piso de prueba con el jugador real. Los tajos le bajan 30 al jugador quieto, la estocada conecta 22 desde
    /// 6.5 m si no se mueve y no conecta si se aparta de lado durante el aviso (con A/D simuladas), llama refuerzos una vez al 50% y al vencerlo
    /// registra el jefe vencido de Viboras. Con T1108_CAPTURAS=1 escribe docs/capturas/t-1108/.
    /// </summary>
    public class JefeSnakesT1108Pruebas : InputTestFixture
    {
        private const string RutaJugador = "Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab";
        private const string RutaJefe = "Assets/_Proyecto/IA/Prefabs/JefeSnakes_E11.prefab";
        private const string RutaDatosJefe = "Assets/_Proyecto/IA/Datos/DatosPandilleroAPie_JefeSnakesE11.asset";
        private const string RutaSnakes = "Assets/_Proyecto/IA/Prefabs/PandilleroArmado_Snakes.prefab";
        private const string RutaViboras = "Assets/_Proyecto/IA/Datos/DatosPandilleroAPie_Viboras.asset";
        private const string CarpetaFotos = "docs/capturas/t-1108";

        private sealed class TerritorioFalso : ITerritorio
        {
            public readonly List<(Vector3 posicion, IdPandilla pandilla, bool jefe)> Vencidos = new List<(Vector3, IdPandilla, bool)>();
            public IReadOnlyList<string> Zonas => new string[0];
            public string ZonaEn(Vector3 posicion) => "neon";
            public string Nombre(string zona) => zona;
            public IdPandilla Dueno(string zona) => IdPandilla.Viboras;
            public int Respeto(string zona) => 0;
            public bool JefeDisponible(string zona) => false;
            public void RegistrarVencido(Vector3 posicion, IdPandilla pandilla, bool jefe) => Vencidos.Add((posicion, pandilla, jefe));
            public void RegistrarCaida(Vector3 posicion) { }
            public event Action<string> Cambio { add { } remove { } }
            public event Action<string, IdPandilla> Conquistada { add { } remove { } }
        }

        private Keyboard teclado;
        private GameObject piso;
        private GameObject jugador;
        private ConductaJefeSnakes conducta;
        private PandilleroAPie jefe;
        private readonly List<float> golpes = new List<float>();
        private readonly List<GameObject> extras = new List<GameObject>();

        public override void Setup()
        {
            base.Setup();
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
            EventosPelea.LimpiarSuscriptores();
            teclado = InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
            piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(400f, 1f, 400f);
            jugador = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaJugador), new Vector3(0f, 0.05f, 0f), Quaternion.identity);
            jugador.GetComponent<EscudoJugador>()?.Iniciar(0f, 0f, 0f);
            golpes.Clear();
            EventosPelea.ImpactoRecibido += (objetivo, info, resultado) =>
            {
                if (objetivo == null && info.Tipo == TipoDanio.Golpe && resultado.DanioAplicado > 0f) golpes.Add(resultado.DanioAplicado);
            };
        }

        public override void TearDown()
        {
            Time.timeScale = 1f;
            if (conducta != null)
            {
                Object.DestroyImmediate(conducta.transform.parent.gameObject);
            }
            foreach (GameObject e in extras) Object.DestroyImmediate(e);
            Object.DestroyImmediate(jugador);
            Object.DestroyImmediate(piso);
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
            EventosPelea.LimpiarSuscriptores();
            base.TearDown();
        }

        // azar: 0.9 hace que planee la estocada primero; 0.1, los tajos.
        private void CrearJefe(Vector3 posicion, float azar, bool conEscoltas = false, bool conRefuerzos = false)
        {
            conducta = ConstructorJefeSnakes.Crear(null, "Puesto_Prueba", posicion, jugador.transform.position - posicion,
                AssetDatabase.LoadAssetAtPath<GameObject>(RutaJefe), AssetDatabase.LoadAssetAtPath<DatosPandilleroAPie>(RutaDatosJefe),
                AssetDatabase.LoadAssetAtPath<GameObject>(RutaSnakes), AssetDatabase.LoadAssetAtPath<DatosPandilleroAPie>(RutaViboras), conRefuerzos, conEscoltas);
            conducta.InyectarAzar(() => azar);
            jefe = conducta.GetComponent<PandilleroAPie>();
            Physics.SyncTransforms();
        }

        private static IEnumerator Esperar(float s)
        {
            float fin = Time.time + s;
            while (Time.time < fin) yield return null;
        }

        private IEnumerator HastaFase(ReglaJefeSnakes.Fase fase, float maxSegundos)
        {
            float fin = Time.time + maxSegundos;
            while (conducta.Regla.Actual != fase && Time.time < fin) yield return null;
            Assert.AreEqual(fase, conducta.Regla.Actual, "el jefe llega a la fase " + fase);
        }

        private float Suma()
        {
            float total = 0f;
            foreach (float g in golpes) total += g;
            return total;
        }

        [UnityTest]
        public IEnumerator Datos_VidaDoscientosSesentaYVelocidadCuatroPuntoDos()
        {
            CrearJefe(new Vector3(0f, 0.05f, 12f), 0.1f);
            yield return null;
            Assert.AreEqual(260f, jefe.Maxima, 0.001f);
            Assert.AreEqual(4.2f, jefe.Datos.Velocidad, 0.001f);
            Assert.IsTrue(jefe.EsJefe);
        }

        [UnityTest]
        public IEnumerator Tajos_ElJugadorQuietoA2m_PierdeTreintaEnTresSegundos()
        {
            CrearJefe(new Vector3(0f, 0.05f, 2f), 0.1f);
            float inicio = Time.time;
            while (Suma() < 30f && Time.time - inicio < 3f) yield return null;
            Assert.AreEqual(30f, Suma(), 0.01f, "tres tajos de 10 en tres segundos o menos");
            Assert.AreEqual(3, golpes.Count, "tres golpes");
            Assert.LessOrEqual(Time.time - inicio, 3f);
            CollectionAssert.AreEqual(new[] { 10f, 10f, 10f }, golpes);
        }

        [UnityTest]
        public IEnumerator Estocada_ElJugadorQuietoA6m_Conecta22()
        {
            CrearJefe(new Vector3(0f, 0.05f, 6.5f), 0.9f);
            yield return HastaFase(ReglaJefeSnakes.Fase.AvisoEstocada, 5f);
            Assert.AreEqual(0f, Suma(), "antes de embestir no hay daño");
            yield return HastaFase(ReglaJefeSnakes.Fase.RecuperaEstocada, 3f);
            Assert.AreEqual(22f, Suma(), 0.01f, "la estocada conecta 22 una sola vez");
            Assert.IsTrue(jefe.Castigable, "recuperándose queda castigable");
            float antes = jefe.Actual;
            jefe.RecibirDanio(new InfoDanio(10f, TipoDanio.Golpe, jefe.transform.position + Vector3.up, Vector3.forward, null));
            Assert.AreEqual(15f, antes - jefe.Actual, 0.01f, "castigable: recibe x1.5");
        }

        [UnityTest]
        public IEnumerator Estocada_SiElJugadorSeApartaTresMetrosDeLadoEnElAviso_NoConecta()
        {
            CrearJefe(new Vector3(0f, 0.05f, 6.5f), 0.9f);
            yield return HastaFase(ReglaJefeSnakes.Fase.AvisoEstocada, 5f);
            float x0 = jugador.transform.position.x;
            Press(teclado.dKey);
            yield return HastaFase(ReglaJefeSnakes.Fase.RecuperaEstocada, 3f);
            Release(teclado.dKey);
            Assert.GreaterOrEqual(Mathf.Abs(jugador.transform.position.x - x0), 2f, "el jugador se apartó de lado");
            Assert.AreEqual(0f, Suma(), "la estocada pasó de largo");
            Assert.AreEqual(0, golpes.Count);
        }

        [UnityTest]
        public IEnumerator Refuerzos_AlCincuentaPorCiento_SeLlamanUnaSolaVez()
        {
            CrearJefe(new Vector3(0f, 0.05f, 14f), 0.1f, conEscoltas: true, conRefuerzos: true);
            yield return null;
            PandillaAPie refuerzos = conducta.Refuerzos;
            Assert.IsNotNull(refuerzos, "el jefe tiene su grupo de refuerzos");
            Assert.IsFalse(refuerzos.gameObject.activeSelf, "los refuerzos esperan apagados");
            PandillaAPie escoltas = conducta.transform.parent.Find("Escoltas").GetComponent<PandillaAPie>();
            Assert.AreEqual(2, escoltas.Presentes, "2 escoltas SNAKES desde el principio");
            jefe.RecibirDanio(new InfoDanio(100f, TipoDanio.Golpe, jefe.transform.position + Vector3.up, Vector3.forward, null)); // 160 de 260: 61%
            yield return Esperar(0.3f);
            Assert.IsFalse(refuerzos.gameObject.activeSelf, "por encima del 50% no llama refuerzos");
            jefe.RecibirDanio(new InfoDanio(40f, TipoDanio.Golpe, jefe.transform.position + Vector3.up, Vector3.forward, null)); // 120: 46%
            yield return Esperar(0.3f);
            Assert.IsTrue(conducta.Regla.RefuerzosLlamados);
            Assert.IsTrue(refuerzos.gameObject.activeSelf, "al 50% se activan los refuerzos");
            Assert.AreEqual(2, refuerzos.Presentes, "2 SNAKES de refuerzo");
            Assert.AreEqual("¡A mí, víboras!", jefe.GritoActual, "el jefe grita al llamarlos");
            // Vence a uno de los refuerzos y sigue bajando la vida del jefe: no se vuelven a llamar.
            PandilleroAPie uno = refuerzos.Pandilleros[0];
            uno.RecibirDanio(new InfoDanio(1000f, TipoDanio.Golpe, uno.transform.position + Vector3.up, Vector3.forward, null));
            jefe.RecibirDanio(new InfoDanio(20f, TipoDanio.Golpe, jefe.transform.position + Vector3.up, Vector3.forward, null));
            yield return Esperar(1f);
            Assert.AreEqual(1, refuerzos.Presentes, "el refuerzo vencido no vuelve: se llamaron una sola vez");
        }

        [UnityTest]
        public IEnumerator Vencerlo_RegistraElJefeVencidoDeViboras()
        {
            TerritorioFalso territorio = new TerritorioFalso();
            Registro<ITerritorio>.Agregar(territorio);
            CrearJefe(new Vector3(0f, 0.05f, 15f), 0.1f);
            yield return null;
            jefe.RecibirDanio(new InfoDanio(1000f, TipoDanio.Golpe, jefe.transform.position + Vector3.up, Vector3.forward, null));
            yield return Esperar(0.5f);
            Assert.IsTrue(jefe.EstaKO);
            Assert.AreEqual(1, territorio.Vencidos.Count, "se registra una sola vez");
            Assert.AreEqual(IdPandilla.Viboras, territorio.Vencidos[0].pandilla);
            Assert.IsTrue(territorio.Vencidos[0].jefe, "como jefe vencido");
        }

        // ---------------------------------------------------------------- capturas

        [UnityTest]
        public IEnumerator Capturas_JefeEnAvisoYEnEstocada()
        {
            if (Environment.GetEnvironmentVariable("T1108_CAPTURAS") != "1")
            {
                Assert.Ignore("Solo escribe docs/capturas/t-1108/ con T1108_CAPTURAS=1");
            }
            Directory.CreateDirectory(CarpetaFotos);
            CrearJefe(new Vector3(0f, 0.05f, 6.5f), 0.9f);
            // Aviso de la estocada con el brillo verde, casi al final del aviso.
            float fin = Time.time + 6f;
            while ((conducta.Regla.Actual != ReglaJefeSnakes.Fase.AvisoEstocada || conducta.Regla.ProgresoAviso < 0.8f) && Time.time < fin) yield return null;
            yield return Foto(CarpetaFotos + "/jefe-aviso.png");
            while (conducta.Regla.Actual != ReglaJefeSnakes.Fase.Estocada && Time.time < fin + 4f) yield return null;
            yield return Foto(CarpetaFotos + "/jefe-estocada.png");
        }

        private IEnumerator Foto(string ruta)
        {
            GameObject go = new GameObject("CamaraFoto");
            extras.Add(go);
            Camera camara = go.AddComponent<Camera>();
            camara.farClipPlane = 200f;
            Vector3 centro = (jefe.transform.position + jugador.transform.position) * 0.5f;
            go.transform.position = centro + Vector3.right * 6f + Vector3.up * 1.8f;
            go.transform.LookAt(jefe.transform.position + Vector3.up * 1.1f);
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
            File.WriteAllBytes(ruta, foto.EncodeToPNG());
            Object.Destroy(foto);
            Object.Destroy(rt);
        }
    }
}
