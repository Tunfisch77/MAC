"""MAC authentication API. No customer-data endpoints, cookies or request-body logging."""
import contextlib
import base64
import hashlib
import hmac
import json
import os
from pathlib import Path
import re
import secrets
import sqlite3
import time
import unicodedata

ITERATIONS = 600_000


def password_hash(password):
    salt = secrets.token_bytes(24)
    key = hashlib.pbkdf2_hmac('sha256', password.encode('utf-8'), salt, ITERATIONS)
    return 'pbkdf2_sha256$%d$%s$%s' % (ITERATIONS, base64.b64encode(salt).decode(), base64.b64encode(key).decode())


def valid_hash(value):
    try:
        alg, count, salt, key = value.split('$')
        return alg == 'pbkdf2_sha256' and 600000 <= int(count) <= 2000000 and 16 <= len(base64.b64decode(salt, validate=True)) <= 64 and len(base64.b64decode(key, validate=True)) == 32
    except (ValueError, TypeError):
        return False


def password_ok(password, value):
    alg, count, salt, expected = value.split('$')
    actual = hashlib.pbkdf2_hmac('sha256', password.encode('utf-8'), base64.b64decode(salt), int(count))
    return hmac.compare_digest(actual, base64.b64decode(expected))


def normal_name(value):
    if not isinstance(value, str):
        raise ApiError(400, 'Vorname und Nachname prüfen.')
    value = ' '.join(unicodedata.normalize('NFKC', value).split())
    if not 1 <= len(value) <= 80 or not any(c.isalpha() for c in value) or any(not (c.isalpha() or unicodedata.category(c).startswith('M') or c in " -'’.") for c in value):
        raise ApiError(400, 'Namen: 1–80 Zeichen; Buchstaben, Leerzeichen, Bindestrich, Punkt oder Apostroph.')
    return ' '.join(w[0].upper() + w[1:] for w in value.split(' '))


def login_key(first, last):
    return (first + '\x1f' + last).casefold()


def check_password(value):
    if not isinstance(value, str) or not 14 <= len(value) <= 128:
        raise ApiError(400, 'Passwort muss 14–128 Zeichen lang sein. Eine lange Passphrase ist geeignet.')
    return value


class ApiError(Exception):
    def __init__(self, status, message):
        self.status, self.message = status, message


