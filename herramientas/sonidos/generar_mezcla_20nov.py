"""Genera la mezcla de los IDs nuevos del 20-nov (T-1531) a partir de los niveles medidos.
Uso: python herramientas/sonidos/generar_mezcla_20nov.py <medidas.csv de medir_niveles_20nov.py>
Salidas:
  herramientas/sonidos/niveles-20nov.csv                          (RMS/LUFS medidos, objetivo y volumen asignado por ID)
  Assets/_Proyecto/Progresion/Sonido/TablaMezcla20nov.cs          (la politica de cada ID nuevo: grupo, voces, volumen, distancia, tono...)
Los WAV de las carpetas 01..18 se importan con normalize: 1 (Unity sube el pico de cada clip a 0 dBFS), asi que el nivel que se oye es el
nivel medido MENOS el pico: lufs_norm = lufs - pico. Volumen = 10^((objetivo - lufs_norm)/20) / ganancia de la seccion, entre 0.02 y 1.
Si ni con 1 llega al objetivo queda en 1 y la columna falta_db dice cuanto falta."""
import csv, math, re, sys

GANANCIA = {'Efectos': 1.0, 'Voces': 0.85, 'Ambiente': 1.0, 'Musica': 0.5, 'Interfaz': 0.6}
DISPARO = (0.025, 0.05)  # variacion de tono y de volumen del disparo (T-676 relajada en T-1471)

# id: (grupo, voces, distancia_m, objetivo_lufs, tono, vvol, minimo_s, bucles)   distancia 0 = 2D
R = {}


def r(id_, grupo, voces, dist, obj, tono=0.04, vvol=0.06, minimo=0.1, bucles=0):
    R[id_] = (grupo, voces, dist, obj, tono, vvol, minimo, bucles)


