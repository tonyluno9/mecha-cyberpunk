using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Progresion.Pruebas;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>Dinero, aviso de movimiento y renta vencida en el HUD a pie (T-110).</summary>
    public class HudDineroPruebas : BaseHudObjetivo
    {
        private const string RutaCarro = "Assets/_Proyecto/Personaje/Pruebas/CarroDePrueba.prefab";
        private const float TolerAviso = 0.2f;
        private const float DuracionEsperada = 2.5f;
        private GameObject carroObj;
        private GameObject relojObj;

        public override void TearDown()
        {
            Object.Destroy(carroObj);
            Object.Destroy(relojObj);
            base.TearDown();
        }

        private IEnumerator Preparar()
        {
            CrearJugador();
            CrearHud();
            yield return Calentar();
            yield return Esperar(0.1f);
        }

        [UnityTest]
        public IEnumerator Dinero_ALaVista_MuestraLaCifraInicial()
        {
            yield return Preparar();
            Assert.IsTrue(hud.GrupoDinero.activeSelf);
            Assert.AreEqual("Ƀ500", hud.TextoDinero.text);
            Assert.IsFalse(hud.GrupoAvisoDinero.activeSelf, "Sin movimiento no hay aviso");
            Assert.IsFalse(hud.GrupoRenta.activeSelf, "Sin adeudo no hay recordatorio");
        }

        [UnityTest]
        public IEnumerator Aviso_AbonarPremio_ActualizaLaCifraYElAvisoDuraDosSegundosYMedio()
        {
            yield return Preparar();
            Registro<ICartera>.Actual.Abonar(600, MotivoDinero.Premio);
            float inicio = Time.time;
            yield return null;

            Assert.AreEqual("Ƀ1,100", hud.TextoDinero.text);
            Assert.IsTrue(hud.GrupoAvisoDinero.activeSelf);
            Assert.AreEqual("+Ƀ600 · PREMIO", hud.TextoAvisoDinero.text);

            while (hud.GrupoAvisoDinero.activeSelf && Time.time - inicio < 5f)
            {
                yield return null;
            }
            Assert.AreEqual(DuracionEsperada, Time.time - inicio, TolerAviso, "El aviso dura 2.5 s");
            Assert.AreEqual(DuracionEsperada, config.SegundosAvisoDinero, 0.0001f);
        }

        [UnityTest]
        public IEnumerator Aviso_UnMovimientoNuevo_ReemplazaAlAnteriorYReiniciaElTiempo()
        {
            yield return Preparar();
            ICartera cartera = Registro<ICartera>.Actual;
            cartera.Abonar(600, MotivoDinero.Premio);
            yield return Esperar(1.5f);
            cartera.CargarDeuda(650, MotivoDinero.Reparacion);
            yield return null;
            Assert.AreEqual("-Ƀ650 · REPARACIÓN", hud.TextoAvisoDinero.text);

            yield return Esperar(1.5f);
            Assert.IsTrue(hud.GrupoAvisoDinero.activeSelf, "A 3 s del primero el segundo sigue (lleva 1.5 s)");
            yield return Esperar(1.2f);
            Assert.IsFalse(hud.GrupoAvisoDinero.activeSelf);
        }

        [UnityTest]
        public IEnumerator Dinero_Negativo_SeMuestraConElColorDeDeuda()
        {
            yield return Preparar();
            Assert.AreNotEqual(config.ColorDeuda, hud.TextoDinero.color);
            Registro<ICartera>.Actual.CargarDeuda(650, MotivoDinero.Reparacion);
            yield return null;

            Assert.AreEqual("-Ƀ150", hud.TextoDinero.text);
            Assert.AreEqual(config.ColorDeuda, hud.TextoDinero.color);
        }

        [UnityTest]
        public IEnumerator Aviso_CargarPartida_CambiaLaCifraSinAviso()
        {
            yield return Preparar();
            ((IGuardable)Registro<ICartera>.Actual).Cargar("{\"dinero\":1234}");
            yield return null;

            Assert.AreEqual("Ƀ1,234", hud.TextoDinero.text);
            Assert.IsFalse(hud.GrupoAvisoDinero.activeSelf, "Cargar una partida no es un movimiento");
        }

        [UnityTest]
        public IEnumerator Renta_VencidaSeMuestraYAlPagarSeQuita()
        {
            yield return Preparar();
            relojObj = new GameObject("RelojDePrueba");
            relojObj.AddComponent<RelojDePrueba>();
            yield return null;
            ICartera cartera = Registro<ICartera>.Actual;
            IRenta renta = Registro<IRenta>.Actual;
            cartera.CargarDeuda(500, MotivoDinero.Otro);
            Assert.AreEqual(0, cartera.Dinero);

            for (int i = 0; i < 7; i++)
            {
                yield return Tecla(teclado.jKey);
            }

            Assert.IsTrue(renta.TallerBloqueado);
            Assert.IsTrue(hud.GrupoRenta.activeSelf);
            Assert.AreEqual("RENTA VENCIDA · Ƀ500", hud.TextoRenta.text);

            cartera.Abonar(600, MotivoDinero.Otro);
            Assert.IsTrue(renta.PagarAdeudo());
            yield return null;
            Assert.IsFalse(hud.GrupoRenta.activeSelf, "Al pagar se quita");
        }

        [UnityTest]
        public IEnumerator Carro_Subido_NoSeVeDineroNiAvisoNiRenta()
        {
            yield return Preparar();
            carroObj = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaCarro), new Vector3(2f, 0f, 1.1f), Quaternion.identity);
            yield return Esperar(0.2f);
            Assert.IsTrue(hud.GrupoDinero.activeSelf, "A pie se ve");

            yield return Tecla(teclado.eKey);
            Assert.AreEqual(ModoJugador.Carro, jugador.GetComponent<JugadorAPie>().Modo);
            Assert.IsFalse(hud.GrupoDinero.activeSelf);

            Registro<ICartera>.Actual.Abonar(600, MotivoDinero.Premio);
            yield return null;
            Assert.IsFalse(hud.GrupoDinero.activeSelf);
            Assert.IsFalse(hud.GrupoAvisoDinero.activeSelf, "En el carro tampoco se anuncia");
            Assert.IsFalse(hud.GrupoRenta.activeSelf);
        }

        [UnityTest]
        public IEnumerator Mecha_NoSeVeNadaDeDinero()
        {
            JugadorFalso falso = RegistrarFalso(ModoJugador.APie);
            CrearHud();
            yield return Calentar();
            falso.CambiarModo(ModoJugador.Mecha);
            yield return null;
            Assert.IsFalse(hud.GrupoDinero.activeSelf);
            Assert.IsFalse(hud.GrupoAvisoDinero.activeSelf);
            Assert.IsFalse(hud.GrupoRenta.activeSelf);
        }

        [UnityTest]
        public IEnumerator Dinero_SinCartera_ElGrupoSeOculta()
        {
            RegistrarFalso(ModoJugador.APie);
            CrearHud();
            yield return Calentar();
            Assert.IsNull(Registro<ICartera>.Actual);
            Assert.IsFalse(hud.GrupoDinero.activeSelf);
        }
    }
}
