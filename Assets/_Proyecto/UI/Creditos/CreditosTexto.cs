using System.Collections.Generic;
using UnityEngine;

namespace MechaCyberpunk.UI.Creditos
{
    public enum TipoLineaCredito
    {
        Titulo,
        Seccion,
        Subseccion,
        Lista,
        Texto,
        Espacio,
    }

    public readonly struct LineaCredito
    {
        public readonly TipoLineaCredito Tipo;
        public readonly string Texto;

        public LineaCredito(TipoLineaCredito tipo, string texto)
        {
            Tipo = tipo;
            Texto = texto;
        }
    }

    /// <summary>
    /// El texto de los créditos (T-471): se lee de Resources/Creditos (TextAsset que genera CreadorCreditos desde docs/creditos.md) y se
    /// parte en líneas: "# " título, "## " sección, "### " subsección, "- " lista, línea vacía espacio y lo demás texto.
    /// </summary>
    public static class CreditosTexto
    {
        public const string NombreRecurso = "Creditos";
        public const string TextoSinCreditos = "# CRÉDITOS\n\n## SIN TEXTO\n\nFalta Resources/Creditos.";

        public static string Cargar()
        {
            TextAsset asset = Resources.Load<TextAsset>(NombreRecurso);
            return asset != null ? asset.text : TextoSinCreditos;
        }

        public static List<LineaCredito> Parsear(string texto)
        {
            List<LineaCredito> lineas = new List<LineaCredito>();
            foreach (string cruda in (texto ?? string.Empty).Replace("\r", string.Empty).Split('\n'))
            {
                string l = cruda.TrimEnd();
                if (l.Length == 0)
                {
                    lineas.Add(new LineaCredito(TipoLineaCredito.Espacio, string.Empty));
                }
                else if (l.StartsWith("### "))
                {
                    lineas.Add(new LineaCredito(TipoLineaCredito.Subseccion, l.Substring(4)));
                }
                else if (l.StartsWith("## "))
                {
                    lineas.Add(new LineaCredito(TipoLineaCredito.Seccion, l.Substring(3)));
                }
                else if (l.StartsWith("# "))
                {
                    lineas.Add(new LineaCredito(TipoLineaCredito.Titulo, l.Substring(2)));
                }
                else if (l.StartsWith("- "))
                {
                    lineas.Add(new LineaCredito(TipoLineaCredito.Lista, l.Substring(2)));
                }
                else
                {
                    lineas.Add(new LineaCredito(TipoLineaCredito.Texto, l));
                }
            }
            return lineas;
        }
    }
}
