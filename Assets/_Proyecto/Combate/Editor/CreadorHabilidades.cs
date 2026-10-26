using MechaCyberpunk.Combate.Habilidades;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Combate.Editor
{
    /// <summary>
    /// Crea las 8 habilidades especiales a nivel I (T-174, valores provisionales de D-tony-136). Idempotente: no pisa un asset que ya existe.
    /// Batchmode: -executeMethod MechaCyberpunk.Combate.Editor.CreadorHabilidades.Crear
    /// </summary>
    public static class CreadorHabilidades
    {
        private const string Carpeta = "Assets/_Proyecto/Combate/Datos/Habilidades";

        // Colores de rama (D-tony-122): militar azul acero, tech turquesa, cuerpo a cuerpo naranja, fuego magenta.
        private static readonly Color Militar = new Color(0.45f, 0.62f, 0.85f);
        private static readonly Color Tech = new Color(0.15f, 0.9f, 0.85f);
        private static readonly Color CuerpoACuerpo = new Color(1f, 0.55f, 0.1f);
        private static readonly Color Fuego = new Color(1f, 0.15f, 0.7f);

        [MenuItem("Mecha Cyberpunk/Crear T-174 habilidades")]
        public static void Crear()
        {
            if (!AssetDatabase.IsValidFolder(Carpeta)) AssetDatabase.CreateFolder("Assets/_Proyecto/Combate/Datos", "Habilidades");

            Hab("Misil", "habilidad_misil", "MISIL", TipoEfectoHabilidad.Proyectil, 20f, 8f, Militar, so =>
            {
                Num(so, "velocidad", 20f); Num(so, "alcance", 25f); Num(so, "radioExplosion", 1.2f); Ent(so, "cantidad", 1);
            });
            Hab("Salva", "habilidad_salva", "SALVA", TipoEfectoHabilidad.Proyectil, 8f, 10f, Militar, so =>
            {
                Num(so, "velocidad", 20f); Num(so, "alcance", 25f); Num(so, "radioExplosion", 0.8f); Ent(so, "cantidad", 3); Num(so, "dispersion", 6f);
            });
            Hab("Lanzallamas", "habilidad_lanzallamas", "LANZALLAMAS", TipoEfectoHabilidad.Cono, 14f, 6f, Fuego, so =>
            {
                Num(so, "alcanceCono", 4f); Num(so, "angulo", 60f);
            });
            Hab("OndaDeCalor", "habilidad_ondacalor", "ONDA DE CALOR", TipoEfectoHabilidad.Cono, 15f, 9f, Fuego, so =>
            {
                Num(so, "alcanceCono", 3f); Num(so, "angulo", 360f); Num(so, "empuje", 1.5f);
            });
            Hab("Descarga", "habilidad_descarga", "DESCARGA", TipoEfectoHabilidad.Cono, 6f, 5f, Tech, so =>
            {
                Num(so, "alcanceCono", 3.5f); Num(so, "angulo", 30f); Num(so, "aturdir", 1f);
            });
            Hab("Escudo", "habilidad_escudo", "ESCUDO", TipoEfectoHabilidad.Escudo, 0f, 8f, Tech, so =>
            {
                Num(so, "reduccion", 0.5f); Num(so, "duracion", 3f);
            });
            Hab("Martillo", "habilidad_martillo", "MARTILLO", TipoEfectoHabilidad.Cono, 16f, 6f, CuerpoACuerpo, so =>
            {
                Num(so, "alcanceCono", 2.8f); Num(so, "angulo", 40f);
                so.FindProperty("rompeGuardia").boolValue = true;
            });
            Hab("Embestida", "habilidad_embestida", "EMBESTIDA", TipoEfectoHabilidad.Embestida, 14f, 7f, CuerpoACuerpo, so =>
            {
                Num(so, "distancia", 5f); Num(so, "duracionEmbestida", 0.3f); Num(so, "empuje", 2f);
            });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Combate] T-174 habilidades creadas: 8.");
        }

        private static void Num(SerializedObject so, string campo, float v) { so.FindProperty(campo).floatValue = v; }
        private static void Ent(SerializedObject so, string campo, int v) { so.FindProperty(campo).intValue = v; }

        private static void Hab(string archivo, string id, string nombre, TipoEfectoHabilidad efecto, float danio, float enfriamiento, Color color,
            System.Action<SerializedObject> extra)
        {
            string ruta = $"{Carpeta}/DatosHabilidad_{archivo}.asset";
            if (AssetDatabase.LoadAssetAtPath<DatosHabilidad>(ruta) != null) return; // no se pisan los valores ajustados en el inspector
            var h = ScriptableObject.CreateInstance<DatosHabilidad>();
            AssetDatabase.CreateAsset(h, ruta);
            var so = new SerializedObject(h);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("nombre").stringValue = nombre;
            so.FindProperty("efecto").enumValueIndex = (int)efecto;
            Num(so, "danio", danio);
            Num(so, "enfriamiento", enfriamiento);
            so.FindProperty("colorRama").colorValue = color;
            extra(so);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(h);
        }
    }
}
