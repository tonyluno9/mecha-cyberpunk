using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MechaCyberpunk.Core.Mundo;
using MechaCyberpunk.Core.Trepar;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;
using Terreno = MechaCyberpunk.Arte.Editor.CerroT1355Terreno;
using Trazado = MechaCyberpunk.Arte.Editor.CerroT1355Trazado;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1355: la Colonia del Cerro (x -295..-480, z -170..215), zona nueva al oeste de Suburbios (docs/plan-zonas-18nov.md, referencias K13 y A13).
    /// Ladera con terrazas, calle base (x = -305), conexión U4, zigzag que pasa por la salida del túnel U6 (-360, 10, 175), plaza de arriba,
    /// escaleras largas de concreto entre las vueltas, filas de casas escalonadas con azoteas a desniveles de 0.5-2.3 m (parkour),
    /// postes con cables, carros viejos y charcos. Idempotente: rehace solo la raíz <see cref="Raiz"/>.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarT1355Cerro.Aplicar (y .Fotos). No corre CreadorTodo ni CreadorPrologoCiudad.
    /// </summary>
    public static class ColocarT1355Cerro
    {
        public const string Raiz = "T1355_Cerro";
        public const int Semilla = 13550;
        public const string CarpetaDatos = "Assets/_Proyecto/Mundo/Datos/Zonas";
        public const string RutaMallas = CarpetaDatos + "/T1355_Cerro_Mallas.asset";
        public const string RutaMaterialTerreno = "Assets/_Proyecto/Mundo/Materiales/Cerro_Terreno.mat";
        public const string RutaTexturaT05 = "Assets/_Proyecto/Mundo/Texturas/T05_ConcretoCerro.png";
        public const string FuenteT05 = "imageassets/calle/T05_ConcretoCerro.png";
        public const string RutaCarro = "Assets/Arte/Vehiculos/Prefabs/V01a_CarroViejito.prefab"; // el CarroViejo de Vehiculos es greybox (T-506)
        public const string CarpetaModulos = "Assets/Arte/ZonasModulos/Prefabs/";
        public const string CarpetaTripo = "Assets/Arte/Zonas";

        public const float IntensidadPoste = 140f;
        public const float AlcancePoste = 16f;
        public const float AnchoU4 = 14f;
        public const float EscalonTerrazaMax = 2.8f;
        public const float Fondo = 8f;
        public static readonly string[] CodigosCasaTripo = { "CC01_CasaCerro", "CC02_CasaCerro" };
        public const float ProbabilidadTripo = 0.35f;
        public static readonly Color[] Tintes =
        {
            new Color(0.38f, 0.58f, 0.46f), new Color(0.82f, 0.48f, 0.28f), new Color(0.8f, 0.5f, 0.56f),
            new Color(0.35f, 0.62f, 0.62f), new Color(0.72f, 0.62f, 0.4f),
        };
        public const float FachadaDesdeEje = Trazado.AnchoCalle * 0.5f + Trazado.AnchoBanqueta + 0.5f;
        public const float AltoBanqueta = 0.2f;
        public const float BajaPisoCalle = 0.12f;
        public const float AnchoEscalera = 3.0f;
        public const float LargoModuloEscalera = 4f;
        public const float AltoModuloEscalera = 2.62f;
        public const float AnchoModuloEscalera = 2.3f;
        public const float HuellaEscalera = 0.32f;
        public const float MitadHuecoEscalera = 5f;

        /// <summary>z de las escaleras que bajan de la pierna 2 (x = -360) a la calle base.</summary>
        public static readonly float[] ZEscaleras = { 120f, 60f, 0f };

        // Tomas de revisión: K13 (en la calle, mirando cuesta arriba con las escaleras al centro) y A13 (aérea).
        public static readonly Vector3 K13Desde = new Vector3(-358.5f, 12.45f, 160f);
        public static readonly Vector3 K13Hacia = new Vector3(-360f, 17f, 60f);
        public static readonly Vector3 A13Desde = new Vector3(-300f, 48f, -100f);
        public static readonly Vector3 A13Hacia = new Vector3(-385f, 16f, 45f);

        private sealed class Fila
        {
            public string Nombre;
            public float XFachada;
            public float Normal;
            public float[][] Tramos;
            public Func<float, float> YCalle;
            public float EMin;
            public float EMax;
            public bool Escalable;
            public float Fondo = ColocarT1355Cerro.Fondo;
            public bool Relleno;
        }

        private sealed class CasaPlan
        {
            public Fila Fila;
            public float Z;
            public float Ancho;
            public float YCalle;
            public float Alto;
            public float E;
            public KitS2c.Estilo Estilo;
            public GameObject Tripo;
            public float Escala = 1f;
            public int Cadena;
            public float Base => YCalle + E;
        }

        private static readonly Dictionary<string, int> Huecos = new Dictionary<string, int>();

        [MenuItem("Mecha Cyberpunk/T1355/Cerro: colocar")]
        public static void Aplicar()
        {
            ColocarS2.Informe.Clear();
            Huecos.Clear();
            ColocarS2.Abrir();
            KitS2c.Obra o = KitS2c.Empezar(Raiz, Semilla);
            Transform terrenoPadre = Hijo(o.Raiz, "Terreno");
            Transform callesPadre = Hijo(o.Raiz, "Calles");
            Transform casasPadre = Hijo(o.Raiz, "Casas");
            Transform escalerasPadre = Hijo(o.Raiz, "Escaleras");
            Transform postesPadre = Hijo(o.Raiz, "Postes");
            Transform carrosPadre = Hijo(o.Raiz, "Carros");
            Transform detallesPadre = Hijo(o.Raiz, "Detalles");
            Transform huecosPadre = Hijo(o.Raiz, "Huecos");
            List<Mesh> mallas = new List<Mesh>();

            // ---- Calles
            List<Vector3> calleBase = Trazado.CalleBase();
            List<Vector3> u4 = Trazado.ConexionU4();
            List<Vector3> zigzag = Trazado.Zigzag();
            float yPlaza = Trazado.AlturaPlaza();

            // ---- Fuentes de altura del terreno
            List<Terreno.Fuente> fuentes = new List<Terreno.Fuente>();
            float semiCalle = Trazado.AnchoCalle * 0.5f + Trazado.AnchoBanqueta + 1f;
            AgregarFuentes(fuentes, calleBase, semiCalle, BajaPisoCalle);
            float semiU4 = AnchoU4 * 0.5f + Trazado.AnchoBanqueta + 1f;
            AgregarFuentes(fuentes, zigzag, semiCalle, BajaPisoCalle);
            List<Vector3> disco = new List<Vector3>();
            for (float r = 0f; r <= Trazado.RadioPlaza - 2f; r += 6f)
            {
                int n = Mathf.Max(1, Mathf.RoundToInt(2f * Mathf.PI * r / 6f));
                for (int k = 0; k < n; k++)
                {
                    float a = k * 2f * Mathf.PI / n;
                    disco.Add(new Vector3(Trazado.CentroPlaza.x + r * Mathf.Cos(a), yPlaza, Trazado.CentroPlaza.z + r * Mathf.Sin(a)));
                }
            }
            fuentes.Add(new Terreno.Fuente { Puntos = disco.ToArray(), Semiancho = 6f, Baja = BajaPisoCalle });
            // La unión U3 viene del Malecón (otra tarea): el terreno solo se queda plano debajo.
            List<Vector3> u3 = new List<Vector3>();
            foreach ((Vector3 a, Vector3 b) in new[] { (new Vector3(-300f, 0f, -205f), new Vector3(-330f, 0f, -150f)), (new Vector3(-330f, 0f, -150f), new Vector3(-305f, 0f, -120f)) })
            {
                for (int k = 0; k <= 12; k++)
                {
                    u3.Add(Vector3.Lerp(a, b, k / 12f));
                }
            }
            fuentes.Add(new Terreno.Fuente { Puntos = u3.ToArray(), Semiancho = 9f, Baja = BajaPisoCalle + 0.1f });
            fuentes.Add(new Terreno.Fuente { Puntos = u4.ToArray(), Semiancho = semiU4, Baja = BajaPisoCalle });

            // ---- Escaleras (datos)
            List<(float z, float yArriba, float yAbajo, float xArriba, float xAbajo, int pasos)> escaleras = new List<(float, float, float, float, float, int)>();
            foreach (float z in ZEscaleras)
            {
                float xArriba = Trazado.XPierna2 + FachadaDesdeEje - 0.5f;
                float xAbajo = Trazado.XBase - FachadaDesdeEje+ 0.5f; // el barandal CE03 (4 m) no debe salir a la calle base (U4)
                int pasos = Mathf.Max(1, Mathf.FloorToInt((xAbajo - xArriba) / LargoModuloEscalera) - 1);
                xAbajo = xArriba + pasos * LargoModuloEscalera;
                (float z, float yArriba, float yAbajo, float xArriba, float xAbajo, int pasos) e = (z, Trazado.AlturaPierna2(z) + AltoBanqueta, AltoBanqueta, xArriba, xAbajo, pasos);
                escaleras.Add(e);
                List<Vector3> pts = new List<Vector3>();
                for (float x = e.xArriba; x <= e.xAbajo; x += 2f)
                {
                    pts.Add(new Vector3(x, Mathf.Lerp(e.yArriba, e.yAbajo, (x - e.xArriba) / (e.xAbajo - e.xArriba)), z));
                }
                fuentes.Add(new Terreno.Fuente { Puntos = pts.ToArray(), Semiancho = AnchoEscalera * 0.5f + 0.5f, Baja = 0.2f });
            }

            // ---- Casas (plan puro) y plataformas
            List<Fila> filas = Filas(fuentes);
            List<CasaPlan> casas = new List<CasaPlan>();
            foreach (Fila f in filas)
            {
                casas.AddRange(PlanearFila(o, f));
            }
            casas.AddRange(Relleno(o, fuentes, calleBase, u4, zigzag, casas));
            List<Terreno.Plataforma> plataformas = casas.Select(c => new Terreno.Plataforma
            {
                X0 = Mathf.Min(c.Fila.XFachada, c.Fila.XFachada - c.Fila.Normal * c.Fila.Fondo) - 0.3f,
                X1 = Mathf.Max(c.Fila.XFachada, c.Fila.XFachada - c.Fila.Normal * c.Fila.Fondo) + 0.3f,
                Z0 = c.Z - c.Ancho * 0.5f,
                Z1 = c.Z + c.Ancho * 0.5f,
                Y = c.Base - 0.05f,
            }).ToList();

            // ---- Terreno
            Mesh malla = Terreno.Malla(fuentes, plataformas);
            mallas.Add(malla);
            GameObject terreno = new GameObject("Ladera");
            terreno.transform.SetParent(terrenoPadre, false);
            terreno.AddComponent<MeshFilter>().sharedMesh = malla;
            terreno.AddComponent<MeshRenderer>().sharedMaterial = MaterialTerreno();
            terreno.AddComponent<MeshCollider>().sharedMesh = malla;

            // ---- Mallas de calle, banquetas y rayas
            Material asfalto = KitS2c.MatAsfalto();
            Material banqueta = KitS2c.MatBanqueta();
            Calle(o, callesPadre, "Calle_Base", calleBase, Trazado.AnchoCalle, asfalto, banqueta, mallas);
            Calle(o, callesPadre, "Calle_U4", u4, AnchoU4, asfalto, banqueta, mallas);
            Calle(o, callesPadre, "Calle_Zigzag", zigzag, Trazado.AnchoCalle, asfalto, banqueta, mallas);

            // ---- Escaleras de concreto con su barandal
            Material concreto = MaterialTerreno();
            int numero = 0;
            foreach (var e in escaleras)
            {
                numero++;
                Mesh me = Terreno.Escalera("T1355_Escalera_" + numero, e.xArriba, e.yArriba - 0.5f, e.xAbajo, e.yAbajo - 0.5f, e.pasos * 10, e.z - AnchoEscalera * 0.5f, e.z + AnchoEscalera * 0.5f);
                mallas.Add(me);
                GameObject go = new GameObject("CE01_EscaleraCerro_" + numero);
                go.transform.SetParent(escalerasPadre, false);
                go.AddComponent<MeshFilter>().sharedMesh = me;
                go.AddComponent<MeshRenderer>().sharedMaterial = concreto;
                go.AddComponent<MeshCollider>().sharedMesh = me;
                CadenaEscalera(escalerasPadre, huecosPadre, e.z, e.xArriba, e.yArriba, e.xAbajo, e.yAbajo, e.pasos);
            }

            // ---- Casas
            List<Vector3> techos = new List<Vector3>();
            int cadenaPrevia = -1;
            int indiceEnCadena = 0;
            int pasoDetalle = 0;
            foreach (CasaPlan c in casas)
            {
                Vector3 centro = new Vector3(c.Fila.XFachada, c.Base, c.Z);
                Vector3 normal = new Vector3(c.Fila.Normal, 0f, 0f);
                float azotea;
                if (c.Tripo != null)
                {
                    azotea = ColocarTripo(casasPadre, c);
                }
                else
                {
                    azotea = KitS2c.Casa(o, casasPadre, centro, normal, c.Ancho, c.Fila.Fondo, c.Estilo);
                    Tintar(casasPadre.GetChild(casasPadre.childCount - 1).gameObject, Tintes[o.Azar.Next(Tintes.Length)]);
                }
                Vector3 cuerpo = new Vector3(c.Fila.XFachada - c.Fila.Normal * c.Fila.Fondo * 0.5f, c.Base - 6f - 0.02f, c.Z);
                KitS2c.Losa(o, casasPadre, "Cimiento", cuerpo, Quaternion.identity, new Vector3(c.Fila.Fondo, 12f, c.Ancho), concreto, 3f, false);
                techos.Add(new Vector3(c.Fila.XFachada, c.Base + azotea, c.Z));
                int cadenaId = c.Fila.Nombre.GetHashCode() ^ c.Cadena;
                indiceEnCadena = cadenaId == cadenaPrevia ? indiceEnCadena + 1 : 0;
                cadenaPrevia = cadenaId;
                if (c.Fila.Escalable && indiceEnCadena % 10 == 0)
                {
                    AccesoTecho(o, detallesPadre, c, azotea);
                }
                pasoDetalle++;
                if (pasoDetalle % 4 == 0)
                {
                    DetalleAzotea(detallesPadre, huecosPadre, new Vector3(c.Fila.XFachada - c.Fila.Normal * (Fondo * 0.6f), c.Base + azotea, c.Z), pasoDetalle % 8 == 0 ? "CC04_Tendedero" : "CC05_Lavadero", c.Fila.Normal);
                }
            }

            // ---- Postes, cables, carros, charcos
            PostesPierna(o, postesPadre, techos);
            Carros(carrosPadre, calleBase, zigzag);
            Charcos(o, detallesPadre, calleBase, zigzag);
            BarandalesPlaza(detallesPadre, huecosPadre, yPlaza);

            // ---- CalleZona
            CrearCalleZona(callesPadre, "CalleZona_Base", calleBase, Trazado.AnchoCalle);
            CrearCalleZona(callesPadre, "CalleZona_U4", u4, AnchoU4);
            CrearCalleZona(callesPadre, "CalleZona_Zigzag", zigzag, Trazado.AnchoCalle);

            AjustarPostes(postesPadre);
            int luces = o.Raiz.GetComponentsInChildren<Light>(true).Length;
            MarcarEstatico(o.Raiz, carrosPadre);
            KitS2c.Terminar(o);
            GuardarMallas(mallas);
            ColocarS2.Informe.AppendLine("Cerro: " + casas.Count + " casas, " + escaleras.Count + " escaleras, " + o.Postes + " postes, " + o.Cables + " cables, " + o.Charcos
                + " charcos, " + luces + " luces, " + Huecos.Count + " tipos de hueco (" + string.Join(", ", Huecos.Select(h => h.Key + " x" + h.Value)) + ")");
            ColocarS2.Guardar("t1355-cerro");
        }

        // ================================================================ Filas de casas

        private static List<Fila> Filas(List<Terreno.Fuente> fuentes)
        {
            float zNorte = 165f, zSur = -22f;
            float[][] sinEscaleras = { new[] { zNorte, zSur } };
            // Tramos del lado de abajo: se parten donde cruzan las escaleras.
            float[][] conHuecos = TramosConHuecos(zNorte, zSur);
            float[][] conHuecosBase = TramosConHuecos(140f, -105f);
            Terreno.Plataforma[] sinPlataformas = new Terreno.Plataforma[0];
            Func<float, Func<float, float>> terrenoEn = x => z => Terreno.Altura(x, z, fuentes, sinPlataformas);
            float xOeste2 = Trazado.XPierna2 - FachadaDesdeEje, xEste2 = Trazado.XPierna2 + FachadaDesdeEje;
            float xOeste3 = Trazado.XPierna3 - FachadaDesdeEje, xEste3 = Trazado.XPierna3 + FachadaDesdeEje;
            return new List<Fila>
            {
                new Fila { Nombre = "A_Pierna2_Oeste", XFachada = xOeste2, Normal = 1f, Tramos = sinEscaleras, YCalle = Trazado.AlturaPierna2, EMin = -0.3f, EMax = 2.8f, Escalable = true },
                new Fila { Nombre = "B_Pierna2_Este", XFachada = xEste2, Normal = -1f, Tramos = conHuecos, YCalle = Trazado.AlturaPierna2, EMin = -5f, EMax = -1.8f, Escalable = false },
                new Fila { Nombre = "C_Pierna3_Oeste", XFachada = xOeste3, Normal = 1f, Tramos = new[] { new[] { 8f, -22f } }, YCalle = Trazado.AlturaPierna3, EMin = -0.3f, EMax = 2.8f, Escalable = true },
                new Fila { Nombre = "E_Pierna3_Este", XFachada = xEste3, Normal = -1f, Tramos = new[] { new[] { 8f, -22f } }, YCalle = Trazado.AlturaPierna3, EMin = -5f, EMax = -1.8f, Escalable = false },
                new Fila { Nombre = "D_CalleBase_Oeste", XFachada = Trazado.XBase - FachadaDesdeEje, Normal = 1f, Tramos = conHuecosBase, YCalle = z => 0f, EMin = -0.3f, EMax = 2.8f, Escalable = true },
            };
        }

        private static Rect RectoDe(float xFachada, float normal, float fondo, float z, float ancho)
        {
            return new Rect(Mathf.Min(xFachada, xFachada - normal * fondo), z - ancho * 0.5f, fondo, ancho);
        }

        /// <summary>
        /// Rellena la ladera: parejas de columnas de casas pegadas de espaldas (callejones de 3 m entre parejas) en todo lo que queda libre
        /// entre las calles, la plaza, las escaleras y las filas principales. Cada casa va en su terraza (el cimiento baja al terreno).
        /// </summary>
        private static List<CasaPlan> Relleno(KitS2c.Obra o, List<Terreno.Fuente> fuentes, List<Vector3> baseCalle, List<Vector3> u4, List<Vector3> zigzag, List<CasaPlan> existentes)
        {
            const float periodo = 19f;
            const float xMin = -472f, xMax = -312f, zMin = -104f, zMax = 212f;
            Terreno.Plataforma[] sinPlataformas = new Terreno.Plataforma[0];
            List<Rect> ocupados = existentes.Select(c => RectoDe(c.Fila.XFachada, c.Fila.Normal, c.Fila.Fondo, c.Z, c.Ancho)).ToList();
            List<Vector3> calles = baseCalle.Concat(zigzag).ToList();
            float radioCalle2 = Mathf.Pow(Trazado.AnchoCalle * 0.5f + Trazado.AnchoBanqueta + 2.5f, 2f);
            float radioU42 = Mathf.Pow(AnchoU4 * 0.5f + Trazado.AnchoBanqueta + 2.5f, 2f);
            float radioPlaza = Trazado.RadioPlaza + 9f;
            Func<Rect, bool> libre = r =>
            {
                if (r.xMin < xMin || r.xMax > xMax || r.yMin < zMin || r.yMax > zMax)
                {
                    return false;
                }
                foreach (Rect q in ocupados)
                {
                    if (r.Overlaps(q))
                    {
                        return false;
                    }
                }
                foreach (float zf in ZEscaleras)
                {
                    if (r.Overlaps(new Rect(-358f, zf - 4f, 52f, 8f)))
                    {
                        return false;
                    }
                }
                Vector2 cp = new Vector2(Mathf.Clamp(Trazado.CentroPlaza.x, r.xMin, r.xMax), Mathf.Clamp(Trazado.CentroPlaza.z, r.yMin, r.yMax));
                if ((cp - new Vector2(Trazado.CentroPlaza.x, Trazado.CentroPlaza.z)).magnitude < radioPlaza)
                {
                    return false;
                }
                foreach (Vector3 s in calles)
                {
                    float dx = s.x - Mathf.Clamp(s.x, r.xMin, r.xMax), dz = s.z - Mathf.Clamp(s.z, r.yMin, r.yMax);
                    if (dx * dx + dz * dz < radioCalle2)
                    {
                        return false;
                    }
                }
                foreach (Vector3 s in u4)
                {
                    float dx = s.x - Mathf.Clamp(s.x, r.xMin, r.xMax), dz = s.z - Mathf.Clamp(s.z, r.yMin, r.yMax);
                    if (dx * dx + dz * dz < radioU42)
                    {
                        return false;
                    }
                }
                return true;
            };

            List<CasaPlan> nuevas = new List<CasaPlan>();
            int numero = 0;
            for (float xw = -466f; xw + 16f <= xMax; xw += periodo)
            {
                foreach ((float xf, float normal) in new[] { (xw, -1f), (xw + 16f, 1f) })
                {
                    float xc = xf - normal * Fondo * 0.5f;
                    List<float[]> tramos = new List<float[]>();
                    float inicio = float.NaN, ultimo = float.NaN;
                    for (float z = zMax - 0.5f; z >= zMin + 0.5f; z -= 1f)
                    {
                        bool ok = libre(RectoDe(xf, normal, Fondo, z, 1f));
                        if (ok && float.IsNaN(inicio))
                        {
                            inicio = z + 0.5f;
                        }
                        if (ok)
                        {
                            ultimo = z - 0.5f;
                        }
                        if ((!ok || z - 1f < zMin + 0.5f) && !float.IsNaN(inicio))
                        {
                            if (inicio - ultimo >= 4.6f)
                            {
                                tramos.Add(new[] { inicio - 0.2f, ultimo + 0.2f });
                            }
                            inicio = float.NaN;
                        }
                    }
                    if (tramos.Count == 0)
                    {
                        continue;
                    }
                    float xRef = xc;
                    Fila fila = new Fila
                    {
                        Nombre = "R" + (++numero) + "_Relleno",
                        XFachada = xf,
                        Normal = normal,
                        Tramos = tramos.ToArray(),
                        YCalle = z => Terreno.Altura(xRef, z, fuentes, sinPlataformas),
                        EMin = -0.3f,
                        EMax = EscalonTerrazaMax,
                        Relleno = true,
                    };
                    nuevas.AddRange(PlanearFila(o, fila));
                }
            }
            return nuevas;
        }

        private static float[][] TramosConHuecos(float zNorte, float zSur)
        {
            List<float[]> tramos = new List<float[]>();
            float cursor = zNorte;
            foreach (float z in ZEscaleras.OrderByDescending(v => v))
            {
                if (z + MitadHuecoEscalera < cursor && z - MitadHuecoEscalera > zSur)
                {
                    tramos.Add(new[] { cursor, z + MitadHuecoEscalera });
                    cursor = z - MitadHuecoEscalera;
                }
            }
            tramos.Add(new[] { cursor, zSur });
            return tramos.ToArray();
        }

        /// <summary>Casas pegadas de cada tramo con su plan de alturas: cada tramo es una cadena de azoteas para el parkour.</summary>
        private static List<CasaPlan> PlanearFila(KitS2c.Obra o, Fila f)
        {
            List<CasaPlan> todas = new List<CasaPlan>();
            for (int cadena = 0; cadena < f.Tramos.Length; cadena++)
            {
                float cursor = f.Tramos[cadena][0], fin = f.Tramos[cadena][1];
                List<CasaPlan> tramo = new List<CasaPlan>();
                while (cursor - fin >= 3.5f)
                {
                    float ancho = o.Rango(4.6f, 6f);
                    if (cursor - ancho < fin + 3.5f)
                    {
                        ancho = cursor - fin;
                    }
                    KitS2c.Estilo estilo = KitS2c.EstiloAlAzar(o);
                    estilo.Pisos = o.Chance(0.4f) ? 3 : 2;
                    estilo.Toldo = o.Chance(0.4f);
                    if (f.Relleno)
                    {
                        estilo.Foco = o.Chance(0.15f);
                        if (!o.Chance(0.25f))
                        {
                            estilo.Neon = null;
                        }
                    }
                    float alto = KitS2c.Medir(estilo.PlantaBaja).Caja.size.y * KitS2c.EscalaAlto + (estilo.Pisos - 1) * KitS2c.Medir(estilo.PisoAlto).Caja.size.y * KitS2c.EscalaAlto;
                    GameObject tripo = null;
                    float escala = 1f;
                    if (o.Chance(ProbabilidadTripo))
                    {
                        GameObject candidato = BuscarTripo(CodigosCasaTripo[o.Azar.Next(CodigosCasaTripo.Length)]);
                        Vector3 tam = candidato == null ? Vector3.zero : TamanoTripo(candidato);
                        if (tam.x > 0.1f && tam.z > 0.1f)
                        {
                            tripo = candidato;
                            escala = Mathf.Clamp(ancho / tam.x, 0.7f, 1.5f);
                            if (tam.z * escala > f.Fondo)
                            {
                                escala = f.Fondo / tam.z;
                            }
                            alto = tam.y * escala;
                        }
                    }
                    float z = cursor - ancho * 0.5f;
                    tramo.Add(new CasaPlan { Fila = f, Z = z, Ancho = ancho, YCalle = f.YCalle(z), Alto = alto, Estilo = estilo, Tripo = tripo, Escala = escala, Cadena = cadena });
                    cursor -= ancho;
                }
                if (tramo.Count == 0)
                {
                    continue;
                }
                float[] e = Trazado.PlanAzoteas(tramo.Select(c => c.YCalle).ToList(), tramo.Select(c => c.Alto).ToList(), Semilla + cadena * 7 + f.Nombre.Length, f.EMin, f.EMax);
                for (int i = 0; i < tramo.Count; i++)
                {
                    tramo[i].E = e[i];
                }
                todas.AddRange(tramo);
            }
            return todas;
        }

        // ================================================================ Calles

        private static void AgregarFuentes(List<Terreno.Fuente> fuentes, List<Vector3> pts, float semi, float baja)
        {
            const int trozo = 12;
            for (int i = 0; i < pts.Count; i += trozo)
            {
                int n = Mathf.Min(trozo + 1, pts.Count - i);
                fuentes.Add(new Terreno.Fuente { Puntos = pts.GetRange(i, n).ToArray(), Semiancho = semi, Baja = baja });
            }
        }

        private static void Calle(KitS2c.Obra o, Transform padre, string nombre, List<Vector3> pts, float ancho, Material asfalto, Material banqueta, List<Mesh> mallas)
        {
            Transform raiz = Hijo(padre, nombre);
            float mitad = ancho * 0.5f;
            Terreno.Datos a = new Terreno.Datos();
            Terreno.Cinta(a, pts, -mitad, mitad, 0f, 8f);
            Terreno.Datos b = new Terreno.Datos();
            Terreno.Cinta(b, pts, mitad, mitad + Trazado.AnchoBanqueta, AltoBanqueta, 2f);
            Terreno.Cinta(b, pts, -mitad - Trazado.AnchoBanqueta, -mitad, AltoBanqueta, 2f);
            Terreno.Cordon(b, pts, mitad, AltoBanqueta, false, 2f);
            Terreno.Cordon(b, pts, -mitad, AltoBanqueta, true, 2f);
            foreach ((Terreno.Datos datos, string sub, Material mat) in new[] { (a, "Arroyo", asfalto), (b, "Banquetas", banqueta) })
            {
                Mesh m = datos.Crear("T1355_" + nombre + "_" + sub);
                mallas.Add(m);
                GameObject go = new GameObject(sub);
                go.transform.SetParent(raiz, false);
                go.AddComponent<MeshFilter>().sharedMesh = m;
                go.AddComponent<MeshRenderer>().sharedMaterial = mat;
                go.AddComponent<MeshCollider>().sharedMesh = m;
            }
            // Rayas del centro (2 m cada 4 m).
            for (int i = 0; i + 1 < pts.Count; i += 2)
            {
                Vector3 p0 = pts[i], p1 = pts[i + 1];
                KitS2c.Linea(raiz, p0, p1, 0.12f, (p0.y + p1.y) * 0.5f);
            }
        }

        private static void CrearCalleZona(Transform padre, string nombre, List<Vector3> pts, float ancho)
        {
            GameObject go = new GameObject(nombre);
            go.transform.SetParent(padre, false);
            CalleZona cz = go.AddComponent<CalleZona>();
            cz.Definir(pts, ancho, true, false, "Cerro");
        }

        // ================================================================ Escaleras, barandales y accesos

        /// <summary>
        /// Escalera de piezas CE01_EscaleraCerro encadenadas (4 m cada una, subida de 2.62 m ajustada con la escala en y), subiendo al oeste,
        /// con CE03 a los dos lados. El ancho es de <see cref="AnchoEscalera"/> m. Si falta el prefab queda el hueco.
        /// </summary>
        private static void CadenaEscalera(Transform padre, Transform huecos, float z, float xArriba, float yArriba, float xAbajo, float yAbajo, int modulos)
        {
            float subidaPorModulo = (yArriba - yAbajo) / modulos;
            float ys = subidaPorModulo / AltoModuloEscalera;
            for (int i = 0; i < modulos; i++)
            {
                float t = (i + 0.5f) / modulos;
                float x = xArriba + (i + 0.5f) * LargoModuloEscalera;
                float y = Mathf.Lerp(yArriba, yAbajo, t);
                Vector3 pos = new Vector3(x, y - ys, z);
                Modulo("CE01_EscaleraCerro", padre, huecos, pos, Quaternion.LookRotation(Vector3.left), new Vector3(AnchoEscalera / AnchoModuloEscalera, ys, 1f));
                foreach (float lado in new[] { 1f, -1f })
                {
                    Vector3 posRiel = new Vector3(x, y - ys, z + lado * (AnchoEscalera * 0.5f + 0.05f));
                    Modulo("CE03_BarandalTuboEscalera", padre, huecos, posRiel, Quaternion.Euler(0f, 180f, 0f), new Vector3(1f, ys, lado > 0f ? -1f : 1f));
                }
            }
        }

        private static void BarandalesPlaza(Transform padre, Transform huecos, float yPlaza)
        {
            for (int k = 0; k < 6; k++)
            {
                float a = (k + 0.5f) * Mathf.PI / 3f + Mathf.PI * 0.5f;
                Vector3 pos = new Vector3(Trazado.CentroPlaza.x + (Trazado.RadioPlaza + 4f) * Mathf.Cos(a), yPlaza + AltoBanqueta, Trazado.CentroPlaza.z + (Trazado.RadioPlaza + 4f) * Mathf.Sin(a));
                Quaternion giro = Quaternion.LookRotation(new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)));
                Modulo("CE02_BarandalTubo", padre, huecos, pos, giro);
            }
        }

        /// <summary>Instancia la pieza modular (prefab de Blender) o deja el hueco vacío "Hueco_CODIGO".</summary>
        private static void Modulo(string codigo, Transform padre, Transform huecos, Vector3 pos, Quaternion giro, Vector3? escala = null)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CorrerZonasT1350.RutaModulo(codigo));
            if (prefab != null)
            {
                GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
                go.transform.SetPositionAndRotation(pos, giro);
                go.transform.localScale = escala ?? Vector3.one;
                return;
            }
            Hueco(huecos, codigo, pos, giro);
        }

        private static void Hueco(Transform huecos, string codigo, Vector3 pos, Quaternion giro)
        {
            GameObject go = new GameObject("Hueco_" + codigo);
            go.transform.SetParent(huecos, false);
            go.transform.SetPositionAndRotation(pos, giro);
            Huecos[codigo] = Huecos.TryGetValue(codigo, out int n) ? n + 1 : 1;
        }

        /// <summary>Tendedero o lavadero de Tripo (si está en Assets/Arte/Zonas) o su hueco, sobre la azotea.</summary>
        private static void DetalleAzotea(Transform padre, Transform huecos, Vector3 pos, string codigo, float normal)
        {
            GameObject prefab = BuscarTripo(codigo);
            Quaternion giro = Quaternion.LookRotation(new Vector3(normal, 0f, 0f));
            if (prefab != null)
            {
                GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
                go.transform.SetPositionAndRotation(pos, giro);
                return;
            }
            Hueco(huecos, codigo, pos, giro);
        }

        private static GameObject BuscarTripo(string codigo)
        {
            if (!AssetDatabase.IsValidFolder(CarpetaTripo))
            {
                return null;
            }
            string guid = AssetDatabase.FindAssets(codigo + " t:Prefab", new[] { CarpetaTripo }).FirstOrDefault();
            return guid == null ? null : AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
        }

        /// <summary>Escalera de servicio (PuntoTrepar) de la calle a la azotea, con rieles y peldaños de concreto.</summary>
        private static void AccesoTecho(KitS2c.Obra o, Transform padre, CasaPlan c, float azotea)
        {
            float pie = c.YCalle + AltoBanqueta;
            float alto = c.Base + azotea - pie;
            Vector3 normal = new Vector3(c.Fila.Normal, 0f, 0f);
            GameObject go = new GameObject("Acceso_" + c.Fila.Nombre + "_" + Mathf.RoundToInt(c.Z));
            go.transform.SetParent(padre, false);
            go.transform.SetPositionAndRotation(new Vector3(c.Fila.XFachada + c.Fila.Normal * 0.05f, pie, c.Z + c.Ancho * 0.5f - 0.4f), Quaternion.LookRotation(-normal));
            PuntoTrepar p = go.AddComponent<PuntoTrepar>();
            SerializedObject so = new SerializedObject(p);
            so.FindProperty("tipo").enumValueIndex = (int)TipoTrepar.Escalera;
            so.FindProperty("alturaSubida").floatValue = alto;
            so.FindProperty("salida").vector3Value = new Vector3(0f, alto, 0.8f);
            so.ApplyModifiedPropertiesWithoutUndo();
            p.AjustarTrigger();
            Material m = MaterialTerreno();
            foreach (float lado in new[] { -0.25f, 0.25f })
            {
                KitS2c.Losa(o, go.transform, "Riel", new Vector3(c.Fila.XFachada + c.Fila.Normal * 0.15f, pie + alto * 0.5f, go.transform.position.z + lado), Quaternion.identity, new Vector3(0.06f, alto, 0.06f), m, 1f, false);
            }
            for (float y = 0.4f; y < alto; y += 0.5f)
            {
                KitS2c.Losa(o, go.transform, "Peldano", new Vector3(c.Fila.XFachada + c.Fila.Normal * 0.15f, pie + y, go.transform.position.z), Quaternion.identity, new Vector3(0.05f, 0.05f, 0.5f), m, 1f, false);
            }
        }

        // ================================================================ Postes, cables, carros, charcos

        private static void PostesPierna(KitS2c.Obra o, Transform padre, List<Vector3> techos)
        {
            float lado = Trazado.AnchoCalle * 0.5f + 0.7f;
            List<Vector3> postesOeste = new List<Vector3>(), postesEste = new List<Vector3>();
            // Pierna 2: un poste cada 26 m, alternando lados, y cables que cruzan la calle.
            int k = 0;
            for (float z = 165f; z > -20f; z -= 16f)
            {
                float y = Trazado.AlturaPierna2(z) + AltoBanqueta;
                bool oeste = k % 2 == 0;
                Vector3 pie = new Vector3(Trazado.XPierna2 + (oeste ? -lado : lado), y, z);
                Vector3 punta = KitS2c.Poste(o, padre, pie, oeste ? Vector3.right : Vector3.left, y - AltoBanqueta, k % 3 == 0);
                (oeste ? postesOeste : postesEste).Add(punta);
                k++;
            }
            // Calle base (lado oeste) cada 28 m.
            List<Vector3> postesBase = new List<Vector3>();
            for (float z = -98f; z < 140f; z += 18f)
            {
                Vector3 pie = new Vector3(Trazado.XBase - lado, AltoBanqueta, z);
                postesBase.Add(KitS2c.Poste(o, padre, pie, Vector3.right, 0f, true));
            }
            // Pierna 3 y plaza.
            List<Vector3> postesTres = new List<Vector3>();
            foreach (float z in new[] { -18f, 4f })
            {
                float y = Trazado.AlturaPierna3(z) + AltoBanqueta;
                postesTres.Add(KitS2c.Poste(o, padre, new Vector3(Trazado.XPierna3 + lado, y, z), Vector3.left, y - AltoBanqueta, false));
            }
            float yp = Trazado.AlturaPlaza() + AltoBanqueta;
            for (int i = 0; i < 3; i++)
            {
                float a = Mathf.PI + i * 2f * Mathf.PI / 3f;
                Vector3 pie = new Vector3(Trazado.CentroPlaza.x + (Trazado.RadioPlaza - lado) * Mathf.Cos(a), yp, Trazado.CentroPlaza.z + (Trazado.RadioPlaza - lado) * Mathf.Sin(a));
                postesTres.Add(KitS2c.Poste(o, padre, pie, new Vector3(-Mathf.Cos(a), 0f, -Mathf.Sin(a)), yp - AltoBanqueta, false));
            }
            // Postes a los lados de cada escalera, uno cada 12 m, para que alumbren los escalones.
            List<Vector3> postesEscalera = new List<Vector3>();
            foreach (float zf in ZEscaleras)
            {
                float xa = Trazado.XPierna2 + FachadaDesdeEje - 0.5f, xb = Trazado.XBase - FachadaDesdeEje + 0.5f;
                float lado2 = 1f;
                for (float x = xa + 8f; x < xb - 4f; x += 12f)
                {
                    float y = Mathf.Lerp(Trazado.AlturaPierna2(zf) + AltoBanqueta, AltoBanqueta, (x - xa) / (xb - xa));
                    postesEscalera.Add(KitS2c.Poste(o, padre, new Vector3(x, y, zf + lado2 * (AnchoEscalera * 0.5f + 0.9f)), new Vector3(0f, 0f, -lado2), y, false));
                    lado2 = -lado2;
                }
            }
            foreach (List<Vector3> serie in new[] { postesOeste, postesEste, postesBase, postesTres })
            {
                for (int i = 0; i + 1 < serie.Count; i++)
                {
                    for (int c = 0; c < 3; c++)
                    {
                        KitS2c.Cable(o, serie[i] + Vector3.down * (c * 0.22f), serie[i + 1] + Vector3.down * (c * 0.22f), o.Rango(0.6f, 1f));
                    }
                }
            }
            // Cruces de la calle entre un poste del oeste y el más cercano del este, y de cada poste a las azoteas de al lado.
            foreach (Vector3 a in postesOeste)
            {
                Vector3 b = postesEste.OrderBy(q => Mathf.Abs(q.z - a.z)).First();
                for (int c = 0; c < 3; c++)
                {
                    KitS2c.Cable(o, a + Vector3.down * (c * 0.25f), b + Vector3.down * (c * 0.25f), o.Rango(0.8f, 1.3f));
                }
            }
            foreach (Vector3 p in postesOeste.Concat(postesEste).Concat(postesBase).Concat(postesTres).Concat(postesEscalera))
            {
                foreach (Vector3 t in techos.OrderBy(t => (t - p).sqrMagnitude).Take(3))
                {
                    KitS2c.Cable(o, p + Vector3.down * 0.3f, t, o.Rango(0.4f, 0.9f), 0.02f);
                }
            }
        }

        private static void Carros(Transform padre, List<Vector3> calleBase, List<Vector3> zigzag)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaCarro);
            if (prefab == null)
            {
                Debug.LogError("[Arte] T1355: falta " + RutaCarro);
                return;
            }
            (List<Vector3> pts, float z, float lado)[] sitios =
            {
                (calleBase, -70f, -1f), (calleBase, -20f, 1f), (calleBase, 45f, -1f), (calleBase, 118f, 1f),
                (zigzag, 140f, 1f), (zigzag, 95f, -1f), (zigzag, 30f, 1f), (zigzag, -15f, -1f),
            };
            int n = 0;
            foreach ((List<Vector3> pts, float z, float lado) s in sitios)
            {
                // Muestra de la pierna (x de la pierna 2 para el zigzag) más cercana a la z pedida.
                int mejor = 0;
                float menor = float.MaxValue;
                for (int i = 1; i + 1 < s.pts.Count; i++)
                {
                    bool recta = Mathf.Abs(s.pts[i + 1].x - s.pts[i - 1].x) < 0.01f;
                    bool enPierna = s.pts == zigzag ? Mathf.Abs(s.pts[i].x - Trazado.XPierna2) < 0.01f : true;
                    float d = Mathf.Abs(s.pts[i].z - s.z);
                    if (recta && enPierna && d < menor)
                    {
                        menor = d;
                        mejor = i;
                    }
                }
                Vector3 t = (s.pts[mejor + 1] - s.pts[mejor - 1]).normalized;
                Vector3 izq = new Vector3(-t.z, 0f, t.x);
                Vector3 avance = s.lado > 0f ? t : -t;
                GameObject car = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
                car.name = "CarroViejo_Cerro_" + (++n);
                car.transform.SetPositionAndRotation(s.pts[mejor] + izq * (s.lado * (Trazado.AnchoCalle * 0.5f - 1.6f)) + Vector3.up * 0.05f, Quaternion.LookRotation(avance));
            }
        }

        private static void Charcos(KitS2c.Obra o, Transform padre, List<Vector3> calleBase, List<Vector3> zigzag)
        {
            for (int i = 0; i < 8; i++)
            {
                Vector3 p = calleBase[o.Azar.Next(calleBase.Count)];
                KitS2c.Charco(o, padre, p + new Vector3(o.Rango(-2.5f, 2.5f), 0f, 0f), o.Rango(1.2f, 3.2f), o.Rango(0.8f, 1.8f), o.Rango(0f, 180f));
            }
            for (int i = 0; i < 10; i++)
            {
                int k = o.Azar.Next(zigzag.Count);
                Vector3 p = zigzag[k];
                if (zigzag.Count - k < 90)
                {
                    continue;
                }
                Vector3 t = (zigzag[Mathf.Min(k + 1, zigzag.Count - 1)] - zigzag[Mathf.Max(k - 1, 0)]).normalized;
                Vector3 izq = new Vector3(-t.z, 0f, t.x);
                KitS2c.Charco(o, padre, p + izq * o.Rango(-2.5f, 2.5f), o.Rango(1.2f, 3.2f), o.Rango(0.8f, 1.8f), Mathf.Atan2(t.x, t.z) * Mathf.Rad2Deg);
            }
        }

        // ================================================================ Luces, estáticos, assets

        /// <summary>Lámparas de sodio de los postes: spot cálido de <see cref="IntensidadPoste"/> y alcance <see cref="AlcancePoste"/>.</summary>
        private static void AjustarPostes(Transform postes)
        {
            foreach (Light l in postes.GetComponentsInChildren<Light>(true))
            {
                if (l.transform.parent != null && l.transform.parent.name.StartsWith("Poste_"))
                {
                    l.intensity = IntensidadPoste;
                    l.range = AlcancePoste;
                }
            }
        }

        /// <summary>Pinta con el tinte el cuerpo y los pisos de color de la casa (bloque de propiedades, sin tocar los materiales).</summary>
        private static void Tintar(GameObject casa, Color tinte)
        {
            MaterialPropertyBlock bloque = new MaterialPropertyBlock();
            bloque.SetColor("_BaseColor", tinte);
            foreach (Renderer r in casa.GetComponentsInChildren<Renderer>(true))
            {
                Material m = r.sharedMaterial;
                if (m != null && (m.name.StartsWith("S2c_Cuerpo_") || m.name.Contains("C03_Fachada") || m.name.Contains("C20")))
                {
                    r.SetPropertyBlock(bloque);
                }
            }
        }

        private static readonly Dictionary<string, Vector3> TamanosTripo = new Dictionary<string, Vector3>();

        /// <summary>Tamaño sin girar (x ancho, y alto, z fondo) del prefab de Tripo, medido con sus renderers; cero si no tiene.</summary>
        private static Vector3 TamanoTripo(GameObject prefab)
        {
            if (TamanosTripo.TryGetValue(prefab.name, out Vector3 ya))
            {
                return ya;
            }
            GameObject g = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            g.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            Renderer[] rs = g.GetComponentsInChildren<Renderer>(true);
            Vector3 tam = Vector3.zero;
            if (rs.Length > 0)
            {
                Bounds b = rs[0].bounds;
                foreach (Renderer r in rs)
                {
                    b.Encapsulate(r.bounds);
                }
                tam = b.size;
            }
            Object.DestroyImmediate(g);
            TamanosTripo[prefab.name] = tam;
            return tam;
        }

        /// <summary>Casa de Tripo con el frente (+Z del modelo) a la calle, apoyada en la base y pegada a la línea de fachada. Devuelve su alto.</summary>
        private static float ColocarTripo(Transform padre, CasaPlan c)
        {
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(c.Tripo, padre);
            go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.LookRotation(new Vector3(c.Fila.Normal, 0f, 0f)));
            go.transform.localScale = Vector3.one * c.Escala;
            Renderer[] rs = go.GetComponentsInChildren<Renderer>(true);
            Bounds b = rs[0].bounds;
            foreach (Renderer r in rs)
            {
                b.Encapsulate(r.bounds);
            }
            Vector3 destino = new Vector3(c.Fila.XFachada - c.Fila.Normal * b.size.x * 0.5f, c.Base, c.Z);
            go.transform.position += destino - new Vector3(b.center.x, b.min.y, b.center.z);
            return b.size.y;
        }

        private static void MarcarEstatico(Transform raiz, Transform sinEstatico)
        {
            const StaticEditorFlags banderas = StaticEditorFlags.ContributeGI | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic
                | StaticEditorFlags.BatchingStatic | StaticEditorFlags.ReflectionProbeStatic;
            foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
            {
                if (t == raiz || t.IsChildOf(sinEstatico) || t.GetComponent<Light>() != null)
                {
                    continue;
                }
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, banderas);
            }
        }

        private static Material MaterialTerreno()
        {
            Material m = AssetDatabase.LoadAssetAtPath<Material>(RutaMaterialTerreno);
            if (m != null)
            {
                // Color liso oscuro con poca textura (la textura estirada hacía franjas).
                m.SetTexture("_BaseMap", null);
                m.SetColor("_BaseColor", new Color(0.2f, 0.19f, 0.18f));
                EditorUtility.SetDirty(m);
                return m;
            }
            Texture2D t05 = null;
            if (File.Exists(FuenteT05))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(RutaTexturaT05));
                File.Copy(FuenteT05, RutaTexturaT05, true);
                AssetDatabase.ImportAsset(RutaTexturaT05);
                t05 = AssetDatabase.LoadAssetAtPath<Texture2D>(RutaTexturaT05);
            }
            Material origen = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Proyecto/Mundo/Materiales/Barrio_Concreto.mat");
            m = new Material(origen);
            m.SetColor("_BaseColor", t05 != null ? Color.white : new Color(0.2f, 0.19f, 0.18f));
            if (t05 != null)
            {
                m.SetTexture("_BaseMap", t05);
            }
            m.SetTextureScale("_BaseMap", Vector2.one);
            m.SetTextureScale("_BumpMap", Vector2.one);
            m.SetFloat("_Smoothness", 0.2f);
            AssetDatabase.CreateAsset(m, RutaMaterialTerreno);
            return m;
        }

        private static void GuardarMallas(List<Mesh> mallas)
        {
            Directory.CreateDirectory(CarpetaDatos);
            AssetDatabase.DeleteAsset(RutaMallas);
            AssetDatabase.CreateAsset(mallas[0], RutaMallas);
            foreach (Mesh m in mallas.Skip(1))
            {
                AssetDatabase.AddObjectToAsset(m, RutaMallas);
            }
            AssetDatabase.SaveAssets();
        }

        private static Transform Hijo(Transform padre, string nombre)
        {
            Transform t = new GameObject(nombre).transform;
            t.SetParent(padre, false);
            return t;
        }

        // ================================================================ Fotos

        [MenuItem("Mecha Cyberpunk/T1355/Cerro: fotos")]
        public static void Fotos()
        {
            ColocarS2.Abrir();
            ColocarS2cLook.Foto(K13Desde, K13Hacia, "docs/capturas/zonas/cerro-k13-juego.png", 66f);
            ColocarS2cLook.Foto(A13Desde, A13Hacia, "docs/capturas/zonas/cerro-a13-juego.png", 66f);
            ColocarS2.LadoALado("imageassets/referencias/look/K13_ColoniaCerro.png", "docs/capturas/zonas/cerro-k13-juego.png", "docs/capturas/zonas/cerro-k13.png");
            ColocarS2.LadoALado("imageassets/referencias/look/A13_ColoniaCerro_Aerea.png", "docs/capturas/zonas/cerro-a13-juego.png", "docs/capturas/zonas/cerro-a13.png");
            EditorSceneManager.OpenScene(ColocarS2.RutaEscena, OpenSceneMode.Single);
        }
    }
}
