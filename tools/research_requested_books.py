"""Cache public bibliographic source pages for the requested list. Never writes DB data."""
import hashlib,json,sys,time,urllib.request,urllib.error
from pathlib import Path
from urllib.parse import urlparse
sys.stdout.reconfigure(encoding='utf-8')
root=Path('artifacts/reading-list-100-20260928/research');root.mkdir(parents=True,exist_ok=True)
allowed={'www.iskultur.com.tr','www.canyayinlari.com','www.yapikrediyayinlari.com.tr','iletisim.com.tr','www.ithakiyayingrubu.com','www.kitapyurdu.com','www.nadirkitap.com','openlibrary.org'}
for url in json.loads(Path(sys.argv[1]).read_text(encoding='utf-8-sig')):
    if urlparse(url).hostname not in allowed or urlparse(url).scheme!='https':raise ValueError('Unapproved source')
    path=root/(hashlib.sha256(url.encode()).hexdigest()+'.html')
    if path.exists():print('CACHED',url);continue
    try:
        req=urllib.request.Request(url,headers={'User-Agent':'KitapKivo-CatalogResearch/1.0','Accept':'text/html,application/json'})
        with urllib.request.urlopen(req,timeout=25) as r:data=r.read()
        if b'cf-chl-' in data or b'Just a moment...' in data:raise ValueError('Access challenge')
        path.write_bytes(data);path.with_suffix('.url').write_text(url,encoding='utf-8')
        print('OK',url,len(data),flush=True)
    except Exception as e:print('ERROR',url,str(e),flush=True)
    time.sleep(1)
