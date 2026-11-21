namespace MechaCyberpunk.Personaje.Combate
{
    /// <summary>
    /// T-1472: qué ID de sonido pide cada arma y cada evento (disparo, recarga, sacar, lo que suena tras el tiro). Lógica pura, sin Unity,
    /// para que las pruebas la comprueben con un ISonidos falso.
    /// </summary>
    public static class SonidosArmasT1472
    {
        public const string Sacar = "ARM_W06_SACAR";
        public const string CambiarPunos = "ARM_CAMBIAR";
        public const string Bomba = "ARM_W02_BOMBA";
        public const string Cerrojo = "ARM_W04_CERROJO";
        public const string ColaCalle = "ARM_COLA_CALLE";
        public const string PlasmaCarga = "ARM_W07_CARGA";
        public const string PlasmaDisparo = "ARM_W07_DISPARO";
        public const string PlasmaRayo = "ARM_W07_RAYO_LOOP"; // T-1531: antes ARM_W07_RAYO
        // T-1532: el resto del flujo del plasma, la mecánica del revólver, el clic vacío, los casquillos y los misiles.
        public const string PlasmaGatillo = "ARM_W07_GATILLO";
        public const string PlasmaCapaRayo = "ARM_W07_RAYO_CAPA_EXTRA";
        public const string PlasmaEnfriar = "ARM_W07_ENFRIAR";
        public const string PlasmaSinEnergia = "ARM_W07_SIN_ENERGIA";
        public const string LanzagranadasDisparo = "ARM_W08_DISPARO";
        public const string RevolverMecanica = "ARM_W06_MECANICA";
        public const string SinBalas = "ARM_SIN_BALAS";
        public const string CasquilloChico = "CAS_CHICO";
        public const string CasquilloGrande = "CAS_GRANDE";
        public const string MisilesLanzar = "IMP_MISILES_LANZAR";
        public const string GranadaSeguro = "ARM_GRANADA_SEGURO";
        public const string GranadaRebote = "ARM_GRANADA_REBOTE";
        public const string ExplosionGranada = "EXP_GRANADA";
        public const string ExplosionOnda = "EXP_ONDA";
        public const string FuegoChico = "FUE_LOOP_CHICO";
        public const string LaserDisparo = "IMP_LASER_DISPARO";
        public const string MisilesFijar = "IMP_MISILES_FIJAR";
        public const string BalaZumbido = "IMP_BALA_ZUMBIDO";

        /// <summary>El sonido del disparo de cada arma.</summary>
        public static string IdDisparo(ArmaAPie arma)
        {
            switch (arma)
            {
                case ArmaAPie.Escopeta: return "ARM_W02_DISPARO";
                case ArmaAPie.Fusil: return "ARM_W03_DISPARO";
                case ArmaAPie.Sniper: return "ARM_W04_DISPARO";
                case ArmaAPie.Subfusil: return "ARM_SUB_DISPARO"; // T-1471 lo mapea a W01 agudo
                case ArmaAPie.Revolver: return "ARM_W06_DISPARO";
                case ArmaAPie.Lanzagranadas: return LanzagranadasDisparo;
                case ArmaAPie.RiflePlasma: return PlasmaDisparo;
                default: return "ARM_W01_DISPARO";
            }
        }

        /// <summary>El sonido de la recarga de cada arma (donde no hay archivo propio se usa el más parecido).</summary>
        public static string IdRecarga(ArmaAPie arma)
        {
            switch (arma)
            {
                case ArmaAPie.Escopeta: return "ARM_ESC_RECARGA_CARTUCHO";
                case ArmaAPie.Lanzagranadas: return "ARM_ESC_RECARGA_CARTUCHO";
                case ArmaAPie.Fusil: return "ARM_W03_RECARGA_CARGADOR";
                case ArmaAPie.Subfusil: return "ARM_W03_RECARGA_CARGADOR";
                case ArmaAPie.RiflePlasma: return "ARM_W03_RECARGA_CARGADOR";
                case ArmaAPie.Sniper: return "ARM_SNI_RECARGA";
                case ArmaAPie.Revolver: return RevolverMecanica; // T-1532: tambor y martillo
                default: return "ARM_PIS_RECARGA";
            }
        }

        /// <summary>
        /// El sonido al sacar o guardar el arma, o null si no suena nada (los puños). LEEME 20-nov: se usa la recarga de cada arma; solo el revólver
        /// tiene su propio sacar y la granada (que no recarga) usa ese mismo.
        /// </summary>
        public static string IdSacar(ArmaAPie arma)
        {
            if (arma == ArmaAPie.Punos)
            {
                return null;
            }
            return arma == ArmaAPie.Revolver || arma == ArmaAPie.Granada ? Sacar : IdRecarga(arma);
        }

        /// <summary>El casquillo que cae tras el tiro: grande en escopeta y francotirador, chico en el resto; null en el revólver (guarda los casquillos) y en lo que no es de balas.</summary>
        public static string IdCasquillo(ArmaAPie arma)
        {
            switch (arma)
            {
                case ArmaAPie.Escopeta:
                case ArmaAPie.Sniper:
                    return CasquilloGrande;
                case ArmaAPie.Pistola:
                case ArmaAPie.Subfusil:
                case ArmaAPie.Fusil:
                    return CasquilloChico;
                default:
                    return null;
            }
        }

        /// <summary>Lo que suena un poco después de cada tiro (bombeo de la escopeta, cerrojo del francotirador), o null.</summary>
        public static string IdTrasDisparo(ArmaAPie arma)
        {
            switch (arma)
            {
                case ArmaAPie.Escopeta: return Bomba;
                case ArmaAPie.Sniper: return Cerrojo;
                case ArmaAPie.Revolver: return RevolverMecanica; // T-1532: el martillo se arma tras el tiro
                default: return null;
            }
        }

        /// <summary>El disparo de un enemigo armado (pandillero, policía): la escopeta si tira varios perdigones, la pistola si no.</summary>
        public static string IdDisparoEnemigo(int perdigones)
        {
            return perdigones > 1 ? "ARM_W02_DISPARO" : "ARM_W01_DISPARO";
        }
    }

    /// <summary>
    /// T-1472: decide cuándo suena la cola de la calle de una ráfaga automática: una sola vez al soltar el gatillo, al acabarse el cargador
    /// o al cambiar de arma, y solo si de verdad se disparó.
    /// </summary>
    public sealed class ReglaColaRafaga
    {
        private bool disparando;

        public bool Disparando => disparando;

        /// <summary>
        /// Se llama cada frame. <paramref name="disparo"/>: este frame salió una bala. <paramref name="sigue"/>: el gatillo sigue pulsado, con balas
        /// y con el arma de ráfaga. Devuelve true el frame en que debe sonar la cola.
        /// </summary>
        public bool Avanzar(bool disparo, bool sigue)
        {
            if (disparo)
            {
                disparando = true;
                return false;
            }
            if (disparando && !sigue)
            {
                disparando = false;
                return true;
            }
            return false;
        }
    }

    /// <summary>
    /// T-1472: tope de sonidos de explosión en cadena: no más de <see cref="MaxEnVentana"/> dentro de <see cref="Ventana"/> segundos y al menos
    /// <see cref="Separacion"/> segundos entre una y otra, para que diez explosiones del mismo frame no se sumen en un solo estruendo.
    /// </summary>
    public sealed class LimiteExplosionesSonido
    {
        public const int MaxEnVentana = 3;
        public const float Ventana = 0.6f;
        public const float Separacion = 0.08f;

        private readonly float[] tiempos = new float[MaxEnVentana];
        private int cuantos;

        /// <summary>true si esta explosión puede sonar ahora (y la cuenta); false si se omite.</summary>
        public bool Permite(float ahora)
        {
            int vivos = 0;
            float ultimo = float.NegativeInfinity;
            for (int i = 0; i < cuantos; i++)
            {
                if (ahora - tiempos[i] < Ventana)
                {
                    tiempos[vivos++] = tiempos[i];
                    ultimo = tiempos[i] > ultimo ? tiempos[i] : ultimo;
                }
            }
            cuantos = vivos;
            if (cuantos >= MaxEnVentana || ahora - ultimo < Separacion)
            {
                return false;
            }
            tiempos[cuantos++] = ahora;
            return true;
        }

        public void Reiniciar() => cuantos = 0;
    }
}
