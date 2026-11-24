using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion.Partida;
using MechaCyberpunk.VR;
using MechaCyberpunk.VR.Guia;
using MechaCyberpunk.VR.Tutorial;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.VR
{
    /// <summary>
    /// VR62: el tutorial de tarjetas de VR. La fila sale en orden y cada tarjeta una vez por partida (avisos únicos); la tarjeta se ve abajo y a
    /// un lado de la vista con su imagen, frase y APRENDIDO, no detiene el juego fuera de las peleas y se cierra con APRENDIDO o al hacer el gesto.
    /// </summary>
    public class VR62TutorialPruebas : BaseJugadorVR
    {
        // ---- Lógica pura ----

        [Test]
        public void Regla_PideEnOrden_SinRepetirNiAprendidas()
        {
            var aprendidas = new HashSet<string> { ReglaTutorialVR.Clave(PaginaGuiaVR.Granada) };
            var regla = new ReglaTutorialVR(aprendidas.Contains);
            Assert.AreEqual(4, regla.Pedir(ReglaTutorialVR.CampoTiro), "La granada ya se aprendió.");
            Assert.IsFalse(regla.Pedir(PaginaGuiaVR.Disparar), "No se repite en la fila.");
            Assert.IsFalse(regla.Pedir(PaginaGuiaVR.Botones), "La de botones no es tarjeta.");
            Assert.IsTrue(regla.Siguiente(out PaginaGuiaVR p));
            Assert.AreEqual(PaginaGuiaVR.Disparar, p, "El campo de tiro empieza con DISPARAR.");
            regla.Quitar(p);
            aprendidas.Add(ReglaTutorialVR.Clave(PaginaGuiaVR.Recargar));
            Assert.IsTrue(regla.Siguiente(out p));
            Assert.AreEqual(PaginaGuiaVR.ArmaLarga, p, "Se salta la que se aprendió mientras esperaba.");
            Assert.AreEqual("vr_tarjeta_moversegirar", ReglaTutorialVR.Clave(PaginaGuiaVR.MoverseGirar));
        }

        [Test]
        public void Paginas_SonDieciseisConImagenYTexto()
        {
            Assert.AreEqual(16, TextosGuiaVR.Paginas.Length);
            Assert.AreEqual(16, System.Enum.GetValues(typeof(PaginaGuiaVR)).Length);
            foreach (var p in TextosGuiaVR.Paginas)
            {
                Assert.IsFalse(string.IsNullOrEmpty(p.imagen));
                Assert.IsFalse(p.imagen.EndsWith("_v1"), p.imagen);
                Assert.IsFalse(string.IsNullOrEmpty(p.explicacion), p.imagen);
                Assert.IsNotNull(Resources.Load<Texture2D>(TextosGuiaVR.CarpetaImagenes + "/" + p.imagen), "Falta en Resources/GuiaVR: " + p.imagen);
                Assert.IsFalse(p.frase.ToUpperInvariant().Contains("RUEDA DE PRÓTESIS"), p.frase);
            }
            StringAssert.Contains("GRIP", TextosGuiaVR.De(PaginaGuiaVR.RuedaArmas).frase, "VR31b: la rueda de armas se mueve con el grip.");
            StringAssert.Contains("CABEZA", TextosGuiaVR.De(PaginaGuiaVR.ProtesisVision).frase);
            foreach (var e in TextosGuiaVR.Etiquetas)
            {
                Assert.IsFalse(e.texto.Contains("RUEDA DE PRÓTESIS"), e.texto);
            }
        }

        [Test]
        public void Traducir_AvisosDeLaRuedaDeProtesis_DicenLosGestos()
        {
            string soloMenu = TeclaVR.Traducir("O MANTÉN GRIP + GATILLO IZQUIERDOS PARA EL MENÚ DE PRÓTESIS");
            Assert.AreEqual(TeclaVR.AyudaProtesis, soloMenu);
            string conGesto = TeclaVR.Traducir("MANOS EN LOS MUSLOS + LOS DOS GRIPS: PIERNAS · O MANTÉN GRIP + GATILLO IZQUIERDOS PARA EL MENÚ DE PRÓTESIS");
            Assert.AreEqual("MANOS DE LA CADERA A LA RODILLA + LOS DOS GRIPS: PIERNAS", conGesto);
            Assert.AreEqual(conGesto, TeclaVR.Traducir(conGesto), "Traducir dos veces da lo mismo.");
            Assert.AreEqual(TeclaVR.AyudaProtesis, TeclaVR.Traducir("MANTÉN TAB Y ELIGE CON EL RATÓN: PIERNAS · BRAZOS"));
            StringAssert.DoesNotContain("GATILLO IZQ", TeclaVR.Traducir("USA TU PRÓTESIS (TAB)"));
        }

        // ---- Con el jugador VR ----

        public override void Setup()
        {
            base.Setup();
            AvisosUnicos.Reiniciar();
            TutorialVR.MostrarEnSimulador = false;
        }

        public override void TearDown()
        {
            TutorialVR.MostrarEnSimulador = false;
            if (TarjetaTutorialVR.Actual != null)
            {
                TarjetaTutorialVR.Actual.Cerrar(false);
            }
            AvisosUnicos.Reiniciar();
            ControlTiempo.SoltarTodo();
            Time.timeScale = 1f;
            base.TearDown();
        }

        // Todas aprendidas menos la que se prueba: así el jugador de prueba (que trae armas) no pide otras antes.
        private static void AprenderTodasMenos(PaginaGuiaVR pagina)
        {
            foreach (PaginaGuiaVR p in System.Enum.GetValues(typeof(PaginaGuiaVR)))
            {
                if (p != pagina)
                {
                    TutorialVR.MarcarAprendida(p);
                }
            }
        }

        private IEnumerator AbrirTarjeta(PaginaGuiaVR pagina)
        {
            yield return ArmarVR();
            TutorialVR tutorial = jugador.GetComponent<TutorialVR>();
            Assert.IsNotNull(tutorial, "TutorialVR se agrega solo al armarse el rig VR.");
            AprenderTodasMenos(pagina);
            Assert.IsTrue(tutorial.Pedir(pagina));
            TutorialVR.MostrarEnSimulador = true;
            float fin = Time.unscaledTime + 3f;
            while (TarjetaTutorialVR.Actual == null && Time.unscaledTime < fin)
            {
                yield return null;
            }
            Assert.IsNotNull(TarjetaTutorialVR.Actual, "La tarjeta sale en calma.");
            Assert.AreEqual(pagina, TarjetaTutorialVR.Actual.Pagina);
            yield return Cuadros(2);
        }

        [UnityTest]
        public IEnumerator Tarjeta_SaleALadoYAbajo_YSeCierraAlHacerElGesto()
        {
            yield return AbrirTarjeta(PaginaGuiaVR.MoverseGirar);
            TarjetaTutorialVR tarjeta = TarjetaTutorialVR.Actual;
            Assert.IsTrue(tarjeta.Visible);
            Assert.AreEqual(RenderMode.WorldSpace, tarjeta.Canvas.renderMode);
            Assert.IsNotNull(tarjeta.Imagen.sprite, "Lleva la imagen de la guía.");
            StringAssert.Contains("MoverseGirar", tarjeta.Imagen.sprite.name);
            StringAssert.Contains("CAMINAR", tarjeta.Frase);
            Assert.IsNotNull(tarjeta.BotonAprendido);
            Transform cabeza = adaptador.Camara.transform;
            Vector3 local = cabeza.InverseTransformPoint(tarjeta.Canvas.transform.position);
            Assert.Greater(local.x, 0.15f, "A un lado de la vista.");
            Assert.Less(local.y, -0.1f, "Abajo de los ojos.");
            Assert.AreEqual(1f, Time.timeScale, 1e-4f, "Fuera de las peleas no detiene el juego.");

            Stick(false, new Vector2(0f, 1f));
            float fin = Time.unscaledTime + 4f;
            while (tarjeta != null && !tarjeta.Lograda && Time.unscaledTime < fin)
            {
                yield return null;
            }
            Assert.IsTrue(tarjeta != null && tarjeta.Lograda, "Caminar con el stick da por hecha MOVERSE Y GIRAR.");
            Stick(false, Vector2.zero);
            yield return Segundos(TarjetaTutorialVR.SegundosPalomita + 0.3f);
            Assert.IsNull(TarjetaTutorialVR.Actual, "Se cierra sola tras la palomita.");
            Assert.IsTrue(TutorialVR.Aprendida(PaginaGuiaVR.MoverseGirar), "Queda aprendida en la partida.");
            Assert.IsFalse(jugador.GetComponent<TutorialVR>().Pedir(PaginaGuiaVR.MoverseGirar), "No vuelve a salir en esta partida.");
        }

        [UnityTest]
        public IEnumerator Tarjeta_AprendidoLaCierraSinGesto()
        {
            yield return AbrirTarjeta(PaginaGuiaVR.ProtesisVision);
            TarjetaTutorialVR tarjeta = TarjetaTutorialVR.Actual;
            StringAssert.Contains("ProtesisVision", tarjeta.Imagen.sprite.name);
            Assert.IsTrue(tarjeta.BotonAprendido.Pulsar());
            yield return Cuadros(2);
            Assert.IsNull(TarjetaTutorialVR.Actual, "APRENDIDO la cierra.");
            Assert.IsTrue(TutorialVR.Aprendida(PaginaGuiaVR.ProtesisVision));
            yield return Segundos(1f);
            Assert.IsNull(TarjetaTutorialVR.Actual, "No sale otra: las demás ya se aprendieron.");
        }
    }
}
