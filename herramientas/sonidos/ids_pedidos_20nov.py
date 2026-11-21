"""Lista los IDs de sonido que pide el codigo del juego (literales entre comillas en Assets/_Proyecto, sin Tests ni Editor) y dice si tienen archivo.
Uso (desde la raiz del repo): python herramientas/sonidos/ids_pedidos_20nov.py <carpeta Assets/Arte/Audio> [salida.txt]
ANTES = lo que tenia el CatalogoSonidos.asset comprometido (T-1471) + los alias de MapaSonidos19nov.
DESPUES = los archivos que hay ahora en las carpetas de audio (sin los descartados) + los alias de MapaSonidos19nov y MapaSonidos20nov cuyo destino existe."""
import glob, os, re, sys

PREFIJOS = "JUG|PUN|ARM|VEH|PAN|CAL|ZON|ECO|UI|MUN|AMB|MUS|HUD|TIE|MEL|IMP|EXP|FUE|POL|NOV|NPC|ROB|DRN|CLI|OBJ|EXO|CAS|MIS|RAD|INT|CON|PAS|NAV|DES|MOTO"
LITERAL = re.compile(r'"((?:%s)_[A-Z0-9_]+)"' % PREFIJOS)
VARIANTE = re.compile(r'^(.+?)(_\d\d)?$')
DESCARTADOS = {"POL_RADIO_DESPACHO"}  # sus archivos siguen en Assets pero ya no se usan (LEEME 20-nov)


def pares(ruta):
    if not os.path.exists(ruta):
        return {}
    return dict(re.findall(r'Par\("([A-Z0-9_]+)", "([A-Z0-9_]+)"', open(ruta, encoding='utf-8').read()))


def main():
    audio = sys.argv[1]
    salida = sys.argv[2] if len(sys.argv) > 2 else 'herramientas/sonidos/ids-pedidos-20nov.txt'
    usados = {}
    for f in glob.glob('Assets/_Proyecto/**/*.cs', recursive=True):
        n = f.replace(os.sep, '/')
        if '/Tests/' in n or '/Editor/' in n:
            continue
        for m in LITERAL.finditer(open(f, encoding='utf-8').read()):
            usados.setdefault(m.group(1), set()).add(os.path.basename(f))
    # ANTES
    antes = set()
    texto = open('Assets/_Proyecto/Progresion/Datos/CatalogoSonidos.asset', encoding='utf-8').read()
    for bloque in re.split(r'\n  - id: ', texto.split('  entradas:')[1].split('  interruptores:')[0])[1:]:
        id_ = bloque.split('\n')[0].strip()
        if re.search(r'finales:\n    - \{', bloque) or re.search(r'provisionales:\n    - \{', bloque):
            antes.add(id_)
    p19 = pares('Assets/_Proyecto/Progresion/Sonido/MapaSonidos19nov.cs')
    p20 = pares('Assets/_Proyecto/Progresion/Sonido/MapaSonidos20nov.cs')
    antes_alias = {v for v, n in p19.items() if n in antes}
    # DESPUES
    despues = set()
    for f in glob.glob(os.path.join(audio, '**', '*.wav'), recursive=True):
        despues.add(VARIANTE.match(os.path.splitext(os.path.basename(f))[0]).group(1))
    for f in glob.glob(os.path.join(audio, '**', '*.mp3'), recursive=True):
        despues.add(VARIANTE.match(os.path.splitext(os.path.basename(f))[0]).group(1))
    despues -= DESCARTADOS
    alias = dict(p19)
    alias.update(p20)
    despues_alias = {v for v, n in alias.items() if n in despues}
    lineas = ["IDs pedidos por el codigo del juego (literales, sin Tests ni Editor): %d" % len(usados), ""]
    sin_antes, sin_despues = [], []
    for id_ in sorted(usados):
        a = id_ in antes or id_ in antes_alias
        d = id_ in despues or id_ in despues_alias or any(x.startswith(id_ + '_') for x in despues)  # un prefijo (ROB_VOZ), no un ID
        if not a: sin_antes.append(id_)
        if not d: sin_despues.append(id_)
        via = ''
        if d and id_ in alias and id_ not in despues: via = ' (alias -> %s)' % alias[id_]
        lineas.append("%s %s%s  <- %s" % ('OK ' if d else 'SIN', id_, via, ', '.join(sorted(usados[id_]))))
    lineas += ["", "(ANTES depende del codigo que se este revisando: corre el script sobre un `git archive` de la rama base para compararlo.)", "Sin archivo ANTES (%d): %s" % (len(sin_antes), ', '.join(sin_antes)), "Sin archivo DESPUES (%d): %s" % (len(sin_despues), ', '.join(sin_despues))]
    open(salida, 'w', encoding='utf-8').write('\n'.join(lineas) + '\n')
    print('\n'.join(lineas[-2:]))


main()
