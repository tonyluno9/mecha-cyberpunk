using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Mundo.Editor
{
    /// <summary>
    /// T9 (T-1601): el muro perimetral completo y bien unido, sin nada afuera. Trabaja sobre Ciudad_Prologo y reutiliza T-1513/T-1524/T-1543.
    /// - Diagnostico: recorre los 4 lados y escribe Logs/t1601-muro.txt con cada union entre piezas vecinas (huecos, encimados, saltos de altura
    ///   o de rotacion, piezas flotando o enterradas, torres desalineadas) y el barrido de raycasts del perimetro. No guarda.
    /// - Aplicar: acomoda cada pieza (misma base en el piso, rotacion alineada al lado, extremo con extremo, torres en la linea y en las esquinas),
    ///   cierra con colliders lo que siga abierto, apaga/borra lo que quede afuera y guarda la escena. Idempotente.
    /// - Fotos: capturas en docs/capturas/_tareas/T9/. Va en una corrida de Unity APARTE (en la misma de Aplicar salen negras).
    /// Orden en batchmode (Unity cerrado):
    ///   -executeMethod MechaCyberpunk.Mundo.Editor.MuroPerimetroT1601.Diagnostico | Aplicar | Fotos
    /// </summary>
    public static class MuroPerimetroT1601
    {
        public const string RutaLog = "Logs/t1601-muro.txt";
        public const string CarpetaFotos = "docs/capturas/_tareas/T9";
        public const string NombreRaizCierre = "T1601_MuroCierre";
        public const string Etiqueta = "[T-1601] ";

        /// <summary>Hueco o encimado que se tolera entre piezas vecinas (m).</summary>
        public const float TolUnion = 0.2f;
        /// <summary>Salto de base (m) entre piezas vecinas que ya cuenta como desnivel.</summary>
        public const float TolAltura = 0.25f;
        /// <summary>Piezas con la base mas arriba que el piso (m): flotando.</summary>
        public const float TolFlota = 0.15f;
        /// <summary>Piezas hundidas mas que esto (m) en el piso: enterradas.</summary>
        public const float TolEnterrada = 1.5f;
        /// <summary>Grados de diferencia con el eje del lado que se toleran.</summary>
        public const float TolGiro = 1.5f;
        /// <summary>Descentrado de una torre respecto a la linea del muro (m).</summary>
        public const float TolTorre = 1.0f;

        public enum Tipo { Tramo, Torre, Puerta }

        public class Pieza
        {
            public Transform T;
            public Tipo Tipo;
            public int Lado;
            public float Ini;      // extremo a lo largo del lado
            public float Fin;
            public float Perp;     // posicion perpendicular (hacia afuera) del centro
            public float Grosor;   // medida perpendicular
            public float BaseY;
            public float TopY;
            public float YawDesvio; // grados contra el eje del lado (modulo 180)
            public bool EsquinaSinTorre;
        }

        public class Union
        {
            public string Texto;
            public string Clase;
        }

        public class Resultado
        {
            public CalculoMuroT1524.Lado[] Lados;
            public List<Pieza> Piezas = new List<Pieza>();
            public List<Union> Problemas = new List<Union>();
            public int Uniones;
            public int Muestras;
            public int RayosSinChoque;
            public List<string> SinChoque = new List<string>();
        }

        // ------------------------------------------------------------------ entradas

        [MenuItem("Mecha Cyberpunk/T-1601 Muro: diagnostico")]
        public static void Diagnostico()
        {
            ReemplazarEdificiosT1502.Abrir();
            Resultado r = Analizar();
            Escribir(Informe("DIAGNOSTICO", r, null));
            Debug.Log(Etiqueta + "diagnostico: " + r.Piezas.Count + " piezas, " + r.Uniones + " uniones, " + r.Problemas.Count + " problemas, " + r.RayosSinChoque + " rayos sin choque; ver " + RutaLog);
        }

        [MenuItem("Mecha Cyberpunk/T-1601 Muro: aplicar")]
        public static void Aplicar()
        {
            Scene escena = ReemplazarEdificiosT1502.Abrir();
            Resultado antes = Analizar();
            StringBuilder cambios = new StringBuilder();

            // 1. Cada pieza queda alineada, en la linea, extremo con extremo y con la base en el piso.
            for (int pasada = 0; pasada < 3; pasada++)
            {
                Resultado r = pasada == 0 ? antes : Analizar();
                Acomodar(r, cambios);
            }

            // 2. Colliders: la raiz de cierre se rehace con lo que siga abierto.
            GameObject viejo = GameObject.Find(NombreRaizCierre);
            if (viejo != null)
            {
                Object.DestroyImmediate(viejo);
            }
            Physics.SyncTransforms();
            int cajas = CerrarConColliders(escena, cambios);

            // 3. Lo de afuera: borrar de verdad lo que quedo apagado y apagar lo que siga activo.
            string afuera = LimpiarAfuera(escena);
            cambios.AppendLine(afuera);

            Resultado despues = Analizar();
            Escribir(Informe("ANTES de aplicar", antes, null) + "\n=== CAMBIOS ===\n" + cambios + "\nCajas de cierre: " + cajas + "\n\n" + Informe("DESPUES de aplicar", despues, null));
            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
            Debug.Log(Etiqueta + "antes " + antes.Problemas.Count + " problemas y " + antes.RayosSinChoque + " rayos sin choque; despues " + despues.Problemas.Count + " problemas y "
                + despues.RayosSinChoque + " rayos sin choque; ver " + RutaLog);
        }

        [MenuItem("Mecha Cyberpunk/T-1601 Muro: fotos")]
        public static void Fotos()
        {
            ReemplazarEdificiosT1502.Abrir();
            Directory.CreateDirectory(CarpetaFotos);
            Resultado r = Analizar();
            Rect l = FueraDelMuroT1543.LineaDelMuro();
            float cx = l.center.x;
            float cz = l.center.y;
            Vector2[] esquinas = { new Vector2(l.xMin, l.yMin), new Vector2(l.xMin, l.yMax), new Vector2(l.xMax, l.yMax), new Vector2(l.xMax, l.yMin) };
            string[] nombres = { "SO", "NO", "NE", "SE" };
            for (int i = 0; i < 4; i++)
            {
                Vector3 e = new Vector3(esquinas[i].x, 0f, esquinas[i].y);
                Vector3 haciaDentro = (new Vector3(cx, 0f, cz) - e).normalized;
                Foto("esquina_" + nombres[i], e - haciaDentro * 0f + haciaDentro * 22f + Vector3.up * 9f, e + Vector3.up * 3f, 60f);
            }
            int n = 0;
            foreach (Pieza p in r.Piezas.Where(x => x.Tipo == Tipo.Puerta).OrderBy(x => x.Lado).ThenBy(x => x.Ini))
            {
                Vector3 c = p.T.position;
                Renderer[] rs = p.T.GetComponentsInChildren<Renderer>();
                Bounds b = rs[0].bounds;
                foreach (Renderer rd in rs)
                {
                    b.Encapsulate(rd.bounds);
                }
                Vector3 dentro = -r.Lados[p.Lado].Fuera;
                Foto("puerta_" + n, new Vector3(b.center.x, 0f, b.center.z) + dentro * 24f + Vector3.up * 7f, new Vector3(b.center.x, 4f, b.center.z), 60f);
                n++;
            }
            Foto("aerea", new Vector3(cx, 330f, cz - 20f), new Vector3(cx, 0f, cz), 40f);
        }

        // ------------------------------------------------------------------ analisis

        public static List<Transform> PiezasDelMuro()
        {
            List<Transform> lista = new List<Transform>();
            foreach (string nombre in new[] { MuroCiudadT1513.NombreRaiz, MuroCierreT1524.NombreRaiz })
            {
                GameObject raiz = GameObject.Find(nombre);
                if (raiz == null)
                {
                    continue;
                }
                foreach (Transform h in raiz.transform)
                {
                    if (h.GetComponentInChildren<Renderer>() != null)
                    {
                        lista.Add(h);
                    }
                }
            }
            return lista;
        }

        private static Tipo TipoDe(Transform t)
        {
            if (t.name.StartsWith("Torre"))
            {
                return Tipo.Torre;
            }
            return t.name.StartsWith("Puerta_") ? Tipo.Puerta : Tipo.Tramo;
        }

        private static Bounds Caja(Transform t)
        {
            Renderer[] rs = t.GetComponentsInChildren<Renderer>();
            Bounds b = rs[0].bounds;
            foreach (Renderer r in rs)
            {
                b.Encapsulate(r.bounds);
            }
            return b;
        }

        /// <summary>Mide cada pieza contra el lado del rectangulo al que pertenece (el mas cercano a su centro).</summary>
        public static Resultado Analizar()
        {
            Physics.SyncTransforms();
            Resultado res = new Resultado { Lados = MuroCierreT1524.LadosDelMuro() };
            foreach (Transform t in PiezasDelMuro())
            {
                Bounds b = Caja(t);
                int mejor = 0;
                float dm = float.MaxValue;
                for (int i = 0; i < res.Lados.Length; i++)
                {
                    float d = Mathf.Abs(Vector3.Dot(b.center - res.Lados[i].Centro, res.Lados[i].Fuera));
                    float dt = Mathf.Abs(Vector3.Dot(b.center - res.Lados[i].Centro, res.Lados[i].Eje));
                    if (dt > res.Lados[i].Mitad + 6f)
                    {
                        d += 1000f; // fuera del tramo del lado: es de otro
                    }
                    if (d < dm)
                    {
                        dm = d;
                        mejor = i;
                    }
                }
                CalculoMuroT1524.Lado lado = res.Lados[mejor];
                float cT = Vector3.Dot(b.center - lado.Centro, lado.Eje);
                float cP = Vector3.Dot(b.center - lado.Centro, lado.Fuera);
                float mitadT = Mathf.Abs(lado.Eje.x) * b.extents.x + Mathf.Abs(lado.Eje.z) * b.extents.z;
                float mitadP = Mathf.Abs(lado.Fuera.x) * b.extents.x + Mathf.Abs(lado.Fuera.z) * b.extents.z;
                Vector3 f = t.forward;
                f.y = 0f;
                float yaw = Vector3.Angle(f.normalized, lado.Eje);
                yaw = Mathf.Min(yaw, 180f - yaw);
                // un modelo largo en X o Z: el desvio solo cuenta contra el eje del modelo, que es 0 o 90 contra el lado.
                float desvio = Mathf.Min(yaw, Mathf.Abs(90f - yaw));
                res.Piezas.Add(new Pieza
                {
                    T = t,
                    Tipo = TipoDe(t),
                    Lado = mejor,
                    Ini = cT - mitadT,
                    Fin = cT + mitadT,
                    Perp = cP,
                    Grosor = mitadP * 2f,
                    BaseY = b.min.y,
                    TopY = b.max.y,
                    YawDesvio = desvio,
                });
            }

            float baseTipica = res.Piezas.Count == 0 ? 0f : res.Piezas.Where(p => p.Tipo == Tipo.Tramo).Select(p => p.BaseY).DefaultIfEmpty(0f).Min();
            for (int i = 0; i < res.Lados.Length; i++)
            {
                List<Pieza> lado = res.Piezas.Where(p => p.Lado == i).OrderBy(p => p.Ini).ThenBy(p => p.Fin).ToList();
                Pieza prev = null;
                foreach (Pieza p in lado)
                {
                    string nom = res.Lados[i].Nombre + " " + p.T.name;
                    float piso = PisoBajo(p, res.Lados[i]);
                    string quienPiso;
                    PisoEn(p.T.position, out quienPiso);
                    if (p.BaseY - piso > TolFlota)
                    {
                        res.Problemas.Add(new Union { Clase = "FLOTANDO", Texto = nom + ": base " + p.BaseY.ToString("F2") + " m, piso " + piso.ToString("F2") + " (" + (p.BaseY - piso).ToString("F2") + " m arriba)" });
                    }
                    else if (piso - p.BaseY > TolEnterrada)
                    {
                        res.Problemas.Add(new Union { Clase = "ENTERRADA", Texto = nom + ": base " + p.BaseY.ToString("F2") + " m, piso " + piso.ToString("F2") + " (" + (piso - p.BaseY).ToString("F2") + " m abajo; piso: " + quienPiso + ")" });
                    }
                    if (p.Tipo != Tipo.Torre && p.YawDesvio > TolGiro)
                    {
                        res.Problemas.Add(new Union { Clase = "ROTACION", Texto = nom + ": " + p.YawDesvio.ToString("F1") + " grados fuera del eje del lado" });
                    }
                    if (p.Tipo == Tipo.Torre && Mathf.Abs(p.Perp) > TolTorre)
                    {
                        res.Problemas.Add(new Union { Clase = "TORRE", Texto = nom + ": descentrada " + p.Perp.ToString("F2") + " m de la linea del muro" });
                    }
                    if (p.Tipo == Tipo.Tramo && Mathf.Abs(p.Perp) > TolTorre)
                    {
                        res.Problemas.Add(new Union { Clase = "DESALINEADO", Texto = nom + ": fuera de linea " + p.Perp.ToString("F2") + " m" });
                    }
                    if (prev != null)
                    {
                        res.Uniones++;
                        float hueco = p.Ini - prev.Fin;
                        bool alguienTorre = p.Tipo == Tipo.Torre || prev.Tipo == Tipo.Torre;
                        string par = res.Lados[i].Nombre + " " + prev.T.name + " | " + p.T.name;
                        if (hueco > TolUnion)
                        {
                            res.Problemas.Add(new Union { Clase = "HUECO", Texto = par + ": " + hueco.ToString("F2") + " m de hueco en t " + prev.Fin.ToString("F1") + ".." + p.Ini.ToString("F1") });
                        }
                        else if (hueco < -TolUnion && !(alguienTorre || prev.Tipo == Tipo.Puerta || p.Tipo == Tipo.Puerta))
                        {
                            res.Problemas.Add(new Union { Clase = "ENCIMADO", Texto = par + ": se encima " + (-hueco).ToString("F2") + " m en t " + p.Ini.ToString("F1") });
                        }
                        if (Mathf.Abs(p.BaseY - prev.BaseY) > TolAltura && p.Tipo == prev.Tipo)
                        {
                            res.Problemas.Add(new Union { Clase = "SALTO_ALTURA", Texto = par + ": base " + prev.BaseY.ToString("F2") + " vs " + p.BaseY.ToString("F2") });
                        }
                        if (p.Tipo == Tipo.Tramo && prev.Tipo == Tipo.Tramo && Mathf.Abs(p.TopY - prev.TopY) > TolAltura)
                        {
                            res.Problemas.Add(new Union { Clase = "SALTO_ALTURA", Texto = par + ": cima " + prev.TopY.ToString("F2") + " vs " + p.TopY.ToString("F2") });
                        }
                        if (p.Tipo == Tipo.Tramo && prev.Tipo == Tipo.Tramo && Mathf.Abs(p.YawDesvio - prev.YawDesvio) > TolGiro)
                        {
                            res.Problemas.Add(new Union { Clase = "SALTO_ROTACION", Texto = par + ": " + prev.YawDesvio.ToString("F1") + " vs " + p.YawDesvio.ToString("F1") });
                        }
                    }
                    prev = p;
                }
                // extremos del lado: deben llegar a la esquina (torre o tramo)
                if (lado.Count > 0)
                {
                    float finLado = res.Lados[i].Mitad;
                    if (lado.First().Ini > -finLado + 4f)
                    {
                        res.Problemas.Add(new Union { Clase = "HUECO", Texto = res.Lados[i].Nombre + ": falta muro al inicio (empieza en t " + lado.First().Ini.ToString("F1") + ", esquina " + (-finLado).ToString("F1") + ")" });
                    }
                    if (lado.Last().Fin < finLado - 4f)
                    {
                        res.Problemas.Add(new Union { Clase = "HUECO", Texto = res.Lados[i].Nombre + ": falta muro al final (termina en t " + lado.Last().Fin.ToString("F1") + ", esquina " + finLado.ToString("F1") + ")" });
                    }
                }
            }
            // una torre en cada esquina
            foreach (Vector3 e in Esquinas(res.Lados))
            {
                bool hay = res.Piezas.Any(p => p.Tipo == Tipo.Torre && Vector2.Distance(new Vector2(p.T.position.x, p.T.position.z), new Vector2(e.x, e.z)) < 5f)
                    || res.Piezas.Any(p => p.Tipo == Tipo.Torre && Vector2.Distance(new Vector2(Caja(p.T).center.x, Caja(p.T).center.z), new Vector2(e.x, e.z)) < 5f);
                if (!hay)
                {
                    res.Problemas.Add(new Union { Clase = "TORRE", Texto = "esquina " + e + ": sin torre MU02" });
                }
            }

            Barrer(res);
            return res;
        }

        private static Vector3[] Esquinas(CalculoMuroT1524.Lado[] l)
        {
            float maxX = l[0].Centro.x;
            float minX = l[1].Centro.x;
            float maxZ = l[2].Centro.z;
            float minZ = l[3].Centro.z;
            return new[] { new Vector3(minX, 0f, minZ), new Vector3(minX, 0f, maxZ), new Vector3(maxX, 0f, maxZ), new Vector3(maxX, 0f, minZ) };
        }

        /// <summary>El piso mas alto bajo los dos extremos y el centro de la pieza (sin contar el muro ni sus cajas).</summary>
        private static float PisoBajo(Pieza p, CalculoMuroT1524.Lado lado)
        {
            Bounds b = Caja(p.T);
            float mejor = float.MinValue;
            foreach (float k in new[] { -0.4f, 0f, 0.4f })
            {
                Vector3 pt = b.center + lado.Eje * ((p.Fin - p.Ini) * k);
                mejor = Mathf.Max(mejor, PisoEn(pt));
            }
            return mejor == float.MinValue ? p.BaseY : mejor;
        }

        private static float PisoEn(Vector3 pt)
        {
            string quien;
            return PisoEn(pt, out quien);
        }

        private static float PisoEn(Vector3 pt, out string quien)
        {
            quien = "";
            float y = float.MinValue;
            foreach (RaycastHit h in Physics.RaycastAll(new Vector3(pt.x, 30f, pt.z), Vector3.down, 80f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (EsDelMuro(h.collider.transform) || h.normal.y < 0.5f)
                {
                    continue;
                }
                // un piso alto de verdad (hasta 3 m: banqueta o terreno); lo mas alto sin contar techos y edificios
                if (h.point.y <= 1f && h.point.y > y)
                {
                    y = h.point.y;
                    quien = h.collider.name + " (" + (h.collider.transform.parent != null ? h.collider.transform.parent.name : "") + ")";
                }
            }
            return y;
        }

        private static bool EsDelMuro(Transform t)
        {
            for (Transform p = t; p != null; p = p.parent)
            {
                string n = p.name;
                if (n == MuroCiudadT1513.NombreRaiz || n == MuroCierreT1524.NombreRaiz || n == NombreRaizCierre || n == "LimiteInvisible" || n == "Muro" || n.StartsWith("MuroAlto_")
                    || n.StartsWith("PuertaSolida_") || n.StartsWith("CierreColision_"))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Rayos cada 1 m a 0.5, 2, 5 y 8 m de altura desde 4 m adentro hacia afuera: todos deben chocar con el muro (sin puertas: son solidas).</summary>
        private static void Barrer(Resultado res)
        {
            foreach (CalculoMuroT1524.Lado lado in res.Lados)
            {
                for (float t = -lado.Mitad + 1f; t <= lado.Mitad - 1f; t += 1f)
                {
                    foreach (float h in new[] { 0.5f, 2f, 5f, 8f })
                    {
                        res.Muestras++;
                        Vector3 pt = lado.Punto(t);
                        Vector3 o = pt - lado.Fuera * 4f + Vector3.up * h;
                        bool choca = false;
                        foreach (RaycastHit g in Physics.RaycastAll(o, lado.Fuera, 8f, ~0, QueryTriggerInteraction.Ignore))
                        {
                            if (EsDelMuro(g.collider.transform))
                            {
                                choca = true;
                                break;
                            }
                        }
                        if (!choca)
                        {
                            res.RayosSinChoque++;
                            if (res.SinChoque.Count < 200)
                            {
                                res.SinChoque.Add(lado.Nombre + " t " + t.ToString("F0") + " h " + h);
                            }
                        }
                    }
                }
            }
        }

        // ------------------------------------------------------------------ arreglos

        /// <summary>Acomoda las piezas: giro alineado, centrada en la linea, base en el piso y tramos pegados extremo con extremo.</summary>
        private static void Acomodar(Resultado r, StringBuilder log)
        {
            Physics.SyncTransforms();
            for (int i = 0; i < r.Lados.Length; i++)
            {
                CalculoMuroT1524.Lado lado = r.Lados[i];
                List<Pieza> piezas = r.Piezas.Where(p => p.Lado == i).OrderBy(p => p.Ini).ToList();
                foreach (Pieza p in piezas)
                {
                    // 1. la linea: centro perpendicular a 0 (piezas de tramo y torres)
                    if (Mathf.Abs(p.Perp) > 0.05f && p.Tipo != Tipo.Puerta)
                    {
                        p.T.position -= lado.Fuera * p.Perp;
                        log.AppendLine("  linea: " + lado.Nombre + " " + p.T.name + " movida " + (-p.Perp).ToString("F2") + " m");
                    }
                }
                Physics.SyncTransforms();
                // 2. los tramos MU01 se tocan: cada uno se pega al extremo del anterior (de adentro hacia las esquinas se respeta el primero)
                List<Pieza> tramos = piezas.Where(p => p.Tipo == Tipo.Tramo).ToList();
                for (int k = 1; k < tramos.Count; k++)
                {
                    Pieza a = tramos[k - 1];
                    Pieza b = tramos[k];
                    Bounds ba = Caja(a.T);
                    Bounds bb = Caja(b.T);
                    float finA = Vector3.Dot(ba.center - lado.Centro, lado.Eje) + (Mathf.Abs(lado.Eje.x) * ba.extents.x + Mathf.Abs(lado.Eje.z) * ba.extents.z);
                    float iniB = Vector3.Dot(bb.center - lado.Centro, lado.Eje) - (Mathf.Abs(lado.Eje.x) * bb.extents.x + Mathf.Abs(lado.Eje.z) * bb.extents.z);
                    float d = iniB - finA;
                    bool entreHay = piezas.Any(x => x != a && x != b && x.Tipo != Tipo.Tramo && x.Ini >= a.Fin - 0.5f && x.Fin <= b.Ini + 0.5f);
                    if (Mathf.Abs(d) > 0.03f && Mathf.Abs(d) <= 1.5f && !entreHay)
                    {
                        b.T.position -= lado.Eje * d;
                        log.AppendLine("  union: " + lado.Nombre + " " + a.T.name + " | " + b.T.name + " " + (d > 0 ? "hueco " : "encimado ") + Mathf.Abs(d).ToString("F2") + " m corregido");
                        Physics.SyncTransforms();
                    }
                }
                // 2b. huecos que quedan entre cualquier par de piezas (torre-puerta): se rellenan con MU01 pegado a las dos
                for (int k = 1; k < piezas.Count; k++)
                {
                    float hueco = piezas[k].Ini - piezas[k - 1].Fin;
                    if (hueco > TolUnion && hueco < 12f)
                    {
                        Rellenar(r, lado, piezas[k - 1].Fin, piezas[k].Ini, log);
                    }
                }
                // 3. base en el piso
                foreach (Pieza p in piezas)
                {
                    float piso = PisoBajo(p, lado);
                    Bounds b = Caja(p.T);
                    float dy = piso - b.min.y;
                    if ((dy < -0.02f && dy > -4f) || (dy > 0.02f && dy < 0.3f))
                    {
                        p.T.position += Vector3.up * dy;
                        log.AppendLine("  altura: " + lado.Nombre + " " + p.T.name + " " + dy.ToString("F2") + " m");
                    }
                }
                Physics.SyncTransforms();
            }
        }

        private static GameObject prefabA;
        private static GameObject prefabB;

        /// <summary>Pone MU01 de t0 a t1 en el lado, con la altura, el grosor y la linea de los tramos vecinos.</summary>
        private static void Rellenar(Resultado r, CalculoMuroT1524.Lado lado, float t0, float t1, StringBuilder log)
        {
            if (prefabA == null)
            {
                prefabA = MuroCiudadT1513.Cargar(MuroCiudadT1513.PrefabA);
                prefabB = MuroCiudadT1513.Cargar(MuroCiudadT1513.PrefabB);
            }
            Pieza modelo = r.Piezas.First(p => p.Tipo == Tipo.Tramo);
            float alto = modelo.TopY - modelo.BaseY;
            Vector3 tamA = MuroCiudadT1513.Medir(prefabA);
            Vector3 tamB = MuroCiudadT1513.Medir(prefabB);
            float escalaA = alto / tamA.y;
            float escalaB = alto / tamB.y;
            float largoA = Mathf.Max(tamA.x, tamA.z) * escalaA;
            float largoB = Mathf.Max(tamB.x, tamB.z) * escalaB;
            CalculoMuroT1513.Reparto rep = CalculoMuroT1513.Repartir(t1 - t0, largoA, largoB);
            GameObject raiz = GameObject.Find(MuroCiudadT1513.NombreRaiz);
            float recorrido = 0f;
            for (int k = 0; k < rep.EsB.Count; k++)
            {
                bool esB = rep.EsB[k];
                float lp = (esB ? largoB : largoA) * rep.Factor;
                Vector3 centro = lado.Punto(t0 + recorrido + lp * 0.5f);
                recorrido += lp;
                MuroCiudadT1513.Colocar(esB ? prefabB : prefabA, raiz.transform, "Relleno_T1601_" + lado.Nombre + "_" + raiz.transform.childCount + (esB ? "b" : "a"),
                    centro, lado.Eje, esB ? escalaB : escalaA, rep.Factor, true, 0f);
            }
            log.AppendLine("  relleno MU01: " + lado.Nombre + " t " + t0.ToString("F1") + ".." + t1.ToString("F1") + " (" + (t1 - t0).ToString("F2") + " m, " + rep.EsB.Count + " piezas, factor " + rep.Factor.ToString("F2") + ")");
            Physics.SyncTransforms();
        }

        private static int CerrarConColliders(Scene escena, StringBuilder log)
        {
            Resultado r = Analizar();
            GameObject raiz = new GameObject(NombreRaizCierre);
            SceneManager.MoveGameObjectToScene(raiz, escena);
            int cajas = 0;
            const float Alto = 10f;
            foreach (CalculoMuroT1524.Lado lado in r.Lados)
            {
                List<float> ts = new List<float>();
                List<bool> cerrado = new List<bool>();
                for (float t = -lado.Mitad + 0.5f; t <= lado.Mitad - 0.5f; t += 1f)
                {
                    ts.Add(t);
                    bool ok = true;
                    foreach (float h in new[] { 0.5f, 2f, 5f, 8f })
                    {
                        bool choca = false;
                        foreach (RaycastHit g in Physics.RaycastAll(lado.Punto(t) - lado.Fuera * 4f + Vector3.up * h, lado.Fuera, 8f, ~0, QueryTriggerInteraction.Ignore))
                        {
                            if (EsDelMuro(g.collider.transform))
                            {
                                choca = true;
                                break;
                            }
                        }
                        ok &= choca;
                    }
                    cerrado.Add(ok);
                }
                float baseY = r.Piezas.Count > 0 ? r.Piezas.Min(p => p.BaseY) - 0.5f : -0.5f;
                foreach (CalculoMuroT1524.Hueco h in CalculoMuroT1524.Huecos(0, ts, cerrado))
                {
                    float ini = h.TIni - 0.6f;
                    float fin = h.TFin + 0.6f;
                    GameObject g = new GameObject("Cierre_" + lado.Nombre + "_" + cajas);
                    g.transform.SetParent(raiz.transform, false);
                    Vector3 c = lado.Punto((ini + fin) * 0.5f);
                    g.transform.SetPositionAndRotation(new Vector3(c.x, baseY + Alto * 0.5f, c.z), Quaternion.LookRotation(lado.Eje));
                    g.AddComponent<BoxCollider>().size = new Vector3(2f, Alto, fin - ini);
                    g.isStatic = true;
                    log.AppendLine("  collider de cierre: " + lado.Nombre + " t " + h.TIni.ToString("F1") + ".." + h.TFin.ToString("F1"));
                    cajas++;
                }
            }
            Physics.SyncTransforms();
            return cajas;
        }

        /// <summary>Borra de la escena lo que solo estaba apagado afuera y apaga (si no, borra) lo que siga activo afuera.</summary>
        private static string LimpiarAfuera(Scene escena)
        {
            Rect linea = FueraDelMuroT1543.LineaDelMuro();
            float margen = FueraDelMuroT1543.GrosorDelMuro() + 1f;
            int borrados = 0;
            List<string> nombres = new List<string>();
            foreach (GameObject raiz in escena.GetRootGameObjects())
            {
                borrados += BorrarInactivosAfuera(raiz.transform, linea, margen, nombres);
            }
            List<string> activos = FueraDelMuroT1543.ActivosAfuera(escena, linea, margen);
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Objetos apagados afuera borrados de la escena: " + borrados);
            foreach (string n in nombres.Take(60))
            {
                sb.AppendLine("  borrado: " + n);
            }
            sb.AppendLine("Activos afuera que quedan: " + activos.Count);
            foreach (string a in activos.Take(40))
            {
                sb.AppendLine("  " + a);
            }
            return sb.ToString();
        }

        /// <summary>Un objeto inactivo cuyo centro (de sus renderers/colliders) queda fuera del muro y que no es parte del muro se destruye con sus hijos.</summary>
        private static int BorrarInactivosAfuera(Transform t, Rect linea, float margen, List<string> nombres)
        {
            if (t == null)
            {
                return 0;
            }
            string n = t.name;
            if (EsDelMuro(t) || n == "T1543_Relleno" || n == FueraDelMuroT1543.RaizLuces || t.GetComponentInChildren<Camera>(true) != null)
            {
                return 0;
            }
            if (!t.gameObject.activeSelf && CentroFuera(t, linea, margen))
            {
                nombres.Add(Ruta(t));
                Object.DestroyImmediate(t.gameObject);
                return 1;
            }
            int total = 0;
            for (int i = t.childCount - 1; i >= 0; i--)
            {
                total += BorrarInactivosAfuera(t.GetChild(i), linea, margen, nombres);
            }
            return total;
        }

        private static bool CentroFuera(Transform t, Rect linea, float margen)
        {
            Renderer[] rs = t.GetComponentsInChildren<Renderer>(true);
            if (rs.Length == 0)
            {
                return CalculoMuroT1543.CentroFuera(linea, margen, t.position) && t.childCount == 0;
            }
            Bounds b = rs[0].bounds;
            foreach (Renderer r in rs)
            {
                b.Encapsulate(r.bounds);
            }
            // todo el objeto afuera: ni su caja toca el rectangulo con margen
            return CalculoMuroT1543.CajaFuera(linea, margen, b);
        }

        private static string Ruta(Transform t)
        {
            StringBuilder sb = new StringBuilder(t.name);
            for (Transform p = t.parent; p != null; p = p.parent)
            {
                sb.Insert(0, p.name + "/");
            }
            return sb.ToString();
        }

        // ------------------------------------------------------------------ informe y fotos

        private static string Informe(string titulo, Resultado r, string extra)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=== T-1601 " + titulo + " (" + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm") + ") ===");
            foreach (CalculoMuroT1524.Lado l in r.Lados)
            {
                List<Pieza> ps = r.Piezas.Where(p => p.Lado == System.Array.IndexOf(r.Lados, l)).ToList();
                sb.AppendLine("Lado " + l.Nombre + ": largo " + (l.Mitad * 2f).ToString("F0") + " m, tramos " + ps.Count(p => p.Tipo == Tipo.Tramo) + ", torres " + ps.Count(p => p.Tipo == Tipo.Torre) + ", puertas " + ps.Count(p => p.Tipo == Tipo.Puerta));
            }
            sb.AppendLine("Piezas: " + r.Piezas.Count + ", uniones revisadas: " + r.Uniones);
            Dictionary<string, int> clases = r.Problemas.GroupBy(p => p.Clase).ToDictionary(g => g.Key, g => g.Count());
            sb.AppendLine("PROBLEMAS DE UNION: " + r.Problemas.Count + " (" + string.Join(", ", clases.Select(k => k.Key + " " + k.Value)) + ")");
            foreach (Union u in r.Problemas)
            {
                sb.AppendLine("  [" + u.Clase + "] " + u.Texto);
            }
            sb.AppendLine("Raycasts del perimetro (cada 1 m, 4 alturas): " + r.Muestras + ", sin choque: " + r.RayosSinChoque);
            foreach (string s in r.SinChoque.Take(60))
            {
                sb.AppendLine("  sin choque: " + s);
            }
            return sb.ToString();
        }

        private static void Escribir(string texto)
        {
            Directory.CreateDirectory("Logs");
            File.WriteAllText(RutaLog, texto);
        }

        private static void Foto(string nombre, Vector3 pos, Vector3 mira, float fov)
        {
            const int Ancho = 1280;
            const int Alto = 720;
            GameObject go = new GameObject("CamFotosT1601");
            Camera cam = go.AddComponent<Camera>();
            cam.fieldOfView = fov;
            cam.farClipPlane = 1500f;
            go.transform.position = pos;
            go.transform.LookAt(mira);
            RenderTexture rt = new RenderTexture(Ancho, Alto, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(Ancho, Alto, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, Ancho, Alto), 0, 0);
            tex.Apply();
            File.WriteAllBytes(Path.Combine(CarpetaFotos, nombre + ".png"), tex.EncodeToPNG());
            RenderTexture.active = null;
            cam.targetTexture = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(go);
            Debug.Log(Etiqueta + "foto " + nombre);
        }
    }
}
