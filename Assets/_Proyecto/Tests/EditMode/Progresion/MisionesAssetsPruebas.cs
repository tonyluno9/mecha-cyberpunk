using System.Collections.Generic;
using System.Linq;
using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests.Progresion
{
    /// <summary>Las 11 misiones como assets (T-183, D-tony-148): los valores de la tabla.</summary>
    public class MisionesAssetsPruebas
    {
        public const string Carpeta = "Assets/_Proyecto/Progresion/Datos/Misiones/";

        public static CatalogoMisiones Catalogo()
        {
            var c = AssetDatabase.LoadAssetAtPath<CatalogoMisiones>(Carpeta + "CatalogoMisiones_Base.asset");
            Assert.IsNotNull(c, "falta CatalogoMisiones_Base.asset (corre CreadorMisiones)");
            return c;
        }

        private static DatosMision M(string id)
        {
            DatosMision m = Catalogo().Buscar(id);
            Assert.IsNotNull(m, "falta la misión " + id);
            return m;
        }

        private static void Verifica(string id, TipoMision tipo, OrigenMision origen, FranjaMision franja, int pago, int barrio, int corp,
            int pasos, int chatarra, bool favor, int liga = 1, int nivelBarrio = -2, int nivelCorp = -2, float limite = 0f)
        {
            DatosMision m = M(id);
            Assert.AreEqual(tipo, m.Tipo, id + " tipo");
            Assert.AreEqual(origen, m.Origen, id + " origen");
            Assert.AreEqual(franja, m.Franja, id + " franja");
            Assert.AreEqual(pago, m.Pago, id + " pago");
            Assert.AreEqual(barrio, m.DeltaBarrio, id + " barrio");
            Assert.AreEqual(corp, m.DeltaCorporaciones, id + " corporaciones");
            Assert.AreEqual(pasos, m.Pasos.Length, id + " pasos");
            Assert.AreEqual(chatarra, m.ChatarraCantidad, id + " chatarra");
            Assert.AreEqual(favor, m.Favor != null, id + " favor");
            Assert.AreEqual(liga, m.LigaMinima, id + " liga");
            Assert.AreEqual(nivelBarrio, m.NivelBarrioMinimo, id + " nivel barrio");
            Assert.AreEqual(nivelCorp, m.NivelCorporacionesMinimo, id + " nivel corporaciones");
            Assert.AreEqual(limite, m.LimiteSegundos, 0.001f, id + " límite");
            Assert.IsTrue(m.Repetible, id + " repetible");
        }

        [Test]
        public void Catalogo_Tiene11MisionesEnOrdenSinRepetir()
        {
            string[] esperado =
            {
                "barrio_tiendita", "barrio_callejon", "barrio_luz", "corp_entrega", "corp_cobro", "corp_limpieza",
                "ilegal_carga", "ilegal_pelea", "robo_bodega", "robo_camion", "robo_soplo"
            };
            CollectionAssert.AreEqual(esperado, Catalogo().Misiones.Select(m => m.Id).ToArray());
            Assert.AreEqual(11, Catalogo().Misiones.Select(m => m.Id).Distinct().Count());
            foreach (string id in esperado)
            {
                Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<DatosMision>(Carpeta + "DatosMision_" + id + ".asset"), id);
            }
        }

        [Test]
        public void Barrio_ValoresDeLaTabla()
        {
            Verifica("barrio_tiendita", TipoMision.Barrio, OrigenMision.Calle, FranjaMision.Cualquiera, 50, 8, 0, 2, 0, true);
            Verifica("barrio_callejon", TipoMision.Barrio, OrigenMision.Calle, FranjaMision.Noche, 100, 12, 0, 2, 0, true);
            Verifica("barrio_luz", TipoMision.Barrio, OrigenMision.Calle, FranjaMision.Cualquiera, 0, 15, -8, 2, 0, true, nivelBarrio: 1);
            Assert.AreEqual(TipoFavor.Descuento, M("barrio_tiendita").Favor.Tipo);
            Assert.AreEqual(TipoFavor.Escondite, M("barrio_callejon").Favor.Tipo);
            Assert.AreEqual(TipoFavor.Informacion, M("barrio_luz").Favor.Tipo);
            Assert.AreEqual("Mision_Encargo_DonaLupe", M("barrio_tiendita").IdEncargo);
            Assert.AreEqual("Mision_Encargo_Taquero", M("barrio_callejon").IdEncargo);
            Assert.AreEqual("Mision_Encargo_Vecindad", M("barrio_luz").IdEncargo);
            Assert.AreEqual(30f, M("barrio_luz").Pasos[0].Calor);
        }

        [Test]
        public void Corporativas_ValoresDeLaTabla()
        {
            Verifica("corp_entrega", TipoMision.Corporativa, OrigenMision.Telefono, FranjaMision.Dia, 400, -5, 8, 2, 0, false, limite: 150f);
            Verifica("corp_cobro", TipoMision.Corporativa, OrigenMision.Telefono, FranjaMision.Dia, 500, -12, 10, 2, 0, false);
            Verifica("corp_limpieza", TipoMision.Corporativa, OrigenMision.Telefono, FranjaMision.Dia, 900, -15, 15, 1, 0, false,
                liga: 2, nivelCorp: 1);
            Assert.AreEqual("Cobra al vecino", M("corp_cobro").Pasos[0].Texto);
        }

        [Test]
        public void IlegalesYRobos_ValoresDeLaTabla()
        {
            Verifica("ilegal_carga", TipoMision.Ilegal, OrigenMision.Bar, FranjaMision.Noche, 350, 0, -5, 2, 0, false);
            Verifica("ilegal_pelea", TipoMision.Ilegal, OrigenMision.Bar, FranjaMision.Noche, 450, 3, -5, 2, 0, false);
            Verifica("robo_bodega", TipoMision.Robo, OrigenMision.Bar, FranjaMision.Noche, 0, 3, -10, 2, 6, false);
            Verifica("robo_camion", TipoMision.Robo, OrigenMision.Bar, FranjaMision.Noche, 200, 5, -15, 3, 10, false, liga: 2);
            Verifica("robo_soplo", TipoMision.Robo, OrigenMision.Bar, FranjaMision.Noche, 0, 3, -8, 2, 12, false);
            Assert.IsTrue(M("robo_soplo").ExigeFavor);
            Assert.AreEqual(TipoFavor.Informacion, M("robo_soplo").FavorRequerido);
            Assert.AreEqual(40f, M("ilegal_carga").Pasos[0].Calor);
            Assert.AreEqual(50f, M("robo_bodega").Pasos[0].Calor);
            Assert.AreEqual(60f, M("robo_camion").Pasos[1].Calor);
            Assert.AreEqual(15f, M("robo_soplo").Pasos[0].Calor);
        }

        [Test]
        public void TextosYTitulos_EnMayusculasYConFrase()
        {
            foreach (DatosMision m in Catalogo().Misiones)
            {
                Assert.AreEqual(m.Titulo.ToUpperInvariant(), m.Titulo, m.Id + " titulo en mayúsculas");
                Assert.IsNotEmpty(m.Descripcion, m.Id + " descripcion");
                foreach (PasoMision p in m.Pasos) Assert.IsNotEmpty(p.Texto, m.Id + " texto de paso");
            }
        }

        [Test]
        public void Puntos_ConPrefijoMisionYElTipoQueCadaPasoPide()
        {
            foreach (DatosMision m in Catalogo().Misiones)
            {
                if (m.Origen == OrigenMision.Calle)
                {
                    Assert.IsTrue(m.IdEncargo.StartsWith("Mision_"), m.Id + " idEncargo");
                    Assert.AreEqual(TipoPuntoMision.Encargo, ReglaMisiones.TipoDePunto(m.IdEncargo), m.Id + " idEncargo es Encargo");
                }
                else
                {
                    Assert.IsEmpty(m.IdEncargo, m.Id + ": solo las de calle llevan idEncargo");
                }
                foreach (PasoMision p in m.Pasos)
                {
                    if (p.Tipo == TipoPaso.Escapar)
                    {
                        Assert.IsEmpty(p.IdPunto, m.Id + ": Escapar no lleva punto");
                        continue;
                    }
                    Assert.IsTrue(p.IdPunto.StartsWith("Mision_"), m.Id + " punto " + p.IdPunto);
                    TipoPuntoMision? t = ReglaMisiones.TipoDePunto(p.IdPunto);
                    Assert.IsNotNull(t, m.Id + " tipo de " + p.IdPunto);
                    Assert.IsTrue(ReglaMisiones.PuntoValidoParaPaso(p.Tipo, t.Value),
                        m.Id + ": un paso " + p.Tipo + " no puede apuntar a un punto " + t + " (" + p.IdPunto + ")");
                }
            }
        }

        [Test]
        public void Favores_LosDeBarrioSonTresDistintos()
        {
            var tipos = new HashSet<TipoFavor>(Catalogo().Misiones.Where(m => m.Tipo == TipoMision.Barrio).Select(m => m.Favor.Tipo));
            Assert.AreEqual(3, tipos.Count);
        }
    }
}
