using System;
using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Habilidades;
using MechaCyberpunk.UI.Clinica;
using MechaCyberpunk.UI.Hud;
using MechaCyberpunk.UI.Kit;
using MechaCyberpunk.UI.MenuPausa;
using MechaCyberpunk.UI.Rueda;
using MechaCyberpunk.VR;
using MechaCyberpunk.VR.Guia;
using MechaCyberpunk.VR.Menus;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.VR
{
    /// <summary>
    /// VR20: los gestos de las prótesis (manos en los muslos = PIERNAS, brazos abiertos = BRAZOS, con los dos grips 0.5 s) activan la prótesis
    /// con la misma API de la rueda solo si está instalada; la guía de controles lleva los dos modelos reales de los controles con etiquetas y
    /// líneas, sale sola la primera vez y no la segunda, se abre desde Pausa → CONTROLES y se cierra con B o con el gatillo sobre CERRAR;
    /// los textos de las prótesis en VR no dicen Tab.
    /// </summary>
    public class GuiaGestosVRPruebas : BaseJugadorVR
    {
        private static readonly Vector3 Cabeza = new Vector3(0f, 1.6f, 0f);
        // Manos sobre los muslos: 0.9 m bajo los ojos, a 0.17 m de cada lado.
        private static readonly Vector3 MusloIzq = new Vector3(-0.17f, 0.7f, 0.05f);
        private static readonly Vector3 MusloDer = new Vector3(0.17f, 0.7f, 0.05f);
        // Brazos abiertos: a 0.75 m de cada lado, 0.2 m bajo los ojos (altura de hombros).
        private static readonly Vector3 AbiertoIzq = new Vector3(-0.75f, 1.4f, 0f);
        private static readonly Vector3 AbiertoDer = new Vector3(0.75f, 1.4f, 0f);
        private static readonly Vector3 ManoDerechaCuarto = new Vector3(0.2f, 1.2f, 0.3f);

        private ImplantesDoble implantes;
        private bool habiaClave;
        private int claveAntes;

        private sealed class ImplantesDoble : IImplantes
        {
            private readonly int[] niveles = new int[4];
            public int Cantidad { get { int n = 0; foreach (int x in niveles) if (x > 0) n++; return n; } }
            public EfectosImplantes Efectos => EfectosImplantes.Ninguno;
            public event Action Cambio;
            public event Action<EspacioImplante, int> Instalado;
            public int Nivel(EspacioImplante espacio) => niveles[(int)espacio];
            public string Nombre(EspacioImplante espacio) => niveles[(int)espacio] > 0 ? "DOBLE" : string.Empty;

            public void Poner(EspacioImplante espacio, int nivel)
            {
                niveles[(int)espacio] = nivel;
                Cambio?.Invoke();
                if (nivel > 0) Instalado?.Invoke(espacio, nivel);
            }
        }

        public override void Setup()
        {
            base.Setup();
            EventosPelea.LimpiarSuscriptores();
            implantes = new ImplantesDoble();
            GuiaControlesVR.MostrarEnSimulador = false;
            habiaClave = PlayerPrefs.HasKey(GuiaControlesVR.ClaveVista);
            claveAntes = PlayerPrefs.GetInt(GuiaControlesVR.ClaveVista, 0);
            // Por omisión ya vista: solo la prueba de la primera vez la borra.
            PlayerPrefs.SetInt(GuiaControlesVR.ClaveVista, 1);
        }

        public override void TearDown()
        {
            if (GuiaControlesVR.Actual != null)
            {
                GuiaControlesVR.Actual.Cerrar();
            }
            GuiaControlesVR.MostrarEnSimulador = false;
            if (habiaClave)
            {
                PlayerPrefs.SetInt(GuiaControlesVR.ClaveVista, claveAntes);
            }
            else
            {
                PlayerPrefs.DeleteKey(GuiaControlesVR.ClaveVista);
            }
            PlayerPrefs.Save();
            Registro<IImplantes>.Quitar(implantes);
            ControlTiempo.SoltarTodo();
            Time.timeScale = 1f;
            EventosPelea.LimpiarSuscriptores();
            base.TearDown();
        }

        private IEnumerator Preparar(EspacioImplante espacio, int nivel)
        {
            implantes.Poner(espacio, nivel);
            yield return ArmarVR();
            // El prefab del jugador registra su propio servicio de implantes: el doble va después para ser el actual.
            Registro<IImplantes>.Agregar(implantes);
            PonerCabeza(Cabeza, Quaternion.identity);
            yield return Cuadros(4);
            Assert.IsNotNull(jugador.GetComponent<GestosProtesisVR>(), "GestosProtesisVR se agrega solo al armarse el rig VR.");
            Assert.IsNotNull(jugador.GetComponent<HabilidadesProtesis>());
        }

        private HabilidadesProtesis Habilidades => jugador.GetComponent<HabilidadesProtesis>();
        private GestosProtesisVR Gestos => jugador.GetComponent<GestosProtesisVR>();

        private IEnumerator PonerManosYGrips(Vector3 izq, Vector3 der, float segundos)
        {
            PonerMano(false, izq, Quaternion.identity);
            PonerMano(true, der, Quaternion.identity);
            yield return Cuadros(3);
            Mantener(controlIzq.gripButton);
            Mantener(controlDer.gripButton);
            yield return Segundos(segundos);
        }

        private void SoltarGrips()
        {
            Soltar(controlIzq.gripButton);
            Soltar(controlDer.gripButton);
        }

        private static IEnumerator SegundosReales(float s)
        {
            float fin = Time.unscaledTime + s;
            while (Time.unscaledTime < fin)
            {
                yield return null;
            }
        }

        private IEnumerator ApuntarA(Vector3 punto)
        {
            PonerMano(true, ManoDerechaCuarto, Quaternion.identity);
            yield return Cuadros(2);
            Vector3 desde = adaptador.ManoDerecha.transform.position;
            Quaternion mundo = Quaternion.LookRotation(punto - desde);
            PonerMano(true, ManoDerechaCuarto, Quaternion.Inverse(adaptador.Rig.rotation) * mundo);
            yield return Cuadros(4);
        }

        // ---- Gestos ----

        [UnityTest]
        public IEnumerator ManosEnLosMuslos_ConPiernas_LaActivaTrasMedioSegundo()
        {
            yield return Preparar(EspacioImplante.Piernas, 1);
            HabilidadesProtesis h = Habilidades;
            Assert.AreEqual(1, h.Nivel(HabilidadProtesis.Piernas));
            yield return PonerManosYGrips(MusloIzq, MusloDer, 0.2f);
            Assert.AreEqual(GestoProtesisVR.Piernas, Gestos.PoseActual, "Las dos manos sobre los muslos son la pose de PIERNAS.");
            Assert.IsTrue(GestosProtesisVR.EnPose, "EnPose avisa al agarre que esos grips son del gesto.");
            Assert.AreEqual(EstadoHabilidad.Lista, h.Estado(HabilidadProtesis.Piernas), "Antes de 0.5 s no se activa.");
            yield return Segundos(0.5f);
            Assert.AreEqual(EstadoHabilidad.Activa, h.Estado(HabilidadProtesis.Piernas), "Con los dos grips 0.5 s se activan las PIERNAS.");
            Assert.AreEqual(1, Gestos.Activaciones);
            Assert.AreEqual(EstadoHabilidad.Lista, h.Estado(HabilidadProtesis.Brazos), "BRAZOS no se toca.");
            SoltarGrips();
            yield return Cuadros(2);
        }

        [UnityTest]
        public IEnumerator ManosEnLosMuslos_SinProtesis_NoActivaNada()
        {
            yield return Preparar(EspacioImplante.Piernas, 0);
            HabilidadesProtesis h = Habilidades;
            yield return PonerManosYGrips(MusloIzq, MusloDer, 1f);
            Assert.AreEqual(GestoProtesisVR.Piernas, Gestos.PoseActual, "La pose se ve...");
            Assert.IsFalse(GestosProtesisVR.EnPose, "...pero sin la prótesis no cuenta (el grip sigue siendo agarre).");
            Assert.AreEqual(EstadoHabilidad.Lista, h.Estado(HabilidadProtesis.Piernas));
            Assert.AreEqual(0, Gestos.Activaciones);
            SoltarGrips();
            yield return Cuadros(2);
        }

        [UnityTest]
        public IEnumerator BrazosAbiertos_ConBrazos_LaActiva()
        {
            yield return Preparar(EspacioImplante.Brazos, 1);
            HabilidadesProtesis h = Habilidades;
            yield return PonerManosYGrips(AbiertoIzq, AbiertoDer, 0.75f);
            Assert.AreEqual(GestoProtesisVR.Brazos, Gestos.PoseActual, "Los dos brazos abiertos a la altura de los hombros son la pose de BRAZOS.");
            Assert.AreEqual(EstadoHabilidad.Activa, h.Estado(HabilidadProtesis.Brazos), "Con los dos grips 0.5 s se activan los BRAZOS.");
            Assert.AreEqual(1, Gestos.Activaciones);
            Assert.AreEqual(EstadoHabilidad.Lista, h.Estado(HabilidadProtesis.Piernas));
            SoltarGrips();
            yield return Cuadros(2);
        }

        [UnityTest]
        public IEnumerator BrazosAbiertos_SinBrazos_NoActivaNada()
        {
            yield return Preparar(EspacioImplante.Brazos, 0);
            HabilidadesProtesis h = Habilidades;
            yield return PonerManosYGrips(AbiertoIzq, AbiertoDer, 1f);
            Assert.AreEqual(GestoProtesisVR.Brazos, Gestos.PoseActual);
            Assert.IsFalse(GestosProtesisVR.EnPose);
            Assert.AreEqual(EstadoHabilidad.Lista, h.Estado(HabilidadProtesis.Brazos));
            Assert.AreEqual(0, Gestos.Activaciones);
            SoltarGrips();
            yield return Cuadros(2);
        }

        [UnityTest]
        public IEnumerator UnaSolaManoEnPose_NoCuenta()
        {
            yield return Preparar(EspacioImplante.Piernas, 1);
            yield return PonerManosYGrips(MusloIzq, ManoDerechaCuarto, 1f);
            Assert.AreEqual(GestoProtesisVR.Ninguno, Gestos.PoseActual, "Hacen falta las DOS manos en la pose.");
            Assert.AreEqual(EstadoHabilidad.Lista, Habilidades.Estado(HabilidadProtesis.Piernas));
            SoltarGrips();
            yield return Cuadros(2);
        }

        // ---- Guía ----

        [UnityTest]
        public IEnumerator Guia_PrimeraVezSaleSolaConLosModelosReales_YLaSegundaNo()
        {
            PlayerPrefs.DeleteKey(GuiaControlesVR.ClaveVista);
            yield return ArmarVR();
            Assert.IsNotNull(jugador.GetComponent<ArranqueGuiaVR>(), "La primera vez el jugador VR espera para mostrar la guía.");
            // Con el simulador solo sale si se pide (se pide ya armado: con el tiempo en 0 la espera de ArmarVR no terminaría).
            GuiaControlesVR.MostrarEnSimulador = true;
            float fin = Time.unscaledTime + 4f;
            while (!GuiaControlesVR.Abierta && Time.unscaledTime < fin)
            {
                yield return null;
            }
            Assert.IsTrue(GuiaControlesVR.Abierta, "La guía sale sola la primera vez en VR.");
            Assert.IsTrue(ArranqueGuiaVR.YaVista, "Queda guardada en PlayerPrefs (" + GuiaControlesVR.ClaveVista + ").");
            GuiaControlesVR guia = GuiaControlesVR.Actual;
            yield return Cuadros(2);

            // VR31: páginas con las imágenes (importadas como Sprite), etiquetas en la de botones y frase en las de gestos.
            Assert.AreEqual(0, guia.Pagina, "Empieza en la página de botones.");
            Assert.AreEqual(6, GuiaControlesVR.Paginas);
            Assert.IsNotNull(guia.Imagen.sprite, "La imagen de la página está en Resources/GuiaVR.");
            StringAssert.Contains("Controles_Botones", guia.Imagen.sprite.name);
            Assert.AreEqual(TextosGuiaVR.Etiquetas.Length, guia.Etiquetas.Count, "Una etiqueta por botón.");
            Assert.IsTrue(guia.Etiquetas[0].gameObject.activeInHierarchy, "Las etiquetas se ven en la página de botones.");
            guia.IrSiguiente();
            Assert.AreEqual(1, guia.Pagina);
            StringAssert.Contains("SacarPistola", guia.Imagen.sprite.name);
            StringAssert.Contains("PISTOLA", guia.Frase);
            Assert.IsFalse(guia.Etiquetas[0].gameObject.activeInHierarchy, "En los gestos no van las etiquetas de los botones.");
            guia.IrAnterior();
            guia.IrAnterior();
            Assert.AreEqual(GuiaControlesVR.Paginas - 1, guia.Pagina, "ANTERIOR da la vuelta.");
            StringAssert.Contains("BRAZOS", guia.Frase);
            guia.IrA(0);
            var textos = new List<string>();
            foreach (Text t in guia.Etiquetas)
            {
                textos.Add(t.text);
            }
            string todo = string.Join(" | ", textos);
            StringAssert.Contains("SALTAR", todo);
            StringAssert.Contains("DISPARAR", todo);
            StringAssert.Contains("RECARGAR", todo);
            StringAssert.Contains("PAUSA", todo);
            Assert.IsNotNull(guia.BotonCerrar);
            Assert.AreEqual(RenderMode.WorldSpace, guia.Canvas.renderMode);
            float distancia = Vector3.Distance(guia.Canvas.transform.position, adaptador.Camara.transform.position);
            Assert.AreEqual(GuiaControlesVR.Distancia, distancia, 0.15f, "A ~1 m de la cabeza.");
            Assert.IsTrue(MenusVR.BloqueaDisparo, "Con la guía abierta el gatillo no dispara.");
            Assert.AreEqual(0f, Time.timeScale, 1e-4f, "Abierta sola detiene el juego.");

            // B la cierra y el juego sigue.
            yield return Pulsar(controlDer.secondaryButton);
            yield return Cuadros(2);
            Assert.IsFalse(GuiaControlesVR.Abierta, "B cierra la guía.");
            Assert.AreEqual(1f, Time.timeScale, 1e-4f);
            Assert.IsTrue(PilaPantallas.Vacia);

            // Segunda vez (otro jugador VR): ya no sale sola.
            GuiaControlesVR.MostrarEnSimulador = false;
            Object.Destroy(jugador);
            yield return Cuadros(3);
            yield return ArmarVR();
            GuiaControlesVR.MostrarEnSimulador = true;
            Assert.IsNull(jugador.GetComponent<ArranqueGuiaVR>(), "Ya vista: no se espera para mostrarla.");
            yield return SegundosReales(2f);
            Assert.IsFalse(GuiaControlesVR.Abierta, "La segunda vez no sale sola.");
        }

        [UnityTest]
        public IEnumerator Guia_SeAbreDesdeLaPausa_YSeCierraConElGatilloSobreCerrar()
        {
            yield return ArmarVR();
            var objeto = new GameObject("PantallaPausaPruebaGuiaVR");
            extras.Add(objeto);
            PantallaPausa pausa = objeto.AddComponent<PantallaPausa>();
            yield return Segundos(0.6f);
            yield return Pulsar(controlIzq.menuButton);
            yield return Cuadros(4);
            Assert.IsTrue(pausa.Abierta, "Menú abre la pausa.");
            BotonCyberfix controles = null;
            foreach (BotonCyberfix b in pausa.GetComponentsInChildren<BotonCyberfix>(true))
            {
                if (b.Etiqueta == TextosGuiaVR.BotonPausa)
                {
                    controles = b;
                }
            }
            Assert.IsNotNull(controles, "En VR la pausa tiene CONTROLES.");
            Assert.IsTrue(controles.gameObject.activeInHierarchy);

            controles.Pulsar();
            yield return Cuadros(2);
            Assert.IsTrue(GuiaControlesVR.Abierta, "CONTROLES abre la guía.");
            GuiaControlesVR guia = GuiaControlesVR.Actual;
            Assert.IsTrue(guia.DesdePausa);
            Assert.IsTrue(pausa.Abierta, "La pausa sigue abierta debajo.");
            CanvasGroup grupo = pausa.GetComponentInChildren<Canvas>(true).GetComponent<CanvasGroup>();
            Assert.IsNotNull(grupo);
            Assert.AreEqual(0f, grupo.alpha, 1e-4f, "La pausa se esconde mientras la guía está abierta.");

            // B la cierra y vuelve a la pausa.
            yield return Pulsar(controlDer.secondaryButton);
            yield return Cuadros(2);
            Assert.IsFalse(GuiaControlesVR.Abierta, "B cierra la guía.");
            Assert.IsTrue(pausa.Abierta, "Se vuelve a la pausa.");
            Assert.AreEqual(1f, grupo.alpha, 1e-4f);

            // Otra vez, ahora con el gatillo apuntando a CERRAR.
            controles.Pulsar();
            yield return Cuadros(2);
            guia = GuiaControlesVR.Actual;
            Assert.IsNotNull(guia);
            yield return ApuntarA(guia.BotonCerrar.transform.position);
            Assert.IsTrue(guia.CerrarApuntado, "El rayo del control derecho apunta a CERRAR.");
            yield return Pulsar(controlDer.triggerButton);
            yield return Cuadros(2);
            Assert.IsFalse(GuiaControlesVR.Abierta, "El gatillo sobre CERRAR cierra la guía.");
            Assert.IsTrue(pausa.Abierta);
            pausa.Cerrar();
            yield return Cuadros(2);
        }

        // ---- Textos ----

        [Test]
        public void TextosDeProtesisEnVR_NoDicenTab()
        {
            var regla = new ReglaTutorialProtesis();
            var textos = new List<string> { regla.TextoVR(true, true), regla.TextoVR(true, false), regla.TextoVR(false, true) };
            StringAssert.Contains("MUSLOS", regla.TextoVR(true, false), "El primer paso en VR enseña el gesto de piernas.");
            StringAssert.Contains("TAB", regla.Texto("TAB", true, true), "En PC sigue diciendo Tab.");
            regla.Avanzar(true, false, false, false, false, true);
            Assert.AreEqual(PasoTutorialProtesis.Piernas, regla.Paso);
            textos.Add(regla.TextoVR(true, true));
            regla.Avanzar(false, true, true, false, false, true);
            Assert.AreEqual(PasoTutorialProtesis.Brazos, regla.Paso);
            textos.Add(regla.TextoVR(true, true));
            textos.Add(ReglaTutorial.TextoHabilidadesVR(true, true, true));
            textos.Add(ReglaTutorial.TextoHabilidadesVR(false, false, true));
            textos.Add(RuedaHabilidadesExo.TextoCentroVR);
            textos.Add(TextosGuiaVR.GestoPiernas);
            textos.Add(TextosGuiaVR.GestoBrazos);
            foreach (var e in TextosGuiaVR.Etiquetas)
            {
                textos.Add(e.texto);
            }
            foreach (string t in textos)
            {
                Assert.IsFalse(string.IsNullOrEmpty(t));
                Assert.IsFalse(t.ToUpperInvariant().Contains("TAB"), "Sin Tab en VR: " + t);
                Assert.IsFalse(t.ToUpperInvariant().Contains("RATÓN"), "Sin ratón en VR: " + t);
            }
        }
    }
}
