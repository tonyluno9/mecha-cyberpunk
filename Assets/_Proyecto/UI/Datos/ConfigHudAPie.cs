using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.UI
{
    /// <summary>Colores, textos, tamaños y márgenes del HUD a pie.</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Config HUD a pie", fileName = "ConfigHudAPie_Nueva")]
    public class ConfigHudAPie : ScriptableObject
    {
        [Tooltip("Color de la barra de vida (turquesa neón).")]
        [SerializeField] private Color colorVida = new Color(0f, 1f, 0.9f, 1f);

        [Tooltip("Color de la barra de vida con vida baja (magenta neón).")]
        [SerializeField] private Color colorVidaBaja = new Color(1f, 0.1f, 0.6f, 1f);

        [Tooltip("Color del texto principal (arma, canción y mira).")]
        [SerializeField] private Color colorTexto = new Color(0.55f, 0.85f, 1f, 1f);

        [Tooltip("Color de la retícula cuando hay un enemigo en la mira (T-424; rojo neón).")]
        [SerializeField] private Color colorMiraBlanco = new Color(1f, 0.15f, 0.2f, 1f);

        [Tooltip("Color de los avisos (naranja neón): arma recargando o sin balas.")]
        [SerializeField] private Color colorAviso = new Color(1f, 0.55f, 0.1f, 1f);

        [Tooltip("Color del hueco de la barra de vida.")]
        [SerializeField] private Color colorHueco = new Color(0.05f, 0.08f, 0.12f, 0.6f);

        [Tooltip("Debajo de esta parte de la vida máxima la barra usa el color de vida baja (de 0 a 1).")]
        [Range(0f, 1f)] [SerializeField] private float umbralVidaBaja = 0.3f;

        [Tooltip("Texto de la canción mientras la radio no avisa ninguna.")]
        [SerializeField] private string textoCancionEspera = "RADIO · sin señal";

        [Tooltip("Tamaño de la letra del texto de vida, arma y canción.")]
        [Min(8)] [SerializeField] private int tamanoTexto = 22;

        [Tooltip("Margen del HUD al borde de la pantalla (px de referencia).")]
        [Min(0f)] [SerializeField] private float margen = 36f;

        [Tooltip("Ancho de la barra de vida (px de referencia).")]
        [Min(10f)] [SerializeField] private float anchoBarra = 260f;

        [Tooltip("Alto de la barra de vida (px de referencia).")]
        [Min(2f)] [SerializeField] private float altoBarra = 12f;

        [Tooltip("Largo de cada brazo de la cruz de la mira (px de referencia).")]
        [Min(1f)] [SerializeField] private float largoMira = 14f;

        [Header("Objetivo e interacción (T-057)")]
        [Tooltip("Color del aviso REGRESA mientras el jugador se aleja (magenta neón).")]
        [SerializeField] private Color colorRegresa = new Color(1f, 0.1f, 0.6f, 1f);

        [Tooltip("Cuánto dura el aviso de por qué no abrió una puerta (s).")]
        [Min(0.1f)] [SerializeField] private float duracionAvisoPuerta = 2f;

        [Tooltip("Texto de la E con una puerta de frente; después va el nombre del lugar.")]
        [SerializeField] private string textoAbrir = "E · Abrir";

        [Tooltip("Texto de la E con un carro o mecha de frente; después va su nombre.")]
        [SerializeField] private string textoSubir = "E · Subir";

        [Tooltip("Prefijo de la E con un interactuable que se puede usar; después va su texto.")]
        [SerializeField] private string prefijoUsar = "E · ";

        [Tooltip("Aviso al pulsar E para bajar cuando el vehículo todavía va rápido.")]
        [SerializeField] private string textoFrenaParaBajar = "Frena para bajar";

        [Tooltip("Aviso del objetivo cuando el jugador se aleja.")]
        [SerializeField] private string textoRegresa = "REGRESA";

        [Tooltip("Aviso de una puerta cerrada sin horario que la abra.")]
        [SerializeField] private string textoCerradoProximamente = "CERRADO · PRÓXIMAMENTE";

        [Tooltip("Aviso de una puerta cerrada por horario; {0} es la hora de apertura.")]
        [SerializeField] private string textoCerradoAbre = "CERRADO · abre {0}";

        [Header("Llamado del mecha (T-066)")]
        [Tooltip("Aviso mientras el mecha viene en camino; {0} son los segundos que faltan.")]
        [SerializeField] private string textoMechaEnCamino = "MECHA EN CAMINO · {0}";

        [Tooltip("Aviso al llamar al mecha fuera de toda zona permitida.")]
        [SerializeField] private string textoZonaProhibida = "AQUÍ NO PUEDE LLEGAR TU MECHA";

        [Tooltip("Aviso mientras el mecha vuelve al taller.")]
        [SerializeField] private string textoMechaRegresa = "TU MECHA REGRESA AL TALLER";

        [Tooltip("Cuánto dura el aviso de zona prohibida (s).")]
        [Min(0.1f)] [SerializeField] private float duracionAvisoLlamado = 2.5f;

        [Header("Calor policial (T-067)")]
        [Tooltip("Texto del grupo de calor.")]
        [SerializeField] private string textoCalor = "CALOR";

        [Tooltip("Color de las marcas llenas y del texto de calor (naranja neón).")]
        [SerializeField] private Color colorCalor = new Color(1f, 0.55f, 0.1f, 1f);

        [Tooltip("Color de las marcas de calor que todavía no se llenan.")]
        [SerializeField] private Color colorMarcaCalorApagada = new Color(0.3f, 0.2f, 0.1f, 0.5f);

        [Tooltip("Ancho de cada marca de calor (px de referencia).")]
        [Min(2f)] [SerializeField] private float anchoMarcaCalor = 28f;

        [Tooltip("Alto de cada marca de calor (px de referencia).")]
        [Min(2f)] [SerializeField] private float altoMarcaCalor = 12f;

        [Header("Dinero y renta (T-110)")]
        [Tooltip("Color del dinero cuando es negativo (deuda; magenta neón).")]
        [SerializeField] private Color colorDeuda = new Color(1f, 0.1f, 0.6f, 1f);

        [Tooltip("Cuánto dura el aviso de un movimiento de dinero (s).")]
        [Min(0.1f)] [SerializeField] private float segundosAvisoDinero = 2.5f;

        [Tooltip("Aviso de renta vencida; {0} es lo que se debe.")]
        [SerializeField] private string textoRentaVencida = "RENTA VENCIDA · {0}";

        [Tooltip("Nombre de cada motivo en el aviso, en el orden de MotivoDinero (Premio, Apuesta, Reparacion, Renta, Compra, Venta, Casino, Clinica, Otro).")]
        [SerializeField] private string[] nombresMotivo =
            { "PREMIO", "APUESTA", "REPARACIÓN", "RENTA", "COMPRA", "VENTA", "VIVACOIN", "I+D+I", "OTRO" };

        [Header("Subir de liga (T-161)")]
        [Tooltip("Cuánto dura el aviso de campeón vencido (s).")]
        [Min(0.1f)] [SerializeField] private float segundosAvisoLiga = 5f;

        [Tooltip("Primera línea al vencer al campeón de una liga; {0} es el nombre de esa liga.")]
        [SerializeField] private string textoCampeonLiga = "¡CAMPEÓN DE LA LIGA {0}!";

        [Tooltip("Segunda línea: la liga que se abre; {0} es su nombre.")]
        [SerializeField] private string textoSeAbreLiga = "SE ABRE LA LIGA {0}";

        [Tooltip("Aviso único al vencer al campeón de la última liga.")]
        [SerializeField] private string textoCampeonClandestino = "¡CAMPEÓN DEL CLANDESTINO!";

        [Tooltip("Color del aviso de liga (amarillo neón).")]
        [SerializeField] private Color colorAvisoLiga = new Color(1f, 0.85f, 0.2f, 1f);

        [Tooltip("Tamaño de la letra de la primera línea del aviso de liga.")]
        [Min(8)] [SerializeField] private int tamanoAvisoLiga = 56;

        [Tooltip("Tamaño de la letra de la segunda línea del aviso de liga.")]
        [Min(8)] [SerializeField] private int tamanoAvisoLiga2 = 32;

        [Header("Textos de vida y arma")]
        [Tooltip("Texto de la vida; {0} es la vida redondeada hacia arriba.")]
        [SerializeField] private string textoVida = "VIDA {0}";

        [Tooltip("Texto del arma con los puños.")]
        [SerializeField] private string textoPunos = "PUÑOS";

        [Tooltip("Texto de la pistola; {0} son las balas y {1} el tamaño del cargador.")]
        [SerializeField] private string textoPistola = "PISTOLA {0}/{1}";

        [Tooltip("Texto de la pistola mientras recarga.")]
        [SerializeField] private string textoPistolaRecargando = "PISTOLA · RECARGANDO";

        [Tooltip("Lo que se agrega al texto de la pistola cuando no le quedan balas.")]
        [SerializeField] private string textoPistolaSinBalas = " · R";

        [Header("Reputación y misiones (T-186)")]
        [Tooltip("Cuánto dura el aviso de un cambio de reputación (s).")]
        [Min(0.1f)] [SerializeField] private float segundosAvisoReputacion = 3f;

        [Tooltip("Parte del aviso del barrio; {0} es el cambio con signo.")]
        [SerializeField] private string textoReputacionBarrio = "BARRIO {0}";

        [Tooltip("Parte del aviso de las corporaciones; {0} es el cambio con signo.")]
        [SerializeField] private string textoReputacionCorporaciones = "CORPORACIONES {0}";

        [Tooltip("Lo que une las partes de un aviso.")]
        [SerializeField] private string separadorAviso = " · ";

        [Tooltip("Segunda línea al cambiar de nivel en el barrio; {0} es el nombre del nivel nuevo.")]
        [SerializeField] private string textoNivelBarrio = "AHORA ERES {0} EN EL BARRIO";

        [Tooltip("Segunda línea al cambiar de nivel con las corporaciones; {0} es el nombre del nivel nuevo.")]
        [SerializeField] private string textoNivelCorporaciones = "AHORA ERES {0} CON LAS CORPORACIONES";

        [Tooltip("Color del aviso de reputación (amarillo).")]
        [SerializeField] private Color colorAvisoReputacion = new Color(1f, 0.85f, 0.2f, 1f);

        [Tooltip("Tamaño de la letra de la primera línea del aviso de reputación.")]
        [Min(8)] [SerializeField] private int tamanoAvisoReputacion = 34;

        [Tooltip("Tamaño de la letra de la segunda línea del aviso de reputación.")]
        [Min(8)] [SerializeField] private int tamanoAvisoReputacion2 = 26;

        [Tooltip("Cuánto dura el aviso de misión terminada (s).")]
        [Min(0.1f)] [SerializeField] private float segundosAvisoMision = 4f;

        [Tooltip("Aviso al cumplir una misión; después van el pago y la recompensa.")]
        [SerializeField] private string textoMisionCumplida = "MISIÓN CUMPLIDA";

        [Tooltip("Aviso al fallar o abandonar una misión.")]
        [SerializeField] private string textoMisionFallida = "MISIÓN FALLIDA";

        [Tooltip("Tamaño de la letra del aviso de misión terminada.")]
        [Min(8)] [SerializeField] private int tamanoAvisoMision = 40;

        [Tooltip("Nombre del motivo de dinero Mision en el aviso de dinero.")]
        [SerializeField] private string nombreMotivoMision = "MISIÓN";

        [Tooltip("Lo que sigue a la cantidad de chatarra de una misión cumplida; {0} es la cantidad y {1} el nombre del objeto.")]
        [SerializeField] private string textoChatarraMision = "+{0} {1}";

        [Header("OJOS a pie (T-195)")]
        [Tooltip("Distancia máxima a la que se lee al pandillero en la mira (m).")]
        [Min(1f)] [SerializeField] private float distanciaLecturaRival = 15f;

        [Tooltip("A qué distancia del rayo de la mira cuenta que lo miras (m).")]
        [Min(0.05f)] [SerializeField] private float radioMiraRival = 0.7f;

        [Tooltip("Altura del centro del pandillero sobre sus pies, para apuntarle (m).")]
        [Min(0f)] [SerializeField] private float alturaCentroRival = 0.9f;

        [Tooltip("Ancho de la barra de vida del rival (px de referencia).")]
        [Min(10f)] [SerializeField] private float anchoBarraRival = 200f;

        [Tooltip("Alto de la barra de vida del rival (px de referencia).")]
        [Min(2f)] [SerializeField] private float altoBarraRival = 8f;

        [Tooltip("Cuánto sube la barra del rival sobre el centro de la mira (px de referencia).")]
        [Min(0f)] [SerializeField] private float alturaBarraRival = 90f;

        [Tooltip("Texto de la vida del rival con OJOS II; {0} es la vida y {1} la máxima.")]
        [SerializeField] private string formatoVidaRival = "{0:0} / {1:0}";

        [Tooltip("Aviso junto a la mira cuando el rival va a golpear.")]
        [SerializeField] private string textoGolpeRival = "¡GOLPE!";

        [Tooltip("Tamaño de la letra del aviso de golpe.")]
        [Min(8)] [SerializeField] private int tamanoTextoGolpeRival = 32;

        [Tooltip("Cuánto a la derecha de la mira queda el aviso de golpe (px de referencia).")]
        [Min(0f)] [SerializeField] private float desplazamientoGolpeRival = 110f;

        public float DistanciaLecturaRival => distanciaLecturaRival;
        public float RadioMiraRival => radioMiraRival;
        public float AlturaCentroRival => alturaCentroRival;
        public float AnchoBarraRival => anchoBarraRival;
        public float AltoBarraRival => altoBarraRival;
        public float AlturaBarraRival => alturaBarraRival;
        public string FormatoVidaRival => formatoVidaRival;
        public string TextoGolpeRival => textoGolpeRival;
        public int TamanoTextoGolpeRival => tamanoTextoGolpeRival;
        public float DesplazamientoGolpeRival => desplazamientoGolpeRival;

        public float SegundosAvisoReputacion => segundosAvisoReputacion;
        public string TextoReputacionBarrio => textoReputacionBarrio;
        public string TextoReputacionCorporaciones => textoReputacionCorporaciones;
        public string SeparadorAviso => separadorAviso;
        public string TextoNivelBarrio => textoNivelBarrio;
        public string TextoNivelCorporaciones => textoNivelCorporaciones;
        public Color ColorAvisoReputacion => colorAvisoReputacion;
        public int TamanoAvisoReputacion => tamanoAvisoReputacion;
        public int TamanoAvisoReputacion2 => tamanoAvisoReputacion2;
        public float SegundosAvisoMision => segundosAvisoMision;
        public string TextoMisionCumplida => textoMisionCumplida;
        public string TextoMisionFallida => textoMisionFallida;
        public int TamanoAvisoMision => tamanoAvisoMision;
        public string TextoChatarraMision => textoChatarraMision;
        public float SegundosAvisoLiga => segundosAvisoLiga;
        public string TextoCampeonLiga => textoCampeonLiga;
        public string TextoSeAbreLiga => textoSeAbreLiga;
        public string TextoCampeonClandestino => textoCampeonClandestino;
        public Color ColorAvisoLiga => colorAvisoLiga;
        public int TamanoAvisoLiga => tamanoAvisoLiga;
        public int TamanoAvisoLiga2 => tamanoAvisoLiga2;
        public Color ColorDeuda => colorDeuda;
        public float SegundosAvisoDinero => segundosAvisoDinero;
        public string TextoRentaVencida => textoRentaVencida;

        /// <summary>Nombre del motivo en el aviso; si la lista no lo trae, el nombre del enum en mayúsculas.</summary>
        public string NombreMotivo(MotivoDinero motivo)
        {
            if (motivo == MotivoDinero.Mision)
            {
                return nombreMotivoMision;
            }
            int indice = (int)motivo;
            if (nombresMotivo != null && indice >= 0 && indice < nombresMotivo.Length && !string.IsNullOrEmpty(nombresMotivo[indice]))
            {
                return nombresMotivo[indice];
            }
            return motivo.ToString().ToUpperInvariant();
        }

        public string TextoCalor => textoCalor;
        public Color ColorCalor => colorCalor;
        public Color ColorMarcaCalorApagada => colorMarcaCalorApagada;
        public float AnchoMarcaCalor => anchoMarcaCalor;
        public float AltoMarcaCalor => altoMarcaCalor;
        public string TextoMechaEnCamino => textoMechaEnCamino;
        public string TextoZonaProhibida => textoZonaProhibida;
        public string TextoMechaRegresa => textoMechaRegresa;
        public float DuracionAvisoLlamado => duracionAvisoLlamado;
        public Color ColorRegresa => colorRegresa;
        public float DuracionAvisoPuerta => duracionAvisoPuerta;
        public string TextoVida => textoVida;
        public string TextoPunos => textoPunos;
        public string TextoPistola => textoPistola;
        public string TextoPistolaRecargando => textoPistolaRecargando;
        public string TextoPistolaSinBalas => textoPistolaSinBalas;
        public string TextoAbrir => textoAbrir;
        public string TextoSubir => textoSubir;
        public string PrefijoUsar => prefijoUsar;
        public string TextoFrenaParaBajar => textoFrenaParaBajar;
        public string TextoRegresa => textoRegresa;
        public string TextoCerradoProximamente => textoCerradoProximamente;
        public string TextoCerradoAbre => textoCerradoAbre;

        public Color ColorVida => colorVida;
        public Color ColorVidaBaja => colorVidaBaja;
        public Color ColorTexto => colorTexto;
        public Color ColorMiraBlanco => colorMiraBlanco;
        public Color ColorAviso => colorAviso;
        public Color ColorHueco => colorHueco;
        public float UmbralVidaBaja => umbralVidaBaja;
        public string TextoCancionEspera => textoCancionEspera;
        public int TamanoTexto => tamanoTexto;
        public float Margen => margen;
        public float AnchoBarra => anchoBarra;
        public float AltoBarra => altoBarra;
        public float LargoMira => largoMira;
    }
}
