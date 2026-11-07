using System;
using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Pruebas;
using MechaCyberpunk.Personaje.Vida;
using MechaCyberpunk.UI.Hud;
using MechaCyberpunk.UI.Kit;
using MechaCyberpunk.UI.Mapa;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>
    /// T-700: el HUD nuevo con el acomodo de H10_HUD.png, con jugador y HUD reales y entrada simulada: vida con su número, escudo, dinero,
    /// estrellas que parpadean, objetivo con distancia, objetos con su tecla real, ayudas de botón que cambian con el contexto, muñequito
    /// con implantes y daño, y lo viejo escondido.
    /// </summary>
    public class HudH10Pruebas : BaseHudObjetivo
    {
        protected override bool QuitarEscudo => false;

        private const string RutaCarro = "Assets/_Proyecto/Personaje/Pruebas/CarroDePrueba.prefab";
        private HudH10 h10;
        private GameObject carro;

        private sealed class ZonaBusquedaFalsa : IZonaBusqueda
        {
            public bool Activa { get; set; } = true;
            public Vector3 Centro => Vector3.zero;
            public float Radio => 30f;
            public bool Visto { get; set; }
        }

        private sealed class ImplantesFalsos : IImplantes
        {
            public readonly int[] Niveles = new int[4];
            public int Nivel(EspacioImplante espacio) => Niveles[(int)espacio];
            public string Nombre(EspacioImplante espacio) => string.Empty;
            public int Cantidad => 0;
            public EfectosImplantes Efectos => EfectosImplantes.Ninguno;
            public event Action Cambio { add { } remove { } }
            public event Action<EspacioImplante, int> Instalado { add { } remove { } }
        }

        private IEnumerator Preparar()
        {
            CrearJugador();
            CrearHud();
            yield return Calentar();
            yield return Esperar(0.3f);
            h10 = hud.GetComponent<HudH10>();
            Assert.IsNotNull(h10, "HudAPie agrega el HUD nuevo");
            Assert.IsTrue(h10.Armado, "El HUD nuevo se armó");
        }

        public override void TearDown()
        {
            if (carro != null)
            {
                UnityEngine.Object.Destroy(carro);
            }
            base.TearDown();
        }

        private static InfoDanio Bala(float cantidad, Vector3 punto) => new InfoDanio(cantidad, TipoDanio.Disparo, punto, Vector3.forward, null);

        private IEnumerator Clic()
        {
            Press(raton.leftButton);
            yield return null;
            Release(raton.leftButton);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Arranque_LoViejoQuedaEscondidoYLoNuevoSeVe()
        {
            yield return Preparar();
            Assert.IsTrue(h10.GrupoIzquierda.activeSelf, "A pie se ve el bloque de arriba a la izquierda");
            Assert.IsTrue(h10.GrupoEstrellas.activeSelf);
            Assert.IsTrue(h10.GrupoArma.activeSelf);
            foreach (GameObject viejo in new[] { hud.GrupoVida, hud.GrupoArma, hud.GrupoCalor, hud.GrupoDinero })
            {
                CanvasGroup lienzo = viejo.GetComponent<CanvasGroup>();
                Assert.IsNotNull(lienzo, viejo.name + " lleva CanvasGroup");
                Assert.AreEqual(0f, lienzo.alpha, "Lo viejo (" + viejo.name + ") ya no se dibuja");
            }
            Assert.IsTrue(hud.GrupoVida.activeSelf, "Pero sigue activo: HudAPie lleva su estado");
        }

        [UnityTest]
        public IEnumerator Vida_MuestraActualSobreMaximaConLaFraccion()
        {
            yield return Preparar();
            VidaAPie vida = jugador.GetComponent<VidaAPie>();
            float escudo = Registro<IEscudo>.Actual != null ? Registro<IEscudo>.Actual.Actual : 0f;
            vida.RecibirDanio(Bala(escudo + 40f, jugador.transform.position + Vector3.up));
            yield return null;
            int actual = Mathf.CeilToInt(vida.Actual);
            int maxima = Mathf.CeilToInt(vida.Maxima);
            Assert.AreEqual(actual + " / " + maxima, h10.TextoVida.text, "Como 143 / 204");
            Assert.AreEqual(vida.Actual / vida.Maxima, h10.RellenoVida.fillAmount, 0.01f);
        }

        [UnityTest]
        public IEnumerator Escudo_BarraDelgadaYSeVaciaAlAbsorber()
        {
            yield return Preparar();
            IEscudo escudo = Registro<IEscudo>.Actual;
            Assert.IsNotNull(escudo, "El jugador trae escudo");
            Assert.IsTrue(h10.BarraEscudo.activeSelf, "Con escudo máximo se ve la barra");
            Assert.AreEqual(1f, h10.RellenoEscudo.fillAmount, 0.01f);
            jugador.GetComponent<VidaAPie>().RecibirDanio(Bala(escudo.Maximo * 0.5f, jugador.transform.position + Vector3.up));
            yield return null;
            Assert.AreEqual(escudo.Actual / escudo.Maximo, h10.RellenoEscudo.fillAmount, 0.01f);
            Assert.Less(h10.RellenoEscudo.fillAmount, 0.9f, "El escudo bajó");
        }

        [UnityTest]
        public IEnumerator Dinero_SeVeConSimboloYSeparadorDeMiles()
        {
            yield return Preparar();
            ICartera cartera = Registro<ICartera>.Actual;
            Assert.IsNotNull(cartera, "Hay cartera");
            cartera.Abonar(12450 - cartera.Dinero, MotivoDinero.Premio);
            yield return null;
            Assert.AreEqual("$ 12,450", h10.TextoDinero.text);
        }

        [UnityTest]
        public IEnumerator Estrellas_SinCalorSonContorno_ConCalorSeLlenanYParpadeanSoloBuscando()
        {
            yield return Preparar();
            ICalor calor = Registro<ICalor>.Actual;
            Assert.IsNotNull(calor, "Hay calor");
            Assert.AreEqual(5, ReglaEstrellasH10.Total);
            Assert.AreEqual(0, h10.EstrellasLlenas, "Sin calor no hay estrellas llenas");
            ConfigHudH10 cfg = ConfigHudH10.Actual;
            Assert.AreSame(cfg.EstrellaContorno, h10.Estrella(0).sprite, "Vacía: de contorno");

            calor.Sumar(calor.Maximo, MotivoCalor.Choque);
            yield return null;
            Assert.AreEqual(5, h10.EstrellasLlenas, "Con el calor al tope: 5");
            Assert.AreSame(cfg.EstrellaLlena, h10.Estrella(4).sprite);
            Assert.IsFalse(h10.Buscando, "Sin zona de búsqueda las estrellas quedan fijas");
            Assert.AreEqual(1f, h10.Estrella(0).color.a, 0.001f);

            ZonaBusquedaFalsa zona = new ZonaBusquedaFalsa();
            Registro<IZonaBusqueda>.Agregar(zona);
            yield return null;
            Assert.IsTrue(h10.Buscando, "Con zona de búsqueda activa, parpadean");
            HashSet<float> alfas = new HashSet<float>();
            float fin = Time.unscaledTime + ReglaEstrellasH10.SegundosParpadeo * 3f;
            while (Time.unscaledTime < fin)
            {
                alfas.Add(Mathf.Round(h10.Estrella(0).color.a * 100f));
                yield return null;
            }
            Assert.GreaterOrEqual(alfas.Count, 2, "El alfa de la estrella llena cambia: parpadea");
            Registro<IZonaBusqueda>.Quitar(zona);
        }

        [UnityTest]
        public IEnumerator Objetivo_SaleDeLaEscenaConSuDistancia()
        {
            CrearJugador();
            CrearHud();
            CrearServicio();
            CrearLocal(IdLugar.Bar, new Vector3(0f, 0f, 60f), HorarioLugar.Siempre, false, true);
            yield return Calentar();
            h10 = hud.GetComponent<HudH10>();
            servicio.Fijar(new Objetivo("Ve al bar", IdLugar.Bar));
            yield return Esperar(0.3f);
            Assert.IsTrue(h10.GrupoObjetivo.activeSelf);
            Assert.IsFalse(string.IsNullOrEmpty(hud.TextoDistancia), "El objetivo del lugar tiene distancia");
            Assert.AreEqual("Ve al bar · " + hud.TextoDistancia, h10.TextoObjetivo.text, "Como 'Ve con el Doc · 120 m'");
            StringAssert.EndsWith(" m", h10.TextoObjetivo.text);
        }

        [UnityTest]
        public IEnumerator Objetos_TeclaRealYCantidad_VenIcono()
        {
            yield return Preparar();
            ServicioInventario inventario = Registro<ServicioInventario>.Actual;
            DatosConsumible bebida = (DatosConsumible)inventario.Catalogo.Buscar("bebida_energetica");
            DatosConsumible papitas = (DatosConsumible)inventario.Catalogo.Buscar("papitas");
            ConsumiblesJugador consumibles = jugador.GetComponent<ConsumiblesJugador>();
            Assert.IsFalse(h10.RanuraVisible(0), "Sin objetos no hay casillas");
            inventario.Agregar(bebida, 1);
            inventario.Agregar(papitas, 2);
            yield return null;
            yield return null;
            Assert.IsTrue(h10.RanuraVisible(0));
            Assert.IsTrue(h10.RanuraVisible(1));
            Assert.IsFalse(h10.RanuraVisible(2));
            Assert.AreEqual("x1", h10.CantidadRanura(0));
            Assert.AreEqual("x2", h10.CantidadRanura(1));
            Assert.AreEqual("[" + consumibles.TextoTecla(bebida) + "]", h10.TeclaRanura(0), "La tecla sale de la acción real");
            Assert.AreEqual("[" + consumibles.TextoTecla(papitas) + "]", h10.TeclaRanura(1));
            Assert.AreEqual("[6]", h10.TeclaRanura(0), "La bebida se usa con el 6");
            Assert.AreEqual("[7]", h10.TeclaRanura(1), "Las papitas con el 7");
            Assert.IsNotNull(h10.IconoRanura(0), "Ícono de la bebida");
            Assert.IsNotNull(h10.IconoRanura(1), "Ícono de las papitas");
            Assert.AreNotSame(h10.IconoRanura(0), h10.IconoRanura(1));

            VidaAPie vida = jugador.GetComponent<VidaAPie>();
            IEscudo escudo = Registro<IEscudo>.Actual;
            vida.RecibirDanio(Bala((escudo != null ? escudo.Actual : 0f) + 30f, jugador.transform.position + Vector3.up));
            yield return null;
            Press(teclado.digit7Key);
            yield return null;
            Release(teclado.digit7Key);
            yield return null;
            yield return null;
            Assert.AreEqual("x1", h10.CantidadRanura(1), "Usar las papitas con la tecla 7 baja la cantidad en el HUD");
        }

        [UnityTest]
        public IEnumerator Arma_BalasGrandesYAyudaDeRecargarConLaTeclaReal()
        {
            yield return Preparar();
            Assert.IsTrue(string.IsNullOrEmpty(h10.TextoBalas.text), "Con los puños no hay balas");
            string golpe = h10.TextosAyuda[0].text;
            Assert.IsTrue(golpe.EndsWith("Golpe"), "Con los puños la ayuda es Golpe: " + golpe);
            Assert.IsTrue(golpe.StartsWith("[Clic]"), "Con la tecla real del ratón: " + golpe);

            yield return Tecla(teclado.digit2Key);
            Assert.IsNotNull(h10.IconoArma.sprite, "La pistola trae su holograma");
            StringAssert.StartsWith("8", h10.TextoBalas.text.Replace("<size=32><color=#8fdcdacc>", string.Empty), "8 balas en el cargador");
            StringAssert.Contains("| 8", h10.TextoBalas.text);
            Assert.IsTrue(string.IsNullOrEmpty(h10.TextosAyuda[0].text) && string.IsNullOrEmpty(h10.TextosAyuda[1].text), "Con el cargador lleno no hay ayudas");

            yield return Clic();
            yield return null;
            Assert.AreEqual("[R] Recargar", h10.TextosAyuda[0].text, "Faltan balas: ofrece recargar con la R real");
            StringAssert.StartsWith("7", h10.TextoBalas.text.Replace("<size=32><color=#8fdcdacc>", string.Empty));

            yield return Tecla(teclado.rKey);
            yield return Esperar(jugador.GetComponent<MechaCyberpunk.Personaje.Combate.PistolaAPie>().Datos.TiempoRecarga + 0.3f);
            Assert.IsTrue(string.IsNullOrEmpty(h10.TextosAyuda[0].text), "Recargado: la ayuda se va");
        }

        [UnityTest]
        public IEnumerator Ayuda_CercaDeUnCarro_DiceSubirAlCarroConLaE_YSeVaAlAlejarse()
        {
            yield return Preparar();
            carro = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaCarro), new Vector3(2f, 0f, 1.1f), Quaternion.identity);
            yield return Esperar(0.5f);
            string subir = "[E] " + ReglaAyudasH10.TextoSubirCarro;
            Assert.AreEqual(subir, h10.TextosAyuda[1].text, "Frente a un carro la ayuda cambia a subir (arriba, con el golpe de los puños abajo)");
            Assert.AreEqual("[Clic] Golpe", h10.TextosAyuda[0].text);
            carro.transform.position = new Vector3(40f, 0f, 40f);
            yield return Esperar(0.5f);
            Assert.IsFalse(h10.TextosAyuda[0].text.Contains("Subir") || h10.TextosAyuda[1].text.Contains("Subir"), "Lejos del carro ya no sale");
        }

        [UnityTest]
        public IEnumerator Cuerpo_ImplantesRellenanYElGolpeLaParteRoja()
        {
            yield return Preparar();
            ImplantesFalsos implantes = new ImplantesFalsos();
            implantes.Niveles[(int)EspacioImplante.Piernas] = 1;
            implantes.Niveles[(int)EspacioImplante.Ojos] = 2;
            Registro<IImplantes>.Agregar(implantes);
            yield return null;
            Assert.Greater(h10.AlfaRellenoParte(ZonaCuerpoH10.Piernas), 0.1f, "Exo de piernas marcado");
            Assert.Greater(h10.AlfaRellenoParte(ZonaCuerpoH10.Cabeza), 0.1f, "Visor de cabeza marcado");
            Assert.AreEqual(0f, h10.AlfaRellenoParte(ZonaCuerpoH10.Brazos), 0.001f, "Sin exo de brazos no se marca");
            Assert.AreEqual(0f, h10.AlfaRellenoParte(ZonaCuerpoH10.Torso), 0.001f);

            Vector3 pies = jugador.transform.position;
            EventosPelea.PublicarImpacto(null, Bala(10f, pies + Vector3.up * 0.4f), new ResultadoDanio(10f, TipoBloqueo.Ninguno, false));
            yield return null;
            Color piernas = h10.ColorLineaParte(ZonaCuerpoH10.Piernas);
            Color brazos = h10.ColorLineaParte(ZonaCuerpoH10.Brazos);
            Assert.Greater(piernas.r - piernas.g, 0.4f, "La parte golpeada se pone roja");
            Assert.Less(brazos.r, 0.6f, "Las demás siguen turquesa");

            EventosPelea.PublicarImpacto(null, Bala(10f, pies + Vector3.up * 1.7f), new ResultadoDanio(10f, TipoBloqueo.Ninguno, false));
            yield return null;
            Color cabeza = h10.ColorLineaParte(ZonaCuerpoH10.Cabeza);
            Assert.Greater(cabeza.r - cabeza.g, 0.4f, "Un tiro a la cabeza pone roja la cabeza");
            Registro<IImplantes>.Quitar(implantes);
        }

        [UnityTest]
        public IEnumerator Cuerpo_ConLaVidaBaja_SePoneRojo()
        {
            yield return Preparar();
            VidaAPie vida = jugador.GetComponent<VidaAPie>();
            IEscudo escudo = Registro<IEscudo>.Actual;
            vida.RecibirDanio(Bala((escudo != null ? escudo.Actual : 0f) + vida.Maxima * 0.85f, jugador.transform.position + Vector3.up * 2.4f));
            yield return null;
            Color torso = h10.ColorLineaParte(ZonaCuerpoH10.Torso);
            Assert.Greater(torso.r - torso.g, 0.3f, "Con la vida baja todo el muñequito se pone rojo");
            Assert.AreEqual(Rojo(), h10.RellenoVida.color, "La barra de vida también");
        }

        private static Color Rojo() => new Color(1f, 0.22f, 0.28f, 1f);

        [UnityTest]
        public IEnumerator Modos_EnElCarroSoloEstrellasYObjetivo_EnElMechaNada()
        {
            CrearJugador();
            CrearHud();
            yield return Calentar();
            h10 = hud.GetComponent<HudH10>();
            JugadorFalso falso = RegistrarFalso(ModoJugador.Carro);
            yield return null;
            Assert.IsTrue(h10.GrupoEstrellas.activeSelf, "En el carro se ven las estrellas");
            Assert.IsFalse(h10.GrupoIzquierda.activeSelf, "pero no la vida");
            Assert.IsFalse(h10.GrupoArma.activeSelf);
            Assert.IsFalse(h10.GrupoObjetos.activeSelf);
            falso.CambiarModo(ModoJugador.Mecha);
            yield return null;
            Assert.IsFalse(h10.GrupoEstrellas.activeSelf, "En el mecha no se ve nada");
            Assert.IsFalse(h10.GrupoObjetivo.activeSelf);
        }

        [UnityTest]
        public IEnumerator Minimapa_EsCuadradoArribaALaDerechaConConoYRutaPunteada()
        {
            Registros.LimpiarTodos();
            GameObject camObj = new GameObject("Camara", typeof(Camera));
            camObj.transform.position = new Vector3(-47f, 0f, -60f);
            Camera camara = camObj.GetComponent<Camera>();
            JugadorMinimapaFalso falso = new JugadorMinimapaFalso { Raiz = camObj.transform, Camara = camara };
            Registro<IJugador>.Agregar(falso);
            Registro<IRutaCalles>.Agregar(new RutaFalsa());
            Registro<IObjetivos>.Agregar(new ObjetivosMinimapaFalso { Objetivo = new Objetivo("IR", new Vector3(47f, 0f, 60f)) });
            GameObject mapaObj = new GameObject("Minimapa");
            Minimapa minimapa = mapaObj.AddComponent<Minimapa>();
            yield return null;
            minimapa.Actualizar();
            try
            {
                RanuraHud ranura = DisposicionHud.Minimapa;
                Assert.AreEqual(new Vector2(1f, 1f), minimapa.Raiz.anchorMin, "Anclado arriba a la derecha");
                Assert.AreEqual(ranura.Posicion, minimapa.Raiz.anchoredPosition);
                Assert.AreEqual(minimapa.Raiz.sizeDelta.x, minimapa.Raiz.sizeDelta.y, "Cuadrado");
                Image mascara = minimapa.Raiz.Find("Mascara").GetComponent<Image>();
                Assert.IsNull(mascara.sprite, "La máscara no es un círculo: es el cuadrado entero");
                Assert.IsNotNull(minimapa.Raiz.Find("ConoVista"), "Lleva el cono de vista del jugador");
                Assert.IsNull(minimapa.Raiz.Find("Anillo"), "Sin el anillo redondo viejo");
                Assert.IsNotNull(minimapa.Raiz.Find("Borde/Arriba"), "Borde fino cuadrado");
                Assert.Greater(minimapa.SegmentosRutaVisibles, 0, "Hay ruta al objetivo");
                Image tramo = minimapa.Raiz.Find("Mascara/Mundo/Desplazado/RutaGps/Tramo_0").GetComponent<Image>();
                Assert.AreEqual(Image.Type.Tiled, tramo.type, "La ruta va punteada (el punto se repite)");
                Assert.IsNotNull(tramo.sprite);
            }
            finally
            {
                UnityEngine.Object.Destroy(mapaObj);
                UnityEngine.Object.Destroy(camObj);
                Registros.LimpiarTodos();
            }
        }

        private sealed class JugadorMinimapaFalso : IJugador
        {
            public Transform Raiz { get; set; }
            public ModoJugador Modo => ModoJugador.APie;
            public Camera Camara { get; set; }
            public event Action<ModoJugador> ModoCambio { add { } remove { } }
            public void Colocar(Pose pose) { }
        }

        private sealed class RutaFalsa : IRutaCalles
        {
            public List<Vector3> Calcular(Vector3 origen, Vector3 destino)
            {
                float medio = (origen.z + destino.z) * 0.5f;
                return new List<Vector3> { origen, new Vector3(origen.x, 0f, medio), new Vector3(destino.x, 0f, medio), destino };
            }
        }

        private sealed class ObjetivosMinimapaFalso : IObjetivos
        {
            public Objetivo Objetivo;
            public bool HayObjetivo => Objetivo.TienePosicion;
            public Objetivo Actual => Objetivo;
            public event Action<Objetivo> Cambio { add { } remove { } }
        }
    }
}
