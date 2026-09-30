"""Supplement known novel candidates with publisher-published genre membership.
These category lists never approve a book as a novel on their own.
"""
import hashlib,json,sys,time,urllib.request
from pathlib import Path
from urllib.parse import urljoin
sys.path.insert(0,str(Path('artifacts/novel-publisher-import/python-deps').resolve()))
from bs4 import BeautifulSoup
sys.stdout.reconfigure(encoding='utf-8')
root=Path(sys.argv[1]);cache=root/'pages';cache.mkdir(exist_ok=True)
genres={'klasikler':['klasikler'],'ask':['ask-ve-romantik'],'romantik':['ask-ve-romantik'],
        'bilim-kurgu':['bilim-kurgu'],'fantastik':['fantastik'],'korku':['korku'],
        'polisiye-gerilim':['polisiye-ve-gizem','gerilim']}
result={}
for category,slugs in genres.items():
    seen=set()
    for page in range(1,31):
        url=f'https://www.dogankitap.com.tr/kitaplarimiz/{category}/{page}'
        key=hashlib.sha256(url.encode()).hexdigest();path=cache/(key+'.html')
        try:
            if path.exists():body=path.read_text(encoding='utf-8')
            else:
                time.sleep(1)
                with urllib.request.urlopen(urllib.request.Request(url,headers={'User-Agent':'FavoBooks-CatalogResearch/1.0'}),timeout=40) as response:
                    body=response.read().decode('utf-8-sig')
                if 'cf-chl-' in body:raise ValueError('Access challenge')
                path.write_bytes(body.encode('utf-8'));(cache/(key+'.url')).write_text(url,encoding='utf-8')
            soup=BeautifulSoup(body,'html.parser');added=0
            for a in soup.select('#kitaplistesi a.kitapkare[href]'):
                link=urljoin(url,a['href'])
                if link in seen:continue
                seen.add(link);added+=1
                entry=result.setdefault(link,{'CategorySlugs':[],'GenreSourceUrls':[]})
                entry['CategorySlugs']=sorted(set(entry['CategorySlugs']+slugs))
                entry['GenreSourceUrls'].append(url)
            print('GENRE',category,page,added,flush=True)
            if not added:break
        except Exception as error:
            print('GENRE ERROR',url,str(error)[:120],flush=True);break
    (root/'publisher-genres.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
print('DONE GENRES',len(result),flush=True)
