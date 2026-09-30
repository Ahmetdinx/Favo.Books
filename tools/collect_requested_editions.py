"""Read-only bibliographic research for an explicit reading list; no database access."""
import hashlib,json,re,sys,time,urllib.request,unicodedata
from pathlib import Path
from urllib.parse import quote
sys.path.insert(0,str(Path('artifacts/novel-publisher-import/python-deps').resolve()))
from bs4 import BeautifulSoup
sys.stdout.reconfigure(encoding='utf-8')
batch=sys.argv[sys.argv.index('--batch')+1] if '--batch' in sys.argv else 'reading-list-100-20260928'
if not re.fullmatch(r'[a-z0-9-]+',batch):raise ValueError('Invalid batch')
data=Path('data/curation')/batch
root=Path('artifacts')/batch/'editions';(root/'pages').mkdir(parents=True,exist_ok=True)
rows=[dict(Number=i,Title=x.split('|')[0].strip(),Author=x.split('|')[1].strip()) for i,x in enumerate((data/'requested.txt').read_text(encoding='utf-8').splitlines(),1)]
queries={7:'On Kişiydiler',11:'Kadın Haggard',25:'Örümcek Ağı White',26:'Zencefil Adam',27:'Martı',38:'Yaşlı Adam ve Deniz',59:'Yevgeni Onegin',61:'Monte Cristo Kontu',66:'Madam Bovary',88:'Narziss Goldmund',93:'Sanatçının Gençlik Portresi',95:'Mrs Dalloway'}
if batch!='reading-list-100-20260928':queries={}
if (data/'queries.json').exists():queries.update({int(k):v for k,v in json.loads((data/'queries.json').read_text(encoding='utf-8')).items()})
if '--numbers' in sys.argv:
    numbers={int(x) for x in sys.argv[sys.argv.index('--numbers')+1].split(',')}
    rows=[r for r in rows if r['Number'] in numbers]
def fetch(url):
    path=root/'pages'/(hashlib.sha256(url.encode()).hexdigest()+'.html')
    if path.exists():return path.read_text(encoding='utf-8-sig')
    time.sleep(1)
    req=urllib.request.Request(url,headers={'User-Agent':'FavoBooks-CatalogResearch/1.0'})
    with urllib.request.urlopen(req,timeout=30) as r:body=r.read().decode('utf-8-sig')
    if 'cf-chl-' in body or 'Just a moment...' in body:raise ValueError('Access challenge')
    path.write_bytes(body.encode());path.with_suffix('.url').write_text(url,encoding='utf-8')
    return body
def text(node):return node.get_text(' ',strip=True) if node else ''
if '--details' not in sys.argv:
    retry='--retry' in sys.argv
    results=json.loads((root/'search.json').read_text(encoding='utf-8')) if (root/'search.json').exists() else []
    if retry:rows=[r for r in rows if r['Number'] in {1,4,5,13,18,20,23,27,55,60,61,62,64,66,79,92,93,99,100}]
    for row in rows:
        try:
            url='https://www.kitapyurdu.com/index.php?route=product/search&filter_name='+quote(queries.get(row['Number'],row['Title']))+'&limit=100'
            soup=BeautifulSoup(fetch(url),'html.parser');items=[]
            for card in soup.select('.ky-product'):
                a=card.select_one('.ky-product-title[href]')
                if a:items.append(dict(Title=text(a),Author=text(card.select_one('.ky-product-author')),Publisher=text(card.select_one('.ky-product-publisher')),Url=a['href'].split('?')[0]))
            results=[r for r in results if r['Number']!=row['Number']]
            results.append(dict(**row,Candidates=items[:100]));print(row['Number'],row['Title'],'candidates',len(items),flush=True)
        except Exception as e:results.append(dict(**row,Error=str(e),Candidates=[]));print(row['Number'],str(e),flush=True)
        (root/'search.json').write_text(json.dumps(results,ensure_ascii=False,indent=2),encoding='utf-8')
else:
    selected=json.loads((data/'selected-editions.json').read_text(encoding='utf-8'))
    if '--numbers' in sys.argv:selected=[r for r in selected if r['Number'] in numbers]
    result=json.loads((root/'details.json').read_text(encoding='utf-8')) if (root/'details.json').exists() else []
    for row in selected:
        try:
            body=fetch(row['Url']);soup=BeautifulSoup(body,'html.parser')
            products=[]
            for node in soup.select('script[type="application/ld+json"]'):
                try:products.append(json.loads(node.string or node.get_text()))
                except ValueError:pass
            result=[r for r in result if r['Number']!=row['Number']]
            result.append(dict(**row,Structured=products,Text=text(soup),Images=[dict(x.attrs) for x in soup.select('img') if 'kitapyurdu.com' in x.get('src','')]))
            print(row['Number'],row['Title'],'OK',flush=True)
        except Exception as e:print(row['Number'],'ERROR',str(e),flush=True)
        (root/'details.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
