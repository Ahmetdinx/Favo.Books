"""Verify exact source hashes and reachable edition covers before the offline import."""
import hashlib,io,json,sys,time,urllib.request
from pathlib import Path
from urllib.parse import urlparse
from datetime import datetime,timezone,timedelta
sys.path.insert(0,str(Path('artifacts/novel-publisher-import/python-deps').resolve()))
from PIL import Image
sys.stdout.reconfigure(encoding='utf-8')
batch=sys.argv[1] if len(sys.argv)>1 else 'reading-list-100-20260928'
if not batch or any(c not in 'abcdefghijklmnopqrstuvwxyz0123456789-' for c in batch):raise ValueError('Invalid batch')
root=Path('data/curation')/batch
art=Path('artifacts')/batch/'editions'
payload=(root/'manifest.json').read_bytes();rows=json.loads(payload)
checks_path=art/'cover-checks.json'
checks=json.loads(checks_path.read_text(encoding='utf-8')) if checks_path.exists() else {}
allowed={'img.kitapyurdu.com','static.nadirkitap.com'}
class Redirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self,req,fp,code,msg,headers,url):
        if urlparse(url).scheme!='https' or urlparse(url).hostname not in allowed:raise ValueError('Unapproved redirect')
        return super().redirect_request(req,fp,code,msg,headers,url)
opener=urllib.request.build_opener(Redirect())
verified=[];errors=[]
for e in rows:
    try:
        source=art/'pages'/(hashlib.sha256(e['SourceUrl'].encode()).hexdigest()+'.html')
        if hashlib.sha256(source.read_bytes()).hexdigest()!=e['SourceSha256']:raise ValueError('Source hash mismatch')
        url=e['CoverImageUrl']
        if urlparse(url).scheme!='https' or urlparse(url).hostname not in allowed:raise ValueError('Unapproved cover')
        cached=checks.get(url)
        if not cached or datetime.fromisoformat(cached['CheckedAtUtc'])<datetime.now(timezone.utc)-timedelta(hours=24):
            time.sleep(.75)
            with opener.open(urllib.request.Request(url,headers={'User-Agent':'FavoBooks-CatalogResearch/1.0','Range':'bytes=0-65535'}),timeout=30) as response:
                mime=response.headers.get_content_type();body=response.read(65536)
            if not mime.startswith('image/'):raise ValueError('Not an image')
            with Image.open(io.BytesIO(body)) as im:width,height=im.size
            if width<80 or height<100:raise ValueError('Too small')
            checks[url]=dict(Width=width,Height=height,Mime=mime,CheckedAtUtc=datetime.now(timezone.utc).isoformat())
            checks_path.write_text(json.dumps(checks,indent=2),encoding='utf-8')
        verified.append(e['Number'])
    except Exception as ex:errors.append(dict(Number=e['Number'],Reason=str(ex)));print('ERROR',e['Number'],str(ex),flush=True)
    if len(verified)%10==0:print('VERIFIED',len(verified),flush=True)
summary=dict(ManifestSha256=hashlib.sha256(payload).hexdigest(),Verified=len(verified),
    CheckedAtUtc=datetime.now(timezone.utc).isoformat(),Errors=errors)
(root/'verification.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(summary,ensure_ascii=False))
if errors:sys.exit(1)
