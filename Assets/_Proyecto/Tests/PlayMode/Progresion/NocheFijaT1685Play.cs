using System;
using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion.Pruebas;
using MechaCyberpunk.Progresion.Recorrido;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.Progresion
{
    /// <summary>
    /// T-1685 (T36) en Play: con el recorrido en el paso 4 el reloj se queda a las 23:00 en pausa; si algo lo adelanta a mediodía
    /// (como al dormir) a los pocos cuadros vuelve a ser de noche. Desde el paso 12 corre normal y el mediodía se queda.
    /// </summary>
    public class NocheFijaT1685Play
    {
        private const int CuadrosEspera = 5;
        private const float Mediodia = 12f;
        private const float Margen = 0.05f;

        private GameObject objReloj;
        private IRelojMundo reloj;

        [SetUp]
        public void Preparar()
        {
            LogAssert.ignoreFailingMessages = true; // sin escena de la ciudad, los pasos avisan que les faltan cosas
            ReglaNocheFija.Activa = true;
            objReloj = new GameObject("RelojDePrueba_T1685");
            reloj = objReloj.AddComponent<RelojDePrueba>();
        }

        [TearDown]
        public void Limpiar()
        {
            if (DirectorRecorrido.Actual != null)
            {
                Object.Destroy(DirectorRecorrido.Actual.gameObject);
            }
            if (objReloj != null)
            {
                Object.Destroy(objReloj);
            }
            ClimaMundo.Lluvia = 0f;
            LogAssert.ignoreFailingMessages = false;
        }

        private static void Saltar(int paso)
        {
            try
            {
                DirectorRecorrido.SaltarAPaso(paso);
            }
            catch (Exception e)
            {
                // Entrar al paso sin la ciudad puede fallar; la noche ya quedó puesta antes (ActualizarMundo) y la mantiene Update.
                Debug.Log("[T1685] El paso " + paso + " no entró completo sin la ciudad: " + e.Message);
            }
        }

        private static IEnumerator Cuadros(int n)
        {
            for (int i = 0; i < n; i++)
            {
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator Paso4_AdelantarAMediodia_VuelveALaNocheEnPocosCuadros()
        {
            yield return null;
            Assert.AreSame(reloj, Registro<IRelojMundo>.Actual, "El reloj de prueba es el del mundo");
            Saltar((int)PasoRecorrido.Maletin);
            yield return Cuadros(CuadrosEspera);
            Assert.IsNotNull(DirectorRecorrido.Actual, "Hay director del recorrido");
            Assert.AreEqual(PasoRecorrido.Maletin, DirectorRecorrido.Actual.Paso);
            Assert.IsTrue(reloj.EsDeNoche, "En el paso 4 es de noche");
            Assert.AreEqual(ReglaNocheFija.HoraNoche, reloj.Hora, Margen, "Son las 23:00");
            Assert.IsTrue(reloj.Pausado, "El reloj está en pausa");
            int diaAntes = reloj.Dia;

            reloj.Pausar(false);
            reloj.AvanzarHasta(Mediodia); // como al dormir
            Debug.Log("[T1685] Tras AvanzarHasta(12): hora=" + reloj.Hora + " noche=" + reloj.EsDeNoche);
            yield return Cuadros(CuadrosEspera);
            Debug.Log("[T1685] A los " + CuadrosEspera + " cuadros: hora=" + reloj.Hora + " noche=" + reloj.EsDeNoche + " pausa=" + reloj.Pausado + " dia=" + reloj.Dia);
            Assert.IsTrue(reloj.EsDeNoche, "Volvió a ser de noche");
            Assert.AreEqual(ReglaNocheFija.HoraNoche, reloj.Hora, Margen, "Volvió a las 23:00");
            Assert.IsTrue(reloj.Pausado, "Sigue en pausa");
            Assert.Greater(reloj.Dia, diaAntes, "Los días cruzados cuentan");

            yield return Cuadros(30);
            Assert.AreEqual(ReglaNocheFija.HoraNoche, reloj.Hora, Margen, "No avanza solo");
        }

        [UnityTest]
        [Ignore("T-historia-libre-23nov: el paso 12 (GuerraSkulls) quedó desconectado; tras Tino es modo libre")]
        public IEnumerator Paso12_ElRelojCorreYElMediodiaSeQueda()
        {
            yield return null;
            Saltar((int)PasoRecorrido.GuerraSkulls);
            yield return Cuadros(CuadrosEspera);
            Assert.IsFalse(reloj.Pausado, "Después del paso 11 el reloj corre");
            reloj.AvanzarHasta(Mediodia);
            yield return Cuadros(CuadrosEspera);
            Assert.IsFalse(reloj.EsDeNoche, "En el paso 12 el mediodía se queda");
            Assert.AreEqual(Mediodia, reloj.Hora, 0.5f, "Sigue cerca de mediodía");
        }
    }
}
