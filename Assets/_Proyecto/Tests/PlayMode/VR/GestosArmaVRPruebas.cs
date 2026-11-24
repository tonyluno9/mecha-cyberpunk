using System.Collections;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.VR;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.VR
{
    /// <summary>
    /// VR27 con el simulador: la mano derecha en la cadera + grip saca y guarda la pistola; A suelta el cargador, la mano izquierda en la cadera
    /// izquierda + grip toma uno nuevo y acercarlo a la pistola lo mete y recarga. Y las reglas puras de las zonas y de qué arma sale.
    /// </summary>
    public class GestosArmaVRPruebas : BaseJugadorVR
    {
        private static readonly Vector3 Cabeza = new Vector3(0f, 1.6f, 0f);
        private static readonly Vector3 ManoDerReposo = new Vector3(0.2f, 1.2f, 0.3f);
        private static readonly Vector3 ManoIzqReposo = new Vector3(-0.2f, 1.2f, 0.3f);
        private static readonly Vector3 CaderaDer = new Vector3(0.2f, 1.0f, 0f);
        private static readonly Vector3 CaderaIzq = new Vector3(-0.2f, 1.0f, 0f);

        [Test]
        public void Zonas_CaderaHombroYReposo()
        {
            Assert.AreEqual(ZonaArmaVR.CaderaDerecha, ReglaGestosArmaVR.Zona(ReglaGestosArmaVR.EnCabeza(CaderaDer, Cabeza, Vector3.forward), true));
            Assert.AreEqual(ZonaArmaVR.HombroDerecho,
                ReglaGestosArmaVR.Zona(ReglaGestosArmaVR.EnCabeza(new Vector3(0.15f, 1.5f, -0.12f), Cabeza, Vector3.forward), true));
            Assert.AreEqual(ZonaArmaVR.CaderaIzquierda, ReglaGestosArmaVR.Zona(ReglaGestosArmaVR.EnCabeza(CaderaIzq, Cabeza, Vector3.forward), false));
            Assert.AreEqual(ZonaArmaVR.Ninguna, ReglaGestosArmaVR.Zona(ReglaGestosArmaVR.EnCabeza(ManoDerReposo, Cabeza, Vector3.forward), true),
                "Con la mano al frente (apuntando) no hay gesto.");
            Assert.AreEqual(ZonaArmaVR.Ninguna, ReglaGestosArmaVR.Zona(ReglaGestosArmaVR.EnCabeza(CaderaDer, Cabeza, Vector3.forward), false),
                "La mano izquierda no saca armas.");
            // Girada 90° a la derecha, la cadera derecha queda hacia -z del mundo.
            Assert.AreEqual(ZonaArmaVR.CaderaDerecha,
                ReglaGestosArmaVR.Zona(ReglaGestosArmaVR.EnCabeza(new Vector3(0f, 1.0f, -0.2f), Cabeza, Vector3.right), true));
        }

        [Test]
        public void QueSacar_CortaEnCaderaLargaEnHombroYGuarda()
        {
            Assert.AreEqual(ArmaAPie.Pistola, ReglaGestosArmaVR.QueSacar(ZonaArmaVR.CaderaDerecha, ArmaAPie.Punos, ArmaAPie.Pistola, ArmaAPie.Fusil));
            Assert.AreEqual(ArmaAPie.Punos, ReglaGestosArmaVR.QueSacar(ZonaArmaVR.CaderaDerecha, ArmaAPie.Pistola, ArmaAPie.Pistola, ArmaAPie.Fusil));
            Assert.AreEqual(ArmaAPie.Fusil, ReglaGestosArmaVR.QueSacar(ZonaArmaVR.HombroDerecho, ArmaAPie.Pistola, ArmaAPie.Pistola, ArmaAPie.Fusil));
            Assert.AreEqual(ArmaAPie.Punos, ReglaGestosArmaVR.QueSacar(ZonaArmaVR.HombroDerecho, ArmaAPie.RiflePlasma, ArmaAPie.Pistola, ArmaAPie.Fusil));
            Assert.AreEqual(ArmaAPie.Pistola, ReglaGestosArmaVR.QueSacar(ZonaArmaVR.HombroDerecho, ArmaAPie.Pistola, ArmaAPie.Pistola, ArmaAPie.Punos),
                "Sin arma larga el hombro no hace nada.");
        }

        [Test]
        public void ReglaRecarga_SinReservaNoSaleYAOtraVezRecarga()
        {
            Assert.IsFalse(ReglaRecargaManualVR.PuedeRecargar(5, 12, 0, false), "Sin reserva no sale cargador.");
            Assert.IsFalse(ReglaRecargaManualVR.PuedeRecargar(12, 12, 30, false), "Lleno no sale cargador.");
            Assert.IsTrue(ReglaRecargaManualVR.PuedeRecargar(5, 12, -1, false), "Reserva infinita sí.");
            var regla = new ReglaRecargaManualVR();
            Assert.AreEqual(AccionRecargaVR.Nada, regla.Avanzar(false, true, false, false, false, 1f));
            Assert.AreEqual(AccionRecargaVR.SoltarCargador, regla.Avanzar(true, true, false, false, false, 1f));
            Assert.AreEqual(AccionRecargaVR.Nada, regla.Avanzar(true, false, true, true, false, 1f), "El grip fuera de la cadera no toma cargador.");
            Assert.AreEqual(AccionRecargaVR.TomarCargador, regla.Avanzar(true, false, true, true, true, 1f));
            Assert.AreEqual(AccionRecargaVR.TirarCargador, regla.Avanzar(true, false, false, false, false, 1f));
            Assert.AreEqual(AccionRecargaVR.RecargarDirecto, regla.Avanzar(true, true, false, false, false, 1f));
            Assert.AreEqual(EstadoCargadorVR.EnArma, regla.Estado);
        }

        [UnityTest]
        public IEnumerator CaderaDerechaMasGrip_GuardaYSacaLaPistola()
        {
            yield return ArmarVR();
            ArmasAPie armas = jugador.GetComponent<ArmasAPie>();
            GestosArmaVR gestos = jugador.GetComponent<GestosArmaVR>();
            Assert.IsNotNull(gestos, "GestosArmaVR debe agregarse solo al armarse el rig.");
            if (armas.ArmaActiva != ArmaAPie.Pistola)
            {
                Assert.IsTrue(armas.Equipar(ArmaAPie.Pistola));
            }
            yield return Cuadros(3);
            // Grip con la mano al frente: no es gesto.
            yield return Pulsar(controlDer.gripButton);
            Assert.AreEqual(ArmaAPie.Pistola, armas.ArmaActiva, "El grip con la mano al frente no guarda el arma.");

            PonerMano(true, CaderaDer, Quaternion.identity);
            yield return Cuadros(3);
            yield return Pulsar(controlDer.gripButton);
            Assert.AreEqual(ZonaArmaVR.CaderaDerecha, gestos.UltimaZona);
            Assert.AreEqual(ArmaAPie.Punos, armas.ArmaActiva, "Cadera + grip guarda la pistola.");
            yield return Cuadros(3);
            yield return Pulsar(controlDer.gripButton);
            Assert.AreEqual(ArmaAPie.Pistola, armas.ArmaActiva, "Cadera + grip otra vez la saca.");
            Assert.AreEqual(2, gestos.Cambios);
        }

        [UnityTest]
        public IEnumerator A_SueltaCargador_CaderaIzquierdaTomaUno_YAlArmaRecarga()
        {
            CargadorPistola.MunicionInfinita = false;
            yield return ArmarVR();
            ArmasAPie armas = jugador.GetComponent<ArmasAPie>();
            PistolaAPie pistola = jugador.GetComponent<PistolaAPie>();
            RecargaManualVR recarga = jugador.GetComponent<RecargaManualVR>();
            Assert.IsNotNull(recarga, "RecargaManualVR debe agregarse sola al armarse el rig.");
            if (armas.ArmaActiva != ArmaAPie.Pistola)
            {
                Assert.IsTrue(armas.Equipar(ArmaAPie.Pistola));
            }
            yield return Cuadros(3);
            yield return Pulsar(controlDer.triggerButton);
            yield return Cuadros(2);
            Assert.AreEqual(pistola.TamanoCargador - 1, pistola.Balas, "Antes de recargar falta una bala.");

            yield return Pulsar(controlDer.primaryButton);
            Assert.AreEqual(EstadoCargadorVR.Fuera, recarga.Estado, "A suelta el cargador.");
            Assert.IsNotNull(recarga.UltimoSoltado, "El cargador soltado cae como objeto.");
            Assert.IsNotNull(recarga.UltimoSoltado.GetComponent<Rigidbody>(), "El cargador cae con física.");

            PonerMano(false, CaderaIzq, Quaternion.identity);
            yield return Cuadros(3);
            Mantener(controlIzq.gripButton);
            yield return Cuadros(2);
            Assert.AreEqual(EstadoCargadorVR.EnMano, recarga.Estado, "Cadera izquierda + grip toma un cargador nuevo.");
            Assert.IsNotNull(recarga.CargadorEnMano);
            Assert.IsTrue(recarga.CargadorEnMano.transform.IsChildOf(adaptador.ManoIzquierda.transform), "El cargador nuevo va en la mano izquierda.");

            PonerMano(false, ManoDerReposo, Quaternion.identity);
            yield return Cuadros(3);
            Assert.AreEqual(EstadoCargadorVR.EnArma, recarga.Estado, "Acercarlo al arma lo mete.");
            Assert.AreEqual(1, recarga.Metidos);
            Assert.IsTrue(pistola.Recargando || pistola.Balas == pistola.TamanoCargador, "Meter el cargador recarga.");
            Soltar(controlIzq.gripButton);
            PonerMano(false, ManoIzqReposo, Quaternion.identity);
            float fin = Time.time + 4f;
            while (pistola.Recargando && Time.time < fin)
            {
                yield return null;
            }
            Assert.AreEqual(pistola.TamanoCargador, pistola.Balas, "Tras meter el cargador queda lleno.");
        }
    }
}
