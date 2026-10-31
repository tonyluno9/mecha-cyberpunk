using System;
using System.Collections.Generic;
using System.Text;

namespace MechaCyberpunk.Personaje.Visual
{
    /// <summary>Tipo de arma que se muestra en el Animator (parámetro TipoArma).</summary>
    public enum TipoArmaVisual
    {
        Ninguna = 0,
        Pistola = 1,
        ArmaLarga = 2,
    }

    /// <summary>Los clips que espera el controlador visual. Cada uno es un archivo de animación de Mixamo (T-473).</summary>
    public enum ClipVisual
    {
        Idle,
        Caminar,
        Correr,
        Saltar,
        Golpe,
        RecibirGolpe,
        Caer,
        ApuntarPistola,
        DispararPistola,
        ApuntarLarga,
        DispararLarga,
        Recargar,
        Cubrirse,
        Rodar,
    }

    /// <summary>Nombres de parámetros y estados del Animator visual, y cómo se reconoce cada clip por el nombre del archivo. Lógica pura.</summary>
    public static class ReglasVisual
    {
        public const string Velocidad = "Velocidad";
        public const string EnSuelo = "EnSuelo";
        public const string Apuntando = "Apuntando";
        public const string TipoArma = "TipoArma";
        public const string Disparar = "Disparar";
        public const string Recargar = "Recargar";
        public const string Golpe = "Golpe";
        public const string Herido = "Herido";
        public const string Caer = "Caer";
        public const string Cubierto = "Cubierto";
        public const string Rodar = "Rodar";

        /// <summary>Velocidad (m/s) a partir de la cual pasa de Idle a Caminar y de Caminar a Correr.</summary>
        public const float UmbralCaminar = 0.1f;
        public const float UmbralCorrer = 4f;

        public static readonly string[] ParametrosFloat = { Velocidad };
        public static readonly string[] ParametrosBool = { EnSuelo, Apuntando, Caer, Cubierto };
        public static readonly string[] ParametrosInt = { TipoArma };
        public static readonly string[] ParametrosTrigger = { Disparar, Recargar, Golpe, Herido, Rodar };

        /// <summary>Estados de la capa base del controlador (el nombre del estado es el del clip).</summary>
        public static readonly string[] Estados =
        {
            "Idle", "Caminar", "Correr", "Saltar", "Golpe", "RecibirGolpe", "Caer", "ApuntarPistola", "DispararPistola",
            "ApuntarLarga", "DispararLarga", "Recargar", "Cubrirse", "Rodar",
        };

        /// <summary>Clips que se repiten en bucle.</summary>
        public static bool EsBucle(ClipVisual clip)
        {
            switch (clip)
            {
                case ClipVisual.Idle:
                case ClipVisual.Caminar:
                case ClipVisual.Correr:
                case ClipVisual.ApuntarPistola:
                case ClipVisual.ApuntarLarga:
                case ClipVisual.Cubrirse:
                    return true;
                default:
                    return false;
            }
        }

        // Nombre normalizado (minúsculas, solo letras y números) -> clip. Nombres en español y los que trae Mixamo.
        private static readonly (string[] Alias, ClipVisual Clip)[] Reconocidos =
        {
            (new[] { "recibirgolpe", "herido", "hit", "reaction", "hitreaction", "gethit", "impact", "damage" }, ClipVisual.RecibirGolpe),
            (new[] { "apuntarpistola", "pistolidle", "pistolaim", "aimpistol" }, ClipVisual.ApuntarPistola),
            (new[] { "dispararpistola", "pistolfire", "pistolshoot", "firepistol", "shootpistol" }, ClipVisual.DispararPistola),
            (new[] { "apuntarlarga", "rifleidle", "rifleaim", "aimrifle" }, ClipVisual.ApuntarLarga),
            (new[] { "dispararlarga", "riflefire", "rifleshoot", "firerifle", "shootrifle", "firingrifle" }, ClipVisual.DispararLarga),
            (new[] { "recargar", "reload", "reloading" }, ClipVisual.Recargar),
            (new[] { "cubrirse", "cover", "crouch", "crouching", "takecover" }, ClipVisual.Cubrirse),
            (new[] { "rodar", "roll", "rolling", "dive", "forwardroll" }, ClipVisual.Rodar),
            (new[] { "caer", "fall", "falling", "dying", "death", "ko", "fallingbackdeath", "knockdown" }, ClipVisual.Caer),
            (new[] { "golpe", "punch", "punching", "attack", "hook", "jab", "crosspunch" }, ClipVisual.Golpe),
            (new[] { "saltar", "jump", "jumping", "runningjump" }, ClipVisual.Saltar),
            (new[] { "correr", "run", "running", "sprint" }, ClipVisual.Correr),
            (new[] { "caminar", "walk", "walking" }, ClipVisual.Caminar),
            (new[] { "idle", "quieto", "breathingidle", "standing", "stand" }, ClipVisual.Idle),
        };

        /// <summary>Minúsculas y solo letras o números, sin la extensión ("Rifle Idle.fbx" da "rifleidle").</summary>
        public static string Normalizar(string nombre)
        {
            if (string.IsNullOrEmpty(nombre))
            {
                return string.Empty;
            }
            int punto = nombre.LastIndexOf('.');
            if (punto > 0 && nombre.Length - punto <= 5)
            {
                nombre = nombre.Substring(0, punto);
            }
            var salida = new StringBuilder();
            foreach (char c in nombre.ToLowerInvariant())
            {
                if (char.IsLetterOrDigit(c))
                {
                    salida.Append(c);
                }
            }
            return salida.ToString();
        }

        /// <summary>Reconoce qué clip es un archivo por su nombre ("Caminar.fbx", "Walking.fbx"). False si no es una animación esperada.</summary>
        public static bool TryClipDeArchivo(string nombreArchivo, out ClipVisual clip)
        {
            string n = Normalizar(nombreArchivo);
            foreach ((string[] alias, ClipVisual c) in Reconocidos)
            {
                foreach (string a in alias)
                {
                    if (n == a)
                    {
                        clip = c;
                        return true;
                    }
                }
            }
            clip = default;
            return false;
        }

        /// <summary>El modelo es Modelo.fbx, o si no, el primer archivo que no es una animación reconocida. Null si no hay.</summary>
        public static string ElegirModelo(IList<string> nombresArchivo)
        {
            foreach (string nombre in nombresArchivo)
            {
                if (Normalizar(nombre) == "modelo")
                {
                    return nombre;
                }
            }
            foreach (string nombre in nombresArchivo)
            {
                if (!TryClipDeArchivo(nombre, out _))
                {
                    return nombre;
                }
            }
            return null;
        }

        /// <summary>Rapidez en el plano horizontal (la caída o el salto no cuentan como correr).</summary>
        public static float VelocidadHorizontal(float x, float z)
        {
            return (float)Math.Sqrt(x * x + z * z);
        }
    }
}
