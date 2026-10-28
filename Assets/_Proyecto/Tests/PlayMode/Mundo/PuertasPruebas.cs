using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Mundo.Pruebas;
using MechaCyberpunk.Mundo.Reloj;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>Puertas de los locales con entrada simulada: E abre, W camina, la puerta se cierra sola y respeta horarios.</summary>
    public class PuertasPruebas : InputTestFixture
    {
        private const float SegundosParaPasar = 1f;       // 1 s a 4.5 m/s son más de los 4 m de la tarea
        private const float MetrosMinimosDentro = 0.5f;
        private const float SegundosEsperaCierre = 4f;    // el config cierra a los 3 s sin nadie
        private const float SegundosIntentoCerrada = 4f;
        private const float MetrosMaximosEnLaCalle = -0.3f;

        private Keyboard teclado;
        private RelojMundo reloj;
        private GameObject camaraObjeto;
        private CamaraLibre camara;

        public override void Setup()
        {
            base.Setup();
            teclado = InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
        }

        public override void TearDown()
        {
            if (camaraObjeto != null)
            {
                Object.DestroyImmediate(camaraObjeto);
            }
            EscenaLugares.Descargar();
            base.TearDown();
        }

        private IEnumerator Preparar(float hora, IdLugar frenteA)
        {
            yield return EscenaLugares.CargarCiudad();
            reloj = EscenaLugares.RelojPausado();
            reloj.AvanzarHasta(hora);
            camara = EscenaLugares.ColocarFrenteA(frenteA, out camaraObjeto);
            yield return EscenaLugares.Calentar();
            Assert.AreEqual(hora, reloj.Hora, 0.01f);
        }

        private IEnumerator PresionarE()
        {
            PressAndRelease(teclado.eKey);
            yield return null;
            yield return null;
        }

        private IEnumerator Caminar(float segundos)
        {
            Press(teclado.wKey);
            yield return EscenaLugares.Esperar(segundos);
            Release(teclado.wKey);
        }

        [UnityTest]
        public IEnumerator Bar_A22_EAbreYCaminarPasaElUmbral()
        {
            yield return Preparar(EscenaLugares.HoraNoche, IdLugar.Bar);
            IPuerta puerta = EscenaLugares.Puerta(IdLugar.Bar);
            Assert.IsFalse(puerta.EstaAbierta, "La puerta empieza cerrada");

            yield return PresionarE();
            Assert.AreEqual(ResultadoPuerta.Abierta, camara.UltimoResultadoPuerta);
            Assert.AreEqual(IdLugar.Bar, camara.UltimaPuerta);
            Assert.IsTrue(puerta.EstaAbierta);

            Vector3 inicio = camara.transform.position;
            yield return Caminar(SegundosParaPasar);
            Assert.Greater(Vector3.Distance(inicio, camara.transform.position), 3.5f, "Debió caminar unos 4 m");
            Assert.Greater(EscenaLugares.MetrosDentro(camara, IdLugar.Bar), MetrosMinimosDentro, "La cámara debe pasar el umbral");
        }

        [UnityTest]
        public IEnumerator Clandestino_A22_EAbre()
        {
            yield return Preparar(EscenaLugares.HoraNoche, IdLugar.Clandestino);
            yield return PresionarE();
            Assert.AreEqual(ResultadoPuerta.Abierta, camara.UltimoResultadoPuerta);
            Assert.IsTrue(EscenaLugares.Puerta(IdLugar.Clandestino).EstaAbierta);
        }

        [UnityTest]
        public IEnumerator Puerta_SinNadieEnElUmbral_SeCierraSolaYNoDejaPasar()
        {
            yield return Preparar(EscenaLugares.HoraNoche, IdLugar.Bar);
            IPuerta puerta = EscenaLugares.Puerta(IdLugar.Bar);
            yield return PresionarE();
            yield return Caminar(SegundosParaPasar);
            Assert.IsTrue(puerta.EstaAbierta, "Sigue abierta un momento después de pasar");

            yield return EscenaLugares.Esperar(SegundosEsperaCierre);
            Assert.IsFalse(puerta.EstaAbierta, "Sin nadie en el umbral la puerta se cierra sola");

            camara.Colocar(new Pose(EscenaLugares.Lugar(IdLugar.Bar).PosicionEntrada + Vector3.up * 0.05f,
                Quaternion.LookRotation(EscenaLugares.HaciaLaPuerta(IdLugar.Bar), Vector3.up)));
            yield return null;
            yield return Caminar(SegundosIntentoCerrada);
            Assert.Less(EscenaLugares.MetrosDentro(camara, IdLugar.Bar), MetrosMaximosEnLaCalle, "Cerrada, la puerta no deja pasar");
        }

        [UnityTest]
        public IEnumerator Puerta_ConAlguienEnElUmbral_NoSeCierra()
        {
            yield return Preparar(EscenaLugares.HoraNoche, IdLugar.Bar);
            IPuerta puerta = EscenaLugares.Puerta(IdLugar.Bar);
            yield return PresionarE();
            // Se queda parado en el umbral: la puerta no debe cerrarse en el doble del tiempo de cierre.
            camara.Colocar(new Pose(puerta.Posicion + Vector3.up * 0.05f, Quaternion.LookRotation(EscenaLugares.HaciaLaPuerta(IdLugar.Bar), Vector3.up)));
            yield return EscenaLugares.Esperar(SegundosEsperaCierre * 2f);
            Assert.IsTrue(puerta.EstaAbierta, "Con alguien en el umbral la puerta se queda abierta");
        }

        [UnityTest]
        public IEnumerator Puerta_ElLocalCierraConAlguienEnElUmbral_NoSeCierraEnEseMomento()
        {
            const float HoraDespuesDelCierreDelBar = 5f;
            yield return Preparar(EscenaLugares.HoraNoche, IdLugar.Bar);
            IPuerta puerta = EscenaLugares.Puerta(IdLugar.Bar);
            yield return PresionarE();
            Assert.IsTrue(puerta.EstaAbierta);
            camara.Colocar(new Pose(puerta.Posicion + Vector3.up * 0.05f, Quaternion.LookRotation(EscenaLugares.HaciaLaPuerta(IdLugar.Bar), Vector3.up)));
            yield return null;
            reloj.AvanzarHasta(HoraDespuesDelCierreDelBar);
            yield return null;
            yield return null;
            Assert.IsFalse(EscenaLugares.Lugar(IdLugar.Bar).EstaAbierto, "El bar ya debe estar cerrado por horario");
            Assert.IsTrue(puerta.EstaAbierta, "Con alguien en el umbral la puerta no se cierra al cerrar el local");
        }

        [UnityTest]
        public IEnumerator Clandestino_A12_EDaCerradaPorHorarioYNoDejaPasar()
        {
            yield return Preparar(EscenaLugares.HoraMediodia, IdLugar.Clandestino);
            yield return PresionarE();
            Assert.AreEqual(ResultadoPuerta.CerradaPorHorario, camara.UltimoResultadoPuerta);
            Assert.IsFalse(EscenaLugares.Puerta(IdLugar.Clandestino).EstaAbierta);

            yield return Caminar(SegundosIntentoCerrada);
            Assert.Less(EscenaLugares.MetrosDentro(camara, IdLugar.Clandestino), MetrosMaximosEnLaCalle, "La puerta cerrada bloquea el paso");
        }

        [UnityTest]
        public IEnumerator Casino_A22_EAbre()
        {
            yield return Preparar(EscenaLugares.HoraNoche, IdLugar.Casino);
            yield return PresionarE();
            Assert.AreEqual(ResultadoPuerta.Abierta, camara.UltimoResultadoPuerta);
            Assert.IsTrue(EscenaLugares.Puerta(IdLugar.Casino).EstaAbierta);
        }

        [UnityTest]
        public IEnumerator Casino_A12_EDaCerradaPorHorario()
        {
            yield return Preparar(EscenaLugares.HoraMediodia, IdLugar.Casino);
            yield return PresionarE();
            Assert.AreEqual(ResultadoPuerta.CerradaPorHorario, camara.UltimoResultadoPuerta);
            Assert.IsFalse(EscenaLugares.Puerta(IdLugar.Casino).EstaAbierta);
        }

        [UnityTest]
        public IEnumerator MercadoNegro_A22_EDaCerradaPorHorario()
        {
            yield return Preparar(EscenaLugares.HoraNoche, IdLugar.MercadoNegro);
            yield return PresionarE();
            Assert.AreEqual(ResultadoPuerta.CerradaPorHorario, camara.UltimoResultadoPuerta, "T-320: de noche el mercado cierra por horario");
            Assert.IsFalse(EscenaLugares.Puerta(IdLugar.MercadoNegro).EstaAbierta);
        }

        [UnityTest]
        public IEnumerator MercadoNegro_A12_EAbre()
        {
            yield return Preparar(EscenaLugares.HoraMediodia, IdLugar.MercadoNegro);
            yield return PresionarE();
            Assert.AreEqual(ResultadoPuerta.Abierta, camara.UltimoResultadoPuerta, "T-320: a las 12:00 el mercado abre");
            Assert.IsTrue(EscenaLugares.Puerta(IdLugar.MercadoNegro).EstaAbierta);
        }

        [UnityTest]
        public IEnumerator Casa_A12YA22_EAbre()
        {
            yield return Preparar(EscenaLugares.HoraMediodia, IdLugar.Casa);
            yield return PresionarE();
            Assert.AreEqual(ResultadoPuerta.Abierta, camara.UltimoResultadoPuerta, "La casa abre a las 12:00");
            EscenaLugares.Puerta(IdLugar.Casa).Cerrar();

            reloj.AvanzarHasta(EscenaLugares.HoraNoche);
            yield return null;
            yield return PresionarE();
            Assert.AreEqual(ResultadoPuerta.Abierta, camara.UltimoResultadoPuerta, "La casa abre a las 22:00");
        }

        [UnityTest]
        public IEnumerator Puerta_YaAbierta_EDaYaAbiertaYNoAvisaCambio()
        {
            yield return Preparar(EscenaLugares.HoraNoche, IdLugar.Bar);
            IPuerta puerta = EscenaLugares.Puerta(IdLugar.Bar);
            int avisos = 0;
            puerta.Cambio += _ => avisos++;
            yield return PresionarE();
            Assert.AreEqual(1, avisos);
            // Con la hoja abierta el rayo de E ya no la toca: se le pide directo por el contrato.
            Assert.AreEqual(ResultadoPuerta.YaAbierta, puerta.IntentarAbrir());
            Assert.AreEqual(1, avisos, "Solo avisa Cambio cuando de verdad abre");
        }

        [UnityTest]
        public IEnumerator Bar_AbiertaYElLugarCierraSinJugadorDentro_LaPuertaSeCierra()
        {
            yield return Preparar(EscenaLugares.HoraNoche, IdLugar.Bar);
            IPuerta puerta = EscenaLugares.Puerta(IdLugar.Bar);
            yield return PresionarE();
            Assert.IsTrue(puerta.EstaAbierta);

            // Se aparta del umbral y el reloj pasa las 04:00: el bar cierra y su puerta también.
            camara.Colocar(new Pose(EscenaLugares.Lugar(IdLugar.Bar).PosicionEntrada + EscenaLugares.HaciaLaPuerta(IdLugar.Bar) * -6f,
                Quaternion.identity));
            reloj.AvanzarHasta(5f);
            yield return null;
            yield return null;
            Assert.IsFalse(puerta.EstaAbierta, "Un lugar que cierra cierra su puerta");
        }
    }
}
