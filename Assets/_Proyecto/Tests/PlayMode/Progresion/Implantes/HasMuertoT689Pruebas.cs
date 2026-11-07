using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Vida;
using MechaCyberpunk.Progresion;
using MechaCyberpunk.Progresion.Prologo.Clinica;
using MechaCyberpunk.Tests.Personaje;
using MechaCyberpunk.UI.Historia;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests
{
    /// <summary>
    /// T-689: al morir a pie, después de la caída sale la pantalla HAS MUERTO (Teko, neón, glitch) y luego se reaparece en la clínica con
    /// un fundido. Capturas con CAPTURAR_S1=1 en docs/capturas/s1t-T-689-*.png.
    /// </summary>
    public class HasMuertoT689Pruebas
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const float MaxSegundos = 40f;

        [TearDown]
        public void Limpiar() => EventosPelea.LimpiarSuscriptores();

        [UnityTest]
        public IEnumerator Morir_MuestraHasMuertoDespuesDeLaCaida_YLuegoReapareceEnLaClinica()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            float espera = Time.time + 3f;
            while (Time.time < espera) yield return null;
            JugadorAPie jugador = Object.FindFirstObjectByType<JugadorAPie>();
            DoctorClinica doctor = Object.FindFirstObjectByType<DoctorClinica>();
            Assert.IsNotNull(jugador);
            Assert.IsNotNull(doctor);
            ServicioHistoria historia = Registro<IHistoria>.Actual as ServicioHistoria;
            historia.Avanzar(EtapaPrologo.Libre);
            VidaAPie vida = jugador.GetComponent<VidaAPie>();
            Vector3 dondeCayo = jugador.transform.position;
            Assert.IsNull(doctor.PantallaMuerte, "antes de morir no hay pantalla");

            vida.RecibirDanio(new InfoDanio(5000f, TipoDanio.Disparo, jugador.transform.position + Vector3.up, Vector3.forward, null));
            Assert.IsTrue(vida.EstaKO);
            // La caída se ve primero: durante la espera de la caída todavía no hay pantalla.
            yield return null;
            Assert.IsNull(doctor.PantallaMuerte, "la caída se ve antes de la pantalla");

            float fin = Time.time + MaxSegundos;
            while (doctor.PantallaMuerte == null && Time.time < fin) yield return null;
            PantallaAviso aviso = doctor.PantallaMuerte;
            Assert.IsNotNull(aviso, "sale la pantalla HAS MUERTO");
            Assert.AreEqual("HAS MUERTO", aviso.Titulo);
            Text principal = aviso.TextoPrincipal;
            Assert.AreEqual(UIFuenteTeko(principal), true, "el título usa la fuente de títulos del juego");
            Assert.Less((jugador.transform.position - dondeCayo).magnitude, 1f, "mientras se ve la pantalla sigue donde cayó");
            while (aviso.Visibilidad < 1f && Time.time < fin) yield return null;
            yield return new WaitForSecondsRealtime(0.4f);
            CapturaS1.FotoCanvas("s1t-T-689-has-muerto", aviso.GetComponentInChildren<Canvas>(), 1280, 720);

            while ((doctor.Ocupado || doctor.PantallaMuerte != null) && Time.time < fin) yield return null;
            Assert.IsFalse(doctor.Ocupado, "terminó la secuencia");
            Assert.IsNull(doctor.PantallaMuerte, "la pantalla se quitó");
            Assert.IsFalse(vida.EstaKO, "reapareció con vida");
            Assert.Greater((jugador.transform.position - dondeCayo).magnitude, 20f, "reapareció en la clínica, lejos de donde cayó");
        }

        private static bool UIFuenteTeko(Text texto) => texto.font != null && texto.font == MechaCyberpunk.UI.Kit.KitUI.Estilo.FuenteDe(MechaCyberpunk.UI.Kit.RolTexto.Titulo);
    }
}
