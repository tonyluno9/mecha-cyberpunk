using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.Personaje.Vida;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests
{
    /// <summary>T-1106: los ganchos de conducta y de filtro de daño de PandilleroAPie. Sin ellos todo sigue igual.</summary>
    public class GanchosConductaT1106Pruebas : InputTestFixture
    {
        private const string RutaJugador = "Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab";
        private const string RutaPandillero = "Assets/_Proyecto/IA/Prefabs/PandilleroArmado.prefab";
        private const string RutaDatos = "Assets/_Proyecto/IA/Datos/DatosPandilleroAPie_Viboras.asset";
        private const float DistanciaInicial = 10f;

        private GameObject piso;
        private GameObject jugador;
        private GameObject objeto;
        private PandilleroAPie pandillero;

        private sealed class ConductaPrueba : MonoBehaviour, IConductaAPie
        {
            public bool Activa = true;
            public int Llamadas;
            public bool Controla => Activa;
            public void Actualizar(PandilleroAPie p, float dt)
            {
                Llamadas++;
                p.MoverPlano(Vector3.right * 3f);
            }
        }

        private sealed class FiltroPrueba : MonoBehaviour, IFiltroDanioAPie
        {
            public float Resultado;
            public TipoBloqueo Tipo = TipoBloqueo.Normal;
            public float Recibida;
            public float Filtrar(PandilleroAPie p, InfoDanio info, out TipoBloqueo bloqueo)
            {
                Recibida = info.Cantidad;
                bloqueo = Tipo;
                return Resultado;
            }
        }

        public override void Setup()
        {
            base.Setup();
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
            EventosPelea.LimpiarSuscriptores();
            InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
            piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(400f, 1f, 400f);
            jugador = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaJugador), new Vector3(0f, 0.05f, 0f), Quaternion.identity);
            jugador.GetComponent<EscudoJugador>()?.Iniciar(0f, 0f, 0f);
        }

        public override void TearDown()
        {
            Object.DestroyImmediate(objeto);
            Object.DestroyImmediate(jugador);
            Object.DestroyImmediate(piso);
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
            EventosPelea.LimpiarSuscriptores();
            base.TearDown();
        }

        // Se instancia apagado para poder agregar la conducta o el filtro antes de que corra Awake.
        private void Crear()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPandillero);
            prefab.SetActive(false);
            objeto = Object.Instantiate(prefab, new Vector3(0f, 0.05f, DistanciaInicial), Quaternion.LookRotation(-Vector3.forward));
            prefab.SetActive(true);
            SerializedObject so = new SerializedObject(objeto.GetComponent<PandilleroAPie>());
            so.FindProperty("datos").objectReferenceValue = AssetDatabase.LoadAssetAtPath<MechaCyberpunk.IA.Datos.DatosPandilleroAPie>(RutaDatos);
            so.ApplyModifiedPropertiesWithoutUndo();
            pandillero = objeto.GetComponent<PandilleroAPie>();
        }

        private static IEnumerator Esperar(float s)
        {
            float fin = Time.time + s;
            while (Time.time < fin) yield return null;
        }

        private float DistanciaAlJugador() => Vector3.Distance(new Vector3(objeto.transform.position.x, 0f, objeto.transform.position.z), new Vector3(jugador.transform.position.x, 0f, jugador.transform.position.z));

        [UnityTest]
        public IEnumerator SinConductaNiFiltro_LaIANormalSeAcercaYElDanioEntraCompleto()
        {
            Crear();
            objeto.SetActive(true);
            pandillero.Alerta = true;
            yield return Esperar(4f);
            Assert.Less(DistanciaAlJugador(), DistanciaInicial - 1f, "sin conducta persigue al jugador");
            float antes = pandillero.Actual;
            ResultadoDanio r = pandillero.RecibirDanio(new InfoDanio(10f, TipoDanio.Golpe, objeto.transform.position, Vector3.forward, null));
            Assert.AreEqual(TipoBloqueo.Ninguno, r.Bloqueo);
            Assert.AreEqual(antes - 10f, pandillero.Actual, 0.01f);
            Assert.IsNotNull(pandillero.Jugador, "Jugador expuesto");
            Assert.IsTrue(pandillero.JugadorAtacable);
        }

        [UnityTest]
        public IEnumerator ConductaQueControla_DetieneLaIAYMueveConMoverPlano()
        {
            Crear();
            ConductaPrueba c = objeto.AddComponent<ConductaPrueba>();
            objeto.SetActive(true);
            pandillero.Alerta = true;
            Vector3 inicio = objeto.transform.position;
            yield return Esperar(1.5f);
            Assert.Greater(c.Llamadas, 5, "la conducta recibe el turno");
            Assert.Greater(objeto.transform.position.x - inicio.x, 2f, "MoverPlano la mueve en X");
            Assert.AreEqual(DistanciaInicial, Mathf.Abs(objeto.transform.position.z - jugador.transform.position.z), 0.3f, "la IA normal no se acerca");
            c.Activa = false;
            int llamadas = c.Llamadas;
            yield return Esperar(1f);
            Assert.AreEqual(llamadas, c.Llamadas, "con Controla=false la conducta ya no actúa");
            Assert.Less(Mathf.Abs(objeto.transform.position.z - jugador.transform.position.z), DistanciaInicial - 0.5f, "vuelve la IA normal y se acerca");
        }

        [UnityTest]
        public IEnumerator FiltroQueDevuelveCero_DejaLaVidaIgualYElResultadoDiceNormal()
        {
            Crear();
            FiltroPrueba f = objeto.AddComponent<FiltroPrueba>();
            f.Resultado = 0f;
            objeto.SetActive(true);
            yield return null;
            float antes = pandillero.Actual;
            int impactos = 0;
            TipoBloqueo visto = TipoBloqueo.Ninguno;
            EventosPelea.ImpactoRecibido += (o, info, res) => { impactos++; visto = res.Bloqueo; };
            ResultadoDanio r = pandillero.RecibirDanio(new InfoDanio(30f, TipoDanio.Golpe, objeto.transform.position, Vector3.forward, null));
            Assert.AreEqual(antes, pandillero.Actual, 0.0001f);
            Assert.AreEqual(TipoBloqueo.Normal, r.Bloqueo);
            Assert.AreEqual(0f, r.DanioAplicado, 0.0001f);
            Assert.AreEqual(30f, f.Recibida, 0.0001f, "el filtro ve la cantidad completa");
            Assert.AreEqual(1, impactos, "el impacto se publica para chispa y sonido");
            Assert.AreEqual(TipoBloqueo.Normal, visto);
        }

        [UnityTest]
        public IEnumerator FiltroParcial_AplicaLaCantidadFiltrada_YElCastigoLlegaMultiplicadoAlFiltro()
        {
            Crear();
            FiltroPrueba f = objeto.AddComponent<FiltroPrueba>();
            f.Resultado = 4f;
            objeto.SetActive(true);
            yield return null;
            float antes = pandillero.Actual;
            pandillero.RecibirDanio(new InfoDanio(40f, TipoDanio.Golpe, objeto.transform.position, Vector3.forward, null));
            Assert.AreEqual(antes - 4f, pandillero.Actual, 0.01f);
            Assert.IsFalse(pandillero.Castigable);
            Assert.AreEqual(40f, f.Recibida, 0.001f);
            pandillero.SetCastigable(true);
            Assert.IsTrue(pandillero.Castigable);
            f.Resultado = 0f;
            pandillero.RecibirDanio(new InfoDanio(40f, TipoDanio.Golpe, objeto.transform.position, Vector3.forward, null));
            Assert.AreEqual(40f * pandillero.Datos.MultiplicadorCastigo, f.Recibida, 0.01f, "el filtro recibe la cantidad ya multiplicada");
        }
    }
}
