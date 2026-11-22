using UnityEngine;

namespace MechaCyberpunk.UI.Voces
{
    /// <summary>T-1624 (T15): la opción de encender o apagar los subtítulos de las voces del mundo. Se guarda en PlayerPrefs (por defecto, encendidos).</summary>
    public static class OpcionSubtitulos
    {
        public const string Clave = "opcion_subtitulos";

        public static bool Activado => PlayerPrefs.GetInt(Clave, 1) != 0;

        public static void Fijar(bool valor)
        {
            PlayerPrefs.SetInt(Clave, valor ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
