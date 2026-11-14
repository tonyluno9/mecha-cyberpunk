using System;
using System.Collections.Generic;

namespace MechaCyberpunk.UI.Hud
{
    /// <summary>
    /// Reglas puras del tutorial ligero (T-441): qué aviso toca, cuánto dura, cuándo se da por visto y cómo se guarda lo visto.
    /// Un aviso sale solo si la acción está disponible y su requisito ya se vio; se va cuando la persona hace la acción o pasa su tiempo,
    /// y en los dos casos no vuelve a salir. Entre un aviso y el siguiente hay una pausa. Apagado (<see cref="Activo"/> falso) no avisa.
    /// </summary>
    public class ReglaTutorial
    {
        public const char Separador = ',';

        private readonly HashSet<AccionTutorial> vistas = new HashSet<AccionTutorial>();
        private readonly float segundosAviso;
        private readonly float segundosPausa;
        private float restante;
        private float pausa;

        public ReglaTutorial(float segundosAviso, float segundosPausa)
        {
            this.segundosAviso = Math.Max(0.1f, segundosAviso);
            this.segundosPausa = Math.Max(0f, segundosPausa);
        }

        /// <summary>False: el tutorial está apagado en Opciones.</summary>
        public bool Activo { get; set; } = true;

        /// <summary>True mientras hay un aviso en pantalla.</summary>
        public bool HayAviso { get; private set; }

        /// <summary>El aviso en pantalla (solo vale si <see cref="HayAviso"/>).</summary>
        public AccionTutorial Aviso { get; private set; }

        /// <summary>Cuántas acciones ya se vieron.</summary>
        public int CantidadVistas => vistas.Count;

        public bool Vista(AccionTutorial accion) => vistas.Contains(accion);

        /// <summary>La acción que hay que ver antes de ofrecer esta (null si no tiene).</summary>
        public static AccionTutorial? Requisito(AccionTutorial accion)
        {
            switch (accion)
            {
                case AccionTutorial.Correr:
                case AccionTutorial.Golpear:
                case AccionTutorial.Apuntar:
                case AccionTutorial.SubirAlCarro:
                case AccionTutorial.Mapa:
                case AccionTutorial.Consumibles:
                case AccionTutorial.Habilidades:
                    return AccionTutorial.Moverse;
                case AccionTutorial.Agacharse:
                    return AccionTutorial.Correr;
                case AccionTutorial.Disparar:
                    return AccionTutorial.Apuntar;
                case AccionTutorial.Recargar:
                case AccionTutorial.Cubrirse:
                    return AccionTutorial.Disparar;
                case AccionTutorial.Rodar:
                    return AccionTutorial.Cubrirse;
                case AccionTutorial.Robar:
                case AccionTutorial.Moto:
                    return AccionTutorial.SubirAlCarro;
                case AccionTutorial.Misiones:
                    return AccionTutorial.Mapa;
                case AccionTutorial.Escudo:
                    return AccionTutorial.Consumibles;
                default:
                    return null;
            }
        }

        /// <summary>Los nombres de las teclas que usan los avisos (T-718): <see cref="Texto(AccionTutorial, Func{string, string})"/> los pide con estos nombres.</summary>
        public const string TeclaMover = "Mover";
        public const string TeclaCorrer = "Correr";
        public const string TeclaSaltar = "Saltar";
        public const string TeclaAgacharse = "Agacharse";
        public const string TeclaCubrirse = "Cubrirse";
        public const string TeclaGolpe = "Atacar";
        public const string TeclaApuntar = "Apuntar";
        public const string TeclaRecargar = "Recargar";
        public const string TeclaInteractuar = "Interactuar";
        public const string TeclaMapa = "Mapa";
        public const string TeclaTelefono = "Telefono";
        public const string TeclaRueda = "Rueda";
        public const string TeclaPiernas = "HabilidadPiernas";
        public const string TeclaBrazos = "HabilidadBrazos";
        public const string TeclaCabeza = "HabilidadCabeza";

