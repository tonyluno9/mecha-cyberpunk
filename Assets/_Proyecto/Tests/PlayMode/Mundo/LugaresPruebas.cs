using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Mundo.Lugares;
using MechaCyberpunk.Mundo.Reloj;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>Los cinco locales del barrio: registro, estado por hora, avisos y letreros.</summary>
    public class LugaresPruebas
    {
        private const float HoraAntesDeCerrar = 5.5f;
        private const float HoraDespuesDeCerrar = 6.5f;
        private const float DistanciaMaximaEntrada = 3f;
        private const float HoraAntesDeAbrir = 18.5f;
        private const float EscalaRapida = 1800f;
        private const float SegundosCorridaRapida = 20f;

        private static readonly IdLugar[] Cinco = { IdLugar.Casa, IdLugar.Bar, IdLugar.Clandestino, IdLugar.Casino, IdLugar.MercadoNegro };

        private RelojMundo reloj;

        [UnitySetUp]
        public IEnumerator Cargar()
        {
            EventosLugares.LimpiarSuscriptores();
            yield return EscenaLugares.CargarCiudad();
            reloj = EscenaLugares.RelojPausado();
        }

        [TearDown]
        public void Limpiar()
        {
            EventosLugares.LimpiarSuscriptores();
            EscenaLugares.Descargar();
        }

        private static LetreroLugar Letrero(IdLugar id)
        {
            return ((Component)EscenaLugares.Lugar(id)).GetComponentInChildren<LetreroLugar>();
        }

        [UnityTest]
        public IEnumerator Registro_LosCincoLugaresYSusPuertas_EstanConEntradaFrenteALaPuerta()
        {
            yield return null;
            Assert.AreEqual(Cinco.Length, Registro<ILugar>.Cantidad);
            Assert.AreEqual(Cinco.Length, Registro<IPuerta>.Cantidad);
            foreach (IdLugar id in Cinco)
            {
                ILugar lugar = EscenaLugares.Lugar(id);
                IPuerta puerta = EscenaLugares.Puerta(id);
                Assert.AreEqual(id, lugar.Id);
                Assert.Less(Vector3.Distance(lugar.PosicionEntrada, puerta.Posicion), DistanciaMaximaEntrada, id + ": la entrada está a menos de 3 m de la puerta");
                Vector3 haciaLaCalle = ((Component)lugar).transform.forward;
                Assert.Greater(Vector3.Dot(lugar.PosicionEntrada - puerta.Posicion, haciaLaCalle), 0f, id + ": la entrada está del lado de la calle");
                Assert.IsFalse(lugar.JugadorDentro);
            }
        }

        [UnityTest]
        public IEnumerator EstaAbierto_SegunHoraYCierreDeDemo()
        {
            yield return null;
            reloj.AvanzarHasta(EscenaLugares.HoraMediodia);
            Assert.IsTrue(EscenaLugares.Lugar(IdLugar.Casa).EstaAbierto);
            Assert.IsFalse(EscenaLugares.Lugar(IdLugar.Bar).EstaAbierto);
            Assert.IsFalse(EscenaLugares.Lugar(IdLugar.Clandestino).EstaAbierto);
            Assert.IsFalse(EscenaLugares.Lugar(IdLugar.Casino).EstaAbierto, "A las 12:00 el casino está cerrado por horario");
            Assert.IsFalse(EscenaLugares.Lugar(IdLugar.MercadoNegro).EstaAbierto, "Está en horario pero cerrado en la demo");

            reloj.AvanzarHasta(EscenaLugares.HoraNoche);
            Assert.IsTrue(EscenaLugares.Lugar(IdLugar.Casa).EstaAbierto);
            Assert.IsTrue(EscenaLugares.Lugar(IdLugar.Bar).EstaAbierto);
            Assert.IsTrue(EscenaLugares.Lugar(IdLugar.Clandestino).EstaAbierto);
            Assert.IsTrue(EscenaLugares.Lugar(IdLugar.Casino).EstaAbierto, "T-303: de noche el casino abre");
            Assert.IsFalse(EscenaLugares.Lugar(IdLugar.MercadoNegro).EstaAbierto);
        }

        [UnityTest]
        public IEnumerator Clandestino_De0530A0630_AvisaUnaSolaVezQueCerro()
        {
            reloj.AvanzarHasta(HoraAntesDeCerrar);
            yield return null;
            yield return null;
            ILugar clandestino = EscenaLugares.Lugar(IdLugar.Clandestino);
            Assert.IsTrue(clandestino.EstaAbierto);

            int avisosLugar = 0;
            bool ultimoLugar = true;
            int avisosGlobales = 0;
            IdLugar ultimoIdGlobal = IdLugar.Ninguno;
            bool ultimoGlobal = true;
            clandestino.EstadoCambio += abierto => { avisosLugar++; ultimoLugar = abierto; };
            EventosLugares.EstadoLugarCambio += (id, abierto) =>
            {
                if (id == IdLugar.Clandestino)
                {
                    avisosGlobales++;
                    ultimoIdGlobal = id;
                    ultimoGlobal = abierto;
                }
            };

            reloj.AvanzarHasta(HoraDespuesDeCerrar);
            yield return null;
            yield return null;
            yield return null;

            Assert.AreEqual(1, avisosLugar, "EstadoCambio avisa una sola vez");
            Assert.IsFalse(ultimoLugar);
            Assert.AreEqual(1, avisosGlobales, "EventosLugares.EstadoLugarCambio avisa una sola vez");
            Assert.AreEqual(IdLugar.Clandestino, ultimoIdGlobal);
            Assert.IsFalse(ultimoGlobal);
            Assert.AreEqual("CERRADO", Letrero(IdLugar.Clandestino).TextoDeEstado);
        }

        [UnityTest]
        public IEnumerator Letreros_ADistintasHoras_MuestranNombreHorarioYEstado()
        {
            reloj.AvanzarHasta(EscenaLugares.HoraMediodia);
            yield return null;
            yield return null;
            StringAssert.Contains("20:00 a 06:00", Letrero(IdLugar.Clandestino).TextoDeNombre);
            StringAssert.Contains("El Sótano", Letrero(IdLugar.Clandestino).TextoDeNombre);
            Assert.AreEqual("CERRADO", Letrero(IdLugar.Clandestino).TextoDeEstado);
            Assert.AreEqual("ABIERTO", Letrero(IdLugar.Casa).TextoDeEstado);
            StringAssert.Contains("24 horas", Letrero(IdLugar.Casa).TextoDeNombre);
            Assert.AreEqual("CERRADO", Letrero(IdLugar.Casino).TextoDeEstado);
            Assert.AreEqual("CERRADO · PRÓXIMAMENTE", Letrero(IdLugar.MercadoNegro).TextoDeEstado, "Cerrado en demo aunque esté en horario");

            reloj.AvanzarHasta(EscenaLugares.HoraNoche);
            yield return null;
            yield return null;
            Assert.AreEqual("ABIERTO", Letrero(IdLugar.Clandestino).TextoDeEstado);
            Assert.AreEqual("ABIERTO", Letrero(IdLugar.Bar).TextoDeEstado);
            StringAssert.Contains("19:00 a 04:00", Letrero(IdLugar.Bar).TextoDeNombre);
            Assert.AreEqual("ABIERTO", Letrero(IdLugar.Casino).TextoDeEstado);
            StringAssert.Contains("20:00 a 06:00", Letrero(IdLugar.Casino).TextoDeNombre);
        }

        [UnityTest]
        public IEnumerator Reloj_CorriendoRapidoVariasHoras_NoDejaErroresEnLaConsola()
        {
            // Cruza las aperturas de las 19:00, 20:00 y las cierres de la noche con el reloj a x1800; un error de consola falla la prueba.
            reloj.AvanzarHasta(HoraAntesDeAbrir);
            yield return null;
            reloj.EscalaTiempo = EscalaRapida;
            reloj.Pausar(false);
            int cambios = 0;
            EventosLugares.EstadoLugarCambio += (_, _) => cambios++;
            yield return EscenaLugares.Esperar(SegundosCorridaRapida);
            reloj.Pausar(true);
            reloj.EscalaTiempo = 1f;
            Assert.Greater(cambios, 2, "En 10 h de reloj abren y cierran varios locales");
        }
    }
}