# --- 02_Armas
r('ARM_SIN_BALAS', 'arma', 1, 30, -21, 0.03, 0.05, 0.1)
r('ARM_W06_MECANICA', 'arma-accesorio', 2, 30, -23, 0.03, 0.08, 0.05)
r('ARM_W07_GATILLO', 'arma', 1, 60, -17, 0.03, 0.05, 0.1)
r('ARM_W07_ENFRIAR', 'arma-accesorio', 1, 40, -22, 0.03, 0.05, 0.1)
r('ARM_W07_RAYO_LOOP', 'plasma-rayo', 1, 80, -19, 0.0, 0.02, 0.1, 1)
r('ARM_W07_RAYO_CAPA_EXTRA', 'plasma-rayo-capa', 1, 80, -26, 0.0, 0.02, 0.1, 1)
r('ARM_W07_SIN_ENERGIA', 'arma', 1, 40, -19, 0.03, 0.05, 0.2)
r('ARM_W08_DISPARO', 'arma', 1, 100, -17, DISPARO[0], DISPARO[1], 0.0)
r('CAS_CHICO', 'casquillo', 3, 12, -31, 0.10, 0.15, 0.04)
r('CAS_GRANDE', 'casquillo', 3, 12, -31, 0.10, 0.15, 0.04)
r('IMP_MISILES_LANZAR', 'misiles', 1, 120, -16, 0.03, 0.05, 0.15)
# --- 04_Explosiones
r('DES_CRISTAL_VENTANA', 'cristal-rotura', 2, 70, -20, 0.05, 0.08, 0.2)
r('EXP_BARRIL', 'explosion', 2, 170, -16, 0.04, 0.05, 0.1)
r('EXP_CARRO', 'explosion', 2, 200, -13.5, 0.04, 0.05, 0.1)
r('EXP_NAVE', 'explosion', 2, 200, -14.5, 0.04, 0.05, 0.1)
r('EXP_DRON_ROBOT', 'explosion', 2, 120, -17, 0.05, 0.06, 0.1)
r('EXP_CRISTALES', 'explosion-detalle', 2, 80, -21, 0.05, 0.08, 0.2)
r('EXP_ESCOMBROS', 'explosion-detalle', 2, 80, -24, 0.05, 0.08, 0.2)
r('FUE_ENCENDER', 'fuego-encender', 2, 50, -21, 0.04, 0.06, 0.3)
r('FUE_LOOP_GRANDE', 'fuego', 3, 60, -31, 0.0, 0.05, 0.5, 3)
# --- 06_Jugador
r('JUG_LATIDO_VIDA_BAJA', 'latido', 1, 0, -24, 0.02, 0.05, 1.0)
r('JUG_ROPA_ROCE', 'jugador-roce', 1, 12, -32, 0.06, 0.10, 0.4)
r('JUG_ZUMBIDO_EXPLOSION', 'zumbido-oido', 1, 0, -27, 0.02, 0.05, 5.0)
r('MEL_BLOQUEO', 'melee', 2, 30, -17, 0.05, 0.06, 0.1)
r('MEL_GOLPE_METAL', 'melee', 2, 30, -17, 0.05, 0.06, 0.1)
# --- 07, 08, 09
r('INT_RECOGER_DINERO', 'dinero', 1, 20, -28, 0.05, 0.06, 0.25)
r('CON_PAPITAS_ABRIR', 'consumible', 1, 0, -29, 0.03, 0.05, 0.3)
r('RAD_CAMBIAR_ESTACION', 'radio-auto', 1, 0, -30, 0.04, 0.05, 0.3)
# --- 10_Policia
r('AMB_SIRENA_LOOP', 'ambulancia', 1, 120, -33, 0.0, 0.02, 1.0, 2)
r('NAV_DANIADA', 'nave', 1, 90, -17, 0.04, 0.06, 1.0)
r('NAV_REFLECTOR', 'nave', 1, 50, -27, 0.03, 0.05, 1.0)
r('NAV_ROTORES_LOOP', 'nave-rotores', 1, 90, -26, 0.0, 0.02, 0.5, 3)
r('POL_ESTRELLA_SUBE', 'calor', 1, 0, -22, 0.02, 0.05, 1.0)
r('POL_RADIO_ESTATICA', 'radio-estatica', 1, 25, -27, 0.03, 0.06, 1.0)
r('POL_SIRENA_MURO', 'sirena-muro', 1, 250, -22, 0.0, 0.02, 2.0, 1)
# --- 11_Nova
r('NOV_ARMADURA_MOVER', 'nova-armadura', 3, 20, -29, 0.06, 0.10, 0.1)
r('NOV_CAIDA_Y_ENCENDIDO', 'nova-voz', 1, 80, -14, 0.03, 0.05, 3.0)
r('NOV_ESCUDO_ACTIVAR', 'nova-escudo', 1, 35, -19, 0.03, 0.06, 0.3)
r('NOV_ESCUDO_IMPACTO', 'nova-escudo-impacto', 3, 35, -18, 0.05, 0.08, 0.08)
r('NOV_JETPACK_LOOP', 'nova-jetpack', 1, 50, -24, 0.0, 0.03, 0.5, 1)
r('NOV_PASOS_CAMINAR', 'nova-paso', 2, 28, -24, 0.06, 0.10, 0.12)
r('NOV_PASOS_CORRER', 'nova-paso', 2, 30, -23, 0.06, 0.10, 0.1)
# --- 12_NPCs, 13_Robots
r('NPC_H_DOLOR', 'voz-npc', 2, 30, -18, 0.08, 0.08, 0.15)
r('NPC_M_DOLOR', 'voz-npc', 2, 30, -18, 0.08, 0.08, 0.15)
r('DRN_PES_AMETRALLADORA', 'dron-disparo', 3, 100, -15, 0.03, 0.06, 0.05)
r('ROB_CHISPAS_DANIO', 'rob-chispas', 2, 25, -24, 0.05, 0.08, 0.2)
r('ROB_ENCENDER', 'rob-encender', 1, 35, -23, 0.04, 0.06, 0.5)
r('ROB_MUERTE', 'rob-muerte', 2, 40, -20, 0.04, 0.06, 0.2)
r('ROB_PERRO_GRUNIDO', 'rob-perro', 1, 30, -21, 0.05, 0.08, 1.0)
r('ROB_PERRO_MOVER', 'rob-paso', 2, 25, -27, 0.06, 0.10, 0.15)
r('ROB_PES_HIDRAULICA', 'rob-hidraulica', 1, 40, -24, 0.04, 0.06, 0.8)
r('ROB_PES_LLEGADA', 'rob-llegada', 1, 120, -14, 0.03, 0.05, 3.0)
r('ROB_PES_PLACA_CAE', 'rob-placa', 1, 50, -20, 0.04, 0.06, 0.5)
r('ROB_SERVOS', 'rob-servos', 2, 25, -27, 0.06, 0.08, 0.15)
r('ROB_VOZ_ALERTA', 'rob-voz', 1, 45, -18, 0.04, 0.06, 2.5)
# --- 14_Ambiente: camas de zona 2D (cruce de 1 s del gestor), clima, interiores 15 m, objetos 6-12 m
r('AMB_AZOTEA_CIUDAD', 'ambiente-zona', 1, 0, -44, 0.0, 0.0, 0.0, 1)
r('AMB_LOMAS', 'ambiente-zona', 1, 0, -44, 0.0, 0.0, 0.0, 1)
r('AMB_NOVA', 'ambiente-zona', 1, 0, -44, 0.0, 0.0, 0.0, 1)
r('AMB_SUBURBIOS_NOCHE', 'ambiente-zona', 1, 0, -44, 0.0, 0.0, 0.0, 1)
r('CLI_LLUVIA_FUERTE', 'clima', 1, 0, -38, 0.0, 0.02, 0.0, 1)
r('CLI_TORMENTA_TRUENOS', 'clima', 1, 0, -38, 0.0, 0.02, 0.0, 1)
r('CLI_VIENTO', 'clima-viento', 1, 0, -42, 0.0, 0.02, 0.0, 1)
r('CLI_LLUVIA_SOBRE_LAMINA', 'clima-lamina', 1, 15, -40, 0.0, 0.02, 0.0, 1)
r('CLI_LLUVIA_DENTRO_CARRO', 'clima-carro', 1, 0, -42, 0.0, 0.02, 0.0, 1)
r('INT_TALLER_CANEPA', 'interior', 1, 15, -38, 0.0, 0.02, 0.0, 1)
r('INT_TIENDA_TINO_RADIO', 'interior-tino-radio', 1, 15, -42, 0.0, 0.02, 0.0, 1)
r('INT_TIENDA_TINO_VENTILADOR', 'interior-tino-ventilador', 1, 15, -42, 0.0, 0.02, 0.0, 1)
r('INT_ARMERIA_VENTILADOR', 'interior', 1, 15, -41, 0.0, 0.02, 0.0, 1)
r('INT_BODEGA_GOTERAS', 'interior-goteras', 1, 15, -44, 0.0, 0.02, 0.0, 1)
r('OBJ_TRANSFORMADOR', 'objeto-zumbido', 2, 12, -44, 0.0, 0.02, 0.0, 2)
r('OBJ_AIRE_ACONDICIONADO', 'objeto-aire', 2, 8, -42, 0.0, 0.02, 0.0, 2)
r('OBJ_CAMARA_SEGURIDAD', 'objeto-camara', 2, 6, -44, 0.0, 0.02, 0.0, 2)
r('OBJ_REFRIGERADOR', 'objeto-refri', 2, 8, -42, 0.0, 0.02, 0.0, 2)
r('OBJ_MAQUINA_EXPENDEDORA', 'objeto-maquina', 1, 8, -34, 0.04, 0.06, 0.5)
r('OBJ_TELE_REINICIO', 'objeto-tele', 1, 10, -33, 0.02, 0.05, 2.0)
# --- 15_Exo_Implantes
r('EXO_ESCUDO_GOLPE', 'exo-escudo', 2, 25, -18, 0.05, 0.06, 0.1)
r('EXO_PIERNAS_SALTO_SUPER', 'exo-salto', 1, 25, -20, 0.04, 0.06, 0.3)
r('EXO_PUESTO_LOOP', 'exo-puesto', 1, 15, -34, 0.02, 0.05, 0.5)
r('EXO_RUEDA_CAMARA_LENTA_ENTRAR', 'rueda-camara-lenta', 1, 0, -20, 0.0, 0.04, 0.3)
r('EXO_RUEDA_CAMARA_LENTA_SALIR', 'rueda-camara-lenta', 1, 0, -24, 0.0, 0.04, 0.3)
r('EXO_SERVOS_CAMINAR', 'exo-servos', 1, 20, -28, 0.05, 0.08, 0.3)
r('EXO_VISOR_ACTIVAR', 'exo-visor', 1, 20, -22, 0.03, 0.05, 0.5)
r('IMP_INSTALAR_TALADRO', 'implante-taladro', 1, 20, -20, 0.02, 0.05, 1.0)
r('IMP_SINAPSIS', 'implante-sinapsis', 2, 15, -22, 0.04, 0.06, 0.1)
# --- 16_Casino (interior: 15 m)
r('CAS_TRAGAMONEDAS', 'casino-tragamonedas', 1, 15, -22, 0.03, 0.06, 1.0)
r('CAS_PREMIO_CHICO', 'casino-premio', 1, 15, -22, 0.03, 0.06, 0.5)
r('CAS_PREMIO_GRANDE', 'casino-premio', 1, 18, -20, 0.03, 0.06, 0.5)
r('CAS_FICHAS', 'casino-fichas', 2, 15, -27, 0.06, 0.08, 0.15)
r('CAS_RULETA_GIRA', 'casino-ruleta', 1, 15, -32, 0.0, 0.03, 1.0)
r('CAS_CARTAS_BARAJAR', 'casino-cartas', 1, 15, -27, 0.04, 0.06, 0.5)
r('CAS_CARTAS_REPARTIR', 'casino-cartas', 1, 15, -28, 0.04, 0.06, 0.5)
# --- 17_Misiones_HUD (2D)
r('HUD_MENSAJE_CELULAR', 'mensaje', 1, 0, -27, 0.02, 0.05, 1.0)
r('MIS_TEMPORIZADOR', 'temporizador', 1, 0, -28.5, 0.02, 0.05, 0.5)
r('PAN_ARRESTADO_ESPOSAS', 'arresto', 1, 0, -22, 0.02, 0.05, 1.0)
r('PAN_HAS_MUERTO', 'muerte', 1, 0, -22, 0.0, 0.03, 5.0)
# --- 18_Radio_Musica (2D)
r('RAD_EST1_CUMBIA_PERCUSION', 'radio', 1, 0, -26, 0.0, 0.0, 0.0, 1)


