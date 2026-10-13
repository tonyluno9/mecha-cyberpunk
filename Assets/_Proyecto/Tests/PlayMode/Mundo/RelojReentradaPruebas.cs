using System.Collections.Generic;
using System.Reflection;
using MechaCyberpunk.Core;
using MechaCyberpunk.Mundo.Datos;
using MechaCyberpunk.Mundo.Reloj;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>Un suscriptor que llama AvanzarHasta mientras el reloj publica no debe hacer perder avisos.</summary>
    public class RelojReentradaPruebas
    {
        private const float HoraDestino = 10f;
        private const float HoraReentrante = 15f;

        private GameObject objeto;
        private ConfigRelojMundo config;

        [TearDown]
        public void Limpiar()
        {
            Object.DestroyImmediate(objeto);
            Object.DestroyImmediate(config);
        }

        private RelojMundo CrearReloj()
        {
            config = ScriptableObject.CreateInstance<ConfigRelojMundo>();
            objeto = new GameObject("RelojPrueba");
            objeto.SetActive(false);
            RelojMundo reloj = objeto.AddComponent<RelojMundo>();
            typeof(RelojMundo).GetField("config", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(reloj, config);
            objeto.SetActive(true);
            reloj.Pausar(true);
            return reloj;
        }

        [Test]
        public void AvanzarHasta_DentroDeUnAviso_NoPierdeAvisosPendientesYLosDaEnOrden()
        {
            RelojMundo reloj = CrearReloj();
            List<string> avisos = new List<string>();
            bool reentro = false;
            reloj.DiaCambio += dia =>
            {
                avisos.Add("Dia" + dia);
                if (!reentro)
                {
                    reentro = true;
                    reloj.AvanzarHasta(HoraReentrante);
                }
            };
            reloj.FranjaCambio += franja => avisos.Add("Franja" + franja);
            reloj.HoraEnteraCambio += hora => avisos.Add("Hora" + hora);

            reloj.AvanzarHasta(HoraDestino);

            CollectionAssert.AreEqual(new[] { "Dia2", "FranjaDia", "Hora10", "Hora15" }, avisos,
                "Los avisos de 21:00 a 10:00 y el del salto reentrante deben llegar todos, en orden");
        }
    }
}
