using System;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.Personaje;
using MechaCyberpunk.Progresion.Sonido;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Progresion
{
    /// <summary>
    /// T-voces-personajes-23nov: los IDs de las voces grabadas existen en la lista del catálogo (sección Voces, encendidos, una voz por personaje) y cada
    /// evento del juego pide el ID correcto.
    /// </summary>
    public class Voces23novPruebas
    {
        private static bool EnCatalogo(string id) => Voces23nov.EsVoz(id) || Array.IndexOf(ReglaMezclaSonidos.IdsSkulls, id) >= 0;

        [Test]
        public void Ids_TodosLosGrabados_SonVocesEncendidasEnSeccionVoces()
        {
            Assert.That(Voces23nov.Ids.Length, Is.GreaterThan(60));
            foreach (string id in Voces23nov.Ids)
            {
                Assert.That(Voces23nov.EsVoz(id), Is.True, id);
                Assert.That(ReglaCatalogoSonidos.SeccionDe(id), Is.EqualTo(SeccionSonido.Voces), id);
                Assert.That(ReglaMezclaSonidos.EncendidoPorDefecto(id), Is.True, id);
            }
        }

        [Test]
        public void Politica_VozDePersonaje_UnaVozPorGrupoYArribaDeLosEfectos()
        {
            foreach (string id in new[] { "JUG_DOLOR_LEVE", "SKU_MATON_ATAQUE", "POL_VOZ_ALTO", "NOV_VOZ_CUBRIRSE", "NOV_JEFE_FRASE", "DOC_VOZ_SALUDO", "POL_MEGAFONO_ALTO" })
            {
                PoliticaSonido p = ReglaMezclaSonidos.Para(id);
                Assert.That(p.Grupo, Does.StartWith("voz-"), id);
                Assert.That(p.MaxVoces, Is.EqualTo(1), id);
                Assert.That(p.Volumen, Is.EqualTo(1f), id);
            }
            Assert.That(ReglaMezclaSonidos.Para("POL_VOZ_ALTO").DistanciaMaxima, Is.GreaterThan(0f));
        }

        [Test]
        public void Carpetas_FrasesLargas_SonJefesYDoc()
        {
            Assert.That(Voces23nov.EsCarpetaDeFrasesLargas("Jefe_SKULLS"), Is.True);
            Assert.That(Voces23nov.EsCarpetaDeFrasesLargas("Jefe_Nova"), Is.True);
            Assert.That(Voces23nov.EsCarpetaDeFrasesLargas("Doc"), Is.True);
            Assert.That(Voces23nov.EsCarpetaDeFrasesLargas("Jugador"), Is.False);
            Assert.That(Voces23nov.CarpetaDe("POL_MEGAFONO_RENDIRSE"), Is.EqualTo("Policia/_megafono"));
        }

        [Test]
        public void Jugador_Eventos_PidenSuVozGrabadaEn2D()
        {
            foreach (string id in new[] { VozJugador.Esfuerzo, VozJugador.Salto, VozJugador.DolorLeve, VozJugador.DolorFuerte, VozJugador.Muerte,
                         VozJugador.Quemado, VozJugador.Tos, VozJugador.RespiracionCansado })
            {
                Assert.That(Voces23nov.CarpetaDe(id), Is.EqualTo("Jugador"), id);
                Assert.That(Voces23nov.Es2D(id), Is.True, id);
            }
            Assert.That(VozJugador.IdDolor(5f, 100f, false), Is.EqualTo("JUG_DOLOR_LEVE"));
            Assert.That(VozJugador.IdDolor(30f, 100f, false), Is.EqualTo("JUG_DOLOR_FUERTE"));
            Assert.That(VozJugador.IdDolor(5f, 100f, true), Is.EqualTo("JUG_DOLOR_FUERTE"));
            Assert.That(VozJugador.IdDolor(0f, 100f, false), Is.Empty);
            Assert.That(Voces23nov.SegundosFundido(VozJugador.RespiracionCansado), Is.GreaterThan(0f));
        }

        [Test]
        public void Respiracion_CorrerSeisSegundos_EmpiezaYAlDescansarPara()
        {
            ReglaRespiracionCansado r = new ReglaRespiracionCansado();
            Assert.That(r.Avanzar(5f, true), Is.False);
            Assert.That(r.Avanzar(1.1f, true), Is.True);
            Assert.That(r.Avanzar(2f, false), Is.True, "sigue jadeando un rato al parar");
            Assert.That(r.Avanzar(2f, false), Is.False);
        }

        [Test]
        public void Doc_Eventos_PidenSuVozEn2D()
        {
            foreach (string id in new[] { VocesDoc.Saludo, VocesDoc.Sientate, VocesDoc.Explicar, VocesDoc.Revivir, VocesDoc.Suero, VocesDoc.Despedida })
            {
                Assert.That(Voces23nov.CarpetaDe(id), Is.EqualTo("Doc"), id);
                Assert.That(Voces23nov.Es2D(id), Is.True, id);
            }
            Assert.That(VocesDoc.TocaPuerta(30f, 0f), Is.True);
            Assert.That(VocesDoc.TocaPuerta(5f, 0f), Is.False);
        }

        [Test]
        public void Faccion_CadaEvento_PideElIdCorrecto()
        {
            Assert.That(VocesFaccion.Id(FaccionVoz.Skulls, EventoVoz.Muerte), Is.EqualTo("SKU_MUERTE"));
            Assert.That(VocesFaccion.Id(FaccionVoz.Skulls, EventoVoz.Alerta), Is.EqualTo("SKU_GRITO_ALERTA"));
            Assert.That(VocesFaccion.Id(FaccionVoz.Maton, EventoVoz.Ataque), Is.EqualTo("SKU_MATON_ATAQUE"));
            Assert.That(VocesFaccion.Id(FaccionVoz.Maton, EventoVoz.Provocar), Is.EqualTo("SKU_MATON_PROVOCAR"));
            Assert.That(VocesFaccion.Id(FaccionVoz.Maton, EventoVoz.Burla), Is.EqualTo("SKU_MATON_RISA"));
            Assert.That(VocesFaccion.Id(FaccionVoz.Maton, EventoVoz.Dolor), Is.EqualTo("SKU_MATON_DOLOR"));
            Assert.That(VocesFaccion.Id(FaccionVoz.Maton, EventoVoz.Muerte), Is.EqualTo("SKU_MATON_MUERTE"));
            Assert.That(VocesFaccion.Id(FaccionVoz.JefeSkulls, EventoVoz.Alerta), Is.EqualTo("SKU_JEFE_ENFRENTAMIENTO"));
            Assert.That(VocesFaccion.Id(FaccionVoz.JefeSkulls, EventoVoz.Muerte), Is.EqualTo("SKU_JEFE_DERROTA"));
            Assert.That(VocesFaccion.Id(FaccionVoz.Policia, EventoVoz.InicioCombate), Is.EqualTo("POL_VOZ_ALTO"));
            Assert.That(VocesFaccion.Id(FaccionVoz.Policia, EventoVoz.Burla), Is.EqualTo("POL_VOZ_RENDIRSE"));
            Assert.That(VocesFaccion.Id(FaccionVoz.Policia, EventoVoz.Cobertura), Is.EqualTo("POL_VOZ_REFUERZOS"));
            Assert.That(VocesFaccion.Id(FaccionVoz.Policia, EventoVoz.OficialCaido), Is.EqualTo("POL_VOZ_OFICIAL_CAIDO"));
            Assert.That(VocesFaccion.Id(FaccionVoz.Policia, EventoVoz.Dolor), Is.EqualTo("POL_VOZ_DOLOR"));
            Assert.That(VocesFaccion.Id(FaccionVoz.Policia, EventoVoz.Muerte), Is.EqualTo("POL_VOZ_MUERTE"));
            Assert.That(VocesFaccion.Id(FaccionVoz.Nova, EventoVoz.Alerta), Is.EqualTo("NOV_VOZ_ALERTA"));
            Assert.That(VocesFaccion.Id(FaccionVoz.Nova, EventoVoz.InicioCombate), Is.EqualTo("NOV_VOZ_ORDEN"));
            Assert.That(VocesFaccion.Id(FaccionVoz.Nova, EventoVoz.Escudo), Is.EqualTo("NOV_VOZ_CUBRIRSE"));
            Assert.That(VocesFaccion.Id(FaccionVoz.Nova, EventoVoz.LoPerdimos), Is.EqualTo("NOV_VOZ_LO_PERDIMOS"));
            Assert.That(VocesFaccion.Id(FaccionVoz.Nova, EventoVoz.Dolor), Is.EqualTo("NOV_VOZ_DOLOR"));
            Assert.That(VocesFaccion.Id(FaccionVoz.Nova, EventoVoz.Muerte), Is.EqualTo("NOV_VOZ_MUERTE"));
            Assert.That(VocesFaccion.Id(FaccionVoz.JefeNova, EventoVoz.Ataque), Is.EqualTo("NOV_JEFE_ATAQUE"));
            Assert.That(VocesFaccion.Id(FaccionVoz.JefeNova, EventoVoz.Muerte), Is.EqualTo("NOV_JEFE_MUERTE"));
            Assert.That(VocesFaccion.Id(FaccionVoz.Ninguna, EventoVoz.Muerte), Is.Empty);
            Assert.That(VocesFaccion.Id(FaccionVoz.Nova, EventoVoz.Ambiente), Is.Empty, "Nova no platica");
        }

        [Test]
        public void Faccion_TodosLosIdsQuePide_EstanEnElCatalogo()
        {
            foreach (FaccionVoz f in Enum.GetValues(typeof(FaccionVoz)))
            {
                foreach (EventoVoz e in Enum.GetValues(typeof(EventoVoz)))
                {
                    string id = VocesFaccion.Id(f, e);
                    if (id.Length > 0)
                    {
                        Assert.That(EnCatalogo(id), Is.True, f + "/" + e + " = " + id);
                    }
                }
            }
        }

        [Test]
        public void Policia_ElAltavozYElRadio_UsanIdsGrabados()
        {
            // Los literales de Trafico (PatrullaMundo.IdsMegafono, GestorPersecucion.IdLoPerdimos) y de GuardiaNova: el ensamblado de pruebas no ve Trafico.
            foreach (string id in new[] { "POL_VOZ_ALTAVOZ", "POL_MEGAFONO_ALTO", "POL_MEGAFONO_RENDIRSE", "POL_VOZ_LO_PERDIMOS", "NOV_VOZ_ALERTA" })
            {
                Assert.That(Voces23nov.EsVoz(id), Is.True, id);
            }
        }

        [Test]
        public void EventoDeSkull_IdsComunes_SeTraducenParaLasOtrasFacciones()
        {
            Assert.That(VocesFaccion.EventoDeSkull(VocesPandilla.GritoAlerta), Is.EqualTo(EventoVoz.Alerta));
            Assert.That(VocesFaccion.EventoDeSkull(VocesPandilla.Insulto), Is.EqualTo(EventoVoz.Burla));
            Assert.That(VocesFaccion.EventoDeSkull(VocesPandilla.Dolor), Is.EqualTo(EventoVoz.Dolor));
            Assert.That(VocesFaccion.EventoDeSkull(VocesPandilla.LoPerdimos), Is.EqualTo(EventoVoz.LoPerdimos));
            Assert.That(Array.IndexOf(ReglaMezclaSonidos.IdsSkulls, "SKU_MUERTE"), Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void CanepaYTino_Eventos_PidenSuVozGrabadaEn3D()
        {
            foreach (string id in new[] { VocesApoyo.CanepaSaludo, VocesApoyo.CanepaReparar, VocesApoyo.CanepaDespedida })
            {
                Assert.That(Voces23nov.CarpetaDe(id), Is.EqualTo("Canepa"), id);
                Assert.That(Voces23nov.Es2D(id), Is.False, id);
                Assert.That(ReglaMezclaSonidos.Para(id).Grupo, Is.EqualTo(Voces23nov.GrupoCanepa), id);
            }
            foreach (string id in new[] { VocesApoyo.TinoSaludo, VocesApoyo.TinoBanco, VocesApoyo.TinoCompra, VocesApoyo.TinoSinDinero, VocesApoyo.TinoDespedida })
            {
                Assert.That(Voces23nov.CarpetaDe(id), Is.EqualTo("Tino"), id);
                Assert.That(Voces23nov.Es2D(id), Is.False, id);
                Assert.That(ReglaMezclaSonidos.Para(id).MaxVoces, Is.EqualTo(1), id);
            }
            Assert.That(VocesApoyo.TocaPuerta(30f, 0f), Is.True);
            Assert.That(VocesApoyo.TocaPuerta(5f, 0f), Is.False);
            Assert.That(VocesApoyo.CambioCercania(false, 4f), Is.EqualTo(1), "llega: saluda");
            Assert.That(VocesApoyo.CambioCercania(true, 7f), Is.EqualTo(0), "en el margen no repite");
            Assert.That(VocesApoyo.CambioCercania(true, 10f), Is.EqualTo(-1), "se va: despedida");
        }

        [Test]
        public void MeseraYPeatones_IdsEnElCatalogo_ConSuGrupo()
        {
            // Los literales de Casino (ReglaVozMesera) y de Personaje.Visual (ReglaVozPeaton): este ensamblado de pruebas no los ve.
            foreach (string id in new[] { "MESERA_ROBOT_SALUDO", "MESERA_ROBOT_SERVIR", "MESERA_ROBOT_SUERTE", "MESERA_ROBOT_DESPEDIDA" })
            {
                Assert.That(Voces23nov.CarpetaDe(id), Is.EqualTo("Mesera_Robot"), id);
                Assert.That(ReglaMezclaSonidos.Para(id).Grupo, Is.EqualTo(Voces23nov.GrupoMesera), id);
            }
            foreach (string momento in new[] { "MIEDO", "ENOJO", "POLICIA", "SALUDO", "VENDEDOR", "RISA" })
            {
                for (int v = 1; v <= 3; v++)
                {
                    string id = "NPC_VOZ" + v + "_" + momento;
                    Assert.That(Voces23nov.CarpetaDe(id), Is.EqualTo("Peatones"), id);
                    Assert.That(Voces23nov.Es2D(id), Is.False, id);
                    PoliticaSonido p = ReglaMezclaSonidos.Para(id);
                    Assert.That(p.Grupo, Is.EqualTo(Voces23nov.GrupoPeatones), id);
                    Assert.That(p.MaxVoces, Is.EqualTo(Voces23nov.MaxVocesPeatones), id);
                    Assert.That(p.DistanciaMaxima, Is.GreaterThan(0f), id);
                }
            }
        }
    }
}
