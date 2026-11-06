using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.IA.Datos;
using MechaCyberpunk.Personaje.Vida;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.IA
{
    /// <summary>T-676: los pandilleros armados no disparan sin línea de vista (ni muestran aviso) y su trazo de bala es un hilo finísimo y cortísimo.</summary>
    public class BalasT676Pruebas : InputTestFixture
    {
        private const string RutaJugador = "Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab";
        private const string RutaPandillero = "Assets/_Proyecto/IA/Prefabs/PandilleroArmado.prefab";
        private const string RutaDatos = "Assets/_Proyecto/IA/Datos/DatosPandilleroAPie_Calaveras.asset";

        private GameObject piso;
        private GameObject jugador;
        private GameObject grupo;
        private GameObject muro;
        private System.Func<float> azarAnterior;

        public override void Setup()
        {
            base.Setup();
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
            EventosPelea.LimpiarSuscriptores();
            azarAnterior = PistolaPandillero.Azar;
            PistolaPandillero.Azar = () => 0f;
            piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(400f, 1f, 400f);
            jugador = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaJugador), new Vector3(0f, 0.05f, 0f), Quaternion.identity);
        }

        // DestroyImmediate: el jugador comparte las acciones de entrada; un Destroy diferido las apagaría para la prueba que sigue.
        public override void TearDown()
        {
            PistolaPandillero.Azar = azarAnterior;
            Object.DestroyImmediate(grupo);
            Object.DestroyImmediate(jugador);
            Object.DestroyImmediate(piso);
            if (muro != null)
            {
                Object.DestroyImmediate(muro);
            }
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
            EventosPelea.LimpiarSuscriptores();
            base.TearDown();
        }

        private void CrearGrupo(float distancia)
        {
            grupo = new GameObject("GrupoPrueba");
            grupo.SetActive(false);
            grupo.transform.position = new Vector3(0f, 0f, distancia);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPandillero);
            DatosPandilleroAPie datos = AssetDatabase.LoadAssetAtPath<DatosPandilleroAPie>(RutaDatos);
            for (int i = 0; i < 2; i++)
            {
                GameObject p = Object.Instantiate(prefab, grupo.transform);
                p.transform.position = new Vector3(i * 2f - 1f, 0.05f, distancia);
                SerializedObject soP = new SerializedObject(p.GetComponent<PandilleroAPie>());
                soP.FindProperty("datos").objectReferenceValue = datos;
                soP.ApplyModifiedPropertiesWithoutUndo();
            }
            PandillaAPie pandilla = grupo.AddComponent<PandillaAPie>();
            SerializedObject so = new SerializedObject(pandilla);
            so.FindProperty("datos").objectReferenceValue = datos;
            so.FindProperty("idPandilla").enumValueIndex = (int)IdPandilla.Calaveras;
            so.ApplyModifiedPropertiesWithoutUndo();
            grupo.SetActive(true);
        }

        private int Disparos()
        {
            int total = 0;
            foreach (PistolaPandillero pistola in grupo.GetComponentsInChildren<PistolaPandillero>(true))
            {
                total += pistola.Disparos;
            }
            return total;
        }

        private int LaseresVisibles()
        {
            int n = 0;
            foreach (LineRenderer l in grupo.GetComponentsInChildren<LineRenderer>(true))
            {
                if (l.enabled && l.name == "Laser")
                {
                    n++;
                }
            }
            return n;
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
        public IEnumerator TrasUnMuro_NoDisparanNiMuestranAviso()
        {
            CrearGrupo(15f);
            muro = GameObject.CreatePrimitive(PrimitiveType.Cube);
            muro.transform.position = new Vector3(0f, 2f, 8f);
            muro.transform.localScale = new Vector3(400f, 40f, 0.5f); // tan ancho que no se puede rodear
            Physics.SyncTransforms();
            VidaAPie vida = jugador.GetComponent<VidaAPie>();
            float maxima = vida.Maxima;
            int avisos = 0;
            float fin = Time.time + 6f;
            while (Time.time < fin)
            {
                avisos += LaseresVisibles();
                yield return null;
            }
            string donde = "";
            foreach (PandilleroAPie p in grupo.GetComponentsInChildren<PandilleroAPie>()) { donde += p.transform.position + " "; }
            Assert.AreEqual(0, Disparos(), "Sin línea de vista no hay disparo (pandilleros en " + donde + ")");
            Assert.AreEqual(0, avisos, "Ni aviso láser a través del muro");
            Assert.AreEqual(maxima, vida.Actual, 0.001f);
        }

        [UnityTest]
        public IEnumerator ConLineaLibre_DisparanYElTrazoEsFinisimoYCortisimo()
        {
            CrearGrupo(15f);
            VidaAPie vida = jugador.GetComponent<VidaAPie>();
            float maxima = vida.Maxima;
            float anchoMax = 0f;
            float fin = Time.time + 5f;
            while (Time.time < fin && vida.Actual >= maxima)
            {
                yield return null;
            }
            Assert.Less(vida.Actual, maxima, "Con línea libre sí disparan");
            int trazosLargos = 0;
            foreach (LineRenderer l in grupo.GetComponentsInChildren<LineRenderer>(true))
            {
                anchoMax = Mathf.Max(anchoMax, l.startWidth);
                if (l.enabled && (l.GetPosition(1) - l.GetPosition(0)).magnitude > 6.5f)
                {
                    trazosLargos++;
                }
            }
            Assert.LessOrEqual(anchoMax, 0.01f, "Trazo finísimo (" + anchoMax + " m)");
            Assert.AreEqual(0, trazosLargos, "Trazo cortísimo (no cruza la calle)");
        }
    }
}
