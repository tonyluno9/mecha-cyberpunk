using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests
{
    /// <summary>
    /// T-1631 (T17): "NOVA INTERVIENE": el policía a pie que recibe la orden de retirarse se aleja del jugador, libera su tope en el director y solo desaparece cuando
    /// está lejos y fuera de la vista; si se ve, nunca desaparece. Piso, jugador real y policía real (prefab PoliciaAPie).
    /// </summary>
    public class RetiradaPoliciaT1631Pruebas
    {
        private const string RutaJugador = "Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab";
        private const string RutaPolicia = "Assets/_Proyecto/IA/Prefabs/PoliciaAPie.prefab";
        private const float Escala = 3f;

        private readonly List<GameObject> extras = new List<GameObject>();
        private GameObject jugador;
        private float escalaAnterior;

        [SetUp]
        public void Preparar()
        {
            Registros.LimpiarTodos();
            escalaAnterior = Time.timeScale;
            ParametrosRespuesta numeros = ParametrosRespuesta.PorOmision();
            numeros.Registrar = false;
            DirectorRespuesta.Configurar(numeros);
            GameObject piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(900f, 1f, 900f);
            extras.Add(piso);
            jugador = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaJugador), new Vector3(0f, 0.05f, 0f), Quaternion.identity);
        }

        [TearDown]
        public void Limpiar()
        {
            Time.timeScale = escalaAnterior;
            foreach (GameObject g in extras)
            {
                if (g != null)
                {
                    Object.Destroy(g);
                }
            }
            extras.Clear();
            if (jugador != null)
            {
                Object.Destroy(jugador);
            }
            DirectorRespuesta.Configurar(null);
            Registros.LimpiarTodos();
        }

        private GameObject CrearCamara(Vector3 posicion, Quaternion giro)
        {
            GameObject camObjeto = new GameObject("CamaraPrueba");
            camObjeto.tag = "MainCamera";
            camObjeto.AddComponent<Camera>();
            camObjeto.transform.SetPositionAndRotation(posicion, giro);
            extras.Add(camObjeto);
            return camObjeto;
        }

        private PandilleroAPie CrearPolicia(Vector3 donde)
        {
            GameObject policia = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPolicia), donde, Quaternion.identity);
            extras.Add(policia);
            PandilleroAPie p = policia.GetComponent<PandilleroAPie>();
            Assert.IsNotNull(p, "el prefab PoliciaAPie lleva PandilleroAPie");
            Assert.IsTrue(p.Datos != null && p.Datos.EsPolicia, "es policía");
            return p;
        }

        [UnityTest]
        public IEnumerator PoliciaOrdenado_SeAleja_LiberaSuTope_YDesapareceLejosYFueraDeVista()
        {
            CrearCamara(new Vector3(0f, 600f, 0f), Quaternion.Euler(-90f, 0f, 0f)); // mira al cielo: nadie ve nada
            PandilleroAPie p = CrearPolicia(new Vector3(4f, 0.05f, 0f));
            GameObject objeto = p.gameObject;
            DirectorRespuesta.Registrar(TipoRespuesta.PoliciaAPie, objeto);
            DirectorRespuesta.Avanzar(Time.time, 5);
            Assert.AreEqual(1, DirectorRespuesta.Vivos(TipoRespuesta.PoliciaAPie), "antes de la orden cuenta como vivo");
            yield return null;
            yield return null;
            ConductaRetiradaPolicia c = ConductaRetiradaPolicia.Ordenar(p);
            Assert.IsNotNull(c);
            Assert.AreSame(c, ConductaRetiradaPolicia.Ordenar(p), "la orden se da una sola vez");
            DirectorRespuesta.Avanzar(Time.time + 0.1f, 5);
            Assert.AreEqual(0, DirectorRespuesta.Vivos(TipoRespuesta.PoliciaAPie), "retirarse libera su tope: no cuenta como baja");
            Time.timeScale = Escala;
            float ultimaDistancia = 0f;
            float limite = Time.time + 40f;
            while (objeto != null && Time.time < limite)
            {
                ultimaDistancia = Vector3.Distance(objeto.transform.position, jugador.transform.position);
                yield return null;
            }
            Assert.IsTrue(objeto == null, "fuera de la vista y lejos, desaparece");
            Assert.GreaterOrEqual(ultimaDistancia, DirectorRespuesta.Parametros.MetrosMinimosRetiroPolicia - 2f, "no desaparece cerca del jugador");
        }

        [UnityTest]
        public IEnumerator PoliciaOrdenado_AunLejos_NoDesapareceSiLaCamaraLoVe()
        {
            // La cámara mira hacia donde se va el policía (+X): lo ve todo el rato.
            GameObject cam = CrearCamara(new Vector3(0f, 1.6f, 0f), Quaternion.LookRotation(Vector3.right));
            cam.GetComponent<Camera>().farClipPlane = 600f;
            PandilleroAPie p = CrearPolicia(new Vector3(10f, 0.05f, 0f));
            GameObject objeto = p.gameObject;
            yield return null;
            yield return null;
            ConductaRetiradaPolicia.Ordenar(p);
            Time.timeScale = Escala;
            float inicio = Time.time;
            float maxima = 0f;
            while (Time.time - inicio < 14f)
            {
                Assert.IsTrue(objeto != null, "el policía a la vista no desaparece");
                maxima = Mathf.Max(maxima, Vector3.Distance(objeto.transform.position, jugador.transform.position));
                yield return null;
            }
            Assert.Greater(maxima, DirectorRespuesta.Parametros.MetrosMinimosRetiroPolicia, "ya estaba más lejos que el mínimo y aun así sigue ahí");
        }

        [UnityTest]
        public IEnumerator PoliciaMuerto_NoRecibeLaOrden()
        {
            CrearCamara(new Vector3(0f, 600f, 0f), Quaternion.Euler(-90f, 0f, 0f));
            PandilleroAPie p = CrearPolicia(new Vector3(4f, 0.05f, 0f));
            yield return null;
            p.RecibirDanio(new InfoDanio(100000f, TipoDanio.Explosion, p.transform.position, Vector3.down, null));
            Assert.IsTrue(p.EstaKO);
            Assert.IsNull(ConductaRetiradaPolicia.Ordenar(p), "uno caído se queda tirado: no se retira");
        }
    }
}
