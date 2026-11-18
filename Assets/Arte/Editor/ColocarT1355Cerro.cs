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
        public const string RutaCarro = "Assets/_Proyecto/Vehiculos/Prefabs/CarroViejo.prefab";
        public const string CarpetaModulos = "Assets/Arte/ZonasModulos/Prefabs/";
        public const string CarpetaTripo = "Assets/Arte/Zonas";

        public const int MaximoLuces = 10;
        public const int LucesDePostes = 7;
        public const float Fondo = 8f;
        public const float FachadaDesdeEje = Trazado.AnchoCalle * 0.5f + Trazado.AnchoBanqueta + 0.5f;
        public const float AltoBanqueta = 0.2f;
        public const float BajaPisoCalle = 0.12f;
        public const float AnchoEscalera = 3.5f;
        public const float HuellaEscalera = 0.32f;
        public const float MitadHuecoEscalera = 5f;

        /// <summary>z de las escaleras que bajan de la pierna 2 (x = -360) a la calle base.</summary>
        public static readonly float[] ZEscaleras = { 120f, 60f, 0f };

        // Tomas de revisión: K13 (en la calle, mirando cuesta arriba con las escaleras al centro) y A13 (aérea).
        public static readonly Vector3 K13Desde = new Vector3(-303f, 2.7f, 60f);
        public static readonly Vector3 K13Hacia = new Vector3(-352f, 17.5f, 60f);
        public static readonly Vector3 A13Desde = new Vector3(-283f, 62f, 14f);
        public static readonly Vector3 A13Hacia = new Vector3(-372f, 14f, 66f);

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
            fuentes.Add(new Terreno.Fuente { Puntos = u4.ToArray(), Semiancho = semiCalle, Baja = BajaPisoCalle });

            // ---- Escaleras (datos)
            List<(float z, float yArriba, float yAbajo, float xArriba, float xAbajo, int pasos)> escaleras = new List<(float, float, float, float, float, int)>();
            foreach (float z in ZEscaleras)
            {
                float xArriba = Trazado.XPierna2 + FachadaDesdeEje - 0.5f;
                float xAbajo = Trazado.XBase - FachadaDesdeEje + 0.5f;
                int pasos = Mathf.RoundToInt((xAbajo - xArriba) / HuellaEscalera);
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
            List<Fila> filas = Filas();
            List<CasaPlan> casas = new List<CasaPlan>();
            foreach (Fila f in filas)
            {
                casas.AddRange(PlanearFila(o, f));
            }
            List<Terreno.Plataforma> plataformas = casas.Select(c => new Terreno.Plataforma
            {
                X0 = Mathf.Min(c.Fila.XFachada, c.Fila.XFachada - c.Fila.Normal * Fondo) - 0.3f,
                X1 = Mathf.Max(c.Fila.XFachada, c.Fila.XFachada - c.Fila.Normal * Fondo) + 0.3f,
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
            Calle(o, callesPadre, "Calle_Base", calleBase, asfalto, banqueta, mallas);
            Calle(o, callesPadre, "Calle_U4", u4, asfalto, banqueta, mallas);
            Calle(o, callesPadre, "Calle_Zigzag", zigzag, asfalto, banqueta, mallas);

            // ---- Escaleras de concreto con su barandal
            Material concreto = MaterialTerreno();
            int numero = 0;
            foreach (var e in escaleras)
            {
                numero++;
                Mesh me = Terreno.Escalera("T1355_Escalera_" + numero, e.xArriba, e.yArriba, e.xAbajo, e.yAbajo, e.pasos, e.z - AnchoEscalera * 0.5f, e.z + AnchoEscalera * 0.5f);
                mallas.Add(me);
                GameObject go = new GameObject("CE01_EscaleraCerro_" + numero);
                go.transform.SetParent(escalerasPadre, false);
                go.AddComponent<MeshFilter>().sharedMesh = me;
                go.AddComponent<MeshRenderer>().sharedMaterial = concreto;
                go.AddComponent<MeshCollider>().sharedMesh = me;
                BarandalEscalera(escalerasPadre, huecosPadre, e.z, e.xArriba, e.yArriba, e.xAbajo, e.yAbajo);
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
                float azotea = KitS2c.Casa(o, casasPadre, centro, normal, c.Ancho, Fondo, c.Estilo);
                Vector3 cuerpo = new Vector3(c.Fila.XFachada - c.Fila.Normal * Fondo * 0.5f, c.Base - 3f - 0.02f, c.Z);
                KitS2c.Losa(o, casasPadre, "Cimiento", cuerpo, Quaternion.identity, new Vector3(Fondo, 6f, c.Ancho), concreto, 3f, false);
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
            CrearCalleZona(callesPadre, "CalleZona_Base", calleBase);
            CrearCalleZona(callesPadre, "CalleZona_U4", u4);
            CrearCalleZona(callesPadre, "CalleZona_Zigzag", zigzag);

            int luces = LimitarLuces(o.Raiz);
            MarcarEstatico(o.Raiz, carrosPadre);
            KitS2c.Terminar(o);
            GuardarMallas(mallas);
            ColocarS2.Informe.AppendLine("Cerro: " + casas.Count + " casas, " + escaleras.Count + " escaleras, " + o.Postes + " postes, " + o.Cables + " cables, " + o.Charcos
                + " charcos, " + luces + " luces, " + Huecos.Count + " tipos de hueco (" + string.Join(", ", Huecos.Select(h => h.Key + " x" + h.Value)) + ")");
            ColocarS2.Guardar("t1355-cerro");
        }

        // ================================================================ Filas de casas

        private static List<Fila> Filas()
        {
            float zNorte = 165f, zSur = -22f;
            float[][] sinEscaleras = { new[] { zNorte, zSur } };
            // Tramos del lado de abajo: se parten donde cruzan las escaleras.
            float[][] conHuecos = TramosConHuecos(zNorte, zSur);
            float[][] conHuecosBase = TramosConHuecos(140f, -105f);
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
                    estilo.Pisos = 2;
                    estilo.Foco = false;
                    estilo.Neon = o.Chance(0.3f) ? (o.Chance(0.5f) ? KitS2c.Magenta : KitS2c.Turquesa) : (Color?)null;
                    estilo.Toldo = o.Chance(0.4f);
                    float alto = KitS2c.Medir(estilo.PlantaBaja).Caja.size.y * KitS2c.EscalaAlto + KitS2c.Medir(estilo.PisoAlto).Caja.size.y * KitS2c.EscalaAlto;
                    float z = cursor - ancho * 0.5f;
                    tramo.Add(new CasaPlan { Fila = f, Z = z, Ancho = ancho, YCalle = f.YCalle(z), Alto = alto, Estilo = estilo, Cadena = cadena });
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

        private static void Calle(KitS2c.Obra o, Transform padre, string nombre, List<Vector3> pts, Material asfalto, Material banqueta, List<Mesh> mallas)
        {
            Transform raiz = Hijo(padre, nombre);
            float mitad = Trazado.AnchoCalle * 0.5f;
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

        private static void CrearCalleZona(Transform padre, string nombre, List<Vector3> pts)
        {
            GameObject go = new GameObject(nombre);
            go.transform.SetParent(padre, false);
            CalleZona cz = go.AddComponent<CalleZona>();
            cz.Definir(pts, Trazado.AnchoCalle, true, false, "Cerro");
        }

        // ================================================================ Escaleras, barandales y accesos

        private static void BarandalEscalera(Transform padre, Transform huecos, float z, float xArriba, float yArriba, float xAbajo, float yAbajo)
        {
            float run = xAbajo - xArriba;
            float s = (yArriba - yAbajo) / run;
            Vector3 arriba = new Vector3(s, 1f, 0f).normalized;
            int tramos = Mathf.CeilToInt(run / 4f);
            foreach (float lado in new[] { 1f, -1f })
            {
                for (int i = 0; i < tramos; i++)
                {
                    float x = xArriba + (i + 0.5f) * 4f;
                    if (x > xAbajo)
                    {
                        break;
                    }
                    Vector3 pos = new Vector3(x, Mathf.Lerp(yArriba, yAbajo, (x - xArriba) / run), z + lado * (AnchoEscalera * 0.5f + 0.1f));
                    Modulo("CE03_BarandalTuboEscalera", padre, huecos, pos, Quaternion.LookRotation(new Vector3(0f, 0f, lado), arriba));
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
        private static void Modulo(string codigo, Transform padre, Transform huecos, Vector3 pos, Quaternion giro)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CarpetaModulos + codigo + ".prefab");
            if (prefab != null)
            {
                GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
                go.transform.SetPositionAndRotation(pos, giro);
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
            for (float z = 160f; z > -20f; z -= 26f)
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
            for (float z = -98f; z < 140f; z += 28f)
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
            foreach (Vector3 p in postesOeste.Concat(postesEste).Concat(postesBase).Concat(postesTres))
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

        /// <summary>Deja a lo más <see cref="MaximoLuces"/> Light reales: <see cref="LucesDePostes"/> de postes repartidas y el resto de neones. Devuelve las que quedan.</summary>
        public static int LimitarLuces(Transform raiz)
        {
            List<Light> luces = raiz.GetComponentsInChildren<Light>(true).ToList();
            List<Light> deposte = luces.Where(l => l.transform.parent != null && l.transform.parent.name.StartsWith("Poste_")).ToList();
            List<Light> otras = luces.Except(deposte).ToList();
            HashSet<Light> quedan = new HashSet<Light>();
            if (deposte.Count > 0)
            {
                float paso = Mathf.Max(1f, deposte.Count / (float)LucesDePostes);
                for (int i = 0; i < LucesDePostes && Mathf.RoundToInt(i * paso) < deposte.Count; i++)
                {
                    quedan.Add(deposte[Mathf.RoundToInt(i * paso)]);
                }
            }
            foreach (Light l in otras.Take(MaximoLuces - quedan.Count))
            {
                quedan.Add(l);
            }
            foreach (Light l in luces)
            {
                if (!quedan.Contains(l))
                {
                    Object.DestroyImmediate(l.gameObject);
                }
            }
            return quedan.Count;
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
            m.SetColor("_BaseColor", t05 != null ? Color.white : new Color(0.5f, 0.47f, 0.44f));
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
