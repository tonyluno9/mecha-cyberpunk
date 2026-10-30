using System;
using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.UI.Hud;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>Pruebas jugables del objetivo en el HUD: texto, ángulo, distancia y REGRESA con entrada simulada (la flecha la dibuja el marcador, T-408).</summary>
    public class HudObjetivoPruebas : BaseHudObjetivo
    {
        private const float DistanciaBarAlFrente = 60f;
        private const float DistanciaPuertaAEntrada = 1.5f;
        private const float DistanciaAlejarse = 40f;
        private const float MaxSegundosCaminata = 20f;
        private const float SegundosParaRegresa = 4f;
        private const float MaxSegundosVolver = 15f;
        private const float ToleranciaAnguloCalculado = 5f;
        private const float ToleranciaAnguloGiro = 10f;
        private const float ToleranciaDistancia = 1f;
        private const float MediaVuelta = 180f;
        private const float CuartoDeVuelta = 90f;

        private IEnumerator Preparar(Vector3 posicionBar)
        {
            CrearJugador();
            CrearHud();
            CrearServicio();
            CrearLocal(IdLugar.Bar, posicionBar, HorarioLugar.Siempre, false, true);
            yield return Calentar();
        }

        private static Vector3 PuertaAlFrente => new Vector3(0f, 0f, DistanciaBarAlFrente);

        private float AnguloEsperado()
        {
            Transform camara = jugador.GetComponentInChildren<Camera>().transform;
            return CalculoFlechaObjetivo.Angulo(camara.position, camara.forward, servicio.Posicion);
        }

        [UnityTest]
        public IEnumerator Fijar_VeAlBar_ElTextoApareceEnElMismoFrameOElSiguiente()
        {
            yield return Preparar(PuertaAlFrente);
            Assert.IsFalse(hud.GrupoObjetivo.activeSelf, "Sin objetivo está oculto");

            servicio.Fijar(new Objetivo("Ve al bar", IdLugar.Bar));
            yield return null;

            Assert.IsTrue(hud.GrupoObjetivo.activeSelf);
            Assert.AreEqual("Ve al bar", hud.TextoObjetivo.text);
        }

        [UnityTest]
        public IEnumerator Flecha_ObjetivoAlFrente_ApuntaArribaConAnguloCero()
        {
            yield return Preparar(PuertaAlFrente);
            servicio.Fijar(new Objetivo("Ve al bar", IdLugar.Bar));
            yield return Esperar(0.2f);

            Assert.IsTrue(hud.HayPosicionObjetivo);
            Assert.AreEqual(0f, hud.AnguloFlecha, ToleranciaAnguloCalculado);
        }

        [UnityTest]
        public IEnumerator Flecha_ObjetivoEnDiagonalALaDerecha_MarcaElAnguloCalculado()
        {
            yield return Preparar(new Vector3(40f, 0f, 40f));
            servicio.Fijar(new Objetivo("Ve al bar", IdLugar.Bar));
            yield return Esperar(0.2f);

            Assert.AreEqual(AnguloEsperado(), hud.AnguloFlecha, ToleranciaAnguloCalculado);
            Assert.AreEqual(45f, hud.AnguloFlecha, ToleranciaAnguloCalculado + 2f, "Está a unos 45 grados a la derecha");
        }

        [UnityTest]
        public IEnumerator Flecha_GirarNoventaALaDerechaConElRaton_LaFlechaGiraMenosNoventa()
        {
            yield return Preparar(PuertaAlFrente);
            servicio.Fijar(new Objetivo("Ve al bar", IdLugar.Bar));
            yield return Esperar(0.2f);
            Assert.AreEqual(0f, hud.AnguloFlecha, ToleranciaAnguloCalculado);

            yield return Girar(CuartoDeVuelta);
            yield return null;

            Assert.AreEqual(-CuartoDeVuelta, hud.AnguloFlecha, ToleranciaAnguloGiro);
            Assert.AreEqual(AnguloEsperado(), hud.AnguloFlecha, ToleranciaAnguloCalculado);
        }

        [UnityTest]
        public IEnumerator Distancia_Mostrada_CoincideConLaDelSeguimiento()
        {
            yield return Preparar(new Vector3(40f, 0f, 40f));
            servicio.Fijar(new Objetivo("Ve al bar", IdLugar.Bar));
            yield return Esperar(0.3f);

            string texto = hud.TextoDistancia;
            StringAssert.EndsWith(" m", texto);
            float mostrada = float.Parse(texto.Substring(0, texto.Length - 2));
            Assert.AreEqual(servicio.Distancia, mostrada, ToleranciaDistancia);
            Assert.Greater(mostrada, 50f, "Del origen a (40, 38.5) hay unos 55 m");
        }

        [UnityTest]
        public IEnumerator Regresa_CaminandoCuarentaMetrosAlReves_AparecePorLoMenosEn4sYDesapareceAlVolver()
        {
            yield return Preparar(PuertaAlFrente);
            servicio.Fijar(new Objetivo("Ve al bar", IdLugar.Bar));
            yield return Esperar(0.2f);
            StringAssert.DoesNotContain(config.TextoRegresa, hud.TextoObjetivo.text);

            // Media vuelta y a caminar en dirección contraria al objetivo.
            yield return Girar(MediaVuelta);
            Press(teclado.wKey);
            float inicio = Time.time;
            while (jugador.transform.position.z > -DistanciaAlejarse && Time.time - inicio < MaxSegundosCaminata)
            {
                yield return null;
            }
            Assert.LessOrEqual(jugador.transform.position.z, -DistanciaAlejarse, "Caminó los 40 m");

            float despues = Time.time;
            while (!hud.TextoObjetivo.text.Contains(config.TextoRegresa) && Time.time - despues < SegundosParaRegresa)
            {
                yield return null;
            }
            Release(teclado.wKey);
            yield return null;
            StringAssert.StartsWith(config.TextoRegresa + " · ", hud.TextoObjetivo.text);
            StringAssert.EndsWith("Ve al bar", hud.TextoObjetivo.text);
            Assert.AreEqual(config.ColorRegresa, hud.TextoObjetivo.color);

            // Media vuelta y a caminar hacia el objetivo: REGRESA se va.
            yield return Girar(MediaVuelta);
            Press(teclado.wKey);
            float inicioVuelta = Time.time;
            while (hud.TextoObjetivo.text.Contains(config.TextoRegresa) && Time.time - inicioVuelta < MaxSegundosVolver)
            {
                yield return null;
            }
            Release(teclado.wKey);
            yield return null;
            Assert.AreEqual("Ve al bar", hud.TextoObjetivo.text);
            Assert.AreEqual(MechaCyberpunk.UI.Kit.KitUI.Estilo.Naranja, hud.TextoObjetivo.color, "El objetivo va en el naranja del kit");
        }

        [UnityTest]
        public IEnumerator Quitar_ElObjetivoSeOculta()
        {
            yield return Preparar(PuertaAlFrente);
            servicio.Fijar(new Objetivo("Ve al bar", IdLugar.Bar));
            yield return Esperar(0.2f);
            Assert.IsTrue(hud.GrupoObjetivo.activeSelf);

            servicio.Quitar();
            yield return null;

            Assert.IsFalse(hud.GrupoObjetivo.activeSelf);
        }

        [UnityTest]
        public IEnumerator Modo_Carro_ElObjetivoSeVeYLaInteraccionNo()
        {
            CrearHud();
            CrearServicio();
            JugadorFalso falso = RegistrarFalso(ModoJugador.Carro);
            CrearLocal(IdLugar.Bar, PuertaAlFrente, HorarioLugar.Siempre, false, true);
            yield return Calentar();

            servicio.Fijar(new Objetivo("Ve al bar", IdLugar.Bar));
            yield return Esperar(0.2f);

            Assert.IsTrue(hud.GrupoObjetivo.activeSelf, "En carro el objetivo se ve");
            Assert.IsTrue(hud.HayPosicionObjetivo, "También la distancia, medida desde el jugador");
            Assert.IsFalse(hud.GrupoInteraccion.activeSelf, "En carro no hay aviso de interacción");
            Assert.AreSame(falso, Registro<IJugador>.Actual);
        }

        [UnityTest]
        public IEnumerator Modo_Mecha_NoSeVeNada_YAlVolverAPieVuelveElObjetivo()
        {
            CrearHud();
            CrearServicio();
            JugadorFalso falso = RegistrarFalso(ModoJugador.Mecha);
            CrearLocal(IdLugar.Bar, PuertaAlFrente, HorarioLugar.Siempre, false, true);
            yield return Calentar();

            servicio.Fijar(new Objetivo("Ve al bar", IdLugar.Bar));
            yield return Esperar(0.2f);
            Assert.IsFalse(hud.GrupoObjetivo.activeSelf);
            Assert.IsFalse(hud.GrupoInteraccion.activeSelf);
            Assert.IsFalse(hud.GrupoCancion.activeSelf);

            falso.CambiarModo(ModoJugador.APie);
            Assert.IsTrue(hud.GrupoObjetivo.activeSelf);
        }

        private class ObjetivosSoloTexto : IObjetivos
        {
            public bool HayObjetivo => true;
            public Objetivo Actual => new Objetivo("Solo texto", IdLugar.Bar);
            public event Action<Objetivo> Cambio { add { } remove { } }
        }

        [UnityTest]
        public IEnumerator OtraImplementacion_DeObjetivos_MuestraSoloElTextoSinFlecha()
        {
            CrearHud();
            RegistrarFalso(ModoJugador.APie);
            Registro<IObjetivos>.Agregar(new ObjetivosSoloTexto());
            yield return Calentar();

            Assert.IsTrue(hud.GrupoObjetivo.activeSelf);
            Assert.AreEqual("Solo texto", hud.TextoObjetivo.text);
            Assert.IsFalse(hud.HayPosicionObjetivo);
            Assert.AreEqual(string.Empty, hud.TextoDistancia);
        }

        [UnityTest]
        public IEnumerator Objetivo_SeFijaAntesQueElHud_ElHudLoMuestraAlNacer()
        {
            CrearJugador(false);
            CrearServicio();
            CrearLocal(IdLugar.Bar, PuertaAlFrente, HorarioLugar.Siempre, false, true);
            yield return null;
            servicio.Fijar(new Objetivo("Ve al bar", IdLugar.Bar));
            yield return null;

            CrearHud();
            yield return Calentar();

            Assert.IsTrue(hud.GrupoObjetivo.activeSelf);
            Assert.AreEqual("Ve al bar", hud.TextoObjetivo.text);
        }
    }
}
