"""Resumable publisher metadata collection; never writes to the application database.

Requires beautifulsoup4. Cache and results belong in ignored artifacts directories.
Only bibliographic facts and review reasons enter the manifest; descriptions stay
in the local source cache. Publisher genre is checked on each product page.
"""
import argparse, hashlib, html, json, re, sys, time, urllib.request, urllib.error
from pathlib import Path
from urllib.parse import urljoin, urlparse

sys.path.insert(0, str(Path('artifacts/novel-publisher-import/python-deps').resolve()))
from bs4 import BeautifulSoup
sys.stdout.reconfigure(encoding='utf-8')

parser = argparse.ArgumentParser()
parser.add_argument('directory')
parser.add_argument('--reuse-candidates',action='store_true')
args = parser.parse_args()
root = Path(args.directory)
root.mkdir(parents=True, exist_ok=True)
cache = root / 'pages'
cache.mkdir(exist_ok=True)

def fetch(url):
    key = hashlib.sha256(url.encode()).hexdigest()
    path = cache / (key + '.html')
    if path.exists():
        return path.read_text(encoding='utf-8')
    time.sleep(1)
    request = urllib.request.Request(url, headers={'User-Agent': 'KitapKivo-CatalogResearch/1.0', 'Accept': 'text/html'})
    with urllib.request.urlopen(request, timeout=40) as response:
        body = response.read().decode('utf-8-sig')
    if 'Just a moment...' in body or 'cf-chl-' in body:
        raise RuntimeError('Access challenge; do not bypass')
    path.write_bytes(body.encode('utf-8'))
    (cache / (key + '.url')).write_text(url, encoding='utf-8')
    return body

def isbn13(value):
    s = re.sub(r'[^0-9X]', '', str(value).upper())
    if len(s) == 10 and sum((10-i)*(10 if c=='X' else int(c)) for i,c in enumerate(s)) % 11 == 0:
        s = '978' + s[:9]
        s += str((-sum(int(c)*(1 if i%2==0 else 3) for i,c in enumerate(s)))%10)
    if len(s) != 13 or not s.isdigit() or not s.startswith(('978','979')):
        return None
    return s if sum(int(c)*(1 if i%2==0 else 3) for i,c in enumerate(s))%10 == 0 else None

def clean(value):
    return re.sub(r'\s+', ' ', html.unescape(str(value))).strip()

def jsonld(soup):
    for script in soup.select('script[type="application/ld+json"]'):
        try:
            value = json.loads(script.string or script.get_text())
            yield from value.get('@graph', [value]) if isinstance(value,dict) else value
        except (ValueError, TypeError):
            continue

exclude_title = re.compile(r'\b(seti?|kutulu|öyküler|hikâyeler|hikayeler|şiirler|rubailer|denemeler|çizgi roman|manga|seçme eserler)\b', re.I)
exclude_text = re.compile(r'\b(öykü (kitabı|derlemesi)|hikâye (kitabı|derlemesi)|şiir (kitabı|derlemesi)|deneme (kitabı|derlemesi)|novella|roman olmayan)\b', re.I)
candidates = {}
if args.reuse_candidates:
    candidates=json.loads((root/'candidates.json').read_text(encoding='utf-8'))

# Classical series first. Each item's individual page must still identify Roman.
series = [] if args.reuse_candidates else ['hasan-ali-yucel-klasikleri/hay-roman', 'modern-klasikler/roman-modern-klasikler',
          'modern-turk-edebiyati-klasikleri/roman-modern-turk-edebiyati-klasikleri',
          'turk-edebiyati-klasikleri/roman-turk-edebiyati-klasikleri', 'turk-edebiyati/roman']
for series_index, part in enumerate(series):
    url = 'https://www.iskultur.com.tr/kitap/' + part
    visited = set()
    for page in range(1,41):
        if url in visited: break
        visited.add(url)
        try: soup = BeautifulSoup(fetch(url), 'html.parser')
        except Exception as e:
            print('LIST ERROR',url,type(e).__name__,flush=True); break
        added = 0
        for a in soup.select('a.product[href]'):
            title = a.select_one('h3'); author = a.select_one('.yazar')
            link = a['href']
            if title and author and link not in candidates:
                candidates[link] = dict(Source='IsKultur', Author=clean(author.get_text()),
                    Title=clean(title.get_text()), Priority=10+series_index, ListUrl=url)
                added += 1
        print('LIST',part,page,'new',added,'total',len(candidates),flush=True)
        nxt = soup.select_one('link[rel="next"]')
        if not nxt or not added: break
        url = urljoin(url,nxt['href'])

for page in ([] if args.reuse_candidates else range(1,61)):
    url = f'https://www.dogankitap.com.tr/kitaplarimiz/romanlar/{page}'
    try: soup = BeautifulSoup(fetch(url), 'html.parser')
    except Exception as e:
        print('LIST ERROR',url,type(e).__name__,flush=True); break
    added = 0
    for a in soup.select('#kitaplistesi a.kitapkare[href]'):
        title = a.select_one('.k-baslik'); author = a.select_one('.k-yazar')
        link = urljoin(url, a['href'])
        if title and author and link not in candidates:
            candidates[link] = dict(Source='Dogan', Author=clean(author.get_text()),
                Title=clean(title.get_text()), Priority=100, ListUrl=url)
            added += 1
    print('LIST Dogan',page,'new',added,'total',len(candidates),flush=True)
    if not added or not soup.select_one('.sayfalama a.next[href]'): break

