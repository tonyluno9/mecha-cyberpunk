using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion.Guardado;
using MechaCyberpunk.Trafico.Testigos;
using MechaCyberpunk.Progresion.Prologo.Pandillas;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.Progresion
{
    /// <summary>
    /// T-729: de dónde sube la búsqueda (las estrellas). Un espía se pone delante del calor del juego y anota cada Sumar con quién lo llamó. En la ciudad real, después de salir
    /// de casa, pasa el asalto completo y el jugador se queda parado: ningún calor. Después se roba un carro del tráfico a plena calle sin patrulla ni testigos cerca: se anota qué suma.
    /// </summary>
    public class CausasDeCalorT729Pruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private string carpeta;
        private Keyboard teclado;

        // Cada delito que llega a los testigos con quién lo llamó (la pila de quien suma calor).
        private readonly List<string> registro = new List<string>();

        private void AlEvaluar(float cantidad, MotivoCalor motivo, ResultadoTestigos resultado)
        {
            string quien = "?";
            StackFrame[] marcos = new StackTrace(1, false).GetFrames();
            if (marcos != null)
            {
                foreach (StackFrame m in marcos)
                {
                    Type tipo = m.GetMethod().DeclaringType;
                    if (tipo != null && !tipo.Name.StartsWith("CalorConTestigos") && !tipo.Name.StartsWith("TestigosDelito") && !tipo.Name.StartsWith("CausasDeCalor"))
                    {
                        quien = tipo.Name + "." + m.GetMethod().Name;
                        break;
                    }
                }
            }
            registro.Add("+" + cantidad.ToString("F1") + " " + motivo + " por " + quien + " -> " + resultado + " (t=" + Time.time.ToString("F1") + ")");
        }

        public override void Setup()
        {
            base.Setup();
            carpeta = Path.Combine(Application.temporaryCachePath, "calor_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(carpeta);
            ServicioGuardado.RutaForzada = Path.Combine(carpeta, "partida.json");
            teclado = InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
        }

        public override void TearDown()
        {
            ServicioGuardado.RutaForzada = null;
            if (carpeta != null && Directory.Exists(carpeta)) Directory.Delete(carpeta, true);
            base.TearDown();
        }

        [UnityTearDown]
        public IEnumerator DescargarEscena()
        {
            foreach (GameObject raiz in SceneManager.GetActiveScene().GetRootGameObjects()) Object.Destroy(raiz);
            Registros.LimpiarTodos();
            yield return null;
        }

        private static IEnumerator Esperar(Func<bool> condicion, string que, float segundos)
        {
            float fin = Time.realtimeSinceStartup + segundos;
            while (!condicion())
            {
                Assert.Less(Time.realtimeSinceStartup, fin, "Tiempo agotado esperando: " + que);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator Asalto_YJugadorQuieto_NoSubeLaBusqueda_YRobarUnCarroSinTestigosSeAnota()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            DirectorPandillas director = null;
            yield return Esperar(() =>
            {
                director = Object.FindFirstObjectByType<DirectorPandillas>();
                return director != null && director.Iniciado && Registro<IJugador>.Actual != null && Registro<ICalor>.Actual != null
                    && Registro<IObjetivos>.Actual != null && Registro<IPuntoHistoria>.Todos.Count > 0;
            }, "la ciudad lista", 40f);
            yield return new WaitForSecondsRealtime(1.5f);
            TestigosDelito.DelitoEvaluado += AlEvaluar;
            IJugador jugador = Registro<IJugador>.Actual;

            director.Cargar("{\"paso\":\"Asalto\"}");
            yield return null;
            yield return new WaitForSecondsRealtime(0.5f);
            Press(teclado.enterKey);
            yield return null;
            Release(teclado.enterKey);
            // El asalto sigue solo (el carro huye); el jugador se queda parado 30 s.
            yield return new WaitForSecondsRealtime(30f);
            Debug.Log("[T-729] A) 30 s de asalto con el jugador parado: calor " + Registro<ICalor>.Actual.Valor + "; delitos: " + (registro.Count == 0 ? "ninguno" : string.Join(" | ", registro)));
            Assert.AreEqual(0, registro.Count, "Sin hacer nada no sube la búsqueda: " + string.Join(" | ", registro));
            Assert.AreEqual(0f, Registro<ICalor>.Actual.Valor, 0.001f);

            // B) robar un carro del tráfico a plena calle.
            IMontable robado = null;
            float menor = float.MaxValue;
            foreach (IMontable m in Registro<IMontable>.Todos)
            {
                if (m == null || m.Modo != ModoJugador.Carro || !m.PuedeSubir || m.Ocupado) continue;
                float d = Vector3.Distance(m.PuntoSubida, jugador.Raiz.position);
                if (d < menor) { menor = d; robado = m; }
            }
            Assert.IsNotNull(robado);
            jugador.Colocar(new Pose(robado.PuntoSubida + Vector3.up * 0.1f, jugador.Raiz.rotation));
            yield return null;
            Assert.IsTrue(((MechaCyberpunk.Personaje.Jugador.JugadorAPie)jugador).Subir(robado));
            yield return new WaitForSecondsRealtime(8f);
            Debug.Log("[T-729] B) robar un carro del tráfico: calor " + Registro<ICalor>.Actual.Valor + "; delitos: " + (registro.Count == 0 ? "ninguno" : string.Join(" | ", registro)));
            TestigosDelito.DelitoEvaluado -= AlEvaluar;
        }
    }
}
