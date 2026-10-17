using MechaCyberpunk.Core;

namespace MechaCyberpunk.Progresion.Prologo
{
    /// <summary>Lo que le pasó al jugador y le importa a la <see cref="MaquinaPrologo"/>.</summary>
    public enum AvisoPrologo
    {
        /// <summary>Terminó la pelea del tutorial y la salida del mecha.</summary>
        TutorialTerminado,

        /// <summary>La emboscada se activó.</summary>
        EmboscadaEmpezo,

        /// <summary>La pandilla cayó.</summary>
        EmboscadaTerminada,

        /// <summary>El jugador llegó al objetivo del paso actual de la ruta de regreso.</summary>
        LlegoAPaso,

        /// <summary>El jugador entró a la casa.</summary>
        EntroACasa,
    }

    /// <summary>A dónde apunta el objetivo que se muestra.</summary>
    public enum DestinoObjetivo
    {
        /// <summary>Sin objetivo del director.</summary>
        Ninguno,

        /// <summary>El carro estacionado.</summary>
        Carro,

        /// <summary>Un paso de la ruta de regreso (ver <see cref="ObjetivoPrologo.IndicePaso"/>).</summary>
        PasoRuta,

        /// <summary>El lugar de la casa, al final de la ruta.</summary>
        Casa,

        /// <summary>La cama.</summary>
        Cama,
    }

    /// <summary>El objetivo a mostrar: su texto y a dónde apunta.</summary>
    public readonly struct ObjetivoPrologo
    {
        public ObjetivoPrologo(string texto, DestinoObjetivo destino, int indicePaso)
        {
            Texto = texto;
            Destino = destino;
            IndicePaso = indicePaso;
        }

        public string Texto { get; }
        public DestinoObjetivo Destino { get; }

        /// <summary>Solo vale con <see cref="DestinoObjetivo.PasoRuta"/>.</summary>
        public int IndicePaso { get; }
    }

    /// <summary>
    /// La noche del prólogo como lógica pura (T-132): con la etapa actual y los avisos da la etapa siguiente y el objetivo a mostrar.
    /// Maneja de PeleaTutorial a Dormir; en cualquier otra etapa está inactiva. Un aviso que no toca a la etapa actual se ignora.
    /// </summary>
    public class MaquinaPrologo
    {
        private readonly string textoCarro;
        private readonly string textoRegreso;
        private readonly string textoCama;
        private readonly int pasosRuta;

        public MaquinaPrologo(string textoCarro, string textoRegreso, string textoCama, int pasosRuta)
        {
            this.textoCarro = textoCarro;
            this.textoRegreso = textoRegreso;
            this.textoCama = textoCama;
            this.pasosRuta = pasosRuta < 0 ? 0 : pasosRuta;
            Etapa = EtapaPrologo.Ninguna;
        }

        /// <summary>La etapa con la que se comporta la máquina (puede ir atrás de la guardada al reanudar).</summary>
        public EtapaPrologo Etapa { get; private set; }

        /// <summary>Paso de la ruta de regreso a la vista. Con el valor de la cantidad de pasos, el objetivo es la casa.</summary>
        public int IndicePaso { get; private set; }

        /// <summary>True si la etapa es una de las que maneja la máquina.</summary>
        public bool Activa => EsDeLaNoche(Etapa);

        /// <summary>La emboscada solo está habilitada mientras el jugador sale del clandestino.</summary>
        public bool EmboscadaHabilitada => Etapa == EtapaPrologo.SalidaClandestino;

        /// <summary>True si la etapa pertenece a la noche que maneja el director.</summary>
        public static bool EsDeLaNoche(EtapaPrologo etapa)
        {
            return etapa >= EtapaPrologo.PeleaTutorial && etapa <= EtapaPrologo.Dormir;
        }

        /// <summary>
        /// Cómo se comporta una partida guardada en esa etapa: Emboscada se repite como SalidaClandestino (la etapa guardada no retrocede);
        /// las demás se quedan como están.
        /// </summary>
        public static EtapaPrologo EtapaAlReanudar(EtapaPrologo guardada)
        {
            return guardada == EtapaPrologo.Emboscada ? EtapaPrologo.SalidaClandestino : guardada;
        }

        /// <summary>Empieza en una etapa sin avisar nada. Con <paramref name="reanudar"/> aplica <see cref="EtapaAlReanudar"/>.</summary>
        public void Iniciar(EtapaPrologo etapa, bool reanudar)
        {
            Etapa = reanudar ? EtapaAlReanudar(etapa) : etapa;
            IndicePaso = 0;
        }

        /// <summary>Procesa un aviso. True si la etapa o el paso cambiaron; false si se ignoró por no tocar a la etapa actual.</summary>
        public bool Avisar(AvisoPrologo aviso)
        {
            switch (aviso)
            {
                case AvisoPrologo.TutorialTerminado:
                    return Pasar(EtapaPrologo.PeleaTutorial, EtapaPrologo.SalidaClandestino);
                case AvisoPrologo.EmboscadaEmpezo:
                    return Pasar(EtapaPrologo.SalidaClandestino, EtapaPrologo.Emboscada);
                case AvisoPrologo.EmboscadaTerminada:
                    return Pasar(EtapaPrologo.Emboscada, EtapaPrologo.RegresoACasa);
                case AvisoPrologo.LlegoAPaso:
                    if (Etapa != EtapaPrologo.RegresoACasa || IndicePaso >= pasosRuta)
                    {
                        return false; // en el lugar final no hay "paso": se llega entrando a la casa
                    }
                    IndicePaso++;
                    return true;
                case AvisoPrologo.EntroACasa:
                    return Pasar(EtapaPrologo.RegresoACasa, EtapaPrologo.Dormir);
                default:
                    return false;
            }
        }

        /// <summary>El objetivo que toca mostrar. En Emboscada lo pone la emboscada: aquí no hay (Ninguno).</summary>
        public ObjetivoPrologo ObjetivoActual
        {
            get
            {
                switch (Etapa)
                {
                    case EtapaPrologo.SalidaClandestino:
                        return new ObjetivoPrologo(textoCarro, DestinoObjetivo.Carro, 0);
                    case EtapaPrologo.RegresoACasa:
                        return IndicePaso < pasosRuta
                            ? new ObjetivoPrologo(textoRegreso, DestinoObjetivo.PasoRuta, IndicePaso)
                            : new ObjetivoPrologo(textoRegreso, DestinoObjetivo.Casa, 0);
                    case EtapaPrologo.Dormir:
                        return new ObjetivoPrologo(textoCama, DestinoObjetivo.Cama, 0);
                    default:
                        return new ObjetivoPrologo(string.Empty, DestinoObjetivo.Ninguno, 0);
                }
            }
        }

        private bool Pasar(EtapaPrologo desde, EtapaPrologo hasta)
        {
            if (Etapa != desde)
            {
                return false;
            }
            Etapa = hasta;
            IndicePaso = 0;
            return true;
        }
    }
}
