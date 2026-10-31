using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Mundo.Lugares;
using MechaCyberpunk.Mundo.Reloj;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-350: un día entero de reloj sobre Ciudad. Estado, avisos, letreros y puertas de cada local siguen su horario, también tras cargar el reloj.</summary>
    public class CicloHorariosPruebas
    {
        private const float PasoHoras = 0.5f;
        private const float HorasPorDia = 24f;
        private const float HoraGuardada = 18.5f;
        private const float HoraPosterior = 19.5f;
        private const int FramesPorPaso = 2;

        private struct Horario
        {
            public IdLugar Id;
            public float Abre;
            public float Cierra;
            public bool Siempre;
            public int CambiosPorDia;
        }

        // Los horarios que el mundo ya define (Casa 24 h, Bar 19 a 04, Clandestino y Casino 20 a 06, Mercado negro y Clínica 09 a 18).
        private static readonly Horario[] Tabla =
        {
            new Horario { Id = IdLugar.Casa, Siempre = true, CambiosPorDia = 0 },
            new Horario { Id = IdLugar.Bar, Abre = 19f, Cierra = 4f, CambiosPorDia = 2 },
            new Horario { Id = IdLugar.Clandestino, Abre = 20f, Cierra = 6f, CambiosPorDia = 2 },
            new Horario { Id = IdLugar.Casino, Abre = 20f, Cierra = 6f, CambiosPorDia = 2 },
            new Horario { Id = IdLugar.MercadoNegro, Abre = 9f, Cierra = 18f, CambiosPorDia = 2 },
            new Horario { Id = IdLugar.Clinica, Abre = 9f, Cierra = 18f, CambiosPorDia = 2 },
        };

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

        private static bool Esperado(Horario h, float hora)
        {
            if (h.Siempre)
            {
                return true;
            }
            return h.Abre < h.Cierra ? hora >= h.Abre && hora < h.Cierra : hora >= h.Abre || hora < h.Cierra;
        }

        private static IPuerta PuertaOpcional(IdLugar id)
        {
            foreach (IPuerta puerta in Registro<IPuerta>.Todos)
            {
                if (puerta.Lugar == id)
                {
                    return puerta;
                }
            }
            return null;
        }

        private static LetreroLugar LetreroOpcional(IdLugar id)
        {
            return ((Component)EscenaLugares.Lugar(id)).GetComponentInChildren<LetreroLugar>();
        }

        private static IEnumerator Frames()
        {
            for (int i = 0; i < FramesPorPaso; i++)
            {
                yield return null;
            }
        }

        // Comprueba lugar, letrero y puerta de un horario a la hora que marca el reloj.
        private static void ComprobarEstado(Horario h, float hora)
        {
            bool abierto = Esperado(h, hora);
            string cuando = h.Id + " a las " + hora.ToString("0.0");
            ILugar lugar = EscenaLugares.Lugar(h.Id);
            Assert.AreEqual(abierto, lugar.EstaAbierto, cuando + ": EstaAbierto");

            LetreroLugar letrero = LetreroOpcional(h.Id);
            if (letrero != null)
            {
                Assert.AreEqual(abierto ? "ABIERTO" : "CERRADO", letrero.TextoDeEstado, cuando + ": letrero");
            }

            IPuerta puerta = PuertaOpcional(h.Id);
            if (puerta != null)
            {
                ResultadoPuerta resultado = puerta.IntentarAbrir();
                Assert.AreEqual(abierto ? ResultadoPuerta.Abierta : ResultadoPuerta.CerradaPorHorario, resultado, cuando + ": puerta");
                puerta.Cerrar();
            }
        }

        [UnityTest]
        public IEnumerator Barrido24Horas_CadaLocalSigueSuHorario_EnLugarLetreroYPuerta()
        {
            reloj.AvanzarHasta(0f);
            yield return Frames();
            for (float hora = 0f; hora < HorasPorDia; hora += PasoHoras)
            {
                reloj.AvanzarHasta(hora);
                yield return Frames();
                foreach (Horario h in Tabla)
                {
                    ComprobarEstado(h, hora);
                }
            }
        }

        [UnityTest]
        public IEnumerator Barrido24Horas_CadaLocalAvisaSusCambios_UnoPorCruce()
        {
            reloj.AvanzarHasta(0f);
            yield return Frames();
            int[] porLugar = new int[Tabla.Length];
            int[] porEvento = new int[Tabla.Length];
            for (int i = 0; i < Tabla.Length; i++)
            {
                int indice = i;
                EscenaLugares.Lugar(Tabla[i].Id).EstadoCambio += _ => porLugar[indice]++;
            }
            EventosLugares.EstadoLugarCambio += (id, _) =>
            {
                for (int i = 0; i < Tabla.Length; i++)
                {
                    if (Tabla[i].Id == id)
                    {
                        porEvento[i]++;
                    }
                }
            };

            for (float hora = PasoHoras; hora < HorasPorDia; hora += PasoHoras)
            {
                reloj.AvanzarHasta(hora);
                yield return Frames();
            }
            reloj.AvanzarHasta(0f);
            yield return Frames();

            for (int i = 0; i < Tabla.Length; i++)
            {
                Assert.AreEqual(Tabla[i].CambiosPorDia, porLugar[i], Tabla[i].Id + ": avisos de EstadoCambio en un día");
                Assert.AreEqual(Tabla[i].CambiosPorDia, porEvento[i], Tabla[i].Id + ": avisos de EventosLugares en un día");
            }
        }

        [UnityTest]
        public IEnumerator Clinica_ConservaElMostradorDeTony_AbiertaYCerrada()
        {
            ILugar clinica = EscenaLugares.Lugar(IdLugar.Clinica);
            Transform raiz = ((Component)clinica).transform.parent;
            foreach (float hora in new[] { 12f, 22f, 12f })
            {
                reloj.AvanzarHasta(hora);
                yield return Frames();
                Assert.IsNotNull(raiz.Find("MostradorClinica"), "El mostrador sigue colocado a las " + hora);
            }
        }

        [UnityTest]
        public IEnumerator GuardarYCargarElReloj_RestauraElEstadoDeLosLocales_YElSiguienteCruceSeAvisaUnaVez()
        {
            reloj.AvanzarHasta(HoraGuardada);
            yield return Frames();
            string guardado = reloj.Guardar();

            reloj.AvanzarHasta(HoraPosterior);
            yield return Frames();
            Assert.IsTrue(EscenaLugares.Lugar(IdLugar.Bar).EstaAbierto, "Después de las 19:00 el bar abre");

            reloj.Cargar(guardado);
            yield return Frames();
            Assert.AreEqual(HoraGuardada, reloj.Hora, 0.01f, "El reloj vuelve a las 18:30");
            foreach (Horario h in Tabla)
            {
                ComprobarEstado(h, HoraGuardada);
            }

            int avisosBar = 0;
            EscenaLugares.Lugar(IdLugar.Bar).EstadoCambio += abierto => avisosBar += abierto ? 1 : 0;
            reloj.AvanzarHasta(HoraPosterior);
            yield return Frames();
            Assert.AreEqual(1, avisosBar, "El cruce de las 19:00 se avisa una sola vez tras cargar");
            foreach (Horario h in Tabla)
            {
                ComprobarEstado(h, HoraPosterior);
            }
        }
    }
}
