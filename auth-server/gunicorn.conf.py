import os
bind='0.0.0.0:'+os.environ.get('PORT','10000')
workers=2
threads=2
timeout=45
accesslog=None
errorlog='-'
capture_output=False
loglevel='warning'
# Render is the only public ingress. Do not expose this port directly on a VPS.
forwarded_allow_ips=os.environ.get('FORWARDED_ALLOW_IPS','127.0.0.1')
secure_scheme_headers={'X-FORWARDED-PROTO':'https'}
limit_request_line=2048
limit_request_fields=30
limit_request_field_size=4096
umask=0o077
