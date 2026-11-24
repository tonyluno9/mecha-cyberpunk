namespace MechaCyberpunk.Personaje.Visual
{
    /// <summary>T-voces-personajes-23nov: para qué habla un peatón (NPC_VOZn_&lt;MOMENTO&gt;).</summary>
    public enum MomentoVozPeaton
    {
        /// <summary>Disparos, una muerte vista o un atropello cerca: sale corriendo o se tira al piso.</summary>
        Miedo,
        /// <summary>El jugador le pegó y se le pone al tiro (revancha).</summary>
        Enojo,
        /// <summary>Vio un delito y saca el celular para llamar a la policía.</summary>
        Policia,
        /// <summary>El jugador pasa muy cerca (con enfriamiento largo).</summary>
        Saludo,
        /// <summary>Un vendedor del mercado o del tianguis pregona cada tanto.</summary>
        Vendedor,
        /// <summary>Se ríe mientras platica en grupo.</summary>
        Risa
    }
}
