using System;
using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Progresion
{
    /// <summary>DatosEscenas (T-124): a qué escena pertenece cada etapa del prólogo.</summary>
    public class DatosEscenasPruebas
    {
        private const string Titulo = "titulo.unity";
        private const string Ciudad = "ciudad.unity";
        private const string Flashback = "flashback.unity";

        private DatosEscenas datos;

        [SetUp]
        public void Preparar()
        {
            datos = ScriptableObject.CreateInstance<DatosEscenas>();
            datos.Fijar(Titulo, Ciudad, Flashback);
        }

        [TearDown]
        public void Limpiar()
        {
            UnityEngine.Object.DestroyImmediate(datos);
        }

        [Test]
        public void RutaPara_CadaEtapa_DaLaEscenaDeLaTabla()
        {
            foreach (EtapaPrologo etapa in Enum.GetValues(typeof(EtapaPrologo)))
            {
                string esperada;
                switch (etapa)
                {
                    case EtapaPrologo.Ninguna:
                    case EtapaPrologo.CreacionPersonaje:
                        esperada = Titulo;
                        break;
                    case EtapaPrologo.Flashback:
                        esperada = Flashback;
                        break;
                    default:
                        esperada = Ciudad;
                        break;
                }
                Assert.AreEqual(esperada, datos.RutaPara(etapa), etapa.ToString());
            }
        }
    }
}
