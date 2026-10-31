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
                case AccionTutorial.Apuntar:
                case AccionTutorial.SubirAlCarro:
                case AccionTutorial.Mapa:
                    return AccionTutorial.Moverse;
                case AccionTutorial.Disparar:
                    return AccionTutorial.Apuntar;
                case AccionTutorial.Recargar:
                case AccionTutorial.Cubrirse:
                    return AccionTutorial.Disparar;
                case AccionTutorial.Rodar:
                    return AccionTutorial.Cubrirse;
                case AccionTutorial.Robar:
                    return AccionTutorial.SubirAlCarro;
                case AccionTutorial.Misiones:
                    return AccionTutorial.Mapa;
                default:
                    return null;
            }
        }

        /// <summary>El texto del aviso, con la tecla de la acción.</summary>
        public static string Texto(AccionTutorial accion)
        {
            switch (accion)
            {
                case AccionTutorial.Moverse: return "W A S D TE MUEVEN · MAYÚS CORRE · ESPACIO SALTA";
                case AccionTutorial.Apuntar: return "CLIC DERECHO APUNTA";
                case AccionTutorial.Disparar: return "CLIC IZQUIERDO DISPARA";
                case AccionTutorial.Recargar: return "R RECARGA EL ARMA";
                case AccionTutorial.Cubrirse: return "Q TE CUBRE DETRÁS DE UNA CAJA O MURO";
                case AccionTutorial.Rodar: return "CTRL RUEDA PARA ESQUIVAR";
                case AccionTutorial.SubirAlCarro: return "E TE SUBE AL CARRO";
                case AccionTutorial.Robar: return "ROBA EL CARRO MARCADO CON E Y LLÉVALO A SALVO";
                case AccionTutorial.Mapa: return "M ABRE EL MAPA";
                case AccionTutorial.Misiones: return "T ABRE EL TELÉFONO Y TUS MISIONES";
                default: return string.Empty;
            }
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
