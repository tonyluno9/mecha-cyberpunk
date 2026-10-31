using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje;
using MechaCyberpunk.Personaje.Combate;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>T-424: la regla pura del apuntado asistido elige el blanco más centrado con línea de vista y gira sin saltar.</summary>
    public class ReglaApuntadoAsistidoPruebas
    {
        private static readonly Vector3 Origen = Vector3.zero;
        private static readonly Vector3 Adelante = Vector3.forward;

        [Test]
        public void ElegirBlanco_TomaElMasCentradoDentroDelRadioYConLineaDeVista()
        {
            Vector3[] centros =
            {
                new Vector3(5f, 0f, 20f),   // unos 14 grados: fuera del radio
                new Vector3(1.5f, 0f, 20f), // unos 4.3 grados
                new Vector3(0.5f, 0f, 20f), // unos 1.4 grados
                new Vector3(0f, 0f, -20f),  // detrás
                new Vector3(0f, 0f, 90f),   // centrado pero lejos
            };
            Assert.AreEqual(2, ReglaApuntadoAsistido.ElegirBlanco(Origen, Adelante, centros, 8f, 40f, null));
            Assert.AreEqual(1, ReglaApuntadoAsistido.ElegirBlanco(Origen, Adelante, centros, 8f, 40f, i => i != 2), "Sin línea de vista no cuenta");
            Assert.AreEqual(-1, ReglaApuntadoAsistido.ElegirBlanco(Origen, Adelante, centros, 1f, 40f, null), "Fuera de un radio chico");
            Assert.AreEqual(-1, ReglaApuntadoAsistido.ElegirBlanco(Origen, Adelante, new Vector3[0], 8f, 40f, null));
        }

        [Test]
        public void Correccion_EsSuaveConTopeYElZoomLaApaga()
        {
            float uno = ReglaApuntadoAsistido.Correccion(10f, 2f, 0.016f, 40f);
            Assert.Greater(uno, 0f);
            Assert.Less(uno, 1f, "Un cuadro nunca cierra el error de golpe");
            Assert.AreEqual(-uno, ReglaApuntadoAsistido.Correccion(-10f, 2f, 0.016f, 40f), 0.0001f);
            Assert.AreEqual(0.4f, ReglaApuntadoAsistido.Correccion(90f, 50f, 0.01f, 40f), 0.0001f, "El tope en grados por segundo manda");
            Assert.AreEqual(0f, ReglaApuntadoAsistido.Correccion(10f, 0f, 0.016f, 40f), 0.0001f, "Fuerza 0 = apagado");
            Assert.AreEqual(1f, ReglaApuntadoAsistido.FactorZoom(0f, 0.1f), 0.0001f);
            Assert.AreEqual(0.1f, ReglaApuntadoAsistido.FactorZoom(1f, 0.1f), 0.0001f);
        }

        [Test]
        public void Datos_ApagadoNoJalaYFuerteEsMasQueSuave()
        {
            DatosApuntadoAsistido datos = ScriptableObject.CreateInstance<DatosApuntadoAsistido>();
            Assert.AreEqual(0f, datos.Fuerza(NivelApuntadoAsistido.Apagado));
            Assert.Greater(datos.Fuerza(NivelApuntadoAsistido.Suave), 0f);
            Assert.Greater(datos.Fuerza(NivelApuntadoAsistido.Fuerte), datos.Fuerza(NivelApuntadoAsistido.Suave));
            Assert.Greater(datos.Radio(NivelApuntadoAsistido.Fuerte), datos.Radio(NivelApuntadoAsistido.Suave));
            Object.DestroyImmediate(datos);
        }
    }
}
