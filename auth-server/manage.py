"""Offline administration helper; never pass passwords as command-line arguments."""
import argparse
import getpass
import os
import sqlite3
from pathlib import Path
from server import password_hash, check_password

parser=argparse.ArgumentParser()
parser.add_argument('action',choices=['hash-admin','backup'])
parser.add_argument('--destination')
args=parser.parse_args()
if args.action=='hash-admin':
    a=getpass.getpass('Neues Adminpasswort (mindestens 20 Zeichen): ')
    b=getpass.getpass('Wiederholen: ')
    if a!=b or len(a)<20:
        raise SystemExit('Passwörter müssen übereinstimmen und mindestens 20 Zeichen lang sein.')
    check_password(a)
    print('\nDiesen Hash als MAC_ADMIN_HASH in der Hosting-Verwaltung hinterlegen:\n'+password_hash(a))
else:
    if not args.destination:
        raise SystemExit('--destination fehlt. Sicherer Speicherort außerhalb des Repositorys erforderlich.')
    path=Path(args.destination)
    if path.exists(): raise SystemExit('Zieldatei existiert bereits.')
    source=os.environ.get('MAC_DATABASE','/data/mac-auth.sqlite3')
    if not Path(source).is_file(): raise SystemExit('Quelldatenbank fehlt.')
    with sqlite3.connect(source) as src, sqlite3.connect(str(path)) as dst:
        src.backup(dst)
    os.chmod(path,0o600)
    print('Konsistente Kontensicherung erstellt. Enthält Passwort-Hashes; geschützt aufbewahren, niemals auf GitHub hochladen.')
