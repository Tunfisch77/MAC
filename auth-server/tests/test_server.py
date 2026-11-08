import io
import json
from pathlib import Path
import sys
import tempfile
import unittest
sys.path.insert(0,str(Path(__file__).resolve().parents[1]))
from server import Application,password_hash,password_ok,normal_name,ApiError

class AuthTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.master='Synthetic Admin Password 47!'
        cls.master_hash=password_hash(cls.master)
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory();self.now=1800000000
        self.app=Application(str(Path(self.temp.name)/'auth.sqlite3'),self.master_hash,True,lambda:self.now)
    def tearDown(self):self.temp.cleanup()
    def req(self,path,body=None,token='',method=None,**env):
        encoded=json.dumps(body or {}).encode();status=[]
        e={'REQUEST_METHOD':method or ('GET' if body is None else 'POST'),'PATH_INFO':path,'CONTENT_TYPE':'application/json','CONTENT_LENGTH':str(len(encoded)),'wsgi.input':io.BytesIO(encoded),'REMOTE_ADDR':'192.0.2.1','wsgi.url_scheme':'https','HTTP_AUTHORIZATION':'Bearer '+token};e.update(env)
        output=b''.join(self.app(e,lambda s,h:status.append((int(s.split()[0]),h))))
        return status[0][0],json.loads(output)
    def login(self,first='',last='',password=None,admin=True,remember=False):
        return self.req('/v1/login',dict(first=first,last=last,password=password or self.master,admin=admin,remember=remember))
    def admin(self):
        code,reply=self.login();self.assertEqual(code,200);return reply['token']
    def create(self,token,first='Tuncay',last='Guliyev',password='Synthetic Advisor 42!'):
        code,reply=self.req('/v1/admin/users',dict(first=first,last=last,password=password),token)
        self.assertEqual(code,200,reply);return reply['user']
    def test_hash_and_names(self):
        a=password_hash('Synthetic secret 1!');b=password_hash('Synthetic secret 1!')
        self.assertNotEqual(a,b);self.assertTrue(password_ok('Synthetic secret 1!',a));self.assertFalse(password_ok('wrong',a))
        self.assertEqual(normal_name('  tuncay  '),'Tuncay');self.assertEqual(normal_name('Öztürk'),'Öztürk')
        with self.assertRaises(ApiError):normal_name('<script>')
    def test_user_pair_permissions_master_and_tokens(self):
        admin=self.admin();user=self.create(admin);other=self.create(admin,'Anna','Muster')
        code,reply=self.login('tuncay','guliyev','Synthetic Advisor 42!',False,True);self.assertEqual(code,200);token=reply['token']
        self.assertEqual(reply['expires']-self.now,30*86400);self.assertEqual(reply['user']['id'],user['id'])
        self.assertEqual(self.req('/v1/admin/users',token=token)[0],403)
        self.assertEqual(self.login('Tuncay','Guliyev','wrong',False)[0],401)
        self.assertEqual(self.login('Unknown','Nobody','Synthetic Advisor 42!',False)[0],401)
        code,master=self.login('Tuncay','Guliyev',admin=True,remember=True);self.assertEqual(code,200);self.assertEqual(master['role'],'admin');self.assertEqual(master['expires']-self.now,8*3600)
        code,assume=self.req('/v1/admin/assume',{'id':other['id']},admin);self.assertEqual(code,200);self.assertEqual(assume['user']['id'],other['id'])
        self.assertEqual(self.req('/v1/me',token=token)[0],200)
        with self.app.connect() as db:
            raw=' '.join(str(tuple(x)) for x in db.execute('SELECT * FROM sessions'))+' '.join(str(tuple(x)) for x in db.execute('SELECT * FROM users'))
        self.assertNotIn(token,raw);self.assertNotIn('Synthetic Advisor 42!',raw);self.assertNotIn(self.master,raw)
    def test_reset_disable_revoke_and_password_change(self):
        admin=self.admin();user=self.create(admin);token=self.login('Tuncay','Guliyev','Synthetic Advisor 42!',False)[1]['token']
        self.assertEqual(self.req('/v1/password',{'old_password':'wrong','new_password':'Synthetic New Password!'},token)[0],403)
        self.assertEqual(self.req('/v1/password',{'old_password':'Synthetic Advisor 42!','new_password':'Synthetic New Password!'},token)[0],200)
        self.assertEqual(self.req('/v1/me',token=token)[0],401)
        token=self.login('Tuncay','Guliyev','Synthetic New Password!',False)[1]['token']
        self.assertEqual(self.req('/v1/admin/user',{'id':user['id'],'enabled':False},admin)[0],200)
        self.assertEqual(self.req('/v1/me',token=token)[0],401);self.assertEqual(self.login('Tuncay','Guliyev',admin=True)[0],401)
        self.assertEqual(self.req('/v1/admin/user',{'id':user['id'],'enabled':True,'password':'Synthetic Reset Password!'},admin)[0],200)
        self.assertEqual(self.login('Tuncay','Guliyev','Synthetic New Password!',False)[0],401)
        token=self.login('Tuncay','Guliyev','Synthetic Reset Password!',False)[1]['token']
        self.assertEqual(self.req('/v1/logout',{},token)[0],200);self.assertEqual(self.req('/v1/me',token=token)[0],401)
    def test_expiration_admin_rotation_and_restart(self):
        admin=self.admin();self.create(admin);token=self.login('Tuncay','Guliyev','Synthetic Advisor 42!',False,True)[1]['token']
        self.app=Application(self.app.database,self.master_hash,True,lambda:self.now)
        self.assertEqual(self.req('/v1/me',token=token)[0],200)
        self.app.admin_epoch='changed';self.assertEqual(self.req('/v1/admin/users',token=admin)[0],401)
        self.now+=31*86400;self.assertEqual(self.req('/v1/me',token=token)[0],401)
    def test_duplicate_and_no_customer_data(self):
        admin=self.admin();self.create(admin)
        self.assertEqual(self.req('/v1/admin/users',dict(first='tuncay',last='GULIYEV',password='Synthetic Another 42!'),admin)[0],409)
        self.assertEqual(self.req('/v1/admin/users',dict(first='Anna',last='Muster',password='short'),admin)[0],400)
        self.assertEqual(self.req('/v1/login',{'first':'A','last':'B','password':'x','customer_records':[]})[0],400)
        self.assertEqual(self.req('/v1/admin/users',token='garbage')[0],401)
        self.assertEqual(self.req('/v1/login',[],CONTENT_LENGTH='20000')[0],413)
        self.assertEqual(self.req('/v1/login',{},CONTENT_TYPE='text/plain')[0],415)
        self.assertEqual(self.req('/v1/login',{},QUERY_STRING='password=bad')[0],400)
        self.app.testing=False;self.assertEqual(self.req('/v1/login',{},**{'wsgi.url_scheme':'http'})[0],400)
        self.assertEqual(self.req('/health',**{'wsgi.url_scheme':'http'})[0],200)
    def test_throttle(self):
        for _ in range(15):self.assertEqual(self.login('Unknown','Nobody','wrong',False)[0],401)
        self.assertEqual(self.login('Unknown','Nobody','wrong',False)[0],429)
        self.now+=901;self.assertEqual(self.login('Unknown','Nobody','wrong',False)[0],401)

if __name__=='__main__':unittest.main(verbosity=2)
