using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-886 (playtest 7-nov, sesión 2): el cuarto del jugador más grande, como R10 (imageassets/referencias/look/R10_Cuarto.png). Pasa de 3 x 4
    /// a 5 x 4 m: la pared izquierda (poniente) se recorre 2 m dentro del volumen de la casa, que ahí está vacío. Piso, techo, pared del fondo
    /// y pared de la puerta se rehacen a la nueva medida con sus mismos materiales; la ZonaInterior crece. Los muebles que ya tenía (todos de
    /// Tony: cama H02, buró H11, ventana con barrotes I04, escritorio con CRT I02 y su silla I03, ropero I01, mochila I05, pósters) se
    /// acomodan como en R10: cama a la izquierda con el buró y su lámpara, la ventana con el neón al centro del fondo, el escritorio a su
    /// derecha, el ropero a la derecha junto a la puerta, el foco pelón al centro con sus cables. El espejo (S2t_Espejo) no se toca.
    /// Idempotente: mueve a posiciones fijas y rehace las paredes.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarS2oCuarto.Colocar
    /// </summary>
    public static class ColocarS2oCuarto
    {
        public const string RutaCuarto = "T550_Cuartos/Cuarto_Habitacion";
        public const float X0 = -66.5f, X1 = -61.5f, Z0 = -75.5f, Z1 = -71.5f, Alto = 2.7f;
        public const float VanoX0 = -62.5f;
        public static float Ancho => X1 - X0;
        public static float Fondo => Z1 - Z0;

        /// <summary>Dónde va cada cosa (centro en planta; y = lo que diga su colocación original).</summary>
        public static readonly (string nombre, float x, float z)[] Muebles =
        {
            ("H02_Cama", -65.65f, -72.45f),
            ("H11_Buro", -64.6f, -71.8f),
            ("I04", -63.55f, -71.75f),
            ("I02", -62.05f, -72.2f),
            ("I03", -62.75f, -72.35f),
            ("I01", -61.89f, -73.6f),   // no más al sur: tapa la entrada desde la puerta (RecorridoPrologoCiudadPruebas, punto 12c)
            ("I05", -66.05f, -74.7f),   // recargada en la pared izquierda, junto a la puerta (LimpiezaS2nPruebas)
        };

        [MenuItem("Mecha Cyberpunk/S2o/7 Cuarto como R10")]
        public static void Colocar()
        {
            ColocarS2.Informe.Clear();
            ColocarS2.Abrir();
            Transform r = ColocarS2.Buscar(RutaCuarto);
            if (r == null)
            {
                ColocarS2.Informe.AppendLine("FALTA " + RutaCuarto);
                return;
            }
            Paredes(r);
            Zona(r);
            Acomodar(r);
            Techo(r);
            ColocarS2.Informe.AppendLine("Cuarto: " + Ancho + " x " + Fondo + " m (antes 3 x 4), muebles como R10");
            ColocarS2.Guardar("s2o-cuarto");
        }

        private static void Paredes(Transform r)
        {
            float y0 = r.position.y, y1 = y0 + Alto;
            Material pared = r.Find("Pared_Fondo").GetComponent<Renderer>().sharedMaterial;
            Material piso = r.Find("Piso").GetComponent<Renderer>().sharedMaterial;
            Material techo = r.Find("Techo").GetComponent<Renderer>().sharedMaterial;
            float alturaPiso = r.Find("Piso").localPosition.y;
            foreach (string n in new[] { "Piso", "Techo", "Pared_Fondo", "Pared_Izquierda", "Pared_Puerta_Izq" })
            {
                Transform viejo = r.Find(n);
                if (viejo != null)
                {
                    Object.DestroyImmediate(viejo.gameObject);
                }
            }
            Vector3 v(float x, float y, float z) => new Vector3(x, y, z);
            ColocarT550.Superficie(r, "Piso", piso, v(X0, y0, Z0), v(X0, y0, Z1), v(X1, y0, Z1), v(X1, y0, Z0), Vector3.up, -0.1f);
            r.Find("Piso").localPosition = new Vector3(0f, alturaPiso, 0f);
            ColocarT550.Superficie(r, "Techo", techo, v(X0, y1, Z0), v(X1, y1, Z0), v(X1, y1, Z1), v(X0, y1, Z1), Vector3.down, 0.1f);
            ColocarT550.Superficie(r, "Pared_Fondo", pared, v(X0, y0, Z1), v(X0, y1, Z1), v(X1, y1, Z1), v(X1, y0, Z1), Vector3.back, 0.1f);
            ColocarT550.Superficie(r, "Pared_Izquierda", pared, v(X0, y0, Z0), v(X0, y1, Z0), v(X0, y1, Z1), v(X0, y0, Z1), Vector3.right, -0.1f);
            ColocarT550.Superficie(r, "Pared_Puerta_Izq", pared, v(VanoX0, y0, Z0), v(VanoX0, y1, Z0), v(X0, y1, Z0), v(X0, y0, Z0), Vector3.forward, -0.1f);
            // Las luces del cuarto solo alumbran su capa de render (la del interior): las superficies nuevas llevan la de la pared que no cambia.
            uint capa = r.Find("Pared_Derecha").GetComponent<Renderer>().renderingLayerMask;
            foreach (string n in new[] { "Piso", "Techo", "Pared_Fondo", "Pared_Izquierda", "Pared_Puerta_Izq" })
            {
                r.Find(n).GetComponent<Renderer>().renderingLayerMask = capa;
            }
        }

        private static void Zona(Transform r)
        {
            Transform zona = r.Find("ZonaInterior");
            BoxCollider caja = zona != null ? zona.GetComponent<BoxCollider>() : null;
            if (caja == null)
            {
                return;
            }
            // La zona cubre cuarto y pasillo: su orilla poniente pasa a la pared nueva, la oriente se queda donde estaba.
            Bounds b = new Bounds(zona.TransformPoint(caja.center), Vector3.Scale(caja.size, zona.lossyScale));
            float este = b.max.x;
            float oeste = X0 + 0.1f;
            zona.position = new Vector3((este + oeste) * 0.5f, zona.position.y, zona.position.z);
            caja.center = Vector3.zero;
            caja.size = new Vector3(este - oeste, caja.size.y, caja.size.z);
        }

        private static void Acomodar(Transform r)
        {
            foreach ((string nombre, float x, float z) in Muebles)
            {
                Transform t = r.Find(nombre);
                if (t == null)
                {
                    ColocarS2.Informe.AppendLine("  FALTA " + nombre);
                    continue;
                }
                Vector3 antes = Centro(t);
                Vector3 delta = new Vector3(x - antes.x, 0f, z - antes.z);
                t.position += delta;
                // Lo que acompaña al mueble se va con él: la lámpara del buró, el brillo del CRT, el neón tras la ventana.
                foreach ((string acompana, string de) in new[] { ("Lampara_Buro_Pantalla", "H11_Buro"), ("Luces/Lampara_Buro", "H11_Buro"), ("Luces/BrilloCRT", "I02"),
                             ("Luces/NeonVentana_Magenta", "I04"), ("Luces/NeonVentana_Turquesa", "I04"), ("S2_Cuarto/Barra_Cortina", "I04") })
                {
                    if (de == nombre && r.Find(acompana) != null)
                    {
                        r.Find(acompana).position += delta;
                    }
                }
            }
            // Pósters: los de la pared izquierda pasan a la pared nueva; los del fondo, sobre la cama.
            foreach (Transform p in r.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("Poster")).ToArray())
            {
                Vector3 c = p.position;
                if (Mathf.Abs(c.z - Z1) < 0.1f)
                {
                    p.position += new Vector3((p.name == "Poster_3" ? -65.05f : -65.95f) - c.x, 0f, 0f);
                }
                else if (c.x < -64f && c.x > -64.6f)
                {
                    p.position += new Vector3(X0 + 0.01f - c.x, 0f, 0f);
                }
            }
            foreach (Transform t in r.Cast<Transform>().Where(t => t.name == "Tuberia" && t.position.x < -64f && t.position.x > -64.7f).ToArray())
            {
                t.position += new Vector3(X0 + 0.04f - t.position.x, 0f, 0f);
            }
        }

        private static Vector3 Centro(Transform t)
        {
            Renderer[] rs = t.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0)
            {
                return t.position;
            }
            Bounds b = rs[0].bounds;
            foreach (Renderer x in rs)
            {
                b.Encapsulate(x.bounds);
            }
            return b.center;
        }

        // El foco pelón al centro del cuarto, con su luz, y los cables que salen de él a las esquinas del techo (como R10).
        private static void Techo(Transform r)
        {
            Vector3 centro = new Vector3((X0 + X1) * 0.5f, 0f, (Z0 + Z1) * 0.5f + 0.25f);
            float techo = r.position.y + Alto;
            Transform foco = r.Find("S2_Cuarto/Foco_Pelon");
            if (foco != null)
            {
                foco.position = new Vector3(centro.x, foco.position.y, centro.z);
            }
            Transform luz = r.Find("Luces/FocoColgante_Luz");
            if (luz != null)
            {
                luz.position = new Vector3(centro.x, luz.position.y, centro.z);
                Light l = luz.GetComponent<Light>();
                l.range = Mathf.Max(l.range, 7f);
            }
            Transform relleno = r.Find("Luces/Relleno_Calido");
            if (relleno != null)
            {
                relleno.position = new Vector3(centro.x, relleno.position.y, relleno.position.z);
            }
            List<LineRenderer> cables = r.GetComponentsInChildren<LineRenderer>(true).Where(c => c.name == "Cable").ToList();
            Vector3 arriba = new Vector3(centro.x, techo - 0.02f, centro.z);
            Vector3[] puntas = { new Vector3(X0 + 0.15f, techo - 0.05f, Z1 - 0.15f), new Vector3(X1 - 0.15f, techo - 0.05f, Z1 - 0.15f),
                new Vector3(X0 + 0.15f, techo - 0.05f, Z0 + 0.4f), new Vector3(X1 - 0.15f, techo - 0.05f, (Z0 + Z1) * 0.5f) };
            for (int i = 0; i < cables.Count; i++)
            {
                LineRenderer c = cables[i];
                c.useWorldSpace = true;
                Vector3 a = i == 0 && foco != null ? foco.position + Vector3.up * 0.06f : arriba;
                Vector3 b = puntas[i % puntas.Length];
                const int n = 10;
                c.positionCount = n;
                for (int k = 0; k < n; k++)
                {
                    float t = k / (n - 1f);
                    c.SetPosition(k, Vector3.Lerp(a, b, t) + Vector3.down * (Mathf.Sin(t * Mathf.PI) * 0.22f));
                }
                EditorUtility.SetDirty(c);
            }
        }

        public static void Fotos(string cuando)
        {
            // Como R10: desde la puerta, a la altura de los ojos, mirando al fondo.
            ColocarS2cLook.Foto(new Vector3(-62.1f, 1.7f, -75.2f), new Vector3(-64f, 1.3f, -71.5f), "docs/capturas/s2o-cuarto-" + cuando + ".png", 70f, 23f);
        }
    }
}
