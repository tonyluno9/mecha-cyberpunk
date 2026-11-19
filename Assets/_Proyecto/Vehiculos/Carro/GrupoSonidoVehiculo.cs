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

        public GrupoSonidoVehiculo(string arranque, string ralenti, string apagar, string claxon, float factorTono)
        {
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
                    // Por revisar a oído: solo se usa VEH_ELECTRICO_ARRANQUE; el ralentí y el apagado reciclan el grupo normal con tono alto.
                    return new GrupoSonidoVehiculo("VEH_ELECTRICO_ARRANQUE", "VEH_NORMAL_RALENTI", "VEH_NORMAL_APAGAR", ClaxonNormal, 1.4f);
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
