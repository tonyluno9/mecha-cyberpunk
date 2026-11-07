using MechaCyberpunk.Progresion.Prologo.Pandillas;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Progresion
{
    public class ReglaAsaltoT717Pruebas
    {
        [Test]
        public void Flujo_RoboHuidaPeleaYDineroDeVuelta()
        {
            ReglaAsalto r = new ReglaAsalto();
            Assert.AreEqual(FaseAsalto.Inactivo, r.Fase);
            r.Iniciar(500);
            Assert.AreEqual(FaseAsalto.Cinematica, r.Fase);
            Assert.IsTrue(r.PuedeSaltar);
            Assert.AreEqual(500, r.Robar(), "El asaltante se lleva todo");
            Assert.AreEqual(0, r.Robar(), "Solo se roba una vez");
            Assert.IsTrue(r.DineroEnManosDelLadron);
            Assert.IsTrue(r.Abordar());
            Assert.AreEqual(FaseAsalto.Persecucion, r.Fase);
            Assert.IsFalse(r.PuedeSaltar);
            Assert.IsTrue(r.CarroDetenido());
            Assert.AreEqual(FaseAsalto.Enfrentando, r.Fase);
            Assert.AreEqual(500, r.AsaltanteVencido(), "Al vencerlo vuelve el dinero");
            Assert.AreEqual(FaseAsalto.Terminado, r.Fase);
            Assert.IsFalse(r.DineroEnManosDelLadron);
        }

        [Test]
        public void SaltarLaCinematica_HaceElRoboAunqueNoSeHayaVisto()
        {
            ReglaAsalto r = new ReglaAsalto();
            r.Iniciar(120);
            Assert.IsTrue(r.Abordar());
            Assert.IsTrue(r.DineroEnManosDelLadron, "El robo pasa aunque se salte");
            Assert.AreEqual(0, r.AsaltanteVencido(), "Sin fase de pelea no devuelve nada todavía");
            r.CarroDetenido();
            Assert.AreEqual(120, r.AsaltanteVencido());
        }

        [Test]
        public void LasFasesNoSeSaltan()
        {
            ReglaAsalto r = new ReglaAsalto();
            r.Iniciar(50);
            Assert.IsFalse(r.CarroDetenido(), "El carro no se detiene antes de la persecución");
            Assert.AreEqual(0, r.AsaltanteVencido(), "No hay pelea antes de que se detenga el carro");
            r.Abordar();
            Assert.AreEqual(0, r.AsaltanteVencido(), "No se vence al asaltante en plena huida");
            Assert.IsFalse(r.Abordar(), "Solo se aborda una vez");
        }

        [Test]
        public void Cancelar_DevuelveElDineroSoloSiSeLoHabianQuitado()
        {
            ReglaAsalto r = new ReglaAsalto();
            r.Iniciar(300);
            Assert.AreEqual(0, r.Cancelar(), "Si no te había quitado nada, no hay nada que devolver");
            Assert.AreEqual(FaseAsalto.Inactivo, r.Fase);
            r.Iniciar(300);
            r.Robar();
            Assert.AreEqual(300, r.Cancelar(), "Si ya te lo había quitado, vuelve");
            Assert.AreEqual(0, r.Cancelar(), "Una sola vez");
        }

        [Test]
        public void SinDinero_ElAsaltoSigueConCeroRobado()
        {
            ReglaAsalto r = new ReglaAsalto();
            r.Iniciar(-5);
            Assert.AreEqual(0, r.DineroRobado);
            Assert.AreEqual(0, r.Robar());
            Assert.IsTrue(r.Abordar());
        }

        [Test]
        public void ObjetivosDelAsalto_SonFrasesCortasYNoHayFaseSinObjetivoActiva()
        {
            foreach (FaseAsalto fase in System.Enum.GetValues(typeof(FaseAsalto)))
            {
                string texto = ReglaAsalto.TextoObjetivo(fase);
                bool activa = fase == FaseAsalto.Cinematica || fase == FaseAsalto.Persecucion || fase == FaseAsalto.Enfrentando;
                Assert.AreEqual(activa, texto.Length > 0, fase + ": el objetivo existe solo mientras el asalto está en curso");
                Assert.LessOrEqual(texto.Length, DirectorPandillas.LargoMaximoObjetivo, fase + ": frase demasiado larga");
            }
            Assert.AreEqual(DirectorPandillas.TextoDelPaso(PasoHistoria.Asalto), ReglaAsalto.TextoObjetivo(FaseAsalto.Cinematica));
        }

        [Test]
        public void SeguimientoObjetivo_Reubicar_MueveElDestinoSinBorrarLaDistancia()
        {
            MechaCyberpunk.Progresion.SeguimientoObjetivo s = new MechaCyberpunk.Progresion.SeguimientoObjetivo(3f, 5f, 2f, 1f);
            s.FijarPosicion(new UnityEngine.Vector3(100f, 0f, 0f));
            s.Avanzar(0.1f, UnityEngine.Vector3.zero);
            Assert.AreEqual(100f, s.Distancia, 0.01f);
            s.Reubicar(new UnityEngine.Vector3(110f, 0f, 0f));
            Assert.AreEqual(100f, s.Distancia, 0.01f, "La distancia no se borra al moverse el destino (sin parpadeo)");
            Assert.IsTrue(s.PosicionResuelta);
            Assert.AreEqual(110f, s.Posicion.x, 0.01f);
            s.Avanzar(0.1f, UnityEngine.Vector3.zero);
            Assert.AreEqual(110f, s.Distancia, 0.01f);
            Assert.AreEqual(110f, s.MejorDistancia, 0.01f, "La mejor distancia se vuelve a medir con el destino nuevo");
        }
    }
}
