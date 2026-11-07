using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// Sesión 2 del 7-nov, paso 3 de docs/plan-look-ciudad.md: Nova (K06), lo mínimo para la demo. La Manzana_6 (sureste, sin lugares de la
    /// historia) se parte con una calle ancha de Nova sin tráfico en el eje de la calle de K01 (z = -43, de x = 54 a 134; 18 m de fachada a
    /// fachada con banquetas de 4 m), con el frente de Nova del kit a los dos lados (torres T20–T23, bolardos, cámaras, botes y luz fría). Se
    /// apagan los edificios greybox de la fila de en medio (54, 57 y 60) y los de la del norte (55, 58 y 61), que tapaban las torres. Idempotente: rehace su raíz S2c_Nova.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarS2cNova.Colocar
    /// </summary>
    public static class ColocarS2cNova
    {
        public const string Raiz = "S2c_Nova";
        public const int Semilla = 7550;
        public const float ZCentro = -43f;
        public const float MedioArroyo = 5f;
        public const float AnchoBanqueta = 4f;
        public const float X0 = 54f;
        public const float X1 = 134f;
        public const float XFachada0 = 57f;
        public const float XFachada1 = 131f;

        public static readonly string[] Edificios =
        {
            "Barrio/Manzana_6/Edificios/Edificio_54", "Barrio/Manzana_6/Edificios/Edificio_57", "Barrio/Manzana_6/Edificios/Edificio_60",
            "Barrio/Manzana_6/Edificios/Edificio_55", "Barrio/Manzana_6/Edificios/Edificio_58", "Barrio/Manzana_6/Edificios/Edificio_61",
        };

        [MenuItem("Mecha Cyberpunk/S2c/6 Nova (calle de K06 en la Manzana_6)")]
        public static void Colocar()
        {
            ColocarS2.Informe.Clear();
            ColocarS2.Abrir();
            List<Bounds> huellas = Edificios.Select(ColocarS2.Buscar).Where(t => t != null).Select(t => Huella(t.gameObject)).ToList();
            int n = ColocarS2cCalle.ApagarEdificios(Edificios);
            KitS2c.Obra o = KitS2c.Empezar(Raiz, Semilla);
            float arroyo = KitS2c.Suelo(new Vector3(94f, 0f, ZCentro), 0.2f) + 0.02f;
            float zNorte = ZCentro + MedioArroyo + AnchoBanqueta, zSur = ZCentro - MedioArroyo - AnchoBanqueta;
            KitS2c.Losa(o, o.Raiz, "Arroyo", new Vector3((X0 + X1) * 0.5f, arroyo - 0.05f, ZCentro), Quaternion.identity, new Vector3(X1 - X0, 0.1f, 2f * MedioArroyo), KitS2c.MatAsfalto(), 8f, true);
            for (float x = X0 + 1f; x < X1 - 2f; x += 5f)
            {
                KitS2c.Linea(o.Raiz, new Vector3(x, arroyo, ZCentro), new Vector3(x + 2f, arroyo, ZCentro), 0.14f, arroyo);
            }
            // Norte: fachadas en z = zNorte mirando al sur; sur: en z = zSur mirando al norte (FrenteNova pone la banqueta del lado de la calle).
            ColocarS2cKit.FrenteNova(o, o.Raiz, new Vector3(XFachada0, 0f, zNorte), Vector3.right, XFachada1 - XFachada0, arroyo);
            ColocarS2cKit.FrenteNova(o, o.Raiz, new Vector3(XFachada1, 0f, zSur), Vector3.left, XFachada1 - XFachada0, arroyo);
            for (int i = 0; i < 18; i++)
            {
                KitS2c.Charco(o, o.Raiz, new Vector3(o.Rango(X0 + 2f, X1 - 2f), arroyo, ZCentro + o.Rango(-4f, 4f)), o.Rango(1.4f, 3.6f), o.Rango(0.9f, 2f), o.Rango(0f, 180f));
            }
            // Cruce pintado en la calle de x = 141 (con tráfico), frente a la calle de Nova.
            float pisoCalle = KitS2c.Suelo(new Vector3(141f, 0f, ZCentro + 9f), 0f);
            for (float x = 141f - 6.3f; x <= 141f + 6.3f; x += 1.05f)
            {
                KitS2c.Linea(o.Raiz, new Vector3(x, pisoCalle, ZCentro - 1.5f), new Vector3(x, pisoCalle, ZCentro + 1.5f), 0.5f, pisoCalle);
            }
            huellas.Add(new Bounds(new Vector3((X0 + X1) * 0.5f, 20f, ZCentro), new Vector3(X1 - X0, 40f, zNorte - zSur)));
            n += ColocarS2cCalle.ApagarEn(huellas.Select(h => { Bounds b = h; b.Expand(new Vector3(4f, 0f, 4f)); return b; }), false);
            KitS2c.Terminar(o);
            ColocarS2.Informe.AppendLine("Nova: " + n + " objetos apagados, " + o.Postes + " postes, " + o.Charcos + " charcos");
            ColocarS2.Guardar("s2c-nova");
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

        [MenuItem("Mecha Cyberpunk/S2c/6b Fotos de Nova")]
        public static void Fotos()
        {
            ColocarS2.Abrir();
            ColocarS2cLook.Foto(new Vector3(58f, 2.6f, ZCentro - 0.5f), new Vector3(130f, 5f, ZCentro), "docs/capturas/s2c-nova-calle.png", 66f);
            ColocarS2.LadoALado("imageassets/referencias/look/K06_CalleNova.png", "docs/capturas/s2c-nova-calle.png", "docs/capturas/s2c-nova-k06.png");
            EditorSceneManager.OpenScene(ColocarS2.RutaEscena, OpenSceneMode.Single);
        }

        public static void ColocarYFotos()
        {
            Colocar();
            Fotos();
        }
    }
}
