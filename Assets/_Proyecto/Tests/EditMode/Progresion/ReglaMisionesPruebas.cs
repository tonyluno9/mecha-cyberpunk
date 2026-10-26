using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Progresion;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Progresion
{
    /// <summary>ReglaMisiones (T-183): disponibilidad en orden, recompensa y penalización.</summary>
    public class ReglaMisionesPruebas
    {
        private DatosMision mision;

        [TearDown]
        public void TearDown()
        {
            if (mision != null) Object.DestroyImmediate(mision);
        }

        private static PasoMision[] UnPaso() => new[] { new PasoMision(TipoPaso.IrA, "Mision_Destino_X", "Ve") };

        private static ContextoMision Bueno()
        {
            return new ContextoMision
            {
                Etapa = EtapaPrologo.Libre,
                HayReloj = true,
                Franja = FranjaDia.Dia,
                Dia = 5,
                Liga = 1,
                NivelBarrio = 0,
                NivelCorporaciones = 0,
                CantidadFavor = null,
                OtraActiva = false,
                UltimoDiaCumplida = ContextoMision.Nunca,
                ExistenPuntos = true
            };
        }

        [Test]
        public void Decidir_TodoEnOrden_Disponible()
        {
            mision = DatosMision.Crear("a", "A", TipoMision.Barrio, OrigenMision.Calle, FranjaMision.Cualquiera, UnPaso());
            Assert.AreEqual(MotivoMision.Disponible, ReglaMisiones.Decidir(mision, Bueno()));
        }

        [Test]
        public void Decidir_CadaMotivoSaleEnSuOrden()
        {
            // Una misión corporativa de noche, con todos los requisitos, y un contexto que falla todo; se arregla de a uno.
            mision = DatosMision.Crear("a", "A", TipoMision.Corporativa, OrigenMision.Telefono, FranjaMision.Noche, UnPaso(),
                repetible: false, exigeFavor: true, favorRequerido: TipoFavor.Informacion, nivelBarrioMinimo: 1, ligaMinima: 3);
            var c = new ContextoMision
            {
                Etapa = EtapaPrologo.RegresoACasa,
                HayReloj = true,
                Franja = FranjaDia.Dia,
                Dia = 5,
                Liga = 1,
                NivelBarrio = 0,
                NivelCorporaciones = -2,
                OtraActiva = true,
                UltimoDiaCumplida = 5,
                ExistenPuntos = false
            };
            Assert.AreEqual(MotivoMision.FueraDeLibre, ReglaMisiones.Decidir(mision, c));
            c.Etapa = EtapaPrologo.Libre;
            Assert.AreEqual(MotivoMision.OtraActiva, ReglaMisiones.Decidir(mision, c));
            c.OtraActiva = false;
            Assert.AreEqual(MotivoMision.FranjaEquivocada, ReglaMisiones.Decidir(mision, c));
            c.Franja = FranjaDia.Noche;
            Assert.AreEqual(MotivoMision.CorporacionesNoLlaman, ReglaMisiones.Decidir(mision, c));
            c.NivelCorporaciones = 0;
            Assert.AreEqual(MotivoMision.ReputacionBaja, ReglaMisiones.Decidir(mision, c));
            c.NivelBarrio = 1;
            Assert.AreEqual(MotivoMision.LigaBaja, ReglaMisiones.Decidir(mision, c));
            c.Liga = 3;
            Assert.AreEqual(MotivoMision.FaltaFavor, ReglaMisiones.Decidir(mision, c));
            c.CantidadFavor = t => t == TipoFavor.Informacion ? 1 : 0;
            Assert.AreEqual(MotivoMision.YaHecha, ReglaMisiones.Decidir(mision, c), "no repetible y ya hecha");
            c.UltimoDiaCumplida = ContextoMision.Nunca;
            Assert.AreEqual(MotivoMision.FaltanPuntos, ReglaMisiones.Decidir(mision, c));
            c.ExistenPuntos = true;
            Assert.AreEqual(MotivoMision.Disponible, ReglaMisiones.Decidir(mision, c));
        }

        [Test]
        public void Decidir_BarrioEnMenosDos_NingunaDeBarrioPeroSiLasDemas()
        {
            mision = DatosMision.Crear("a", "A", TipoMision.Barrio, OrigenMision.Calle, FranjaMision.Cualquiera, UnPaso());
            ContextoMision c = Bueno();
            c.NivelBarrio = -2;
            Assert.AreEqual(MotivoMision.BarrioNoTeHabla, ReglaMisiones.Decidir(mision, c));
            c.NivelBarrio = -1;
            Assert.AreEqual(MotivoMision.Disponible, ReglaMisiones.Decidir(mision, c), "en -1 todavía te hablan");

            Object.DestroyImmediate(mision);
            mision = DatosMision.Crear("b", "B", TipoMision.Robo, OrigenMision.Bar, FranjaMision.Cualquiera, UnPaso());
            c.NivelBarrio = -2;
            Assert.AreEqual(MotivoMision.Disponible, ReglaMisiones.Decidir(mision, c), "un robo no depende de que el barrio te hable");
        }

        [Test]
        public void Decidir_SinReloj_ValeCualquierFranja()
        {
            mision = DatosMision.Crear("a", "A", TipoMision.Robo, OrigenMision.Bar, FranjaMision.Noche, UnPaso());
            ContextoMision c = Bueno();
            c.Franja = FranjaDia.Dia;
            Assert.AreEqual(MotivoMision.FranjaEquivocada, ReglaMisiones.Decidir(mision, c));
            c.HayReloj = false;
            Assert.AreEqual(MotivoMision.Disponible, ReglaMisiones.Decidir(mision, c));
        }

        [Test]
        public void Decidir_RepetibleHechaHoy_HechaHoyYAlDiaSiguienteDisponible()
        {
            mision = DatosMision.Crear("a", "A", TipoMision.Barrio, OrigenMision.Calle, FranjaMision.Cualquiera, UnPaso(), repetible: true);
            ContextoMision c = Bueno();
            c.UltimoDiaCumplida = 5;
            Assert.AreEqual(MotivoMision.HechaHoy, ReglaMisiones.Decidir(mision, c));
            c.Dia = 6;
            Assert.AreEqual(MotivoMision.Disponible, ReglaMisiones.Decidir(mision, c));
        }

        [Test]
        public void Penalizacion_SoloALasReputacionesQueSubian()
        {
            mision = DatosMision.Crear("a", "A", TipoMision.Barrio, OrigenMision.Calle, FranjaMision.Cualquiera, UnPaso(),
                deltaBarrio: 15, deltaCorporaciones: -8);
            PenalizacionMision p = ReglaMisiones.Penalizacion(mision);
            Assert.AreEqual(-3, p.Barrio);
            Assert.AreEqual(0, p.Corporaciones, "la que bajaba no se toca");

            Object.DestroyImmediate(mision);
            mision = DatosMision.Crear("b", "B", TipoMision.Robo, OrigenMision.Bar, FranjaMision.Noche, UnPaso(),
                deltaBarrio: 3, deltaCorporaciones: -10);
            Assert.AreEqual(-3, ReglaMisiones.Penalizacion(mision).Barrio);
            Assert.AreEqual(0, ReglaMisiones.Penalizacion(mision).Corporaciones);

            Object.DestroyImmediate(mision);
            mision = DatosMision.Crear("c", "C", TipoMision.Corporativa, OrigenMision.Telefono, FranjaMision.Dia, UnPaso(),
                deltaBarrio: -5, deltaCorporaciones: 8);
            Assert.AreEqual(0, ReglaMisiones.Penalizacion(mision).Barrio);
            Assert.AreEqual(-3, ReglaMisiones.Penalizacion(mision).Corporaciones);
        }

        [Test]
        public void Recompensa_TraeLoQueDiceLaMision()
        {
            var chatarra = DatosObjeto.Crear("chatarra", "Chatarra", TipoObjeto.Chatarra, 50, 20);
            mision = DatosMision.Crear("a", "A", TipoMision.Robo, OrigenMision.Bar, FranjaMision.Noche, UnPaso(),
                pago: 200, deltaBarrio: 5, deltaCorporaciones: -15, chatarraObjeto: chatarra, chatarraCantidad: 10);
            RecompensaMision r = ReglaMisiones.Recompensa(mision);
            Assert.AreEqual(200, r.Pago);
            Assert.AreSame(chatarra, r.Chatarra);
            Assert.AreEqual(10, r.ChatarraCantidad);
            Assert.AreEqual(5, r.DeltaBarrio);
            Assert.AreEqual(-15, r.DeltaCorporaciones);
            Assert.AreEqual(500, mision.ValorChatarra);
            Object.DestroyImmediate(chatarra);
        }

        [TestCase("Mision_Encargo_DonaLupe", TipoPuntoMision.Encargo)]
        [TestCase("Mision_Botin_Camion", TipoPuntoMision.Botin)]
        [TestCase("Mision_Pelea_Baldio", TipoPuntoMision.Pelea)]
        [TestCase("Mision_Escondite_Azotea", TipoPuntoMision.Escondite)]
        public void TipoDePunto_LeeElTipoDelId(string id, TipoPuntoMision esperado)
        {
            Assert.AreEqual(esperado, ReglaMisiones.TipoDePunto(id));
        }

        [TestCase("Encargo_DonaLupe")]
        [TestCase("Mision_Raro_X")]
        [TestCase("")]
        public void TipoDePunto_IdMalo_Null(string id)
        {
            Assert.IsNull(ReglaMisiones.TipoDePunto(id));
        }
    }
}
