"""Collect the publisher's explicit Turkish-literature novel category into the same source cache."""
import hashlib,json,re,sys,time,urllib.request
from pathlib import Path
from urllib.parse import urljoin
sys.path.insert(0,str(Path('artifacts/novel-publisher-import/python-deps').resolve()))
from bs4 import BeautifulSoup
sys.stdout.reconfigure(encoding='utf-8')
root=Path(sys.argv[1]);cache=root/'pages';cache.mkdir(parents=True,exist_ok=True)
def fetch(url):
    key=hashlib.sha256(url.encode()).hexdigest();path=cache/(key+'.html')
    if path.exists():return path.read_text(encoding='utf-8')
    time.sleep(1)
    with urllib.request.urlopen(urllib.request.Request(url,headers={'User-Agent':'FavoBooks-CatalogResearch/1.0'}),timeout=40) as response:
        body=response.read().decode('utf-8-sig')
    if 'cf-chl-' in body:raise ValueError('Access challenge')
    path.write_bytes(body.encode('utf-8'));(cache/(key+'.url')).write_text(url,encoding='utf-8')
    return body
def text(node):return re.sub(r'\s+',' ',node.get_text(' ',strip=True)).strip() if node else ''
items={};accepted=[];rejected=[]
for page in range(1,15):
    url=f'https://www.ithakiyayingrubu.com/turk-edebiyati-roman?pagenumber={page}&pagesize=24'
    soup=BeautifulSoup(fetch(url),'html.parser');added=0
    for a in soup.select('.product-card__name a[href]'):
        link=urljoin(url,a['href'])
        if link not in items:items[link]=(text(a),url);added+=1
    print('ITHAKI LIST',page,added,'total',len(items),flush=True)
    if not added:break
for index,(url,(expected,listurl)) in enumerate(items.items()):
    try:
        body=fetch(url);soup=BeautifulSoup(body,'html.parser');title=text(soup.select_one('.product__name'))
        category=text(soup.select_one('.product__features .productfeatures-category .value'))
        if title!=expected or category!='Türk Edebiyatı-Roman':raise ValueError('Title/category mismatch')
        authors=[text(a) for a in soup.select('#Specificationyazar .value')]
        if len(authors)!=1:raise ValueError('Missing or ambiguous author')
        isbn=text(soup.select_one('.product__features .gtin .value'))
        if not re.fullmatch(r'978(?:975|605|625)\d{7}',isbn) or sum(int(c)*(1 if i%2==0 else 3) for i,c in enumerate(isbn))%10:
            raise ValueError('Invalid Turkish-edition ISBN')
        publisher=text(soup.select_one('.productfeatures-manufacturer .value'))
        if publisher not in ['İthaki Yayınları','Müptela Yayınları']:raise ValueError('Unexpected publisher')
        desc=text(soup.select_one('.product__description'))
        if re.search(r'\b(öykü (kitabı|derlemesi)|hikâye (kitabı|derlemesi)|şiir (kitabı|derlemesi)|deneme (kitabı|derlemesi)|novella)\b',desc,re.I):
            raise ValueError('Conflicting form')
        cover=soup.select_one('.product__gallery img[data-full]')
        if not cover:raise ValueError('Missing cover')
        year=text(soup.select_one('#Specificationyayintarihi .value'));pages=text(soup.select_one('#Specificationsayfasayisi .value'))
        accepted.append(dict(Title=title,AuthorNames=authors,Isbn13=isbn,CoverImageUrl=cover['data-full'],SourceUrl=url,
            SourceCategory=category,Publisher=publisher,LanguageCode='tur',OriginalLanguageCode='und',
            PageCount=int(pages) if pages.isdigit() else None,PublicationYear=int(year) if year.isdigit() else None,
            Priority=100,CategorySlugs=[],SourceSha256=hashlib.sha256(body.encode()).hexdigest(),ListUrl=listurl))
    except Exception as e:rejected.append(dict(SourceUrl=url,Title=expected,Reason=str(e)[:160]))
    if (index+1)%20==0:print('ITHAKI DETAIL',index+1,'/',len(items),'accepted',len(accepted),flush=True)
    (root/'ithaki-candidates.json').write_text(json.dumps(accepted,ensure_ascii=False,indent=2),encoding='utf-8')
    (root/'ithaki-rejected.json').write_text(json.dumps(rejected,ensure_ascii=False,indent=2),encoding='utf-8')
print('DONE ITHAKI',len(accepted),flush=True)
