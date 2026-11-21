using System;

namespace MechaCyberpunk.Vehiculos
{
    /// <summary>T-1475: tipo de vehículo para elegir su grupo de sonidos de motor.</summary>
    public enum TipoVehiculoSonido
    {
        Normal,
        Moto,
        MotoGrande,
        Pesado,
        Superdeportivo,
        Electrico,
    }

    /// <summary>
    /// T-1475: IDs de arranque, ralentí, apagado y claxon de cada tipo de vehículo, y el factor de tono base (reciclar un grupo con otro tono).
    /// Pura: sin Unity.
    /// </summary>
    public readonly struct GrupoSonidoVehiculo
    {
        public const string ClaxonNormal = "VEH_CLAXON_NORMAL";
        public const string ClaxonCamion = "VEH_CLAXON_CAMION";

        /// <summary>Masa (kg) desde la que un vehículo cuenta como pesado.</summary>
        public const float MasaPesado = 2500f;

        /// <summary>Masa (kg) desde la que una moto cuenta como grande.</summary>
        public const float MasaMotoGrande = 300f;

        public readonly string Arranque;
        public readonly string Ralenti;
        public readonly string Apagar;
        public readonly string Claxon;
        public readonly float FactorTono;
        /// <summary>T-1534: bucle en movimiento (solo el eléctrico: sin ralentí, parado va en silencio). Nulo si el tipo usa ralentí.</summary>
        public readonly string Mover;
        /// <summary>T-1534: sonido de pisar el acelerador del eléctrico (el catálogo elige _01 o _02). Nulo si no es eléctrico.</summary>
        public readonly string Acelerar;
        /// <summary>T-1534: sonido de pasar a buena velocidad (eléctrico). Nulo si no es eléctrico.</summary>
        public readonly string Pasar;

        /// <summary>True si es un eléctrico (turbinas): sin ralentí, MOVER en movimiento y silencio parado.</summary>
        public bool EsElectrico => Mover != null;

        public GrupoSonidoVehiculo(string arranque, string ralenti, string apagar, string claxon, float factorTono, string mover = null, string acelerar = null, string pasar = null)
        {
            Mover = mover;
            Acelerar = acelerar;
            Pasar = pasar;
            Arranque = arranque;
            Ralenti = ralenti;
            Apagar = apagar;
            Claxon = claxon;
            FactorTono = factorTono;
        }

        /// <summary>El grupo de sonidos del tipo.</summary>
        public static GrupoSonidoVehiculo Para(TipoVehiculoSonido tipo)
        {
            switch (tipo)
            {
                case TipoVehiculoSonido.Moto:
                    return new GrupoSonidoVehiculo("MOTO_ARRANQUE", "MOTO_RALENTI", "MOTO_APAGAR", ClaxonNormal, 1f);
                case TipoVehiculoSonido.MotoGrande:
                    return new GrupoSonidoVehiculo("MOTO_ARRANQUE", "MOTO_RALENTI", "MOTO_APAGAR", ClaxonNormal, 0.8f);
                case TipoVehiculoSonido.Pesado:
                    return new GrupoSonidoVehiculo("VEH_PESADO_ARRANQUE", "VEH_PESADO_RALENTI", "VEH_PESADO_APAGAR", ClaxonCamion, 1f);
                case TipoVehiculoSonido.Superdeportivo:
                    return new GrupoSonidoVehiculo("VEH_NORMAL_ARRANQUE", "VEH_NORMAL_RALENTI", "VEH_NORMAL_APAGAR", ClaxonNormal, 1.25f);
                case TipoVehiculoSonido.Electrico:
                    // T-1534 (LEEME_SONIDOS): turbinas sin ralentí ni apagado propio. ARRANQUE solo al encender, ACELERAR al pisar, MOVER y PASAR en movimiento
                    // subiendo el tono con la rapidez; parado, en silencio.
                    return new GrupoSonidoVehiculo("VEH_ELECTRICO_ARRANQUE", null, null, ClaxonNormal, 1f, "VEH_ELECTRICO_MOVER", "VEH_ELECTRICO_ACELERAR", "VEH_ELECTRICO_PASAR");
                default:
                    return new GrupoSonidoVehiculo("VEH_NORMAL_ARRANQUE", "VEH_NORMAL_RALENTI", "VEH_NORMAL_APAGAR", ClaxonNormal, 1f);
            }
        }

        /// <summary>Adivina el tipo por si es moto, la masa y el nombre del modelo (sin tocar prefabs).</summary>
        public static TipoVehiculoSonido Detectar(bool esMoto, float masa, string nombreModelo)
        {
            string n = (nombreModelo ?? "").ToLowerInvariant();
            if (esMoto)
            {
                return masa >= MasaMotoGrande || n.Contains("grande") ? TipoVehiculoSonido.MotoGrande : TipoVehiculoSonido.Moto;
            }
            if (masa >= MasaPesado || n.Contains("camion") || n.Contains("camión") || n.Contains("tráiler") || n.Contains("trailer"))
            {
                return TipoVehiculoSonido.Pesado;
            }
            if (n.Contains("eléctric") || n.Contains("electric"))
            {
                return TipoVehiculoSonido.Electrico;
            }
            if (n.Contains("deportivo"))
            {
                return TipoVehiculoSonido.Superdeportivo;
            }
            return TipoVehiculoSonido.Normal;
        }
    }
}
