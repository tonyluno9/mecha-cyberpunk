using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.IA.Datos;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.IA
{
    /// <summary>T-423: los pandilleros se cubren, huyen con poca vida y piden refuerzos.</summary>
    public class TacticaPandillerosPruebas
    {
        private const string RutaPandillero = "Assets/_Proyecto/IA/Prefabs/PandilleroArmado.prefab";
        private const string RutaDatos = "Assets/_Proyecto/IA/Datos/DatosPandilleroAPie_Calaveras.asset";
        private const string RutaJugador = "Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab";
        private GameObject piso;
        private GameObject jugador;
        private GameObject grupo;
        private PandillaAPie pandilla;
        private System.Func<float> azarAnterior;

        [SetUp]
        public void Preparar()
        {
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
            EventosPelea.LimpiarSuscriptores();
            azarAnterior = PistolaPandillero.Azar;
            PistolaPandillero.Azar = () => 1f; // nunca acierta: la prueba no depende de la vida del jugador
            piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(400f, 1f, 400f);
            jugador = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaJugador), new Vector3(0f, 0.05f, 0f), Quaternion.identity);
            grupo = new GameObject("GrupoT423");
            grupo.SetActive(false);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPandillero);
            DatosPandilleroAPie datos = AssetDatabase.LoadAssetAtPath<DatosPandilleroAPie>(RutaDatos);
            for (int i = 0; i < 3; i++)
            {
                GameObject p = Object.Instantiate(prefab, grupo.transform);
                p.transform.position = new Vector3(i * 2f - 2f, 0.05f, 22f);
                SerializedObject soP = new SerializedObject(p.GetComponent<PandilleroAPie>());
                soP.FindProperty("datos").objectReferenceValue = datos;
                soP.ApplyModifiedPropertiesWithoutUndo();
            }
            pandilla = grupo.AddComponent<PandillaAPie>();
            SerializedObject so = new SerializedObject(pandilla);
            so.FindProperty("datos").objectReferenceValue = datos;
            so.FindProperty("idPandilla").enumValueIndex = (int)IdPandilla.Calaveras;
            so.ApplyModifiedPropertiesWithoutUndo();
            grupo.SetActive(true);
        }

        [TearDown]
        public void Limpiar()
        {
            PistolaPandillero.Azar = azarAnterior;
            Object.Destroy(grupo);
            Object.Destroy(jugador);
            Object.Destroy(piso);
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
            EventosPelea.LimpiarSuscriptores();
        }

        private static IEnumerator Esperar(float s)
        {
            float fin = Time.time + s;
            while (Time.time < fin)
            {
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator PocaVida_HuyeYElDanioAvisaAlGrupo()
        {
            yield return Esperar(0.5f);
            Registro<IJugador>.Actual.Colocar(new Pose(new Vector3(0f, 0.05f, 10f), Quaternion.identity));
            yield return Esperar(0.5f);
            PandilleroAPie herido = pandilla.Pandilleros[0];
            herido.RecibirDanio(new InfoDanio(herido.Maxima * 0.85f, TipoDanio.Disparo, herido.transform.position, Vector3.forward, null));
            Assert.IsNotNull(herido.GritoActual, "Grita para pedir refuerzos.");
            Assert.IsTrue(pandilla.EnAlerta, "Recibir daño pone al grupo en alerta.");
            yield return Esperar(1f);
            Assert.AreEqual(ReglaTacticaPandillero.Modo.Huir, herido.ModoTactico);
            float antes = Vector3.Distance(herido.transform.position, jugador.transform.position);
            yield return Esperar(1.5f);
            Assert.Greater(Vector3.Distance(herido.transform.position, jugador.transform.position), antes, "Corre lejos del jugador.");
        }

        [UnityTest]
        public IEnumerator DespuesDeDisparar_BuscaCubiertaCercana()
        {
            GameObject caja = GameObject.CreatePrimitive(PrimitiveType.Cube);
            caja.transform.position = new Vector3(4f, 1f, 16f);
            caja.transform.localScale = new Vector3(3f, 2f, 1f);
            yield return Esperar(0.5f);
            Registro<IJugador>.Actual.Colocar(new Pose(new Vector3(0f, 0.05f, 10f), Quaternion.identity));
            bool seCubrio = false;
            float fin = Time.time + 12f;
            while (Time.time < fin && !seCubrio)
            {
                foreach (PandilleroAPie p in pandilla.Pandilleros)
                {
                    seCubrio |= p.ModoTactico == ReglaTacticaPandillero.Modo.Cubrirse && p.TieneCubierta;
                }
                yield return null;
            }
            if (!Application.isBatchMode)
            {
                // Captura de revisión (T-423): vista cenital de los pandilleros repartidos y cubiertos.
                GameObject objetoCamara = new GameObject("CamaraCaptura");
                Camera camara = objetoCamara.AddComponent<Camera>();
                objetoCamara.transform.position = new Vector3(-8f, 9f, 2f);
                objetoCamara.transform.LookAt(new Vector3(2f, 1f, 16f));
                yield return new WaitForEndOfFrame();
                Texture2D foto = ScreenCapture.CaptureScreenshotAsTexture();
                System.IO.File.WriteAllBytes(System.IO.Path.Combine("docs", "capturas", "T-423-1.png"), foto.EncodeToPNG());
                Object.Destroy(foto);
                Object.Destroy(objetoCamara);
            }
            Object.Destroy(caja);
            Assert.IsTrue(seCubrio, "Tras disparar, un pandillero se cubre tras la caja.");
        }
    }
}
