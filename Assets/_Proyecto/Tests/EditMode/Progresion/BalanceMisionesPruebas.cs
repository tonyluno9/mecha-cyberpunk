using System.Collections.Generic;
using System.Linq;
using System.Text;
using MechaCyberpunk.Core;
using MechaCyberpunk.Pelea.Datos;
using MechaCyberpunk.Progresion;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests.Progresion
{
    /// <summary>Reglas de balance de las misiones (T-183, D-tony-149). Leen los assets reales y publican el margen de cada regla.</summary>
    public class BalanceMisionesPruebas
    {
        private const int CalorNivel1 = 25;
        private const string RutaCalendario = "Assets/_Proyecto/Pelea/Datos/CalendarioLigas_Base.asset";

        private static IEnumerable<DatosMision> Todas => MisionesAssetsPruebas.Catalogo().Misiones;

        /// <summary>Premio de ganar del rival 1 de una liga.</summary>
        private static int Rival1(int liga)
        {
            var calendario = AssetDatabase.LoadAssetAtPath<CalendarioLigas>(RutaCalendario);
            Assert.IsNotNull(calendario, RutaCalendario);
            CartelPelea cartel = calendario.Cartel(liga, 1);
            Assert.IsNotNull(cartel, "rival 1 de la liga " + liga);
            return cartel.premioGanar;
        }

        private static int Total(DatosMision m) => m.Pago + m.ValorChatarra;

        [Test]
        public void ReglaA_MisionDeBarrioPagaMenosQueElRival1DeLaLiga1()
        {
            int tope = Rival1(1);
            foreach (DatosMision m in Todas.Where(x => x.Tipo == TipoMision.Barrio))
            {
                Assert.Less(m.Pago, tope, $"REGLA a: {m.Id} paga ${m.Pago} y el rival 1 de la liga 1 paga ${tope}.");
            }
        }

        [Test]
        public void ReglaB_NingunaPagaMasQueElRival1DeSuLigaMinima()
        {
            foreach (DatosMision m in Todas)
            {
                int tope = Rival1(m.LigaMinima);
                Assert.LessOrEqual(Total(m), tope,
                    $"REGLA b: {m.Id} da ${m.Pago} + chatarra ${m.ValorChatarra} = ${Total(m)} y el rival 1 de la liga {m.LigaMinima} paga ${tope}.");
            }
        }

        [Test]
        public void ReglaC_NingunaSubeLasDosReputaciones()
        {
            foreach (DatosMision m in Todas)
            {
                Assert.IsFalse(m.DeltaBarrio > 0 && m.DeltaCorporaciones > 0,
                    $"REGLA c: {m.Id} sube barrio ({m.DeltaBarrio}) y corporaciones ({m.DeltaCorporaciones}).");
            }
        }

        [Test]
        public void ReglaD_Toda_DeBarrioSubeElBarrioYDaUnFavor()
        {
            foreach (DatosMision m in Todas.Where(x => x.Tipo == TipoMision.Barrio))
            {
                Assert.Greater(m.DeltaBarrio, 0, $"REGLA d: {m.Id} no sube el barrio.");
                Assert.IsNotNull(m.Favor, $"REGLA d: {m.Id} no da favor.");
            }
        }

        [Test]
        public void ReglaE_TodaCorporativaBajaElBarrioAlMenos5YSubeCorporaciones()
        {
            foreach (DatosMision m in Todas.Where(x => x.Tipo == TipoMision.Corporativa))
            {
                Assert.LessOrEqual(m.DeltaBarrio, -5, $"REGLA e: {m.Id} baja el barrio solo {m.DeltaBarrio}.");
                Assert.Greater(m.DeltaCorporaciones, 0, $"REGLA e: {m.Id} no sube corporaciones.");
            }
        }

        [Test]
        public void ReglaF_LosRobosPaganMasEnChatarraQueEnEfectivo()
        {
            foreach (DatosMision m in Todas.Where(x => x.Tipo == TipoMision.Robo))
            {
                Assert.Greater(m.ValorChatarra, m.Pago,
                    $"REGLA f: {m.Id} paga ${m.Pago} en efectivo y ${m.ValorChatarra} en chatarra.");
            }
        }

        [Test]
        public void ReglaG_IlegalesYRobosSuman25DeCalorOLlevanEscapar()
        {
            foreach (DatosMision m in Todas.Where(x => x.Tipo == TipoMision.Ilegal || x.Tipo == TipoMision.Robo))
            {
                bool escapar = m.Pasos.Any(p => p.Tipo == TipoPaso.Escapar);
                Assert.IsTrue(m.CalorTotal >= CalorNivel1 || escapar,
                    $"REGLA g: {m.Id} suma {m.CalorTotal} de calor y no lleva Escapar.");
            }
        }

        [Test]
        public void ReglaH_Corporativa_CuestaBarrioDeFormaVisible_YUnaPeleaPagaMasQueUnaDeBarrio()
        {
            // "Una pelea paga más que una misión de barrio" y "aceptar un contrato corporativo baja el barrio de forma visible":
            // el umbral del nivel 1 es 15, así que dos contratos corporativos seguidos (-5 y -12) cruzan un nivel.
            int maxBarrio = Todas.Where(x => x.Tipo == TipoMision.Barrio).Max(x => x.Pago);
            Assert.Less(maxBarrio, Rival1(1));
            int peorBarrioCorp = Todas.Where(x => x.Tipo == TipoMision.Corporativa).Min(x => x.DeltaBarrio);
            Assert.LessOrEqual(peorBarrioCorp, -15, "el contrato más caro en barrio baja al menos un umbral de nivel (15).");
        }

        [Test]
        public void Bitacora_ImprimeLaTablaYElMargenDeCadaRegla()
        {
            var sb = new StringBuilder("[Progresion] Balance de misiones (T-183)\n");
            sb.AppendLine("id | tipo | pago | chatarra($) | total | barrio | corp | calor | liga | pasos");
            foreach (DatosMision m in Todas)
            {
                sb.AppendLine($"{m.Id} | {m.Tipo} | ${m.Pago} | ${m.ValorChatarra} | ${Total(m)} | {m.DeltaBarrio:+0;-0;0} | " +
                    $"{m.DeltaCorporaciones:+0;-0;0} | {m.CalorTotal} | {m.LigaMinima} | {string.Join(">", m.Pasos.Select(p => p.Tipo))}");
            }
            int r1 = Rival1(1);
            int maxBarrio = Todas.Where(x => x.Tipo == TipoMision.Barrio).Max(x => x.Pago);
            sb.AppendLine($"a) barrio max ${maxBarrio} < rival1 liga1 ${r1}: margen ${r1 - maxBarrio}");
            foreach (DatosMision m in Todas)
            {
                sb.AppendLine($"b) {m.Id}: ${Total(m)} <= rival1 liga{m.LigaMinima} ${Rival1(m.LigaMinima)}: margen ${Rival1(m.LigaMinima) - Total(m)}");
            }
            foreach (DatosMision m in Todas.Where(x => x.Tipo == TipoMision.Corporativa))
            {
                sb.AppendLine($"e) {m.Id}: barrio {m.DeltaBarrio} (<= -5, margen {-5 - m.DeltaBarrio}), corp +{m.DeltaCorporaciones}");
            }
            foreach (DatosMision m in Todas.Where(x => x.Tipo == TipoMision.Robo))
            {
                sb.AppendLine($"f) {m.Id}: chatarra ${m.ValorChatarra} > efectivo ${m.Pago}: margen ${m.ValorChatarra - m.Pago}");
            }
            foreach (DatosMision m in Todas.Where(x => x.Tipo == TipoMision.Ilegal || x.Tipo == TipoMision.Robo))
            {
                sb.AppendLine($"g) {m.Id}: calor {m.CalorTotal} (>= {CalorNivel1}, margen {m.CalorTotal - CalorNivel1}), Escapar: {m.Pasos.Any(p => p.Tipo == TipoPaso.Escapar)}");
            }
            Debug.Log(sb.ToString());
            Assert.Greater(sb.Length, 0);
        }
    }
}
