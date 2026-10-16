using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Mundo.Ambiente;
using MechaCyberpunk.Mundo.Reloj;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>Reloj del mundo y ciclo de día y noche en Ciudad.unity: luz, ambiente, farolas y contrato IRelojMundo.</summary>
    public class CicloDiaNochePruebas
    {
        private const string RutaCiudad = "Assets/_Proyecto/Escenas/Ciudad.unity";
        private const float HoraMediodia = 12f;
        private const float HoraMedianoche = 0f;
        private const float HoraNoche = 22f;
        private const float EscalaRapida = 360f;
        private const float SegundosMaximosEspera = 10f;

        private RelojMundo reloj;

        [UnitySetUp]
        public IEnumerator CargarCiudad()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaCiudad, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
            reloj = Object.FindFirstObjectByType<RelojMundo>();
            Assert.IsNotNull(reloj, "Ciudad debe traer un RelojMundo");
            reloj.Pausar(true);
        }

        [TearDown]
        public void Limpiar()
        {
            foreach (GameObject raiz in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                Object.DestroyImmediate(raiz);
            }
        }

        private static Light LuzDireccional() => GameObject.Find("LuzNoche").GetComponent<Light>();

        [UnityTest]
        public IEnumerator Luz_AlMediodia_EsMasIntensaQueAMedianocheYElAmbienteCambia()
        {
            reloj.AvanzarHasta(HoraMedianoche);
            yield return null;
            float intensidadNoche = LuzDireccional().intensity;
            Color ambienteNoche = RenderSettings.ambientLight;
            Color nieblaNoche = RenderSettings.fogColor;

            reloj.AvanzarHasta(HoraMediodia);
            yield return null;
            float intensidadDia = LuzDireccional().intensity;
            Assert.Greater(intensidadDia, intensidadNoche, "A las 12:00 la luz debe ser más intensa que a las 00:00");
            Assert.AreNotEqual(ambienteNoche, RenderSettings.ambientLight, "El color del ambiente debe cambiar");
            Assert.Greater(RenderSettings.ambientLight.grayscale, ambienteNoche.grayscale, "De día el ambiente es más claro");
            Assert.AreNotEqual(nieblaNoche, RenderSettings.fogColor, "El color de la niebla debe cambiar");
            Assert.IsTrue(RenderSettings.fog);
        }

        [UnityTest]
        public IEnumerator Luces_A22_TodasLasLuzNocturnaPrendidasYA12Apagadas()
        {
            LuzNocturna[] luces = Object.FindObjectsByType<LuzNocturna>(FindObjectsSortMode.None);
            Assert.Greater(luces.Length, 20, "El barrio debe traer farolas y letreros con LuzNocturna");

            reloj.AvanzarHasta(HoraNoche);
            yield return null;
            foreach (LuzNocturna luz in luces)
            {
                Assert.IsTrue(luz.EstaPrendida, luz.name + " debe estar prendida a las 22:00");
            }
            int farolasPrendidas = 0;
            foreach (Light luz in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (luz.type == LightType.Point && luz.enabled)
                {
                    farolasPrendidas++;
                }
            }
            Assert.Greater(farolasPrendidas, 0, "Las luces de las farolas deben estar prendidas de noche");

            reloj.AvanzarHasta(HoraMediodia);
            yield return null;
            foreach (LuzNocturna luz in luces)
            {
                Assert.IsFalse(luz.EstaPrendida, luz.name + " debe estar apagada a las 12:00");
            }
            foreach (Light luz in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (luz.type == LightType.Point && luz.GetComponentInParent<LuzTorreta>() == null) // las torretas parpadean siempre
                {
                    Assert.IsFalse(luz.enabled, "Las luces de las farolas deben estar apagadas de día");
                }
            }
        }

        [UnityTest]
        public IEnumerator Contrato_ElRelojDeLaEscena_EsElRegistroActual()
        {
            yield return null;
            Assert.AreSame(reloj, Registro<IRelojMundo>.Actual);
            Assert.AreEqual(7f, Registro<IRelojMundo>.Actual.HoraAmanecer);
            Assert.AreEqual(19f, Registro<IRelojMundo>.Actual.HoraAnochecer);
            Assert.AreEqual(FranjaDia.Noche, Registro<IRelojMundo>.Actual.Franja);
        }

        [UnityTest]
        public IEnumerator Contrato_AvanzarHasta_PublicaDiaFranjaYUnaHora()
        {
            reloj.AvanzarHasta(21f);
            yield return null;
            List<int> horas = new List<int>();
            List<FranjaDia> franjas = new List<FranjaDia>();
            List<int> dias = new List<int>();
            IRelojMundo contrato = Registro<IRelojMundo>.Actual;
            contrato.HoraEnteraCambio += horas.Add;
            contrato.FranjaCambio += franjas.Add;
            contrato.DiaCambio += dias.Add;
            int diaAntes = contrato.Dia;

            contrato.AvanzarHasta(8f);
            Assert.AreEqual(new[] { 8 }, horas.ToArray());
            Assert.AreEqual(new[] { FranjaDia.Dia }, franjas.ToArray());
            Assert.AreEqual(new[] { diaAntes + 1 }, dias.ToArray());
            Assert.AreEqual(8f, contrato.Hora, 0.001f);

            horas.Clear();
            franjas.Clear();
            dias.Clear();
            contrato.AvanzarHasta(8f);
            Assert.IsEmpty(horas, "Saltar a la hora actual no avisa nada");
            Assert.IsEmpty(franjas);
            Assert.IsEmpty(dias);

            contrato.AvanzarHasta(10f);
            Assert.AreEqual(new[] { 10 }, horas.ToArray());
            Assert.IsEmpty(franjas, "La franja no cambió");
            Assert.IsEmpty(dias, "No cruzó la medianoche");
        }

        [UnityTest]
        public IEnumerator Contrato_AvanzandoConDt_PublicaTodasLasHorasEnOrdenYElCambioDeDia()
        {
            reloj.AvanzarHasta(21f);
            List<int> horas = new List<int>();
            List<int> dias = new List<int>();
            List<FranjaDia> franjas = new List<FranjaDia>();
            reloj.HoraEnteraCambio += horas.Add;
            reloj.DiaCambio += dias.Add;
            reloj.FranjaCambio += franjas.Add;
            int diaAntes = reloj.Dia;

            reloj.Pausar(false);
            reloj.EscalaTiempo = EscalaRapida;
            float limite = Time.realtimeSinceStartup + SegundosMaximosEspera;
            while (dias.Count == 0 && Time.realtimeSinceStartup < limite)
            {
                yield return null;
            }
            reloj.Pausar(true);

            Assert.AreEqual(new[] { diaAntes + 1 }, dias.ToArray(), "Debe avisar el día nuevo una sola vez");
            Assert.AreEqual(22, horas[0]);
            for (int i = 1; i < horas.Count; i++)
            {
                Assert.AreEqual((horas[i - 1] + 1) % 24, horas[i], "Las horas enteras deben llegar en orden, sin saltarse ninguna");
            }
            Assert.Contains(0, horas);
            Assert.IsEmpty(franjas, "Entre las 21:00 y las 00:00 no cambia la franja");
        }
    }
}