        /// <summary>Las teclas de siempre, para cuando no hay Input System que preguntar (pruebas y respaldo).</summary>
        public static string TeclaPorOmision(string nombre)
        {
            switch (nombre)
            {
                case TeclaMover: return "W A S D";
                case TeclaCorrer: return "Shift";
                case TeclaSaltar: return "Espacio";
                case TeclaAgacharse: return "Ctrl";
                case TeclaCubrirse: return "Q";
                case TeclaGolpe: return "Clic";
                case TeclaApuntar: return "Clic der.";
                case TeclaRecargar: return "R";
                case TeclaInteractuar: return "E";
                case TeclaMapa: return "M";
                case TeclaTelefono: return "T";
                case TeclaRueda: return "Tab";
                case TeclaPiernas: return "1";
                case TeclaBrazos: return "2";
                case TeclaCabeza: return "3";
                default: return nombre;
            }
        }

        /// <summary>El texto del aviso con las teclas de siempre.</summary>
        public static string Texto(AccionTutorial accion) => Texto(accion, TeclaPorOmision);

        /// <summary>
        /// El texto del aviso. Las teclas no están escritas aquí: salen de <paramref name="tecla"/>, que contesta con la tecla real (Input System) de cada nombre
        /// (<see cref="TeclaMover"/>, <see cref="TeclaCorrer"/>...). El jugador que cambió una tecla ve la suya.
        /// </summary>
        public static string Texto(AccionTutorial accion, Func<string, string> tecla)
        {
            Func<string, string> k = nombre => (tecla != null ? tecla(nombre) : TeclaPorOmision(nombre)).ToUpperInvariant();
            switch (accion)
            {
                case AccionTutorial.Moverse: return k(TeclaMover) + " TE MUEVEN · " + k(TeclaSaltar) + " SALTA";
                case AccionTutorial.Correr: return "MANTÉN " + k(TeclaCorrer) + " PARA CORRER";
                case AccionTutorial.Agacharse: return k(TeclaAgacharse) + " TE AGACHA · " + k(TeclaAgacharse) + " + " + k(TeclaSaltar) + " RUEDA";
                case AccionTutorial.Golpear: return k(TeclaGolpe) + " GOLPEA";
                case AccionTutorial.Apuntar: return k(TeclaApuntar) + " APUNTA";
                case AccionTutorial.Disparar: return k(TeclaGolpe) + " DISPARA";
                case AccionTutorial.Recargar: return k(TeclaRecargar) + " RECARGA EL ARMA";
                case AccionTutorial.Cubrirse: return k(TeclaCubrirse) + " TE CUBRE DETRÁS DE UNA CAJA O MURO";
                case AccionTutorial.Rodar: return "AGACHADO (" + k(TeclaAgacharse) + ") + " + k(TeclaSaltar) + " RUEDA PARA ESQUIVAR";
                case AccionTutorial.SubirAlCarro: return k(TeclaInteractuar) + " TE SUBE AL CARRO";
                case AccionTutorial.Robar: return "ROBA EL CARRO MARCADO CON " + k(TeclaInteractuar) + " Y LLÉVALO A SALVO";
                case AccionTutorial.Moto: return k(TeclaInteractuar) + " TE SUBE A LA MOTO · " + k(TeclaMover) + " ACELERA, FRENA Y GIRA";
                case AccionTutorial.Mapa: return k(TeclaMapa) + " ABRE EL MAPA";
                case AccionTutorial.Misiones: return k(TeclaTelefono) + " ABRE EL TELÉFONO Y TUS MISIONES";
                case AccionTutorial.Consumibles: return "MANTÉN " + k(TeclaRueda) + " PARA ELEGIR UN ARMA O USAR UN OBJETO";
                case AccionTutorial.Escudo: return "TU ESCUDO ABSORBE BALAS Y SE RECARGA SOLO";
                case AccionTutorial.Habilidades: return TextoHabilidades(tecla, true, true, true);
                default: return string.Empty;
            }
        }

