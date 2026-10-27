using System.Collections.Generic;
using System.Linq;
using System.Text;
using MechaCyberpunk.Combate.Habilidades;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.Datos;
using MechaCyberpunk.Mecha;
using MechaCyberpunk.Pelea.Datos;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests
{
    /// <summary>
    /// Reglas de balance de las habilidades y la municion (T-179, D-tony-140). Leen los assets reales; ningun numero del juego va escrito aqui.
    /// </summary>
    public class BalanceHabilidadesPruebas
    {
        private const string RutaCatalogo = "Assets/_Proyecto/Mecha/Datos/Piezas/CatalogoPiezas_Base.asset";
        private const string RutaJugador = "Assets/_Proyecto/Mecha/Datos/DatosMecha_Jugador.asset";
        private const string RutaCalendario = "Assets/_Proyecto/Pelea/Datos/CalendarioLigas_Base.asset";
        private const string RutaRival = "Assets/_Proyecto/IA/Datos/Ligas/DatosMecha_Liga";
        private const string RutaJefe = "Assets/_Proyecto/IA/Datos/Ligas/DatosHabilidadRival_Jefe";
        private const int Ligas = 4;
        private const int Jefe = 5;
        private const int UsosPorRanura = 2;
        private const float MaxFraccionVidaRival = 0.35f;
        private const float MinVecesGolpeSerie = 1.2f;
        private const float MaxFraccionPremioMunicion = 0.6f;
        private const float MaxFraccionVidaJefe = 0.25f;
        private const float RazonMaximaContraJefeSinMejorar = 1.0f;
        private const float RazonMinimaContraJefeConLiga = 0.7f;
        private static readonly RamaPieza[] Ramas = { RamaPieza.Militar, RamaPieza.Tech, RamaPieza.CuerpoACuerpo, RamaPieza.Fuego };
        private static readonly RanuraPieza[] RanurasConHabilidad = { RanuraPieza.BrazoIzquierdo, RanuraPieza.Torso };

        private static CatalogoPiezas Catalogo => Cargar<CatalogoPiezas>(RutaCatalogo);
        private static CalendarioLigas Calendario => Cargar<CalendarioLigas>(RutaCalendario);
        private static ValoresMecha Serie => Cargar<DatosMecha>(RutaJugador).Valores;

        private static T Cargar<T>(string ruta) where T : Object
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(ruta);
            Assert.IsNotNull(a, ruta);
            return a;
        }

        private static ValoresMecha Rival(int liga, int n) => Cargar<DatosMecha>(RutaRival + liga + "_" + n + ".asset").Valores;
        private static DatosHabilidadRival HabJefe(int liga) => Cargar<DatosHabilidadRival>(RutaJefe + liga + ".asset");

        private static DatosPieza Pieza(RanuraPieza ranura, RamaPieza rama, int nivel) =>
            Catalogo.De(ranura).Single(p => p.Rama == rama && p.Nivel == nivel);

        private static List<DatosPieza> Puestas(RamaPieza rama, int nivel)
        {
            var lista = new List<DatosPieza>();
            for (int r = 0; r < 5; r++)
                lista.Add(nivel == 0 ? Catalogo.DeSerie((RanuraPieza)r) : Pieza((RanuraPieza)r, rama, nivel));
            return lista;
        }

        private static ValoresMecha Mecha(RamaPieza rama, int nivel) => ReglaEstadisticasPiezas.Componer(Serie, Puestas(rama, nivel));

        private static int Golpes(float vida, float danio) => Mathf.CeilToInt(vida / danio);

        /// <summary>Alcance de una habilidad (m): el del proyectil, el del cono o la distancia de la embestida. El escudo no tiene.</summary>
        private static float AlcanceDe(DatosHabilidad h)
        {
            switch (h.Efecto)
            {
                case TipoEfectoHabilidad.Proyectil: return h.Alcance;
                case TipoEfectoHabilidad.Cono: return h.AlcanceCono;
                case TipoEfectoHabilidad.Embestida: return h.Distancia;
                default: return 0f;
            }
        }

        /// <summary>Dano total de un uso a un nivel (los proyectiles de la salva suman).</summary>
        private static float DanioUso(DatosHabilidad h, int nivel) => h.Danio * h.Cantidad * ReglaHabilidad.Potencia(nivel);

        /// <summary>Regla a) para una habilidad: devuelve null si pasa, o el motivo.</summary>
        private static string RevisarReglaA(DatosHabilidad h, int nivel, float vidaRival, float golpeSerie)
        {
            float d = DanioUso(h, nivel);
            if (d > MaxFraccionVidaRival * vidaRival)
                return $"{h.Nombre} nivel {nivel} quita {d:F1} y el maximo es {MaxFraccionVidaRival * vidaRival:F1} ({MaxFraccionVidaRival:P0} de {vidaRival}).";
            if (d < MinVecesGolpeSerie * golpeSerie)
                return $"{h.Nombre} nivel {nivel} quita {d:F1} y el minimo es {MinVecesGolpeSerie * golpeSerie:F1} ({MinVecesGolpeSerie} x golpe de serie).";
            return null;
        }

        private static bool HaceDanio(DatosHabilidad h) => h.Efecto != TipoEfectoHabilidad.Escudo && h.Danio > 0f && h.Aturdir <= 0f; // DESCARGA aturde y no cuenta (D-tony-140)

        private static IEnumerable<(RamaPieza rama, DatosHabilidad hab)> HabilidadesQueHacenDanio()
        {
            foreach (RamaPieza rama in Ramas)
                foreach (RanuraPieza ranura in RanurasConHabilidad)
                {
                    DatosHabilidad h = Pieza(ranura, rama, 1).Habilidad;
                    Assert.IsNotNull(h, $"{rama} {ranura} sin habilidad");
                    if (HaceDanio(h)) yield return (rama, h);
                }
        }

        [Test]
        public void ReglaA_UnUsoNoTumbaAlRivalYPesaMasQueUnGolpe()
        {
            var sb = new StringBuilder("REGLA a (habilidad | liga | dano del uso | max 35% vida rival 1 | min 1.2 x golpe serie)\n");
            float golpeSerie = Serie.danioGolpe;
            foreach (var (rama, h) in HabilidadesQueHacenDanio())
                for (int l = 1; l <= Ligas; l++)
                {
                    float vida = Rival(l, 1).vidaMaxima;
                    sb.AppendLine($"| {h.Nombre} | {l} | {DanioUso(h, l):F1} | {MaxFraccionVidaRival * vida:F1} | {MinVecesGolpeSerie * golpeSerie:F1} |");
                    Assert.IsNull(RevisarReglaA(h, l, vida, golpeSerie), $"REGLA a, rama {rama}, liga {l}");
                }
            Debug.Log(sb.ToString());
        }

        [Test]
        public void ReglaA_FallaSiElMisilHace100_AssetCopiadoEnLaPrueba()
        {
            var misil = Object.Instantiate(Pieza(RanuraPieza.BrazoIzquierdo, RamaPieza.Militar, 1).Habilidad);
            try
            {
                Assert.IsNull(RevisarReglaA(misil, 1, Rival(1, 1).vidaMaxima, Serie.danioGolpe), "con los valores reales pasa");
                var so = new SerializedObject(misil);
                so.FindProperty("danio").floatValue = 100f;
                so.ApplyModifiedPropertiesWithoutUndo();
                Assert.IsNotNull(RevisarReglaA(misil, 1, Rival(1, 1).vidaMaxima, Serie.danioGolpe));
            }
            finally { Object.DestroyImmediate(misil); }
        }

        [Test]
        public void ReglaB_LaMunicionDeUnaPeleaTipicaCuestaMenosDeLaMitadDelPremio()
        {
            int premio = Calendario.Cartel(1, 1).premioGanar;
            int total = 0;
            var sb = new StringBuilder("REGLA b (ranura | rama mas cara | precio por uso | usos | costo)\n");
            foreach (RanuraPieza ranura in RanurasConHabilidad)
            {
                int precio = 0;
                RamaPieza cara = Ramas[0];
                foreach (RamaPieza rama in Ramas)
                {
                    int p = Pieza(ranura, rama, 1).Municion.Oferta.Precio;
                    if (p > precio) { precio = p; cara = rama; }
                }
                total += precio * UsosPorRanura;
                sb.AppendLine($"| {ranura} | {cara} | {precio} | {UsosPorRanura} | {precio * UsosPorRanura} |");
            }
            sb.AppendLine($"| total | | | | {total} de premio {premio} (max {MaxFraccionPremioMunicion * premio:F0}) |");
            Debug.Log(sb.ToString());
            Assert.LessOrEqual(total, MaxFraccionPremioMunicion * premio, "REGLA b: " + total + " contra premio " + premio);
        }

        [Test]
        public void ReglaC_NingunaHabilidadDominaAOtraDeSuRanura()
        {
            var sb = new StringBuilder("REGLA c (ranura | habilidad | dano | enfriamiento | precio municion | alcance)\n");
            foreach (RanuraPieza ranura in RanurasConHabilidad)
            {
                var filas = Ramas.Select(r => Pieza(ranura, r, 1)).Select(p => (h: p.Habilidad, precio: p.Municion.Oferta.Precio)).ToList();
                foreach (var f in filas)
                    sb.AppendLine($"| {ranura} | {f.h.Nombre} | {DanioUso(f.h, 1):F1} | {f.h.Enfriamiento:F1} | {f.precio} | {AlcanceDe(f.h):F1} |");
                foreach (var a in filas)
                    foreach (var b in filas)
                    {
                        if (a.h == b.h) continue;
                        float da = DanioUso(a.h, 1), db = DanioUso(b.h, 1);
                        bool igualOMejor = da >= db && a.h.Enfriamiento <= b.h.Enfriamiento && a.precio <= b.precio && AlcanceDe(a.h) >= AlcanceDe(b.h);
                        bool mejorEnUna = da > db || a.h.Enfriamiento < b.h.Enfriamiento || a.precio < b.precio || AlcanceDe(a.h) > AlcanceDe(b.h);
                        Assert.IsFalse(igualOMejor && mejorEnUna, $"REGLA c: {a.h.Nombre} domina a {b.h.Nombre} en {ranura}.");
                    }
            }
            Debug.Log(sb.ToString());
        }

        [Test]
        public void ReglaD_ConPiezasTechLaHabilidadTechEnfriaMasRapidoQueLasOtras()
        {
            var sb = new StringBuilder("REGLA d (nivel | ranura | tech con piezas | la mas rapida de las otras sin piezas)\n");
            for (int n = 1; n <= Ligas; n++)
            {
                float reduccion = ReglaEstadisticasPiezas.ReduccionEnfriamiento(Puestas(RamaPieza.Tech, n));
                foreach (RanuraPieza ranura in RanurasConHabilidad)
                {
                    DatosHabilidad tech = Pieza(ranura, RamaPieza.Tech, n).Habilidad;
                    float conPiezas = ReglaHabilidad.Enfriamiento(tech.Enfriamiento, reduccion);
                    float otras = Ramas.Where(r => r != RamaPieza.Tech).Min(r => Pieza(ranura, r, n).Habilidad.Enfriamiento);
                    sb.AppendLine($"| {n} | {ranura} {tech.Nombre} | {conPiezas:F2} (reduccion {reduccion:P0}) | {otras:F2} |");
                    Assert.Less(conPiezas, otras, $"REGLA d: nivel {n}, {tech.Nombre} con piezas tech ({conPiezas:F2}) debe enfriar mas rapido que {otras:F2}.");
                }
            }
            Debug.Log(sb.ToString());
        }

        [Test]
        public void ReglaE_LaHabilidadDelJefeNoQuitaMasDeUnCuartoDeLaVidaDelJugador()
        {
            var sb = new StringBuilder("REGLA e (liga | jefe | habilidad | dano | max 25% de la vida del jugador fuego L-1)\n");
            for (int l = 1; l <= Ligas; l++)
            {
                DatosHabilidadRival hr = HabJefe(l);
                float dano = DanioUso(hr.Habilidad, hr.Nivel);
                float vida = Mecha(RamaPieza.Fuego, l - 1).vidaMaxima;
                sb.AppendLine($"| {l} | {Rival(l, Jefe).nombre} | {hr.Habilidad.Nombre} {hr.Nivel} | {dano:F1} | {MaxFraccionVidaJefe * vida:F1} (vida {vida}) |");
                Assert.LessOrEqual(dano, MaxFraccionVidaJefe * vida, $"REGLA e: liga {l}, {hr.Habilidad.Nombre} quita {dano:F1} de {vida}.");
            }
            Debug.Log(sb.ToString());
        }

        /// <summary>Dano por golpe que aporta la habilidad del brazo de la rama y el nivel del jugador (0 con el mecha de serie).</summary>
        private static float AporteJugador(RamaPieza rama, int nivel, float intervalo)
        {
            if (nivel == 0) return 0f;
            DatosHabilidad h = Pieza(RanuraPieza.BrazoIzquierdo, rama, nivel).Habilidad;
            float enf = ReglaHabilidad.Enfriamiento(h.Enfriamiento, ReglaEstadisticasPiezas.ReduccionEnfriamiento(Puestas(rama, nivel)));
            return DanioUso(h, nivel) * intervalo / Mathf.Max(0.01f, enf);
        }

        private static float AporteJefe(int liga, float intervalo)
        {
            DatosHabilidadRival hr = HabJefe(liga);
            return DanioUso(hr.Habilidad, hr.Nivel) * intervalo / (hr.Habilidad.Enfriamiento * hr.FactorEnfriamiento);
        }

        private static float Razon(RamaPieza rama, int nivelJugador, int liga)
        {
            ValoresMecha yo = Mecha(rama, nivelJugador), jefe = Rival(liga, Jefe);
            float danioYo = yo.danioGolpe + AporteJugador(rama, nivelJugador, yo.recargaGolpe);
            float danioJefe = jefe.danioGolpe + AporteJefe(liga, jefe.recargaGolpe);
            return (float)Golpes(yo.vidaMaxima, danioJefe) / Golpes(jefe.vidaMaxima, danioYo);
        }

        [Test]
        public void ReglaF_RazonesContraElJefeConHabilidades()
        {
            var sb = new StringBuilder("REGLA f (rama | liga | razon nivel L-1 (max <1.0) | razon nivel L (min 0.7))\n");
            foreach (RamaPieza rama in Ramas)
                for (int l = 1; l <= Ligas; l++)
                {
                    float sin = Razon(rama, l - 1, l), con = Razon(rama, l, l);
                    sb.AppendLine($"| {rama} | {l} | {sin:F2} | {con:F2} |");
                    Assert.Less(sin, RazonMaximaContraJefeSinMejorar, $"REGLA f: rama {rama}, liga {l}, nivel L-1: razon {sin:F2}.");
                    Assert.GreaterOrEqual(con, RazonMinimaContraJefeConLiga, $"REGLA f: rama {rama}, liga {l}, nivel L: razon {con:F2}.");
                }
            Debug.Log(sb.ToString());
        }

        [Test]
        public void Imprime_TablaFinalDeHabilidadesMunicionYJefes()
        {
            var sb = new StringBuilder("TABLA FINAL\nHABILIDADES (rama | ranura | nombre | efecto | dano | enfriamiento | alcance | municion | precio)\n");
            foreach (RamaPieza rama in Ramas)
                foreach (RanuraPieza ranura in RanurasConHabilidad)
                {
                    DatosPieza p = Pieza(ranura, rama, 1);
                    sb.AppendLine($"| {rama} | {ranura} | {p.Habilidad.Nombre} | {p.Habilidad.Efecto} | {DanioUso(p.Habilidad, 1):F1} | {p.Habilidad.Enfriamiento:F1} | {AlcanceDe(p.Habilidad):F1} | {p.Municion.Nombre} max {p.Municion.Maximo} | {p.Municion.Oferta.Precio} |");
                }
            sb.AppendLine("JEFES (liga | jefe | habilidad | nivel | dano | enfriamiento x factor)");
            for (int l = 1; l <= Ligas; l++)
            {
                DatosHabilidadRival hr = HabJefe(l);
                sb.AppendLine($"| {l} | {Rival(l, Jefe).nombre} | {hr.Habilidad.Nombre} | {hr.Nivel} | {DanioUso(hr.Habilidad, hr.Nivel):F1} | {hr.Habilidad.Enfriamiento * hr.FactorEnfriamiento:F1} (x{hr.FactorEnfriamiento}) |");
            }
            Debug.Log(sb.ToString());
        }
    }
}
