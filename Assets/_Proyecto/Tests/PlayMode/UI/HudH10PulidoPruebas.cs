using System;
using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion;
using MechaCyberpunk.Personaje.Vida;
using MechaCyberpunk.UI.Hud;
using MechaCyberpunk.UI.Mapa;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>
    /// T-701: el HUD pulido para parecerse a H10, con el HUD y el jugador reales: sin panel de zona ni hora, el nombre de zona que se desvanece,
    /// el aviso de mensaje compacto que se esconde solo, el número de vida legible, el muñequito sin relleno rojo con la vida llena y las
    /// etiquetas más grandes.
    /// </summary>
    public class HudH10PulidoPruebas : BaseHudObjetivo
    {
        protected override bool QuitarEscudo => false;
        private HudH10 h10;

        private IEnumerator Preparar()
        {
            CrearJugador();
            CrearHud();
            yield return Calentar();
            yield return Esperar(0.3f);
            h10 = hud.GetComponent<HudH10>();
            Assert.IsTrue(h10.Armado);
        }

        [UnityTest]
        public IEnumerator PanelDeZona_YaNoSeDibujaEnLaPantallaNormal()
        {
            yield return Preparar();
            HudTerritorio territorio = UnityEngine.Object.FindFirstObjectByType<HudTerritorio>();
            if (territorio == null)
            {
                territorio = new GameObject("Territorio").AddComponent<HudTerritorio>();
                yield return null;
            }
            Transform panel = territorio.transform.Find("CanvasTerritorio/PanelZona");
            Assert.IsNotNull(panel);
            CanvasGroup grupo = panel.GetComponent<CanvasGroup>();
            Assert.IsNotNull(grupo, "El panel viejo lleva CanvasGroup");
            Assert.AreEqual(0f, grupo.alpha, "Y no se dibuja");
        }

        [UnityTest]
        public IEnumerator HoraYDinero_LaHoraYaNoSaleEnElHudAPie()
        {
            yield return Preparar();
            Assert.IsNull(h10.GrupoIzquierda.transform.Find("Hora"), "H10 no lleva la hora y el HUD nuevo tampoco");
            Assert.IsNotNull(h10.TextoDinero);
        }

        [UnityTest]
        public IEnumerator NombreDeZona_SaleChicoArribaAlCentroYSeDesvanece()
        {
            yield return Preparar();
            Assert.IsFalse(h10.TituloVisible, "No se ve de entrada");
            Assert.IsTrue(HudH10.MostrarTitulo("MEGALOPOLIS · SUBURBIOS", 2.4f));
            yield return null;
            yield return null;
            Assert.IsTrue(h10.TituloVisible, "Sale al entrar a la zona");
            Assert.AreEqual("MEGALOPOLIS · SUBURBIOS", h10.TituloZona);
            Text texto = h10.Canvas.transform.Find("TituloZona").GetComponent<Text>();
            Assert.LessOrEqual(texto.fontSize, 32, "Chico");
            Assert.AreEqual(0.5f, texto.rectTransform.anchorMin.x, "Al centro");
            Assert.AreEqual(1f, texto.rectTransform.anchorMin.y, "Arriba");
            Assert.AreEqual(ConfigHudH10.Actual.Fuente, texto.font, "Con la tipografía delgada del HUD nuevo");
            yield return Esperar(2.6f);
            Assert.IsFalse(h10.TituloVisible, "Y se desvanece solo");
            Assert.AreEqual(string.Empty, texto.text);
        }

        [UnityTest]
        public IEnumerator NombreDeZona_EltextoDeInicioDeLaHistoriaVaPorAhiYNoPorLaCola()
        {
            yield return Preparar();
            MechaCyberpunk.UI.Historia.PantallaHistoriaPandillas pantalla = new GameObject("Historia").AddComponent<MechaCyberpunk.UI.Historia.PantallaHistoriaPandillas>();
            ColaAvisos cola = hud.GetComponent<ColaAvisos>();
            pantalla.MostrarAyuda("MEGALOPOLIS · SUBURBIOS", 4f);
            yield return null;
            Assert.IsTrue(h10.TituloVisible, "Sale en el nombre chico de arriba");
            Assert.AreEqual(0, cola.EnEspera, "No entra a la cola de avisos del centro");
            Assert.IsFalse(cola.Visible);
            pantalla.MostrarAyuda("ENCARGO: ENTREGA", 2f);
            yield return null;
            Assert.IsTrue(cola.Visible || cola.EnEspera > 0, "Los demás avisos siguen por la cola");
            UnityEngine.Object.Destroy(pantalla.gameObject);
        }

        [UnityTest]
        public IEnumerator Vida_ElNumeroVaAlLadoDeLaBarraYSinPalabrasVidaNiEscudo()
        {
            yield return Preparar();
            Assert.GreaterOrEqual(h10.TextoVida.fontSize, 32, "El número de vida es de texto normal (como H10)");
            RectTransform barra = (RectTransform)h10.GrupoIzquierda.transform.Find("BarraVida");
            RectTransform escudo = (RectTransform)h10.GrupoIzquierda.transform.Find("BarraEscudo");
            Assert.IsFalse(h10.TextoVida.transform.IsChildOf(barra), "El número va al lado, no dentro del marco");
            Assert.GreaterOrEqual(h10.TextoVida.rectTransform.anchoredPosition.x, barra.anchoredPosition.x + barra.sizeDelta.x, "A la derecha de la barra");
            Assert.Greater(escudo.sizeDelta.x, 0f);
            Assert.Less(escudo.sizeDelta.x, barra.sizeDelta.x, "El escudo es corto y la vida larga");
            Assert.Greater(escudo.anchoredPosition.y, barra.anchoredPosition.y, "El escudo va arriba de la vida");
            Assert.IsNull(h10.GrupoIzquierda.transform.Find("EtiquetaEscudo"), "Sin la palabra ESCUDO");
            Assert.IsNull(h10.GrupoIzquierda.transform.Find("EtiquetaVida"), "Sin la palabra VIDA");
            foreach (Text texto in h10.GrupoIzquierda.GetComponentsInChildren<Text>(true))
            {
                Assert.AreNotEqual("VIDA", texto.text);
                Assert.AreNotEqual("ESCUDO", texto.text);
            }
        }

        [UnityTest]
        public IEnumerator Muneco_ConLaVidaLlenaEsSoloContornoTurquesaAunqueElEscudoAbsorbaUnGolpe()
        {
            yield return Preparar();
            IEscudo escudo = Registro<IEscudo>.Actual;
            Assert.IsNotNull(escudo);
            jugador.GetComponent<VidaAPie>().RecibirDanio(new InfoDanio(escudo.Actual * 0.5f, TipoDanio.Disparo, jugador.transform.position + Vector3.up * 1.2f, Vector3.forward, null));
            yield return Esperar(0.2f);
            VidaAPie vida = jugador.GetComponent<VidaAPie>();
            Assert.AreEqual(vida.Maxima, vida.Actual, 0.01f, "La vida sigue llena: el escudo absorbió");
            for (int z = 0; z < ConfigHudH10.Zonas; z++)
            {
                ZonaCuerpoH10 zona = (ZonaCuerpoH10)z;
                Color linea = h10.ColorLineaParte(zona);
                Assert.Less(linea.r, 0.6f, zona + ": contorno turquesa, no rojo");
                Assert.Greater(linea.g, 0.6f, zona + ": contorno turquesa");
                Assert.AreEqual(0f, h10.AlfaRellenoParte(zona), 0.001f, zona + ": sin relleno");
            }
        }

        [UnityTest]
        public IEnumerator Muneco_ElDanioALaVidaPintaSoloLaParteGolpeada()
        {
            yield return Preparar();
            VidaAPie vida = jugador.GetComponent<VidaAPie>();
            IEscudo escudo = Registro<IEscudo>.Actual;
            vida.RecibirDanio(new InfoDanio(escudo.Actual + 20f, TipoDanio.Disparo, jugador.transform.position + Vector3.up * 0.4f, Vector3.forward, null));
            yield return null;
            Color piernas = h10.ColorLineaParte(ZonaCuerpoH10.Piernas);
            Assert.Greater(piernas.r - piernas.g, 0.4f, "La parte dañada va en rojo");
            Assert.Less(h10.ColorLineaParte(ZonaCuerpoH10.Torso).r, 0.6f, "Las demás siguen turquesa");
            Assert.Less(h10.ColorLineaParte(ZonaCuerpoH10.Cabeza).r, 0.6f);
        }

        [UnityTest]
        public IEnumerator AvisoDelDoc_EsCompactoYSeEscondeSoloAlPocoTiempo()
        {
            yield return Preparar();
            ServicioMensajes mensajes = Registro<ServicioMensajes>.Actual;
            if (mensajes == null)
            {
                mensajes = new GameObject("Mensajes").AddComponent<ServicioMensajes>();
                yield return null;
            }
            Assert.IsNotNull(Registro<ServicioMensajes>.Actual);
            AvisoMensaje aviso = UnityEngine.Object.FindFirstObjectByType<AvisoMensaje>();
            Assert.IsNotNull(aviso);
            Assert.IsTrue(mensajes.Entregar(ReglaMensajes.IdLibre, 1), "Llega un mensaje");
            float inicio = Time.realtimeSinceStartup;
            float limite = inicio + 3f;
            while (!aviso.Visible && Time.realtimeSinceStartup < limite)
            {
                yield return null;
            }
            Assert.IsTrue(aviso.Visible, "Sale el aviso");
            StringAssert.Contains("EL INVENTOR", aviso.Texto);
            RectTransform panel = (RectTransform)aviso.transform.Find("CanvasAvisoMensaje/Aviso");
            Assert.IsNotNull(panel);
            Assert.LessOrEqual(panel.sizeDelta.x, 600f, "Compacto de ancho");
            Assert.LessOrEqual(panel.sizeDelta.y, 130f, "y de alto");
            Assert.IsNotNull(panel.Find("Borde/Arriba"), "De línea fina");
            Assert.Less(panel.anchoredPosition.y, -(MechaCyberpunk.UI.Kit.DisposicionHud.LadoMinimapa + MechaCyberpunk.UI.Kit.DisposicionHud.MargenSuperiorH10), "Debajo del minimapa y del objetivo");
            limite = Time.realtimeSinceStartup + 7f;
            while (aviso.Visible && Time.realtimeSinceStartup < limite)
            {
                yield return null;
            }
            Assert.IsFalse(aviso.Visible, "Se esconde solo a los pocos segundos");
            Assert.Less(Time.realtimeSinceStartup - inicio, 7f);
        }
    }

    /// <summary>T-701: el minimapa con calles de la escena viva, íconos que no se encimen y el marcador del objetivo discreto.</summary>
    public class MinimapaCallesT701Pruebas
    {
        private sealed class JugadorFalso : IJugador
        {
            public Transform Raiz { get; set; }
            public ModoJugador Modo => ModoJugador.APie;
            public Camera Camara { get; set; }
            public event Action<ModoJugador> ModoCambio { add { } remove { } }
            public void Colocar(Pose pose) { }
        }

        private GameObject escenario;

        [TearDown]
        public void Limpiar()
        {
            if (escenario != null)
            {
                UnityEngine.Object.Destroy(escenario);
            }
            Registros.LimpiarTodos();
            CatalogoCallesMapa.Reiniciar();
        }

        private static GameObject Edificio(Transform padre, Vector3 centro, Vector3 tamano)
        {
            GameObject cubo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cubo.name = "Edificio";
            cubo.transform.SetParent(padre, false);
            cubo.transform.position = centro + Vector3.up * tamano.y * 0.5f;
            cubo.transform.localScale = tamano;
            return cubo;
        }

        [UnityTest]
        public IEnumerator Calles_SeDibujanDeLosEdificiosDeLaEscenaYNoDelBarrioViejo()
        {
            Registros.LimpiarTodos();
            CatalogoCallesMapa.Reiniciar();
            escenario = new GameObject("Escenario");
            // Una ciudad que NO es el barrio viejo: dos manzanas lejos del origen, con una calle de 14 m entre ellas.
            Edificio(escenario.transform, new Vector3(500f, 0f, 500f), new Vector3(40f, 12f, 40f));
            Edificio(escenario.transform, new Vector3(554f, 0f, 500f), new Vector3(40f, 12f, 40f));
            GameObject camObj = new GameObject("Camara", typeof(Camera));
            camObj.transform.SetParent(escenario.transform, false);
            camObj.transform.position = new Vector3(527f, 1f, 470f);
            Registro<IJugador>.Agregar(new JugadorFalso { Raiz = camObj.transform, Camara = camObj.GetComponent<Camera>() });
            Minimapa minimapa = new GameObject("Minimapa").AddComponent<Minimapa>();
            escenario.transform.SetAsLastSibling();
            yield return null;
            minimapa.Actualizar();
            try
            {
                Assert.Greater(CatalogoCallesMapa.Tramos.Count, 0, "Se encontraron los contornos de las manzanas");
                Assert.Greater(minimapa.LineasCallesVisibles, 0, "Se dibujan líneas de calle cerca del jugador");
                Transform capa = minimapa.Raiz.Find("Mascara/Mundo/Desplazado/Calles");
                Assert.IsNotNull(capa, "Capa de calles");
                Assert.IsNull(minimapa.Raiz.Find("Mascara/Mundo/Desplazado/Manzana_1"), "Sin las manzanas fijas del barrio viejo");
                Image linea = capa.GetChild(0).GetComponent<Image>();
                Assert.LessOrEqual(linea.rectTransform.sizeDelta.y, 2f, "Líneas finas");
                Assert.Greater(linea.color.b, 0.7f, "Claras");
            }
            finally
            {
                UnityEngine.Object.Destroy(minimapa.gameObject);
            }
        }

        [UnityTest]
        public IEnumerator Calles_LejosDeTodoNoSeDibujaNada()
        {
            Registros.LimpiarTodos();
            CatalogoCallesMapa.Reiniciar();
            escenario = new GameObject("Escenario");
            Edificio(escenario.transform, new Vector3(500f, 0f, 500f), new Vector3(40f, 12f, 40f));
            GameObject camObj = new GameObject("Camara", typeof(Camera));
            camObj.transform.SetParent(escenario.transform, false);
            camObj.transform.position = new Vector3(0f, 1f, 0f);
            Registro<IJugador>.Agregar(new JugadorFalso { Raiz = camObj.transform, Camara = camObj.GetComponent<Camera>() });
            Minimapa minimapa = new GameObject("Minimapa").AddComponent<Minimapa>();
            yield return null;
            minimapa.Actualizar();
            Assert.AreEqual(0, minimapa.LineasCallesVisibles, "Solo se dibujan las calles cercanas");
            UnityEngine.Object.Destroy(minimapa.gameObject);
        }

        [UnityTest]
        public IEnumerator Minimapa_EsDel13PorCientoDelAnchoYCuadrado()
        {
            Registros.LimpiarTodos();
            escenario = new GameObject("Escenario");
            GameObject camObj = new GameObject("Camara", typeof(Camera));
            camObj.transform.SetParent(escenario.transform, false);
            Registro<IJugador>.Agregar(new JugadorFalso { Raiz = camObj.transform, Camara = camObj.GetComponent<Camera>() });
            Minimapa minimapa = new GameObject("Minimapa").AddComponent<Minimapa>();
            yield return null;
            float lado = minimapa.Raiz.sizeDelta.x;
            Assert.AreEqual(minimapa.Raiz.sizeDelta.y, lado, "Cuadrado");
            Assert.AreEqual(0.135f, lado / 1920f, 0.01f, "Cerca del 13 % del ancho, como H10");
            UnityEngine.Object.Destroy(minimapa.gameObject);
        }

        [UnityTest]
        public IEnumerator Marcador_ElRomboEsChicoYLaDistanciaFina()
        {
            Registros.LimpiarTodos();
            GameObject g = new GameObject("Marcador");
            MarcadorObjetivo marcador = g.AddComponent<MarcadorObjetivo>();
            yield return null;
            RectTransform figura = (RectTransform)marcador.Rombo.Find("Figura");
            Assert.IsNotNull(figura);
            Assert.LessOrEqual(figura.sizeDelta.x * Mathf.Sqrt(2f), 24f, "Rombo chico (diagonal)");
            Text distancia = marcador.Rombo.Find("Distancia").GetComponent<Text>();
            Assert.LessOrEqual(distancia.fontSize, 20, "Distancia chica");
            Assert.LessOrEqual(distancia.color.a, 0.8f, "y discreta");
            UnityEngine.Object.Destroy(g);
        }
    }
}
