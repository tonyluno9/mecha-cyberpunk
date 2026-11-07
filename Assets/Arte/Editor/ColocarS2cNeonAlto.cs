using System.Collections.Generic;
using System.Linq;
using MechaCyberpunk.Mundo.Ambiente;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// Sesión 2 del 7-nov, paso 3 de docs/plan-look-ciudad.md: Neón Alto (K03). La calle del casino VivaCoin (x = 47, de z = 10 a 86) se viste
    /// como K03: letreros de neón chicos y verticales en las fachadas de los dos lados (magenta y turquesa, con su luz), cables que cruzan de
    /// fachada a fachada y charcos que reflejan el neón. Las fachadas se encuentran con rayos desde el eje de la calle, así que no se toca ningún
    /// edificio. Idempotente: rehace su raíz S2c_NeonAlto.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarS2cNeonAlto.Colocar
    /// </summary>
    public static class ColocarS2cNeonAlto
    {
        public const string Raiz = "S2c_NeonAlto";
        public const int Semilla = 7440;
        public const float XCalle = 47f;
        public const float Z0 = 12f;
        public const float Z1 = 85f;
        public const float MedioArroyo = 6.5f;

        public static readonly (string nombre, Vector3 desde, Vector3 hacia)[] Tomas =
        {
            ("calle", new Vector3(45.5f, 2.6f, 6f), new Vector3(49f, 4f, 60f)),
            ("casino", new Vector3(44f, 2.6f, 29f), new Vector3(58f, 5f, 46f)),
        };

        [MenuItem("Mecha Cyberpunk/S2c/5 Neón Alto (neones, cables y charcos de K03)")]
        public static void Colocar()
        {
            ColocarS2.Informe.Clear();
            ColocarS2.Abrir();
            KitS2c.Obra o = KitS2c.Empezar(Raiz, Semilla);
            Physics.SyncTransforms();
            List<(Vector3 punto, Vector3 normal)> poniente = new List<(Vector3, Vector3)>(), oriente = new List<(Vector3, Vector3)>();
            for (float z = Z0; z < Z1; z += o.Rango(3.2f, 5f))
            {
                foreach (float s in new[] { -1f, 1f })
                {
                    if (!o.Chance(0.75f))
                    {
                        continue;
                    }
                    float y = o.Rango(4.2f, 13f);
                    if (Pared(new Vector3(XCalle, y, z), new Vector3(s, 0f, 0f), out RaycastHit h))
                    {
                        (s < 0f ? poniente : oriente).Add((h.point, h.normal));
                        Color c = o.Chance(0.6f) ? KitS2c.Magenta : KitS2c.Turquesa;
                        bool bandera = o.Chance(0.55f);
                        Vector3 lado = Vector3.Cross(Vector3.up, h.normal).normalized;
                        KitS2c.Neon(o, o.Raiz, h.point, h.normal, lado, c, bandera);
                        // Los de bandera de Neón Alto son altos (verticales de 2 a 3 veces), como en K03.
                        Transform neon = o.Raiz.GetChild(o.Raiz.childCount - 1);
                        if (bandera && o.Chance(0.7f))
                        {
                            neon.localScale = new Vector3(1f, o.Rango(2f, 3.2f), 1f);
                        }
                    }
                }
            }
            // Cables de fachada a fachada (entre 7 y 11 m), como los mazos que cruzan en K03.
            foreach ((Vector3 p, Vector3 _) in poniente.Where((x, i) => i % 2 == 0))
            {
                Vector3 b = oriente.OrderBy(x => Mathf.Abs(x.punto.z - p.z - o.Rango(-4f, 4f))).FirstOrDefault().punto;
                if (b != Vector3.zero)
                {
                    float ya = o.Rango(7f, 11f), yb = o.Rango(7f, 11f);
                    KitS2c.Cable(o, new Vector3(p.x, ya, p.z), new Vector3(b.x, yb, b.z), o.Rango(1f, 1.8f));
                    KitS2c.Cable(o, new Vector3(p.x, ya - 0.3f, p.z), new Vector3(b.x, yb - 0.35f, b.z), o.Rango(1.2f, 2f));
                }
            }
            for (int i = 0; i < 22; i++)
            {
                KitS2c.Charco(o, o.Raiz, new Vector3(XCalle + o.Rango(-MedioArroyo + 0.8f, MedioArroyo - 0.8f), 0f, o.Rango(Z0 - 8f, Z1)), o.Rango(1.4f, 3.6f), o.Rango(0.9f, 2.2f), o.Rango(0f, 180f));
            }
            int luces = Recolorear(o);
            KitS2c.Terminar(o);
            ColocarS2.Informe.AppendLine("Luces verdes que pasan a magenta o turquesa: " + luces);
            ColocarS2.Informe.AppendLine("Neón Alto: " + o.Neones + " neones, " + o.Cables + " cables, " + o.Charcos + " charcos");
            ColocarS2.Guardar("s2c-neon-alto");
        }

        /// <summary>Caja de Neón Alto (Manzana_3 y la calle del casino).</summary>
        public static readonly Bounds Zona = new Bounds(new Vector3(88f, 20f, 47f), new Vector3(104f, 60f, 84f));

        /// <summary>Las luces verdes del barrio (T-507 lo pintó verde) pasan a magenta o turquesa, como K03 y M11 (Neón Alto es magenta).</summary>
        private static int Recolorear(KitS2c.Obra o)
        {
            int n = 0;
            foreach (Light l in Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                         .Where(l => l.type != LightType.Directional && Zona.Contains(l.transform.position)))
            {
                Color c = l.color;
                if (c.g > c.r + 0.15f && c.g > c.b + 0.2f)
                {
                    l.color = n % 2 == 0 ? KitS2c.Magenta : KitS2c.Turquesa;
                    EditorUtility.SetDirty(l);
                    n++;
                }
            }
            return n;
        }

        // La fachada más cercana en esa dirección (sin contar lo de S2c ni triggers), a no más de 16 m del eje.
        private static bool Pared(Vector3 desde, Vector3 dir, out RaycastHit golpe)
        {
            golpe = default;
            RaycastHit[] hs = Physics.RaycastAll(desde, dir, 16f, ~(1 << ReflejoCharcos.CapaCharcos), QueryTriggerInteraction.Ignore)
                .Where(h => !h.collider.transform.root.name.StartsWith("S2c_") && Mathf.Abs(h.normal.y) < 0.3f && Vector3.Dot(h.normal, -dir) > 0.7f)
                .OrderBy(h => h.distance).ToArray();
            if (hs.Length == 0 || hs[0].distance < MedioArroyo)
            {
                return false;
            }
            golpe = hs[0];
            return true;
        }

        [MenuItem("Mecha Cyberpunk/S2c/5b Fotos de Neón Alto")]
        public static void Fotos()
        {
            ColocarS2.Abrir();
            foreach ((string nombre, Vector3 desde, Vector3 hacia) in Tomas)
            {
                ColocarS2cLook.Foto(desde, hacia, "docs/capturas/s2c-neon-alto-" + nombre + ".png", 66f);
            }
            ColocarS2.LadoALado("imageassets/referencias/look/K03_NeonAlto.png", "docs/capturas/s2c-neon-alto-casino.png", "docs/capturas/s2c-neon-alto-k03.png");
            EditorSceneManager.OpenScene(ColocarS2.RutaEscena, OpenSceneMode.Single);
        }

        public static void ColocarYFotos()
        {
            ColocarS2cLook.Aplicar();
            Colocar();
            Fotos();
        }
    }
}
