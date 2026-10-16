using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Pruebas;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>El jugador aparece en el punto cuyo id le piden, también si el punto llega después.</summary>
    public class AparicionJugadorPruebas : InputTestFixture
    {
        private const string RutaPrefab = "Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab";
        private const float DistanciaMaxima = 0.1f;
        private const float GradosMaximos = 1f;
        private const float EsperaSinPunto = 1.6f;

        private readonly List<GameObject> creados = new List<GameObject>();
        private readonly List<string> avisos = new List<string>();
        private GameObject jugador;
        private AparicionJugador aparicion;

        public override void Setup()
        {
            base.Setup();
            Registros.LimpiarTodos();
            InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
            avisos.Clear();
            Application.logMessageReceived += AlRecibirMensaje;

            GameObject piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(400f, 1f, 400f);
            creados.Add(piso);
        }

        public override void TearDown()
        {
            Application.logMessageReceived -= AlRecibirMensaje;
            foreach (GameObject objeto in creados)
            {
                if (objeto != null)
                {
                    Object.Destroy(objeto);
                }
            }
            creados.Clear();
            if (jugador != null)
            {
                Object.Destroy(jugador);
            }
            Registros.LimpiarTodos();
            base.TearDown();
        }

        private void AlRecibirMensaje(string mensaje, string pila, LogType tipo)
        {
            if (tipo == LogType.Warning)
            {
                avisos.Add(mensaje);
            }
        }

        private PuntoAparicionDePrueba CrearPunto(string id, ModoJugador modo, Vector3 posicion, float giro)
        {
            GameObject objeto = new GameObject("Punto " + id);
            objeto.transform.SetPositionAndRotation(posicion, Quaternion.Euler(0f, giro, 0f));
            PuntoAparicionDePrueba punto = objeto.AddComponent<PuntoAparicionDePrueba>();
            punto.Configurar(id, modo);
            creados.Add(objeto);
            return punto;
        }

        private void CrearJugador(string id)
        {
            jugador = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab), new Vector3(-30f, 0.05f, -30f), Quaternion.identity);
            aparicion = jugador.GetComponent<AparicionJugador>();
            aparicion.IdAparicion = id;
        }

        private void AssertEnPunto(PuntoAparicionDePrueba punto)
        {
            Assert.Less(Vector3.Distance(punto.transform.position, jugador.transform.position), DistanciaMaxima, "Aparece sobre el punto");
            Assert.Less(Quaternion.Angle(punto.transform.rotation, jugador.transform.rotation), GradosMaximos, "Y con su rotación");
        }

        private int AvisosCon(string texto)
        {
            int cuenta = 0;
            foreach (string aviso in avisos)
            {
                if (aviso.Contains(texto))
                {
                    cuenta++;
                }
            }
            return cuenta;
        }

        [UnityTest]
        public IEnumerator Aparicion_ConElPuntoYaRegistrado_ApareceEnElPrimerFrame()
        {
            PuntoAparicionDePrueba punto = CrearPunto("Prueba_Inicio", ModoJugador.APie, new Vector3(10f, 0.05f, 20f), 130f);
            CrearJugador("Prueba_Inicio");
            yield return null;

            AssertEnPunto(punto);
            Assert.AreEqual(0, AvisosCon("[Personaje]"), "Sin avisos");
        }

        [UnityTest]
        public IEnumerator Aparicion_ConElPuntoQueLlegaDespues_ApareceCuandoSeRegistra()
        {
            CrearJugador("Prueba_Inicio");
            yield return new WaitForSeconds(0.3f);
            Assert.IsTrue(aparicion.EstaEsperando, "Sigue esperando el punto");
            Assert.Less(jugador.transform.position.x, -20f, "Y no se movió");

            PuntoAparicionDePrueba punto = CrearPunto("Prueba_Inicio", ModoJugador.APie, new Vector3(-12f, 0.05f, 6f), 250f);
            yield return null;

            AssertEnPunto(punto);
            Assert.IsFalse(aparicion.EstaEsperando);
            Assert.AreEqual(0, AvisosCon("[Personaje]"), "Sin avisos");
        }

        [UnityTest]
        public IEnumerator Aparicion_ConOtrosPuntos_ElegidoPorId()
        {
            CrearPunto("Otro", ModoJugador.APie, new Vector3(50f, 0.05f, 50f), 10f);
            PuntoAparicionDePrueba bueno = CrearPunto("Casa_Puerta", ModoJugador.APie, new Vector3(5f, 0.05f, -8f), 45f);
            CrearPunto("Otro_Mas", ModoJugador.APie, new Vector3(-50f, 0.05f, 50f), 90f);
            CrearJugador("Casa_Puerta");
            yield return null;

            AssertEnPunto(bueno);
        }

        [UnityTest]
        public IEnumerator Aparicion_ConUnIdQueNoExiste_UnSoloAvisoYSeQuedaDondeEsta()
        {
            CrearPunto("Otro", ModoJugador.APie, new Vector3(50f, 0.05f, 50f), 10f);
            CrearJugador("No_Existe");
            Vector3 inicio = jugador.transform.position;
            yield return new WaitForSeconds(EsperaSinPunto);
            yield return new WaitForSeconds(0.5f);

            Assert.AreEqual(1, AvisosCon("No_Existe"), "Un solo aviso con el id");
            Assert.IsFalse(aparicion.EstaEsperando);
            Assert.Less(Vector3.Distance(inicio, jugador.transform.position), DistanciaMaxima, "Se queda donde estaba");
        }

        [UnityTest]
        public IEnumerator Aparicion_ConIdVacio_NoHaceNadaNiAvisa()
        {
            CrearPunto("Otro", ModoJugador.APie, new Vector3(50f, 0.05f, 50f), 10f);
            CrearJugador(string.Empty);
            Vector3 inicio = jugador.transform.position;
            yield return new WaitForSeconds(EsperaSinPunto);

            Assert.AreEqual(0, AvisosCon("[Personaje]"));
            Assert.Less(Vector3.Distance(inicio, jugador.transform.position), DistanciaMaxima);
        }

        [UnityTest]
        public IEnumerator Aparicion_PuntoDeModoCarro_ColocaAPieYAvisaUnaVez()
        {
            PuntoAparicionDePrueba punto = CrearPunto("Casa_Cochera", ModoJugador.Carro, new Vector3(8f, 0.05f, 3f), 200f);
            CrearJugador("Casa_Cochera");
            yield return new WaitForSeconds(0.5f);

            AssertEnPunto(punto);
            Assert.AreEqual(ModoJugador.APie, jugador.GetComponent<JugadorAPie>().Modo, "El jugador queda a pie");
            Assert.AreEqual(1, AvisosCon("Casa_Cochera"), "Un solo aviso");
        }
    }
}
