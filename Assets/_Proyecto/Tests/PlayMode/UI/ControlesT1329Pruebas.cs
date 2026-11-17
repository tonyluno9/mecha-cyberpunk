using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Habilidades;
using MechaCyberpunk.Tests.Personaje;
using MechaCyberpunk.UI.Hud;
using MechaCyberpunk.UI.Rueda;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>
    /// T-1329 (playtest 17-nov): la rueda de armas sale con el botón medio del ratón; Tab abre la rueda de habilidades del EXO (mantener Tab, mover el
    /// ratón, clic); las habilidades ya no tienen tecla suelta; el aviso de interacción sale al centro con la tecla en un cuadrito.
    /// </summary>
    public class ControlesT1329Pruebas : BaseHudObjetivo
    {
        private const string RutaCarro = "Assets/_Proyecto/Personaje/Pruebas/CarroDePrueba.prefab";

        private HabilidadesProtesis habilidades;
        private RuedaHabilidadesExo ruedaExo;
        private RuedaArmasObjetos ruedaArmas;
        private ImplantesFalsos falsos;
        private GameObject carro;

        public override void TearDown()
        {
            if (carro != null)
            {
                Object.Destroy(carro);
            }
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
            Pausa.Fijar(false);
            base.TearDown();
        }

        private IEnumerator Preparar()
        {
            CrearJugador();
            CrearHud();
            falsos = new ImplantesFalsos();
            falsos.Poner(EspacioImplante.Piernas, 1);
            falsos.Poner(EspacioImplante.Brazos, 1);
            Registro<IImplantes>.Agregar(falsos);
            yield return Calentar();
            yield return Esperar(0.3f);
            habilidades = jugador.GetComponentInChildren<HabilidadesProtesis>();
            ruedaExo = hud.GetComponent<RuedaHabilidadesExo>();
            ruedaArmas = hud.GetComponent<RuedaArmasObjetos>();
            Assert.IsNotNull(habilidades, "El jugador trae las habilidades");
            Assert.IsNotNull(ruedaExo, "El HUD a pie trae la rueda del EXO");
            Assert.IsTrue(ruedaExo.Armada);
            yield return null;
        }

        private IEnumerator Cuadros(int n = 2)
        {
            for (int i = 0; i < n; i++)
            {
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator BotonMedio_AbreLaRuedaDeArmas_YTabNoLaAbre()
        {
            yield return Preparar();
            Press(teclado.tabKey);
            yield return Cuadros();
            Assert.IsFalse(RuedaArmasObjetos.Abierta, "Tab ya no abre la rueda de armas");
            Release(teclado.tabKey);
            yield return Cuadros();
            Press(raton.middleButton);
            yield return Cuadros();
            Assert.IsTrue(RuedaArmasObjetos.Abierta, "Mantener la ruedita del ratón abre la rueda de armas");
            Assert.IsFalse(RuedaHabilidadesExo.Abierta);
            Release(raton.middleButton);
            yield return Cuadros();
            Assert.IsFalse(RuedaArmasObjetos.Abierta, "Al soltar se cierra");
            Assert.AreEqual(1f, Time.timeScale, 0.001f);
        }

        [UnityTest]
        public IEnumerator MantenerTab_MoverElRaton_Clic_ActivaLaHabilidad()
        {
            yield return Preparar();
            Assert.AreEqual(EstadoHabilidad.Lista, habilidades.Estado(HabilidadProtesis.Brazos));
            Press(teclado.tabKey);
            yield return Cuadros();
            Assert.IsTrue(RuedaHabilidadesExo.Abierta, "Mantener Tab abre la rueda del EXO");
            Assert.IsFalse(RuedaArmasObjetos.Abierta);
            Assert.AreEqual(0.25f, Time.timeScale, 0.001f);
            float angulo = RuedaHabilidadesExo.AnguloDe(HabilidadProtesis.Brazos) * Mathf.Deg2Rad;
            Set(raton.delta, new Vector2(Mathf.Sin(angulo), Mathf.Cos(angulo)) * MechaCyberpunk.UI.Rueda.ReglaRuedaArmas.RadioMaximo);
            yield return Cuadros();
            Assert.AreEqual((int)HabilidadProtesis.Brazos, ruedaExo.Resaltada, "Mover el ratón hacia BRAZOS la resalta");
            Press(raton.leftButton);
            yield return Cuadros();
            Release(raton.leftButton);
            yield return Cuadros();
            Assert.AreEqual(EstadoHabilidad.Activa, habilidades.Estado(HabilidadProtesis.Brazos), "El clic activa la habilidad resaltada");
            Release(teclado.tabKey);
            yield return Cuadros();
            Assert.IsFalse(RuedaHabilidadesExo.Abierta, "Soltar Tab cierra la rueda");
            Assert.AreEqual(1f, Time.timeScale, 0.001f);
            Assert.AreEqual(EstadoHabilidad.Lista, habilidades.Estado(HabilidadProtesis.Piernas), "Solo se activó la resaltada");
        }

        [UnityTest]
        public IEnumerator ClicSinResaltar_NoActivaNada()
        {
            yield return Preparar();
            Press(teclado.tabKey);
            yield return Cuadros();
            Press(raton.leftButton);
            yield return Cuadros();
            Release(raton.leftButton);
            Release(teclado.tabKey);
            yield return Cuadros();
            Assert.AreEqual(EstadoHabilidad.Lista, habilidades.Estado(HabilidadProtesis.Piernas));
            Assert.AreEqual(EstadoHabilidad.Lista, habilidades.Estado(HabilidadProtesis.Brazos));
        }

        [UnityTest]
        public IEnumerator LasTeclasSueltasDeLasHabilidades_YaNoHacenNada()
        {
            yield return Preparar();
            foreach (var tecla in new[] { teclado.qKey, teclado.cKey, teclado.xKey, teclado.digit4Key, teclado.digit5Key })
            {
                Press(tecla);
                yield return Cuadros();
                Release(tecla);
                yield return Cuadros();
            }
            for (int i = 0; i < 5; i++)
            {
                Assert.AreEqual(EstadoHabilidad.Lista, habilidades.Estado((HabilidadProtesis)i), "Sin tecla suelta: " + (HabilidadProtesis)i);
            }
        }

        [UnityTest]
        public IEnumerator AvisoDeInteraccion_SaleAlCentroConLaTeclaEnUnCuadrito_NoALaDerecha()
        {
            yield return Preparar();
            HudH10 h10 = hud.GetComponent<HudH10>();
            Assert.IsNotNull(h10.AvisoCentro);
            Assert.AreEqual(new Vector2(0.5f, 0.5f), h10.AvisoCentro.anchorMin, "Anclado al centro de la pantalla");
            Assert.AreEqual(0f, h10.AvisoCentro.anchoredPosition.x, 0.01f, "Centrado en horizontal");
            Assert.Less(h10.AvisoCentro.anchoredPosition.y, 0f, "Abajo del centro");
            Assert.IsFalse(h10.AvisoCentroVisible, "Sin nada enfrente no se ve");

            carro = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaCarro), new Vector3(2f, 0f, 1.1f), Quaternion.identity);
            yield return Esperar(0.5f);
            Assert.IsTrue(h10.AvisoCentroVisible, "Frente al carro sale el aviso");
            Assert.AreEqual("E", h10.TeclaAvisoCentro.text, "El cuadrito lleva la tecla real");
            Assert.AreEqual(ReglaAyudasH10.TextoSubirCarro, h10.TextoAvisoCentro.text);
            foreach (UnityEngine.UI.Text t in h10.TextosAyuda)
            {
                Assert.IsFalse(t.text.Contains(ReglaAyudasH10.TextoSubirCarro), "A la derecha ya no sale la interacción");
            }
            Assert.IsNotNull(h10.TeclaAvisoCentro.GetComponentInParent<UnityEngine.UI.Image>(), "La tecla va dentro de una caja");
        }
    }
}
