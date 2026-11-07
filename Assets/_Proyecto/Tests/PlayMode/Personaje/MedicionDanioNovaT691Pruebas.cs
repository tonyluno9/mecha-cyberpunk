using System.Collections;
using System.Collections.Generic;
using System.Text;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Vida;
using MechaCyberpunk.Personaje.Visual;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests
{
    /// <summary>
    /// T-691: medición del "daño progresivo misterioso" cerca de los robots de Nova. El jugador se queda 30 s junto a un guardia de Nova
    /// (sin arma) y se registra cada cambio de vida y cada impacto que lo causó. Todo daño debe tener un impacto publicado (una fuente
    /// conocida); sin impactos no debe bajar la vida.
    /// </summary>
    public class MedicionDanioNovaT691Pruebas
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const float SegundosMedicion = 30f;

        [TearDown]
        public void Limpiar() => EventosPelea.LimpiarSuscriptores();

        [UnityTest]
        public IEnumerator JuntoAlRobotDeNova_SinAtacarlo_NoHayDanioSinFuente()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            float espera = Time.time + 3f;
            while (Time.time < espera) yield return null;
            JugadorAPie jugador = Object.FindFirstObjectByType<JugadorAPie>();
            VidaAPie vida = jugador.GetComponent<VidaAPie>();
            GuardiaNova[] guardias = Object.FindObjectsByType<GuardiaNova>(FindObjectsSortMode.None);
            Assert.Greater(guardias.Length, 0, "la ciudad trae guardias de Nova");
            GuardiaNova guardia = guardias[0];
            jugador.Colocar(new Pose(guardia.transform.position + guardia.transform.forward * 2f, Quaternion.identity));
            yield return null;

            var registro = new StringBuilder();
            int impactos = 0;
            float vidaAnterior = vida.Actual;
            float perdidaSinFuente = 0f;
            EventosPelea.ImpactoRecibido += (objetivo, info, resultado) =>
            {
                impactos++;
                registro.AppendLine("  impacto t=" + Time.time.ToString("F1") + " cantidad=" + info.Cantidad + " tipo=" + info.Tipo);
            };
            int impactosVistos = 0;
            float fin = Time.time + SegundosMedicion;
            while (Time.time < fin)
            {
                yield return null;
                if (vida.Actual < vidaAnterior - 0.001f)
                {
                    registro.AppendLine("  vida " + vidaAnterior.ToString("F1") + " -> " + vida.Actual.ToString("F1") + " t=" + Time.time.ToString("F1")
                        + " impactosHastaAhora=" + impactos);
                    if (impactos == impactosVistos)
                    {
                        perdidaSinFuente += vidaAnterior - vida.Actual;
                    }
                }
                impactosVistos = impactos;
                vidaAnterior = vida.Actual;
            }
            Debug.Log("[T-691 medicion] guardias=" + guardias.Length + " impactos=" + impactos + " perdidaSinFuente=" + perdidaSinFuente
                + " vidaFinal=" + vida.Actual + "/" + vida.Maxima + "\n" + registro);
            Assert.AreEqual(0f, perdidaSinFuente, 0.001f, "no baja vida sin un impacto publicado:\n" + registro);
        }
    }
}
