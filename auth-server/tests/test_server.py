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
        return status[0][0],json.loads(output) if output else None
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

    def auto(self,first='',last='',password=None,remember=False,**env):
        return self.req('/v2/login',dict(first=first,last=last,password=self.master if password is None else password,remember=remember),**env)
    def test_automatic_login_and_roles(self):
        code,admin=self.auto(remember=True)
        self.assertEqual(code,200);self.assertEqual(admin['role'],'admin');self.assertIsNone(admin['user'])
        self.assertEqual(admin['expires']-self.now,8*3600)
        user=self.create(admin['token'])
        code,advisor=self.auto('tuncay','GULIYEV','Synthetic Advisor 42!',True)
        self.assertEqual(code,200);self.assertEqual(advisor['role'],'advisor')
        self.assertEqual(advisor['expires']-self.now,30*86400)
        self.assertEqual(self.req('/v1/admin/users',token=advisor['token'])[0],403)
        code,target=self.auto('Tuncay','Guliyev',remember=True)
        self.assertEqual(code,200);self.assertEqual(target['role'],'admin');self.assertEqual(target['user']['id'],user['id'])
        self.assertEqual(target['expires']-self.now,8*3600)
        for first,last,pw in [('Tuncay','Guliyev','wrong'),('Unknown','Person',self.master),('Guliyev','Tuncay','Synthetic Advisor 42!'),('','','Synthetic Advisor 42!')]:
            self.assertEqual(self.auto(first,last,pw)[0],401)
        self.assertEqual(self.auto('Only','')[0],400)
        self.assertEqual(self.req('/v2/login',{'admin':True,'password':self.master})[0],400)
        self.req('/v1/admin/user',{'id':user['id'],'enabled':False},admin['token'])
        self.assertEqual(self.auto('Tuncay','Guliyev')[0],401)
        self.assertEqual(self.auto('Tuncay','Guliyev','Synthetic Advisor 42!')[0],401)
        self.assertEqual(self.req('/v1/me',token=target['token'])[0],401)
    def test_master_reuse_rejected_and_legacy_collision_is_not_admin(self):
        admin=self.admin();user=self.create(admin)
        self.assertEqual(self.req('/v1/admin/users',dict(first='No',last='Reuse',password=self.master),admin)[0],400)
        self.assertEqual(self.req('/v1/admin/user',dict(id=user['id'],password=self.master),admin)[0],400)
        token=self.auto('Tuncay','Guliyev','Synthetic Advisor 42!')[1]['token']
        self.assertEqual(self.req('/v1/password',dict(old_password='Synthetic Advisor 42!',new_password=self.master),token)[0],400)
        with self.app.connect() as db:
            db.execute('UPDATE users SET password=? WHERE id=?',(self.master_hash,user['id']))
        self.assertEqual(self.auto('Tuncay','Guliyev')[1]['role'],'advisor')
    def test_auto_rate_limit_and_transport(self):
        for _ in range(15):self.assertEqual(self.auto('Unknown','Person','wrong')[0],401)
        self.assertEqual(self.auto('Unknown','Person','wrong')[0],429)
        self.now+=901
        self.app.testing=False
        self.assertEqual(self.auto(**{'wsgi.url_scheme':'http'})[0],400)
        # An application-level spoofed header must not bypass scheme enforcement.
        self.assertEqual(self.auto(**{'wsgi.url_scheme':'http','HTTP_X_FORWARDED_PROTO':'https'})[0],400)
        code,health=self.req('/health');self.assertEqual(code,200);self.assertTrue(health['auto_login'])
        self.assertEqual(self.req('/')[0],200)
        self.assertEqual(self.req('/',method='HEAD'),(200,None))
        self.assertEqual(self.req('/',method='HEAD',**{'wsgi.url_scheme':'http'}),(400,None))
        self.assertEqual(self.req('/health',method='HEAD',**{'wsgi.url_scheme':'http'}),(200,None))
    def test_master_limit_shared_with_legacy_route(self):
        for i in range(60):
            self.assertEqual(self.auto(password='wrong',REMOTE_ADDR='192.0.2.'+str(i+1))[0],401)
        self.assertEqual(self.auto(REMOTE_ADDR='192.0.2.200')[0],429)
        self.assertEqual(self.login()[0],429)

if __name__=='__main__':unittest.main(verbosity=2)
