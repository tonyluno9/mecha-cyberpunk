using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Vida;
using MechaCyberpunk.Progresion;
using MechaCyberpunk.UI.Datos;
using MechaCyberpunk.UI.Rueda;
using MechaCyberpunk.UI.Tiendas;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests
{
    /// <summary>
    /// T-1529: la tiendita de la esquina (reglas y datos), las ranuras de consumibles tras gastar el primero (bebida y luego papitas) y la vida máxima
    /// que dan los implantes. Los chequeos de datos piden haber corrido CreadorTienditaT1529.Aplicar y VidaImplantesT1529.Aplicar.
    /// </summary>
    public class TiendaConsumiblesT1529Pruebas
    {
        private static readonly string[] CarpetaImplantes = { "Assets/_Proyecto/Progresion/Datos/Implantes" };

        [SetUp]
        public void SetUp() => RanurasConsumibles.Reiniciar();

        [TearDown]
        public void TearDown() => RanurasConsumibles.Reiniciar();

        // ---- Ranuras: bebida y luego papitas ----

        [Test]
        public void Ranuras_SeGastaLaBebida_LasPapitasSiguenEnLaRanura2()
        {
            string[] r = RanurasConsumibles.Efectivos(new[] { "bebida", "papitas" });
            Assert.AreEqual("bebida", r[0]);
            Assert.AreEqual("papitas", r[1]);
            // La bebida se acaba: ya solo lleva papitas.
            r = RanurasConsumibles.Efectivos(new[] { "papitas" });
            Assert.IsNull(r[0], "La ranura 1 queda vacía");
            Assert.AreEqual("papitas", r[1], "Las papitas siguen en la ranura 2 (tecla C), no saltan a la 1");
        }

        [Test]
        public void Ranuras_SeCompraOtraCosaConLaRanura1Vacia_LaTomaElNuevo()
        {
            RanurasConsumibles.Efectivos(new[] { "bebida", "papitas" });
            RanurasConsumibles.Efectivos(new[] { "papitas" });
            string[] r = RanurasConsumibles.Efectivos(new[] { "papitas", "venda" });
            Assert.AreEqual("venda", r[0]);
            Assert.AreEqual("papitas", r[1]);
        }

        [Test]
        public void Ranuras_SiVuelveALlevarLaBebida_RegresaALaRanura1()
        {
            RanurasConsumibles.Efectivos(new[] { "bebida", "papitas" });
            RanurasConsumibles.Efectivos(new[] { "papitas" });
            string[] r = RanurasConsumibles.Efectivos(new[] { "bebida", "papitas" });
            Assert.AreEqual("bebida", r[0]);
            Assert.AreEqual("papitas", r[1]);
        }

        // ---- Tiendita ----

        [Test]
        public void Tiendita_ElFrenteMiraAMenosZGiradoYLaDistanciaSaleDeLaCaja()
        {
            Vector3 frente = ReglaTiendita.Frente(180f);
            Assert.Less((frente - Vector3.forward).magnitude, 1e-4f, "Con 180 grados el frente (-Z) mira a +Z");
            Assert.AreEqual(3f, ReglaTiendita.DistanciaAlFrente(frente, new Vector3(5f, 3f, 3f)), 1e-4f, "Mira a +Z: cuenta la mitad en z");
            Assert.AreEqual(5f, ReglaTiendita.DistanciaAlFrente(ReglaTiendita.Frente(90f), new Vector3(5f, 3f, 3f)), 1e-3f, "Girado 90: cuenta la mitad en x");
        }

        [Test]
        public void Tiendita_MostradorDelanteDelEdificio_TenderoEntreLaParedYElMostrador()
        {
            Vector3 frente = ReglaTiendita.Frente(180f);
            Vector3 centro = new Vector3(10f, 3f, 20f);
            Vector3 mitad = new Vector3(4f, 3f, 3f);
            Vector3 mostrador = ReglaTiendita.PosicionMostrador(centro, frente, mitad, 0.2f);
            Vector3 tendero = ReglaTiendita.PosicionTendero(centro, frente, mitad, 0.2f);
            Assert.AreEqual(0.2f, mostrador.y, 1e-4f, "A la altura del piso del sitio");
            Assert.AreEqual(20f + 3f + ReglaTiendita.SeparacionMostrador, mostrador.z, 1e-3f);
            Assert.Less(tendero.z, mostrador.z, "El tendero queda detrás del mostrador");
            Assert.Greater(tendero.z, 20f + 3f, "y fuera de la pared del frente");
        }

        [Test]
        public void Tiendita_ElegirSitio_PrimeroLibre_OElPrimeroSiNingunoLoEsta()
        {
            Assert.AreEqual(2, ReglaTiendita.ElegirSitio(new[] { false, false, true, true }));
            Assert.AreEqual(0, ReglaTiendita.ElegirSitio(new[] { false, false }));
            Assert.AreEqual(-1, ReglaTiendita.ElegirSitio(new bool[0]));
            Assert.AreEqual(-1, ReglaTiendita.ElegirSitio(null));
        }

        [Test]
        public void Tiendita_DatosListos_MaquinasDeDecoradoYTienditaConLosCincoConsumibles()
        {
            var datos = AssetDatabase.LoadAssetAtPath<DatosExpendedoras>("Assets/_Proyecto/UI/Datos/Resources/DatosExpendedoras_Ciudad.asset");
            Assert.IsNotNull(datos);
            Assert.IsFalse(datos.MaquinasVenden, "Las máquinas ya no venden consumibles (CreadorTienditaT1529.Aplicar)");
            Assert.IsNotNull(datos.Tiendita, "Edificio ED02");
            Assert.IsNotNull(datos.Mostrador, "Mostrador");
            Assert.IsNotNull(datos.Tendero, "Tendero");
            Assert.IsNotNull(datos.TiendaTiendita);
            Assert.AreEqual("Tiendita", datos.TiendaTiendita.Nombre);
            Assert.AreEqual(5, datos.TiendaTiendita.Ofertas.Count);
            Assert.GreaterOrEqual(datos.SitiosTiendita.Count, 1);
            StringAssert.Contains("ED02", AssetDatabase.GetAssetPath(datos.Tiendita));
        }

        // ---- Implantes: vida máxima ----

        [Test]
        public void Implantes_ReglaDeVidaPorNivel_DiezYVeinticinco()
        {
            Assert.AreEqual(0f, ReglaImplantes.VidaMaximaExtra(0));
            Assert.AreEqual(10f, ReglaImplantes.VidaMaximaExtra(1));
            Assert.AreEqual(25f, ReglaImplantes.VidaMaximaExtra(2));
        }

        [Test]
        public void Implantes_CombinarSumaLaVidaMaxima_YNingunoNoCambiaNada()
        {
            EfectosImplantes a = EfectosImplantes.Ninguno;
            a.vidaMaximaExtra = 10f;
            EfectosImplantes b = EfectosImplantes.Ninguno;
            b.vidaMaximaExtra = 25f;
            Assert.AreEqual(35f, EfectosImplantes.Combinar(a, b).vidaMaximaExtra, 1e-4f);
            Assert.AreEqual(10f, EfectosImplantes.Combinar(a, EfectosImplantes.Ninguno).vidaMaximaExtra, 1e-4f);
            Assert.AreEqual(0f, EfectosImplantes.Ninguno.vidaMaximaExtra);
        }

        [Test]
        public void Implantes_LosOchoDatosLlevanSuVida_SegunSuNivel()
        {
            string[] guids = AssetDatabase.FindAssets("t:DatosImplante", CarpetaImplantes);
            Assert.AreEqual(8, guids.Length, "Cuatro espacios por dos niveles");
            foreach (string guid in guids)
            {
                var d = AssetDatabase.LoadAssetAtPath<DatosImplante>(AssetDatabase.GUIDToAssetPath(guid));
                Assert.AreEqual(ReglaImplantes.VidaMaximaExtra(d.Nivel), d.Efectos.vidaMaximaExtra, 1e-4f, d.Id + " lleva su vida máxima (VidaImplantesT1529.Aplicar)");
            }
        }

        [Test]
        public void Modelo_PonerMaxima_SubeLaVidaDeAhoraLoMismo_YAlBajarNoPasaDelMaximo()
        {
            var modelo = new ModeloVidaRegenerable(100f, 0f, 0f);
            modelo.Aplicar(30f);
            modelo.PonerMaxima(125f);
            Assert.AreEqual(125f, modelo.Maxima);
            Assert.AreEqual(95f, modelo.Actual, 1e-4f, "70 + 25 de vida nueva");
            modelo.PonerMaxima(100f);
            Assert.AreEqual(100f, modelo.Maxima);
            Assert.AreEqual(95f, modelo.Actual, 1e-4f);
            modelo.PonerMaxima(90f);
            Assert.AreEqual(90f, modelo.Actual, 1e-4f, "No queda por encima del máximo");
        }

        [Test]
        public void Modelo_PonerMaxima_ConElMismoValor_NoAvisa()
        {
            var modelo = new ModeloVidaRegenerable(100f, 0f, 0f);
            int avisos = 0;
            modelo.Cambio += () => avisos++;
            modelo.PonerMaxima(100f);
            Assert.AreEqual(0, avisos);
        }
    }
}
