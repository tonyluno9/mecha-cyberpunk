using System;
using MechaCyberpunk.UI.Hud;
using NUnit.Framework;
using UnityEngine.InputSystem;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>T-718: el tutorial completo (caminar, correr, agacharse, rodar, golpes, pistola, carro, moto, teléfono, objetos, escudo) con las teclas reales, sin pasos que se traben.</summary>
    public class TutorialTeclasRealesT718Pruebas
    {
        private static string Marca(string nombre) => "<" + nombre + ">";

        [Test]
        public void Texto_LasTeclasSalenDelResolver_NoEstanEscritasEnElTexto()
        {
            string[] fijas = { "W A S D", "SHIFT", "MAYÚS", "ESPACIO", "CLIC", "CTRL", " Q ", " R ", " E ", " M ", " T ", "TAB", " F " };
            foreach (AccionTutorial accion in Enum.GetValues(typeof(AccionTutorial)))
            {
                string texto = ReglaTutorial.Texto(accion, Marca);
                Assert.IsNotEmpty(texto, accion.ToString());
                if (accion != AccionTutorial.Escudo)
                {
                    StringAssert.Contains("<", texto, accion + ": el aviso pide su tecla al Input System");
                }
                foreach (string fija in fijas)
                {
                    StringAssert.DoesNotContain(fija, " " + texto + " ", accion + ": no debe traer la tecla escrita (" + fija.Trim() + ")");
                }
            }
        }

        [Test]
        public void Texto_ConLaTeclaCambiada_MuestraLaNueva()
        {
            string texto = ReglaTutorial.Texto(AccionTutorial.Correr, n => n == ReglaTutorial.TeclaCorrer ? "Z" : "?");
            StringAssert.Contains("Z", texto);
            Assert.AreEqual("MANTÉN SHIFT PARA CORRER", ReglaTutorial.Texto(AccionTutorial.Correr), "Sin resolver, las de siempre");
        }

        [Test]
        public void Cadena_TodaAccionLlegaAMoverse_SinCiclos()
        {
            foreach (AccionTutorial accion in Enum.GetValues(typeof(AccionTutorial)))
            {
                AccionTutorial actual = accion;
                int pasos = 0;
                while (ReglaTutorial.Requisito(actual).HasValue)
                {
                    actual = ReglaTutorial.Requisito(actual).Value;
                    Assert.Less(++pasos, 20, accion + ": requisitos en ciclo");
                }
                Assert.AreEqual(AccionTutorial.Moverse, actual, accion + " depende de moverse");
            }
        }

        [Test]
        public void Recorrido_ConTodoDisponible_SeOfreceCadaAccionYNingunaSeTraba()
        {
            ReglaTutorial regla = new ReglaTutorial(3f, 0f);
            int vueltas = 0;
            while (regla.CantidadVistas < Enum.GetValues(typeof(AccionTutorial)).Length && vueltas++ < 200)
            {
                regla.Avanzar(0.1f, a => true);
                if (regla.HayAviso)
                {
                    regla.Hecha(regla.Aviso); // la persona hace lo que dice el aviso
                }
            }
            foreach (AccionTutorial accion in Enum.GetValues(typeof(AccionTutorial)))
            {
                Assert.IsTrue(regla.Vista(accion), accion + " se ofreció");
            }
        }

        [Test]
        public void Recorrido_SiUnaAccionNuncaSePuedeHacer_LasDemasSiguen()
        {
            // Sin objetos que usar, sin escudo y sin motos cerca el resto del tutorial no se queda esperando.
            ReglaTutorial regla = new ReglaTutorial(2f, 0f);
            Func<AccionTutorial, bool> disponible = a => a != AccionTutorial.Consumibles && a != AccionTutorial.Escudo && a != AccionTutorial.Moto;
            for (int i = 0; i < 300; i++)
            {
                regla.Avanzar(2.1f, disponible);
            }
            foreach (AccionTutorial accion in Enum.GetValues(typeof(AccionTutorial)))
            {
                bool esperaba = accion == AccionTutorial.Consumibles || accion == AccionTutorial.Escudo || accion == AccionTutorial.Moto;
                Assert.AreEqual(!esperaba, regla.Vista(accion), accion.ToString());
            }
        }

        [Test]
        public void TextoMover_SaleDeLasCuatroTeclasDeLaAccion()
        {
            InputAction mover = new InputAction("Mover");
            mover.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s").With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            Assert.AreEqual("W A S D", TeclasHud.TextoMover(mover, "?"));
            mover.Dispose();
            Assert.AreEqual("respaldo", TeclasHud.TextoMover(null, "respaldo"));

            InputAction otra = new InputAction("Mover");
            otra.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/i").With("Down", "<Keyboard>/k").With("Left", "<Keyboard>/j").With("Right", "<Keyboard>/l");
            Assert.AreEqual("I J K L", TeclasHud.TextoMover(otra, "?"), "Si el jugador las cambió, se ven las suyas");
            otra.Dispose();
        }

        [Test]
        public void TeclasPorOmision_CoincidenConLasAccionesDelJugador()
        {
            // Los nombres que pide el tutorial existen en el mapa de controles del jugador (ControlesAPie): si se renombra una, esta prueba avisa.
            string ruta = "Assets/_Proyecto/Personaje/Entrada/ControlesAPie.inputactions";
            InputActionAsset asset = UnityEditor.AssetDatabase.LoadAssetAtPath<InputActionAsset>(ruta);
            Assert.IsNotNull(asset);
            foreach (string nombre in new[] { ReglaTutorial.TeclaMover, ReglaTutorial.TeclaCorrer, ReglaTutorial.TeclaSaltar, ReglaTutorial.TeclaGolpe,
                ReglaTutorial.TeclaApuntar, ReglaTutorial.TeclaRecargar, ReglaTutorial.TeclaInteractuar, ReglaTutorial.TeclaTelefono })
            {
                Assert.IsNotNull(asset.FindAction("APie/" + nombre), "Falta la acción " + nombre + " en el mapa APie");
            }
        }
    }
}
