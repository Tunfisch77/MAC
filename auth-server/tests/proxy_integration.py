import os,sys,tempfile,subprocess,socket,time,urllib.request,urllib.error,json,runpy
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT))
from server import password_hash
with tempfile.TemporaryDirectory() as d:
 env=os.environ.copy();env.update(MAC_DATABASE=d+'/test.sqlite3',MAC_ADMIN_HASH=password_hash('Synthetic Proxy Admin 47!'),RENDER='true')
 env.pop('FORWARDED_ALLOW_IPS',None)
 with socket.socket() as sock:sock.bind(('127.0.0.1',0));port=sock.getsockname()[1]
 env['PORT']=str(port)
 p=subprocess.Popen([sys.executable,'-m','gunicorn','-c','gunicorn.conf.py','server:create_app()'],cwd=ROOT,env=env,stdout=subprocess.PIPE,stderr=subprocess.PIPE)
 def call(path,headers={},method='GET'):
  req=urllib.request.Request('http://127.0.0.1:'+str(port)+path,headers=headers,method=method)
  try:r=urllib.request.urlopen(req,timeout=2)
  except urllib.error.HTTPError as e:r=e
  with r:return r.status,r.read()
 try:
  for _ in range(80):
   if p.poll() is not None:raise Exception(p.stderr.read().decode())
   try:
    if call('/health')[0]==200:break
   except (OSError,urllib.error.URLError):pass
   time.sleep(.1)
  assert call('/')[0]==400,'HTTP unexpectedly allowed'
  status,body=call('/',{'X-Forwarded-Proto':'https'});assert status==200 and json.loads(body)['auto_login'] is True
  assert call('/',{'X-Forwarded-Proto':'https'},'HEAD')==(200,b'')
  assert call('/health',method='HEAD')==(200,b'')
 finally:
  p.terminate();out,err=p.communicate(timeout=10)
  assert b'no-body' not in err and b'Permission denied' not in err,err.decode()
 print('PASS: real Gunicorn, Render proxy default, HTTP rejected, forwarded HTTPS accepted, HEAD body-free, no control-socket error')