(root/'candidates.json').write_text(json.dumps(candidates,ensure_ascii=False,indent=2),encoding='utf-8')
accepted, rejected = [], []
for i,(url,candidate) in enumerate(candidates.items()):
    try:
        if exclude_title.search(candidate['Title']): raise ValueError('collection or excluded form in title')
        body = fetch(url)
        soup = BeautifulSoup(body,'html.parser')
        kind = 'Book' if candidate['Source']=='Dogan' else 'Product'
        product = next((x for x in jsonld(soup) if x.get('@type')==kind),None)
        if not product: raise ValueError('missing structured product')
        title = clean(product.get('name',''))
        if title != candidate['Title']:
            # Product headings resolve list subtitles that were joined without whitespace.
            heading=soup.select_one('.kd-baslik') if candidate['Source']=='Dogan' else None
            heading_title=clean(' '.join(heading.find_all(string=True,recursive=False))) if heading else ''
            if not heading or heading_title!=title:
                raise ValueError('list/product title mismatch')
        if exclude_title.search(title): raise ValueError('excluded title')
        if candidate['Source']=='Dogan':
            if product.get('genre') != 'Romanlar': raise ValueError('product not Romanlar')
            authors = [clean(a.get_text()) for a in soup.select('.kd-yazar a')]
            if len(authors)!=1: raise ValueError('ambiguous multiple authors/editors')
            if clean(product.get('author',{}).get('name','')) != authors[0]: raise ValueError('author mismatch')
            isbn = isbn13(product.get('isbn',''))
            description = soup.select_one('.kd-aciklama')
            year = str(product.get('datePublished',''))[:4]
            pages = str(product.get('numberOfPages',''))
            original = soup.select_one('.kd-kunye .hidden')
            original_language = clean(original.get_text()).split(':')[-1].strip() if original else ''
            original_code = {'Türkçe':'tur','İngilizce':'eng','Fransızca':'fra','Almanca':'deu','İspanyolca':'spa','İtalyanca':'ita','Rusça':'rus'}.get(original_language,'und')
            genre = 'Romanlar'
        else:
            genre = clean(product.get('pattern',''))
            if not re.search(r'(^|/)\s*Roman\s*($|/)',genre,re.I): raise ValueError('product not Roman')
            authors = [candidate['Author']]
            isbn = isbn13(product.get('sku',''))
            description = soup.select_one('.woocommerce-product-details__short-description')
            if not description: description = soup.select_one('#tab-description')
            if not description: description = soup.select_one('.text-underline.lineH20')
            props = {x.get('name'):x.get('value') for x in product.get('additionalProperty',[])}
            year = str(props.get('Yılı','')); pages = str(props.get('Sayfa Sayısı',''))
            original_code = 'und'
        if not isbn: raise ValueError('invalid ISBN')
        if not isbn.startswith(('978975','978605','978625')): raise ValueError('Turkish edition imprint requires review')
        if any(x in title.lower() for x in ['english','ingilizce','i̇ngilizce','children’s classic']): raise ValueError('foreign edition')
        if description and exclude_text.search(description.get_text(' ',strip=True)): raise ValueError('conflicting literary form in description')
        cover = product.get('image','')
        if isinstance(cover,list): cover=cover[0]
        if isinstance(cover,dict): cover=cover.get('url','')
        cover = urljoin(url,cover)
        if not cover.startswith('https://') or any(x in cover.lower() for x in ['placeholder','no-image','default']): raise ValueError('missing cover')
        accepted.append(dict(Title=title,AuthorNames=authors,Isbn13=isbn,CoverImageUrl=cover,
            SourceUrl=url,SourceCategory=genre,Publisher='Doğan Kitap' if kind=='Book' else 'İş Bankası Kültür Yayınları',
            LanguageCode='tur',OriginalLanguageCode=original_code,
            PageCount=int(pages) if pages.isdigit() and int(pages)>0 else None,
            PublicationYear=int(year) if year.isdigit() and 1450<int(year)<2100 else None,
            Priority=candidate['Priority'],CategorySlugs=['klasikler'] if candidate['Priority']<14 else [],
            SourceSha256=hashlib.sha256(body.encode()).hexdigest(),ListUrl=candidate['ListUrl']))
    except Exception as e:
        rejected.append(dict(SourceUrl=url,Title=candidate['Title'],Reason=str(e)[:160]))
    if (i+1)%20==0:
        (root/'publisher-candidates.json').write_text(json.dumps(accepted,ensure_ascii=False,indent=2),encoding='utf-8')
        (root/'rejected.json').write_text(json.dumps(rejected,ensure_ascii=False,indent=2),encoding='utf-8')
        print('DETAIL',i+1,'/',len(candidates),'accepted',len(accepted),'rejected',len(rejected),flush=True)
(root/'publisher-candidates.json').write_text(json.dumps(accepted,ensure_ascii=False,indent=2),encoding='utf-8')
(root/'rejected.json').write_text(json.dumps(rejected,ensure_ascii=False,indent=2),encoding='utf-8')
print('DONE',len(accepted),'accepted',len(rejected),'rejected',flush=True)
