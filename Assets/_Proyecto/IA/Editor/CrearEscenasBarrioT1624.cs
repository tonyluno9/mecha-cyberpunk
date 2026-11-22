using System.IO;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.Barrio;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.IA.Editor
{
    /// <summary>
    /// T-1624 (T12 y T15): crea (o actualiza) los guiones de las escenas del barrio en IA/Resources/Barrio y el prefab marcador MarcadorEscenaBarrio.
    /// Se puede repetir sin perder el audio que Tony ya haya asignado a las líneas (solo reescribe texto y gestos de las líneas que ya existen por posición).
    /// Batchmode: -executeMethod MechaCyberpunk.IA.Editor.CrearEscenasBarrioT1624.Aplicar
    /// Menú de depuración (en Play): Mecha Cyberpunk/Barrio/...
    /// </summary>
    public static class CrearEscenasBarrioT1624
    {
        public const string CarpetaEscenas = "Assets/_Proyecto/IA/Resources/Barrio";
        public const string RutaMarcador = "Assets/_Proyecto/IA/Prefabs/MarcadorEscenaBarrio.prefab";
        private const float PesoBullying = 1f;
        private const float PesoNormal = 0.6f;
        private const float RadioMarcador = 10f;
        private const string Skull = "SKULL";
        private const string Snake = "SNAKE";
        private const string Risa = "SKU_RISA_BURLA";

        [MenuItem("Mecha Cyberpunk/IA/T-1624 Crear escenas del barrio")]
        public static void Aplicar()
        {
            AsegurarCarpeta(CarpetaEscenas);
            // Bullying (T12 y T15, paso 3).
            Crear("bully_piso_tianguis", TipoEscenaBarrio.Bullying, IdPandilla.Calaveras, 2, "Dos SKULLS le cobran piso a un vendedor del tianguis y le tiran la mercancía.", true, PesoBullying,
                new LineaBarrio(RolBarrio.Agresor1, Skull, "¿Y la cuota de esta semana, jefe?"),
                new LineaBarrio(RolBarrio.Victima, "VENDEDOR", "Ya les pagué ayer...", GestoBarrio.Suplicar),
                new LineaBarrio(RolBarrio.Agresor2, Skull, "Pues hoy pagas doble.", GestoBarrio.Empujar),
                new LineaBarrio(RolBarrio.Agresor1, Skull, string.Empty, GestoBarrio.Patear, string.Empty, 0.2f),
                new LineaBarrio(RolBarrio.Victima, string.Empty, string.Empty, GestoBarrio.Tumbar, string.Empty, 0.2f),
                new LineaBarrio(RolBarrio.Agresor2, Skull, "(risas)", GestoBarrio.Burla, Risa));
            Crear("bully_celular", TipoEscenaBarrio.Bullying, IdPandilla.Calaveras, 2, "Un SKULL le quita el celular a un joven y lo empuja al suelo.", true, PesoBullying,
                new LineaBarrio(RolBarrio.Agresor1, Skull, "Préstame tu cel, carnal... gracias.", GestoBarrio.Quitar),
                new LineaBarrio(RolBarrio.Agresor1, Skull, string.Empty, GestoBarrio.Empujar),
                new LineaBarrio(RolBarrio.Victima, string.Empty, string.Empty, GestoBarrio.Tumbar, string.Empty, 0.2f),
                new LineaBarrio(RolBarrio.Agresor2, Skull, "(risas)", GestoBarrio.Burla, Risa));
            Crear("bully_snakes_senora", TipoEscenaBarrio.Bullying, IdPandilla.Viboras, 2, "SNAKES acorralan a una señora con su mandado en Neón Alto.", true, PesoBullying,
                new LineaBarrio(RolBarrio.Agresor1, Snake, "Esta calle es nuestra, doña."),
                new LineaBarrio(RolBarrio.Victima, "SEÑORA", "Ya no traigo nada, muchachos...", GestoBarrio.Suplicar),
                new LineaBarrio(RolBarrio.Agresor2, Snake, "Aquí se paga por pasar.", GestoBarrio.Burla));
            Crear("bully_repartidor", TipoEscenaBarrio.Bullying, IdPandilla.Calaveras, 2, "Un pandillero tira a un repartidor de su moto o bici y patea su caja.", true, PesoBullying,
                new LineaBarrio(RolBarrio.Agresor1, Skull, "¡Ora, repartidor, aquí no se pasa gratis!", GestoBarrio.Empujar),
                new LineaBarrio(RolBarrio.Victima, string.Empty, string.Empty, GestoBarrio.Tumbar, string.Empty, 0.2f),
                new LineaBarrio(RolBarrio.Agresor2, Skull, string.Empty, GestoBarrio.Patear),
                new LineaBarrio(RolBarrio.Agresor1, Skull, "(risas)", GestoBarrio.Burla, Risa));
            Crear("bully_abuelo", TipoEscenaBarrio.Bullying, IdPandilla.Calaveras, 2, "Dos SKULLS se burlan de un señor mayor y lo empujan; se cae y ellos se van riendo.", true, PesoBullying,
                new LineaBarrio(RolBarrio.Agresor1, Skull, "Ándale, abuelo, más rápido.", GestoBarrio.Empujar),
                new LineaBarrio(RolBarrio.Victima, "ABUELO", string.Empty, GestoBarrio.Tumbar, string.Empty, 0.2f),
                new LineaBarrio(RolBarrio.Agresor2, Skull, "(risas)", GestoBarrio.Burla, Risa));
            Crear("bully_comerciante_cortina", TipoEscenaBarrio.Bullying, IdPandilla.Calaveras, 2, "Un grupo intimida a un comerciante que cierra su cortina.", true, PesoBullying,
                new LineaBarrio(RolBarrio.Agresor1, Skull, "Mañana venimos por lo de la protección."),
                new LineaBarrio(RolBarrio.Victima, "COMERCIANTE", "Sí, sí... aquí los espero.", GestoBarrio.Suplicar),
                new LineaBarrio(RolBarrio.Agresor2, Skull, string.Empty, GestoBarrio.Burla));
            // Escenas normales (T15, paso 4).
            Crear("vendedor_tacos", TipoEscenaBarrio.Normal, IdPandilla.Ninguna, 1, "Un vendedor ofrece tacos.", false, PesoNormal,
                new LineaBarrio(RolBarrio.Persona1, "VENDEDOR", "¡Lleve sus tacos!"));
            Crear("vendedor_elotes", TipoEscenaBarrio.Normal, IdPandilla.Ninguna, 1, "Un vendedor ofrece elotes y esquites.", false, PesoNormal,
                new LineaBarrio(RolBarrio.Persona1, "VENDEDOR", "¡Elotes, esquites!"));
            Crear("vecinos_muro", TipoEscenaBarrio.Normal, IdPandilla.Ninguna, 1, "Dos vecinos platican del muro.", false, PesoNormal,
                new LineaBarrio(RolBarrio.Persona1, "VECINA", "Dicen que nadie ha salido del muro en años..."),
                new LineaBarrio(RolBarrio.Persona2, "VECINO", "Mejor ni preguntes, comadre.", GestoBarrio.Ninguno, string.Empty, 0.8f));
            Crear("vecinos_nova", TipoEscenaBarrio.Normal, IdPandilla.Ninguna, 1, "Dos vecinos platican de NOVA.", false, PesoNormal,
                new LineaBarrio(RolBarrio.Persona1, "VECINO", "NOVA dice que todo es por nuestra seguridad."),
                new LineaBarrio(RolBarrio.Persona2, "VECINA", "Pues yo no me siento más segura, la verdad.", GestoBarrio.Ninguno, string.Empty, 0.8f));
            Crear("pareja_discutiendo", TipoEscenaBarrio.Normal, IdPandilla.Ninguna, 1, "Una pareja discute.", false, PesoNormal,
                new LineaBarrio(RolBarrio.Persona1, "ÉL", "¡Siempre dices lo mismo!"),
                new LineaBarrio(RolBarrio.Persona2, "ELLA", "¡Es que nunca me escuchas!", GestoBarrio.Ninguno, string.Empty, 0.5f));
            Crear("celular", TipoEscenaBarrio.Normal, IdPandilla.Ninguna, 1, "Alguien habla por celular.", false, PesoNormal,
                new LineaBarrio(RolBarrio.Persona1, "HOMBRE", "Sí, ya voy para allá... no, no me esperes.", GestoBarrio.Telefono));
            Crear("borracho", TipoEscenaBarrio.Normal, IdPandilla.Ninguna, 1, "El borracho de la esquina.", false, PesoNormal,
                new LineaBarrio(RolBarrio.Persona1, "BORRACHO", "¡Yo invito la... la ronda! ¡Salud!", GestoBarrio.Burla));
            CrearMarcador();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[IA] T-1624: escenas del barrio y marcador listos.");
        }

        private static void Crear(string id, TipoEscenaBarrio tipo, IdPandilla pandilla, int agresores, string descripcion, bool rie, float peso, params LineaBarrio[] lineas)
        {
            string ruta = CarpetaEscenas + "/EscenaBarrio_" + id + ".asset";
            EscenaBarrio escena = AssetDatabase.LoadAssetAtPath<EscenaBarrio>(ruta);
            if (escena == null)
            {
                escena = ScriptableObject.CreateInstance<EscenaBarrio>();
                AssetDatabase.CreateAsset(escena, ruta);
            }
            else
            {
                // Conserva el audio que ya se haya asignado a una línea de la misma posición.
                LineaBarrio[] viejas = escena.Lineas;
                for (int i = 0; i < lineas.Length && i < viejas.Length; i++)
                {
                    if (viejas[i].Audio != null)
                    {
                        lineas[i] = CopiarConAudio(lineas[i], viejas[i].Audio);
                    }
                }
            }
            escena.Definir(id, tipo, pandilla, agresores, descripcion, rie, lineas);
            SerializedObject so = new SerializedObject(escena);
            so.FindProperty("peso").floatValue = peso;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(escena);
        }

        private static LineaBarrio CopiarConAudio(LineaBarrio nueva, AudioClip clip)
        {
            LineaBarrio copia = JsonUtility.FromJson<LineaBarrio>(JsonUtility.ToJson(nueva));
            // LineaBarrio no es un objeto de Unity: el audio se asigna por reflexión del campo serializado.
            typeof(LineaBarrio).GetField("audio", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(copia, clip);
            return copia;
        }

        private static void CrearMarcador()
        {
            AsegurarCarpeta("Assets/_Proyecto/IA/Prefabs");
            GameObject raiz = new GameObject("MarcadorEscenaBarrio");
            try
            {
                raiz.AddComponent<MarcadorEscenaBarrio>().Definir(string.Empty, RadioMarcador);
                PrefabUtility.SaveAsPrefabAsset(raiz, RutaMarcador);
            }
            finally
            {
                Object.DestroyImmediate(raiz);
            }
        }

        private static void AsegurarCarpeta(string ruta)
        {
            if (AssetDatabase.IsValidFolder(ruta))
            {
                return;
            }
            string padre = Path.GetDirectoryName(ruta).Replace('\\', '/');
            AsegurarCarpeta(padre);
            AssetDatabase.CreateFolder(padre, Path.GetFileName(ruta));
        }

        [MenuItem("Mecha Cyberpunk/Barrio/Forzar bullying (Play)")]
        private static void MenuBullying() => Debug.Log("[Barrio] " + DirectorEscenasBarrio.ForzarBullying());

        [MenuItem("Mecha Cyberpunk/Barrio/Forzar bullying SKULLS (Play)")]
        private static void MenuBullyingSkulls() => Debug.Log("[Barrio] " + DirectorEscenasBarrio.ForzarBullying(MechaCyberpunk.Core.IdPandilla.Calaveras));

        [MenuItem("Mecha Cyberpunk/Barrio/Forzar bullying SNAKES (Play)")]
        private static void MenuBullyingSnakes() => Debug.Log("[Barrio] " + DirectorEscenasBarrio.ForzarBullying(MechaCyberpunk.Core.IdPandilla.Viboras));

        [MenuItem("Mecha Cyberpunk/Barrio/Listar escenas (Play)")]
        private static void MenuListar() => DirectorEscenasBarrio.ListarEscenas();
    }
}
