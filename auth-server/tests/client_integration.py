"""Run precompiled C# client against isolated local WSGI API with synthetic credentials."""
from pathlib import Path
import os,subprocess,sys,tempfile,threading
from wsgiref.simple_server import make_server,WSGIRequestHandler
sys.path.insert(0,str(Path(__file__).resolve().parents[1]))
from server import Application,password_hash
class Quiet(WSGIRequestHandler):
    def log_message(self,*args):pass
with tempfile.TemporaryDirectory() as folder:
    app=Application(str(Path(folder)/'db.sqlite3'),password_hash('Synthetic Admin Password 47!'),True)
    server=make_server('127.0.0.1',0,app,handler_class=Quiet)
    thread=threading.Thread(target=server.serve_forever,daemon=True);thread.start()
    try:
        command=sys.argv[1:]+['http://127.0.0.1:'+str(server.server_port)]
        subprocess.run(command,check=True,timeout=90)
    finally:server.shutdown();server.server_close();thread.join()
