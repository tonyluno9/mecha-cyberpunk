using System.Collections;
using System.Linq;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.Personaje.Visual;
using MechaCyberpunk.Progresion.Prologo;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Progresion
{
    /// <summary>T-1520: sin cuarto de verdad. El cuarto no está en la escena, la partida nueva empieza en la calle levantándose y la cama y el espejo siguen sirviendo junto a la puerta.</summary>
    public class InicioEnLaCalleT1520Pruebas
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string IdInicio = "Inicio_Calle";

        [UnitySetUp]
        public IEnumerator Cargar()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
        }

        private static IPuntoAparicion Aparicion(string id)
        {
            return Registro<IPuntoAparicion>.Todos.FirstOrDefault(p => p != null && p.Id == id);
        }

        [UnityTest]
        public IEnumerator EnLaEscena_NoHayCuartoNiEspejoDelCuarto_NiApagados()
        {
            Transform[] todos = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.IsFalse(todos.Any(t => t.name == "Cuarto_Habitacion" || t.name == "T550_Cuartos" || t.name == "S2t_Espejo"), "el cuarto no se carga, ni apagado");
            yield return null;
        }

        [UnityTest]
        public IEnumerator InicioCalle_EstaAfueraCercaDeLaPuerta_ConSueloYSinTecho()
        {
            IPuntoAparicion inicio = Aparicion(IdInicio);
            IPuntoAparicion puerta = Aparicion("Casa_Puerta");
            Assert.IsNotNull(inicio, "hay aparición " + IdInicio);
            Assert.IsNotNull(puerta, "hay aparición Casa_Puerta");
            Vector3 p = inicio.Pose.position;
            Assert.LessOrEqual(Vector3.Distance(p, puerta.Pose.position), 15f, "cerca de donde empezaba la historia");
            Assert.IsTrue(Physics.Raycast(p + Vector3.up * 2f, Vector3.down, out RaycastHit suelo, 4f, ~0, QueryTriggerInteraction.Ignore), "hay suelo");
            Assert.Less(Mathf.Abs(suelo.point.y - p.y), 0.5f, "la aparición está sobre el suelo");
            Assert.IsFalse(Physics.Raycast(p + Vector3.up * 1.8f, Vector3.up, 10f, ~0, QueryTriggerInteraction.Ignore), "sin techo encima (no es un interior)");
            Assert.IsFalse(Physics.CheckCapsule(p + Vector3.up * 0.5f, p + Vector3.up * 1.6f, 0.4f, ~0, QueryTriggerInteraction.Ignore), "nada dentro del cuerpo");
            yield return null;
        }

        [UnityTest]
        public IEnumerator PartidaNueva_EmpiezaEnLaCalleLevantandose_YDespuesRecuperaElControl()
        {
            // Como el menú: partida nueva pendiente y se recarga la escena (la de UnitySetUp ya corrió sin ella).
            MechaCyberpunk.Progresion.Partida.FlujoPartida.Interceptar(_ => { });
            MechaCyberpunk.Progresion.Partida.FlujoPartida.IrANuevaPartida();
            MechaCyberpunk.Progresion.Partida.FlujoPartida.Interceptar(null);
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
            IPuntoAparicion inicio = Aparicion(IdInicio);
            Assert.IsNotNull(inicio);
            float limite = Time.realtimeSinceStartup + 5f;
            JugadorAPie jugador = null;
            while (Time.realtimeSinceStartup < limite)
            {
                jugador = Registro<IJugador>.Actual as JugadorAPie;
                if (jugador != null && Vector3.Distance(jugador.Raiz.position, inicio.Pose.position) < 1.5f)
                {
                    break;
                }
                yield return null;
            }
            Assert.IsNotNull(jugador, "hay jugador");
            Assert.Less(Vector3.Distance(jugador.Raiz.position, inicio.Pose.position), 1.5f, "el jugador empieza en " + IdInicio);
            VisualPersonaje visual = jugador.GetComponentInChildren<VisualPersonaje>(true);
            ControladorAPie control = jugador.GetComponent<ControladorAPie>();
            Assert.IsNotNull(visual);
            Assert.IsTrue(visual.Caido, "empieza tumbado");
            Assert.IsFalse(control.ControlActivo, "sin control mientras se levanta");
            limite = Time.realtimeSinceStartup + 12f;
            while (!control.ControlActivo && Time.realtimeSinceStartup < limite)
            {
                yield return null;
            }
            Assert.IsFalse(visual.Caido, "ya se levantó");
            Assert.IsTrue(control.ControlActivo, "recupera el control");
        }

        [UnityTest]
        public IEnumerator CamaYEspejo_SirvenJuntoALaPuerta_SinDependerDelCuarto()
        {
            IPuntoAparicion puerta = Aparicion("Casa_Puerta");
            CamaJugador cama = Object.FindObjectsByType<CamaJugador>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).FirstOrDefault();
            EspejoJugador espejo = Object.FindObjectsByType<EspejoJugador>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).FirstOrDefault();
            Assert.IsNotNull(cama, "hay cama activa");
            Assert.IsNotNull(espejo, "hay espejo activo");
            Assert.LessOrEqual(Vector3.Distance(cama.transform.position, puerta.Pose.position), 2.5f, "cama junto a la puerta");
            Assert.LessOrEqual(Vector3.Distance(espejo.transform.position, puerta.Pose.position), 2.5f, "espejo junto a la puerta");
            yield return null;
        }

        [UnityTest]
        public IEnumerator MorirSigueAparecendoEnLaClinica()
        {
            Assert.IsNotNull(Aparicion("Clinica_Camilla"), "la clínica del Doc sigue siendo donde se despierta al morir");
            yield return null;
        }
    }
}
