using System.Collections.Generic;
using System.Linq;
using System.Text;
using MechaCyberpunk.Core;
using MechaCyberpunk.Mecha;
using MechaCyberpunk.Pelea.Datos;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests
{
    /// <summary>
    /// Reglas de balance entre las piezas y las ligas (T-172, D-tony-125). Leen los assets reales; ningun numero va escrito aqui.
    /// Razon = golpes que el rival necesita para tumbarte / golpes que tu necesitas para tumbarlo (sin bloqueo, ritmo ni furia).
    /// </summary>
    public class BalancePiezasLigasPruebas
    {
        private const string RutaCatalogo = "Assets/_Proyecto/Mecha/Datos/Piezas/CatalogoPiezas_Base.asset";
        private const string RutaJugador = "Assets/_Proyecto/Mecha/Datos/DatosMecha_Jugador.asset";
        private const string RutaCalendario = "Assets/_Proyecto/Pelea/Datos/CalendarioLigas_Base.asset";
        private const string RutaRival = "Assets/_Proyecto/IA/Datos/Ligas/DatosMecha_Liga";
        private const int Ligas = 4;
        private const int Jefe = 5;
        private const float RazonMinimaRival1 = 1.0f;
        private const float RazonMaximaJefeSinMejorar = 1.0f;
        private const float RazonMinimaJefeConLiga = 0.8f;
        private const float FraccionPremios = 0.8f;
        private static readonly RamaPieza[] Ramas = { RamaPieza.Militar, RamaPieza.Tech, RamaPieza.CuerpoACuerpo, RamaPieza.Fuego };

        private static CatalogoPiezas Catalogo => Cargar<CatalogoPiezas>(RutaCatalogo);
        private static CalendarioLigas Calendario => Cargar<CalendarioLigas>(RutaCalendario);

        private static T Cargar<T>(string ruta) where T : Object
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(ruta);
            Assert.IsNotNull(a, ruta);
            return a;
        }

        private static ValoresMecha Rival(int liga, int n) => Cargar<DatosMecha>(RutaRival + liga + "_" + n + ".asset").Valores;

        private static DatosPieza Pieza(RanuraPieza ranura, RamaPieza rama, int nivel) =>
            Catalogo.De(ranura).Single(p => p.Rama == rama && p.Nivel == nivel);

        /// <summary>Mecha con las 5 piezas de una rama y nivel (nivel 0 = de serie).</summary>
        private static ValoresMecha Mecha(RamaPieza rama, int nivel)
        {
            var puestas = new List<DatosPieza>();
            for (int r = 0; r < 5; r++)
            {
                var ranura = (RanuraPieza)r;
                puestas.Add(nivel == 0 ? Catalogo.DeSerie(ranura) : Pieza(ranura, rama, nivel));
            }
            return ReglaEstadisticasPiezas.Componer(Cargar<DatosMecha>(RutaJugador).Valores, puestas);
        }

        private static int Golpes(float vida, float danio) => Mathf.CeilToInt(vida / danio);

        private static float Razon(ValoresMecha yo, ValoresMecha rival) =>
            (float)Golpes(yo.vidaMaxima, rival.danioGolpe) / Golpes(rival.vidaMaxima, yo.danioGolpe);

        private static string Datos(ValoresMecha yo, ValoresMecha rival) =>
            $"(tu vida {yo.vidaMaxima} dano {yo.danioGolpe}; rival {rival.nombre} vida {rival.vidaMaxima} dano {rival.danioGolpe}; razon {Razon(yo, rival):F2})";

        [Test]
        public void ReglaA_NivelAnteriorPuedeEmpezarLaLiga_RazonContraRival1AlMenos1()
        {
            foreach (RamaPieza rama in Ramas)
                for (int l = 1; l <= Ligas; l++)
                {
                    ValoresMecha yo = Mecha(rama, l - 1), r = Rival(l, 1);
                    Assert.GreaterOrEqual(Razon(yo, r), RazonMinimaRival1,
                        $"REGLA a: rama {rama}, liga {l}, nivel {l - 1} contra el rival 1 {Datos(yo, r)}: debe poder empezar la liga sin mejorar (>= {RazonMinimaRival1}).");
                }
        }

        [Test]
        public void ReglaB_NivelAnteriorNoLeGanaAlJefe_RazonMenorQue1()
        {
            foreach (RamaPieza rama in Ramas)
                for (int l = 1; l <= Ligas; l++)
                {
                    ValoresMecha yo = Mecha(rama, l - 1), r = Rival(l, Jefe);
                    Assert.Less(Razon(yo, r), RazonMaximaJefeSinMejorar,
                        $"REGLA b: rama {rama}, liga {l}, nivel {l - 1} contra el jefe {Datos(yo, r)}: para el jefe hay que mejorar (< {RazonMaximaJefeSinMejorar}).");
                }
        }

        [Test]
        public void ReglaC_NivelDeLaLigaPuedeGanarleAlJefe_RazonAlMenos08()
        {
            foreach (RamaPieza rama in Ramas)
                for (int l = 1; l <= Ligas; l++)
                {
                    ValoresMecha yo = Mecha(rama, l), r = Rival(l, Jefe);
                    Assert.GreaterOrEqual(Razon(yo, r), RazonMinimaJefeConLiga,
                        $"REGLA c: rama {rama}, liga {l}, nivel {l} contra el jefe {Datos(yo, r)}: con las piezas de la liga el jefe se puede ganar (>= {RazonMinimaJefeConLiga}).");
                }
        }

        [Test]
        public void ReglaD_UnaPiezaNivel1CuestaMenosQueElPremioDelRival1DeLaLiga1()
        {
            int premio = Calendario.Cartel(1, 1).premioGanar;
            foreach (RamaPieza rama in Ramas)
                for (int r = 0; r < 5; r++)
                {
                    DatosPieza p = Pieza((RanuraPieza)r, rama, 1);
                    Assert.LessOrEqual(p.Precio, premio,
                        $"REGLA d: rama {rama}, {p.Id} cuesta ${p.Precio} y el rival 1 de la liga 1 paga ${premio}.");
                }
        }

        [Test]
        public void ReglaE_LasCincoPiezasDelNivelSeJuntanPeleandoEnLaLigaAnterior()
        {
            CalendarioLigas cal = Calendario;
            for (int l = 2; l <= Ligas; l++)
            {
                int suma = 0;
                for (int n = 1; n <= cal.RivalesEn(l - 1); n++) suma += cal.Cartel(l - 1, n).premioGanar;
                foreach (RamaPieza rama in Ramas)
                {
                    int costo = 0;
                    for (int r = 0; r < 5; r++) costo += Pieza((RanuraPieza)r, rama, l).Precio;
                    Assert.LessOrEqual(costo, FraccionPremios * suma,
                        $"REGLA e: rama {rama}, nivel {l}: las 5 piezas cuestan ${costo} y {FraccionPremios} x premios de la liga {l - 1} = ${FraccionPremios * suma}.");
                }
            }
        }

        [Test]
        public void ReglaF_NingunaRamaEsPeorQueOtraEnTodo()
        {
            for (int n = 1; n <= Ligas; n++)
                foreach (RamaPieza a in Ramas)
                    foreach (RamaPieza b in Ramas)
                    {
                        if (a == b) continue;
                        ValoresMecha x = Mecha(a, n), y = Mecha(b, n);
                        bool peorEnTodo = x.vidaMaxima < y.vidaMaxima && x.danioGolpe < y.danioGolpe
                            && x.velocidadMaxima < y.velocidadMaxima && x.recargaGolpe >= y.recargaGolpe;
                        Assert.IsFalse(peorEnTodo,
                            $"REGLA f: nivel {n}, rama {a} es peor que {b} en todo (vida {x.vidaMaxima}/{y.vidaMaxima}, dano {x.danioGolpe}/{y.danioGolpe}, vel {x.velocidadMaxima}/{y.velocidadMaxima}, recarga {x.recargaGolpe}/{y.recargaGolpe}).");
                    }
        }

        [Test]
        public void Imprime_TablaDeRazones()
        {
            var sb = new StringBuilder("TABLA DE RAZONES (rama | liga | a: nivel L-1 vs rival1 | b: nivel L-1 vs jefe | c: nivel L vs jefe)\n");
            foreach (RamaPieza rama in Ramas)
                for (int l = 1; l <= Ligas; l++)
                    sb.AppendLine($"| {rama} | {l} | {Razon(Mecha(rama, l - 1), Rival(l, 1)):F2} | {Razon(Mecha(rama, l - 1), Rival(l, Jefe)):F2} | {Razon(Mecha(rama, l), Rival(l, Jefe)):F2} |");
            Debug.Log(sb.ToString());
        }
    }
}
