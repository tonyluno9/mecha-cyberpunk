using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// S2 zona 5 (docs/playtest-05nov.md): el callejón junto a la casa según docs/referencias-ciudad/callejon-noche.png. Sobre lo de T-250/T-502 (contenedor,
    /// llantas, tambos, tubos, ventilas, escalera, reja, vapor y charcos) agrega las puertas traseras con su lámpara a los dos lados, medidores, cajas de
    /// madera al frente, bolsas de basura, cables y luz cálida de las lámparas; todo pegado a los muros (franja de 1.3 m) para dejar libre el centro, por
    /// donde pasa la ruta de la demo. Idempotente (rehace S2_Callejon). Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarS2Callejon.Colocar
    /// </summary>
    public static class ColocarS2Callejon
    {
        public const string NombreRaiz = "S2_Callejon";
        private const string C = "Assets/Arte/Calle/Prefabs/";
        private const string L = "Assets/Arte/Callejon/Prefabs/";
        // Callejón: de la calle (x -55) a la reja (x -86); muro norte z -54.5, muro sur z -63.5; piso 0.2.
        public const float MuroNorte = -54.5f, MuroSur = -63.5f, Piso = 0.2f;
        private static readonly Color Calido = new Color(1f, 0.7f, 0.4f);
        private static readonly Color Turquesa = new Color(0.3f, 1f, 0.9f);

        [MenuItem("Mecha Cyberpunk/S2/5 Callejón")]
        public static void Colocar()
        {
            ColocarS2.Informe.Clear();
            ColocarS2.Abrir();
            Object.DestroyImmediate(GameObject.Find(NombreRaiz));
            Transform r = new GameObject(NombreRaiz).transform;
            // Cubos viejos del callejón (cajas, bolsas y barriles de greybox con su modelo encima): solo se ven los modelos.
            foreach (string n in new[] { "Caja1", "Caja2", "Caja3", "Caja4", "Bolsas1", "Bolsas2", "LetreroFondo" })
            {
                Transform t = ColocarS2.Buscar("Barrio/Manzana_4/Callejon/" + n);
                if (t != null && t.GetComponent<MeshFilter>() != null && t.GetComponent<MeshFilter>().sharedMesh != null && t.GetComponent<MeshFilter>().sharedMesh.name == "Cube")
                {
                    t.GetComponent<Renderer>().enabled = false;
                }
            }

            // Puerta trasera en el muro sur con su lámpara de jaula y su repisa, como las de la referencia; y lámpara sobre la del muro norte (T-250).
            Pared(r, L + "L02_PuertaTrasera", -67.5f, false, Piso, 1f);
            Pared(r, C + "N01", -67.5f, false, Piso + 2.55f, 0.9f);
            Pared(r, C + "N01", -70f, true, Piso + 2.55f, 0.9f);
            Pared(r, C + "C14_RegistroMedidores", -69.2f, false, Piso + 1.1f, 0.9f);
            Pared(r, C + "C14_RegistroMedidores", -72.1f, true, Piso + 1.2f, 0.9f);
            Pared(r, C + "CY03_MazoCables", -65.6f, false, Piso + 0.6f, 1f);
            Pared(r, C + "CY03_MazoCables", -76.5f, true, Piso + 0.6f, 1f);
            // Al frente (boca del callejón) y al fondo: cajas de madera, tambos con franja, llantas y bolsas, junto a los muros.
            Suelo(r, C + "C17_Huacales", -58.2f, MuroNorte - 0.75f, 15f, 1f);
            Suelo(r, C + "C17_Huacales", -59.3f, MuroNorte - 0.7f, -10f, 0.85f);
            Suelo(r, C + "C17_Huacales", -58.6f, MuroSur + 0.7f, 5f, 1f);
            Suelo(r, C + "C16_Tambo", -80.4f, MuroSur + 0.6f, 0f, 1f);
            Suelo(r, C + "C16_Tambo", -81.2f, MuroSur + 0.55f, 40f, 1f);
            Suelo(r, C + "C16_Tambo", -78.5f, MuroNorte - 0.55f, 0f, 1f);
            Suelo(r, L + "L04_Llantas", -82.6f, MuroNorte - 0.75f, 25f, 1f);
            Suelo(r, C + "N06", -61.0f, MuroSur + 0.6f, 200f, 0.8f);
            Suelo(r, C + "N06", -75.0f, MuroNorte - 0.55f, 60f, 0.7f);

            Transform luces = new GameObject("Luces").transform;
            luces.SetParent(r, false);
            Luz(luces, "Lampara_Sur", new Vector3(-67.5f, Piso + 2.35f, MuroSur + 0.6f), Calido, 2.4f, 5f);
            Luz(luces, "Lampara_Norte", new Vector3(-70f, Piso + 2.35f, MuroNorte - 0.6f), Calido, 2.4f, 5f);
            Luz(luces, "Turquesa_Medidores", new Vector3(-72.1f, Piso + 1.6f, MuroNorte - 0.5f), Turquesa, 0.9f, 2.5f);
            ColocarS2.Informe.AppendLine("Callejón: " + r.childCount + " piezas");
            ColocarS2.Guardar("s2-callejon");
        }

        // Pega una pieza al muro norte (mirando al sur) o al sur (mirando al norte) en la x dada, a la altura 'y'.
        private static void Pared(Transform r, string ruta, float x, bool norte, float y, float escala)
        {
            GameObject g = ColocarS2Interiores.Modelo(r, ruta, new Vector3(x, 0f, 0f), norte ? 0f : 180f, y, escala, sinCollider: true);
            if (g == null)
            {
                return;
            }
            Bounds b = ArteT250.Limites(g);
            float z = norte ? MuroNorte - 0.02f - b.max.z : MuroSur + 0.02f - b.min.z;
            g.transform.position += new Vector3(0f, 0f, z + (norte ? 0f : 0f) - 0f);
            Bounds b2 = ArteT250.Limites(g);
            g.transform.position += new Vector3(0f, 0f, norte ? (MuroNorte - 0.02f) - b2.max.z : (MuroSur + 0.02f) - b2.min.z);
        }

        private static void Suelo(Transform r, string ruta, float x, float z, float yaw, float escala)
        {
            ColocarS2Interiores.Modelo(r, ruta, new Vector3(x, 0f, z), yaw, Piso, escala);
        }

        private static void Luz(Transform padre, string nombre, Vector3 pos, Color color, float intensidad, float alcance)
        {
            Light l = new GameObject(nombre).AddComponent<Light>();
            l.transform.SetParent(padre, false);
            l.transform.position = pos;
            l.type = LightType.Point;
            l.color = color;
            l.intensity = intensidad;
            l.range = alcance;
            l.shadows = LightShadows.None;
        }
    }
}