        /// <summary>
        /// T-992: el aviso de las habilidades de prótesis, solo con las que se tienen: "[1] PIERNAS · [2] BRAZOS · [3] CABEZA". Vacío si no hay ninguna.
        /// </summary>
        public static string TextoHabilidades(Func<string, string> tecla, bool piernas, bool brazos, bool cabeza)
        {
            Func<string, string> k = nombre => (tecla != null ? tecla(nombre) : TeclaPorOmision(nombre)).ToUpperInvariant();
            List<string> partes = new List<string>();
            if (piernas) partes.Add("[" + k(TeclaPiernas) + "] PIERNAS");
            if (brazos) partes.Add("[" + k(TeclaBrazos) + "] BRAZOS");
            if (cabeza) partes.Add("[" + k(TeclaCabeza) + "] CABEZA");
            return string.Join(" · ", partes);
        }

        /// <summary>La persona hizo la acción: se da por vista y, si era el aviso en pantalla, se quita.</summary>
        public void Hecha(AccionTutorial accion)
        {
            vistas.Add(accion);
            if (HayAviso && Aviso == accion)
            {
                Quitar();
            }
        }

        /// <summary>
        /// Avanza el reloj. <paramref name="disponible"/> dice si la acción se puede hacer ahora (por ejemplo, hay un carro cerca).
        /// Devuelve true si cambió el aviso (apareció o se quitó).
        /// </summary>
        public bool Avanzar(float dt, Func<AccionTutorial, bool> disponible)
        {
            if (!Activo)
            {
                return QuitarSiHay();
            }
            if (HayAviso)
            {
                if (disponible != null && !disponible(Aviso))
                {
                    // Dejó de poder hacerse (se bajó del carro, abrió el menú): se esconde sin darlo por visto.
                    HayAviso = false;
                    return true;
                }
                restante -= dt;
                if (restante <= 0f)
                {
                    vistas.Add(Aviso);
                    Quitar();
                    return true;
                }
                return false;
            }
            if (pausa > 0f)
            {
                pausa -= dt;
                return false;
            }
            foreach (AccionTutorial accion in Enum.GetValues(typeof(AccionTutorial)))
            {
                if (vistas.Contains(accion) || (disponible != null && !disponible(accion)))
                {
                    continue;
                }
                AccionTutorial? requisito = Requisito(accion);
                if (requisito.HasValue && !vistas.Contains(requisito.Value))
                {
                    continue;
                }
                Aviso = accion;
                HayAviso = true;
                restante = segundosAviso;
                return true;
            }
            return false;
        }

        /// <summary>Lo visto como texto para guardar: nombres separados por comas.</summary>
        public string Serializar()
        {
            List<string> nombres = new List<string>();
            foreach (AccionTutorial accion in Enum.GetValues(typeof(AccionTutorial)))
            {
                if (vistas.Contains(accion))
                {
                    nombres.Add(accion.ToString());
                }
            }
            return string.Join(Separador.ToString(), nombres);
        }

        /// <summary>Carga lo visto de un texto guardado; ignora nombres que no conoce.</summary>
        public void Cargar(string texto)
        {
            vistas.Clear();
            if (string.IsNullOrEmpty(texto))
            {
                return;
            }
            foreach (string parte in texto.Split(Separador))
            {
                if (Enum.TryParse(parte.Trim(), out AccionTutorial accion) && Enum.IsDefined(typeof(AccionTutorial), accion))
                {
                    vistas.Add(accion);
                }
            }
        }

        private void Quitar()
        {
            HayAviso = false;
            pausa = segundosPausa;
        }

        private bool QuitarSiHay()
        {
            if (!HayAviso)
            {
                return false;
            }
            HayAviso = false;
            return true;
        }
    }
}
