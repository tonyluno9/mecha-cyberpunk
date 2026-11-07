using System.Collections.Generic;
using MechaCyberpunk.Core;

namespace MechaCyberpunk.UI.Hud
{
    /// <summary>Una ayuda de botón del HUD: la tecla real y lo que hace.</summary>
    public readonly struct AyudaH10
    {
        public string Tecla { get; }
        public string Texto { get; }

        public AyudaH10(string tecla, string texto)
        {
            Tecla = tecla;
            Texto = texto;
        }
    }

    /// <summary>Lo que pasa ahora y decide qué ayudas salen.</summary>
    public struct ContextoAyudaH10
    {
        public ModoJugador Modo;
        public bool ArmaDeFuego;
        public int Balas;
        public int Cargador;
        public bool Recargando;
        public TipoCandidatoInteraccion Candidato;
        public string NombreCandidato;
        public string TextoInteractuable;
        public bool PuedeUsarInteractuable;
        public string TeclaRecargar;
        public string TeclaGolpe;
        public string TeclaInteractuar;
    }

    /// <summary>
    /// Qué ayudas de botón muestra el HUD abajo a la derecha (T-700), como mucho <see cref="Maximo"/>, en este orden: lo que hace la tecla de
    /// interactuar con lo que tienes enfrente (subir al carro, abrir, usar, hablar), recargar si el cargador no está lleno, y golpe con los puños.
    ///  Solo a pie: en el carro y el mecha no salen. Las teclas llegan ya leídas del Input System.
    /// </summary>
    public static class ReglaAyudasH10
    {
        public const int Maximo = 2;
        public const string TextoRecargar = "Recargar";
        public const string TextoGolpe = "Golpe";
        public const string TextoSubirCarro = "Subir al carro";
        public const string TextoSubirMoto = "Subir a la moto";
        public const string TextoAbrir = "Abrir";
        public const string TextoUsar = "Usar";

        public static List<AyudaH10> Ayudas(ContextoAyudaH10 c)
        {
            List<AyudaH10> lista = new List<AyudaH10>();
            if (c.Modo != ModoJugador.APie)
            {
                return lista;
            }
            string interaccion = TextoInteraccion(c);
            if (interaccion.Length > 0)
            {
                lista.Add(new AyudaH10(c.TeclaInteractuar, interaccion));
            }
            if (c.ArmaDeFuego && !c.Recargando && c.Cargador > 0 && c.Balas < c.Cargador)
            {
                lista.Add(new AyudaH10(c.TeclaRecargar, TextoRecargar));
            }
            if (!c.ArmaDeFuego)
            {
                lista.Add(new AyudaH10(c.TeclaGolpe, TextoGolpe));
            }
            if (lista.Count > Maximo)
            {
                lista.RemoveRange(Maximo, lista.Count - Maximo);
            }
            return lista;
        }

        /// <summary>Lo que hace la tecla de interactuar con lo que hay enfrente; vacío si no hay nada (o si lo usable no se puede usar: eso es un aviso).</summary>
        public static string TextoInteraccion(ContextoAyudaH10 c)
        {
            switch (c.Candidato)
            {
                case TipoCandidatoInteraccion.Montable:
                    return !string.IsNullOrEmpty(c.NombreCandidato) && c.NombreCandidato.ToLowerInvariant().Contains("moto") ? TextoSubirMoto : TextoSubirCarro;
                case TipoCandidatoInteraccion.Puerta:
                    return string.IsNullOrEmpty(c.NombreCandidato) ? TextoAbrir : TextoAbrir + " " + c.NombreCandidato;
                case TipoCandidatoInteraccion.Interactuable:
                    if (!c.PuedeUsarInteractuable)
                    {
                        return string.Empty;
                    }
                    return string.IsNullOrEmpty(c.TextoInteractuable) ? TextoUsar : c.TextoInteractuable;
                default:
                    return string.Empty;
            }
        }
    }
}
