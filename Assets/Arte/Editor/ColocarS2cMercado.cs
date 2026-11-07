using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// Sesión 2 del 7-nov, paso 3 de docs/plan-look-ciudad.md: El Mercado (K02). El tianguis de la avenida (z = 0, de x = -37 a 37) queda entre
    /// casas de 2 pisos con locales del kit en lugar de los edificios greybox altos (Manzana_2: 10 y 15; Manzana_5: 47 y 52), con sus esquinas
    /// sobre la calle de la casa (x = -47) y la de x = 47. Se quedan el clandestino, el bar, el callejón del mercado de armas y los puestos.
    /// Sin focos colgando (T-803 ya los quitó; aquí no se pone ninguno). Idempotente: rehace su raíz S2c_Mercado.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarS2cMercado.Colocar
    /// </summary>
    public static class ColocarS2cMercado
    {
        public const string Raiz = "S2c_Mercado";
        public const int Semilla = 7330;
        public const float ZNorte = 10f;
        public const float ZSur = -10f;
        public const float Lote = 6f;
        public const float Fondo = 8f;
        public const float Piso = 0.2f;

        public static readonly string[] Edificios =
        {
            "Barrio/Manzana_2/Edificios/Edificio_10", "Barrio/Manzana_2/Edificios/Edificio_15",
            "Barrio/Manzana_5/Edificios/Edificio_47", "Barrio/Manzana_5/Edificios/Edificio_52",
        };

        public static readonly (string nombre, Vector3 desde, Vector3 hacia)[] Tomas =
        {
            ("tianguis", new Vector3(-36f, 2.6f, -2f), new Vector3(20f, 1.2f, 0f)),
            ("tianguis-oriente", new Vector3(36f, 2.6f, 2f), new Vector3(-20f, 1.2f, 0f)),
            ("alto", new Vector3(0f, 45f, -55f), new Vector3(0f, 0f, 0f)),
        };

        [MenuItem("Mecha Cyberpunk/S2c/4 El Mercado (tianguis entre casas del kit)")]
        public static void Colocar()
        {
            ColocarS2.Informe.Clear();
            ColocarS2.Abrir();
            List<Bounds> huellas = Edificios.Select(ColocarS2.Buscar).Where(t => t != null).Select(t => Huella(t.gameObject)).ToList();
            int n = ColocarS2cCalle.ApagarEdificios(Edificios);
            KitS2c.Obra o = KitS2c.Empezar(Raiz, Semilla);
            Altos.Clear();
            Transform casas = new GameObject("Casas").transform;
            casas.SetParent(o.Raiz, false);
            // Lado norte (Manzana_2, mira al sur): edificio 10 con esquina a la calle de la casa, y edificio 15 junto al callejón de armas.
            Esquina(o, casas, new Vector3(-37f, Piso, ZNorte), Vector3.back, Vector3.left, Lote, Lote);
            Frente(o, casas, "E", -31f, ZNorte, -25f, ZNorte, Vector3.back, 10f);
            Frente(o, casas, "B", -19f, ZNorte, -12.8f, ZNorte, Vector3.back, Fondo);
            Frente(o, casas, "C", -37f, ZNorte + Lote, -37f, 20f, Vector3.left, Fondo);
            // Lado sur (Manzana_5, mira al norte): edificios 47 y 52 con esquinas a las dos calles; el bar queda en medio.
            Esquina(o, casas, new Vector3(-37f, Piso, ZSur), Vector3.forward, Vector3.left, Lote, Lote);
            Frente(o, casas, "B", -31f, ZSur, -12.8f, ZSur, Vector3.forward, Fondo);
            Frente(o, casas, "E", 12.8f, ZSur, 31f, ZSur, Vector3.forward, Fondo);
            Esquina(o, casas, new Vector3(37f, Piso, ZSur), Vector3.forward, Vector3.right, Lote, Lote);
            Frente(o, casas, "C", -37f, ZSur - Lote, -37f, -35f, Vector3.left, Fondo);
            Frente(o, casas, "F", 37f, -35f, 37f, ZSur - Lote, Vector3.right, Fondo);
            // Cables que cruzan el tianguis de casa a casa y charcos en el arroyo de la avenida (como K02).
            List<Vector3> norte = Altos.Where(a => a.z > 0f).ToList(), sur = Altos.Where(a => a.z < 0f).ToList();
            foreach (Vector3 a in norte)
            {
                foreach (Vector3 b in sur.OrderBy(b => Mathf.Abs(b.x - a.x)).Take(2))
                {
                    KitS2c.Cable(o, a, b, o.Rango(1f, 1.8f));
                }
            }
            const float arroyo = 0f;   // arroyo de la avenida del barrio (Barrio/Calles)
            for (int i = 0; i < 16; i++)
            {
                KitS2c.Charco(o, o.Raiz, new Vector3(o.Rango(-36f, 36f), arroyo, o.Rango(-5.5f, 5.5f)), o.Rango(1.2f, 3.4f), o.Rango(0.8f, 2f), o.Rango(0f, 180f));
            }
            n += ColocarS2cCalle.ApagarEn(huellas.Select(h => { Bounds b = h; b.Expand(new Vector3(3f, 0f, 3f)); return b; }), false);
            KitS2c.Terminar(o);
            ColocarS2.Informe.AppendLine("Mercado: " + n + " objetos apagados, " + o.Casas + " casas, " + o.Neones + " neones");
            ColocarS2.Guardar("s2c-mercado");
        }

        private static readonly List<Vector3> Altos = new List<Vector3>();

        private static void Frente(KitS2c.Obra o, Transform padre, string receta, float x0, float z0, float x1, float z1, Vector3 normal, float fondo)
        {
            Altos.AddRange(KitS2c.Frente(o, padre, new Vector3(x0, Piso, z0), new Vector3(x1, Piso, z1), normal, fondo, i => ColocarS2cKit.Receta(receta, o, i)));
        }

        private static void Esquina(KitS2c.Obra o, Transform padre, Vector3 punto, Vector3 a, Vector3 b, float anchoA, float anchoB)
        {
            KitS2c.Casa(o, padre, punto - b * (anchoA * 0.5f), a, anchoA, anchoB, ColocarS2cKit.Receta("B", o, 0));
            KitS2c.Casa(o, padre, punto - a * (anchoB * 0.5f), b, anchoB, anchoA, ColocarS2cKit.Receta("C", o, 1));
        }

        private static Bounds Huella(GameObject g)
        {
            Renderer[] rs = g.GetComponentsInChildren<Renderer>(true);
            Bounds b = rs.Length > 0 ? rs[0].bounds : new Bounds(g.transform.position, Vector3.one);
            foreach (Renderer r in rs)
            {
                b.Encapsulate(r.bounds);
            }
            b.center = new Vector3(b.center.x, 20f, b.center.z);
            b.size = new Vector3(b.size.x, 40f, b.size.z);
            return b;
        }

        [MenuItem("Mecha Cyberpunk/S2c/4b Fotos del Mercado")]
        public static void Fotos()
        {
            ColocarS2.Abrir();
            foreach ((string nombre, Vector3 desde, Vector3 hacia) in Tomas)
            {
                ColocarS2cLook.Foto(desde, hacia, "docs/capturas/s2c-mercado-" + nombre + ".png", nombre == "alto" ? 60f : 66f);
            }
            ColocarS2.LadoALado("imageassets/referencias/look/K02_Tianguis.png", "docs/capturas/s2c-mercado-tianguis.png", "docs/capturas/s2c-mercado-k02.png");
            EditorSceneManager.OpenScene(ColocarS2.RutaEscena, OpenSceneMode.Single);
        }

        public static void ColocarYFotos()
        {
            Colocar();
            Fotos();
        }
    }
}