class Application:
    def __init__(self, database=None, admin_hash=None, testing=False, clock=time.time):
        self.testing, self.clock = testing, clock
        self.database = database or os.environ.get('MAC_DATABASE', '/data/mac-auth.sqlite3')
        self.admin_hash = admin_hash or os.environ.get('MAC_ADMIN_HASH', '')
        if not valid_hash(self.admin_hash):
            raise RuntimeError('MAC_ADMIN_HASH fehlt oder ist ungültig. Mit manage.py hash-admin erzeugen.')
        self.admin_epoch = hashlib.sha256(self.admin_hash.encode()).hexdigest()
        self.dummy_hash = password_hash(secrets.token_urlsafe(32))
        Path(self.database).parent.mkdir(parents=True, exist_ok=True)
        with self.connect() as db:
            db.executescript('''
            CREATE TABLE IF NOT EXISTS users(id TEXT PRIMARY KEY, first TEXT NOT NULL, last TEXT NOT NULL,
              login TEXT NOT NULL UNIQUE, password TEXT NOT NULL, enabled INTEGER NOT NULL DEFAULT 1);
            CREATE TABLE IF NOT EXISTS sessions(token_hash TEXT PRIMARY KEY, user_id TEXT, role TEXT NOT NULL,
              expires INTEGER NOT NULL, admin_epoch TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS limits(key TEXT PRIMARY KEY, since INTEGER NOT NULL, count INTEGER NOT NULL);
            CREATE INDEX IF NOT EXISTS session_user ON sessions(user_id);
            ''')
        os.chmod(self.database, 0o600)

    @contextlib.contextmanager
    def connect(self):
        db = sqlite3.connect(self.database, timeout=10)
        db.row_factory = sqlite3.Row
        db.execute('PRAGMA busy_timeout=10000')
        try:
            with db:
                yield db
        finally:
            db.close()

    def throttle(self, db, label, value, maximum, interval=900):
        now = int(self.clock())
        key = hashlib.sha256((label + '\0' + value).encode()).hexdigest()
        # Short atomic transaction across workers; rejected attempts are committed too.
        db.execute('INSERT INTO limits VALUES(?,?,1) ON CONFLICT(key) DO UPDATE SET count=CASE WHEN since<=? THEN 1 ELSE count+1 END, since=CASE WHEN since<=? THEN ? ELSE since END', (key, now, now-interval, now-interval, now))
        count = db.execute('SELECT count FROM limits WHERE key=?', (key,)).fetchone()[0]
        db.execute('DELETE FROM limits WHERE since<?', (now-86400,))
        db.commit()
        if count > maximum:
            raise ApiError(429, 'Zu viele Versuche. Bitte in 15 Minuten erneut versuchen.')

    @staticmethod
    def user_public(user):
        return dict(id=user['id'], first=user['first'], last=user['last'], enabled=bool(user['enabled'])) if user else None

    def session(self, db, token):
        if not re.fullmatch(r'[A-Za-z0-9_-]{43}', token or ''):
            raise ApiError(401, 'Anmeldung erforderlich oder abgelaufen.')
        row = db.execute('SELECT * FROM sessions WHERE token_hash=?', (hashlib.sha256(token.encode()).hexdigest(),)).fetchone()
        if row is None or row['expires'] <= self.clock() or (row['role'] == 'admin' and row['admin_epoch'] != self.admin_epoch):
            raise ApiError(401, 'Anmeldung erforderlich oder abgelaufen.')
        user = db.execute('SELECT * FROM users WHERE id=?', (row['user_id'],)).fetchone() if row['user_id'] else None
        if row['user_id'] and (not user or not user['enabled']):
            raise ApiError(401, 'Anmeldung erforderlich oder abgelaufen.')
        return row, user

    def issue(self, db, user, role, remember=False):
        token = secrets.token_urlsafe(32)
        expiry = int(self.clock()) + (30*86400 if remember and role == 'advisor' else 8*3600)
        db.execute('DELETE FROM sessions WHERE expires<=?', (int(self.clock()),))
        db.execute('INSERT INTO sessions VALUES(?,?,?,?,?)', (hashlib.sha256(token.encode()).hexdigest(), user['id'] if user else None, role, expiry, self.admin_epoch if role == 'admin' else ''))
        return dict(token=token, role=role, user=self.user_public(user), expires=expiry)

    def route(self, db, method, path, body, token, ip):
        if method == 'GET' and path == '/health':
            return {'service':'mac-auth', 'api':1}
        if method == 'POST' and path == '/v1/login':
            self.keys(body, {'first','last','password','admin','remember'})
            self.throttle(db, 'login-ip', ip, 40)
            admin = body.get('admin', False)
            if not isinstance(admin, bool) or not isinstance(body.get('remember', False), bool):
                raise ApiError(400, 'Ungültige Anmeldung.')
            password = body.get('password', '')
            if not isinstance(password, str) or not 1 <= len(password) <= 128:
                raise ApiError(401, 'Name oder Passwort stimmt nicht.')
            first, last = body.get('first', ''), body.get('last', '')
            # Explicit admin-only login also works before the first advisor is created.
            if admin and first == '' and last == '':
                user = None
            else:
                first, last = normal_name(first), normal_name(last)
                key = login_key(first, last)
                if not admin:
                    self.throttle(db, 'login-account', key, 15)
                user = db.execute('SELECT * FROM users WHERE login=?', (key,)).fetchone()
            if admin:
                self.throttle(db, 'master-global', 'admin', 60)
                ok = password_ok(password, self.admin_hash)
            else:
                ok = password_ok(password, user['password'] if user else self.dummy_hash)
            if not ok or (not user and not (admin and first == '' and last == '')) or (user and not user['enabled']):
                raise ApiError(401, 'Name oder Passwort stimmt nicht.')
            return self.issue(db, user, 'admin' if admin else 'advisor', body.get('remember', False))
        session, user = self.session(db, token)
        if method == 'GET' and path == '/v1/me':
            return dict(role=session['role'], user=self.user_public(user), expires=session['expires'])
        if method == 'POST' and path == '/v1/logout':
            self.keys(body, set())
            db.execute('DELETE FROM sessions WHERE token_hash=?', (session['token_hash'],))
            return {'ok':True}
        if method == 'POST' and path == '/v1/password':
            self.keys(body, {'old_password','new_password'})
            if session['role'] != 'advisor' or not user:
                raise ApiError(403, 'Das eigene Passwort kann nur im Beratermodus geändert werden.')
            self.throttle(db, 'change-password', user['id'], 10)
            old = body.get('old_password','')
            if not isinstance(old,str) or len(old)>128 or not password_ok(old,user['password']):
                raise ApiError(403, 'Bisheriges Passwort stimmt nicht.')
            new = check_password(body.get('new_password'))
            db.execute('UPDATE users SET password=? WHERE id=?', (password_hash(new),user['id']))
            db.execute('DELETE FROM sessions WHERE user_id=?', (user['id'],))
            return {'ok':True}
        if session['role'] != 'admin':
            raise ApiError(403, 'Adminberechtigung erforderlich.')
        self.throttle(db, 'admin-operations', session['token_hash'], 1000)
        if path == '/v1/admin/users' and method == 'GET':
            return {'users':[self.user_public(u) for u in db.execute('SELECT * FROM users ORDER BY last,first')]}
        if path == '/v1/admin/users' and method == 'POST':
            self.keys(body, {'first','last','password'})
            first, last = normal_name(body.get('first')), normal_name(body.get('last'))
            password = check_password(body.get('password'))
            uid = secrets.token_hex(16)
            try:
                db.execute('INSERT INTO users(id,first,last,login,password) VALUES(?,?,?,?,?)', (uid,first,last,login_key(first,last),password_hash(password)))
            except sqlite3.IntegrityError:
                raise ApiError(409, 'Dieser Beratername ist bereits vergeben. Bei Namensgleichheit einen eindeutigen Namenszusatz vereinbaren.')
            return {'user':self.user_public(db.execute('SELECT * FROM users WHERE id=?',(uid,)).fetchone())}
        if path == '/v1/admin/user' and method == 'POST':
            self.keys(body, {'id','password','enabled'})
            uid = body.get('id')
            if not isinstance(uid,str) or not re.fullmatch('[a-f0-9]{32}', uid) or not db.execute('SELECT 1 FROM users WHERE id=?',(uid,)).fetchone():
                raise ApiError(404, 'Berater nicht gefunden.')
            if 'password' in body:
                db.execute('UPDATE users SET password=? WHERE id=?',(password_hash(check_password(body['password'])),uid))
            if 'enabled' in body:
                if not isinstance(body['enabled'],bool):
                    raise ApiError(400,'Ungültiger Kontostatus.')
                db.execute('UPDATE users SET enabled=? WHERE id=?',(int(body['enabled']),uid))
            # Revoke all old sessions on reset, lock, unlock or explicit revoke.
            db.execute('DELETE FROM sessions WHERE user_id=?',(uid,))
            return {'ok':True}
        if path == '/v1/admin/assume' and method == 'POST':
            self.keys(body, {'id'})
            uid=body.get('id')
            if not isinstance(uid,str):
                raise ApiError(400,'Berater auswählen.')
            target=db.execute('SELECT * FROM users WHERE id=? AND enabled=1',(uid,)).fetchone()
            if not target:
                raise ApiError(404,'Aktiven Berater auswählen.')
            return self.issue(db,target,'admin')
        raise ApiError(404, 'Funktion nicht gefunden.')

    @staticmethod
    def keys(body, allowed):
        if not isinstance(body,dict) or set(body)-allowed:
            raise ApiError(400,'Unzulässige Anfragefelder.')

    def __call__(self, env, start_response):
        status=200
        try:
            if not self.testing and env.get('wsgi.url_scheme') != 'https' and env.get('PATH_INFO') != '/health':
                raise ApiError(400,'Nur HTTPS ist erlaubt.')
            method, path=env.get('REQUEST_METHOD',''),env.get('PATH_INFO','')
            if method not in ('GET','POST'):
                raise ApiError(405,'Methode nicht erlaubt.')
            if env.get('QUERY_STRING'):
                raise ApiError(400,'URL-Parameter sind nicht erlaubt.')
            try: length=int(env.get('CONTENT_LENGTH') or 0)
            except ValueError: raise ApiError(400,'Ungültige Anfrage.')
            if length<0 or length>16384:
                raise ApiError(413,'Anfrage zu groß.')
            body={}
            if method=='POST':
                if env.get('CONTENT_TYPE','').split(';')[0].lower()!='application/json':
                    raise ApiError(415,'JSON erforderlich.')
                try: body=json.loads(env['wsgi.input'].read(length))
                except (ValueError,UnicodeDecodeError): raise ApiError(400,'Ungültiges JSON.')
                if not isinstance(body,dict): raise ApiError(400,'JSON-Objekt erforderlich.')
            auth=env.get('HTTP_AUTHORIZATION','')
            token=auth[7:] if auth.startswith('Bearer ') else ''
            with self.connect() as db:
                result=self.route(db,method,path,body,token,env.get('REMOTE_ADDR','unknown'))
        except ApiError as e:
            status,result=e.status,{'error':e.message}
        except Exception:
            # Never emit request, headers, credentials or traceback to a log.
            status,result=500,{'error':'Anmeldedienst vorübergehend nicht verfügbar.'}
        data=json.dumps(result,ensure_ascii=False).encode('utf-8')
        labels={200:'OK',400:'Bad Request',401:'Unauthorized',403:'Forbidden',404:'Not Found',405:'Method Not Allowed',409:'Conflict',413:'Content Too Large',415:'Unsupported Media Type',429:'Too Many Requests',500:'Internal Server Error'}
        headers=[('Content-Type','application/json; charset=utf-8'),('Content-Length',str(len(data))),('Cache-Control','no-store'),('X-Content-Type-Options','nosniff'),('Strict-Transport-Security','max-age=31536000')]
        if status==429: headers.append(('Retry-After','900'))
        start_response(str(status)+' '+labels[status],headers)
        return [data]


def create_app():
    return Application()
