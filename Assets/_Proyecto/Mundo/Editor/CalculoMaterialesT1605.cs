using System;

namespace MechaCyberpunk.Mundo.Editor
{
    /// <summary>Lógica pura de T14: adivina el tipo de un material por su nombre. Lo que brilla solo, el agua, el asfalto y el vidrio quedan como Otro (no se tocan).</summary>
    public static class CalculoMaterialesT1605
    {
        private static readonly string[] Excluidos =
        {
            "neon", "emis", "foco", "luz", "vidrio", "cristal", "pantalla", "led", "letrero", "aviso", "asfalto", "calle", "charco", "agua", "mar", "cielo", "nube", "halo", "vapor", "humo",
        };
        private static readonly string[] Pintura = { "carro", "pintura", "auto", "carroceria", "taxi", "camioneta" };
        private static readonly string[] Metal = { "metal", "acero", "reja", "fierro", "tubo", "lamina", "chapa", "herrer", "puerta", "cable", "poste", "tambo", "cortina" };
        private static readonly string[] Banqueta = { "banqueta", "acera", "piso", "suelo", "adoquin", "loseta", "guarnicion" };
        private static readonly string[] Pared = { "pared", "muro", "fachada", "concreto", "ladrillo", "techo", "azotea", "edificio", "estuco", "block", "casa" };

        public static TipoMaterialT1605 Clasificar(string nombre)
        {
            if (string.IsNullOrEmpty(nombre))
            {
                return TipoMaterialT1605.Otro;
            }
            string n = nombre.ToLowerInvariant();
            if (Contiene(n, Excluidos))
            {
                return TipoMaterialT1605.Otro;
            }
            if (Contiene(n, Pintura))
            {
                return TipoMaterialT1605.Pintura;
            }
            if (Contiene(n, Metal))
            {
                return TipoMaterialT1605.Metal;
            }
            if (Contiene(n, Banqueta))
            {
                return TipoMaterialT1605.Banqueta;
            }
            if (Contiene(n, Pared))
            {
                return TipoMaterialT1605.Pared;
            }
            return TipoMaterialT1605.Otro;
        }

        private static bool Contiene(string texto, string[] claves)
        {
            for (int i = 0; i < claves.Length; i++)
            {
                if (texto.IndexOf(claves[i], StringComparison.Ordinal) >= 0)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
