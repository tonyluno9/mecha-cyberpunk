using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using T = MechaCyberpunk.Arte.Editor.ColocarT1356Trazado;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1402 (Autopista elevada, ronda 3): corre DESPUÉS de ZonaAutopistaT1395.Aplicar. Edita el prefab T1356_Autopista (no la escena) y pone lo nuevo bajo el hijo raíz
    /// "T1402_Autopista", que Aplicar borra y rehace. Busca la composición de K18 con la cámara BAJO el tablero: una columna gruesa del tablero principal (z = 250) en primer plano
    /// al centro-derecha, la rampa norte (z = 232) subiendo a la izquierda hacia el túnel, y la torre de reflectores dentro del cuadro, a la izquierda, sobre el cerro.
    /// Qué hace: (1) calcula la columna y la cámara (EncuadreK18); (2) mueve la torre de reflectores (y sus haces) dentro del cuadro, más cerca y a escala, sin tocar el cerro,
    /// los tableros ni nada; (3) baja las torres del fondo que tapaban el cielo a la derecha; (4) pone chatarra AU03 y charcos bajo el tablero en primer plano.
    /// Métodos: Aplicar (Logs/t1402-autopista-aplicar.txt), Validar (Logs/t1402-autopista-validar.txt), Fotos (corrida aparte, docs/capturas/t-1402/).
    /// </summary>
    public static class ZonaAutopistaT1402
    {
        public const string RutaPrefab = "Assets/Arte/Mundo/Zonas/T1356_Autopista.prefab";
        public const string NombreRaiz = "T1402_Autopista";
        public const string Raiz1395 = "T1395_Autopista";

        /// <summary>Variante de AU03 para el primer plano (la _a trae carros chocados, como K18) y la alterna.</summary>
        private const string Variante = "_a";
        private const string VarianteAlterna = "_b";
        private const float AlturaCamara = 1.8f;
        private const float DistanciaXCamara = 22f;
        private const float ZCamara = 241f;
        private const float GiroMiraNorte = 8f;
        private const float YCharco = 0.04f;
        private const int MaxObjetosNuevos = 300;
        private const int MaxChatarra = 6;
        private static readonly StringBuilder informe = new StringBuilder();

        // ---------------------------------------------------------------- Encuadre

        /// <summary>Columna del tablero principal cerca de x = -230, cámara bajo el tablero junto a ella y punto de mira (a lo largo de la rampa norte hacia el oeste).</summary>
        public static bool EncuadreK18(float dx, out Vector3 columna, out Vector3 camara, out Vector3 mira, out Vector3 dir)
        {
            columna = Vector3.zero;
            camara = Vector3.zero;
            mira = Vector3.zero;
            dir = Vector3.left;
            List<Vector3> cols = T.Columnas(T.Principal()).Where(c => Mathf.Abs(c.z - T.ZTablero) < 1f && c.x > -280f && c.x < -170f).OrderBy(c => Mathf.Abs(c.x + 230f)).ToList();
            if (cols.Count == 0)
            {
                return false;
            }
            columna = cols[0];
            camara = new Vector3(columna.x + dx, AlturaCamara, ZCamara);
            dir = Quaternion.AngleAxis(GiroMiraNorte, Vector3.up) * Vector3.left;
            mira = camara + dir * 80f;
            mira.y = 6f;
            return true;
        }

        // ---------------------------------------------------------------- Utilidades

        private static Bounds? Caja(GameObject go, bool soloVisible)
        {
            Bounds? caja = null;
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
            {
                if (soloVisible && (!r.enabled || r.name.StartsWith("Halo")))
                {
                    continue;
                }
                if (caja.HasValue)
                {
                    Bounds w = caja.Value;
                    w.Encapsulate(r.bounds);
                    caja = w;
                }
                else
                {
                    caja = r.bounds;
                }
            }
            return caja;
        }

        /// <summary>Cajas de todo lo visible del prefab (menos lo de T1402, pisos grandes, fondo, cerro, halos, cables, charcos y líneas).</summary>
        private static List<Bounds> Ocupados(Transform raiz, Transform mia)
        {
            Transform acantilado = raiz.Find("Acantilado");
            Transform fondo = raiz.Find("Fondo");
            List<Bounds> lista = new List<Bounds>();
            foreach (Renderer r in raiz.GetComponentsInChildren<Renderer>(false))
            {
                if (!r.enabled || !r.gameObject.activeInHierarchy || (mia != null && r.transform.IsChildOf(mia)))
                {
                    continue;
                }
                if (r.name == "Cables" || r.name.StartsWith("Halo") || r.name.StartsWith("Haz_") || r.name.StartsWith("Charco") || r.name.StartsWith("Linea"))
                {
                    continue;
                }
                Bounds b = r.bounds;
                if (b.size.x > 60f || b.size.z > 60f || b.size.y < 0.4f || (acantilado != null && r.transform.IsChildOf(acantilado)) || (fondo != null && r.transform.IsChildOf(fondo)))
                {
                    continue;
                }
                lista.Add(b);
            }
            return lista;
        }

        private static float DistanciaPlanta(Vector3 p, IList<Vector3> linea)
        {
            float m = float.MaxValue;
            for (int i = 0; i + 1 < linea.Count; i++)
            {
                Vector2 a = new Vector2(linea[i].x, linea[i].z), b = new Vector2(linea[i + 1].x, linea[i + 1].z), q = new Vector2(p.x, p.z);
                Vector2 ab = b - a;
                float t = ab.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(q - a, ab) / ab.sqrMagnitude);
                m = Mathf.Min(m, (q - (a + ab * t)).magnitude);
            }
            return m;
        }

        // ---------------------------------------------------------------- Aplicar

        [MenuItem("Mecha Cyberpunk/T-1402/Autopista ronda 3 (aplicar)")]
        public static void Aplicar()
        {
            informe.Clear();
            informe.AppendLine("T-1402 Autopista (ronda 3)");
            GameObject contenido = PrefabUtility.LoadPrefabContents(RutaPrefab);
            try
            {
                Transform raiz = contenido.transform;
                if (raiz.Find(Raiz1395) == null)
                {
                    informe.AppendLine("FALTA " + Raiz1395 + ": corre primero ZonaAutopistaT1395.Aplicar. No se cambió nada.");
                }
                else if (!EncuadreK18(DistanciaXCamara, out Vector3 columna, out Vector3 camara, out Vector3 mira, out Vector3 dir))
                {
                    informe.AppendLine("FALTA una columna del tablero principal entre x -280 y -170: no se cambió nada.");
                }
                else
                {
                    int lucesAntes = contenido.GetComponentsInChildren<Light>(true).Length;
                    KitS2c.Obra o = KitS2c.Empezar(NombreRaiz, 1402, raiz);
                    informe.AppendLine("Columna del tablero en " + columna.ToString("F1") + ", cámara K18 en " + camara.ToString("F1") + ", mira hacia " + mira.ToString("F1"));
                    MoverTorre(raiz, camara, dir);
                    BajarTorresDelFondo(raiz);
                    Transform chatarra = new GameObject("Chatarra").transform;
                    chatarra.SetParent(o.Raiz, false);
                    Transform charcos = new GameObject("Charcos").transform;
                    charcos.SetParent(o.Raiz, false);
                    List<Bounds> ocupados = Ocupados(raiz, o.Raiz);
                    PonerChatarra(chatarra, columna, ocupados);
                    PonerCharcos(o, charcos, columna, ocupados);
                    KitS2c.Terminar(o);
                    int lucesDespues = contenido.GetComponentsInChildren<Light>(true).Length;
                    informe.AppendLine("Luces: antes " + lucesAntes + ", después " + lucesDespues + " (no se agregan). Objetos nuevos: " + o.Raiz.GetComponentsInChildren<Transform>(true).Length + " (tope " + MaxObjetosNuevos + ").");
                    PrefabUtility.SaveAsPrefabAsset(contenido, RutaPrefab);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contenido);
            }
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/t1402-autopista-aplicar.txt", informe.ToString());
            Debug.Log("[Arte] T-1402 autopista aplicar:\n" + informe);
        }

        // ---------------------------------------------------------------- Torre de reflectores

        /// <summary>
        /// La torre (T1386_Autopista/.../TorreReflectores en (-330, 0, 312), 168 m) queda fuera de cuadro desde la cámara bajo el tablero. Se busca, a la izquierda del eje de la mira
        /// (de -6 a -22 grados) y de 230 a 90 m, el sitio más lejano donde su caja (más margen) no choque con nada visible, con el cerro ni con los tableros (a menos de 14 m).
        /// Allí se pone con escala (0.6, h/168, 0.6) con h = 0.55 x distancia (su remate queda a ~29 grados sobre el horizonte, como K18) y sus tres haces se recolocan en la nueva punta.
        /// </summary>
        private static void MoverTorre(Transform raiz, Vector3 camara, Vector3 dir)
        {
            Transform torre = raiz.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "TorreReflectores");
            if (torre == null)
            {
                informe.AppendLine("No existe TorreReflectores en el prefab: no hay torre que mover.");
                return;
            }
            Transform pa = torre.parent;
            torre.localScale = Vector3.one;
            Bounds? original = Caja(torre.gameObject, false);
            float alto0 = original.HasValue ? original.Value.size.y : 168f;
            Transform acantilado = raiz.Find("Acantilado");
            List<Bounds> cerro = acantilado == null ? new List<Bounds>() : acantilado.GetComponentsInChildren<Renderer>(true).Select(r => r.bounds).ToList();
            List<Bounds> ocupados = Ocupados(raiz, raiz.Find(NombreRaiz));
            // El fondo, las otras torres de reflectores y la propia torre no cuentan como estorbo (la torre se mueve).
            List<Vector3> principal = T.Principal(), rampa = T.RampaNorte(), tunel = T.Tunel();
            Vector3? sitio = null;
            float distanciaElegida = 0f, giroElegido = 0f;
            for (float d = 230f; d >= 90f && !sitio.HasValue; d -= 10f)
            {
                foreach (float giro in new[] { -14f, -10f, -18f, -6f, -22f })
                {
                    Vector3 p = camara + Quaternion.AngleAxis(giro, Vector3.up) * dir * d;
                    p.y = 0f;
                    Bounds caja = new Bounds(new Vector3(p.x, 40f, p.z), new Vector3(20f, 80f, 20f));
                    bool choca = ocupados.Any(b => b.Intersects(caja)) || cerro.Any(b => b.Intersects(caja));
                    float lejos = Mathf.Min(DistanciaPlanta(p, principal), DistanciaPlanta(p, rampa), DistanciaPlanta(p, tunel));
                    if (!choca && lejos > 14f && p.z < 292f)
                    {
                        sitio = p;
                        distanciaElegida = d;
                        giroElegido = giro;
                        break;
                    }
                }
            }
            if (!sitio.HasValue)
            {
                informe.AppendLine("TORRE: no hay sitio libre dentro del cuadro entre 90 y 230 m; se deja donde estaba (-330, 0, 312). Habrá que revisarla en la foto.");
                return;
            }
            Vector3 destino = sitio.Value;
            Vector3 delta = destino - new Vector3(torre.position.x, 0f, torre.position.z);
            torre.position += new Vector3(delta.x, 0f, delta.z);
            float h = 0.55f * distanciaElegida;
            float sy = Mathf.Clamp(h / alto0, 0.2f, 1f);
            torre.localScale = new Vector3(0.6f, sy, 0.6f);
            Vector3 cima = torre.position + Vector3.up * (alto0 * sy);
            Vector3[] direcciones = { new Vector3(-0.35f, 1f, 0.05f), new Vector3(0.35f, 1f, 0.1f), new Vector3(0f, 1f, -0.4f) };
            const float largoHaz = 60f;
            int haces = 0;
            for (int i = 0; i < direcciones.Length; i++)
            {
                Transform haz = pa == null ? null : pa.Find("Haz_" + i);
                if (haz == null)
                {
                    continue;
                }
                Vector3 d = direcciones[i].normalized;
                haz.rotation = Quaternion.FromToRotation(Vector3.up, -d);
                haz.position = cima + d * largoHaz;
                haz.localScale = new Vector3(largoHaz * 0.2f, largoHaz, largoHaz * 0.2f);
                haces++;
            }
            informe.AppendLine("TORRE de reflectores movida de (-330, 0, 312) a " + destino.ToString("F1") + " (" + distanciaElegida.ToString("F0") + " m de la cámara, " + giroElegido.ToString("F0") + " grados a la izquierda de la mira), alto "
                + (alto0 * sy).ToString("F0") + " m (escala y " + sy.ToString("F2") + ", xz 0.6), " + haces + " haces recolocados. No choca con el cerro, los tableros ni lo visible del prefab.");
        }

        /// <summary>Las torres del fondo (Fondo/Torre_x) entre x -330 y -60 quedaban como un muro de neón a la derecha del cuadro: se bajan a 45% de altura (escala absoluta: se puede correr dos veces).</summary>
        private static void BajarTorresDelFondo(Transform raiz)
        {
            Transform fondo = raiz.Find("Fondo");
            if (fondo == null)
            {
                informe.AppendLine("Fondo: no existe el grupo Fondo; no se bajan torres.");
                return;
            }
            int bajadas = 0;
            foreach (Transform t in fondo)
            {
                if (t.name.StartsWith("Torre_") && t.position.x > -330f && t.position.x < -60f)
                {
                    Vector3 s = t.localScale;
                    t.localScale = new Vector3(s.x, 0.45f, s.z);
                    bajadas++;
                }
            }
            informe.AppendLine("Fondo: " + bajadas + " torres entre x -330 y -60 bajadas a 45% de altura para que el cielo y el cerro se vean a la derecha del cuadro.");
        }

        // ---------------------------------------------------------------- Chatarra y charcos bajo el tablero

        private static void PonerChatarra(Transform padre, Vector3 columna, List<Bounds> ocupados)
        {
            GameObject a = KitS2c.Prefab("AU03_ChatarraPuente" + Variante);
            GameObject b = KitS2c.Prefab("AU03_ChatarraPuente" + VarianteAlterna);
            if (a == null && b == null)
            {
                informe.AppendLine("FALTA AU03_ChatarraPuente: sin chatarra de primer plano.");
                return;
            }
            GameObject[] modelos = new[] { a, b }.Where(x => x != null).ToArray();
            // (dx, dz) respecto de la columna: primer plano a la derecha y al centro, junto al pie de la columna y detrás.
            (float dx, float dz)[] puntos = { (9f, -4f), (14f, -2.5f), (4.5f, -5.5f), (-5f, -4f), (17f, 3f), (-12f, 3.5f), (9f, 5f), (-2f, 6f) };
            int puestas = 0;
            foreach ((float dx, float dz) in puntos)
            {
                if (puestas >= MaxChatarra)
                {
                    break;
                }
                GameObject prefab = modelos[puestas % modelos.Length];
                GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
                go.name = "AU03_Primer_" + puestas;
                go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0f, (puestas * 67f) % 360f, 0f));
                go.transform.localScale = Vector3.one;
                Bounds? c0 = Caja(go, true);
                if (!c0.HasValue)
                {
                    Object.DestroyImmediate(go);
                    continue;
                }
                Vector3 meta = new Vector3(columna.x + dx, 0f, columna.z + dz);
                go.transform.position += new Vector3(meta.x - c0.Value.center.x, -c0.Value.min.y, meta.z - c0.Value.center.z);
                Bounds fin = Caja(go, true).Value;
                Bounds interior = fin;
                interior.Expand(-0.4f);
                if (ocupados.Any(x => x.Intersects(interior)) || fin.max.z > columna.z + 6.8f || fin.min.z < columna.z - 7f)
                {
                    informe.AppendLine("Chatarra en dx " + dx + ", dz " + dz + ": hay algo; se omite.");
                    Object.DestroyImmediate(go);
                    continue;
                }
                ocupados.Add(fin);
                puestas++;
            }
            informe.AppendLine("Chatarra AU03 de primer plano (variante " + Variante + " con la " + VarianteAlterna + " alterna): " + puestas + " montones junto al pie de la columna " + columna.ToString("F0") + ", base en y = 0.");
        }

        private static void PonerCharcos(KitS2c.Obra o, Transform padre, Vector3 columna, List<Bounds> ocupados)
        {
            int puestos = 0;
            float[] dxs = { 20f, 15f, 11f, 7f, 2f, -4f, -9f, -15f, -22f };
            float[] dzs = { -3.6f, 1.8f, -6f };
            foreach (float dx in dxs)
            {
                foreach (float dz in dzs)
                {
                    if (puestos >= 12)
                    {
                        continue;
                    }
                    float largo = o.Rango(2.2f, 3.8f), ancho = o.Rango(1.4f, 2.4f);
                    Vector3 c = new Vector3(columna.x + dx, YCharco, columna.z + dz);
                    Bounds caja = new Bounds(new Vector3(c.x, 0.3f, c.z), new Vector3(largo + 0.4f, 0.5f, largo + 0.4f));
                    if (ocupados.Any(b => b.Intersects(caja)))
                    {
                        continue;
                    }
                    KitS2c.Charco(o, padre, c, largo, ancho, o.Rango(0f, 180f));
                    puestos++;
                }
            }
            informe.AppendLine("Charcos bajo el tablero: " + puestos + " (a y = " + YCharco + ", sin chocar con columnas, chatarra ni nada visible).");
        }

        // ---------------------------------------------------------------- Validar

        [MenuItem("Mecha Cyberpunk/T-1402/Autopista ronda 3 (validar)")]
        public static void Validar()
        {
            StringBuilder s = new StringBuilder("T-1402 validar Autopista\n");
            int fallos = 0;
            GameObject contenido = PrefabUtility.LoadPrefabContents(RutaPrefab);
            try
            {
                Transform raiz = contenido.transform;
                Transform mia = raiz.Find(NombreRaiz);
                if (mia == null || !EncuadreK18(DistanciaXCamara, out Vector3 columna, out Vector3 camara, out _, out Vector3 dir))
                {
                    s.AppendLine("FALLO: no existe " + NombreRaiz + " o no hay columna para el encuadre.");
                    fallos++;
                }
                else
                {
                    int objetos = mia.GetComponentsInChildren<Transform>(true).Length;
                    s.AppendLine("Objetos bajo " + NombreRaiz + ": " + objetos + (objetos > MaxObjetosNuevos ? "  FALLO (tope " + MaxObjetosNuevos + ")" : ""));
                    fallos += objetos > MaxObjetosNuevos ? 1 : 0;
                    int luces = mia.GetComponentsInChildren<Light>(true).Length;
                    s.AppendLine("Luces nuevas (debe ser 0): " + luces);
                    fallos += luces;
                    Transform chatarra = mia.Find("Chatarra");
                    List<Bounds> ocupados = Ocupados(raiz, mia);
                    int montones = chatarra == null ? 0 : chatarra.childCount;
                    s.AppendLine("Chatarra AU03 de primer plano: " + montones);
                    if (montones < 2)
                    {
                        s.AppendLine("FALLO: casi no hay chatarra bajo el tablero.");
                        fallos++;
                    }
                    if (chatarra != null)
                    {
                        foreach (Transform m in chatarra)
                        {
                            Bounds? caja = Caja(m.gameObject, true);
                            if (!caja.HasValue)
                            {
                                continue;
                            }
                            if (Mathf.Abs(caja.Value.min.y) > 0.15f)
                            {
                                s.AppendLine("ALTURA " + m.name + ": base en y=" + caja.Value.min.y.ToString("F2"));
                                fallos++;
                            }
                            Bounds interior = caja.Value;
                            interior.Expand(-0.4f);
                            if (ocupados.Any(b => b.Intersects(interior)))
                            {
                                s.AppendLine("CHOQUE " + m.name + " con otra cosa.");
                                fallos++;
                            }
                        }
                    }
                    int charcos = mia.Find("Charcos")?.childCount ?? 0;
                    s.AppendLine("Charcos: " + charcos);
                    if (charcos < 3)
                    {
                        s.AppendLine("FALLO: pocos charcos");
                        fallos++;
                    }
                    Transform torre = raiz.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "TorreReflectores");
                    if (torre != null)
                    {
                        Vector3 v = torre.position - camara;
                        v.y = 0f;
                        float angulo = Vector3.Angle(v, dir);
                        s.AppendLine("Torre de reflectores en " + torre.position.ToString("F1") + " a " + v.magnitude.ToString("F0") + " m de la cámara y " + angulo.ToString("F1") + " grados del eje de la mira, escala " + torre.localScale.ToString("F2"));
                        if (angulo > 30f)
                        {
                            s.AppendLine("FALLO: la torre sigue fuera de cuadro.");
                            fallos++;
                        }
                        Bounds? bt = Caja(torre.gameObject, false);
                        Transform acantilado = raiz.Find("Acantilado");
                        if (bt.HasValue && acantilado != null)
                        {
                            Bounds huella = new Bounds(new Vector3(bt.Value.center.x, 40f, bt.Value.center.z), new Vector3(bt.Value.size.x, 80f, bt.Value.size.z));
                            if (acantilado.GetComponentsInChildren<Renderer>(true).Any(r => r.bounds.Intersects(huella)))
                            {
                                s.AppendLine("CHOQUE: la torre entra en el cerro.");
                                fallos++;
                            }
                        }
                        if (bt.HasValue)
                        {
                            Bounds huellaTorre = new Bounds(new Vector3(bt.Value.center.x, 40f, bt.Value.center.z), new Vector3(bt.Value.size.x, 80f, bt.Value.size.z));
                            Bounds? choca = ocupados.Where(b => b.Intersects(huellaTorre)).Select(b => (Bounds?)b).FirstOrDefault();
                            if (choca.HasValue)
                            {
                                s.AppendLine("CHOQUE: la torre se encima con algo visible en " + choca.Value.center.ToString("F0"));
                                fallos++;
                            }
                        }
                    }
                    else
                    {
                        s.AppendLine("AVISO: no hay TorreReflectores en el prefab.");
                    }
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contenido);
            }
            s.AppendLine(fallos == 0 ? "VALIDAR: sin fallos" : "VALIDAR: " + fallos + " fallos");
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/t1402-autopista-validar.txt", s.ToString());
            Debug.Log("[Arte] T-1402 autopista validar:\n" + s);
        }

        // ---------------------------------------------------------------- Fotos

        /// <summary>
        /// K18 desde bajo el tablero (junto a la columna, mirando al oeste con 8 grados al norte): la columna al centro-derecha, la rampa a la izquierda subiendo al túnel y la torre dentro del cuadro.
        /// Variantes: k18b más cerca de la columna, k18c más lejos y más ancha. A18: aéreas como en la ronda 2. No guarda nada; corrida de Unity aparte de Aplicar.
        /// </summary>
        [MenuItem("Mecha Cyberpunk/T-1402/Autopista ronda 3 (fotos)")]
        public static void Fotos()
        {
            ColocarS2.Abrir();
            Directory.CreateDirectory("docs/capturas/t-1402");
            Directory.CreateDirectory("Logs");
            foreach ((string nombre, float dx, float campo) in new[] { ("autopista-k18", DistanciaXCamara, 62f), ("autopista-k18b", 16f, 66f), ("autopista-k18c", 30f, 70f) })
            {
                if (EncuadreK18(dx, out _, out Vector3 camara, out Vector3 mira, out _))
                {
                    Tomar("K18_AutopistaElevada", nombre, camara, mira, campo);
                }
            }
            Tomar("A18_Autopista_Aerea", "autopista-a18", new Vector3(-205f, 46f, 150f), new Vector3(-335f, 12f, 238f), 62f);
        }

        private static void Tomar(string referencia, string nombre, Vector3 desde, Vector3 hacia, float campo)
        {
            string juego = "Logs/t1402-" + nombre + "-juego.png";
            ColocarS2cLook.Foto(desde, hacia, juego, campo);
            ColocarS2.LadoALado("imageassets/referencias/look/" + referencia + ".png", juego, "docs/capturas/t-1402/" + nombre + ".png");
        }
    }
}
