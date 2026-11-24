using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Jugador;
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
        public void T95_Espalda_ZonaAmpliaEncimaODetrasDelHombro()
        {
            Vector3[] espalda =
            {
                new Vector3(0.15f, -0.1f, -0.12f), // sobre el hombro, atrás
                new Vector3(0.25f, 0.05f, -0.2f), // detrás, a la altura de los ojos
                new Vector3(0.1f, 0.3f, 0.05f), // encima de la cabeza
                new Vector3(0f, 0.2f, -0.3f), // detrás de la nuca, alto
                new Vector3(0.35f, -0.3f, -0.15f) // detrás del hombro, bajo
            };
            foreach (Vector3 p in espalda)
            {
                Assert.AreEqual(ZonaArmaVR.HombroDerecho, ReglaGestosArmaVR.Zona(p, true), "Espalda en " + p);
            }
            Vector3[] fuera =
            {
                new Vector3(0.2f, -0.4f, 0.3f), // apuntando al frente
                new Vector3(0.2f, 0.1f, 0.5f), // apuntando arriba al frente
                new Vector3(0.7f, -0.1f, -0.1f), // brazo estirado al lado
                new Vector3(0.25f, -0.6f, -0.1f) // cadera: no es espalda
            };
            foreach (Vector3 p in fuera)
            {
                Assert.AreNotEqual(ZonaArmaVR.HombroDerecho, ReglaGestosArmaVR.Zona(p, true), "No es espalda en " + p);
            }
            Assert.AreEqual(ZonaArmaVR.CaderaDerecha, ReglaGestosArmaVR.Zona(new Vector3(0.3f, -0.75f, 0.1f), true), "Cadera holgada.");
            // Mirando casi recto al piso, el giro sale de la coronilla: la espalda sigue detrás.
            Vector3 abajo = new Vector3(0f, -1f, 0.01f).normalized;
            Vector3 arribaAbajo = new Vector3(0f, 0.01f, 1f).normalized;
            Vector3 local = ReglaGestosArmaVR.EnCabeza(Cabeza + new Vector3(0.2f, 0.05f, -0.2f), Cabeza, abajo, arribaAbajo);
            Assert.AreEqual(ZonaArmaVR.HombroDerecho, ReglaGestosArmaVR.Zona(local, true), "Mirando al piso la espalda no brinca.");
        }

        [Test]
        public void T95_ElegirLarga_FusilLuegoEscopetaLuegoLasDemas()
        {
            Assert.AreEqual(ArmaAPie.Fusil, ReglaGestosArmaVR.ElegirLarga(a => a == ArmaAPie.Fusil || a == ArmaAPie.Escopeta || a == ArmaAPie.Sniper));
            Assert.AreEqual(ArmaAPie.Escopeta, ReglaGestosArmaVR.ElegirLarga(a => a == ArmaAPie.Escopeta || a == ArmaAPie.Sniper));
            Assert.AreEqual(ArmaAPie.Sniper, ReglaGestosArmaVR.ElegirLarga(a => a == ArmaAPie.Sniper || a == ArmaAPie.Pistola));
            Assert.AreEqual(ArmaAPie.Punos, ReglaGestosArmaVR.ElegirLarga(a => a == ArmaAPie.Pistola || a == ArmaAPie.Punos));
            Assert.AreEqual(ArmaAPie.Revolver, ReglaGestosArmaVR.ElegirCorta(a => a == ArmaAPie.Revolver || a == ArmaAPie.Pistola, ArmaAPie.Revolver));
            Assert.AreEqual(ArmaAPie.Pistola, ReglaGestosArmaVR.ElegirCorta(a => a == ArmaAPie.Pistola, ArmaAPie.Revolver));
        }

        [Test]
        public void T95_Funda_SacaAlPulsarYGuardaAlSoltarEnSuZona()
        {
            var funda = new ReglaFundaVR();
            ArmaAPie mano = ArmaAPie.Punos;
            for (int i = 0; i < 5; i++)
            {
                mano = funda.Avanzar(true, false, ZonaArmaVR.CaderaDerecha, mano, ArmaAPie.Pistola, ArmaAPie.Fusil);
                Assert.AreEqual(ArmaAPie.Pistola, mano, "Pulsar en la cadera saca la pistola.");
                mano = funda.Avanzar(false, false, ZonaArmaVR.Ninguna, mano, ArmaAPie.Punos, ArmaAPie.Punos);
                mano = funda.Avanzar(false, true, ZonaArmaVR.CaderaDerecha, mano, ArmaAPie.Punos, ArmaAPie.Punos);
                Assert.AreEqual(ArmaAPie.Punos, mano, "Volver a la cadera y soltar la guarda.");
                Assert.IsTrue(funda.Guardo);

                mano = funda.Avanzar(true, false, ZonaArmaVR.HombroDerecho, mano, ArmaAPie.Pistola, ArmaAPie.Fusil);
                Assert.AreEqual(ArmaAPie.Fusil, mano, "Pulsar en la espalda saca el fusil.");
                mano = funda.Avanzar(false, false, ZonaArmaVR.Ninguna, mano, ArmaAPie.Punos, ArmaAPie.Punos);
                mano = funda.Avanzar(false, true, ZonaArmaVR.HombroDerecho, mano, ArmaAPie.Punos, ArmaAPie.Punos);
                Assert.AreEqual(ArmaAPie.Punos, mano, "Volver a la espalda y soltar lo guarda.");
            }

            // Sacar y soltar sin salir de la zona no la guarda al instante.
            mano = funda.Avanzar(true, false, ZonaArmaVR.CaderaDerecha, ArmaAPie.Punos, ArmaAPie.Pistola, ArmaAPie.Fusil);
            mano = funda.Avanzar(false, true, ZonaArmaVR.CaderaDerecha, mano, ArmaAPie.Punos, ArmaAPie.Punos);
            Assert.AreEqual(ArmaAPie.Pistola, mano, "Sacar y soltar ahí mismo deja la pistola en la mano.");
            Assert.IsFalse(funda.Guardo);
            // Soltar fuera de la zona no guarda; la pistola en la espalda tampoco.
            mano = funda.Avanzar(true, false, ZonaArmaVR.Ninguna, mano, ArmaAPie.Punos, ArmaAPie.Punos);
            mano = funda.Avanzar(false, true, ZonaArmaVR.HombroDerecho, mano, ArmaAPie.Punos, ArmaAPie.Punos);
            Assert.AreEqual(ArmaAPie.Pistola, mano, "La pistola no se guarda en la espalda.");
            // Con la pistola en la mano, pulsar y soltar en la cadera la guarda.
            mano = funda.Avanzar(true, false, ZonaArmaVR.CaderaDerecha, mano, ArmaAPie.Pistola, ArmaAPie.Fusil);
            Assert.AreEqual(ArmaAPie.Pistola, mano, "Pulsar con la pistola en la cadera no la cambia todavía.");
            mano = funda.Avanzar(false, true, ZonaArmaVR.CaderaDerecha, mano, ArmaAPie.Punos, ArmaAPie.Punos);
            Assert.AreEqual(ArmaAPie.Punos, mano);
            // Con la pistola en la mano, pulsar en la espalda cambia al fusil y soltar ahí no lo guarda.
            mano = funda.Avanzar(true, false, ZonaArmaVR.HombroDerecho, ArmaAPie.Pistola, ArmaAPie.Pistola, ArmaAPie.Fusil);
            Assert.AreEqual(ArmaAPie.Fusil, mano);
            mano = funda.Avanzar(false, true, ZonaArmaVR.HombroDerecho, mano, ArmaAPie.Punos, ArmaAPie.Punos);
            Assert.AreEqual(ArmaAPie.Fusil, mano);
            // Soltar sin haber pulsado (p. ej. tras una pausa) no hace nada.
            funda.Reiniciar();
            Assert.AreEqual(ArmaAPie.Fusil, funda.Avanzar(false, true, ZonaArmaVR.HombroDerecho, ArmaAPie.Fusil, ArmaAPie.Punos, ArmaAPie.Punos));
        }

        [UnityTest]
        public IEnumerator T95_CincoVeces_PistolaEnLaCaderaYFusilEnLaEspalda()
        {
            yield return ArmarVR();
            ArmasAPie armas = jugador.GetComponent<ArmasAPie>();
            GestosArmaVR gestos = jugador.GetComponent<GestosArmaVR>();
            Assert.IsNotNull(gestos);
            ArsenalJugador arsenal = Registro<ArsenalJugador>.Actual;
            Assert.IsNotNull(arsenal, "Hay arsenal.");
            Registro<ServicioInventario>.Actual.Agregar(arsenal.Catalogo.Buscar(ArmaAPie.Fusil, 0), 1);
            yield return Cuadros(2);
            Assert.IsTrue(armas.TieneArma(ArmaAPie.Fusil), "El jugador tiene el fusil.");
            Assert.IsTrue(armas.TieneArma(ArmaAPie.Pistola), "El jugador tiene la pistola.");
            armas.Equipar(ArmaAPie.Punos);
            yield return Cuadros(3);
            Vector3 espalda = new Vector3(0.2f, 1.65f, -0.15f);
            for (int i = 0; i < 5; i++)
            {
                yield return SacarYGuardar(armas, CaderaDer, ArmaAPie.Pistola, "Pistola, vuelta " + i);
                yield return SacarYGuardar(armas, espalda, ArmaAPie.Fusil, "Fusil, vuelta " + i);
            }
            Assert.AreEqual(10, gestos.Guardadas, "Se guardó 10 veces.");
            Assert.AreEqual(20, gestos.Cambios, "10 sacadas y 10 guardadas.");
        }

        private IEnumerator SacarYGuardar(ArmasAPie armas, Vector3 zona, ArmaAPie esperada, string que)
        {
            PonerMano(true, zona, Quaternion.identity);
            yield return Cuadros(3);
            Mantener(controlDer.gripButton);
            yield return Cuadros(2);
            Assert.AreEqual(esperada, armas.ArmaActiva, que + ": grip en la zona la saca.");
            PonerMano(true, ManoDerReposo, Quaternion.identity);
            yield return Cuadros(4);
            Assert.AreEqual(esperada, armas.ArmaActiva, que + ": sigue en la mano al apuntar.");
            PonerMano(true, zona, Quaternion.identity);
            yield return Cuadros(3);
            Soltar(controlDer.gripButton);
            yield return Cuadros(2);
            Assert.AreEqual(ArmaAPie.Punos, armas.ArmaActiva, que + ": soltar el grip en la zona la guarda.");
            yield return Cuadros(2);
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
