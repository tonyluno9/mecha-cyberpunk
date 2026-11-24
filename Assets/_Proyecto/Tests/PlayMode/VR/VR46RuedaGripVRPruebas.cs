using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Vida;
using MechaCyberpunk.UI.Rueda;
using MechaCyberpunk.VR;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.VR
{
    /// <summary>
    /// VR46: la rueda de armas en VR. Mantener X la abre; el selector no se mueve sin el grip izquierdo; con el grip la mano izquierda lo mueve y
    /// resalta; al soltar el grip se elige (equipa el arma o usa el consumible, papitas incluidas). En VR la rueda no lleva fondo.
    /// </summary>
    public class VR46RuedaGripVRPruebas : BaseJugadorVR
    {
        private const float Radio = 160f;
        private static readonly Vector3 ManoIzq = new Vector3(-0.2f, 1.2f, 0.3f);

        [Test]
        public void Regla_SinGripNoSeMueve_ConGripSigueALaMano_AlSoltarAvisa()
        {
            var regla = new ReglaRuedaGripVR();
            regla.Abrir(false);
            Assert.AreEqual(EventoRuedaGripVR.Nada, regla.Avanzar(false, ManoIzq + Vector3.right * 0.2f, Vector3.right, Vector3.up, Radio));
            Assert.AreEqual(Vector2.zero, regla.Cursor, "Sin grip el selector se queda al centro aunque la mano se mueva.");

            Vector3 inicio = ManoIzq + Vector3.right * 0.2f;
            Assert.AreEqual(EventoRuedaGripVR.Tomo, regla.Avanzar(true, inicio, Vector3.right, Vector3.up, Radio));
            Assert.AreEqual(Vector2.zero, regla.Cursor, "Al apretar el grip la mano de ese momento es el centro.");
            regla.Avanzar(true, inicio + Vector3.right * ReglaRuedaGripVR.MetrosAlBorde, Vector3.right, Vector3.up, Radio);
            Assert.AreEqual(Radio, regla.Cursor.x, 0.5f, "Mover la mano MetrosAlBorde a la derecha lleva el selector al borde.");
            regla.Avanzar(true, inicio + Vector3.up * ReglaRuedaGripVR.MetrosAlBorde * 0.5f + Vector3.forward * 0.3f, Vector3.right, Vector3.up, Radio);
            Assert.AreEqual(Radio * 0.5f, regla.Cursor.y, 0.5f, "Hacia arriba sube; hacia adelante no cuenta.");
            Assert.AreEqual(0f, regla.Cursor.x, 0.5f);
            regla.Avanzar(true, inicio + Vector3.left * 2f, Vector3.right, Vector3.up, Radio);
            Assert.AreEqual(Radio, regla.Cursor.magnitude, 0.5f, "No sale del borde.");
            // Los ejes se toman al apretar: otros ejes después (girar la cabeza) no cambian nada.
            Vector2 antes = regla.Cursor;
            regla.Avanzar(true, inicio + Vector3.left * 2f, Vector3.forward, Vector3.up, Radio);
            Assert.AreEqual(antes, regla.Cursor, "Girar la cabeza con el grip apretado no mueve el selector.");
            Assert.AreEqual(EventoRuedaGripVR.Solto, regla.Avanzar(false, inicio, Vector3.right, Vector3.up, Radio), "Soltar el grip avisa.");
            Assert.IsFalse(regla.Tomado);
        }

        [Test]
        public void Regla_GripApretadoAlAbrir_NoCuentaHastaSoltarlo()
        {
            var regla = new ReglaRuedaGripVR();
            regla.Abrir(true);
            Assert.AreEqual(EventoRuedaGripVR.Nada, regla.Avanzar(true, ManoIzq, Vector3.right, Vector3.up, Radio));
            Assert.AreEqual(EventoRuedaGripVR.Nada, regla.Avanzar(true, ManoIzq + Vector3.right, Vector3.right, Vector3.up, Radio));
            Assert.AreEqual(Vector2.zero, regla.Cursor);
            Assert.AreEqual(EventoRuedaGripVR.Nada, regla.Avanzar(false, ManoIzq, Vector3.right, Vector3.up, Radio), "Soltarlo no elige nada.");
            Assert.AreEqual(EventoRuedaGripVR.Tomo, regla.Avanzar(true, ManoIzq, Vector3.right, Vector3.up, Radio), "Apretarlo otra vez sí cuenta.");
        }

        private RuedaArmasObjetos rueda;
        private ArmasAPie armas;

        private IEnumerator AbrirRueda()
        {
            PonerMano(false, ManoIzq, Quaternion.identity);
            Mantener(controlIzq.primaryButton);
            yield return Cuadros(4);
            rueda = RuedaArmasObjetos.Actual;
            Assert.IsNotNull(rueda, "El jugador trae la rueda de armas en su HUD.");
            Assert.IsTrue(RuedaArmasObjetos.Abierta, "Mantener X abre la rueda.");
        }

        // Con el grip apretado lleva la mano hacia la casilla y la suelta.
        private IEnumerator ElegirConGrip(int casilla)
        {
            Mantener(controlIzq.gripButton);
            yield return Cuadros(2);
            float a = rueda.AnguloDe(casilla) * Mathf.Deg2Rad;
            Assert.IsFalse(float.IsNaN(a), "La casilla " + casilla + " está en la rueda.");
            Vector3 hacia = new Vector3(Mathf.Sin(a), Mathf.Cos(a), 0f) * (ReglaRuedaGripVR.MetrosAlBorde * 0.8f);
            yield return MoverMano(false, ManoIzq, ManoIzq + hacia, 0.15f, Quaternion.identity);
            yield return Cuadros(2);
            Assert.AreEqual(casilla, rueda.Resaltada, "La mano con el grip resalta la casilla bajo el selector.");
            Soltar(controlIzq.gripButton);
            yield return Cuadros(4);
        }

        [UnityTest]
        public IEnumerator X_AbreSinFondo_ElSelectorEsperaAlGrip_YSoltarloEquipa()
        {
            yield return ArmarVR();
            armas = jugador.GetComponent<ArmasAPie>();
            Assert.IsNotNull(jugador.GetComponent<RuedaArmasVR>(), "RuedaArmasVR se agrega sola al armarse el rig.");
            yield return AbrirRueda();
            Transform fondo = rueda.transform.Find("CanvasRueda/Rueda/Fondo");
            if (fondo != null)
            {
                Assert.IsFalse(fondo.GetComponent<UnityEngine.UI.Image>().enabled, "En VR la rueda no lleva fondo.");
            }

            // Sin grip, la mano se mueve y el selector no.
            yield return MoverMano(false, ManoIzq, ManoIzq + Vector3.right * 0.1f, 0.15f, Quaternion.identity);
            yield return Cuadros(2);
            Assert.AreEqual(Vector2.zero, rueda.Cursor, "Sin el grip izquierdo el selector no se mueve.");
            Assert.AreEqual(-1, rueda.Resaltada);
            PonerMano(false, ManoIzq, Quaternion.identity);
            yield return Cuadros(2);

            yield return ElegirConGrip((int)ArmaAPie.Punos);
            Assert.IsFalse(RuedaArmasObjetos.Abierta, "Soltar el grip elige y cierra (aunque X siga apretada).");
            Assert.AreEqual(ArmaAPie.Punos, armas.ArmaActiva, "Se equipan los puños.");
            Soltar(controlIzq.primaryButton);
            yield return Cuadros(3);
            Assert.IsFalse(RuedaArmasObjetos.Abierta, "Con X apretada la rueda no se vuelve a abrir sola.");

            yield return AbrirRueda();
            yield return ElegirConGrip((int)ArmaAPie.Pistola);
            Assert.AreEqual(ArmaAPie.Pistola, armas.ArmaActiva, "Se equipa la pistola desde la rueda.");
            Soltar(controlIzq.primaryButton);
            yield return Cuadros(2);
        }

        [UnityTest]
        public IEnumerator SoltarXSinGrip_CierraSinElegir_YLasPapitasSeComen()
        {
            yield return ArmarVR();
            armas = jugador.GetComponent<ArmasAPie>();
            ArmaAPie antes = armas.ArmaActiva;
            yield return AbrirRueda();
            Soltar(controlIzq.primaryButton);
            yield return Cuadros(3);
            Assert.IsFalse(RuedaArmasObjetos.Abierta, "Soltar X cierra.");
            Assert.AreEqual(antes, armas.ArmaActiva, "Sin grip no se elige nada.");

            ConsumiblesJugador consumibles = jugador.GetComponentInChildren<ConsumiblesJugador>();
            ServicioInventario inventario = Registro<ServicioInventario>.Actual;
            Assert.IsNotNull(consumibles);
            Assert.IsNotNull(inventario);
            int lugar = -1;
            for (int i = 0; i < consumibles.Catalogo.Count; i++)
            {
                if (consumibles.Catalogo[i] != null && consumibles.Catalogo[i].Id == "papitas")
                {
                    lugar = i;
                }
            }
            Assert.GreaterOrEqual(lugar, 0, "Las papitas están en el catálogo de consumibles.");
            DatosConsumible papitas = consumibles.Catalogo[lugar];
            inventario.Agregar(papitas, 2);
            VidaAPie vida = jugador.GetComponent<VidaAPie>();
            vida.RecibirDanio(new InfoDanio(30f, TipoDanio.Disparo, Vector3.zero, Vector3.forward, null));
            yield return Cuadros(2);

            yield return AbrirRueda();
            yield return ElegirConGrip(ReglaRuedaArmas.CasillaObjeto(lugar));
            Assert.AreEqual(1, inventario.Cantidad(papitas), "Soltar el grip sobre las papitas se las come.");
            Soltar(controlIzq.primaryButton);
            yield return Cuadros(2);
        }
    }
}