def seccion(id_):
    p = id_[:3]
    if p in ('PAN', 'NPC') or id_.startswith('ROB_VOZ'):
        return 'Voces'
    if p in ('AMB', 'CLI', 'OBJ') or id_ in ('INT_TALLER_CANEPA', 'INT_TIENDA_TINO_RADIO', 'INT_TIENDA_TINO_VENTILADOR', 'INT_ARMERIA_VENTILADOR', 'INT_BODEGA_GOTERAS'):
        return 'Ambiente'
    if p in ('MUS', 'RAD'):
        return 'Musica'
    if p in ('UI_', 'HUD', 'TIE', 'MIS'):
        return 'Interfaz'
    return 'Efectos'


def fmt(v):
    return ('%.3f' % v).rstrip('0').rstrip('.') if v < 0.1 else ('%.2f' % v).rstrip('0').rstrip('.')


def main():
    medidas = {}
    for f in csv.DictReader(open(sys.argv[1], encoding='utf-8')):
        medidas[f['id']] = f
    ids = list(csv.DictReader(open('herramientas/sonidos/ids-20nov.csv', encoding='utf-8')))
    estado = {}
    for x in ids:
        i = re.match(r'^(.+?)(_\d\d)?$', x['id']).group(1)
        estado.setdefault(i, set()).add(x['estado'])
    # ARM_W07_DISPARO es renombrado pero ya tenia politica desde 19-nov
    nuevos = sorted(i for i, e in estado.items() if e & {'nuevo', 'renombrado'} and 'igual' not in e and i in medidas and i != 'ARM_W07_DISPARO')
    faltan = [i for i in nuevos if i not in R]
    sobran = [i for i in R if i not in nuevos]
    if faltan or sobran:
        sys.exit('faltan reglas: %s; reglas de mas: %s' % (faltan, sobran))
    filas, c = [], []
    for i in nuevos:
        grupo, voces, dist, obj, tono, vvol, minimo, bucles = R[i]
        m = medidas[i]
        lufs_n = float(m['lufs']) - float(m['pico_db'])
        sec = seccion(i)
        ideal = 10 ** ((obj - lufs_n) / 20) / GANANCIA[sec]
        vol = float(fmt(min(1.0, max(0.02, ideal))))
        falta = round(20 * math.log10(ideal / vol), 1) if ideal > 1.0 else 0.0
        filas.append([i, m['carpeta'], ','.join(sorted(estado[i])), m['archivos'], m['duracion_s'], m['rms_db'], m['pico_db'], m['lufs'], round(lufs_n, 1), sec, obj, fmt(vol), round(ideal, 3), falta, grupo, voces, '3D' if dist > 0 else '2D', dist, tono, vvol, minimo, 'si' if bucles else 'no'])
        c.append((i, grupo, voces, vol, tono, minimo, dist, vvol, bucles))
    otros = []
    for i in sorted(estado):
        if i in R or i not in medidas or estado[i] == {'quitado'}:
            continue
        m = medidas[i]
        otros.append([i, m['carpeta'], ','.join(sorted(estado[i])), m['archivos'], m['duracion_s'], m['rms_db'], m['pico_db'], m['lufs'], round(float(m['lufs']) - float(m['pico_db']), 1), seccion(i)] + [''] * 12)
    with open('herramientas/sonidos/niveles-20nov.csv', 'w', newline='', encoding='utf-8') as f:
        w = csv.writer(f)
        w.writerow(['id', 'carpeta', 'estado', 'archivos', 'duracion_s', 'rms_db', 'pico_db', 'lufs_aprox', 'lufs_normalizado', 'seccion', 'objetivo_lufs', 'volumen', 'volumen_ideal', 'falta_db', 'grupo', 'voces', 'espacial', 'distancia_m', 'variacion_tono', 'variacion_volumen', 'segundos_minimos', 'bucle'])
        w.writerows(filas)
        w.writerows(otros)
    lin = ['// <auto-generated> herramientas/sonidos/generar_mezcla_20nov.py (T-1531); no editar a mano: cambia las reglas del script y vuelve a correrlo. Niveles en herramientas/sonidos/niveles-20nov.csv.',
           'namespace MechaCyberpunk.Progresion.Sonido', '{',
           '    /// <summary>T-1531: la política de mezcla de cada ID nuevo del 20-nov (volumen compensado por el nivel medido de su archivo, ya normalizado al pico).</summary>',
           '    public static class TablaMezcla20nov', '    {',
           '        /// <summary>Una fila: grupo, voces, volumen, tono, mínimo entre repeticiones, distancia (0 = 2D) y variación de volumen. Bucles &gt; 0 = es un bucle y se permiten tantos a la vez.</summary>',
           '        public readonly struct Fila', '        {',
           '            public Fila(string id, string grupo, int voces, float volumen, float tono, float minimo, float distancia, float variacionVolumen, int bucles)',
           '            {', '                Id = id;', '                Grupo = grupo;', '                Voces = voces;', '                Volumen = volumen;', '                Tono = tono;', '                Minimo = minimo;', '                Distancia = distancia;', '                VariacionVolumen = variacionVolumen;', '                Bucles = bucles;', '            }', '',
           '            public string Id { get; }', '            public string Grupo { get; }', '            public int Voces { get; }', '            public float Volumen { get; }', '            public float Tono { get; }', '            public float Minimo { get; }', '            public float Distancia { get; }', '            public float VariacionVolumen { get; }', '            public int Bucles { get; }', '        }', '',
           '        public static readonly Fila[] Filas =', '        {']
    for (i, g, v, vol, t, mn, d, vv, b) in c:
        lin.append('            new Fila("%s", "%s", %d, %sf, %sf, %sf, %sf, %sf, %d),' % (i, g, v, fmt(vol), fmt(t) if t else '0', fmt(mn) if mn else '0', fmt(d) if d else '0', fmt(vv) if vv else '0', b))
    lin += ['        };', '    }', '}', '']
    open('Assets/_Proyecto/Progresion/Sonido/TablaMezcla20nov.cs', 'w', encoding='utf-8', newline='\n').write('\n'.join(lin))
    print(len(c), 'IDs nuevos;', sum(1 for f in filas if f[13] > 0), 'no alcanzan el objetivo con volumen 1')
    for f in filas:
        if f[13] > 0:
            print('  falta', f[13], 'dB:', f[0], 'lufs_n', f[8])


main()
