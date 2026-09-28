"""Discover publisher evidence for the explicit 100-title request; no DB writes."""
import hashlib,json,re,sys,time,unicodedata,urllib.request
from pathlib import Path
from concurrent.futures import ThreadPoolExecutor
sys.path.insert(0,str(Path('artifacts/novel-publisher-import/python-deps').resolve()))
from bs4 import BeautifulSoup
sys.stdout.reconfigure(encoding='utf-8')
root=Path('artifacts/reading-list-100-20260928/publisher')
(root/'pages').mkdir(parents=True,exist_ok=True)
rows=json.loads(Path('artifacts/reading-list-100-20260928/matches.json').read_text(encoding='utf-8'))
overrides={61:'monte-cristo-kontu',66:'madame-bovary',97:'orlando',98:'gurur-ve-onyargi',99:'emma'}
def slug(s):
    s=''.join(c for c in unicodedata.normalize('NFKD',s.lower().replace('ı','i')) if not unicodedata.combining(c))
    return re.sub(r'[^a-z0-9]+','-',s.replace("'",'').replace('’','')).strip('-')
def fetch(row):
    url='https://www.iskultur.com.tr/'+overrides.get(row['Number'],slug(row['Title']))+'.aspx'
    if row['Number'] in (7,11): return None
    path=root/'pages'/(hashlib.sha256(url.encode()).hexdigest()+'.html')
    try:
        if path.exists():body=path.read_text(encoding='utf-8')
        elif '--cached-only' in sys.argv:
            return None
        else:
            req=urllib.request.Request(url,headers={'User-Agent':'KitapKivo-CatalogResearch/1.0'})
            with urllib.request.urlopen(req,timeout=25) as response:body=response.read().decode('utf-8-sig')
            path.write_text(body,encoding='utf-8')
            path.with_suffix('.url').write_text(url,encoding='utf-8')
            time.sleep(1)
        soup=BeautifulSoup(body,'html.parser')
        for script in soup.select('script[type="application/ld+json"]'):
            text=re.sub(r'^\s*/\*"publisher":.*?\*/\s*$','',script.string or script.get_text(),flags=re.M)
            for match in re.finditer(r'\{\s*"@type"\s*:\s*"Product"',text):
                try:product,_=json.JSONDecoder().raw_decode(text[match.start():])
                except ValueError:continue
                # Retain raw evidence for manual title/author/form review.
                authors=[a.get_text(' ',strip=True) for a in soup.select('[itemprop="author"]')]
                return dict(Number=row['Number'],SourceUrl=url,Product=product,Authors=list(dict.fromkeys(authors)),Text=soup.get_text(' ',strip=True))
    except Exception as e: print(row['Number'],type(e).__name__,str(e),flush=True)
    return None
with ThreadPoolExecutor(max_workers=3) as pool:
    found=[]
    for row in pool.map(fetch,rows):
        if row:
            found.append(row)
            print(row['Number'],row['Product'].get('name'),row['Product'].get('pattern'),row['Authors'],flush=True)
            (root/'evidence.json').write_text(json.dumps(found,ensure_ascii=False,indent=2),encoding='utf-8')
print('DONE',len(found))
