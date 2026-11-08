"""Initialize only the mounted data directory, then permanently drop root."""
import os
from pathlib import Path
os.umask(0o077)
Path('/data').mkdir(exist_ok=True)
if os.getuid()==0:
    os.chown('/data',10001,10001)
    for name in ('mac-auth.sqlite3','mac-auth.sqlite3-journal','mac-auth.sqlite3-wal','mac-auth.sqlite3-shm'):
        path=Path('/data')/name
        if path.is_file() and not path.is_symlink():
            os.chown(path,10001,10001);os.chmod(path,0o600)
    os.setgroups([]);os.setgid(10001);os.setuid(10001)
os.execvp('gunicorn',['gunicorn','--config','gunicorn.conf.py','server:create_app()'])
