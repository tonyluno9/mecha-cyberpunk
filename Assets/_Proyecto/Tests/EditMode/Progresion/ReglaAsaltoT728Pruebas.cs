using MechaCyberpunk.Progresion.Prologo.Pandillas;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Progresion
{
    /// <summary>T-728: el asalto rehecho. A quien asaltan es una persona de afuera: tu dinero no se toca, y al detener al asaltante la víctima te da una parte.</summary>
    public class ReglaAsaltoT728Pruebas
    {
        [Test]
        public void Flujo_RoboHuidaPeleaYRecompensaDeLaVictima()
        {
            ReglaAsalto r = new ReglaAsalto();
            Assert.AreEqual(FaseAsalto.Inactivo, r.Fase);
            r.Iniciar();
            Assert.AreEqual(FaseAsalto.Cinematica, r.Fase);
            Assert.IsTrue(r.PuedeSaltar);
            Assert.AreEqual(ReglaAsalto.DineroDeLaVictima, r.Robar(), "El asaltante le quita su dinero a la víctima");
            Assert.AreEqual(0, r.Robar(), "Solo se roba una vez");
            Assert.IsTrue(r.DineroEnManosDelLadron);
            Assert.IsTrue(r.Abordar());
            Assert.AreEqual(FaseAsalto.Persecucion, r.Fase);
            Assert.IsFalse(r.PuedeSaltar);
            Assert.IsTrue(r.CarroDetenido());
            Assert.AreEqual(FaseAsalto.Enfrentando, r.Fase);
            Assert.AreEqual(150, r.AsaltanteVencido(), "Al vencerlo la víctima te da la mitad de lo robado");
            Assert.AreEqual(FaseAsalto.Terminado, r.Fase);
            Assert.IsFalse(r.DineroEnManosDelLadron);
        }

        [Test]
        public void LaRecompensa_EsLaMitadYNuncaNegativa()
        {
            Assert.AreEqual(150, ReglaAsalto.Recompensa(300));
            Assert.AreEqual(0, ReglaAsalto.Recompensa(0));
            Assert.AreEqual(0, ReglaAsalto.Recompensa(-40));
            Assert.AreEqual(ReglaAsalto.DineroDeLaVictima / 2, ReglaAsalto.Recompensa(ReglaAsalto.DineroDeLaVictima));
        }

        [Test]
        public void SaltarLaCinematica_HaceElRoboAunqueNoSeHayaVisto()
        {
            ReglaAsalto r = new ReglaAsalto();
            r.Iniciar();
            Assert.IsTrue(r.Abordar());
            Assert.IsTrue(r.DineroEnManosDelLadron, "El robo pasa aunque se salte");
            Assert.AreEqual(0, r.AsaltanteVencido(), "Sin fase de pelea no da nada todavía");
            r.CarroDetenido();
            Assert.AreEqual(150, r.AsaltanteVencido());
        }

        [Test]
        public void LasFasesNoSeSaltan()
        {
            ReglaAsalto r = new ReglaAsalto();
            r.Iniciar();
            Assert.IsFalse(r.CarroDetenido(), "El carro no se detiene antes de la persecución");
            Assert.AreEqual(0, r.AsaltanteVencido(), "No hay pelea antes de que se detenga el carro");
            r.Abordar();
            Assert.AreEqual(0, r.AsaltanteVencido(), "No se vence al asaltante en plena huida");
            Assert.IsFalse(r.Abordar(), "Solo se aborda una vez");
        }

        [Test]
        public void Cancelar_DejaElAsaltoInactivoYSinPremio()
        {
            ReglaAsalto r = new ReglaAsalto();
            r.Iniciar();
            r.Robar();
            r.Cancelar();
            Assert.AreEqual(FaseAsalto.Inactivo, r.Fase);
            Assert.IsFalse(r.DineroEnManosDelLadron);
            Assert.AreEqual(0, r.AsaltanteVencido());
        }

        [Test]
        public void Objetivos_PrimeroRobasUnCarroYLuegoLosPersigues()
        {
            Assert.AreEqual(AsaltoDinero.TextoRobarCarro, ReglaAsalto.TextoObjetivo(FaseAsalto.Cinematica, false));
            Assert.AreEqual(AsaltoDinero.TextoRobarCarro, ReglaAsalto.TextoObjetivo(FaseAsalto.Persecucion, false), "A pie: róbate un carro");
            Assert.AreEqual(AsaltoDinero.TextoPerseguir, ReglaAsalto.TextoObjetivo(FaseAsalto.Persecucion, true), "Ya en un carro: persíguelos");
            Assert.AreEqual(AsaltoDinero.TextoRecuperar, ReglaAsalto.TextoObjetivo(FaseAsalto.Enfrentando, true));
            Assert.AreEqual(string.Empty, ReglaAsalto.TextoObjetivo(FaseAsalto.Inactivo, false));
            Assert.AreEqual(string.Empty, ReglaAsalto.TextoObjetivo(FaseAsalto.Terminado, true));
            Assert.AreEqual("RÓBATE UN CARRO", AsaltoDinero.TextoRobarCarro);
            Assert.AreEqual("PERSÍGUELOS", AsaltoDinero.TextoPerseguir);
        }

        [Test]
        public void ObjetivosDelAsalto_SonFrasesCortas()
        {
            foreach (FaseAsalto fase in System.Enum.GetValues(typeof(FaseAsalto)))
            {
                foreach (bool enVehiculo in new[] { false, true })
                {
                    Assert.LessOrEqual(ReglaAsalto.TextoObjetivo(fase, enVehiculo).Length, DirectorPandillas.LargoMaximoObjetivo, fase + ": frase demasiado larga");
                }
            }
            Assert.AreEqual(DirectorPandillas.TextoDelPaso(PasoHistoria.Asalto), ReglaAsalto.TextoObjetivo(FaseAsalto.Cinematica, false));
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
