using System;
using System.Collections;
using System.IO;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.IA.Datos;
using MechaCyberpunk.Personaje.Habilidades;
using MechaCyberpunk.Progresion.Apoyo;
using MechaCyberpunk.Progresion.Prologo.Pandillas;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>
    /// T-992: capturas de la barra de habilidades y del visor en Ciudad_Prologo de día, con el jugador a pie y el HUD H10 reales.
    /// Solo con CAPTURAR_T992=1; salen en docs/capturas/t992/.
    /// </summary>
    public class HabilidadesProtesisT992CapturasPruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string RutaPandillero = "Assets/_Proyecto/IA/Prefabs/PandilleroArmado.prefab";
        private const string RutaDatosPandillero = "Assets/_Proyecto/IA/Datos/DatosPandilleroAPie_Calaveras.asset";

        private sealed class ImplantesDoble : IImplantes
        {
            public readonly int[] Niveles = new int[4];
            public int Nivel(EspacioImplante espacio) => Niveles[(int)espacio];
            public string Nombre(EspacioImplante espacio) => Niveles[(int)espacio] > 0 ? "DOBLE" : string.Empty;
            public int Cantidad { get { int n = 0; foreach (int x in Niveles) if (x > 0) n++; return n; } }
            public EfectosImplantes Efectos => EfectosImplantes.Ninguno;
            public event Action Cambio;
            public event Action<EspacioImplante, int> Instalado { add { } remove { } }
            public void Poner(EspacioImplante e, int nivel) { Niveles[(int)e] = nivel; Cambio?.Invoke(); }
        }

        private Keyboard teclado;
        private Mouse raton;

        public override void Setup()
        {
            base.Setup();
            Registros.LimpiarTodos();
            Time.timeScale = 1f;
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();
        }

        [UnityTearDown]
        public IEnumerator DescargarEscena()
        {
            if (VisorRayosX.Actual != null) Object.Destroy(VisorRayosX.Actual.gameObject);
            if (SceneManager.GetActiveScene().path == RutaEscena)
            {
                foreach (GameObject raiz in SceneManager.GetActiveScene().GetRootGameObjects()) Object.Destroy(raiz);
                Registros.LimpiarTodos();
            }
            yield return null;
        }

        private static IEnumerator EsperarHasta(Func<bool> condicion, string que, float segundos)
        {
            float fin = Time.realtimeSinceStartup + segundos;
            while (!condicion())
            {
                Assert.Less(Time.realtimeSinceStartup, fin, "Tiempo agotado esperando: " + que);
                yield return null;
            }
        }

        private static void Capturar(string nombre)
        {
            Directory.CreateDirectory(Path.Combine("docs", "capturas", "t992"));
            CapturaUI.Capturar(Path.Combine("docs", "capturas", "t992", nombre), Camera.main);
        }

        private IEnumerator Tecla(UnityEngine.InputSystem.Controls.KeyControl tecla)
        {
            Press(tecla);
            yield return null;
            yield return null;
            Release(tecla);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Capturas_BarraListaBarraActivaYVisor()
        {
            if (Environment.GetEnvironmentVariable("CAPTURAR_T992") != "1")
            {
                Assert.Ignore("Sin CAPTURAR_T992=1 no se sacan capturas.");
            }
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            DirectorPandillas historia = null;
            yield return EsperarHasta(() =>
            {
                historia = Object.FindFirstObjectByType<DirectorPandillas>();
                return historia != null && historia.Iniciado && Registro<IJugador>.Actual != null && Registro<IRelojMundo>.Actual != null;
            }, "el director arrancó", 30f);
            for (int i = 0; i < 12 && historia.Paso != PasoHistoria.Libre; i++)
            {
                Press(teclado.f6Key);
                yield return null;
                Release(teclado.f6Key);
                yield return null;
            }
            Registro<IRelojMundo>.Actual.AvanzarHasta(12f);
            IJugador j = Registro<IJugador>.Actual;

            ImplantesDoble implantes = new ImplantesDoble();
            IImplantes real = Registro<IImplantes>.Actual;
            if (real != null) Registro<IImplantes>.Quitar(real);
            implantes.Poner(EspacioImplante.Piernas, 1);
            implantes.Poner(EspacioImplante.Brazos, 1);
            Registro<IImplantes>.Agregar(implantes);

            foreach (MonoBehaviour p in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (p.GetType().Name == "PersonajeApoyo" && p.gameObject.name.ToLowerInvariant().Contains("doc"))
                {
                    Vector3 frente = p.transform.position + p.transform.forward * 3f;
                    Vector3 hacia = p.transform.position - frente;
                    hacia.y = 0f;
                    j.Colocar(new Pose(frente, Quaternion.LookRotation(hacia.normalized, Vector3.up)));
                }
            }
            yield return new WaitForSecondsRealtime(0.8f);
            HabilidadesProtesis habilidades = j.Raiz.GetComponentInChildren<HabilidadesProtesis>();
            Assert.IsNotNull(habilidades);

            // 1. Lista: PIERNAS y BRAZOS listas, CABEZA apagada (sin visor).
            Capturar("barra-lista.png");

            // 2. BRAZOS recargando y PIERNAS activa vaciándose.
            yield return Tecla(teclado.digit2Key);
            Assert.AreEqual(EstadoHabilidad.Activa, habilidades.Estado(HabilidadProtesis.Brazos));
            float fin = Time.time + 6.6f;
            while (Time.time < fin) yield return null;
            Assert.AreEqual(EstadoHabilidad.Recargando, habilidades.Estado(HabilidadProtesis.Brazos));
            yield return Tecla(teclado.digit1Key);
            Assert.AreEqual(EstadoHabilidad.Activa, habilidades.Estado(HabilidadProtesis.Piernas));
            fin = Time.time + 2.2f;
            while (Time.time < fin) yield return null;
            Capturar("barra-activa.png");
            fin = Time.time + 4.5f;
            while (Time.time < fin) yield return null;

            // 3. Visor: un pandillero detrás de una pared, visto con CABEZA activa.
            implantes.Poner(EspacioImplante.Ojos, 1);
            VisorRayosX visor = VisorRayosX.Crear();
            Assert.IsNotNull(visor);
            Vector3 adelante = j.Raiz.forward;
            adelante.y = 0f;
            adelante.Normalize();
            Vector3 origen = j.Raiz.position;
            GameObject pared = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pared.transform.position = origen + adelante * 7f + Vector3.up * 2f;
            pared.transform.rotation = Quaternion.LookRotation(adelante);
            pared.transform.localScale = new Vector3(8f, 4f, 0.5f);
            GameObject contenedor = new GameObject("EnemigoCaptura");
            contenedor.SetActive(false);
            GameObject pandillero = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPandillero), contenedor.transform);
            pandillero.transform.position = origen + adelante * 10f;
            SerializedObject so = new SerializedObject(pandillero.GetComponent<PandilleroAPie>());
            so.FindProperty("datos").objectReferenceValue = AssetDatabase.LoadAssetAtPath<DatosPandilleroAPie>(RutaDatosPandillero);
            so.ApplyModifiedPropertiesWithoutUndo();
            contenedor.SetActive(true);
            yield return new WaitForSecondsRealtime(0.3f);
            yield return Tecla(teclado.digit3Key);
            Assert.AreEqual(EstadoHabilidad.Activa, habilidades.Estado(HabilidadProtesis.Cabeza));
            Assert.Greater(visor.Marcados, 0);
            yield return new WaitForSecondsRealtime(0.4f);
            Capturar("visor.png");
            Object.Destroy(pared);
            Object.Destroy(contenedor);
        }
    }
}
