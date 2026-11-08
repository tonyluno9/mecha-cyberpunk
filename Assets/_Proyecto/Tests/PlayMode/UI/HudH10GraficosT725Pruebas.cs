using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Personaje.Vida;
using MechaCyberpunk.UI.Hud;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>
    /// T-725: el HUD H10 con los gráficos del chat de arte (H20 a H26) y la barra de vida que deja la parte perdida y la baja suave con DOTween.
    /// Jugador y HUD reales, daño real por el servicio de vida.
    /// </summary>
    public class HudH10GraficosT725Pruebas : BaseHudObjetivo
    {
        protected override bool QuitarEscudo => false;
        private HudH10 h10;
        private ConfigHudH10 cfg;

        private IEnumerator Preparar()
        {
            CrearJugador();
            CrearHud();
            yield return Calentar();
            yield return Esperar(0.3f);
            h10 = hud.GetComponent<HudH10>();
            cfg = ConfigHudH10.Actual;
            Assert.IsTrue(h10.Armado);
        }

        private static InfoDanio Bala(float cantidad, Vector3 punto) => new InfoDanio(cantidad, TipoDanio.Disparo, punto, Vector3.forward, null);

        private static bool EsDelChatDeArte(Sprite sprite) => sprite != null && AssetDatabase.GetAssetPath(sprite).Contains("UI/Iconos/H10/");

        [UnityTest]
        public IEnumerator Graficos_BarrasEmblemaEstrellasYMunequitoSalenDeLosPngDeArte()
        {
            yield return Preparar();
            Transform izquierda = h10.GrupoIzquierda.transform;
            Assert.AreSame(cfg.MarcoVida, izquierda.Find("BarraVida/Marco").GetComponent<Image>().sprite, "Marco de la vida (H21)");
            Assert.AreSame(cfg.MarcoEscudo, izquierda.Find("BarraEscudo/Marco").GetComponent<Image>().sprite, "Marco del escudo (H21)");
            Assert.AreSame(cfg.RellenoVida, h10.RellenoVida.sprite, "Relleno de la vida (H20)");
            Assert.AreSame(cfg.RellenoEscudo, h10.RellenoEscudo.sprite, "Relleno del escudo (H20)");
            Assert.IsTrue(EsDelChatDeArte(h10.EmblemaVida.sprite), "Emblema a la izquierda de las barras");
            Assert.IsTrue(EsDelChatDeArte(h10.Estrella(0).sprite), "Estrella vacía de H25");
            foreach (string parte in new[] { "Cabeza", "Torso", "Brazos", "Piernas" })
            {
                Assert.IsTrue(EsDelChatDeArte(h10.GrupoArma.transform.Find("Cuerpo/Linea" + parte).GetComponent<Image>().sprite), "Muñequito H24: " + parte);
            }
            Assert.AreNotSame(h10.RellenoVida.sprite, h10.RellenoEscudo.sprite, "Cada barra con su relleno");
        }

        [UnityTest]
        public IEnumerator Graficos_CasillasConLosIconosDeArte()
        {
            yield return Preparar();
            ServicioInventario inventario = Registro<ServicioInventario>.Actual;
            DatosConsumible bebida = (DatosConsumible)inventario.Catalogo.Buscar("bebida_energetica");
            Assert.IsNotNull(bebida, "El catálogo trae la bebida");
            inventario.Agregar(bebida, 1);
            string consumiblesTecla = jugador.GetComponent<MechaCyberpunk.Personaje.Jugador.ConsumiblesJugador>().TextoTecla(bebida);
            yield return null;
            yield return null;
            Assert.IsTrue(h10.RanuraVisible(0));
            Assert.IsTrue(EsDelChatDeArte(h10.IconoRanura(0)), "El ícono es de H23");
            Assert.AreSame(cfg.Icono(bebida.Id), h10.IconoRanura(0));
            Transform casilla = h10.GrupoObjetos.transform.Find("Objeto0/Casilla");
            Assert.IsNotNull(casilla, "Casilla de H26");
            Assert.AreSame(cfg.CasillaObjeto, casilla.GetComponent<Image>().sprite);
            Assert.AreEqual(consumiblesTecla, h10.TeclaRanura(0), "La tecla real va en el cuadrito de la esquina");
        }

        [UnityTest]
        public IEnumerator Barra_AlRecibirDanioLaParteQueSePerdioSeQuedaYLuegoBaja()
        {
            yield return Preparar();
            VidaAPie vida = jugador.GetComponent<VidaAPie>();
            IEscudo escudo = Registro<IEscudo>.Actual;
            Assert.AreEqual(1f, h10.PerdidaVida.fillAmount, 0.01f, "Llena al empezar");
            vida.RecibirDanio(Bala((escudo != null ? escudo.Actual : 0f) + vida.Maxima * 0.4f, jugador.transform.position + Vector3.up));
            yield return null;
            float actual = vida.Actual / vida.Maxima;
            Assert.AreEqual(actual, h10.RellenoVida.fillAmount, 0.01f, "El relleno marca la vida de una vez");
            Assert.Greater(h10.PerdidaVida.fillAmount, actual + 0.2f, "La parte perdida se queda");
            yield return Esperar(cfg.SegundosEsperaPerdida * 0.5f);
            Assert.Greater(h10.PerdidaVida.fillAmount, actual + 0.2f, "Todavía espera antes de bajar");
            Assert.IsTrue(h10.PerdidaBajando);
            yield return Esperar(cfg.SegundosEsperaPerdida + cfg.SegundosBajaPerdida + 0.4f);
            Assert.AreEqual(actual, h10.PerdidaVida.fillAmount, 0.01f, "Luego baja hasta la vida actual");
            Assert.IsFalse(h10.PerdidaBajando);
        }

        [UnityTest]
        public IEnumerator Barra_AlCurarseLaParteQueSePerdioSubeJuntoConElRelleno()
        {
            yield return Preparar();
            VidaAPie vida = jugador.GetComponent<VidaAPie>();
            IEscudo escudo = Registro<IEscudo>.Actual;
            vida.RecibirDanio(Bala((escudo != null ? escudo.Actual : 0f) + vida.Maxima * 0.5f, jugador.transform.position + Vector3.up));
            yield return Esperar(cfg.SegundosEsperaPerdida + cfg.SegundosBajaPerdida + 0.4f);
            vida.Curar(vida.Maxima * 0.25f);
            yield return null;
            Assert.AreEqual(vida.Actual / vida.Maxima, h10.RellenoVida.fillAmount, 0.01f);
            Assert.AreEqual(h10.RellenoVida.fillAmount, h10.PerdidaVida.fillAmount, 0.01f, "Al curar no queda parte perdida");
        }

        [UnityTest]
        public IEnumerator Barra_ElEscudoTambienDejaSuParteQueSePerdio()
        {
            yield return Preparar();
            VidaAPie vida = jugador.GetComponent<VidaAPie>();
            IEscudo escudo = Registro<IEscudo>.Actual;
            Assert.IsNotNull(escudo);
            vida.RecibirDanio(Bala(escudo.Maximo * 0.6f, jugador.transform.position + Vector3.up));
            yield return null;
            Assert.AreEqual(escudo.Actual / escudo.Maximo, h10.RellenoEscudo.fillAmount, 0.01f);
            Assert.Greater(h10.PerdidaEscudo.fillAmount, h10.RellenoEscudo.fillAmount + 0.2f);
            yield return Esperar(cfg.SegundosEsperaPerdida + cfg.SegundosBajaPerdida + 0.4f);
            Assert.AreEqual(h10.RellenoEscudo.fillAmount, h10.PerdidaEscudo.fillAmount, 0.01f);
        }

        [UnityTest]
        public IEnumerator Munequito_SigueMarcandoElDanioEnRojoConLosGraficosNuevos()
        {
            yield return Preparar();
            VidaAPie vida = jugador.GetComponent<VidaAPie>();
            IEscudo escudo = Registro<IEscudo>.Actual;
            vida.RecibirDanio(Bala((escudo != null ? escudo.Actual : 0f) + 20f, jugador.transform.position + Vector3.up * 2.4f));
            yield return null;
            Color cabeza = h10.ColorLineaParte(ZonaCuerpoH10.Cabeza);
            Assert.Greater(cabeza.r - cabeza.g, 0.2f, "La cabeza golpeada se pone roja");
            Assert.Greater(h10.AlfaRellenoParte(ZonaCuerpoH10.Cabeza), 0.05f, "y su relleno (el interior del H24) se marca");
        }
    }
}
