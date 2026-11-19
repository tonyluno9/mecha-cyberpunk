using System.Collections.Generic;
using MechaCyberpunk.Progresion.Sonido;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Sonido
{
    /// <summary>T-1471: los IDs viejos suenan con el archivo nuevo cuando existe; los nuevos y los mapeados quedan encendidos.</summary>
    public class MapaSonidosT1471Pruebas
    {
        private static AudioClip Clip(string nombre) => AudioClip.Create(nombre, 441, 1, 44100, false);

        private static CatalogoSonidos.Entrada Entrada(string id, params AudioClip[] finales) => new CatalogoSonidos.Entrada { id = id, finales = finales };

        private static CatalogoSonidos Catalogo(params CatalogoSonidos.Entrada[] entradas)
        {
            CatalogoSonidos c = ScriptableObject.CreateInstance<CatalogoSonidos>();
            c.Fijar(new List<CatalogoSonidos.Entrada>(entradas));
            return c;
        }

        [Test]
        public void IdViejo_UsaElClipNuevo_CuandoElNuevoTieneClips()
        {
            AudioClip viejo = Clip("viejo");
            AudioClip nuevo = Clip("nuevo");
            CatalogoSonidos c = Catalogo(Entrada("ARM_PIS_DISPARO", viejo), Entrada("ARM_W01_DISPARO", nuevo));
            Assert.AreSame(nuevo, c.Elegir("ARM_PIS_DISPARO"));
            Assert.AreSame(nuevo, c.Elegir("ARM_SUB_DISPARO"), "el subfusil usa el disparo W01 (con tono base alto)");
            Assert.AreSame(nuevo, c.Elegir("ARM_W01_DISPARO"));
        }

        [Test]
        public void IdViejo_UsaSuClipViejo_CuandoElNuevoNoTieneClips()
        {
            AudioClip viejo = Clip("viejo");
            CatalogoSonidos c = Catalogo(Entrada("ARM_PIS_DISPARO", viejo), Entrada("ARM_W01_DISPARO"));
            Assert.AreSame(viejo, c.Elegir("ARM_PIS_DISPARO"));
            CatalogoSonidos sinEntrada = Catalogo(Entrada("ARM_PIS_DISPARO", viejo));
            Assert.AreSame(viejo, sinEntrada.Elegir("ARM_PIS_DISPARO"));
        }

        [Test]
        public void IdSinMapa_SigueConSuClip()
        {
            AudioClip clip = Clip("lgr");
            CatalogoSonidos c = Catalogo(Entrada("ARM_LGR_DISPARO", clip), Entrada("ARM_W01_DISPARO", Clip("otro")));
            Assert.AreSame(clip, c.Elegir("ARM_LGR_DISPARO"));
        }

        [Test]
        public void ElAliasViejoDeLaRecarga_SigueComoRespaldo()
        {
            AudioClip fus = Clip("fus");
            CatalogoSonidos c = Catalogo(Entrada("ARM_FUS_RECARGA", fus));
            Assert.AreSame(fus, c.Elegir("ARM_SUB_RECARGA"));
            AudioClip nuevo = Clip("w03");
            c = Catalogo(Entrada("ARM_FUS_RECARGA", fus), Entrada("ARM_W03_RECARGA_CARGADOR", nuevo));
            Assert.AreSame(nuevo, c.Elegir("ARM_SUB_RECARGA"));
        }

        [Test]
        public void CadaViejoMapeado_ApuntaAUnIdNuevoDeLaLista()
        {
            foreach (MapaSonidos19nov.Par par in MapaSonidos19nov.Pares)
            {
                Assert.IsTrue(MapaSonidos19nov.EsNuevo(par.Nuevo), par.Viejo + " -> " + par.Nuevo + " no esta en la lista de nuevos");
                Assert.AreEqual(par.Nuevo, MapaSonidos19nov.Nuevo(par.Viejo));
            }
            Assert.IsNull(MapaSonidos19nov.Nuevo("ARM_LGR_DISPARO"));
            Assert.AreEqual("HUD_NOTIFICACION", MapaSonidos19nov.Nuevo("UI_OBJETIVO_NUEVO"));
            Assert.AreEqual("UI_ERROR", MapaSonidos19nov.Nuevo("ECO_SIN_DINERO"));
            Assert.AreEqual("POL_SIRENA_LOOP", MapaSonidos19nov.Nuevo("CAL_PATRULLA_SIRENA"));
        }

        [Test]
        public void ElTonoBase_DelSubfusilEsAlto_YElDeLaPistolaNo()
        {
            Assert.AreEqual(1.25f, MapaSonidos19nov.TonoBase("ARM_SUB_DISPARO"), 1e-5f);
            Assert.AreEqual(1.25f, ReglaMezclaSonidos.Para("ARM_SUB_DISPARO").TonoBase, 1e-5f);
            Assert.AreEqual(1f, ReglaMezclaSonidos.Para("ARM_PIS_DISPARO").TonoBase, 1e-5f);
            Assert.AreEqual(1f, ReglaMezclaSonidos.Para("ARM_W01_DISPARO").TonoBase, 1e-5f);
        }

        [Test]
        public void Reemplazar_CambiaLosFinalesViejos_PorLosNuevos_YAgregaLosQueNoEstaban()
        {
            AudioClip viejo = Clip("viejo");
            AudioClip prov = Clip("prov");
            AudioClip nuevo1 = Clip("n1");
            AudioClip nuevo2 = Clip("n2");
            SortedDictionary<string, CatalogoSonidos.Entrada> entradas = new SortedDictionary<string, CatalogoSonidos.Entrada>
            {
                { "UI_MOVER", new CatalogoSonidos.Entrada { id = "UI_MOVER", finales = new[] { viejo }, provisionales = new[] { prov } } },
                { "UI_TEXTO", Entrada("UI_TEXTO", viejo) },
            };
            MapaSonidos19nov.Reemplazar(entradas, new[] { Entrada("UI_MOVER", nuevo1, nuevo2), Entrada("HUD_LOGRO", nuevo1), Entrada("UI_ATRAS") });
            CollectionAssert.AreEqual(new[] { nuevo1, nuevo2 }, entradas["UI_MOVER"].finales, "reemplaza, no mezcla");
            CollectionAssert.AreEqual(new[] { prov }, entradas["UI_MOVER"].provisionales);
            CollectionAssert.AreEqual(new[] { viejo }, entradas["UI_TEXTO"].finales);
            Assert.IsTrue(entradas.ContainsKey("HUD_LOGRO"));
            Assert.IsFalse(entradas.ContainsKey("UI_ATRAS"), "una entrada nueva sin clips no se agrega");
        }

        [Test]
        public void Sincronizar_EnciendeTodosLosNuevos_AunqueHubieraUnoApagado()
        {
            CatalogoSonidos c = Catalogo(Entrada("UI_ERROR", Clip("e")));
            c.FijarEncendido("UI_ERROR", false);
            c.SincronizarInterruptores();
            foreach (string id in MapaSonidos19nov.IdsNuevos)
            {
                Assert.IsTrue(c.Encendido(id), id);
                Assert.IsTrue(ReglaMezclaSonidos.EncendidoPorDefecto(id), id);
            }
        }

        [Test]
        public void Sincronizar_EnciendeElViejoMapeado_SoloSiElNuevoTieneClips()
        {
            CatalogoSonidos c = Catalogo(Entrada("VEH_CLAXON_VIEJO", Clip("v")), Entrada("VEH_CLAXON_NORMAL", Clip("n")), Entrada("MUN_HOLOGRAMA", Clip("h")));
            c.FijarEncendido("VEH_CLAXON_VIEJO", false);
            c.FijarEncendido("VEH_CLAXON_TRAFICO", false);
            c.FijarEncendido("ARM_BALA_ROZA", false);
            c.FijarEncendido("UI_GUARDADO", false);
            c.SincronizarInterruptores();
            Assert.IsTrue(c.Encendido("VEH_CLAXON_VIEJO"), "el claxon del jugador se enciende: ya tiene su claxon nuevo");
            Assert.IsFalse(c.Encendido("VEH_CLAXON_TRAFICO"), "el del trafico sigue apagado (spam)");
            Assert.IsFalse(c.Encendido("ARM_BALA_ROZA"), "T-729: el silbido que rozaba sigue apagado");
            Assert.IsFalse(c.Encendido("MUN_HOLOGRAMA"), "los molestos sin mapa siguen apagados");

            CatalogoSonidos sinNuevo = Catalogo(Entrada("VEH_CLAXON_VIEJO", Clip("v")));
            sinNuevo.FijarEncendido("VEH_CLAXON_VIEJO", false);
            sinNuevo.SincronizarInterruptores();
            Assert.IsFalse(sinNuevo.Encendido("VEH_CLAXON_VIEJO"), "sin archivo nuevo queda como estaba");
        }

        [Test]
        public void Sincronizar_NoPierdeLosInterruptoresDeLosViejosMapeadosSinEntrada()
        {
            CatalogoSonidos c = Catalogo();
            c.SincronizarInterruptores();
            foreach (MapaSonidos19nov.Par par in MapaSonidos19nov.Pares)
            {
                if (par.Encender) Assert.IsTrue(c.Encendido(par.Viejo), par.Viejo);
            }
        }

        [TestCase("HUD_VIDA_BAJA", "Interfaz")]
        [TestCase("TIE_COMPRAR", "Interfaz")]
        [TestCase("ARM_W01_DISPARO", "Efectos")]
        public void LaSeccionDeLosPrefijosNuevos(string id, string seccion)
        {
            Assert.AreEqual(seccion, MechaCyberpunk.Progresion.Sonido.ReglaCatalogoSonidos.SeccionDe(id).ToString());
        }
    }
}
