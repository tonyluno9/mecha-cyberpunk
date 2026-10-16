using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Interaccion;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Pruebas;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>
    /// Pruebas jugables del HUD al subir al carro de prueba, con el jugador real: avisos, grupos por modo y el filtro por
    /// modo de la interacción (en el carro las puertas no cuentan).
    /// </summary>
    public class HudCarroPruebas : BaseHudObjetivo
    {
        private const string RutaCarro = "Assets/_Proyecto/Personaje/Pruebas/CarroDePrueba.prefab";
        // El PuntoSubida del carro queda 1.5 m al frente del jugador, que mira a +Z.
        private static readonly Vector3 PosicionCarro = new Vector3(2f, 0f, 1.1f);
        private const float DistanciaPuertaAlAsiento = 1.5f;
        private const float AlturaAsiento = 0.1f;

        private GameObject carroObj;
        private CarroDePrueba carro;

        public override void TearDown()
        {
            UnityEngine.Object.Destroy(carroObj);
            base.TearDown();
        }

        private IEnumerator Preparar()
        {
            CrearJugador();
            CrearHud();
            CrearServicio();
            carroObj = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaCarro), PosicionCarro, Quaternion.identity);
            carro = carroObj.GetComponent<CarroDePrueba>();
            yield return Calentar();
            yield return Esperar(0.1f);
        }

        [UnityTest]
        public IEnumerator Carro_FrenteAlCarro_DiceESubirCarroDePrueba()
        {
            yield return Preparar();

            Assert.IsTrue(hud.GrupoInteraccion.activeSelf);
            Assert.AreEqual("E · Subir Carro de prueba", hud.TextoAvisoInteraccion.text);
        }

        [UnityTest]
        public IEnumerator Carro_LejosDelCarro_NoHayAviso()
        {
            yield return Preparar();
            jugador.GetComponent<JugadorAPie>().Colocar(new Pose(new Vector3(-20f, 0.05f, 0f), Quaternion.identity));
            yield return Esperar(0.1f);

            Assert.IsFalse(hud.GrupoInteraccion.activeSelf);
        }

        [UnityTest]
        public IEnumerator Carro_EnElCarro_MuestraObjetivoYCancionYOcultaVidaMiraYArma()
        {
            yield return Preparar();
            CrearLocal(IdLugar.Bar, new Vector3(30f, 0f, 60f), HorarioLugar.Siempre, false, true);
            servicio.Fijar(new Objetivo("Ve al bar", IdLugar.Bar));
            yield return Esperar(0.1f);
            Assert.IsTrue(hud.GrupoVida.activeSelf, "A pie se ve la vida");

            yield return Tecla(teclado.eKey);

            Assert.AreEqual(ModoJugador.Carro, jugador.GetComponent<JugadorAPie>().Modo);
            Assert.IsTrue(hud.GrupoObjetivo.activeSelf, "El objetivo se ve en el carro");
            Assert.AreEqual("Ve al bar", hud.TextoObjetivo.text);
            Assert.IsTrue(hud.GrupoCancion.activeSelf, "La canción se ve en el carro");
            Assert.IsFalse(hud.GrupoVida.activeSelf);
            Assert.IsFalse(hud.GrupoMira.activeSelf);
            Assert.IsFalse(hud.GrupoArma.activeSelf);
            Assert.IsFalse(hud.GrupoInteraccion.activeSelf, "Sin aviso no hay texto de interacción");
        }

        [UnityTest]
        public IEnumerator Carro_EnElCarroConUnaPuertaDeFrente_LaPuertaNoCuentaYLaEBaja()
        {
            yield return Preparar();
            // La puerta del local queda 1.5 m al frente del asiento: a pie estaría a más de 2.5 m, en el carro a 1.5 m.
            Vector3 asiento = carro.Asiento.position;
            GameObject local = CrearLocal(IdLugar.Bar, new Vector3(asiento.x, 0f, asiento.z + DistanciaPuertaAlAsiento),
                HorarioLugar.Siempre, false, true);
            PuertaDePrueba puerta = local.GetComponentInChildren<PuertaDePrueba>();
            JugadorAPie jugadorAPie = jugador.GetComponent<JugadorAPie>();
            InteraccionAPie interaccion = jugador.GetComponent<InteraccionAPie>();
            yield return Tecla(teclado.eKey);
            Assert.AreEqual(ModoJugador.Carro, jugadorAPie.Modo);
            Assert.AreEqual(AlturaAsiento, jugador.transform.position.y - PosicionCarro.y, 0.01f);
            yield return Esperar(0.2f);

            Assert.AreEqual(TipoCandidatoAPie.Ninguno, interaccion.TipoCandidato, "En el carro no hay candidato de puerta");
            Assert.IsNull(interaccion.PuertaCandidata);
            Assert.IsFalse(hud.GrupoInteraccion.activeSelf, "Ni 'E · Abrir' en el HUD");

            yield return Tecla(teclado.eKey);

            Assert.AreEqual(ModoJugador.APie, jugadorAPie.Modo, "La E baja del carro en vez de abrir la puerta");
            Assert.AreEqual(0, puerta.IntentosAbrir, "La puerta no recibió ningún intento");
            Assert.IsFalse(puerta.EstaAbierta);
        }
    }
}
